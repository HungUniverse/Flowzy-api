using System.Globalization;
using System.Text.Json;
using Flowzy.Repository.Entities;
using Flowzy.Repository.Repositories;
using Flowzy.Service.Contracts;
using Flowzy.Service.Exceptions;

namespace Flowzy.Service.Groups;

/// <summary>Java GroupInvitationServiceImpl / GroupJoinRequestServiceImpl compatibility.</summary>
public sealed class GroupMembershipService(IGroupMembershipRepository repository, TimeProvider time) : IGroupMembershipService
{
    private DateTime Now => time.GetUtcNow().UtcDateTime;
    private bool Expired(DateTime createdAt) => createdAt.AddHours(72) < Now;
    private Task<Account> Account(string email, CancellationToken ct) => RequireAccount(email, ct);
    private async Task<Account> RequireAccount(string email, CancellationToken ct) =>
        await repository.AccountAsync(email, ct) ?? throw new UnauthorizedException("User account not found");
    private async Task<StudentGroup> Group(long id, bool locked, CancellationToken ct) =>
        await repository.GroupAsync(id, locked, ct) ?? throw new NotFoundException($"Group not found with id: {id}");
    private static void Unlocked(StudentGroup group)
    {
        if (group.IsLocked) throw new ConflictException("Group membership is locked. Unlock the group before changing members");
    }
    private async Task<Student> Leader(StudentGroup group, string email, string message, CancellationToken ct)
    {
        var student = await repository.StudentAsync(email, ct) ?? throw new ForbiddenException(message);
        if (group.LeaderStudentId != student.Id) throw new ForbiddenException(message);
        return student;
    }
    private static void Pending(string status, string entity)
    {
        if (status != "PENDING") throw new BadRequestException($"{entity} is not pending");
    }

    public async Task<InvitationResponse> InviteAsync(long groupId, CreateInvitationRequest request, string email, CancellationToken ct)
    {
        await using var transaction = await repository.BeginAsync(ct);
        var group = await Group(groupId, true, ct);
        Unlocked(group);
        var caller = await Account(email, ct);
        Student inviter;
        if (caller.Role == "ADMIN") inviter = group.LeaderStudent ?? throw new BadRequestException("Cannot invite to a group without a leader");
        else
        {
            StudentTermWriteGuard.RequireWritable(group);
            inviter = await Leader(group, email, "Only group leader or admin can invite", ct);
        }
        var value = request.StudentCodeOrEmail?.Trim() ?? "";
        if (value.Length == 0) throw new BadRequestException("Student code or email is required");
        var invitee = await repository.ResolveInviteeAsync(value, ct)
            ?? throw new NotFoundException($"Student profile not found for code or email: {value}");
        if (inviter.Id == invitee.Id) throw new BadRequestException("Cannot invite yourself");
        if (await repository.IsMemberAsync(groupId, invitee.Id, ct)) throw new BadRequestException("Student is already a member of this group");
        if (await repository.HasMembershipAsync(invitee.Id, group.Term, group.CourseCode, ct)) throw new BadRequestException("Student is already in a group for this course");
        if (await repository.MemberCountAsync(groupId, ct) >= 6) throw new BadRequestException("Group is already full");
        if ((await repository.InvitationsAsync(groupId, invitee.Id, true, ct)).Count != 0)
            throw new BadRequestException("An invitation is already pending for this student");
        var invitation = new GroupInvitation { GroupId = groupId, InviterStudentId = inviter.Id, InviteeStudentId = invitee.Id,
            Status = "PENDING", Message = request.Message, CreatedAt = Now };
        repository.AddInvitation(invitation);
        await repository.SaveAsync(ct);
        await Notify(invitee.AccountId, "GROUP_INVITATION_CREATED", "New Group Invitation",
            $"You have been invited to join group {group.Name} by {inviter.FullName}.", "OPEN_GROUP_INVITATIONS", groupId, invitation.Id, true, ct);
        await repository.SaveAsync(ct);
        await transaction.CommitAsync(ct);
        return Map(invitation, group, inviter, invitee);
    }

    public async Task<IReadOnlyList<InvitationResponse>> InvitationsAsync(long? groupId, string email, CancellationToken ct)
    {
        List<GroupInvitation> invitations;
        if (groupId is null)
        {
            var student = await repository.StudentAsync(email, ct) ?? throw new NotFoundException($"Student profile not found for email: {email}");
            invitations = (await repository.InvitationsAsync(null, student.Id, true, ct)).Where(x => !Expired(x.CreatedAt)).ToList();
        }
        else
        {
            var group = await Group(groupId.Value, false, ct);
            if ((await Account(email, ct)).Role != "ADMIN")
                await Leader(group, email, "Only group leader or admin can view group invitations", ct);
            invitations = await repository.InvitationsAsync(groupId, null, false, ct);
        }
        return invitations.Select(x => Map(x, x.Group, x.InviterStudent, x.InviteeStudent)).ToList();
    }

    public async Task RespondInvitationAsync(long id, string action, string email, CancellationToken ct)
    {
        await using var transaction = await repository.BeginAsync(ct);
        var invitation = await repository.InvitationAsync(id, ct) ?? throw new NotFoundException($"Invitation not found with id: {id}");
        Pending(invitation.Status, "Invitation");
        if (Expired(invitation.CreatedAt)) throw new BadRequestException("Invitation has expired");
        if (action == "cancel")
        {
            if ((await Account(email, ct)).Role != "ADMIN")
            {
                var group = await Group(invitation.GroupId, true, ct);
                StudentTermWriteGuard.RequireWritable(group);
                await Leader(group, email, "Only group leader or admin can cancel invitation", ct);
                await repository.ReloadAsync(invitation, ct);
                Pending(invitation.Status, "Invitation");
            }
            invitation.Status = "CANCELED";
        }
        else
        {
            var student = await repository.StudentAsync(email, ct) ?? throw new NotFoundException($"Student profile not found for email: {email}");
            if (action == "accept")
                _ = await repository.LockStudentAsync(student.Id, ct) ?? throw new NotFoundException("Student profile not found");
            if (invitation.InviteeStudentId != student.Id) throw new ForbiddenException($"You can only {action} invitations sent to you");
            var group = await Group(invitation.GroupId, true, ct);
            StudentTermWriteGuard.RequireWritable(group);
            await repository.ReloadAsync(invitation, ct);
            Pending(invitation.Status, "Invitation");
            if (action == "accept")
            {
                Unlocked(group);
                if (group.Status != "ACTIVE") throw new BadRequestException("Group is not active");
                if (await repository.MemberCountAsync(group.Id, ct) >= 6) throw new BadRequestException("Group is already full, cannot accept invitation");
                if (await repository.HasMembershipAsync(student.Id, group.Term, group.CourseCode, ct))
                    throw new BadRequestException("You are already a member of a group in the same term and course");
                invitation.Status = "ACCEPTED";
                repository.AddMember(new StudentGroupMember { GroupId = group.Id, StudentId = student.Id, MemberRole = "MEMBER", JoinedAt = Now });
                await Cleanup(student.Id, group, invitation.Id, null, ct);
            }
            else invitation.Status = "DECLINED";
            if (group.LeaderStudent is not null)
                await Notify(group.LeaderStudent.AccountId, action == "accept" ? "GROUP_INVITATION_ACCEPTED" : "GROUP_INVITATION_DECLINED",
                    action == "accept" ? "Invitation Accepted" : "Invitation Declined",
                    $"{student.FullName} has {(action == "accept" ? "accepted" : "declined")} the invitation to join your group {group.Name}.",
                    "OPEN_GROUP", group.Id, invitation.Id, true, ct);
        }
        invitation.RespondedAt = Now;
        await repository.SaveAsync(ct);
        await transaction.CommitAsync(ct);
    }

    public async Task<JoinRequestResponse> JoinAsync(long groupId, CreateJoinRequest request, string email, CancellationToken ct)
    {
        await using var transaction = await repository.BeginAsync(ct);
        var group = await Group(groupId, true, ct);
        Unlocked(group);
        if (group.Status != "ACTIVE") throw new BadRequestException("Group is not active");
        if ((await Account(email, ct)).Role != "STUDENT") throw new ForbiddenException("Only students can submit join requests");
        StudentTermWriteGuard.RequireWritable(group);
        var student = await repository.StudentAsync(email, ct) ?? throw new NotFoundException("Student profile not found");
        if (await repository.HasMembershipAsync(student.Id, group.Term, group.CourseCode, ct)) throw new BadRequestException("You are already in a group for this course and term");
        if (await repository.IsMemberAsync(groupId, student.Id, ct)) throw new BadRequestException("You are already a member of this group");
        if (await repository.MemberCountAsync(groupId, ct) >= 6) throw new BadRequestException("Group is already full");
        if ((await repository.InvitationsAsync(groupId, student.Id, true, ct)).Count != 0)
            throw new BadRequestException("You have a pending invitation for this group");
        var pending = (await repository.RequestsAsync(groupId, student.Id, true, ct)).FirstOrDefault();
        if (pending is not null)
        {
            if (!Expired(pending.CreatedAt)) throw new BadRequestException("You have already submitted a pending join request for this group");
            pending.Status = "CANCELED"; pending.RespondedAt = Now; pending.UpdatedAt = Now;
            // Flush the old pending row before inserting its replacement (partial unique index).
            await repository.SaveAsync(ct);
        }
        var join = new GroupJoinRequest { GroupId = groupId, StudentId = student.Id, Status = "PENDING", Message = request.Message, CreatedAt = Now, UpdatedAt = Now };
        repository.AddRequest(join);
        await repository.SaveAsync(ct);
        if (group.LeaderStudent is not null)
            await Notify(group.LeaderStudent.AccountId, "GROUP_JOIN_REQUEST_CREATED", "New Join Request",
                $"{student.FullName} has requested to join your group {group.Name}.", "OPEN_GROUP", groupId, join.Id, false, ct, true);
        await repository.SaveAsync(ct);
        await transaction.CommitAsync(ct);
        return Map(join, group, student, null);
    }

    public async Task<IReadOnlyList<JoinRequestResponse>> JoinRequestsAsync(long? groupId, string email, CancellationToken ct)
    {
        Account caller;
        long? studentId = null;
        if (groupId is null)
        {
            caller = await Account(email, ct);
            if (caller.Role != "STUDENT") throw new ForbiddenException("Only students can view their join requests");
            studentId = (await repository.StudentAsync(email, ct) ?? throw new NotFoundException("Student profile not found")).Id;
        }
        else
        {
            var group = await Group(groupId.Value, false, ct);
            caller = await Account(email, ct);
            if (caller.Role == "MENTOR")
            {
                var mentor = await repository.MentorAsync(email, ct) ?? throw new ForbiddenException("Mentor profile not found");
                if (group.MentorId != mentor.Id) throw new ForbiddenException("You are not the assigned mentor for this group");
            }
            else if (caller.Role == "STUDENT")
            {
                var student = await repository.StudentAsync(email, ct) ?? throw new ForbiddenException("Student profile not found");
                if (group.LeaderStudentId != student.Id && !await repository.IsMemberAsync(group.Id, student.Id, ct))
                    throw new ForbiddenException("Only group members can view join requests");
            }
            else if (caller.Role != "ADMIN") throw new ForbiddenException("Unauthorized role to view join requests");
        }
        return (await repository.RequestsAsync(groupId, studentId, false, ct)).Select(x => Map(x, x.Group, x.Student, x.RespondedByStudent)).ToList();
    }

    public async Task RespondJoinAsync(long? groupId, long id, string action, string email, CancellationToken ct)
    {
        await using var transaction = await repository.BeginAsync(ct);
        var request = await repository.RequestAsync(id, ct) ?? throw new NotFoundException($"Join request not found with id: {id}");
        if (action == "approve")
        {
            _ = await repository.LockStudentAsync(request.StudentId, ct) ?? throw new NotFoundException("Student profile not found");
            await repository.ReloadAsync(request, ct);
        }
        Pending(request.Status, "Join request");
        if (action == "approve" && Expired(request.CreatedAt)) throw new BadRequestException("Join request has expired");
        if (groupId.HasValue && request.GroupId != groupId) throw new BadRequestException("Join request does not belong to this group");
        Student? responder;
        bool studentCaller;
        var group = await Group(request.GroupId, false, ct);
        if (action == "cancel")
        {
            responder = await repository.StudentAsync(email, ct);
            if (responder?.Id != request.StudentId) throw new ForbiddenException("Only the requester can cancel the join request");
            studentCaller = true;
        }
        else
        {
            studentCaller = (await Account(email, ct)).Role != "ADMIN";
            if (studentCaller) await Leader(group, email, $"Only group leader or admin can {action} join request", ct);
            responder = await repository.StudentAsync(email, ct);
        }
        group = await Group(request.GroupId, true, ct);
        await repository.ReloadAsync(request, ct);
        Pending(request.Status, "Join request");
        if (action == "approve") Unlocked(group);
        if (studentCaller) StudentTermWriteGuard.RequireWritable(group);
        if (action == "approve")
        {
            if (group.Status != "ACTIVE") throw new BadRequestException("Group is not active");
            if (await repository.MemberCountAsync(group.Id, ct) >= 6) throw new BadRequestException("Group is already full, cannot approve join request");
            if (await repository.HasMembershipAsync(request.StudentId, group.Term, group.CourseCode, ct))
                throw new BadRequestException("Student is already in a group for this course and term");
            repository.AddMember(new StudentGroupMember { GroupId = group.Id, StudentId = request.StudentId, MemberRole = "MEMBER", JoinedAt = Now });
            request.Status = "ACCEPTED";
            await Cleanup(request.StudentId, group, null, request.Id, ct);
        }
        else request.Status = action == "reject" ? "REJECTED" : "CANCELED";
        if (action != "cancel")
        {
            request.RespondedByStudentId = responder?.Id;
            await Notify(request.Student.AccountId, action == "approve" ? "GROUP_JOIN_REQUEST_APPROVED" : "GROUP_JOIN_REQUEST_REJECTED",
                action == "approve" ? "Join Request Approved" : "Join Request Rejected",
                $"Your request to join group {group.Name} has been {(action == "approve" ? "approved" : "rejected")}.",
                "OPEN_GROUP", group.Id, request.Id, false, ct);
        }
        request.RespondedAt = Now; request.UpdatedAt = Now;
        await repository.SaveAsync(ct);
        await transaction.CommitAsync(ct);
    }

    private async Task Cleanup(long studentId, StudentGroup group, long? invitationId, long? requestId, CancellationToken ct)
    {
        foreach (var other in await repository.InvitationsAsync(null, studentId, true, ct))
            if (other.Id != invitationId && other.Group.Term == group.Term && other.Group.CourseCode == group.CourseCode)
            { other.Status = "DECLINED"; other.RespondedAt = Now; }
        foreach (var other in await repository.RequestsAsync(null, studentId, true, ct))
            if (other.Id != requestId && other.Group.Term == group.Term && other.Group.CourseCode == group.CourseCode)
            { other.Status = "CANCELED"; other.RespondedAt = Now; other.UpdatedAt = Now; }
    }

    private Task Notify(long recipient, string type, string title, string body, string action, long groupId, long entityId,
        bool invitation, CancellationToken ct, bool requestsSection = false)
    {
        var id = entityId.ToString(CultureInfo.InvariantCulture);
        var values = new Dictionary<string, string> { ["groupId"] = groupId.ToString(CultureInfo.InvariantCulture), [invitation ? "invitationId" : "requestId"] = id };
        if (requestsSection) values["section"] = "requests";
        return repository.AddNotificationAsync(new Notification { RecipientId = recipient, Type = type, Title = title, Body = body,
            ActionKey = action, ActionParams = JsonSerializer.Serialize(values), EntityType = invitation ? "GroupInvitation" : "GroupJoinRequest",
            EntityId = id, EventKey = type + ":" + id, CreatedAt = Now, UpdatedAt = Now }, ct);
    }

    private static InvitationResponse Map(GroupInvitation e, StudentGroup g, Student inviter, Student target) =>
        new(e.Id, g.Id, g.Name, g.GroupNo, g.CourseCode, g.Term, inviter.Id, inviter.StudentCode, inviter.FullName,
            target.Id, target.StudentCode, target.FullName, target.Id, target.StudentCode, target.FullName, e.Status, e.Message, e.CreatedAt, e.RespondedAt);
    private static JoinRequestResponse Map(GroupJoinRequest e, StudentGroup g, Student student, Student? responder) =>
        new(e.Id, g.Id, g.Name, g.GroupNo, g.CourseCode, g.Term, student.Id, student.StudentCode, student.FullName,
            e.Status, e.Message, responder?.Id, responder?.StudentCode, responder?.FullName, e.RespondedAt, e.CreatedAt, e.UpdatedAt);
}
