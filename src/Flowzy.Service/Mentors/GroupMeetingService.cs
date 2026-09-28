using System.Globalization;
using System.Text.Json;
using Flowzy.Repository.Entities;
using Flowzy.Repository.Repositories;
using Flowzy.Service.Contracts;
using Flowzy.Service.Exceptions;
using Flowzy.Service.Groups;

namespace Flowzy.Service.Mentors;

public sealed class GroupMeetingService(IGroupMeetingRepository repository, TimeProvider clock) : IGroupMeetingService
{
    // PostgreSQL timestamps round-trip at microsecond precision; event keys must survive a retry.
    private DateTime Now { get { var value = clock.GetUtcNow().UtcDateTime; return value.AddTicks(-(value.Ticks % 10)); } }
    private static readonly TimeZoneInfo BusinessZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");

    public async Task<IReadOnlyList<MentorAvailabilitySlotResponse>> Availability(long gid, string email, CancellationToken ct)
    {
        var group = await Group(gid, false, ct);
        if (group.MentorId is null) throw new BadRequestException("Group does not have an assigned mentor");
        var student = await repository.Student(email, ct);
        if (student is null || !await repository.IsMember(gid, student.Id, ct)) throw new ForbiddenException("Only group members can access mentor availability");
        return (await repository.Availability(group.MentorId.Value, Now, ct)).Select(s => new MentorAvailabilitySlotResponse(s.Id,
            s.MentorId, s.Mentor.MentorCode, s.Mentor.FullName, s.StartAt, s.EndAt, s.MeetLink, s.Note, s.Status, s.CreatedAt, s.UpdatedAt)).ToList();
    }

    public async Task<MeetingResponse> Create(long gid, CreateOrBookMeetingRequest r, string email, CancellationToken ct)
    {
        await using var tx = await repository.Begin(ct);
        var group = await Group(gid, true, ct);
        var meeting = new MentorMeeting { GroupId = gid, Group = group, Status = "SCHEDULED", CreatedAt = Now, UpdatedAt = Now };
        var recipients = (await repository.MemberAccounts(gid, ct)).ToHashSet();
        if (r.SlotId is not null)
        {
            if (group.MentorId is null) throw new BadRequestException("Group does not have an assigned mentor");
            var student = await repository.Student(email, ct);
            if (student is null || group.LeaderStudentId != student.Id) throw new ForbiddenException("Only the group leader can book a meeting");
            StudentTermWriteGuard.RequireWritable(group);
            await Quota(gid, ct);
            var slot = await repository.Slot(r.SlotId.Value, ct) ?? throw new NotFoundException("Availability slot not found");
            if (slot.StartAt <= Now) throw new BadRequestException("Cannot book a slot in the past");
            if (slot.MentorId != group.MentorId) throw new BadRequestException("Slot does not belong to the group's assigned mentor");
            if (slot.Status != "AVAILABLE") throw new BadRequestException("Slot is not available for booking");
            Times(slot.StartAt, slot.EndAt, "Availability slot");
            meeting.MeetLink = Link(slot.MeetLink); meeting.StartAt = slot.StartAt; meeting.EndAt = slot.EndAt;
            meeting.Note = slot.Note; meeting.MentorId = group.MentorId.Value; meeting.SlotId = slot.Id;
            meeting.BookedByStudentId = student.Id; slot.Status = "BOOKED"; slot.UpdatedAt = Now;
            recipients.Add(group.Mentor!.AccountId); recipients.Remove(student.AccountId);
        }
        else
        {
            var mentor = await AssignedMentor(group, email, ct);
            await ValidateTimes(r.StartAt, r.EndAt, mentor.Id, null, ct);
            meeting.MeetLink = Link(r.MeetLink); await Quota(gid, ct);
            meeting.MentorId = mentor.Id; meeting.StartAt = r.StartAt!.Value; meeting.EndAt = r.EndAt!.Value; meeting.Note = Optional(r.Note);
        }
        repository.Add(meeting); await repository.Save(ct);
        await Notify(meeting, recipients, "MENTOR_MEETING_BOOKED", r.SlotId is null ? "Mentor Meeting Scheduled" : "Meeting Booked",
            r.SlotId is null ? $"A mentor meeting was scheduled for group {group.Name}." : $"Group {group.Name} has booked a meeting.",
            (r.SlotId is null ? "MENTOR_MEETING_CREATED:" : "MENTOR_MEETING_BOOKED:") + meeting.Id, ct, r.SlotId);
        await repository.Save(ct);
        var result = Map((await repository.Meeting(meeting.Id, ct))!); await tx.CommitAsync(ct); return result;
    }

    public async Task<MeetingResponse> Update(long gid, long id, UpdateMeetingRequest r, string email, CancellationToken ct)
    {
        await using var tx = await repository.Begin(ct);
        var meeting = await LockedMeeting(gid, id, ct);
        await AssignedMentor(meeting.Group, email, ct);
        if (meeting.Status != "SCHEDULED" || meeting.EvidenceImageUrl is not null) throw new ConflictException("Completed or canceled meetings cannot be changed");
        var start = r.StartAt ?? meeting.StartAt; var end = r.EndAt ?? meeting.EndAt;
        await ValidateTimes(start, end, meeting.MentorId, id, ct);
        meeting.MeetLink = Link(r.MeetLink ?? meeting.MeetLink); meeting.StartAt = start; meeting.EndAt = end;
        if (r.Note is not null) meeting.Note = Optional(r.Note);
        meeting.UpdatedAt = Now; await repository.Save(ct); await repository.Reload(meeting, ct);
        await Notify(meeting, await repository.MemberAccounts(gid, ct), "MENTOR_MEETING_CONFIRMED", "Mentor Meeting Updated",
            $"The mentor meeting for group {meeting.Group.Name} was updated.", $"MENTOR_MEETING_UPDATED:{id}:{InstantKey(meeting.UpdatedAt)}", ct);
        await repository.Save(ct); await tx.CommitAsync(ct); return Map(meeting);
    }

    public async Task<MeetingResponse> Evidence(long gid, long id, string url, string email, CancellationToken ct)
    {
        await using var tx = await repository.Begin(ct);
        var meeting = await LockedMeeting(gid, id, ct); StudentTermWriteGuard.RequireWritable(meeting.Group);
        if (meeting.Status != "SCHEDULED") throw new ConflictException("Evidence can only be submitted for a scheduled meeting");
        if (meeting.EvidenceImageUrl is not null) throw new ConflictException("Meeting evidence has already been submitted");
        if (meeting.EndAt > Now) throw new BadRequestException("Meeting evidence can only be submitted after the meeting ends");
        var student = await repository.Student(email, ct);
        if (student is null || !await repository.IsMember(gid, student.Id, ct) || student.Status != "ACTIVE" || student.Account.Status != "ACTIVE")
            throw new ForbiddenException("Only an active group member can submit evidence");
        meeting.EvidenceImageUrl = url.Trim(); meeting.EvidenceSubmittedByStudentId = student.Id; meeting.EvidenceSubmittedByStudent = student;
        meeting.EvidenceSubmittedAt = Now; meeting.CompletedAt = Now; meeting.Status = "COMPLETED"; meeting.UpdatedAt = Now;
        var recipients = new List<long> { meeting.Mentor.AccountId };
        if (meeting.Group.Instructor is not null) recipients.Add(meeting.Group.Instructor.AccountId);
        await Notify(meeting, recipients, "MENTOR_MEETING_COMPLETED", "Meeting Evidence Submitted",
            $"Evidence was submitted for group {meeting.Group.Name}.", "MENTOR_MEETING_EVIDENCE:" + id, ct);
        await repository.Save(ct); await tx.CommitAsync(ct); return Map(meeting);
    }

    public async Task<MeetingResponse> Cancel(long gid, long id, string reason, string email, CancellationToken ct)
    {
        await using var tx = await repository.Begin(ct);
        var meeting = await LockedMeeting(gid, id, ct);
        // Java returns terminal-state no-ops before resolving the actor.
        if (meeting.Status == "CANCELED") { await tx.CommitAsync(ct); return Map(meeting); }
        if (meeting.Status == "COMPLETED") throw new BadRequestException("Completed meetings cannot be canceled");
        if (meeting.EvidenceImageUrl is not null) throw new ConflictException("Meetings with evidence cannot be canceled");
        var actor = await Account(email, ct);
        if (actor.Role != "MENTOR") throw new ForbiddenException("Only the assigned mentor can cancel this meeting");
        var mentor = await repository.Mentor(email, false, ct) ?? throw new ForbiddenException("Mentor profile not found");
        if (meeting.Group.MentorId != mentor.Id) throw new ForbiddenException("Only the assigned mentor can cancel this meeting");
        meeting.Status = "CANCELED"; meeting.CanceledAt = Now; meeting.CancelReason = reason.Trim(); meeting.UpdatedAt = Now;
        if (meeting.Slot is not null) { meeting.Slot.Status = "CANCELED"; meeting.Slot.UpdatedAt = Now; }
        var recipients = (await repository.MemberAccounts(gid, ct)).ToHashSet();
        if (meeting.Group.Mentor is not null) recipients.Add(meeting.Group.Mentor.AccountId);
        recipients.Remove(actor.Id);
        await Notify(meeting, recipients, "MENTOR_MEETING_CANCELED", "Meeting Canceled", $"Meeting for group {meeting.Group.Name} has been canceled.", "MENTOR_MEETING_CANCELED:" + id, ct);
        await repository.Save(ct); await tx.CommitAsync(ct); return Map(meeting);
    }

    public async Task<MeetingResponse> Confirm(long gid, long id, string email, CancellationToken ct)
    {
        await using var tx = await repository.Begin(ct);
        var meeting = await LockedMeeting(gid, id, ct);
        if (meeting.Status == "CANCELED") throw new BadRequestException("Canceled meetings cannot be confirmed");
        if (meeting.Status == "COMPLETED") { await tx.CommitAsync(ct); return Map(meeting); }
        if (meeting.Slot is null || meeting.Slot.StartAt > Now) throw new BadRequestException("Meeting can only be confirmed after it starts");
        var actor = await Account(email, ct); var recipients = new HashSet<long>(); var changed = false;
        const string denied = "Only group leader or assigned mentor can confirm this meeting";
        if (actor.Role == "STUDENT")
        {
            StudentTermWriteGuard.RequireWritable(meeting.Group);
            var student = await repository.Student(email, ct) ?? throw new ForbiddenException("Student profile not found");
            if (meeting.Group.LeaderStudentId != student.Id) throw new ForbiddenException(denied);
            if (meeting.LeaderConfirmedAt is null) { meeting.LeaderConfirmedAt = Now; meeting.LeaderConfirmedByStudentId = student.Id; changed = true; }
            recipients.Add(meeting.Mentor.AccountId);
        }
        else if (actor.Role == "MENTOR")
        {
            var mentor = await repository.Mentor(email, false, ct) ?? throw new ForbiddenException("Mentor profile not found");
            if (meeting.Group.MentorId != mentor.Id) throw new ForbiddenException(denied);
            if (meeting.MentorConfirmedAt is null) { meeting.MentorConfirmedAt = Now; changed = true; }
            if (meeting.Group.LeaderStudent is not null) recipients.Add(meeting.Group.LeaderStudent.AccountId);
        }
        else throw new ForbiddenException(denied);
        if (changed) meeting.UpdatedAt = Now;
        await repository.Save(ct); await repository.Reload(meeting, ct); recipients.Remove(actor.Id);
        await Notify(meeting, recipients, "MENTOR_MEETING_CONFIRMED", "Meeting Confirmation Updated",
            $"A participant confirmed the meeting for group {meeting.Group.Name}. Evidence is still required to complete it.",
            $"MENTOR_MEETING_CONFIRMED:{id}:{actor.Id}:{InstantKey(meeting.UpdatedAt)}", ct);
        await repository.Save(ct); await tx.CommitAsync(ct); return Map(meeting);
    }

    public async Task<IReadOnlyList<MeetingResponse>> List(long gid, string email, CancellationToken ct)
    { var group = await Group(gid, false, ct); await CanView(group, email, ct); return (await repository.List(gid, ct)).Select(Map).ToList(); }
    public async Task<MeetingResponse> Get(long gid, long id, string email, CancellationToken ct)
    { var meeting = await Meeting(gid, id, ct); await CanView(meeting.Group, email, ct); return Map(meeting); }

    private async Task<StudentGroup> Group(long id, bool locked, CancellationToken ct) => await repository.Group(id, locked, ct) ?? throw new NotFoundException("Group not found");
    private async Task<Account> Account(string email, CancellationToken ct) => await repository.Account(email, ct) ?? throw new UnauthorizedException("User account not found");
    private async Task<MentorMeeting> Meeting(long gid, long id, CancellationToken ct)
    {
        var meeting = await repository.Meeting(id, ct) ?? throw new NotFoundException("Meeting not found with id: " + id);
        if (meeting.GroupId != gid) throw new NotFoundException("Meeting not found in this group");
        return meeting;
    }
    private async Task<MentorMeeting> LockedMeeting(long gid, long id, CancellationToken ct)
    { var meeting = await Meeting(gid, id, ct); await Group(gid, true, ct); await repository.Reload(meeting, ct); return meeting; }
    private async Task<Mentor> AssignedMentor(StudentGroup group, string email, CancellationToken ct)
    {
        var mentor = await repository.Mentor(email, true, ct);
        if (mentor is null || group.MentorId != mentor.Id) throw new ForbiddenException("Only the assigned mentor can manage meetings");
        return mentor;
    }
    private async Task CanView(StudentGroup group, string email, CancellationToken ct)
    {
        var actor = await Account(email, ct);
        if (actor.Role == "STUDENT" && await repository.Student(email, ct) is { } s && await repository.IsMember(group.Id, s.Id, ct)) return;
        if (actor.Role == "MENTOR" && await repository.Mentor(email, false, ct) is { } m && group.MentorId == m.Id) return;
        if (actor.Role == "INSTRUCTOR" && await repository.Instructor(email, ct) is { } i && group.InstructorId == i.Id) return;
        throw new ForbiddenException("Access denied to group meetings");
    }
    private async Task Quota(long group, CancellationToken ct)
    { if (await repository.ActiveCount(group, ct) >= 2) throw new ConflictException("A group can have at most 2 non-canceled mentor meetings"); }
    private async Task ValidateTimes(DateTime? start, DateTime? end, long mentor, long? exclude, CancellationToken ct)
    {
        Times(start, end, "Meeting");
        if (await repository.Overlap(mentor, start!.Value, end!.Value, exclude, ct)) throw new ConflictException("This meeting overlaps another non-canceled mentor meeting");
    }
    private static void Times(DateTime? start, DateTime? end, string subject)
    {
        if (start is null || end is null) throw new BadRequestException(subject + " start time and end time are required");
        var local = TimeZoneInfo.ConvertTimeFromUtc(start.Value.ToUniversalTime(), BusinessZone);
        if ((local.Minute != 0 && local.Minute != 30) || local.Ticks % TimeSpan.TicksPerMinute != 0)
            throw new BadRequestException(subject + " must start on the hour or half hour with no seconds");
        if (end.Value - start.Value != TimeSpan.FromHours(1)) throw new BadRequestException(subject + " must last exactly 60 minutes");
    }
    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string Link(string? value)
    {
        var link = Optional(value) ?? throw new BadRequestException("Meet link is required");
        if (link.Length > 500 || !(link.StartsWith("http://", StringComparison.Ordinal) || link.StartsWith("https://", StringComparison.Ordinal)))
            throw new BadRequestException("Meet link must use HTTP or HTTPS and contain at most 500 characters");
        return link;
    }
    private static string InstantKey(DateTime value)
    {
        value = value.ToUniversalTime();
        var fraction = value.Ticks % TimeSpan.TicksPerSecond;
        var format = fraction == 0 ? "yyyy-MM-dd'T'HH:mm:ss'Z'" : fraction % TimeSpan.TicksPerMillisecond == 0
            ? "yyyy-MM-dd'T'HH:mm:ss.fff'Z'" : "yyyy-MM-dd'T'HH:mm:ss.ffffff'Z'";
        return value.ToString(format, CultureInfo.InvariantCulture);
    }
    private async Task Notify(MentorMeeting meeting, IEnumerable<long> recipients, string type, string title, string body, string key, CancellationToken ct, long? slot = null)
    {
        var id = meeting.Id.ToString(CultureInfo.InvariantCulture);
        var args = new Dictionary<string, string> { ["groupId"] = meeting.GroupId.ToString(CultureInfo.InvariantCulture), ["meetingId"] = id };
        if (slot is not null) args["slotId"] = slot.Value.ToString(CultureInfo.InvariantCulture);
        foreach (var recipient in recipients.Distinct()) await repository.Notify(new Notification { RecipientId = recipient, Type = type,
            Title = title, Body = body, ActionKey = "OPEN_MEETING", ActionParams = JsonSerializer.Serialize(args), EntityType = "MentorMeeting",
            EntityId = id, EventKey = key, CreatedAt = Now, UpdatedAt = Now }, ct);
    }
    private static MeetingResponse Map(MentorMeeting e) => new(e.Id,e.SlotId,e.GroupId,e.Group.Name,e.Group.GroupNo,e.Group.ProjectName,
        e.Group.SelectedProblemId,e.Group.SelectedProblem?.Title,e.MentorId,e.Mentor.MentorCode,e.Mentor.FullName,e.BookedByStudentId,
        e.BookedByStudent?.StudentCode,e.BookedByStudent?.FullName,e.StartAt,e.EndAt,e.MeetLink,e.Note,e.Status,e.LeaderConfirmedByStudentId,
        e.LeaderConfirmedAt,e.MentorConfirmedAt,e.CompletedAt,e.CanceledAt,e.CancelReason,e.EvidenceImageUrl,e.EvidenceSubmittedByStudentId,
        e.EvidenceSubmittedByStudent?.FullName,e.EvidenceSubmittedAt,e.CreatedAt,e.UpdatedAt);
}
