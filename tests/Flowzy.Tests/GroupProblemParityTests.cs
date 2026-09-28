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

public sealed class GroupProblemParityTests(FlowzyApiFactory factory) : IClassFixture<FlowzyApiFactory>
{
    private sealed record Fixture(long Group, long Leader, string Domain, long DomainId, string Term,
        HttpClient LeaderClient, HttpClient Member, HttpClient Outsider, HttpClient Instructor, HttpClient Admin) : IDisposable
    {
        public void Dispose() { LeaderClient.Dispose(); Member.Dispose(); Outsider.Dispose(); Instructor.Dispose(); Admin.Dispose(); }
    }
    private async Task<Fixture> Seed(bool closed = false)
    {
        using var bootstrap = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FlowzyDbContext>();
        var key = Guid.NewGuid().ToString("N");
        Account Account(string role, int i) => new() { Email = $"{i}.{key}@local.test", Role = role, Status = "ACTIVE", PasswordHash = "unused" };
        var students = Enumerable.Range(0, 3).Select(i => new Student { StudentCode = $"{i}.{key}", FullName = $"Student {i}", Status = "ACTIVE", Account = Account("STUDENT", i) }).ToArray();
        var instructor = new Instructor { InstructorCode = key, FullName = "Instructor", Status = "ACTIVE", Account = Account("INSTRUCTOR", 3) };
        var admin = Account("ADMIN", 4);
        var term = closed ? new AcademicTerm { Code = "C" + key[..20], Status = "CLOSED" }
            : await db.AcademicTerms.FirstOrDefaultAsync(x => x.Status == "OPEN") ?? new AcademicTerm { Code = "SU26", Status = "OPEN" };
        if (term.Id == 0) db.AcademicTerms.Add(term);
        var domain = new ProblemDomain { Code = key, Name = "Domain", Status = "ACTIVE" };
        db.Students.AddRange(students); db.Instructors.Add(instructor); db.Accounts.Add(admin); db.ProblemDomains.Add(domain);
        await db.SaveChangesAsync();
        var group = new StudentGroup { Term = term.Code, CourseCode = "C" + key[..20], GroupNo = "1", Name = "Proposal " + key, Status = "ACTIVE", InstructorId = instructor.Id };
        db.StudentGroups.Add(group); await db.SaveChangesAsync();
        db.StudentGroupMembers.AddRange(new StudentGroupMember { GroupId = group.Id, StudentId = students[0].Id, MemberRole = "LEADER" },
            new StudentGroupMember { GroupId = group.Id, StudentId = students[1].Id, MemberRole = "MEMBER" });
        await db.SaveChangesAsync(); group.LeaderStudentId = students[0].Id; await db.SaveChangesAsync();
        var jwt = scope.ServiceProvider.GetRequiredService<IJwtService>();
        HttpClient Client(Account a) { var c = factory.CreateClient(); c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt.GenerateAccessToken(a)); return c; }
        return new(group.Id, students[0].Id, key, domain.Id, term.Code, Client(students[0].Account), Client(students[1].Account), Client(students[2].Account), Client(instructor.Account), Client(admin));
    }
    private async Task WithDb(Func<FlowzyDbContext, Task> action)
    {
        await using var scope = factory.Services.CreateAsyncScope(); await action(scope.ServiceProvider.GetRequiredService<FlowzyDbContext>());
    }
    private static string Url(Fixture f, string suffix) => $"/api/groups/{f.Group}/problems/{suffix}";
    private static object Proposal(Fixture f, string title = "  Original title  ") => new { title, statement = "  Original statement\n", strategicTheme = " theme ", researchArea = " area ", difficultyLevel = "INTERMEDIATE", expectedOutput = " output ", domainCode = f.Domain };
    private static async Task<JsonElement> Body(HttpResponseMessage response, HttpStatusCode status = HttpStatusCode.OK)
    {
        var raw = await response.Content.ReadAsStringAsync(); response.StatusCode.Should().Be(status, raw);
        using var doc = JsonDocument.Parse(raw); return doc.RootElement.Clone();
    }
    private static async Task Error(HttpResponseMessage response, HttpStatusCode status, string message) =>
        (await Body(response, status)).GetProperty("message").GetString().Should().Be(message);
    private async Task<long> Problem(Fixture f, string source = "OFFICIAL", string status = "ACTIVE", bool owned = false)
    {
        long id = 0;
        await WithDb(async db => {
            var p = new Problem { Title = "Problem", Statement = "Statement", DifficultyLevel = "BEGINNER", SourceType = source, Status = status,
                DomainId = f.DomainId, ProposedByGroupId = owned ? f.Group : null, ProposedByStudentId = owned ? f.Leader : null };
            db.Problems.Add(p); await db.SaveChangesAsync(); id = p.Id;
        }); return id;
    }

    [Fact]
    public async Task ProposeUpdateListDeleteKeepJavaCodeRawTextAndSelection()
    {
        using var f = await Seed();
        var created = (await Body(await f.LeaderClient.PostAsJsonAsync(Url(f, "propose"), Proposal(f)))).GetProperty("data");
        var id = created.GetProperty("id").GetInt64();
        created.GetProperty("code").GetString().Should().MatchRegex($"^SP-{f.Group}-[0-9a-f]{{8}}$");
        created.GetProperty("title").GetString().Should().Be("  Original title  ");
        created.GetProperty("statement").GetString().Should().Be("  Original statement\n");
        created.GetProperty("status").GetString().Should().Be("PENDING_REVIEW");
        created.GetProperty("sourceType").GetString().Should().Be("SELF_PROPOSED");
        created.GetProperty("proposedByStudent").GetProperty("id").GetInt64().Should().Be(f.Leader);
        var updated = (await Body(await f.LeaderClient.PutAsJsonAsync(Url(f, $"proposals/{id}"), Proposal(f, " Updated ")))).GetProperty("data");
        updated.GetProperty("code").GetString().Should().Be(created.GetProperty("code").GetString());
        updated.GetProperty("title").GetString().Should().Be(" Updated ");
        var listed = (await Body(await f.Member.GetAsync(Url(f, "proposals")))).GetProperty("data");
        listed.EnumerateArray().Should().ContainSingle(x => x.GetProperty("id").GetInt64() == id);
        await WithDb(async db => (await db.StudentGroups.SingleAsync(x => x.Id == f.Group)).SelectedProblemId.Should().Be(id));
        await Body(await f.LeaderClient.DeleteAsync(Url(f, $"proposals/{id}")));
        await WithDb(async db => {
            (await db.Problems.AnyAsync(x => x.Id == id)).Should().BeFalse();
            (await db.StudentGroups.SingleAsync(x => x.Id == f.Group)).SelectedProblemId.Should().BeNull();
        });
    }

    [Fact]
    public async Task SelectRequiresActiveOfficialAndClearIsIdempotentEvenWhenMembershipLocked()
    {
        using var f = await Seed();
        await WithDb(async db => { (await db.StudentGroups.SingleAsync(x => x.Id == f.Group)).IsLocked = true; await db.SaveChangesAsync(); });
        await Error(await f.LeaderClient.PostAsJsonAsync(Url(f, "select"), new { problemId = long.MaxValue }), HttpStatusCode.BadRequest, $"Problem not found with id: {long.MaxValue}");
        var inactive = await Problem(f, status: "INACTIVE");
        await Error(await f.LeaderClient.PostAsJsonAsync(Url(f, "select"), new { problemId = inactive }), HttpStatusCode.BadRequest, "Selected problem must be ACTIVE");
        var self = await Problem(f, "SELF_PROPOSED", owned: true);
        await Error(await f.LeaderClient.PostAsJsonAsync(Url(f, "select"), new { problemId = self }), HttpStatusCode.BadRequest, "Selected problem must be an official problem");
        var official = await Problem(f);
        var selected = (await Body(await f.LeaderClient.PostAsJsonAsync(Url(f, "select"), new { problemId = official }))).GetProperty("data");
        selected.GetProperty("selectedProblem").GetProperty("id").GetInt64().Should().Be(official);
        await Body(await f.LeaderClient.DeleteAsync(Url(f, "select"))); await Body(await f.LeaderClient.DeleteAsync(Url(f, "select")));
        var missing = await Body(await f.LeaderClient.PostAsJsonAsync(Url(f, "select"), new { }), HttpStatusCode.BadRequest);
        missing.GetProperty("data").GetProperty("problemId").GetString().Should().Be("Problem ID is required");
    }

    [Fact]
    public async Task OnlyStudentMembersReadAndOnlyLeaderWrites()
    {
        using var f = await Seed();
        await Error(await f.Member.PostAsJsonAsync(Url(f, "propose"), Proposal(f)), HttpStatusCode.Forbidden, "Access denied: only the group leader can perform this action");
        await Error(await f.Outsider.GetAsync(Url(f, "proposals")), HttpStatusCode.Forbidden, "Access denied: you do not belong to this group");
        await Error(await f.Outsider.PostAsJsonAsync(Url(f, "propose"), Proposal(f)), HttpStatusCode.Forbidden, "Access denied: you do not belong to this group");
        await Body(await f.Instructor.GetAsync(Url(f, "proposals")), HttpStatusCode.Forbidden);
        await Body(await f.Admin.GetAsync(Url(f, "proposals")), HttpStatusCode.Forbidden);
        using var anonymous = factory.CreateClient(); await Body(await anonymous.GetAsync(Url(f, "proposals")), HttpStatusCode.Unauthorized);
        await Error(await f.Member.GetAsync($"/api/groups/{long.MaxValue}/problems/proposals"), HttpStatusCode.Forbidden, $"Group not found with id: {long.MaxValue}");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UpdateAndDeleteDistinguishOwnershipSourceAndReviewedStatus(bool deleting)
    {
        using var f = await Seed();
        Task<HttpResponseMessage> Send(long id) => deleting ? f.LeaderClient.DeleteAsync(Url(f, $"proposals/{id}")) : f.LeaderClient.PutAsJsonAsync(Url(f, $"proposals/{id}"), Proposal(f));
        var verb = deleting ? "deleted" : "edited";
        await Error(await Send(long.MaxValue), HttpStatusCode.NotFound, $"Problem not found with id: {long.MaxValue}");
        await Error(await Send(await Problem(f)), HttpStatusCode.NotFound, "Proposal not found in this group");
        await Error(await Send(await Problem(f, owned: true)), HttpStatusCode.BadRequest, $"Only self-proposed problems can be {verb} by the group");
        var reviewed = await Problem(f, "SELF_PROPOSED", "ACTIVE", true);
        await Error(await Send(reviewed), HttpStatusCode.Conflict, $"Only pending proposals can be {verb} before review");
        await WithDb(async db => (await db.Problems.SingleAsync(x => x.Id == reviewed)).Title.Should().Be("Problem"));
    }

    [Fact]
    public async Task ClosedTermRejectsWritesButAllowsMemberReads()
    {
        using var f = await Seed(true);
        await Error(await f.LeaderClient.PostAsJsonAsync(Url(f, "propose"), Proposal(f)), HttpStatusCode.Conflict, $"Academic term {f.Term} has ended; this group is read-only for students");
        await Body(await f.Member.GetAsync(Url(f, "proposals")));
    }

    [Fact]
    public async Task DtoAndDomainValidationDoNotLeaveProposals()
    {
        using var f = await Seed();
        var tooLong = await Body(await f.LeaderClient.PostAsJsonAsync(Url(f, "propose"), Proposal(f, new string('x', 256))), HttpStatusCode.BadRequest);
        tooLong.GetProperty("data").GetProperty("title").GetString().Should().Be("Title cannot exceed 255 characters");
        await Error(await f.LeaderClient.PostAsJsonAsync(Url(f, "propose"), new { title = "Title", statement = "Statement", difficultyLevel = "intermediate", domainCode = f.Domain }), HttpStatusCode.BadRequest, "Malformed JSON request");
        await WithDb(async db => { (await db.ProblemDomains.SingleAsync(x => x.Id == f.DomainId)).Status = "INACTIVE"; await db.SaveChangesAsync(); });
        await Error(await f.LeaderClient.PostAsJsonAsync(Url(f, "propose"), Proposal(f)), HttpStatusCode.BadRequest, "Problem domain must be ACTIVE");
        await WithDb(async db => (await db.Problems.AnyAsync(x => x.ProposedByGroupId == f.Group)).Should().BeFalse());
    }

    [Fact]
    public async Task FailureSelectingNewProposalRollsBackInsertedProblem()
    {
        using var f = await Seed();
        // Fault injection lives only in this Testcontainers database, never app migrations.
        var suffix = f.Group.ToString(System.Globalization.CultureInfo.InvariantCulture);
        await WithDb(db => db.Database.ExecuteSqlRawAsync($"""
            CREATE FUNCTION parity_reject_selection_{suffix}() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN RAISE EXCEPTION 'test selection failure'; END; $$;
            CREATE TRIGGER parity_reject_selection_{suffix} BEFORE UPDATE OF selected_problem_id ON student_groups
            FOR EACH ROW WHEN (NEW.id = {suffix} AND NEW.selected_problem_id IS NOT NULL)
            EXECUTE FUNCTION parity_reject_selection_{suffix}();
            """));
        try
        {
            await Body(await f.LeaderClient.PostAsJsonAsync(Url(f, "propose"), Proposal(f)), HttpStatusCode.Conflict);
            await WithDb(async db => {
                (await db.Problems.AnyAsync(x => x.ProposedByGroupId == f.Group)).Should().BeFalse();
                (await db.StudentGroups.SingleAsync(x => x.Id == f.Group)).SelectedProblemId.Should().BeNull();
            });
        }
        finally
        {
            await WithDb(db => db.Database.ExecuteSqlRawAsync($"DROP TRIGGER parity_reject_selection_{suffix} ON student_groups; DROP FUNCTION parity_reject_selection_{suffix}();"));
        }
    }
}
