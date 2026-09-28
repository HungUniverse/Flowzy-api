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

public sealed class GroupMeetingParityTests(FlowzyApiFactory factory) : IClassFixture<FlowzyApiFactory>
{
    private sealed record Fixture(long Group, long SecondGroup, long MentorId, long MentorAccount, long InstructorAccount,
        long LeaderId, long LeaderAccount, long MemberId, long MemberAccount, string Term,
        HttpClient Leader, HttpClient Member, HttpClient Outsider, HttpClient Mentor, HttpClient Instructor, HttpClient Admin) : IDisposable
    { public void Dispose() { Leader.Dispose(); Member.Dispose(); Outsider.Dispose(); Mentor.Dispose(); Instructor.Dispose(); Admin.Dispose(); } }

    private async Task<Fixture> Seed()
    {
        using var bootstrap = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<FlowzyDbContext>();
        var key = Guid.NewGuid().ToString("N");
        Account Account(string role, int i) => new() { Email = $"{i}.{key}@local.test", Role = role, Status = "ACTIVE", PasswordHash = "unused" };
        var students = Enumerable.Range(0, 3).Select(i => new Student { StudentCode = i + key, FullName = "Student " + i, Status = "ACTIVE", Account = Account("STUDENT", i) }).ToArray();
        var mentor = new Mentor { MentorCode = key, FullName = "Mentor", Status = "ACTIVE", Account = Account("MENTOR", 3) };
        var instructor = new Instructor { InstructorCode = key, FullName = "Instructor", Status = "ACTIVE", Account = Account("INSTRUCTOR", 4) };
        var admin = Account("ADMIN", 5);
        var term = await db.AcademicTerms.FirstOrDefaultAsync(x => x.Status == "OPEN") ?? new AcademicTerm { Code = "M" + key[..12], Status = "OPEN" };
        if (term.Id == 0) db.AcademicTerms.Add(term);
        db.Students.AddRange(students); db.Mentors.Add(mentor); db.Instructors.Add(instructor); db.Accounts.Add(admin); await db.SaveChangesAsync();
        var groups = Enumerable.Range(0, 2).Select(i => new StudentGroup { Term = term.Code, CourseCode = "C" + key[..20] + i,
            GroupNo = "1", Name = key + i, Status = "ACTIVE", MentorId = mentor.Id, InstructorId = instructor.Id }).ToArray();
        db.StudentGroups.AddRange(groups); await db.SaveChangesAsync();
        foreach (var group in groups)
        {
            db.StudentGroupMembers.Add(new StudentGroupMember { GroupId = group.Id, StudentId = students[0].Id, MemberRole = "LEADER" });
            db.StudentGroupMembers.Add(new StudentGroupMember { GroupId = group.Id, StudentId = students[1].Id, MemberRole = "MEMBER" });
            await db.SaveChangesAsync(); group.LeaderStudentId = students[0].Id;
        }
        await db.SaveChangesAsync(); var jwt = scope.ServiceProvider.GetRequiredService<IJwtService>();
        HttpClient Client(Account a) { var c = factory.CreateClient(); c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt.GenerateAccessToken(a)); return c; }
        return new(groups[0].Id, groups[1].Id, mentor.Id, mentor.AccountId, instructor.AccountId, students[0].Id, students[0].AccountId, students[1].Id,
            students[1].AccountId, term.Code, Client(students[0].Account), Client(students[1].Account), Client(students[2].Account), Client(mentor.Account), Client(instructor.Account), Client(admin));
    }
    private async Task WithDb(Func<FlowzyDbContext, Task> action)
    { await using var scope = factory.Services.CreateAsyncScope(); await action(scope.ServiceProvider.GetRequiredService<FlowzyDbContext>()); }
    private static string Base(Fixture f) => $"/api/groups/{f.Group}/mentor";
    private static string Url(Fixture f, long id, string? action = null) => Base(f) + $"/meetings/{id}" + (action is null ? "" : "/" + action);
    private static DateTime Time(int days = 1) => DateTime.UtcNow.Date.AddDays(days).AddHours(10);
    private static object Direct(DateTime start, string? note = "  original note  ") => new { startAt = start, endAt = start.AddHours(1), meetLink = "https://example.test/meeting", note };
    private async Task<long> Slot(Fixture f, DateTime start, string status = "AVAILABLE")
    {
        long id = 0; await WithDb(async db => {
            var s = new MentorAvailabilitySlot { MentorId = f.MentorId, StartAt = start, EndAt = start.AddHours(1), MeetLink = "https://example.test/slot", Note = " slot note ", Status = status };
            db.MentorAvailabilitySlots.Add(s); await db.SaveChangesAsync(); id = s.Id;
        }); return id;
    }
    private async Task<long> Meeting(Fixture f, DateTime start, string status = "SCHEDULED", bool slot = false)
    {
        var slotId = slot ? await Slot(f, start, "BOOKED") : (long?)null;
        long id = 0; await WithDb(async db => {
            var m = new MentorMeeting { GroupId = f.Group, MentorId = f.MentorId, StartAt = start, EndAt = start.AddHours(1), SlotId = slotId,
                MeetLink = "https://example.test/meeting", Note = "original note", Status = status, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            db.MentorMeetings.Add(m); await db.SaveChangesAsync(); id = m.Id;
        }); return id;
    }
    private static async Task<JsonElement> Body(HttpResponseMessage response, HttpStatusCode status = HttpStatusCode.OK)
    {
        var raw = await response.Content.ReadAsStringAsync(); response.StatusCode.Should().Be(status, raw);
        using var doc = JsonDocument.Parse(raw); return doc.RootElement.Clone();
    }
    private static async Task Error(HttpResponseMessage response, HttpStatusCode status, string message) =>
        (await Body(response, status)).GetProperty("message").GetString().Should().Be(message);

    [Fact]
    public async Task ReadsAllowAssignedInstructorButAvailabilityIsMembersOnlyAndSorted()
    {
        using var f = await Seed(); var id = await Meeting(f, Time(-2)); var first = await Slot(f, Time(1)); var second = await Slot(f, Time(2));
        await Slot(f, Time(-1)); await Slot(f, Time(3), "CANCELED");
        foreach (var client in new[] { f.Member, f.Mentor, f.Instructor })
        { await Body(await client.GetAsync(Url(f, id))); await Body(await client.GetAsync(Base(f) + "/meetings")); }
        foreach (var client in new[] { f.Admin, f.Outsider }) await Error(await client.GetAsync(Url(f, id)), HttpStatusCode.Forbidden, "Access denied to group meetings");
        foreach (var client in new[] { f.Mentor, f.Instructor, f.Outsider }) await Error(await client.GetAsync(Base(f) + "/availability"), HttpStatusCode.Forbidden, "Only group members can access mentor availability");
        var slots = (await Body(await f.Member.GetAsync(Base(f) + "/availability"))).GetProperty("data");
        slots.EnumerateArray().Select(x => x.GetProperty("id").GetInt64()).Should().Equal(first, second);
        await Error(await f.Member.GetAsync(Url(f, long.MaxValue)), HttpStatusCode.NotFound, "Meeting not found with id: " + long.MaxValue);
        await Error(await f.Member.GetAsync($"/api/groups/{f.SecondGroup}/mentor/meetings/{id}"), HttpStatusCode.NotFound, "Meeting not found in this group");
        using var anonymous = factory.CreateClient(); await Body(await anonymous.GetAsync(Url(f, id)), HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DirectPastMeetingAndUpdateKeepOmittedNoteAndNotifyMembers()
    {
        using var f = await Seed();
        var created = (await Body(await f.Mentor.PostAsJsonAsync(Base(f) + "/meetings", Direct(Time(-3))))).GetProperty("data"); var id = created.GetProperty("id").GetInt64();
        created.GetProperty("note").GetString().Should().Be("original note"); created.GetProperty("slotId").ValueKind.Should().Be(JsonValueKind.Null);
        var updated = (await Body(await f.Mentor.PatchAsJsonAsync(Url(f, id), new { meetLink = "https://example.test/updated", startAt = Time(-2), endAt = Time(-2).AddHours(1) }))).GetProperty("data");
        updated.GetProperty("note").GetString().Should().Be("original note"); updated.GetProperty("startAt").GetDateTime().Should().Be(Time(-2));
        var cleared = (await Body(await f.Mentor.PatchAsJsonAsync(Url(f, id), new { meetLink = "http://example.test", note = " " }))).GetProperty("data"); cleared.GetProperty("note").ValueKind.Should().Be(JsonValueKind.Null);
        var invalid = await Body(await f.Mentor.PatchAsJsonAsync(Url(f, id), new { note = "only note" }), HttpStatusCode.BadRequest);
        invalid.GetProperty("data").GetProperty("meetLink").GetString().Should().Be("Meet link is required");
        await WithDb(async db => {
            var notices = await db.Notifications.Where(x => x.EventKey == "MENTOR_MEETING_CREATED:" + id).ToListAsync();
            notices.Select(x => x.RecipientId).Should().BeEquivalentTo([f.LeaderAccount, f.MemberAccount]);
            notices.Should().OnlyContain(x => x.Type == "MENTOR_MEETING_BOOKED" && x.ActionKey == "OPEN_MEETING");
        });
    }

    [Theory]
    [InlineData(17, 60, "Meeting must start on the hour or half hour with no seconds")]
    [InlineData(0, 45, "Meeting must last exactly 60 minutes")]
    public async Task DirectTimePolicyMatchesJava(int minute, int duration, string message)
    {
        using var f = await Seed(); var start = Time().AddMinutes(minute);
        await Error(await f.Mentor.PostAsJsonAsync(Base(f) + "/meetings", new { startAt = start, endAt = start.AddMinutes(duration), meetLink = "https://example.test" }), HttpStatusCode.BadRequest, message);
    }

    [Fact]
    public async Task CreateDtoRejectsMixedShapeMissingLinkAndOversizeFields()
    {
        using var f = await Seed();
        var mixed = await Body(await f.Leader.PostAsJsonAsync(Base(f) + "/meetings", new { slotId = 1, startAt = Time(), endAt = Time().AddHours(1) }), HttpStatusCode.BadRequest);
        mixed.GetProperty("data").GetProperty("validRequestShape").GetString().Should().Be("Provide either slotId or both startAt and endAt");
        var missing = await Body(await f.Mentor.PostAsJsonAsync(Base(f) + "/meetings", new { startAt = Time(), endAt = Time().AddHours(1) }), HttpStatusCode.BadRequest);
        missing.GetProperty("data").GetProperty("meetLinkProvidedForDirectMeeting").GetString().Should().Be("Meet link is required when creating a meeting directly");
        await Body(await f.Mentor.PostAsJsonAsync(Base(f) + "/meetings", new { startAt = Time(), endAt = Time().AddHours(1), meetLink = "https://" + new string('a', 500) }), HttpStatusCode.BadRequest);
        await Body(await f.Mentor.PostAsJsonAsync(Base(f) + "/meetings", Direct(Time(), new string('a', 501))), HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task BookingChecksLeaderFutureAvailabilityAndClosedTerm()
    {
        using var f = await Seed(); var slot = await Slot(f, Time()); var past = await Slot(f, Time(-1));
        await Error(await f.Member.PostAsJsonAsync(Base(f) + "/meetings", new { slotId = slot }), HttpStatusCode.Forbidden, "Only the group leader can book a meeting");
        await Error(await f.Leader.PostAsJsonAsync(Base(f) + "/meetings", new { slotId = past }), HttpStatusCode.BadRequest, "Cannot book a slot in the past");
        var body = await Body(await f.Leader.PostAsJsonAsync(Base(f) + "/meetings", new { slotId = slot }));
        body.GetProperty("message").GetString().Should().Be("Meeting booked successfully"); var id = body.GetProperty("data").GetProperty("id").GetInt64();
        await Error(await f.Leader.PostAsJsonAsync(Base(f) + "/meetings", new { slotId = slot }), HttpStatusCode.BadRequest, "Slot is not available for booking");
        await WithDb(async db => {
            (await db.MentorAvailabilitySlots.SingleAsync(x => x.Id == slot)).Status.Should().Be("BOOKED");
            var notices = await db.Notifications.Where(x => x.EventKey == "MENTOR_MEETING_BOOKED:" + id).ToListAsync();
            notices.Select(x => x.RecipientId).Should().BeEquivalentTo([f.MentorAccount, f.MemberAccount]);
            notices[0].ActionParams.Should().Contain("slotId");
            (await db.AcademicTerms.SingleAsync(x => x.Code == f.Term)).Status = "CLOSED"; await db.SaveChangesAsync();
        });
        await Error(await f.Leader.PostAsJsonAsync(Base(f) + "/meetings", new { slotId = slot }), HttpStatusCode.Conflict, $"Academic term {f.Term} has ended; this group is read-only for students");
    }

    [Fact]
    public async Task QuotaCountsCompletedAndScheduledButNotCanceled()
    {
        using var f = await Seed(); await Meeting(f, Time(-3), "COMPLETED"); var scheduled = await Meeting(f, Time(-2)); await Meeting(f, Time(-1), "CANCELED");
        await Error(await f.Mentor.PostAsJsonAsync(Base(f) + "/meetings", Direct(Time())), HttpStatusCode.Conflict, "A group can have at most 2 non-canceled mentor meetings");
        var slot = await Slot(f, Time(2));
        await Error(await f.Leader.PostAsJsonAsync(Base(f) + "/meetings", new { slotId = slot }), HttpStatusCode.Conflict, "A group can have at most 2 non-canceled mentor meetings");
        await Body(await f.Mentor.PatchAsJsonAsync(Url(f, scheduled, "cancel"), new { reason = "Canceled" }));
        await Body(await f.Mentor.PostAsJsonAsync(Base(f) + "/meetings", Direct(Time())));
    }

    [Fact]
    public async Task MemberEvidenceRequiresEndedScheduledActiveAndCompletesOnlyOnce()
    {
        using var f = await Seed(); var future = await Meeting(f, Time()); var past = await Meeting(f, Time(-1));
        await Error(await f.Member.PutAsJsonAsync(Url(f, future, "evidence"), new { imageUrl = "https://example.test/evidence" }), HttpStatusCode.BadRequest, "Meeting evidence can only be submitted after the meeting ends");
        await Error(await f.Outsider.PutAsJsonAsync(Url(f, past, "evidence"), new { imageUrl = "https://example.test/evidence" }), HttpStatusCode.Forbidden, "Only an active group member can submit evidence");
        await WithDb(async db => { (await db.Students.SingleAsync(x => x.Id == f.MemberId)).Status = "INACTIVE"; await db.SaveChangesAsync(); });
        await Error(await f.Member.PutAsJsonAsync(Url(f, past, "evidence"), new { imageUrl = "https://example.test/evidence" }), HttpStatusCode.Forbidden, "Only an active group member can submit evidence");
        await WithDb(async db => { (await db.Students.SingleAsync(x => x.Id == f.MemberId)).Status = "ACTIVE"; await db.SaveChangesAsync(); });
        var data = (await Body(await f.Member.PutAsJsonAsync(Url(f, past, "evidence"), new { imageUrl = "https://example.test/evidence  " }))).GetProperty("data");
        data.GetProperty("status").GetString().Should().Be("COMPLETED"); data.GetProperty("evidenceSubmittedByStudentId").GetInt64().Should().Be(f.MemberId);
        data.GetProperty("evidenceImageUrl").GetString().Should().Be("https://example.test/evidence");
        await Error(await f.Leader.PutAsJsonAsync(Url(f, past, "evidence"), new { imageUrl = "https://example.test/replace" }), HttpStatusCode.Conflict, "Evidence can only be submitted for a scheduled meeting");
        await Error(await f.Mentor.PatchAsJsonAsync(Url(f, past, "cancel"), new { reason = "cancel" }), HttpStatusCode.BadRequest, "Completed meetings cannot be canceled");
        await Error(await f.Mentor.PatchAsJsonAsync(Url(f, past), new { meetLink = "https://example.test" }), HttpStatusCode.Conflict, "Completed or canceled meetings cannot be changed");
        await WithDb(async db => (await db.Notifications.Where(x => x.EventKey == "MENTOR_MEETING_EVIDENCE:" + past).Select(x => x.RecipientId).ToListAsync()).Should().BeEquivalentTo([f.MentorAccount, f.InstructorAccount]));
    }

    [Fact]
    public async Task CancelIsIdempotentAndCancelsSlotInsteadOfReopeningIt()
    {
        using var f = await Seed(); var id = await Meeting(f, Time(), slot: true);
        await Error(await f.Leader.PatchAsJsonAsync(Url(f, id, "cancel"), new { reason = "No" }), HttpStatusCode.Forbidden, "Only the assigned mentor can cancel this meeting");
        var first = (await Body(await f.Mentor.PatchAsJsonAsync(Url(f, id, "cancel"), new { reason = "  Busy  " }))).GetProperty("data");
        var again = (await Body(await f.Mentor.PatchAsJsonAsync(Url(f, id, "cancel"), new { reason = "Changed" }))).GetProperty("data");
        again.GetProperty("cancelReason").GetString().Should().Be("Busy"); again.GetProperty("canceledAt").GetDateTime().Should().BeCloseTo(first.GetProperty("canceledAt").GetDateTime(), TimeSpan.FromMicroseconds(1));
        await WithDb(async db => {
            var m = await db.MentorMeetings.Include(x => x.Slot).SingleAsync(x => x.Id == id); m.Slot!.Status.Should().Be("CANCELED");
            (await db.Notifications.CountAsync(x => x.EventKey == "MENTOR_MEETING_CANCELED:" + id)).Should().Be(2);
        });
        await Error(await f.Mentor.PatchAsync(Url(f, id, "confirm"), null), HttpStatusCode.BadRequest, "Canceled meetings cannot be confirmed");
    }

    [Fact]
    public async Task ConfirmationNeedsStartedSlotKeepsFirstTimestampsAndDoesNotComplete()
    {
        using var f = await Seed(); var direct = await Meeting(f, Time(-3)); var future = await Meeting(f, Time(), slot: true); var past = await Meeting(f, Time(-1), slot: true);
        foreach (var id in new[] { direct, future }) await Error(await f.Leader.PatchAsync(Url(f, id, "confirm"), null), HttpStatusCode.BadRequest, "Meeting can only be confirmed after it starts");
        await Error(await f.Member.PatchAsync(Url(f, past, "confirm"), null), HttpStatusCode.Forbidden, "Only group leader or assigned mentor can confirm this meeting");
        var first = (await Body(await f.Leader.PatchAsync(Url(f, past, "confirm"), null))).GetProperty("data");
        var again = (await Body(await f.Leader.PatchAsync(Url(f, past, "confirm"), null))).GetProperty("data");
        again.GetProperty("leaderConfirmedAt").GetDateTime().Should().BeCloseTo(first.GetProperty("leaderConfirmedAt").GetDateTime(), TimeSpan.FromMicroseconds(1));
        await WithDb(async db => (await db.Notifications.CountAsync(x => x.EntityId == past.ToString() && x.Type == "MENTOR_MEETING_CONFIRMED")).Should().Be(1));
        var mentor = (await Body(await f.Mentor.PatchAsync(Url(f, past, "confirm"), null))).GetProperty("data");
        mentor.GetProperty("status").GetString().Should().Be("SCHEDULED"); mentor.GetProperty("completedAt").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task ConcurrentSlotBookingsAndMentorOverlapDoNotDoubleAllocate()
    {
        using var f = await Seed(); var slot = await Slot(f, Time());
        var bookings = await Task.WhenAll(f.Leader.PostAsJsonAsync(Base(f) + "/meetings", new { slotId = slot }),
            f.Leader.PostAsJsonAsync($"/api/groups/{f.SecondGroup}/mentor/meetings", new { slotId = slot }));
        bookings.Select(x => x.StatusCode).Should().BeEquivalentTo([HttpStatusCode.OK, HttpStatusCode.BadRequest]);
        var creates = await Task.WhenAll(f.Mentor.PostAsJsonAsync(Base(f) + "/meetings", Direct(Time(2))),
            f.Mentor.PostAsJsonAsync($"/api/groups/{f.SecondGroup}/mentor/meetings", Direct(Time(2))));
        creates.Select(x => x.StatusCode).Should().BeEquivalentTo([HttpStatusCode.OK, HttpStatusCode.Conflict]);
        await WithDb(async db => {
            (await db.MentorMeetings.CountAsync(x => x.SlotId == slot)).Should().Be(1);
            (await db.MentorMeetings.CountAsync(x => x.MentorId == f.MentorId && x.StartAt == Time(2))).Should().Be(1);
        });
    }

    [Fact]
    public async Task ConcurrentCreatesRespectGroupQuota()
    {
        using var f = await Seed(); await Meeting(f, Time(-2));
        var results = await Task.WhenAll(f.Mentor.PostAsJsonAsync(Base(f) + "/meetings", Direct(Time(2))),
            f.Mentor.PostAsJsonAsync(Base(f) + "/meetings", Direct(Time(3))));
        results.Select(x => x.StatusCode).Should().BeEquivalentTo([HttpStatusCode.OK, HttpStatusCode.Conflict]);
        await WithDb(async db => (await db.MentorMeetings.CountAsync(x => x.GroupId == f.Group)).Should().Be(2));
    }

    [Fact]
    public async Task ClosedTermBlocksStudentEvidenceAndConfirmationButNotMentorDirectWrites()
    {
        using var f = await Seed(); var id = await Meeting(f, Time(-2), slot: true);
        await WithDb(async db => { (await db.AcademicTerms.SingleAsync(x => x.Code == f.Term)).Status = "CLOSED"; await db.SaveChangesAsync(); });
        var error = $"Academic term {f.Term} has ended; this group is read-only for students";
        await Error(await f.Leader.PatchAsync(Url(f, id, "confirm"), null), HttpStatusCode.Conflict, error);
        await Error(await f.Member.PutAsJsonAsync(Url(f, id, "evidence"), new { imageUrl = "https://example.test/evidence" }), HttpStatusCode.Conflict, error);
        await Body(await f.Mentor.PatchAsync(Url(f, id, "confirm"), null));
        await Body(await f.Mentor.PatchAsJsonAsync(Url(f, id), new { meetLink = "https://example.test/updated" }));
        await Body(await f.Mentor.PostAsJsonAsync(Base(f) + "/meetings", Direct(Time(-1))));
    }

    [Fact]
    public async Task NotificationFailureRollsBackBookingAndSlotStatus()
    {
        using var f = await Seed(); var slot = await Slot(f, Time()); var recipient = f.MentorAccount.ToString(System.Globalization.CultureInfo.InvariantCulture);
        await WithDb(db => db.Database.ExecuteSqlRawAsync($"""
            CREATE FUNCTION parity_fail_meeting_{recipient}() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'fixture failure'; END; $$;
            CREATE TRIGGER parity_fail_meeting_{recipient} BEFORE INSERT ON notifications FOR EACH ROW
            WHEN (NEW.recipient_id = {recipient} AND NEW.type = 'MENTOR_MEETING_BOOKED') EXECUTE FUNCTION parity_fail_meeting_{recipient}();
            """));
        try
        {
            await Body(await f.Leader.PostAsJsonAsync(Base(f) + "/meetings", new { slotId = slot }), HttpStatusCode.Conflict);
            await WithDb(async db => {
                (await db.MentorMeetings.AnyAsync(x => x.SlotId == slot)).Should().BeFalse();
                (await db.MentorAvailabilitySlots.SingleAsync(x => x.Id == slot)).Status.Should().Be("AVAILABLE");
            });
        }
        finally { await WithDb(db => db.Database.ExecuteSqlRawAsync($"DROP TRIGGER parity_fail_meeting_{recipient} ON notifications; DROP FUNCTION parity_fail_meeting_{recipient}();")); }
    }
}
