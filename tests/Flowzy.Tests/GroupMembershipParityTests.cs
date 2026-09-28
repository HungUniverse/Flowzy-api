using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Flowzy.Repository.Data;
using Flowzy.Repository.Entities;
using Flowzy.Service.Authentication;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Flowzy.Tests;

/// <summary>Fixtures follow the revised Java sections 13/14, using real V1–V33 constraints.</summary>
public sealed class GroupMembershipParityTests(FlowzyApiFactory factory) : IClassFixture<FlowzyApiFactory>
{
    private sealed record Fixture(long Group, long Second, long OtherCourse, long Leader, long Target,
        long Outsider, string TargetCode, string TargetEmail, string Term,
        HttpClient LeaderClient, HttpClient TargetClient, HttpClient OutsiderClient, HttpClient Admin, HttpClient Mentor, HttpClient Instructor) : IDisposable
    {
        public void Dispose()
        { LeaderClient.Dispose(); TargetClient.Dispose(); OutsiderClient.Dispose(); Admin.Dispose(); Mentor.Dispose(); Instructor.Dispose(); }
    }

    private async Task<Fixture> Seed(bool closed = false)
    {
        using var bootstrap = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FlowzyDbContext>();
        var suffix = Guid.NewGuid().ToString("N");
        Account Account(string role, string key) => new() { Email = $"{key}.{suffix}@local.test", PasswordHash = "unused", Role = role, Status = "ACTIVE", MustChangePassword = false };
        var students = Enumerable.Range(0, 3).Select(i => new Student { Account = Account("STUDENT", $"student{i}"), StudentCode = $"S{i}-{suffix}", FullName = $"Member {i}", Status = "ACTIVE" }).ToArray();
        var admin = Account("ADMIN", "admin");
        var mentor = new Mentor { Account = Account("MENTOR", "mentor"), MentorCode = "M" + suffix, FullName = "Assigned Mentor", Status = "ACTIVE" };
        var instructor = new Instructor { Account = Account("INSTRUCTOR", "instructor"), InstructorCode = "I" + suffix, FullName = "Instructor", Status = "ACTIVE" };
        var term = closed ? new AcademicTerm { Code = "C" + suffix[..20], Status = "CLOSED" } : await db.AcademicTerms.FirstOrDefaultAsync(x => x.Status == "OPEN") ?? new AcademicTerm { Code = "SU26", Status = "OPEN" };
        if (term.Id == 0) db.AcademicTerms.Add(term);
        db.Students.AddRange(students); db.Accounts.Add(admin); db.Mentors.Add(mentor); db.Instructors.Add(instructor);
        await db.SaveChangesAsync();
        var course = "C" + suffix[..20];
        var groups = Enumerable.Range(0, 3).Select(i => new StudentGroup { Term = term.Code, CourseCode = i == 2 ? course + "X" : course,
            GroupNo = (i + 1).ToString(), Name = $"membership-{suffix}-{i}", Status = "ACTIVE", MentorId = mentor.Id }).ToArray();
        db.StudentGroups.AddRange(groups); await db.SaveChangesAsync();
        // A different leader for each same-course group satisfies membership uniqueness.
        for (var i = 0; i < groups.Length; i++)
        {
            var leader = i == 1 ? students[2] : students[0];
            db.StudentGroupMembers.Add(new StudentGroupMember { GroupId = groups[i].Id, StudentId = leader.Id, MemberRole = "LEADER" });
            await db.SaveChangesAsync(); groups[i].LeaderStudentId = leader.Id;
        }
        await db.SaveChangesAsync();
        var jwt = scope.ServiceProvider.GetRequiredService<IJwtService>();
        HttpClient Client(Account account) { var client = factory.CreateClient(); client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt.GenerateAccessToken(account)); return client; }
        return new(groups[0].Id, groups[1].Id, groups[2].Id, students[0].Id, students[1].Id, students[2].Id,
            students[1].StudentCode, students[1].Account.Email, term.Code, Client(students[0].Account), Client(students[1].Account), Client(students[2].Account), Client(admin), Client(mentor.Account), Client(instructor.Account));
    }

    private async Task WithDb(Func<FlowzyDbContext, Task> action)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        await action(scope.ServiceProvider.GetRequiredService<FlowzyDbContext>());
    }
    private async Task<long> Invitation(Fixture f, long group, long? student = null, int hoursAgo = 0)
    {
        long id = 0;
        await WithDb(async db => {
            var g = await db.StudentGroups.SingleAsync(x => x.Id == group);
            var e = new GroupInvitation { GroupId = group, InviterStudentId = g.LeaderStudentId!.Value, InviteeStudentId = student ?? f.Target, Status = "PENDING", CreatedAt = DateTime.UtcNow.AddHours(-hoursAgo) };
            db.GroupInvitations.Add(e); await db.SaveChangesAsync(); id = e.Id;
        }); return id;
    }
    private async Task<long> Request(Fixture f, long group, long? student = null, int hoursAgo = 0)
    {
        long id = 0;
        await WithDb(async db => {
            var now = DateTime.UtcNow.AddHours(-hoursAgo);
            var e = new GroupJoinRequest { GroupId = group, StudentId = student ?? f.Target, Status = "PENDING", CreatedAt = now, UpdatedAt = now };
            db.GroupJoinRequests.Add(e); await db.SaveChangesAsync(); id = e.Id;
        }); return id;
    }
    private async Task Fill(long group, int count)
    {
        await WithDb(async db => {
            var existing = await db.StudentGroupMembers.CountAsync(x => x.GroupId == group);
            for (var i = existing; i < count; i++)
            {
                var key = Guid.NewGuid().ToString("N");
                var student = new Student { StudentCode = key, FullName = "Capacity member", Status = "ACTIVE", Account = new Account { Email = key + "@local.test", PasswordHash = "x", Role = "STUDENT", Status = "ACTIVE" } };
                db.StudentGroupMembers.Add(new StudentGroupMember { GroupId = group, Student = student, MemberRole = "MEMBER" });
            }
            await db.SaveChangesAsync();
        });
    }
    private static async Task<JsonElement> Data(HttpResponseMessage response, HttpStatusCode expected = HttpStatusCode.OK)
    {
        using (response)
        {
            var body = await response.Content.ReadAsStringAsync();
            response.StatusCode.Should().Be(expected, body);
            using var json = JsonDocument.Parse(body);
            return json.RootElement.TryGetProperty("data", out var data) ? data.Clone() : json.RootElement.Clone();
        }
    }
    private static async Task Error(HttpResponseMessage response, HttpStatusCode status, string message)
    {
        using (response) { response.StatusCode.Should().Be(status, await response.Content.ReadAsStringAsync()); (await response.Content.ReadAsStringAsync()).Should().Contain(message); }
    }

    [Fact]
    public async Task AdminInvitesUsingLeaderAndOnlyLeaderOrAdminCanListInvitations()
    {
        using var f = await Seed();
        var result = await Data(await f.Admin.PostAsJsonAsync($"/api/groups/{f.Group}/invitations", new { studentCodeOrEmail = f.TargetEmail.ToUpperInvariant(), message = "  keep whitespace  " }));
        result.GetProperty("inviterId").GetInt64().Should().Be(f.Leader);
        result.GetProperty("message").GetString().Should().Be("  keep whitespace  ");
        var id = result.GetProperty("id").GetInt64();
        (await Data(await f.Admin.GetAsync($"/api/groups/{f.Group}/invitations"))).GetArrayLength().Should().Be(1);
        await WithDb(async db => { db.StudentGroupMembers.Add(new StudentGroupMember { GroupId = f.Group, StudentId = f.Target, MemberRole = "MEMBER" }); await db.SaveChangesAsync(); });
        await Error(await f.TargetClient.GetAsync($"/api/groups/{f.Group}/invitations"), HttpStatusCode.Forbidden, "Only group leader or admin can view group invitations");
        await WithDb(async db => {
            var notification = await db.Notifications.SingleAsync(x => x.EventKey == "GROUP_INVITATION_CREATED:" + id);
            notification.RecipientId.Should().Be((await db.Students.FindAsync(f.Target))!.AccountId);
            notification.ActionKey.Should().Be("OPEN_GROUP_INVITATIONS");
            JsonDocument.Parse(notification.ActionParams!).RootElement.GetProperty("invitationId").GetString().Should().Be(id.ToString());
        });
    }

    [Fact]
    public async Task AcceptInvitationPersistsAcceptedAndCleansOnlySameTermCourse()
    {
        using var f = await Seed();
        var invitation = await Invitation(f, f.Group);
        var sameInvitation = await Invitation(f, f.Second); var otherInvitation = await Invitation(f, f.OtherCourse);
        var sameRequest = await Request(f, f.Second); var otherRequest = await Request(f, f.OtherCourse);
        await Data(await f.TargetClient.PostAsync($"/api/groups/invitations/{invitation}/accept", null));
        await WithDb(async db => {
            (await db.GroupInvitations.FindAsync(invitation))!.Status.Should().Be("ACCEPTED");
            (await db.GroupInvitations.FindAsync(sameInvitation))!.Status.Should().Be("DECLINED");
            (await db.GroupInvitations.FindAsync(otherInvitation))!.Status.Should().Be("PENDING");
            (await db.GroupJoinRequests.FindAsync(sameRequest))!.Status.Should().Be("CANCELED");
            (await db.GroupJoinRequests.FindAsync(otherRequest))!.Status.Should().Be("PENDING");
            (await db.StudentGroupMembers.CountAsync(x => x.StudentId == f.Target && x.GroupId == f.Group)).Should().Be(1);
            (await db.Notifications.CountAsync(x => x.EventKey == "GROUP_INVITATION_ACCEPTED:" + invitation)).Should().Be(1);
        });
        await Error(await f.TargetClient.PostAsync($"/api/groups/invitations/{invitation}/accept", null), HttpStatusCode.BadRequest, "Invitation is not pending");
    }

    [Theory]
    [InlineData("accept")]
    [InlineData("decline")]
    [InlineData("cancel")]
    public async Task ExpiredInvitationsAreHiddenFromMineAndCannotBeRespondedTo(string action)
    {
        using var f = await Seed(); var id = await Invitation(f, f.Group, hoursAgo: 73);
        (await Data(await f.TargetClient.GetAsync("/api/groups/invitations/me"))).GetArrayLength().Should().Be(0);
        (await Data(await f.LeaderClient.GetAsync($"/api/groups/{f.Group}/invitations"))).GetArrayLength().Should().Be(1);
        await Error(await (action == "cancel" ? f.Admin : f.TargetClient).PostAsync($"/api/groups/invitations/{id}/{action}", null), HttpStatusCode.BadRequest, "Invitation has expired");
    }

    [Fact]
    public async Task CancelAndDeclineDoNotRequireUnlockedMembership()
    {
        using var f = await Seed(); var invitation = await Invitation(f, f.Group);
        await WithDb(async db => { (await db.StudentGroups.FindAsync(f.Group))!.IsLocked = true; await db.SaveChangesAsync(); });
        await Data(await f.TargetClient.PostAsync($"/api/groups/invitations/{invitation}/decline", null));
        var second = await Invitation(f, f.Group);
        await Data(await f.LeaderClient.PostAsync($"/api/groups/invitations/{second}/cancel", null));
        await WithDb(async db => { (await db.GroupInvitations.FindAsync(second))!.Status.Should().Be("CANCELED"); });
    }

    [Fact]
    public async Task JoinRequestGuardsPendingInvitationExpiryAndMessageValidation()
    {
        using var f = await Seed(); var route = $"/api/groups/{f.Group}/join-requests";
        var expired = await Request(f, f.Group, hoursAgo: 73);
        var replacement = await Data(await f.TargetClient.PostAsJsonAsync(route, new { message = " raw " }));
        replacement.GetProperty("message").GetString().Should().Be(" raw ");
        await WithDb(async db => { (await db.GroupJoinRequests.FindAsync(expired))!.Status.Should().Be("CANCELED"); });
        await Error(await f.TargetClient.PostAsJsonAsync(route, new { }), HttpStatusCode.BadRequest, "You have already submitted a pending join request for this group");
        await Error(await f.TargetClient.PostAsJsonAsync(route, new { message = new string('x', 501) }), HttpStatusCode.BadRequest, "Message must be at most 500 characters");
        await Invitation(f, f.Second);
        await Error(await f.TargetClient.PostAsJsonAsync($"/api/groups/{f.Second}/join-requests", new { }), HttpStatusCode.BadRequest, "You have a pending invitation for this group");
    }

    [Fact]
    public async Task AssignedMentorAndAdminReadRequestsButInstructorAndNonMemberCannot()
    {
        using var f = await Seed(); await Request(f, f.Group);
        var route = $"/api/groups/{f.Group}/join-requests";
        (await Data(await f.Mentor.GetAsync(route))).GetArrayLength().Should().Be(1);
        (await Data(await f.Admin.GetAsync(route))).GetArrayLength().Should().Be(1);
        await Error(await f.Instructor.GetAsync(route), HttpStatusCode.Forbidden, "Unauthorized role to view join requests");
        await Error(await f.OutsiderClient.GetAsync(route), HttpStatusCode.Forbidden, "Only group members can view join requests");
        await Error(await f.Mentor.PostAsJsonAsync(route, new { }), HttpStatusCode.Forbidden, "Only students can submit join requests");
        using var anonymous = factory.CreateClient(); (await anonymous.GetAsync(route)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        (await f.Admin.GetAsync("/api/groups/join-requests/me")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminApprovesClosedTermAndCleanupLeavesOtherCourseAlone()
    {
        using var f = await Seed(closed: true); var request = await Request(f, f.Group);
        var same = await Request(f, f.Second); var other = await Request(f, f.OtherCourse);
        var invitation = await Invitation(f, f.Second);
        await Error(await f.LeaderClient.PostAsync($"/api/groups/join-requests/{request}/approve", null), HttpStatusCode.Conflict, "read-only for students");
        await Data(await f.Admin.PostAsync($"/api/groups/{f.Group}/join-requests/{request}/approve", null));
        await WithDb(async db => {
            var approved = (await db.GroupJoinRequests.FindAsync(request))!;
            approved.Status.Should().Be("ACCEPTED"); approved.RespondedByStudentId.Should().BeNull();
            (await db.GroupJoinRequests.FindAsync(same))!.Status.Should().Be("CANCELED");
            (await db.GroupJoinRequests.FindAsync(other))!.Status.Should().Be("PENDING");
            (await db.GroupInvitations.FindAsync(invitation))!.Status.Should().Be("DECLINED");
            (await db.Notifications.CountAsync(x => x.EventKey == "GROUP_JOIN_REQUEST_APPROVED:" + request)).Should().Be(1);
        });
    }

    [Theory]
    [InlineData("approve")]
    [InlineData("accept")]
    public async Task FullGroupRejectsMembershipAndRollsBackRequestAndNotification(string action)
    {
        using var f = await Seed(); await Fill(f.Group, 6);
        var id = action == "approve" ? await Request(f, f.Group) : await Invitation(f, f.Group);
        var route = action == "approve" ? $"/api/groups/join-requests/{id}/approve" : $"/api/groups/invitations/{id}/accept";
        await Error(await (action == "approve" ? f.Admin : f.TargetClient).PostAsync(route, null), HttpStatusCode.BadRequest, "Group is already full");
        await WithDb(async db => {
            (await db.StudentGroupMembers.CountAsync(x => x.GroupId == f.Group)).Should().Be(6);
            if (action == "approve") (await db.GroupJoinRequests.FindAsync(id))!.Status.Should().Be("PENDING");
            else (await db.GroupInvitations.FindAsync(id))!.Status.Should().Be("PENDING");
        });
    }

    [Fact]
    public async Task LockedGroupBlocksAdminApprovalButNotRejectionOrRequesterCancellation()
    {
        using var f = await Seed(); var id = await Request(f, f.Group);
        await WithDb(async db => { (await db.StudentGroups.FindAsync(f.Group))!.IsLocked = true; await db.SaveChangesAsync(); });
        await Error(await f.Admin.PostAsync($"/api/groups/join-requests/{id}/approve", null), HttpStatusCode.Conflict, "Group membership is locked. Unlock the group before changing members");
        await Data(await f.Admin.PostAsync($"/api/groups/join-requests/{id}/reject", null));
        var next = await Request(f, f.Group, hoursAgo: 73);
        await Data(await f.TargetClient.PostAsync($"/api/groups/{f.Group}/join-requests/{next}/cancel", null));
        await WithDb(async db => { (await db.GroupJoinRequests.FindAsync(next))!.Status.Should().Be("CANCELED"); });
    }

    [Fact]
    public async Task ApproveExpiredRequestFailsButRejectExpiredRequestSucceeds()
    {
        using var f = await Seed(); var id = await Request(f, f.Group, hoursAgo: 73);
        await Error(await f.Admin.PostAsync($"/api/groups/join-requests/{id}/approve", null), HttpStatusCode.BadRequest, "Join request has expired");
        await Data(await f.LeaderClient.PostAsync($"/api/groups/{f.Group}/join-requests/{id}/reject", null));
        await WithDb(async db => { var request = (await db.GroupJoinRequests.FindAsync(id))!; request.Status.Should().Be("REJECTED"); request.RespondedByStudentId.Should().Be(f.Leader); });
    }

    [Fact]
    public async Task ConcurrentAcceptAndApproveForSameStudentCreateOnlyOneMembership()
    {
        using var f = await Seed(); var invitation = await Invitation(f, f.Group); var request = await Request(f, f.Second);
        var responses = await Task.WhenAll(f.TargetClient.PostAsync($"/api/groups/invitations/{invitation}/accept", null), f.Admin.PostAsync($"/api/groups/join-requests/{request}/approve", null));
        responses.Select(x => x.StatusCode).Should().BeEquivalentTo([HttpStatusCode.OK, HttpStatusCode.BadRequest]);
        foreach (var response in responses) response.Dispose();
        await WithDb(async db => { (await db.StudentGroupMembers.CountAsync(x => x.StudentId == f.Target && (x.GroupId == f.Group || x.GroupId == f.Second))).Should().Be(1); });
    }

    [Fact]
    public async Task ConcurrentApprovalsForLastPlaceDoNotExceedSixMembers()
    {
        using var f = await Seed(); await Fill(f.Group, 5);
        // Outsider already belongs to Second in this scope; use a fresh ungrouped student instead.
        long secondStudent = 0;
        await WithDb(async db => { var key = Guid.NewGuid().ToString("N"); var student = new Student { StudentCode = key, FullName = "Competitor", Status = "ACTIVE", Account = new Account { Email = key + "@local.test", PasswordHash = "x", Role = "STUDENT", Status = "ACTIVE" } }; db.Students.Add(student); await db.SaveChangesAsync(); secondStudent = student.Id; });
        var first = await Request(f, f.Group); var second = await Request(f, f.Group, secondStudent);
        var responses = await Task.WhenAll(f.Admin.PostAsync($"/api/groups/join-requests/{first}/approve", null), f.Admin.PostAsync($"/api/groups/join-requests/{second}/approve", null));
        responses.Select(x => x.StatusCode).Should().BeEquivalentTo([HttpStatusCode.OK, HttpStatusCode.BadRequest]);
        foreach (var response in responses) response.Dispose();
        await WithDb(async db => { (await db.StudentGroupMembers.CountAsync(x => x.GroupId == f.Group)).Should().Be(6); });
    }
}
