using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Flowzy.Repository.Data;
using Flowzy.Repository.Entities;
using Flowzy.Service.Authentication;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Flowzy.Tests;

public sealed class InstructorBoardParityTests(FlowzyApiFactory factory) : IClassFixture<FlowzyApiFactory>
{
    private const string Board = "/api/instructor/groups/board";
    private static string Claim(long id) => $"/api/instructor/groups/{id}/claim";
    private sealed record Fixture(long[] Groups, long InstructorId, long AccountId, string Term, string Course, string OtherCourse,
        string MemberName, string MemberCode, string MemberEmail, string MemberClass, HttpClient Mine, HttpClient Other, HttpClient Student, HttpClient Admin) : IDisposable
    { public void Dispose() { Mine.Dispose(); Other.Dispose(); Student.Dispose(); Admin.Dispose(); } }
    private async Task<Fixture> Seed()
    {
        using var bootstrap = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<FlowzyDbContext>();
        var key = Guid.NewGuid().ToString("N")[..12];
        Account Account(string role, string suffix) => new() { Email = key + suffix + "@board.test", Role = role, Status = "ACTIVE", PasswordHash = "unused" };
        var instructors = Enumerable.Range(0, 2).Select(i => new Instructor { InstructorCode = key + i, FullName = "Instructor " + i, Status = "ACTIVE", Account = Account("INSTRUCTOR", "i" + i) }).ToArray();
        var students = Enumerable.Range(0, 8).Select(i => new Student { StudentCode = key + "S" + i, FullName = "Student " + key + i, ClassName = "Class" + key + i, Status = "ACTIVE", Account = Account("STUDENT", "s" + i) }).ToArray();
        var admin = Account("ADMIN", "a");
        var term = await db.AcademicTerms.FirstOrDefaultAsync(x => x.Status == "OPEN") ?? new AcademicTerm { Code = "B" + key, Status = "OPEN" };
        if (term.Id == 0) db.AcademicTerms.Add(term);
        var closed = new AcademicTerm { Code = "C" + key, Status = "CLOSED" }; db.AcademicTerms.Add(closed);
        db.Instructors.AddRange(instructors); db.Students.AddRange(students); db.Accounts.Add(admin); await db.SaveChangesAsync();
        var course = "A" + key; var courseB = "B" + key; var timestamp = DateTime.UtcNow.Date.AddDays(-1);
        var groups = Enumerable.Range(0, 7).Select(i => new StudentGroup { Term = i == 6 ? closed.Code : term.Code,
            CourseCode = i == 3 ? courseB : course, GroupNo = i.ToString(), Name = "Group " + key + i, ProjectName = i == 0 ? "Project" + key : null,
            Status = i == 4 ? "INACTIVE" : "ACTIVE", InstructorId = i == 1 ? instructors[0].Id : i == 2 ? instructors[1].Id : null,
            CreatedAt = timestamp, UpdatedAt = timestamp }).ToArray();
        db.StudentGroups.AddRange(groups); await db.SaveChangesAsync();
        for (var i = 0; i < groups.Length; i++)
            if (i != 5) db.StudentGroupMembers.Add(new StudentGroupMember { GroupId = groups[i].Id, StudentId = students[i].Id, MemberRole = "LEADER", JoinedAt = timestamp });
        db.StudentGroupMembers.Add(new StudentGroupMember { GroupId = groups[0].Id, StudentId = students[7].Id, MemberRole = "MEMBER", JoinedAt = timestamp.AddDays(-1) });
        await db.SaveChangesAsync();
        var jwt = scope.ServiceProvider.GetRequiredService<IJwtService>();
        HttpClient Client(Account a) { var client = factory.CreateClient(); client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt.GenerateAccessToken(a)); return client; }
        return new(groups.Select(x => x.Id).ToArray(), instructors[0].Id, instructors[0].AccountId, term.Code, course, courseB,
            students[7].FullName, students[7].StudentCode, students[7].Account.Email, students[7].ClassName!, Client(instructors[0].Account), Client(instructors[1].Account), Client(students[0].Account), Client(admin));
    }
    private async Task WithDb(Func<FlowzyDbContext, Task> action)
    { await using var scope = factory.Services.CreateAsyncScope(); await action(scope.ServiceProvider.GetRequiredService<FlowzyDbContext>()); }
    private static string Query(Fixture f) => Board + "?term=" + f.Term + "&courseCode=" + f.Course;
    private static async Task<JsonElement> Body(HttpResponseMessage response, HttpStatusCode expected = HttpStatusCode.OK)
    {
        var raw = await response.Content.ReadAsStringAsync(); response.StatusCode.Should().Be(expected, raw);
        using var document = JsonDocument.Parse(raw); return document.RootElement.Clone();
    }
    private static async Task Error(HttpResponseMessage response, HttpStatusCode expected, string message) =>
        (await Body(response, expected)).GetProperty("message").GetString().Should().Be(message);
    private static long[] Ids(JsonElement data) => data.GetProperty("groups").GetProperty("content").EnumerateArray().Select(x => x.GetProperty("id").GetInt64()).ToArray();

    [Fact]
    public async Task EligibilityCountsCourseScopePagingAndMemberOrderingMatchJava()
    {
        using var f = await Seed();
        var data = (await Body(await f.Mine.GetAsync(Query(f)))).GetProperty("data");
        Ids(data).Should().Equal(f.Groups[2], f.Groups[1], f.Groups[0]);
        var summary = data.GetProperty("summary"); summary.GetProperty("totalGroups").GetInt64().Should().Be(3);
        foreach (var field in new[] { "availableGroups", "myGroups", "otherGroups" }) summary.GetProperty(field).GetInt64().Should().Be(1);
        var courses = data.GetProperty("courses").EnumerateArray().ToArray();
        courses.Single(x => x.GetProperty("courseCode").GetString() == f.OtherCourse).GetProperty("totalGroups").GetInt64().Should().Be(1);
        var members = data.GetProperty("groups").GetProperty("content")[2].GetProperty("members");
        members[0].GetProperty("role").GetString().Should().Be("LEADER"); members[1].GetProperty("role").GetString().Should().Be("MEMBER");
        var page = (await Body(await f.Mine.GetAsync(Query(f) + "&size=1&page=1"))).GetProperty("data");
        Ids(page).Should().Equal(f.Groups[1]); page.GetProperty("groups").GetProperty("totalElements").GetInt64().Should().Be(3);
        var normalized = (await Body(await f.Mine.GetAsync(Board + "?term=" + Uri.EscapeDataString(" " + f.Term.ToLowerInvariant() + " ") + "&courseCode=" + Uri.EscapeDataString(" " + f.Course.ToLowerInvariant() + " ")))).GetProperty("data");
        Ids(normalized).Should().Equal(Ids(data));
    }

    [Fact]
    public async Task SearchesMembersAndKeepsSummaryIndependentOfSearchAndAssignment()
    {
        using var f = await Seed();
        foreach (var field in new[] { f.MemberName, f.MemberCode, f.MemberEmail, f.MemberClass })
        {
            var data = (await Body(await f.Mine.GetAsync(Query(f) + "&search=" + Uri.EscapeDataString(" " + field.ToUpperInvariant() + " ")))).GetProperty("data");
            Ids(data).Should().Equal(f.Groups[0]); data.GetProperty("summary").GetProperty("totalGroups").GetInt64().Should().Be(3);
        }
        var none = (await Body(await f.Mine.GetAsync(Query(f) + "&assignment=MINE&search=does-not-exist"))).GetProperty("data");
        Ids(none).Should().BeEmpty(); none.GetProperty("summary").GetProperty("myGroups").GetInt64().Should().Be(1);
        var wildcard = (await Body(await f.Mine.GetAsync(Query(f) + "&search=%25"))).GetProperty("data"); Ids(wildcard).Should().HaveCount(3);
        foreach (var (assignment, index) in new[] { ("AVAILABLE", 0), ("MINE", 1), ("OTHER", 2) })
            Ids((await Body(await f.Mine.GetAsync(Query(f) + "&assignment=" + assignment))).GetProperty("data")).Should().Equal(f.Groups[index]);
    }

    [Theory]
    [InlineData("assignment=mine", "Invalid parameter format: assignment")]
    [InlineData("assignment=INVALID", "Invalid parameter format: assignment")]
    [InlineData("page=-1", "Page index must be zero or greater")]
    [InlineData("size=0", "Page size must be between 1 and 100")]
    [InlineData("size=101", "Page size must be between 1 and 100")]
    public async Task RejectsInvalidQuery(string query, string message)
    { using var f = await Seed(); await Error(await f.Mine.GetAsync(Query(f) + "&" + query), HttpStatusCode.BadRequest, message); }

    [Fact]
    public async Task ClaimsAreIdempotentAndCompetingInstructorsCannotOverwrite()
    {
        using var f = await Seed();
        var responses = await Task.WhenAll(f.Mine.PostAsync(Claim(f.Groups[0]), null), f.Other.PostAsync(Claim(f.Groups[0]), null));
        responses.Select(x => x.StatusCode).Should().BeEquivalentTo([HttpStatusCode.OK, HttpStatusCode.Conflict]);
        var winner = responses[0].IsSuccessStatusCode ? f.Mine : f.Other; var loser = winner == f.Mine ? f.Other : f.Mine;
        DateTime updated = default; long? owner = null;
        await WithDb(async db => { var group = await db.StudentGroups.FindAsync(f.Groups[0]); updated = group!.UpdatedAt; owner = group.InstructorId; });
        await Body(await winner.PostAsync(Claim(f.Groups[0]), null));
        await Error(await loser.PostAsync(Claim(f.Groups[0]), null), HttpStatusCode.Conflict, "Group has already been claimed by another instructor");
        await WithDb(async db => { var group = await db.StudentGroups.FindAsync(f.Groups[0]); group!.UpdatedAt.Should().Be(updated); group.InstructorId.Should().Be(owner); });
    }

    [Fact]
    public async Task ClaimGuardsKeepJavaOrderAndAllowLockedGroups()
    {
        using var f = await Seed();
        await Error(await f.Mine.PostAsync(Claim(long.MaxValue), null), HttpStatusCode.NotFound, "Group not found with id: " + long.MaxValue);
        await Error(await f.Mine.PostAsync(Claim(f.Groups[4]), null), HttpStatusCode.Conflict, "Only active groups can be claimed");
        await Error(await f.Mine.PostAsync(Claim(f.Groups[5]), null), HttpStatusCode.Conflict, "Groups without members cannot be claimed");
        await Error(await f.Mine.PostAsync(Claim(f.Groups[6]), null), HttpStatusCode.Conflict, "Group academic term is closed");
        await WithDb(async db => { (await db.StudentGroups.FindAsync(f.Groups[0]))!.IsLocked = true; await db.SaveChangesAsync(); });
        var claimed = (await Body(await f.Mine.PostAsync(Claim(f.Groups[0]), null))).GetProperty("data");
        claimed.GetProperty("assignmentState").GetString().Should().Be("MINE"); claimed.GetProperty("isLock").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task AuthorizationAndInactiveProfileGuardBothEndpoints()
    {
        using var f = await Seed(); using var anonymous = factory.CreateClient();
        await Body(await anonymous.GetAsync(Query(f)), HttpStatusCode.Unauthorized);
        foreach (var client in new[] { f.Admin, f.Student })
        { await Body(await client.GetAsync(Query(f)), HttpStatusCode.Forbidden); await Body(await client.PostAsync(Claim(f.Groups[0]), null), HttpStatusCode.Forbidden); }
        await WithDb(async db => { (await db.Instructors.FindAsync(f.InstructorId))!.Status = "INACTIVE"; await db.SaveChangesAsync(); });
        await Error(await f.Mine.GetAsync(Query(f)), HttpStatusCode.Forbidden, "Inactive instructors cannot claim groups");
        await Error(await f.Mine.PostAsync(Claim(long.MaxValue), null), HttpStatusCode.Forbidden, "Inactive instructors cannot claim groups");
    }
}
