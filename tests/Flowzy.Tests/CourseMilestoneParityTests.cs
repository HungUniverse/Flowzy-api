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

public sealed class CourseMilestoneParityTests(FlowzyApiFactory factory) : IClassFixture<FlowzyApiFactory>
{
    private const string Base = "/api/course-milestones";
    private const string Alias = "/api/instructor/milestones";
    private static DateTime Deadline => DateTime.UtcNow.Date.AddDays(5);
    private sealed record Fixture(string Term, string Course, long InstructorId, long OtherId, long GroupId, long StudentId, long StudentAccount,
        HttpClient Instructor, HttpClient Other, HttpClient Student, HttpClient Outsider, HttpClient Admin) : IDisposable
    { public void Dispose() { Instructor.Dispose(); Other.Dispose(); Student.Dispose(); Outsider.Dispose(); Admin.Dispose(); } }
    private async Task<Fixture> Seed()
    {
        using var bootstrap = factory.CreateClient(); await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FlowzyDbContext>(); var key = Guid.NewGuid().ToString("N")[..14].ToUpperInvariant();
        Account Account(string role, int n) => new() { Email = key + n + "@timeline.test", Role = role, Status = "ACTIVE", PasswordHash = "unused" };
        var instructors = Enumerable.Range(0, 2).Select(i => new Instructor { InstructorCode = key + i, FullName = "Instructor" + i, Status = "ACTIVE", Account = Account("INSTRUCTOR", i) }).ToArray();
        var students = Enumerable.Range(2, 2).Select(i => new Student { StudentCode = key + i, FullName = "Student" + i, Status = "ACTIVE", Account = Account("STUDENT", i) }).ToArray();
        var admin = Account("ADMIN", 4);
        var term = await db.AcademicTerms.FirstOrDefaultAsync(x => x.Status == "OPEN") ?? new AcademicTerm { Code = "T" + key, Status = "OPEN" };
        if (term.Id == 0) db.AcademicTerms.Add(term);
        db.Instructors.AddRange(instructors); db.Students.AddRange(students); db.Accounts.Add(admin); await db.SaveChangesAsync();
        var group = new StudentGroup { Term = term.Code, CourseCode = "C" + key, Name = "Timeline Group " + key, GroupNo = "1", Status = "ACTIVE", InstructorId = instructors[0].Id };
        db.StudentGroups.Add(group); await db.SaveChangesAsync();
        db.StudentGroupMembers.Add(new StudentGroupMember { GroupId = group.Id, StudentId = students[0].Id, MemberRole = "LEADER" }); await db.SaveChangesAsync();
        group.LeaderStudentId = students[0].Id; await db.SaveChangesAsync();
        var jwt = scope.ServiceProvider.GetRequiredService<IJwtService>();
        HttpClient Client(Account account) { var c = factory.CreateClient(); c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt.GenerateAccessToken(account)); return c; }
        return new(term.Code, group.CourseCode, instructors[0].Id, instructors[1].Id, group.Id, students[0].Id, students[0].AccountId,
            Client(instructors[0].Account), Client(instructors[1].Account), Client(students[0].Account), Client(students[1].Account), Client(admin));
    }
    private async Task WithDb(Func<FlowzyDbContext, Task> action)
    { await using var scope = factory.Services.CreateAsyncScope(); await action(scope.ServiceProvider.GetRequiredService<FlowzyDbContext>()); }
    private static Dictionary<string, object?> Request(Fixture f, string title = "Milestone", int? weight = 20) => new()
    { ["term"] = f.Term, ["courseCode"] = f.Course, ["title"] = title, ["description"] = "  raw description  ", ["weight"] = weight, ["deadlineAt"] = Deadline };
    private static async Task<JsonElement> Body(HttpResponseMessage response, HttpStatusCode expected = HttpStatusCode.OK)
    { var raw = await response.Content.ReadAsStringAsync(); response.StatusCode.Should().Be(expected, raw); using var doc = JsonDocument.Parse(raw); return doc.RootElement.Clone(); }
    private static async Task Error(HttpResponseMessage response, HttpStatusCode expected, string message) =>
        (await Body(response, expected)).GetProperty("message").GetString().Should().Be(message);
    private async Task<long> Create(Fixture f, string title = "Milestone", int? weight = 20) =>
        (await Body(await f.Instructor.PostAsJsonAsync(Base, Request(f, title, weight)))).GetProperty("data").GetProperty("id").GetInt64();
    private async Task<long> Row(Fixture f, string title, string status, long position, long? owner = null, int? weight = 0)
    {
        long id = 0; await WithDb(async db => { var m = new CourseMilestone { Term = f.Term, CourseCode = f.Course, Title = title,
            Status = status, Position = position, InstructorId = owner ?? f.InstructorId, Type = "TIMELINE", MaxScore = 10, DueDate = Deadline, Weight = weight };
            db.CourseMilestones.Add(m); await db.SaveChangesAsync(); id = m.Id; }); return id;
    }
    private static long[] Ids(JsonElement body) => body.GetProperty("data").EnumerateArray().Select(x => x.GetProperty("id").GetInt64()).ToArray();

    [Fact]
    public async Task AliasDefaultsRawDescriptionCaseInsensitivePositionAndLeaderNotification()
    {
        using var f = await Seed(); var prior = await Row(f, "Archived", "ARCHIVED", 9);
        var request = Request(f, "  New title  ", null); request.Remove("deadlineAt"); request["dueDate"] = Deadline;
        request["term"] = " " + f.Term.ToLowerInvariant() + " "; request["courseCode"] = f.Course.ToLowerInvariant();
        var data = (await Body(await f.Instructor.PostAsJsonAsync(Alias, request))).GetProperty("data");
        var id = data.GetProperty("id").GetInt64(); data.GetProperty("title").GetString().Should().Be("New title");
        data.GetProperty("description").GetString().Should().Be("  raw description  "); data.GetProperty("weight").ValueKind.Should().Be(JsonValueKind.Null);
        data.GetProperty("position").GetInt64().Should().Be(10); data.GetProperty("maxScore").GetDecimal().Should().Be(10);
        data.GetProperty("deadlineAt").GetDateTime().Should().Be(Deadline); data.TryGetProperty("dueDate", out _).Should().BeFalse();
        await WithDb(async db => { var notice = await db.Notifications.SingleAsync(x => x.EntityType == "CourseMilestone" && x.EntityId == id.ToString());
            notice.RecipientId.Should().Be(f.StudentAccount); notice.Type.Should().Be("TIMELINE_ITEM_CREATED"); notice.ActionKey.Should().Be("OPEN_MILESTONE");
            notice.Title.Should().Be("New Milestone Released"); notice.ActionParams.Should().Contain("milestoneId"); });
    }

    [Theory]
    [InlineData("weight", "-1", "Weight must be non-negative")]
    [InlineData("weight", "101", "Weight cannot exceed 100")]
    [InlineData("maxScore", "0", "Max score must be greater than 0")]
    [InlineData("maxScore", "0.001", "Max score must be greater than 0")]
    [InlineData("position", "-1", "Position must be non-negative")]
    [InlineData("title", "", "Title is required")]
    public async Task CreateAndUpdateEnforceJavaDtoRanges(string field, string raw, string message)
    {
        using var f = await Seed(); var id = await Create(f); var request = Request(f, "Changed");
        request[field] = field == "title" ? raw : field == "maxScore" ? decimal.Parse(raw, System.Globalization.CultureInfo.InvariantCulture) : long.Parse(raw);
        foreach (var response in new[] { await f.Instructor.PostAsJsonAsync(Base, request), await f.Instructor.PatchAsJsonAsync(Base + "/" + id, request) })
            (await Body(response, HttpStatusCode.BadRequest)).GetProperty("data").GetProperty(field).GetString().Should().Be(message);
        await WithDb(async db => (await db.CourseMilestones.FindAsync(id))!.Title.Should().Be("Milestone"));
    }

    [Fact]
    public async Task LengthValidationAndInvalidStatusHaveOriginalErrorShape()
    {
        using var f = await Seed();
        foreach (var (field, length, message) in new[] { ("term", 31, "Term must be at most 30 characters"), ("courseCode", 31, "Course code must be at most 30 characters"), ("title", 256, "Title must be at most 255 characters") })
        { var request = Request(f); request[field] = new string('x', length); (await Body(await f.Instructor.PostAsJsonAsync(Base, request), HttpStatusCode.BadRequest)).GetProperty("data").GetProperty(field).GetString().Should().Be(message); }
        var id = await Create(f); var update = Request(f); update["status"] = "active";
        await Error(await f.Instructor.PutAsJsonAsync(Alias + "/" + id, update), HttpStatusCode.BadRequest, "Malformed JSON request");
        update["status"] = "INACTIVE"; await Body(await f.Instructor.PutAsJsonAsync(Alias + "/" + id, update));
    }

    [Fact]
    public async Task StudentListVisibilityDiffersFromDetailAndUsesPairedExactMembershipScope()
    {
        using var f = await Seed(); var active = await Row(f, "Active", "ACTIVE", 3); var closed = await Row(f, "Closed", "CLOSED", 1);
        var inactive = await Row(f, "Inactive", "INACTIVE", 0); var archived = await Row(f, "Archived", "ARCHIVED", 2);
        await Row(f, "Other owner", "ACTIVE", 0, f.OtherId);
        Ids(await Body(await f.Student.GetAsync(Base))).Should().Equal(closed, active);
        Ids(await Body(await f.Student.GetAsync($"/api/groups/{f.GroupId}/milestones"))).Should().Equal(closed, active);
        Ids(await Body(await f.Instructor.GetAsync(Base + "?term=" + f.Term))).Should().Equal(inactive, closed, archived, active);
        await Body(await f.Student.GetAsync(Base + "/" + inactive));
        await Error(await f.Student.GetAsync(Base + "/" + archived), HttpStatusCode.Forbidden, "Milestone is not visible to you");
        foreach (var filter in new[] { "term=" + f.Term, "courseCode=" + f.Course })
            await Error(await f.Student.GetAsync(Base + "?" + filter), HttpStatusCode.BadRequest, "Both term and courseCode are required when filtering student timelines");
        await Error(await f.Student.GetAsync(Base + "?term=" + f.Term.ToLowerInvariant() + "&courseCode=" + f.Course), HttpStatusCode.Forbidden, "You are not in a group for this term and course");
        Ids(await Body(await f.Student.GetAsync(Base + "?term=" + f.Term + "&courseCode=" + f.Course))).Should().Equal(closed, active);
        await WithDb(async db => { (await db.Students.FindAsync(f.StudentId))!.Status = "INACTIVE"; (await db.StudentGroups.FindAsync(f.GroupId))!.Status = "INACTIVE"; await db.SaveChangesAsync(); });
        Ids(await Body(await f.Student.GetAsync(Base))).Should().Equal(closed, active);
        await WithDb(async db => { (await db.StudentGroups.FindAsync(f.GroupId))!.InstructorId = null; await db.SaveChangesAsync(); });
        Ids(await Body(await f.Student.GetAsync(Base))).Should().BeEmpty();
    }

    [Fact]
    public async Task AuthorizationDistinguishesInstructorAliasAndPreservesLookupPrecedence()
    {
        using var f = await Seed(); var id = await Create(f);
        await Error(await f.Other.GetAsync(Base + "/" + id), HttpStatusCode.Forbidden, "You do not manage this timeline milestone");
        await Error(await f.Other.PatchAsJsonAsync(Base + "/" + id, Request(f)), HttpStatusCode.Forbidden, "You do not manage this timeline milestone");
        await Error(await f.Outsider.GetAsync(Base + "/" + id), HttpStatusCode.Forbidden, "Milestone is not visible to you");
        await Error(await f.Admin.GetAsync(Base), HttpStatusCode.Forbidden, "You cannot view this instructor timeline");
        await Error(await f.Admin.GetAsync(Base + "/" + long.MaxValue), HttpStatusCode.NotFound, "Timeline milestone not found");
        await Error(await f.Student.PostAsJsonAsync(Base, Request(f)), HttpStatusCode.Forbidden, "Only instructors can manage timelines");
        await Error(await f.Admin.GetAsync($"/api/groups/{f.GroupId}/milestones"), HttpStatusCode.Forbidden, "You cannot view this group's timeline");
        foreach (var client in new[] { f.Student, f.Admin })
        { await Body(await client.GetAsync(Alias), HttpStatusCode.Forbidden); await Body(await client.GetAsync(Alias + "/0/outcomes"), HttpStatusCode.Forbidden); }
        foreach (var client in new[] { f.Student, f.Admin, f.Instructor })
        { await Body(await client.GetAsync(Base + "/0/outcomes"), HttpStatusCode.Gone); await Body(await client.PostAsync(Base + "/0/outcomes", null), HttpStatusCode.Gone); }
        await Body(await f.Instructor.GetAsync(Alias + "/0/outcomes"), HttpStatusCode.Gone);
        using var anonymous = factory.CreateClient(); await Body(await anonymous.GetAsync(Base), HttpStatusCode.Unauthorized);
        await WithDb(async db => { (await db.Instructors.FindAsync(f.InstructorId))!.Status = "INACTIVE"; await db.SaveChangesAsync(); });
        await Error(await f.Instructor.DeleteAsync(Base + "/" + long.MaxValue), HttpStatusCode.Forbidden, "Instructor profile is inactive");
    }

    [Fact]
    public async Task CreateRequiresOpenAssignedScopeButUpdateAndArchiveRemainAllowedAfterClose()
    {
        using var f = await Seed(); var id = await Create(f);
        await Error(await f.Other.PostAsJsonAsync(Base, Request(f)), HttpStatusCode.Forbidden, "You must have at least one assigned group in this term and course");
        var missing = Request(f); missing["term"] = "MISSING";
        await Error(await f.Instructor.PostAsJsonAsync(Base, missing), HttpStatusCode.BadRequest, "Academic term does not exist");
        await WithDb(async db => { (await db.AcademicTerms.SingleAsync(x => x.Code == f.Term)).Status = "CLOSED"; (await db.StudentGroups.FindAsync(f.GroupId))!.InstructorId = null; await db.SaveChangesAsync(); });
        await Error(await f.Instructor.PostAsJsonAsync(Base, Request(f, "new")), HttpStatusCode.BadRequest, "Academic term is closed");
        var update = Request(f, "Updated"); update.Remove("deadlineAt"); update["dueDate"] = Deadline.AddDays(1);
        await Body(await f.Instructor.PatchAsJsonAsync(Base + "/" + id, update));
        await Body(await f.Instructor.DeleteAsync(Alias + "/" + id)); await Body(await f.Instructor.DeleteAsync(Base + "/" + id));
        await WithDb(async db => (await db.CourseMilestones.FindAsync(id))!.Status.Should().Be("ARCHIVED"));
    }

    [Fact]
    public async Task TitleUniquenessWeightLimitNullablePatchAndConcurrentWeightChecks()
    {
        using var f = await Seed(); var id = await Create(f, "Unique", 60);
        await Error(await f.Instructor.PostAsJsonAsync(Base, Request(f, " UNIQUE ")), HttpStatusCode.BadRequest, "Timeline milestone title must be unique in your term and course");
        var outcomes = await Task.WhenAll(f.Instructor.PostAsJsonAsync(Base, Request(f, "Second", 30)), f.Instructor.PostAsJsonAsync(Base, Request(f, "Third", 30)));
        outcomes.Select(x => x.StatusCode).Should().BeEquivalentTo([HttpStatusCode.OK, HttpStatusCode.BadRequest]);
        var update = Request(f, "Unique", null); update.Remove("description");
        var data = (await Body(await f.Instructor.PatchAsJsonAsync(Base + "/" + id, update))).GetProperty("data");
        data.GetProperty("weight").ValueKind.Should().Be(JsonValueKind.Null); data.GetProperty("description").ValueKind.Should().Be(JsonValueKind.Null);
        await Create(f, "Fourth", 70);
        update["weight"] = 100; update["status"] = "CLOSED"; await Body(await f.Instructor.PatchAsJsonAsync(Base + "/" + id, update));
        update["status"] = "ACTIVE"; await Error(await f.Instructor.PatchAsJsonAsync(Base + "/" + id, update), HttpStatusCode.BadRequest, "Total active milestone weight cannot exceed 100");
        await Body(await f.Instructor.DeleteAsync(Base + "/" + id));
        await Error(await f.Instructor.PostAsJsonAsync(Base, Request(f, "unique", 0)), HttpStatusCode.BadRequest, "Timeline milestone title must be unique in your term and course");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExistingLegacyAndMatrixGradesProtectMaxScoreAndArchivePreservesHistory(bool matrix)
    {
        using var f = await Seed(); var id = await Create(f); long submissionId = 0;
        await WithDb(async db => { var sub = new MilestoneSubmission { MilestoneId = id, GroupId = f.GroupId, Status = "SUBMITTED", SubmittedAt = Deadline, Late = false, Version = 1 };
            db.MilestoneSubmissions.Add(sub); await db.SaveChangesAsync(); submissionId = sub.Id;
            if (matrix) db.MilestoneGroupGrades.Add(new MilestoneGroupGrade { MilestoneId = id, GroupId = f.GroupId, InstructorId = f.InstructorId, Score = 8, MaxScoreSnapshot = 10, WeightSnapshot = 20 });
            else db.MilestoneGrades.Add(new MilestoneGrade { SubmissionId = sub.Id, InstructorId = f.InstructorId, Score = 8, MaxScore = 10 });
            await db.SaveChangesAsync(); });
        var update = Request(f); update["maxScore"] = 7;
        await Error(await f.Instructor.PutAsJsonAsync(Base + "/" + id, update), HttpStatusCode.BadRequest, "Max score cannot be lower than an existing grade");
        update["maxScore"] = 8; update["deadlineAt"] = Deadline.AddHours(-1); await Body(await f.Instructor.PutAsJsonAsync(Base + "/" + id, update));
        await WithDb(async db => (await db.MilestoneSubmissions.FindAsync(submissionId))!.Late.Should().BeTrue());
        update["deadlineAt"] = Deadline; await Body(await f.Instructor.PutAsJsonAsync(Base + "/" + id, update));
        await WithDb(async db => (await db.MilestoneSubmissions.FindAsync(submissionId))!.Late.Should().BeFalse());
        await Body(await f.Instructor.DeleteAsync(Base + "/" + id));
        await WithDb(async db => { (await db.MilestoneSubmissions.FindAsync(submissionId)).Should().NotBeNull();
            if (matrix) (await db.MilestoneGroupGrades.AnyAsync(x => x.MilestoneId == id)).Should().BeTrue();
            else (await db.MilestoneGrades.AnyAsync(x => x.SubmissionId == submissionId)).Should().BeTrue(); });
    }

    [Fact]
    public async Task MissingDeadlineAndDuplicateTitleKeepCreateVersusUpdateErrorOrder()
    {
        using var f = await Seed(); var id = await Create(f); await Create(f, "Other title", 0);
        var request = Request(f); request.Remove("deadlineAt");
        await Error(await f.Instructor.PostAsJsonAsync(Base, request), HttpStatusCode.BadRequest, "Deadline is required");
        request["title"] = "Other title";
        await Error(await f.Instructor.PatchAsJsonAsync(Base + "/" + id, request), HttpStatusCode.BadRequest, "Timeline milestone title must be unique in your term and course");
        request["title"] = "New title";
        await Error(await f.Instructor.PatchAsJsonAsync(Base + "/" + id, request), HttpStatusCode.BadRequest, "Deadline is required");
        request["deadlineAt"] = Deadline; request["maxScore"] = 0.01m;
        (await Body(await f.Instructor.PatchAsJsonAsync(Base + "/" + id, request))).GetProperty("data").GetProperty("maxScore").GetDecimal().Should().Be(0.01m);
    }

    [Fact]
    public async Task NotificationsRespectRecipientStatusAndMilestoneStateWithoutFilteringInactiveGroups()
    {
        using var f = await Seed();
        await WithDb(async db => { (await db.StudentGroups.FindAsync(f.GroupId))!.Status = "INACTIVE"; await db.SaveChangesAsync(); });
        var id = await Create(f);
        await WithDb(async db => (await db.Notifications.CountAsync(x => x.EntityType == "CourseMilestone" && x.EntityId == id.ToString())).Should().Be(1));
        var update = Request(f, "Inactive milestone"); update["status"] = "INACTIVE";
        await Body(await f.Instructor.PatchAsJsonAsync(Base + "/" + id, update));
        await WithDb(async db => { (await db.Notifications.CountAsync(x => x.EntityType == "CourseMilestone" && x.EntityId == id.ToString())).Should().Be(1);
            (await db.Accounts.FindAsync(f.StudentAccount))!.Status = "INACTIVE"; await db.SaveChangesAsync(); });
        update["status"] = "ACTIVE"; update["title"] = "Reactivated";
        await Body(await f.Instructor.PutAsJsonAsync(Alias + "/" + id, update));
        await WithDb(async db => (await db.Notifications.CountAsync(x => x.EntityType == "CourseMilestone" && x.EntityId == id.ToString())).Should().Be(1));
    }

    [Fact]
    public async Task NotificationFailureRollsBackNewMilestone()
    {
        using var f = await Seed(); var recipient = f.StudentAccount;
        await WithDb(db => db.Database.ExecuteSqlRawAsync($"""
            CREATE FUNCTION parity_fail_timeline_{recipient}() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'fixture failure'; END; $$;
            CREATE TRIGGER parity_fail_timeline_{recipient} BEFORE INSERT ON notifications FOR EACH ROW
            WHEN (NEW.recipient_id = {recipient} AND NEW.type = 'TIMELINE_ITEM_CREATED') EXECUTE FUNCTION parity_fail_timeline_{recipient}();
            """));
        try
        {
            await Body(await f.Instructor.PostAsJsonAsync(Base, Request(f)), HttpStatusCode.Conflict);
            await WithDb(async db => (await db.CourseMilestones.AnyAsync(x => x.InstructorId == f.InstructorId)).Should().BeFalse());
        }
        finally { await WithDb(db => db.Database.ExecuteSqlRawAsync($"DROP TRIGGER parity_fail_timeline_{recipient} ON notifications; DROP FUNCTION parity_fail_timeline_{recipient}();")); }
    }

    [Fact]
    public async Task LateFlagFailureRollsBackMilestoneAndNotificationTogether()
    {
        using var f = await Seed(); var id = await Create(f);
        await WithDb(async db => { db.MilestoneSubmissions.Add(new MilestoneSubmission { MilestoneId = id, GroupId = f.GroupId, SubmittedAt = Deadline, Status = "SUBMITTED", Late = false, Version = 1 }); await db.SaveChangesAsync();
            await db.Database.ExecuteSqlRawAsync($"""
                CREATE FUNCTION parity_fail_late_{id}() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'fixture failure'; END; $$;
                CREATE TRIGGER parity_fail_late_{id} BEFORE UPDATE ON milestone_submissions FOR EACH ROW WHEN (OLD.milestone_id = {id}) EXECUTE FUNCTION parity_fail_late_{id}();
                """); });
        try
        {
            var request = Request(f, "Must rollback"); request["deadlineAt"] = Deadline.AddHours(-1);
            await Body(await f.Instructor.PatchAsJsonAsync(Base + "/" + id, request), HttpStatusCode.Conflict);
            await WithDb(async db => { var m = await db.CourseMilestones.FindAsync(id); m!.Title.Should().Be("Milestone"); m.DueDate.Should().Be(Deadline);
                (await db.Notifications.CountAsync(x => x.EntityType == "CourseMilestone" && x.EntityId == id.ToString())).Should().Be(1);
                (await db.MilestoneSubmissions.SingleAsync(x => x.MilestoneId == id)).Late.Should().BeFalse(); });
        }
        finally { await WithDb(db => db.Database.ExecuteSqlRawAsync($"DROP TRIGGER parity_fail_late_{id} ON milestone_submissions; DROP FUNCTION parity_fail_late_{id}();")); }
    }
}
