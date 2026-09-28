using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Headers;
using Flowzy.Repository.Data;
using Flowzy.Repository.Entities;
using Flowzy.Service.Authentication;
using Testcontainers.PostgreSql;
using Xunit;

namespace Flowzy.Tests;

public class FlowzyApiFactory : WebApplicationFactory<global::Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("flowzy_test")
        .WithUsername("flowzy_test")
        .WithPassword("flowzy_test_password")
        .Build();

    public Task InitializeAsync() => postgres.StartAsync();
    protected PostgreSqlContainer DatabaseContainer => postgres;
    async Task IAsyncLifetime.DisposeAsync()
    {
        await DisposeAsync();
        await postgres.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Default", postgres.GetConnectionString());
        builder.UseSetting("Jwt:SecretKey", "dGVzdC1qd3Qtc2VjcmV0LWtleS10aGF0LWlzLWxvbmctZW5vdWdoLWZvci1oczI1Ng==");
        builder.UseSetting("Admin:Email", "admin.integration@local.test");
        builder.UseSetting("Admin:Password", "Integration123");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = postgres.GetConnectionString(),
            ["Jwt:SecretKey"] = "dGVzdC1qd3Qtc2VjcmV0LWtleS10aGF0LWlzLWxvbmctZW5vdWdoLWZvci1oczI1Ng==",
            ["Admin:Email"] = "admin.integration@local.test",
            ["Admin:Password"] = "Integration123"
        }));
    }
}

public sealed class ApiIntegrationTests(FlowzyApiFactory factory) : IClassFixture<FlowzyApiFactory>
{
    [Fact]
    public async Task EmptyPostgresMigratesAndAuthContractWorks()
    {
        using var client = factory.CreateClient();
        var health = await client.GetAsync("/actuator/health");
        health.StatusCode.Should().Be(HttpStatusCode.OK);

        var login = await client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin.integration@local.test",
            password = "Integration123"
        });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await login.Content.ReadAsStringAsync();
        body.Should().Contain("\"code\":200").And.Contain("\"tokenType\":\"Bearer\"");
    }
}

public sealed class MilestoneGradeParityTests(FlowzyApiFactory factory) : IClassFixture<FlowzyApiFactory>
{
    [Fact]
    public async Task AdminGetsJavaCompatibleWeightedAverageAndBulkGrade404()
    {
        using var client = factory.CreateClient();
        long groupId;
        string token;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FlowzyDbContext>();
            var admin = db.Accounts.Single(x => x.Email == "admin.integration@local.test");
            admin.MustChangePassword = false;

            var term = new AcademicTerm { Code = "GRADE-TEST", Status = "OPEN" };
            db.AcademicTerms.Add(term);
            var group = new StudentGroup
            {
                Term = term.Code, CourseCode = "PRN999", GroupNo = "1", Name = "Grade parity",
                Status = "ACTIVE"
            };
            db.StudentGroups.Add(group);
            var active = new CourseMilestone
            {
                Term = term.Code, CourseCode = group.CourseCode, Title = "Active", Type = "TIMELINE",
                Status = "ACTIVE", Weight = 40, MaxScore = 10
            };
            var inactive = new CourseMilestone
            {
                Term = term.Code, CourseCode = group.CourseCode, Title = "Inactive", Type = "TIMELINE",
                Status = "INACTIVE", Weight = 60, MaxScore = 10
            };
            db.CourseMilestones.AddRange(active, inactive);
            await db.SaveChangesAsync();

            var activeSubmission = new MilestoneSubmission
            {
                GroupId = group.Id, MilestoneId = active.Id, Status = "GRADED"
            };
            var inactiveSubmission = new MilestoneSubmission
            {
                GroupId = group.Id, MilestoneId = inactive.Id, Status = "GRADED"
            };
            db.MilestoneSubmissions.AddRange(activeSubmission, inactiveSubmission);
            await db.SaveChangesAsync();
            db.MilestoneGrades.AddRange(
                new MilestoneGrade { SubmissionId = activeSubmission.Id, Score = 8.25m, MaxScore = 10 },
                new MilestoneGrade { SubmissionId = inactiveSubmission.Id, Score = 1m, MaxScore = 10 });
            await db.SaveChangesAsync();

            groupId = group.Id;
            token = scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateAccessToken(admin);
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var average = await client.GetAsync($"/api/student-groups/{groupId}/average-grade");
        average.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await average.Content.ReadAsStringAsync();
        json.Should().Contain("\"message\":\"Average grade calculated successfully\"")
            .And.Contain("\"averageGrade\":8.25")
            .And.Contain("\"average\":8.25");

        var unsupported = await client.PostAsync("/api/milestone-submissions/bulk-grade", null);
        unsupported.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await unsupported.Content.ReadAsStringAsync()).Should().Contain("Bulk grading not supported");
    }
}

public sealed class InstructorProblemParityTests(FlowzyApiFactory factory) : IClassFixture<FlowzyApiFactory>
{
    [Fact]
    public async Task InstructorListsAndRejectsOnlyAssignedPendingProposal()
    {
        using var client = factory.CreateClient();
        long problemId;
        long groupId;
        string token;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FlowzyDbContext>();
            var account = new Account
            {
                Email = "instructor.problem@local.test", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123"),
                Role = "INSTRUCTOR", Status = "ACTIVE", MustChangePassword = false
            };
            var instructor = new Instructor
            {
                Account = account, InstructorCode = "INS-PROBLEM", FullName = "Problem Reviewer", Status = "ACTIVE"
            };
            db.Instructors.Add(instructor);
            db.AcademicTerms.Add(new AcademicTerm { Code = "PROBLEM-TEST", Status = "OPEN" });
            await db.SaveChangesAsync();

            var group = new StudentGroup
            {
                Term = "PROBLEM-TEST", CourseCode = "PRN998", GroupNo = "1", Name = "Proposal group",
                Status = "ACTIVE", InstructorId = instructor.Id
            };
            db.StudentGroups.Add(group);
            await db.SaveChangesAsync();
            var problem = new Problem
            {
                Title = "Pending proposal", Statement = "Proposal statement", DifficultyLevel = "INTERMEDIATE",
                SourceType = "SELF_PROPOSED", Status = "PENDING_REVIEW", ProposedByGroupId = group.Id
            };
            db.Problems.Add(problem);
            await db.SaveChangesAsync();
            group.SelectedProblemId = problem.Id;
            await db.SaveChangesAsync();

            problemId = problem.Id;
            groupId = group.Id;
            token = scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateAccessToken(account);
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var pending = await client.GetAsync("/api/instructor/problems/pending");
        pending.StatusCode.Should().Be(HttpStatusCode.OK);
        (await pending.Content.ReadAsStringAsync()).Should().Contain("Pending proposal");

        var missingComment = await client.PatchAsJsonAsync($"/api/instructor/problems/{problemId}/review",
            new { status = "REJECTED", comment = " " });
        missingComment.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await missingComment.Content.ReadAsStringAsync()).Should().Contain("Comment is required when rejecting a proposal");

        var rejected = await client.PatchAsJsonAsync($"/api/instructor/problems/{problemId}/review",
            new { status = "REJECTED", comment = "Needs revision" });
        rejected.StatusCode.Should().Be(HttpStatusCode.OK);
        var response = await rejected.Content.ReadAsStringAsync();
        response.Should().Contain("\"status\":\"REJECTED\"").And.Contain("Needs revision");

        await using var verifyScope = factory.Services.CreateAsyncScope();
        var verifyDb = verifyScope.ServiceProvider.GetRequiredService<FlowzyDbContext>();
        (await verifyDb.StudentGroups.FindAsync(groupId))!.SelectedProblemId.Should().BeNull();
    }
}

public sealed class StudentDiscoveryParityTests(FlowzyApiFactory factory) : IClassFixture<FlowzyApiFactory>
{
    [Fact]
    public async Task StudentCanOnlyDiscoverActiveStudentsOutsideRequestedCourseGroup()
    {
        using var client = factory.CreateClient();
        long discoverableId;
        long inactiveId;
        string token;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FlowzyDbContext>();
            var callerAccount = new Account
            {
                Email = "student.discovery.caller@local.test", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123"),
                Role = "STUDENT", Status = "ACTIVE", MustChangePassword = false
            };
            var caller = new Student
            {
                Account = callerAccount, StudentCode = "SD-CALLER", FullName = "Discovery Caller", Status = "ACTIVE"
            };
            var discoverable = new Student
            {
                Account = new Account
                {
                    Email = "student.discovery.free@local.test", PasswordHash = "unused", Role = "STUDENT",
                    Status = "ACTIVE", MustChangePassword = false
                },
                StudentCode = "SD-FREE", FullName = "Free Student", Status = "ACTIVE"
            };
            var grouped = new Student
            {
                Account = new Account
                {
                    Email = "student.discovery.grouped@local.test", PasswordHash = "unused", Role = "STUDENT",
                    Status = "ACTIVE", MustChangePassword = false
                },
                StudentCode = "SD-GROUPED", FullName = "Grouped Student", Status = "ACTIVE"
            };
            var inactive = new Student
            {
                Account = new Account
                {
                    Email = "student.discovery.inactive@local.test", PasswordHash = "unused", Role = "STUDENT",
                    Status = "ACTIVE", MustChangePassword = false
                },
                StudentCode = "SD-INACTIVE", FullName = "Inactive Student", Status = "INACTIVE"
            };
            db.Students.AddRange(caller, discoverable, grouped, inactive);
            db.AcademicTerms.Add(new AcademicTerm { Code = "DISCOVERY-TEST", Status = "OPEN" });
            await db.SaveChangesAsync();
            var group = new StudentGroup
            {
                Term = "DISCOVERY-TEST", CourseCode = "PRN997", GroupNo = "1", Name = "Existing group",
                Status = "ACTIVE"
            };
            db.StudentGroups.Add(group);
            await db.SaveChangesAsync();
            db.StudentGroupMembers.Add(new StudentGroupMember
            {
                GroupId = group.Id, StudentId = grouped.Id, MemberRole = "MEMBER"
            });
            await db.SaveChangesAsync();

            discoverableId = discoverable.Id;
            inactiveId = inactive.Id;
            token = scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateAccessToken(callerAccount);
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var missingTerm = await client.GetAsync("/api/students/ungrouped?courseCode=PRN997");
        missingTerm.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await missingTerm.Content.ReadAsStringAsync()).Should().Contain("term is required");

        var result = await client.GetAsync(
            "/api/students/ungrouped?term=DISCOVERY-TEST&courseCode=PRN997&search=Free&page=0&size=20");
        result.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await result.Content.ReadAsStringAsync();
        json.Should().Contain("\"message\":\"Ungrouped students retrieved successfully\"")
            .And.Contain("SD-FREE")
            .And.NotContain("SD-GROUPED")
            .And.Contain("\"page\":0")
            .And.Contain("\"number\":0");

        var byId = await client.GetAsync($"/api/students/{discoverableId}");
        byId.StatusCode.Should().Be(HttpStatusCode.OK);
        (await byId.Content.ReadAsStringAsync()).Should().Contain("Student profile retrieved successfully");

        var inactiveResult = await client.GetAsync($"/api/students/{inactiveId}");
        inactiveResult.StatusCode.Should().Be(HttpStatusCode.NotFound);
        (await inactiveResult.Content.ReadAsStringAsync()).Should().Contain($"Student not found with id: {inactiveId}");
    }
}

public sealed class InstructorSubmissionParityTests(FlowzyApiFactory factory) : IClassFixture<FlowzyApiFactory>
{
    [Fact]
    public async Task InstructorOnlySeesSubmissionsOwnedByBothMilestoneAndGroupWithFilters()
    {
        using var client = factory.CreateClient();
        string token;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FlowzyDbContext>();
            var account = new Account
            {
                Email = "instructor.submissions@local.test", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123"),
                Role = "INSTRUCTOR", Status = "ACTIVE", MustChangePassword = false
            };
            var instructor = new Instructor
            {
                Account = account, InstructorCode = "INS-SUBMISSIONS", FullName = "Submission Instructor",
                Status = "ACTIVE"
            };
            var otherAccount = new Account
            {
                Email = "instructor.submissions.other@local.test", PasswordHash = "unused", Role = "INSTRUCTOR",
                Status = "ACTIVE", MustChangePassword = false
            };
            var other = new Instructor
            {
                Account = otherAccount, InstructorCode = "INS-SUBMISSIONS-OTHER", FullName = "Other Instructor",
                Status = "ACTIVE"
            };
            db.Instructors.AddRange(instructor, other);
            db.AcademicTerms.Add(new AcademicTerm { Code = "SUBMISSION-TEST", Status = "OPEN" });
            await db.SaveChangesAsync();

            var ownedGroup = new StudentGroup
            {
                Term = "SUBMISSION-TEST", CourseCode = "PRN996", GroupNo = "1", Name = "Owned group",
                Status = "ACTIVE", InstructorId = instructor.Id
            };
            var otherGroup = new StudentGroup
            {
                Term = "SUBMISSION-TEST", CourseCode = "PRN996", GroupNo = "2", Name = "Other group",
                Status = "ACTIVE", InstructorId = other.Id
            };
            var ownedMilestone = new CourseMilestone
            {
                Term = "SUBMISSION-TEST", CourseCode = "PRN996", Title = "Owned milestone", Type = "TIMELINE",
                Status = "ACTIVE", MaxScore = 10, InstructorId = instructor.Id
            };
            var otherMilestone = new CourseMilestone
            {
                Term = "SUBMISSION-TEST", CourseCode = "PRN996", Title = "Other milestone", Type = "TIMELINE",
                Status = "ACTIVE", MaxScore = 10, InstructorId = other.Id
            };
            db.StudentGroups.AddRange(ownedGroup, otherGroup);
            db.CourseMilestones.AddRange(ownedMilestone, otherMilestone);
            await db.SaveChangesAsync();

            var visible = new MilestoneSubmission
            {
                GroupId = ownedGroup.Id, MilestoneId = ownedMilestone.Id, SubmittedBy = "SD01",
                FileUrl = "https://example.test/visible", Comments = "visible", Late = true,
                Status = "SUBMITTED", Version = 1
            };
            var wrongGroup = new MilestoneSubmission
            {
                GroupId = otherGroup.Id, MilestoneId = ownedMilestone.Id, SubmittedBy = "SD02",
                FileUrl = "https://example.test/wrong-group", Late = true, Status = "SUBMITTED", Version = 1
            };
            var wrongMilestone = new MilestoneSubmission
            {
                GroupId = ownedGroup.Id, MilestoneId = otherMilestone.Id, SubmittedBy = "SD03",
                FileUrl = "https://example.test/wrong-milestone", Late = true, Status = "SUBMITTED", Version = 1
            };
            db.MilestoneSubmissions.AddRange(visible, wrongGroup, wrongMilestone);
            await db.SaveChangesAsync();
            db.MilestoneGrades.Add(new MilestoneGrade
            {
                SubmissionId = visible.Id, InstructorId = instructor.Id, Score = 8.5m,
                MaxScore = 10, Feedback = "Good"
            });
            await db.SaveChangesAsync();
            token = scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateAccessToken(account);
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var response = await client.GetAsync(
            "/api/instructor/submissions?term=submission-test&courseCode=prn996&status=SUBMITTED&late=true");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await response.Content.ReadAsStringAsync();
        json.Should().Contain("Instructor submissions retrieved successfully")
            .And.Contain("https://example.test/visible")
            .And.Contain("\"score\":8.5")
            .And.NotContain("wrong-group")
            .And.NotContain("wrong-milestone");
    }
}

public sealed class MentorAvailabilityParityTests(FlowzyApiFactory factory) : IClassFixture<FlowzyApiFactory>
{
    [Fact]
    public async Task MentorAvailabilityEnforcesTimeOverlapOwnershipAndBookedRules()
    {
        using var client = factory.CreateClient();
        string token;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FlowzyDbContext>();
            var account = new Account
            {
                Email = "mentor.availability@local.test", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123"),
                Role = "MENTOR", Status = "ACTIVE", MustChangePassword = false
            };
            db.Mentors.Add(new Mentor
            {
                Account = account, MentorCode = "MEN-AVAILABILITY", FullName = "Availability Mentor",
                Status = "ACTIVE"
            });
            await db.SaveChangesAsync();
            token = scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateAccessToken(account);
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var start = new DateTimeOffset(DateTime.UtcNow.AddDays(2).Date.AddHours(3), TimeSpan.Zero);
        var end = start.AddHours(1);
        var created = await client.PostAsJsonAsync("/api/mentor/availability", new
        {
            startAt = start,
            endAt = end,
            meetLink = "https://meet.google.com/abc-defg-hij",
            note = "First slot"
        });
        created.StatusCode.Should().Be(HttpStatusCode.OK);
        var createdJson = await created.Content.ReadAsStringAsync();
        createdJson.Should().Contain("Availability slot created successfully")
            .And.Contain("\"status\":\"AVAILABLE\"");
        var slotId = System.Text.Json.JsonDocument.Parse(createdJson).RootElement
            .GetProperty("data").GetProperty("id").GetInt64();

        var overlap = await client.PostAsJsonAsync("/api/mentor/availability", new
        {
            startAt = start.AddMinutes(30),
            endAt = end.AddMinutes(30),
            meetLink = "https://meet.google.com/def-ghij-klm"
        });
        overlap.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await overlap.Content.ReadAsStringAsync()).Should().Contain("Slot overlaps with an existing availability slot");

        var invalidDuration = await client.PatchAsJsonAsync($"/api/mentor/availability/{slotId}", new
        {
            endAt = start.AddMinutes(90)
        });
        invalidDuration.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await invalidDuration.Content.ReadAsStringAsync()).Should().Contain("must last exactly 60 minutes");

        var updated = await client.PatchAsJsonAsync($"/api/mentor/availability/{slotId}", new
        {
            note = "Updated slot"
        });
        updated.StatusCode.Should().Be(HttpStatusCode.OK);
        (await updated.Content.ReadAsStringAsync()).Should().Contain("Updated slot");

        var listed = await client.GetAsync("/api/mentor/availability/me");
        listed.StatusCode.Should().Be(HttpStatusCode.OK);
        (await listed.Content.ReadAsStringAsync()).Should().Contain("Updated slot");

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FlowzyDbContext>();
            (await db.MentorAvailabilitySlots.FindAsync(slotId))!.Status = "BOOKED";
            await db.SaveChangesAsync();
        }
        var cancelBooked = await client.DeleteAsync($"/api/mentor/availability/{slotId}");
        cancelBooked.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await cancelBooked.Content.ReadAsStringAsync()).Should().Contain("Cannot cancel a booked slot");
    }
}

public sealed class FeedbackParityTests(FlowzyApiFactory factory) : IClassFixture<FlowzyApiFactory>
{
    [Fact]
    public async Task StudentSubmitsOnlyClosedTermFeedbackAndMentorReceivesAnonymousSummary()
    {
        using var client = factory.CreateClient();
        long closedFeedbackId;
        long openFeedbackId;
        string studentToken;
        string mentorToken;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FlowzyDbContext>();
            var studentAccount = new Account
            {
                Email = "student.feedback@local.test", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123"),
                Role = "STUDENT", Status = "ACTIVE", MustChangePassword = false
            };
            var student = new Student
            {
                Account = studentAccount, StudentCode = "SD-FEEDBACK", FullName = "Feedback Student",
                Status = "ACTIVE"
            };
            var mentorAccount = new Account
            {
                Email = "mentor.feedback@local.test", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123"),
                Role = "MENTOR", Status = "ACTIVE", MustChangePassword = false
            };
            var mentor = new Mentor
            {
                Account = mentorAccount, MentorCode = "MEN-FEEDBACK", FullName = "Feedback Mentor",
                Status = "ACTIVE"
            };
            var closedTerm = new AcademicTerm { Code = "FEEDBACK-CLOSED", Status = "CLOSED" };
            var openTerm = new AcademicTerm { Code = "FEEDBACK-OPEN", Status = "OPEN" };
            db.Students.Add(student);
            db.Mentors.Add(mentor);
            db.AcademicTerms.AddRange(closedTerm, openTerm);
            await db.SaveChangesAsync();
            var closedGroup = new StudentGroup
            {
                Term = closedTerm.Code, CourseCode = "PRN995", GroupNo = "1", Name = "Closed group",
                Status = "ACTIVE", MentorId = mentor.Id
            };
            var openGroup = new StudentGroup
            {
                Term = openTerm.Code, CourseCode = "PRN995", GroupNo = "2", Name = "Open group",
                Status = "ACTIVE", MentorId = mentor.Id
            };
            db.StudentGroups.AddRange(closedGroup, openGroup);
            await db.SaveChangesAsync();
            var closedFeedback = new TermFeedback
            {
                AcademicTermId = closedTerm.Id, GroupId = closedGroup.Id, StudentId = student.Id,
                TargetType = "MENTOR", MentorId = mentor.Id, Status = "PENDING"
            };
            var openFeedback = new TermFeedback
            {
                AcademicTermId = openTerm.Id, GroupId = openGroup.Id, StudentId = student.Id,
                TargetType = "MENTOR", MentorId = mentor.Id, Status = "PENDING"
            };
            db.TermFeedbacks.AddRange(closedFeedback, openFeedback);
            await db.SaveChangesAsync();
            closedFeedbackId = closedFeedback.Id;
            openFeedbackId = openFeedback.Id;
            var jwt = scope.ServiceProvider.GetRequiredService<IJwtService>();
            studentToken = jwt.GenerateAccessToken(studentAccount);
            mentorToken = jwt.GenerateAccessToken(mentorAccount);
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", studentToken);
        var own = await client.GetAsync("/api/feedback/me?status=PENDING");
        own.StatusCode.Should().Be(HttpStatusCode.OK);
        (await own.Content.ReadAsStringAsync()).Should().Contain("FEEDBACK-CLOSED").And.Contain("FEEDBACK-OPEN");

        var openSubmit = await client.PutAsJsonAsync($"/api/feedback/{openFeedbackId}", new
        {
            rating = 4,
            comment = "Not allowed yet"
        });
        openSubmit.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await openSubmit.Content.ReadAsStringAsync()).Should()
            .Contain("Feedback submission is not allowed while the academic term is OPEN.");

        var submitted = await client.PutAsJsonAsync($"/api/feedback/{closedFeedbackId}", new
        {
            rating = 5,
            comment = "  Very helpful  "
        });
        submitted.StatusCode.Should().Be(HttpStatusCode.OK);
        var submittedJson = await submitted.Content.ReadAsStringAsync();
        submittedJson.Should().Contain("\"comment\":\"Very helpful\"")
            .And.Contain("\"status\":\"SUBMITTED\"")
            .And.Contain("\"version\":1");

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", mentorToken);
        var received = await client.GetAsync("/api/feedback/received?term=feedback-closed&courseCode=prn995");
        received.StatusCode.Should().Be(HttpStatusCode.OK);
        var receivedJson = await received.Content.ReadAsStringAsync();
        receivedJson.Should().Contain("\"targetType\":\"MENTOR\"")
            .And.Contain("\"totalCount\":1")
            .And.Contain("\"averageRating\":5")
            .And.Contain("Very helpful")
            .And.NotContain("Feedback Student")
            .And.NotContain("SD-FEEDBACK");
    }
}

public sealed class MentorMeetingReportParityTests(FlowzyApiFactory factory) : IClassFixture<FlowzyApiFactory>
{
    [Fact]
    public async Task MentorListsOwnedTermsAndExportsJavaCompatibleWorkbook()
    {
        using var client = factory.CreateClient();
        string token;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FlowzyDbContext>();
            var account = new Account
            {
                Email = "mentor.report@local.test", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Password123"),
                Role = "MENTOR", Status = "ACTIVE", MustChangePassword = false
            };
            var mentor = new Mentor
            {
                Account = account, MentorCode = "MEN-REPORT", FullName = "Report Mentor", Status = "ACTIVE"
            };
            db.Mentors.Add(mentor);
            db.AcademicTerms.Add(new AcademicTerm { Code = "REPORT-TEST", Status = "CLOSED" });
            await db.SaveChangesAsync();
            var group = new StudentGroup
            {
                Term = "REPORT-TEST", CourseCode = "PRN994", GroupNo = "1", Name = "Report group",
                ProjectName = "Report project", Status = "ACTIVE", MentorId = mentor.Id
            };
            db.StudentGroups.Add(group);
            await db.SaveChangesAsync();
            db.MentorMeetings.Add(new MentorMeeting
            {
                GroupId = group.Id, MentorId = mentor.Id, StartAt = DateTime.UtcNow.AddDays(-2),
                EndAt = DateTime.UtcNow.AddDays(-2).AddHours(1), Status = "COMPLETED",
                MeetLink = "https://meet.google.com/abc-defg-hij", Note = "Architecture review",
                CompletedAt = DateTime.UtcNow.AddDays(-2).AddHours(1)
            });
            await db.SaveChangesAsync();
            token = scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateAccessToken(account);
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var terms = await client.GetAsync("/api/mentor/meeting-reports/terms");
        terms.StatusCode.Should().Be(HttpStatusCode.OK);
        (await terms.Content.ReadAsStringAsync()).Should().Contain("REPORT-TEST").And.Contain("CLOSED");

        var missing = await client.GetAsync("/api/mentor/meeting-reports/export.xlsx");
        missing.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await missing.Content.ReadAsStringAsync()).Should().Contain("Academic term is required");

        var export = await client.GetAsync("/api/mentor/meeting-reports/export.xlsx?term=report-test");
        export.StatusCode.Should().Be(HttpStatusCode.OK);
        export.Content.Headers.ContentType!.MediaType.Should()
            .Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        export.Content.Headers.ContentDisposition!.FileName.Should().Be("\"mentor-meeting-report-REPORT-TEST.xlsx\"");
        var bytes = await export.Content.ReadAsByteArrayAsync();
        bytes.Take(2).Should().Equal((byte)'P', (byte)'K');
        using var stream = new MemoryStream(bytes);
        using var workbook = new NPOI.XSSF.UserModel.XSSFWorkbook(stream);
        workbook.GetSheet("Group Summary").GetRow(1).GetCell(3).StringCellValue.Should().Be("Report group");
        workbook.GetSheet("Group Summary").GetRow(1).GetCell(7).NumericCellValue.Should().Be(1);
        workbook.GetSheet("Meeting Details").GetRow(1).GetCell(8).StringCellValue.Should()
            .Be("Architecture review");
    }
}

public sealed class AdminGroupParityTests(FlowzyApiFactory factory) : IClassFixture<FlowzyApiFactory>
{
    [Fact]
    public async Task AdminGroupListPreservesFiltersPagingAndNestedSummaryContract()
    {
        using var client = factory.CreateClient();
        string token;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FlowzyDbContext>();
            var admin = db.Accounts.Single(x => x.Email == "admin.integration@local.test");
            admin.MustChangePassword = false;
            var student = new Student
            {
                Account = new Account
                {
                    Email = "student.admin.group@local.test", PasswordHash = "unused", Role = "STUDENT",
                    Status = "ACTIVE", MustChangePassword = false
                },
                StudentCode = "SD-ADMIN-GROUP", FullName = "Group Leader", Status = "ACTIVE"
            };
            db.Students.Add(student);
            db.AcademicTerms.Add(new AcademicTerm { Code = "ADMIN-GROUP-TEST", Status = "CLOSED" });
            await db.SaveChangesAsync();
            var problem = new Problem
            {
                Code = "PB-ADMIN-GROUP", Title = "Selected problem", Statement = "Statement",
                DifficultyLevel = "BEGINNER", SourceType = "OFFICIAL", Status = "ACTIVE"
            };
            db.Problems.Add(problem);
            await db.SaveChangesAsync();
            var active = new StudentGroup
            {
                Term = "ADMIN-GROUP-TEST", CourseCode = "PRN993", GroupNo = "1",
                Name = "Searchable assignment group", ProjectName = "Flowzy project", Status = "ACTIVE",
                LeaderStudentId = student.Id, SelectedProblemId = problem.Id, IsLocked = true
            };
            var inactive = new StudentGroup
            {
                Term = "ADMIN-GROUP-TEST", CourseCode = "PRN993", GroupNo = "2",
                Name = "Inactive assignment group", Status = "INACTIVE"
            };
            db.StudentGroups.AddRange(active, inactive);
            await db.SaveChangesAsync();
            db.StudentGroupMembers.Add(new StudentGroupMember
            {
                GroupId = active.Id, StudentId = student.Id, MemberRole = "LEADER"
            });
            db.GroupRecruitmentNeeds.Add(new GroupRecruitmentNeed
            {
                GroupId = active.Id, Role = "SOFTWARE_DEVELOPER", Quantity = 2
            });
            await db.SaveChangesAsync();
            token = scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateAccessToken(admin);
        }

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var defaultActive = await client.GetAsync("/api/admin/groups?search=Searchable&page=0&size=20");
        defaultActive.StatusCode.Should().Be(HttpStatusCode.OK);
        var activeJson = await defaultActive.Content.ReadAsStringAsync();
        activeJson.Should().Contain("Searchable assignment group")
            .And.Contain("\"termStatus\":\"CLOSED\"")
            .And.Contain("\"studentReadOnly\":true")
            .And.Contain("\"isLock\":true")
            .And.Contain("\"memberCount\":1")
            .And.Contain("Lập trình phần mềm")
            .And.Contain("Selected problem")
            .And.NotContain("Inactive assignment group");

        var all = await client.GetAsync("/api/admin/groups?status=ALL&page=0&size=20");
        all.StatusCode.Should().Be(HttpStatusCode.OK);
        (await all.Content.ReadAsStringAsync()).Should().Contain("Searchable assignment group")
            .And.Contain("Inactive assignment group")
            .And.Contain("\"totalElements\":2");

        var invalid = await client.GetAsync("/api/admin/groups?status=DELETED");
        invalid.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await invalid.Content.ReadAsStringAsync()).Should().Contain("Status must be ACTIVE, INACTIVE, or ALL");
    }
}

public sealed class AdminFeedbackParityTests(FlowzyApiFactory factory) : IClassFixture<FlowzyApiFactory>
{
    [Fact]
    public async Task AdminFiltersIdentifiedFeedbackAndExportsSubmittedRows()
    {
        using var client = factory.CreateClient();
        string token;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FlowzyDbContext>();
            var admin = db.Accounts.Single(x => x.Email == "admin.integration@local.test");
            admin.MustChangePassword = false;
            var student = new Student { Account = new Account { Email = "admin.feedback.student@local.test", PasswordHash = "x", Role = "STUDENT", Status = "ACTIVE" }, StudentCode = "AF-STUDENT", FullName = "Admin Feedback Student", Status = "ACTIVE" };
            var mentor = new Mentor { Account = new Account { Email = "admin.feedback.mentor@local.test", PasswordHash = "x", Role = "MENTOR", Status = "ACTIVE" }, MentorCode = "AF-MENTOR", FullName = "Admin Feedback Mentor", Status = "ACTIVE" };
            var term = new AcademicTerm { Code = "ADMIN-FEEDBACK", Status = "CLOSED" };
            db.Students.Add(student); db.Mentors.Add(mentor); db.AcademicTerms.Add(term);
            await db.SaveChangesAsync();
            var group = new StudentGroup { Term = term.Code, CourseCode = "PRN992", GroupNo = "1", Name = "Feedback export group", Status = "ACTIVE", MentorId = mentor.Id };
            db.StudentGroups.Add(group); await db.SaveChangesAsync();
            db.TermFeedbacks.Add(new TermFeedback { AcademicTermId = term.Id, GroupId = group.Id, StudentId = student.Id, TargetType = "MENTOR", MentorId = mentor.Id, Rating = 4, Comment = "Exported feedback", Status = "SUBMITTED", SubmittedAt = DateTime.UtcNow });
            await db.SaveChangesAsync();
            token = scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateAccessToken(admin);
        }
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var list = await client.GetAsync("/api/admin/feedback?term=admin-feedback&targetType=MENTOR&targetSearch=AF-MENTOR&status=SUBMITTED");
        list.StatusCode.Should().Be(HttpStatusCode.OK);
        (await list.Content.ReadAsStringAsync()).Should().Contain("Admin Feedback Student").And.Contain("Admin Feedback Mentor").And.Contain("Exported feedback");
        var export = await client.GetAsync("/api/admin/feedback/export.xlsx?term=admin-feedback");
        export.StatusCode.Should().Be(HttpStatusCode.OK);
        using var workbook = new NPOI.XSSF.UserModel.XSSFWorkbook(new MemoryStream(await export.Content.ReadAsByteArrayAsync()));
        workbook.GetSheet("Mentor Feedback").GetRow(1).GetCell(13).StringCellValue.Should().Be("Exported feedback");
        workbook.GetSheet("Instructor Feedback").LastRowNum.Should().Be(0);
    }
}
