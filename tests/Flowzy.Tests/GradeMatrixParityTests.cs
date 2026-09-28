using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Flowzy.Repository.Data;
using Flowzy.Repository.Entities;
using Flowzy.Service.Authentication;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Xunit;

namespace Flowzy.Tests;
public sealed class GradeMatrixParityTests(FlowzyApiFactory factory) : IClassFixture<FlowzyApiFactory>
{
    private sealed record Fixture(long Group, long Milestone, long InstructorId, long InstructorAccount, long LeaderId, long LeaderAccount, long MemberId, long MemberAccount, long OutsiderId,
        string Term, string Course, HttpClient Instructor, HttpClient Leader, HttpClient Member, HttpClient Outsider, HttpClient Admin) : IDisposable
    { public void Dispose() { Instructor.Dispose(); Leader.Dispose(); Member.Dispose(); Outsider.Dispose(); Admin.Dispose(); } }
    private async Task<Fixture> Seed()
    {
        using var bootstrap = factory.CreateClient(); await using var scope = factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<FlowzyDbContext>();
        var key = Guid.NewGuid().ToString("N")[..12].ToUpperInvariant();
        Account Account(string role, int i) => new() { Email = key + i + "@matrix.test", Role = role, Status = "ACTIVE", PasswordHash = "unused" };
        var instructor = new Instructor { InstructorCode = key, FullName = "Instructor", Status = "ACTIVE", Account = Account("INSTRUCTOR", 0) };
        var students = Enumerable.Range(1, 3).Select(i => new Student { StudentCode = key + i, FullName = "Student " + i, Status = "ACTIVE", Account = Account("STUDENT", i) }).ToArray();
        var admin = Account("ADMIN", 4);
        var term = await db.AcademicTerms.FirstOrDefaultAsync(x => x.Status == "OPEN") ?? new AcademicTerm { Code = "M" + key, Status = "OPEN" };
        if (term.Id == 0) db.AcademicTerms.Add(term);
        db.Instructors.Add(instructor); db.Students.AddRange(students); db.Accounts.Add(admin); await db.SaveChangesAsync();
        var group = new StudentGroup { Term = term.Code, CourseCode = "C" + key, GroupNo = "1", Name = "Group " + key, InstructorId = instructor.Id, Status = "ACTIVE" };
        db.StudentGroups.Add(group); await db.SaveChangesAsync();
        db.StudentGroupMembers.Add(new StudentGroupMember { GroupId = group.Id, StudentId = students[0].Id, MemberRole = "LEADER" });
        db.StudentGroupMembers.Add(new StudentGroupMember { GroupId = group.Id, StudentId = students[1].Id, MemberRole = "MEMBER" }); await db.SaveChangesAsync();
        group.LeaderStudentId = students[0].Id;
        var milestone = new CourseMilestone { Term = term.Code, CourseCode = group.CourseCode, Title = "Review", Type = "TIMELINE", Status = "ACTIVE", MaxScore = 10, Weight = 40, Position = 0, InstructorId = instructor.Id };
        db.CourseMilestones.Add(milestone); await db.SaveChangesAsync();
        var jwt = scope.ServiceProvider.GetRequiredService<IJwtService>();
        HttpClient Client(Account a) { var c = factory.CreateClient(); c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt.GenerateAccessToken(a)); return c; }
        return new(group.Id, milestone.Id, instructor.Id, instructor.AccountId, students[0].Id, students[0].AccountId, students[1].Id, students[1].AccountId, students[2].Id,
            term.Code, group.CourseCode, Client(instructor.Account), Client(students[0].Account), Client(students[1].Account), Client(students[2].Account), Client(admin));
    }
    private async Task WithDb(Func<FlowzyDbContext, Task> action) { await using var scope = factory.Services.CreateAsyncScope(); await action(scope.ServiceProvider.GetRequiredService<FlowzyDbContext>()); }
    private static string Contributions(Fixture f) => $"/api/groups/{f.Group}/milestones/{f.Milestone}/contributions";
    private static string Agreement(Fixture f) => $"/api/groups/{f.Group}/milestones/{f.Milestone}/contribution-agreement";
    private static string Grade(Fixture f) => $"/api/instructor/milestones/{f.Milestone}/groups/{f.Group}/grade";
    private static string Matrix(Fixture f) => $"/api/groups/{f.Group}/grades";
    private static object Items(Fixture f, decimal first = 100, decimal second = 50) => new { items = new[] { new { studentId = f.LeaderId, contributionPercent = first }, new { studentId = f.MemberId, contributionPercent = second } } };
    private static async Task<JsonElement> Body(HttpResponseMessage response, HttpStatusCode expected = HttpStatusCode.OK)
    { var raw = await response.Content.ReadAsStringAsync(); response.StatusCode.Should().Be(expected, raw); using var doc = JsonDocument.Parse(raw); return doc.RootElement.Clone(); }
    private static async Task Error(HttpResponseMessage response, HttpStatusCode expected, string message) => (await Body(response, expected)).GetProperty("message").GetString().Should().Be(message);
    private static async Task Ready(Fixture f)
    { await Body(await f.Leader.PutAsJsonAsync(Contributions(f), Items(f))); await Body(await f.Leader.PutAsJsonAsync(Agreement(f), new { decision = "AGREE" })); await Body(await f.Member.PutAsJsonAsync(Agreement(f), new { decision = "AGREE" })); }

    [Fact]
    public async Task FullRevisionAgreementGradeFlowKeepsPercentagesIndependentAndLocksAfterGrade()
    {
        using var f = await Seed(); await Ready(f);
        var graded = (await Body(await f.Instructor.PutAsJsonAsync(Grade(f), new { score = 8.00m, feedback = "Good" }))).GetProperty("data");
        graded.GetProperty("gradeComplete").GetBoolean().Should().BeTrue();
        var matrix = (await Body(await f.Member.GetAsync(Matrix(f)))).GetProperty("data"); matrix.GetProperty("complete").GetBoolean().Should().BeTrue();
        var rows = matrix.GetProperty("members"); rows[0].GetProperty("totalScore").GetDecimal().Should().Be(3.2000m); rows[1].GetProperty("totalScore").GetDecimal().Should().Be(1.6000m);
        matrix.GetProperty("milestones")[0].GetProperty("approvedCount").GetInt32().Should().Be(2);
        await Error(await f.Leader.PutAsJsonAsync(Contributions(f), Items(f)), HttpStatusCode.Conflict, "Contributions are locked after the instructor grades this milestone");
        await Error(await f.Member.PutAsJsonAsync(Agreement(f), new { decision = "AGREE" }), HttpStatusCode.Conflict, "Contribution agreements are locked after grading");
        await WithDb(async db => { var notices = await db.Notifications.Where(x => x.Type == "MILESTONE_GROUP_GRADED" && x.EntityId == graded.GetProperty("id").GetInt64().ToString()).ToListAsync();
            notices.Select(x => x.RecipientId).Should().BeEquivalentTo([f.LeaderAccount, f.MemberAccount]); notices.Should().OnlyContain(x => x.ActionKey == "OPEN_GRADES");
            db.MilestoneContributionAgreements.RemoveRange(db.MilestoneContributionAgreements.Where(x => x.Revision.MilestoneId == f.Milestone)); await db.SaveChangesAsync(); });
        // Existing grades do not require a new agreement, and a same-scale identical grade sends no new notification.
        await Body(await f.Instructor.PutAsJsonAsync(Grade(f), new { score = 8.00m, feedback = "Good" }));
        await WithDb(async db => (await db.Notifications.CountAsync(x => x.Type == "MILESTONE_GROUP_GRADED" && x.EntityId == graded.GetProperty("id").GetInt64().ToString())).Should().Be(2));
        await Body(await f.Instructor.PutAsJsonAsync(Grade(f), new { score = 9.00m, feedback = "Updated" }));
    }

    [Fact]
    public async Task ChangesRequestedFreezeResponsesUntilLeaderSubmitsNewRevision()
    {
        using var f = await Seed();
        await Error(await f.Member.PutAsJsonAsync(Agreement(f), new { decision = "AGREE" }), HttpStatusCode.Conflict, "The group leader must submit contributions first");
        await Body(await f.Leader.PutAsJsonAsync(Contributions(f), Items(f)));
        await Error(await f.Instructor.PutAsJsonAsync(Grade(f), new { score = 8 }), HttpStatusCode.Conflict, "Every active member must agree to the contribution revision before grading");
        await Error(await f.Member.PutAsJsonAsync(Agreement(f), new { decision = "REQUEST_CHANGES", reason = " " }), HttpStatusCode.BadRequest, "A reason is required when requesting changes");
        var reply = (await Body(await f.Member.PutAsJsonAsync(Agreement(f), new { decision = "REQUEST_CHANGES", reason = "  Please review  " }))).GetProperty("data");
        reply.GetProperty("reason").GetString().Should().Be("Please review"); reply.GetProperty("status").GetString().Should().Be("CHANGES_REQUESTED");
        await Error(await f.Leader.PutAsJsonAsync(Agreement(f), new { decision = "AGREE" }), HttpStatusCode.Conflict, "The leader must submit a new revision before more responses are accepted");
        await Body(await f.Leader.PutAsJsonAsync(Contributions(f), Items(f, 90, 70)));
        var matrix = (await Body(await f.Leader.GetAsync(Matrix(f)))).GetProperty("data"); var col = matrix.GetProperty("milestones")[0];
        col.GetProperty("contributionRevision").GetInt32().Should().Be(2); col.GetProperty("approvedCount").GetInt32().Should().Be(0); col.GetProperty("contributionAgreementStatus").GetString().Should().Be("PENDING");
        await WithDb(async db => { (await db.MilestoneContributionAgreements.AnyAsync(x => x.Revision.MilestoneId == f.Milestone)).Should().BeFalse();
            (await db.Notifications.CountAsync(x => x.RecipientId == f.LeaderAccount && x.Type == "CONTRIBUTION_CHANGES_REQUESTED")).Should().Be(1); });
    }

    [Fact]
    public async Task GradeRequiresExactActiveMemberContributionSetAndIgnoresInactiveAgreementsInSummary()
    {
        using var f = await Seed();
        await Error(await f.Instructor.PutAsJsonAsync(Grade(f), new { score = 8 }), HttpStatusCode.BadRequest, "Contribution must be provided for every active group member before grading");
        await Ready(f);
        await WithDb(async db => { db.MilestoneMemberScores.Add(new MilestoneMemberScore { MilestoneId = f.Milestone, GroupId = f.Group, StudentId = f.OutsiderId, ContributionPercent = 10 });
            var rev = await db.MilestoneContributionRevisions.SingleAsync(x => x.MilestoneId == f.Milestone);
            db.MilestoneContributionAgreements.Add(new MilestoneContributionAgreement { RevisionId = rev.Id, StudentId = f.OutsiderId, Decision = "REQUEST_CHANGES", Reason = "Former member" }); await db.SaveChangesAsync(); });
        var matrix = (await Body(await f.Admin.GetAsync(Matrix(f)))).GetProperty("data"); matrix.GetProperty("milestones")[0].GetProperty("contributionAgreementStatus").GetString().Should().Be("AGREED");
        await Error(await f.Instructor.PutAsJsonAsync(Grade(f), new { score = 8 }), HttpStatusCode.BadRequest, "Contribution must be provided for every active group member before grading");
        await WithDb(async db => { db.MilestoneMemberScores.RemoveRange(db.MilestoneMemberScores.Where(x => x.MilestoneId == f.Milestone && x.StudentId == f.OutsiderId)); await db.SaveChangesAsync(); });
        await Body(await f.Instructor.PutAsJsonAsync(Grade(f), new { score = 8 }));
    }

    [Theory]
    [InlineData("{}", "Score is required")]
    [InlineData("{\"score\":-1}", "Score must be non-negative")]
    public async Task MissingAndNegativeGradeAreValidated(string json, string message)
    { using var f = await Seed(); var body = await Body(await f.Instructor.PutAsync(Grade(f), new StringContent(json, Encoding.UTF8, "application/json")), HttpStatusCode.BadRequest); body.GetProperty("data").GetProperty("score").GetString().Should().Be(message); }

    [Fact]
    public async Task ContributionDtoAndMembershipErrorsMatchJava()
    {
        using var f = await Seed();
        await Body(await f.Leader.PutAsJsonAsync(Contributions(f), new { items = Array.Empty<object>() }), HttpStatusCode.BadRequest);
        await Body(await f.Leader.PutAsJsonAsync(Contributions(f), new { items = new[] { new { contributionPercent = 50 } } }), HttpStatusCode.BadRequest);
        await Body(await f.Leader.PutAsJsonAsync(Contributions(f), Items(f, -1, 50)), HttpStatusCode.BadRequest);
        await Body(await f.Leader.PutAsJsonAsync(Contributions(f), Items(f, 101, 50)), HttpStatusCode.BadRequest);
        await Error(await f.Leader.PutAsJsonAsync(Contributions(f), new { items = new[] { new { studentId = f.LeaderId, contributionPercent = 50 }, new { studentId = f.LeaderId, contributionPercent = 50 } } }), HttpStatusCode.BadRequest, "Duplicate contribution for student id: " + f.LeaderId);
        await Error(await f.Member.PutAsJsonAsync(Contributions(f), Items(f)), HttpStatusCode.Forbidden, "Only the group leader can update milestone contributions");
        await Error(await f.Outsider.PutAsJsonAsync(Agreement(f), new { decision = "AGREE" }), HttpStatusCode.Forbidden, "Only active group members can respond to contributions");
        await Error(await f.Leader.PutAsJsonAsync(Agreement(f), new { decision = "agree" }), HttpStatusCode.BadRequest, "Malformed JSON request");
        await Body(await f.Leader.PutAsJsonAsync(Agreement(f), new { decision = "AGREE", reason = new string('x', 1001) }), HttpStatusCode.BadRequest);
        await Body(await f.Instructor.PutAsJsonAsync(Grade(f), new { score = 8, feedback = new string('x', 5001) }), HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ScopeClosedTermAndRoleErrorsKeepPrecedence()
    {
        using var f = await Seed(); await Ready(f);
        await Error(await f.Admin.PutAsJsonAsync(Contributions(f), Items(f)), HttpStatusCode.Forbidden, "Only students can perform this action");
        await Error(await f.Outsider.GetAsync(Matrix(f)), HttpStatusCode.Forbidden, "You do not have permission to view grades for this group");
        using var anon = factory.CreateClient(); await Body(await anon.GetAsync(Matrix(f)), HttpStatusCode.Unauthorized);
        await WithDb(async db => { var m = await db.CourseMilestones.FindAsync(f.Milestone); m!.Term = " " + f.Term.ToLowerInvariant() + " "; m.CourseCode = " " + f.Course.ToLowerInvariant() + " ";
            (await db.AcademicTerms.SingleAsync(x => x.Code == f.Term)).Status = "CLOSED"; await db.SaveChangesAsync(); });
        await Error(await f.Leader.PutAsJsonAsync(Contributions(f), Items(f)), HttpStatusCode.Conflict, $"Academic term {f.Term} has ended; this group is read-only for students");
        await Body(await f.Instructor.PutAsJsonAsync(Grade(f), new { score = 8 }));
        await WithDb(async db => { (await db.StudentGroups.FindAsync(f.Group))!.Status = "INACTIVE"; await db.SaveChangesAsync(); });
        await Error(await f.Instructor.PutAsJsonAsync(Grade(f), new { score = 8 }), HttpStatusCode.BadRequest, "Student group is not active");
        await WithDb(async db => { (await db.CourseMilestones.FindAsync(f.Milestone))!.Status = "CLOSED"; await db.SaveChangesAsync(); });
        await Error(await f.Instructor.PutAsJsonAsync(Grade(f), new { score = 8 }), HttpStatusCode.BadRequest, "Only active milestones can be graded");
    }

    [Fact]
    public async Task MatrixCompletenessAndOwnerScopeAreNotInferredFromMemberRows()
    {
        using var f = await Seed();
        await WithDb(async db => { (await db.CourseMilestones.FindAsync(f.Milestone))!.Status = "INACTIVE"; await db.SaveChangesAsync(); });
        var matrix = (await Body(await f.Admin.GetAsync(Matrix(f)))).GetProperty("data"); matrix.GetProperty("complete").GetBoolean().Should().BeFalse();
        matrix.GetProperty("members")[0].GetProperty("complete").GetBoolean().Should().BeTrue();
        await WithDb(async db => { var m = await db.CourseMilestones.FindAsync(f.Milestone); m!.Status = "CLOSED"; m.InstructorId = null; await db.SaveChangesAsync(); });
        await Error(await f.Admin.GetAsync(Matrix(f)), HttpStatusCode.Conflict, "No active milestones are owned by this group's assigned instructor; check group instructor assignment or milestone owner");
        await WithDb(async db => { (await db.StudentGroups.FindAsync(f.Group))!.InstructorId = null; await db.SaveChangesAsync(); });
        await Error(await f.Admin.GetAsync(Matrix(f)), HttpStatusCode.Conflict, "Group must be assigned an instructor before grading milestones");
    }

    [Fact]
    public async Task ConcurrentResponsesAndRevisionsDoNotLoseAgreementOrRevisionNumbers()
    {
        using var f = await Seed();
        var submissions = await Task.WhenAll(f.Leader.PutAsJsonAsync(Contributions(f), Items(f)), f.Leader.PutAsJsonAsync(Contributions(f), Items(f, 90, 60)));
        foreach (var response in submissions) await Body(response);
        var agreements = await Task.WhenAll(f.Leader.PutAsJsonAsync(Agreement(f), new { decision = "AGREE" }), f.Member.PutAsJsonAsync(Agreement(f), new { decision = "AGREE" }));
        foreach (var response in agreements) await Body(response);
        var col = (await Body(await f.Leader.GetAsync(Matrix(f)))).GetProperty("data").GetProperty("milestones")[0];
        col.GetProperty("contributionRevision").GetInt32().Should().Be(2); col.GetProperty("approvedCount").GetInt32().Should().Be(2);
        var grades = await Task.WhenAll(f.Instructor.PutAsJsonAsync(Grade(f), new { score = 7.00m }), f.Instructor.PutAsJsonAsync(Grade(f), new { score = 8.00m }));
        foreach (var response in grades) await Body(response);
        await WithDb(async db => (await db.MilestoneGroupGrades.CountAsync(x => x.GroupId == f.Group)).Should().Be(1));
    }

    [Fact]
    public async Task MemberScoreFailureRollsBackGradeAndNotifications()
    {
        using var f = await Seed(); await Ready(f);
        await WithDb(db => db.Database.ExecuteSqlRawAsync($"""
            CREATE FUNCTION parity_matrix_fail_{f.Group}() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'fixture failure'; END; $$;
            CREATE TRIGGER parity_matrix_fail_{f.Group} BEFORE UPDATE ON milestone_member_scores FOR EACH ROW WHEN (NEW.group_id = {f.Group} AND NEW.calculated_score IS NOT NULL) EXECUTE FUNCTION parity_matrix_fail_{f.Group}();
            """));
        try
        {
            await Body(await f.Instructor.PutAsJsonAsync(Grade(f), new { score = 8 }), HttpStatusCode.Conflict);
            await WithDb(async db => { (await db.MilestoneGroupGrades.AnyAsync(x => x.GroupId == f.Group)).Should().BeFalse();
                (await db.MilestoneMemberScores.Where(x => x.GroupId == f.Group).ToListAsync()).Should().OnlyContain(x => x.CalculatedScore == null && x.GroupGradeId == null);
                (await db.Notifications.AnyAsync(x => x.RecipientId == f.MemberAccount && x.Type == "MILESTONE_GROUP_GRADED")).Should().BeFalse(); });
        }
        finally { await WithDb(db => db.Database.ExecuteSqlRawAsync($"DROP TRIGGER parity_matrix_fail_{f.Group} ON milestone_member_scores; DROP FUNCTION parity_matrix_fail_{f.Group}();")); }
    }

    [Fact]
    public async Task WeightedTotalsRoundEachMilestoneAndMatrixRequiresActualGroupGrades()
    {
        using var f = await Seed();
        await WithDb(async db => {
            var second = new CourseMilestone { InstructorId = f.InstructorId, Term = f.Term, CourseCode = f.Course, Title = "Second", Type = "TIMELINE", Status = "CLOSED", MaxScore = 7, Weight = 33, Position = 1 };
            db.CourseMilestones.Add(second); await db.SaveChangesAsync();
            foreach (var mid in new[] { f.Milestone, second.Id })
                foreach (var sid in new[] { f.LeaderId, f.MemberId })
                    db.MilestoneMemberScores.Add(new MilestoneMemberScore { MilestoneId = mid, GroupId = f.Group, StudentId = sid, ContributionPercent = 50, CalculatedScore = 1.2345m, MaxScoreSnapshot = 7, WeightSnapshot = 33 });
            await db.SaveChangesAsync();
        });
        var matrix = (await Body(await f.Admin.GetAsync(Matrix(f)))).GetProperty("data");
        matrix.GetProperty("members")[0].GetProperty("totalScore").GetDecimal().Should().Be(1.1640m);
        matrix.GetProperty("members")[0].GetProperty("complete").GetBoolean().Should().BeTrue();
        matrix.GetProperty("complete").GetBoolean().Should().BeFalse();
        matrix.GetProperty("milestones").EnumerateArray().Should().OnlyContain(x => !x.GetProperty("gradeComplete").GetBoolean());
        await WithDb(async db => { foreach (var mid in await db.CourseMilestones.Where(x => x.InstructorId == f.InstructorId).Select(x => x.Id).ToListAsync())
            db.MilestoneGroupGrades.Add(new MilestoneGroupGrade { MilestoneId = mid, GroupId = f.Group, InstructorId = f.InstructorId, Score = 2.4690m, MaxScoreSnapshot = 7, WeightSnapshot = 33 }); await db.SaveChangesAsync(); });
        (await Body(await f.Admin.GetAsync(Matrix(f)))).GetProperty("data").GetProperty("complete").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task FirstGradeRacingNewContributionsCannotAcceptBoth()
    {
        using var f = await Seed(); await Ready(f);
        var responses = await Task.WhenAll(f.Instructor.PutAsJsonAsync(Grade(f), new { score = 8.00m }), f.Leader.PutAsJsonAsync(Contributions(f), Items(f, 80, 80)));
        responses.Select(x => x.StatusCode).Should().BeEquivalentTo([HttpStatusCode.OK, HttpStatusCode.Conflict]);
        await WithDb(async db => { var revision = await db.MilestoneContributionRevisions.SingleAsync(x => x.GroupId == f.Group);
            if (responses[0].IsSuccessStatusCode) { revision.Revision.Should().Be(1); (await db.MilestoneMemberScores.Where(x => x.GroupId == f.Group).ToListAsync()).Should().OnlyContain(x => x.CalculatedScore != null); }
            else { revision.Revision.Should().Be(2); (await db.MilestoneGroupGrades.AnyAsync(x => x.GroupId == f.Group)).Should().BeFalse(); (await db.MilestoneContributionAgreements.AnyAsync(x => x.RevisionId == revision.Id)).Should().BeFalse(); }
        });
    }

    [Fact]
    public async Task RevisionNotificationFailureRollsBackPercentagesRevisionAndAgreementCleanup()
    {
        using var f = await Seed(); await Ready(f);
        await WithDb(db => db.Database.ExecuteSqlRawAsync($"""
            CREATE FUNCTION parity_revision_fail_{f.Group}() RETURNS trigger LANGUAGE plpgsql AS $$ BEGIN RAISE EXCEPTION 'fixture failure'; END; $$;
            CREATE TRIGGER parity_revision_fail_{f.Group} BEFORE INSERT ON notifications FOR EACH ROW
            WHEN (NEW.recipient_id = {f.MemberAccount} AND NEW.type = 'CONTRIBUTION_REVISION_CREATED') EXECUTE FUNCTION parity_revision_fail_{f.Group}();
            """));
        try
        {
            await Body(await f.Leader.PutAsJsonAsync(Contributions(f), Items(f, 25, 25)), HttpStatusCode.Conflict);
            await WithDb(async db => { var rev = await db.MilestoneContributionRevisions.SingleAsync(x => x.GroupId == f.Group); rev.Revision.Should().Be(1);
                (await db.MilestoneContributionAgreements.CountAsync(x => x.RevisionId == rev.Id)).Should().Be(2);
                (await db.MilestoneMemberScores.SingleAsync(x => x.GroupId == f.Group && x.StudentId == f.LeaderId)).ContributionPercent.Should().Be(100);
                (await db.Notifications.CountAsync(x => x.RecipientId == f.MemberAccount && x.Type == "CONTRIBUTION_REVISION_CREATED")).Should().Be(1); });
        }
        finally { await WithDb(db => db.Database.ExecuteSqlRawAsync($"DROP TRIGGER parity_revision_fail_{f.Group} ON notifications; DROP FUNCTION parity_revision_fail_{f.Group}();")); }
    }

    [Fact]
    public async Task ExportMatchesCsvScopeLineEndingsAndNumericRawWorkbook()
    {
        using var f = await Seed(); await Ready(f); await Body(await f.Instructor.PutAsJsonAsync(Grade(f), new { score = 8.00m }));
        await WithDb(async db => { db.CourseMilestones.Add(new CourseMilestone { InstructorId = f.InstructorId, Term = f.Term, CourseCode = "OTHER", Title = "Unrelated", Type = "TIMELINE", Status = "ACTIVE", MaxScore = 10, Weight = 10 }); await db.SaveChangesAsync(); });
        var query = "?term=" + f.Term + "&courseCode=" + f.Course + "&groupId=" + f.Group;
        var csvResponse = await f.Instructor.GetAsync("/api/instructor/grades/export.csv" + query); csvResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        csvResponse.Content.Headers.ContentDisposition!.ToString().Should().Be($"attachment; filename=\"grades-{f.Term}-{f.Course}.csv\"");
        var bytes = await csvResponse.Content.ReadAsByteArrayAsync(); bytes.Take(3).Should().Equal(0xEF, 0xBB, 0xBF);
        var csv = Encoding.UTF8.GetString(bytes[3..]); csv.Should().StartWith("groupId,groupNo,groupName,studentId,studentCode,studentName,Review contribution %,Review individual score,finalTotal,complete\r\n");
        csv.Should().NotContain("Unrelated").And.NotContain("\r\r\n").And.Contain(",3.2000,true\r\n");
        var xlsxResponse = await f.Instructor.GetAsync("/api/instructor/grades/export.xlsx" + query); xlsxResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var stream = new MemoryStream(await xlsxResponse.Content.ReadAsByteArrayAsync()); using var workbook = new XSSFWorkbook(stream); var sheet = workbook.GetSheet("RAW");
        sheet.Should().NotBeNull(); sheet.GetRow(0).Cells.Select(x => x.StringCellValue).Should().Equal("Tên Nhóm", "Họ & Tên", "MSSV", "Email (đuôi FPT)", "Review", "Final");
        sheet.GetRow(1).GetCell(4).CellType.Should().Be(CellType.Numeric); sheet.GetRow(1).GetCell(4).NumericCellValue.Should().Be(8);
        sheet.GetRow(1).GetCell(5).NumericCellValue.Should().BeApproximately(3.2, 0.00001); sheet.GetRow(1).GetCell(5).CellStyle.GetDataFormatString().Should().Be("0.0###");
        await Error(await f.Instructor.GetAsync("/api/instructor/grades/export.csv?groupId=" + long.MaxValue), HttpStatusCode.NotFound, "Group not found for this instructor scope");
    }
}
