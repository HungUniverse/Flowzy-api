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

public sealed class LegacySubmissionParityTests(FlowzyApiFactory factory) : IClassFixture<FlowzyApiFactory>
{
    private sealed record Fixture(long Group, long OtherGroup, long Milestone, long Submission, long OtherSubmission,
        long OldInstructor, long NewInstructor, HttpClient Old, HttpClient Current, HttpClient Member, HttpClient Outsider, HttpClient Mentor, HttpClient Admin) : IDisposable
    {
        public void Dispose() { Old.Dispose(); Current.Dispose(); Member.Dispose(); Outsider.Dispose(); Mentor.Dispose(); Admin.Dispose(); }
    }

    private async Task<Fixture> Seed()
    {
        using var bootstrap = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<FlowzyDbContext>();
        var key = Guid.NewGuid().ToString("N");
        Account Account(string role, int i) => new() { Email = $"{i}.{key}@local.test", Role = role, Status = "ACTIVE", PasswordHash = "unused" };
        var instructors = Enumerable.Range(0, 2).Select(i => new Instructor { Account = Account("INSTRUCTOR", i), InstructorCode = i + key, FullName = "Instructor", Status = "ACTIVE" }).ToArray();
        var students = Enumerable.Range(2, 2).Select(i => new Student { Account = Account("STUDENT", i), StudentCode = i + key, FullName = "Student", Status = "ACTIVE" }).ToArray();
        var mentor = new Mentor { Account = Account("MENTOR", 4), MentorCode = key, FullName = "Mentor", Status = "ACTIVE" };
        var admin = Account("ADMIN", 5);
        var term = await db.AcademicTerms.FirstOrDefaultAsync(x => x.Status == "OPEN") ?? new AcademicTerm { Code = "SU26", Status = "OPEN" };
        if (term.Id == 0) db.AcademicTerms.Add(term);
        db.Instructors.AddRange(instructors); db.Students.AddRange(students); db.Mentors.Add(mentor); db.Accounts.Add(admin); await db.SaveChangesAsync();
        var course = "C" + key[..20];
        var groups = Enumerable.Range(0, 2).Select(i => new StudentGroup { Term = term.Code, CourseCode = course, GroupNo = (i + 1).ToString(), Name = key + i, Status = "ACTIVE", InstructorId = instructors[0].Id, MentorId = mentor.Id }).ToArray();
        var milestone = new CourseMilestone { Term = term.Code, CourseCode = course, Title = "Historical milestone", InstructorId = instructors[0].Id, Status = "ARCHIVED", Type = "TIMELINE", MaxScore = 20 };
        db.StudentGroups.AddRange(groups); db.CourseMilestones.Add(milestone); await db.SaveChangesAsync();
        db.StudentGroupMembers.Add(new StudentGroupMember { GroupId = groups[0].Id, StudentId = students[0].Id, MemberRole = "MEMBER" });
        var submissions = groups.Select(g => new MilestoneSubmission { GroupId = g.Id, MilestoneId = milestone.Id, SubmittedBy = "Historical student", FileUrl = "https://example.test/file", Comments = "Comment", Status = "GRADED", Version = 7, Late = true }).ToArray();
        db.MilestoneSubmissions.AddRange(submissions); await db.SaveChangesAsync();
        db.MilestoneGrades.Add(new MilestoneGrade { SubmissionId = submissions[0].Id, InstructorId = instructors[0].Id, Score = 8.5m, MaxScore = 10, Feedback = "Saved feedback" });
        // Reassignment changes group visibility, not the historical milestone owner.
        groups[0].InstructorId = instructors[1].Id; await db.SaveChangesAsync();
        var jwt = scope.ServiceProvider.GetRequiredService<IJwtService>();
        HttpClient Client(Account a) { var c = factory.CreateClient(); c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt.GenerateAccessToken(a)); return c; }
        return new(groups[0].Id, groups[1].Id, milestone.Id, submissions[0].Id, submissions[1].Id,
            instructors[0].Id, instructors[1].Id, Client(instructors[0].Account), Client(instructors[1].Account), Client(students[0].Account), Client(students[1].Account), Client(mentor.Account), Client(admin));
    }
    private static async Task<JsonElement> Body(HttpResponseMessage response, HttpStatusCode status = HttpStatusCode.OK)
    {
        var raw = await response.Content.ReadAsStringAsync(); response.StatusCode.Should().Be(status, raw);
        using var doc = JsonDocument.Parse(raw); return doc.RootElement.Clone();
    }
    private static async Task Error(HttpResponseMessage response, HttpStatusCode status, string message) =>
        (await Body(response, status)).GetProperty("message").GetString().Should().Be(message);

    [Fact]
    public async Task ReassignmentTransfersDetailAndGroupReadAccessNotMilestoneOwnership()
    {
        using var f = await Seed();
        await Error(await f.Old.GetAsync($"/api/milestone-submissions/{f.Submission}"), HttpStatusCode.Forbidden, "You do not have permission to view this submission");
        await Error(await f.Old.GetAsync($"/api/milestone-submissions/groups/{f.Group}"), HttpStatusCode.Forbidden, "You do not have permission to view submissions for this group");
        var body = await Body(await f.Current.GetAsync($"/api/milestone-submissions/{f.Submission}")); var data = body.GetProperty("data");
        data.GetProperty("score").GetDecimal().Should().Be(8.5m); data.GetProperty("maxScore").GetDecimal().Should().Be(10m);
        data.GetProperty("version").GetInt64().Should().Be(7); data.GetProperty("late").GetBoolean().Should().BeTrue();
        await Body(await f.Current.GetAsync($"/api/milestone-submissions/groups/{f.Group}"));
        var owned = (await Body(await f.Old.GetAsync($"/api/milestone-submissions/milestones/{f.Milestone}"))).GetProperty("data");
        owned.EnumerateArray().Select(x => x.GetProperty("id").GetInt64()).Should().Equal(f.OtherSubmission);
        await Error(await f.Current.GetAsync($"/api/milestone-submissions/milestones/{f.Milestone}"), HttpStatusCode.Forbidden, "You do not manage this timeline milestone");
    }

    [Fact]
    public async Task AdminAndMemberReadHistoricalDataButMentorAndOutsiderAreDenied()
    {
        using var f = await Seed();
        foreach (var client in new[] { f.Admin, f.Member })
        {
            await Body(await client.GetAsync($"/api/milestone-submissions/{f.Submission}"));
            await Body(await client.GetAsync($"/api/milestone-submissions/groups/{f.Group}"));
            await Error(await client.GetAsync($"/api/milestone-submissions/milestones/{f.Milestone}"), HttpStatusCode.Forbidden, "Only the assigned instructor can view milestone submissions");
        }
        foreach (var client in new[] { f.Outsider, f.Mentor })
        {
            await Error(await client.GetAsync($"/api/milestone-submissions/{f.Submission}"), HttpStatusCode.Forbidden, "You do not have permission to view this submission");
            await Error(await client.GetAsync($"/api/milestone-submissions/groups/{f.Group}"), HttpStatusCode.Forbidden, "You do not have permission to view submissions for this group");
        }
        using var anonymous = factory.CreateClient();
        await Body(await anonymous.GetAsync($"/api/milestone-submissions/{f.Submission}"), HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MissingResourcesUseJavaMessagesBeforeRoleCheckAndUnassignmentRemovesInstructorAccess()
    {
        using var f = await Seed();
        await Error(await f.Mentor.GetAsync($"/api/milestone-submissions/{long.MaxValue}"), HttpStatusCode.NotFound, "Milestone submission not found");
        await Error(await f.Member.GetAsync($"/api/milestone-submissions/groups/{long.MaxValue}"), HttpStatusCode.NotFound, "Student group not found");
        await Error(await f.Admin.GetAsync($"/api/milestone-submissions/milestones/{long.MaxValue}"), HttpStatusCode.NotFound, "Course milestone not found");
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FlowzyDbContext>();
            (await db.StudentGroups.SingleAsync(x => x.Id == f.Group)).InstructorId = null; await db.SaveChangesAsync();
        }
        await Error(await f.Current.GetAsync($"/api/milestone-submissions/{f.Submission}"), HttpStatusCode.Forbidden, "You do not have permission to view this submission");
        await Body(await f.Member.GetAsync($"/api/milestone-submissions/{f.Submission}"));
    }
}
