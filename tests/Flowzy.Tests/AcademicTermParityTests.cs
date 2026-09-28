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

public sealed class AcademicTermParityTests(FlowzyApiFactory factory) : IClassFixture<FlowzyApiFactory>
{
    private sealed record Fixture(string Code, long Term, long Group, long EmptyGroup, Student[] Students,
        long Invitation, long Request, string Email, HttpClient Admin, HttpClient Student) : IDisposable
    { public void Dispose() { Admin.Dispose(); Student.Dispose(); } }

    private async Task WithDb(Func<FlowzyDbContext, Task> action)
    { await using var scope = factory.Services.CreateAsyncScope(); await action(scope.ServiceProvider.GetRequiredService<FlowzyDbContext>()); }

    private async Task<Fixture> Seed()
    {
        using var bootstrap = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<FlowzyDbContext>();
        // This fixture owns a disposable Testcontainers database, never the local application DB.
        await db.AcademicTerms.Where(x => x.Status == "OPEN").ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, "CLOSED"));
        var key = Guid.NewGuid().ToString("N"); var code = "T" + key[..20].ToUpperInvariant();
        Account Account(string role, int i) => new() { Email = $"{i}.{key}@local.test", PasswordHash = "unused", Role = role, Status = "ACTIVE" };
        var admin = Account("ADMIN", 0);
        var students = Enumerable.Range(1, 4).Select(i => new Student { StudentCode = i + key, FullName = "Student " + i, Status = "ACTIVE", Account = Account("STUDENT", i) }).ToArray();
        var mentor = new Mentor { MentorCode = key, FullName = "Mentor", Status = "ACTIVE", Account = Account("MENTOR", 5) };
        var instructor = new Instructor { InstructorCode = key, FullName = "Instructor", Status = "ACTIVE", Account = Account("INSTRUCTOR", 6) };
        var term = new AcademicTerm { Code = code, Status = "OPEN" };
        db.Accounts.Add(admin); db.Students.AddRange(students); db.Mentors.Add(mentor); db.Instructors.Add(instructor); db.AcademicTerms.Add(term); await db.SaveChangesAsync();
        var group = new StudentGroup { Term = code, CourseCode = "PRN232", GroupNo = "1", Name = key, Status = "ACTIVE", MentorId = mentor.Id, InstructorId = instructor.Id };
        var empty = new StudentGroup { Term = code, CourseCode = "PRN232", GroupNo = "2", Name = key + "empty", Status = "ACTIVE" };
        db.StudentGroups.AddRange(group, empty); await db.SaveChangesAsync();
        foreach (var s in students) db.StudentGroupMembers.Add(new StudentGroupMember { GroupId = group.Id, StudentId = s.Id, MemberRole = "MEMBER" });
        await db.SaveChangesAsync(); group.LeaderStudentId = students[0].Id;
        var invitation = new GroupInvitation { GroupId = empty.Id, InviterStudentId = students[0].Id, InviteeStudentId = students[1].Id, Status = "PENDING", CreatedAt = DateTime.UtcNow };
        var request = new GroupJoinRequest { GroupId = empty.Id, StudentId = students[2].Id, Status = "PENDING", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
        db.GroupInvitations.Add(invitation); db.GroupJoinRequests.Add(request); await db.SaveChangesAsync();
        var jwt = scope.ServiceProvider.GetRequiredService<IJwtService>();
        HttpClient Client(Account a) { var c = factory.CreateClient(); c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt.GenerateAccessToken(a)); return c; }
        return new(code, term.Id, group.Id, empty.Id, students, invitation.Id, request.Id, admin.Email, Client(admin), Client(students[0].Account));
    }

    private static async Task<JsonElement> Body(HttpResponseMessage response, HttpStatusCode status = HttpStatusCode.OK)
    {
        var raw = await response.Content.ReadAsStringAsync(); response.StatusCode.Should().Be(status, raw);
        using var doc = JsonDocument.Parse(raw); return doc.RootElement.Clone();
    }
    private static async Task Error(HttpResponseMessage response, HttpStatusCode status, string message) =>
        (await Body(response, status)).GetProperty("message").GetString().Should().Be(message);
    private static Task<HttpResponseMessage> Close(Fixture f) => f.Admin.PatchAsync($"/api/admin/terms/{f.Code.ToLowerInvariant()}/close", null);

    [Fact]
    public async Task CreateExistingOpenAndCloseAgainAreIdempotentWithSnapshotAndPendingCleanup()
    {
        using var f = await Seed();
        var created = await Body(await f.Admin.PostAsJsonAsync("/api/admin/terms", new { code = " " + f.Code.ToLowerInvariant() + " " }));
        created.GetProperty("data").GetProperty("id").GetInt64().Should().Be(f.Term);
        var first = (await Body(await Close(f))).GetProperty("data");
        first.GetProperty("status").GetString().Should().Be("CLOSED"); first.GetProperty("closedByEmail").GetString().Should().Be(f.Email);
        first.GetProperty("groupCount").GetInt64().Should().Be(2); first.GetProperty("totalExpectedFeedbacks").GetInt64().Should().Be(8);
        first.GetProperty("totalSubmittedFeedbacks").GetInt64().Should().Be(0);
        var second = (await Body(await Close(f))).GetProperty("data");
        // PostgreSQL persists microseconds; .NET/Java in-memory clocks have finer precision.
        second.GetProperty("closedAt").GetDateTime().Should().BeCloseTo(first.GetProperty("closedAt").GetDateTime(), TimeSpan.FromMicroseconds(1));
        second.GetProperty("totalExpectedFeedbacks").GetInt64().Should().Be(8);
        await WithDb(async db => {
            var invitation = await db.GroupInvitations.SingleAsync(x => x.Id == f.Invitation); invitation.Status.Should().Be("CANCELED"); invitation.RespondedAt.Should().NotBeNull();
            var request = await db.GroupJoinRequests.SingleAsync(x => x.Id == f.Request); request.Status.Should().Be("CANCELED"); request.RespondedAt.Should().Be(invitation.RespondedAt);
            (await db.TermFeedbacks.CountAsync(x => x.AcademicTermId == f.Term)).Should().Be(8);
            var notices = await db.Notifications.Where(x => x.EntityType == "AcademicTerm" && x.EntityId == f.Term.ToString()).ToListAsync(); notices.Should().HaveCount(4);
            notices.Should().OnlyContain(x => x.Type == "TERM_FEEDBACK_AVAILABLE" && x.ActionKey == "OPEN_FEEDBACK" && x.Title == "Feedback Forms Generated");
            notices[0].ActionParams.Should().Contain(f.Code);
            (await db.StudentGroups.SingleAsync(x => x.Id == f.Group)).Status.Should().Be("ACTIVE");
        });
        await Error(await f.Admin.PostAsJsonAsync("/api/admin/terms", new { code = f.Code }), HttpStatusCode.Conflict, "Academic term is already closed: " + f.Code);
    }

    [Fact]
    public async Task CreateNewTermRequiresClosingCurrentAndDeletionRequiresEmptyHistory()
    {
        using var f = await Seed(); var next = "N" + Guid.NewGuid().ToString("N")[..20].ToUpperInvariant();
        await Error(await f.Admin.PostAsJsonAsync("/api/admin/terms", new { code = next }), HttpStatusCode.Conflict, $"Close academic term {f.Code} before creating a new term");
        await Error(await f.Admin.DeleteAsync($"/api/admin/terms/{f.Code}"), HttpStatusCode.Conflict, "Academic term cannot be deleted because it already has group or feedback history");
        await Body(await Close(f)); await Body(await f.Admin.PostAsJsonAsync("/api/admin/terms", new { code = next.ToLowerInvariant() }));
        await Body(await f.Admin.DeleteAsync($"/api/admin/terms/{next.ToLowerInvariant()}"));
        await Error(await f.Admin.DeleteAsync($"/api/admin/terms/{next}"), HttpStatusCode.NotFound, "Academic term not found with code: " + next);
    }

    [Fact]
    public async Task ArchiveIgnoresPendingFeedbackReactivatesOtherOpenMembershipAndRevokesOnlyArchivedTokens()
    {
        using var f = await Seed(); await Body(await Close(f));
        var next = "N" + Guid.NewGuid().ToString("N")[..20].ToUpperInvariant(); await Body(await f.Admin.PostAsJsonAsync("/api/admin/terms", new { code = next }));
        await WithDb(async db => {
            var group = new StudentGroup { Term = next, CourseCode = "PRN232", GroupNo = "1", Name = next, Status = "INACTIVE" };
            db.StudentGroups.Add(group); await db.SaveChangesAsync();
            db.StudentGroupMembers.Add(new StudentGroupMember { GroupId = group.Id, StudentId = f.Students[1].Id, MemberRole = "MEMBER" });
            foreach (var student in f.Students)
            {
                var s = await db.Students.Include(x => x.Account).SingleAsync(x => x.Id == student.Id);
                if (student.Id == f.Students[1].Id || student.Id == f.Students[2].Id) { s.Status = "INACTIVE"; s.Account.Status = "INACTIVE"; }
                if (student.Id == f.Students[3].Id) s.Account.Status = "LOCKED";
                db.RefreshTokens.Add(new RefreshToken { AccountId = s.AccountId, TokenHash = Guid.NewGuid().ToString("N"), ExpiresAt = DateTime.UtcNow.AddDays(1), CreatedAt = DateTime.UtcNow });
            }
            await db.SaveChangesAsync();
        });
        var data = (await Body(await f.Admin.PostAsync($"/api/admin/terms/{f.Code}/archive-students", null))).GetProperty("data");
        data.GetProperty("archivedStudents").GetInt64().Should().Be(2); data.GetProperty("alreadyInactiveStudents").GetInt64().Should().Be(1);
        data.GetProperty("skippedActiveInOpenTerm").GetInt64().Should().Be(1); data.GetProperty("skippedPendingFeedbackStudents").GetInt64().Should().Be(0);
        await WithDb(async db => {
            (await db.StudentGroups.Where(x => x.Term == f.Code).Select(x => x.Status).ToListAsync()).Should().OnlyContain(x => x == "INACTIVE");
            for (var i = 0; i < 4; i++)
            {
                var id = f.Students[i].Id; var s = await db.Students.Include(x => x.Account).SingleAsync(x => x.Id == id);
                s.Status.Should().Be(i == 1 ? "ACTIVE" : "INACTIVE"); s.Account.Status.Should().Be(s.Status);
                (await db.RefreshTokens.AnyAsync(x => x.AccountId == s.AccountId)).Should().Be(i is 1 or 2);
            }
            (await db.TermFeedbacks.CountAsync(x => x.AcademicTermId == f.Term && x.Status == "PENDING")).Should().Be(8);
        });
        var again = (await Body(await f.Admin.PostAsync($"/api/admin/terms/{f.Code}/archive-students", null))).GetProperty("data");
        again.GetProperty("archivedStudents").GetInt64().Should().Be(0); again.GetProperty("alreadyInactiveStudents").GetInt64().Should().Be(3);
    }

    [Fact]
    public async Task ConcurrentCloseCreatesOneSnapshotAndConcurrentCreationKeepsOneOpen()
    {
        using var f = await Seed();
        var responses = await Task.WhenAll(Close(f), Close(f));
        var first = (await Body(responses[0])).GetProperty("data"); var second = (await Body(responses[1])).GetProperty("data");
        first.GetProperty("closedAt").GetDateTime().Should().BeCloseTo(second.GetProperty("closedAt").GetDateTime(), TimeSpan.FromMicroseconds(1));
        await WithDb(async db => (await db.TermFeedbacks.CountAsync(x => x.AcademicTermId == f.Term)).Should().Be(8));
        var create = await Task.WhenAll(f.Admin.PostAsJsonAsync("/api/admin/terms", new { code = "N" + Guid.NewGuid().ToString("N")[..20] }),
            f.Admin.PostAsJsonAsync("/api/admin/terms", new { code = "N" + Guid.NewGuid().ToString("N")[..20] }));
        create.Select(x => x.StatusCode).Should().BeEquivalentTo([HttpStatusCode.OK, HttpStatusCode.Conflict]);
        await WithDb(async db => (await db.AcademicTerms.CountAsync(x => x.Status == "OPEN")).Should().Be(1));
    }

    [Fact]
    public async Task CloseFailureRollsBackTermPendingCancellationAndSnapshot()
    {
        using var f = await Seed(); var id = f.Term.ToString(System.Globalization.CultureInfo.InvariantCulture);
        await WithDb(db => db.Database.ExecuteSqlRawAsync($"""
            CREATE FUNCTION parity_fail_term_{id}() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'fixture failure'; END; $$;
            CREATE TRIGGER parity_fail_term_{id} BEFORE INSERT ON term_feedbacks FOR EACH ROW
            WHEN (NEW.academic_term_id = {id}) EXECUTE FUNCTION parity_fail_term_{id}();
            """));
        try
        {
            await Body(await Close(f), HttpStatusCode.Conflict);
            await WithDb(async db => {
                var term = await db.AcademicTerms.SingleAsync(x => x.Id == f.Term); term.Status.Should().Be("OPEN"); term.ClosedAt.Should().BeNull();
                (await db.GroupInvitations.SingleAsync(x => x.Id == f.Invitation)).Status.Should().Be("PENDING");
                (await db.GroupJoinRequests.SingleAsync(x => x.Id == f.Request)).Status.Should().Be("PENDING");
                (await db.TermFeedbacks.AnyAsync(x => x.AcademicTermId == f.Term)).Should().BeFalse();
                (await db.Notifications.AnyAsync(x => x.EntityType == "AcademicTerm" && x.EntityId == id)).Should().BeFalse();
            });
        }
        finally { await WithDb(db => db.Database.ExecuteSqlRawAsync($"DROP TRIGGER parity_fail_term_{id} ON term_feedbacks; DROP FUNCTION parity_fail_term_{id}();")); }
    }

    [Fact]
    public async Task PermissionsValidationPagingAndOpenArchiveMatchJava()
    {
        using var f = await Seed();
        await Error(await f.Admin.PostAsync($"/api/admin/terms/{f.Code}/archive-students", null), HttpStatusCode.BadRequest, "Academic term must be closed before students can be archived");
        foreach (var code in new[] { "", new string('x', 31) })
        {
            var invalid = await Body(await f.Admin.PostAsJsonAsync("/api/admin/terms", new { code }), HttpStatusCode.BadRequest);
            invalid.GetProperty("data").GetProperty("code").GetString().Should().Be(code.Length == 0 ? "Term code is required" : "Term code must be at most 30 characters");
        }
        await Error(await f.Admin.GetAsync("/api/admin/terms?page=-1"), HttpStatusCode.BadRequest, "Page index must be zero or greater");
        await Error(await f.Admin.GetAsync("/api/admin/terms?size=101"), HttpStatusCode.BadRequest, "Page size must be between 1 and 100");
        await Body(await f.Student.GetAsync("/api/admin/terms"), HttpStatusCode.Forbidden);
        await Body(await f.Student.PostAsJsonAsync("/api/admin/terms", new { code = "T2026" }), HttpStatusCode.Forbidden);
        await Body(await f.Student.PatchAsync($"/api/admin/terms/{f.Code}/close", null), HttpStatusCode.Forbidden);
        await Body(await f.Student.PostAsync($"/api/admin/terms/{f.Code}/archive-students", null), HttpStatusCode.Forbidden);
        await Body(await f.Student.DeleteAsync($"/api/admin/terms/{f.Code}"), HttpStatusCode.Forbidden);
        using var anonymous = factory.CreateClient(); await Body(await anonymous.GetAsync("/api/admin/terms"), HttpStatusCode.Unauthorized);
        var data = (await Body(await f.Admin.GetAsync("/api/admin/terms?size=1"))).GetProperty("data");
        data.GetProperty("content")[0].GetProperty("code").GetString().Should().Be(f.Code);
    }

    [Fact]
    public async Task ExistingFeedbackIsPreservedAndInactiveRecipientsDoNotReceiveNotifications()
    {
        using var f = await Seed();
        await WithDb(async db => {
            var group = await db.StudentGroups.SingleAsync(x => x.Id == f.Group);
            db.TermFeedbacks.Add(new TermFeedback { AcademicTermId = f.Term, GroupId = f.Group, StudentId = f.Students[0].Id,
                TargetType = "MENTOR", MentorId = group.MentorId, Status = "SUBMITTED", Rating = 5, Comment = "Preserve", SubmittedAt = DateTime.UtcNow });
            (await db.Accounts.SingleAsync(x => x.Id == f.Students[1].AccountId)).Status = "INACTIVE";
            await db.SaveChangesAsync();
        });
        var data = (await Body(await Close(f))).GetProperty("data");
        data.GetProperty("totalExpectedFeedbacks").GetInt64().Should().Be(8); data.GetProperty("totalSubmittedFeedbacks").GetInt64().Should().Be(1);
        await WithDb(async db => {
            (await db.TermFeedbacks.SingleAsync(x => x.AcademicTermId == f.Term && x.StudentId == f.Students[0].Id && x.TargetType == "MENTOR")).Comment.Should().Be("Preserve");
            var recipients = await db.Notifications.Where(x => x.EntityType == "AcademicTerm" && x.EntityId == f.Term.ToString()).Select(x => x.RecipientId).ToListAsync();
            recipients.Should().HaveCount(3).And.NotContain(f.Students[1].AccountId);
        });
    }

    [Fact]
    public async Task ArchiveFailureRestoresGroupStudentAccountAndRevokedTokens()
    {
        using var f = await Seed(); await Body(await Close(f));
        var studentId = f.Students[0].Id.ToString(System.Globalization.CultureInfo.InvariantCulture);
        await WithDb(async db => {
            db.RefreshTokens.Add(new RefreshToken { AccountId = f.Students[0].AccountId, TokenHash = Guid.NewGuid().ToString("N"), ExpiresAt = DateTime.UtcNow.AddDays(1), CreatedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();
            await db.Database.ExecuteSqlRawAsync($"""
                CREATE FUNCTION parity_fail_archive_{studentId}() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'fixture failure'; END; $$;
                CREATE TRIGGER parity_fail_archive_{studentId} BEFORE UPDATE ON students FOR EACH ROW
                WHEN (NEW.id = {studentId} AND NEW.status = 'INACTIVE') EXECUTE FUNCTION parity_fail_archive_{studentId}();
                """);
        });
        try
        {
            await Body(await f.Admin.PostAsync($"/api/admin/terms/{f.Code}/archive-students", null), HttpStatusCode.Conflict);
            await WithDb(async db => {
                (await db.StudentGroups.SingleAsync(x => x.Id == f.Group)).Status.Should().Be("ACTIVE");
                var s = await db.Students.Include(x => x.Account).SingleAsync(x => x.Id == f.Students[0].Id);
                s.Status.Should().Be("ACTIVE"); s.Account.Status.Should().Be("ACTIVE");
                (await db.RefreshTokens.AnyAsync(x => x.AccountId == s.AccountId)).Should().BeTrue();
            });
        }
        finally { await WithDb(db => db.Database.ExecuteSqlRawAsync($"DROP TRIGGER parity_fail_archive_{studentId} ON students; DROP FUNCTION parity_fail_archive_{studentId}();")); }
    }

    [Fact]
    public async Task ArchiveWithoutStudentsStillInactivatesGroups()
    {
        using var f = await Seed();
        var code = "E" + Guid.NewGuid().ToString("N")[..20].ToUpperInvariant();
        await WithDb(async db => {
            db.AcademicTerms.Add(new AcademicTerm { Code = code, Status = "CLOSED" }); await db.SaveChangesAsync();
            db.StudentGroups.Add(new StudentGroup { Term = code, CourseCode = "PRN232", GroupNo = "1", Name = code, Status = "ACTIVE" }); await db.SaveChangesAsync();
        });
        var data = (await Body(await f.Admin.PostAsync($"/api/admin/terms/{code}/archive-students", null))).GetProperty("data");
        data.GetProperty("archivedStudents").GetInt64().Should().Be(0); data.GetProperty("alreadyInactiveStudents").GetInt64().Should().Be(0);
        await WithDb(async db => (await db.StudentGroups.SingleAsync(x => x.Term == code)).Status.Should().Be("INACTIVE"));
    }
}
