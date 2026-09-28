using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Flowzy.Repository.Data;
using Flowzy.Repository.Entities;
using Flowzy.Service.Authentication;
using Flowzy.Service.Imports;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NPOI.SS.UserModel;
using Xunit;

namespace Flowzy.Tests;

public sealed class TaskAndImportParityTests(FlowzyApiFactory factory) : IClassFixture<FlowzyApiFactory>
{
    private async Task<(long Group, long Member, HttpClient Leader, HttpClient Assignee, HttpClient Other)> TaskScope()
    {
        _ = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<FlowzyDbContext>();
        var suffix = Guid.NewGuid().ToString("N"); var students = Enumerable.Range(0, 3).Select(i => new Student
        { StudentCode = $"S{i}-{suffix}", FullName = $"Student {i}", Status = "ACTIVE", Account = new Account
          { Email = $"s{i}.{suffix}@local.test", PasswordHash = "unused", Role = "STUDENT", Status = "ACTIVE", MustChangePassword = false } }).ToList();
        db.Students.AddRange(students);
        var term = await db.AcademicTerms.FirstOrDefaultAsync(x => x.Status == "OPEN");
        if (term is null) { term = new AcademicTerm { Code = "SU26", Status = "OPEN" }; db.AcademicTerms.Add(term); }
        await db.SaveChangesAsync();
        var group = new StudentGroup { Term = term.Code, CourseCode = "EXE101", GroupNo = suffix[..24], Name = suffix, Status = "ACTIVE" };
        db.StudentGroups.Add(group); await db.SaveChangesAsync();
        db.StudentGroupMembers.AddRange(students.Select((s, i) => new StudentGroupMember { GroupId = group.Id, StudentId = s.Id, MemberRole = i == 0 ? "LEADER" : "MEMBER" }));
        await db.SaveChangesAsync(); group.LeaderStudentId = students[0].Id; await db.SaveChangesAsync();
        var jwt = scope.ServiceProvider.GetRequiredService<IJwtService>();
        HttpClient Client(Account a) { var c = factory.CreateClient(); c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", jwt.GenerateAccessToken(a)); return c; }
        return (group.Id, students[1].Id, Client(students[0].Account), Client(students[1].Account), Client(students[2].Account));
    }

    [Fact]
    public async Task TaskPermissionsConcurrencyChecklistAndArchiveMatchJava()
    {
        var s = await TaskScope(); using var leader = s.Leader; using var member = s.Assignee; using var other = s.Other;
        var route = $"/api/groups/{s.Group}/tasks";
        var created = await leader.PostAsJsonAsync(route, new { title = "  First task  ", assigneeStudentIds = new[] { s.Member }, dueAt = DateTime.UtcNow.AddDays(1) });
        var task = await Data(created, HttpStatusCode.Created); var id = task.GetProperty("id").GetInt64();
        task.GetProperty("title").GetString().Should().Be("First task"); task.GetProperty("status").GetString().Should().Be("BACKLOG");
        (await member.PostAsJsonAsync(route, new { title = "No permission" })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await other.PatchAsJsonAsync(route + $"/{id}/move", new { status = "DONE", position = 0, version = 0 })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await member.PatchAsJsonAsync(route + $"/{id}/move", new { status = "BACKLOG", position = 0, version = 0 })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var moved = await Data(await member.PatchAsJsonAsync(route + $"/{id}/move", new { status = "IN_PROGRESS", position = 0, version = 0 }), HttpStatusCode.OK);
        moved.GetProperty("version").GetInt64().Should().Be(1);
        var stale = await leader.PatchAsJsonAsync(route + $"/{id}", new { title = "stale", version = 0 }); stale.StatusCode.Should().Be(HttpStatusCode.Conflict);
        (await stale.Content.ReadAsStringAsync()).Should().Contain("The resource has been modified by another transaction. Please reload and try again.");
        var checklist = await Data(await member.PostAsJsonAsync(route + $"/{id}/checklist-items", new { title = "Verify" }), HttpStatusCode.Created);
        var itemId = checklist.GetProperty("id").GetInt64();
        (await other.PatchAsJsonAsync(route + $"/{id}/checklist-items/{itemId}", new { completed = true })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await Data(await member.PatchAsJsonAsync(route + $"/{id}/checklist-items/{itemId}", new { completed = true }), HttpStatusCode.OK);
        var detail = await Data(await leader.GetAsync(route + $"/{id}"), HttpStatusCode.OK); detail.GetProperty("checklistProgressPercent").GetInt32().Should().Be(100);
        await Data(await member.DeleteAsync(route + $"/{id}/checklist-items/{itemId}"), HttpStatusCode.OK);
        var mine = await Data(await member.GetAsync($"/api/tasks/me?groupId={s.Group}&status=IN_PROGRESS"), HttpStatusCode.OK);
        mine.GetProperty("totalElements").GetInt64().Should().Be(1);
        var unassigned = await Data(await leader.PutAsJsonAsync(route + $"/{id}/assignees", new { assigneeStudentIds = Array.Empty<long>(), version = 1 }), HttpStatusCode.OK);
        unassigned.GetProperty("assignees").GetArrayLength().Should().Be(0);
        (await member.PatchAsJsonAsync(route + $"/{id}/move", new { status = "DONE", position = 0, version = 2 })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await Data(await leader.PostAsJsonAsync(route + "/reorder", new { taskId = id, targetStatus = "DONE", targetIndex = 0 }), HttpStatusCode.OK);
        var comment = await Data(await other.PostAsJsonAsync(route + $"/{id}/comments", new { content = "hello" }), HttpStatusCode.Created);
        var commentId = comment.GetProperty("id").GetInt64();
        (await member.PatchAsJsonAsync(route + $"/{id}/comments/{commentId}", new { content = "edit" })).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        await Data(await leader.DeleteAsync(route + $"/{id}/comments/{commentId}"), HttpStatusCode.OK);
        await Data(await leader.DeleteAsync(route + $"/{id}"), HttpStatusCode.OK);
        (await member.PostAsJsonAsync(route + $"/{id}/comments", new { content = "archived" })).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        await Data(await leader.PostAsync(route + $"/{id}/restore", null), HttpStatusCode.OK);
        var board = await Data(await leader.GetAsync($"/api/groups/{s.Group}/board"), HttpStatusCode.OK);
        board.GetProperty("columns").GetArrayLength().Should().Be(5); board.GetProperty("activeTaskCount").GetInt32().Should().Be(1);
        var activities = await Data(await leader.GetAsync(route + $"/{id}/activities"), HttpStatusCode.OK);
        activities.GetProperty("content").EnumerateArray().Select(x => x.GetProperty("activityType").GetString()).Should().Contain(["TASK_CREATED", "TASK_MOVED", "TASK_ARCHIVED", "TASK_RESTORED", "CHECKLIST_ITEM_UPDATED"]);
    }

    [Fact]
    public async Task ConcurrentTaskMovesConflictAndBoardsHaveIndependentOrdering()
    {
        var s = await TaskScope(); using var leader = s.Leader; using var member = s.Assignee; using var other = s.Other;
        var route = $"/api/groups/{s.Group}/tasks";
        var task = await Data(await leader.PostAsJsonAsync(route, new { title = "Concurrent" }), HttpStatusCode.Created); var id = task.GetProperty("id").GetInt64();
        var responses = await Task.WhenAll(leader.PatchAsJsonAsync(route + $"/{id}/move", new { status = "TODO", position = 0, version = 0 }), leader.PatchAsJsonAsync(route + $"/{id}/move", new { status = "DONE", position = 0, version = 0 }));
        responses.Select(x => x.StatusCode).Should().BeEquivalentTo([HttpStatusCode.OK, HttpStatusCode.Conflict]);
        var second = await Data(await leader.PostAsJsonAsync($"/api/groups/{s.Group}/boards", new { name = "Second" }), HttpStatusCode.Created);
        var boardId = second.GetProperty("id").GetInt64();
        var isolated = await Data(await leader.PostAsJsonAsync(route, new { title = "Other board", boardId }), HttpStatusCode.Created); isolated.GetProperty("position").GetInt64().Should().Be(0);
        var board = await Data(await leader.GetAsync($"/api/groups/{s.Group}/boards/{boardId}"), HttpStatusCode.OK); board.GetProperty("activeTaskCount").GetInt32().Should().Be(1);
        (await Data(await leader.GetAsync($"/api/groups/{s.Group}/boards"), HttpStatusCode.OK)).GetArrayLength().Should().Be(2);
    }

    [Fact]
    public async Task BoardDefaultPromotionAndDeletionAreTransactional()
    {
        var s = await TaskScope(); using var leader = s.Leader; using var member = s.Assignee; using var other = s.Other;
        var route = $"/api/groups/{s.Group}/boards";
        var initial = await Task.WhenAll(leader.GetAsync(route), member.GetAsync(route));
        var first = await Data(initial[0], HttpStatusCode.OK); var second = await Data(initial[1], HttpStatusCode.OK);
        var defaultId = first[0].GetProperty("id").GetInt64(); second[0].GetProperty("id").GetInt64().Should().Be(defaultId);
        var created = await Data(await leader.PostAsJsonAsync(route, new { name = "  Second  " }), HttpStatusCode.Created);
        created.GetProperty("name").GetString().Should().Be("Second"); var id = created.GetProperty("id").GetInt64();
        var memberCreate = await member.PostAsJsonAsync(route, new { name = "Denied" }); memberCreate.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        (await memberCreate.Content.ReadAsStringAsync()).Should().Contain("Only the group leader is authorized to create task boards");
        await Data(await leader.PatchAsJsonAsync(route + "/" + id, new { defaultBoard = true }), HttpStatusCode.OK);
        await Data(await leader.PatchAsJsonAsync(route + "/" + defaultId, new { defaultBoard = true }), HttpStatusCode.OK);
        var archiveDefault = await leader.PatchAsJsonAsync(route + "/" + defaultId, new { archived = true }); archiveDefault.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await archiveDefault.Content.ReadAsStringAsync()).Should().Contain("Cannot archive the default board");
        (await leader.DeleteAsync(route + "/" + defaultId)).StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var invalidRename = await leader.PatchAsJsonAsync(route + "/" + id, new { name = new string('x', 256), defaultBoard = true });
        invalidRename.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await invalidRename.Content.ReadAsStringAsync()).Should().Contain("Board name must be at most 255 characters");
        await Data(await leader.DeleteAsync(route + "/" + id), HttpStatusCode.OK);
        var populated = await Data(await leader.PostAsJsonAsync(route, new { name = "Populated" }), HttpStatusCode.Created);
        var populatedId = populated.GetProperty("id").GetInt64();
        await Data(await leader.PostAsJsonAsync($"/api/groups/{s.Group}/tasks", new { title = "Keep", boardId = populatedId }), HttpStatusCode.Created);
        (await leader.DeleteAsync(route + "/" + populatedId)).StatusCode.Should().Be(HttpStatusCode.Conflict);
        await Data(await leader.PatchAsJsonAsync(route + "/" + populatedId, new { archived = true }), HttpStatusCode.OK);
        (await Data(await leader.GetAsync(route), HttpStatusCode.OK)).GetArrayLength().Should().Be(1);
        await using var scope = factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<FlowzyDbContext>();
        (await db.TaskBoards.CountAsync(x => x.GroupId == s.Group && x.DefaultBoard)).Should().Be(1);
    }

    [Fact]
    public async Task ClosedTermMakesStudentTasksAndBoardsReadOnlyButAdminCanRemoveMember()
    {
        var s = await TaskScope(); using var leader = s.Leader; using var member = s.Assignee; using var other = s.Other;
        var route = $"/api/groups/{s.Group}";
        await Data(await leader.PostAsJsonAsync(route + "/tasks", new { title = "Existing" }), HttpStatusCode.Created);
        var termCode = "C" + Guid.NewGuid().ToString("N")[..12];
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FlowzyDbContext>();
            db.AcademicTerms.Add(new AcademicTerm { Code = termCode, Status = "CLOSED" }); await db.SaveChangesAsync();
            var group = await db.StudentGroups.FindAsync(s.Group); group!.Term = termCode; await db.SaveChangesAsync();
        }
        foreach (var path in new[] { "/tasks", "/boards" })
        {
            var response = await leader.PostAsJsonAsync(route + path, new { name = "New", title = "New" });
            response.StatusCode.Should().Be(HttpStatusCode.Conflict);
            (await response.Content.ReadAsStringAsync()).Should().Contain($"Academic term {termCode} has ended; this group is read-only for students");
        }
        (await leader.GetAsync(route + "/board")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await member.PostAsync(route + "/leave", null)).StatusCode.Should().Be(HttpStatusCode.Conflict);
        using var admin = await Admin();
        await Data(await admin.DeleteAsync(route + $"/members/{s.Member}"), HttpStatusCode.OK);
    }

    [Fact]
    public async Task MemberRemovalCleansActiveAssignmentsButRestoreCleansArchivedAssignments()
    {
        var s = await TaskScope(); using var leader = s.Leader; using var member = s.Assignee; using var other = s.Other;
        var route = $"/api/groups/{s.Group}/tasks";
        var active = await Data(await leader.PostAsJsonAsync(route, new { title = "Active", assigneeStudentIds = new[] { s.Member } }), HttpStatusCode.Created);
        var archived = await Data(await leader.PostAsJsonAsync(route, new { title = "Archived", assigneeStudentIds = new[] { s.Member } }), HttpStatusCode.Created);
        var activeId = active.GetProperty("id").GetInt64(); var archivedId = archived.GetProperty("id").GetInt64();
        await Data(await leader.DeleteAsync(route + "/" + archivedId), HttpStatusCode.OK);
        await Data(await leader.DeleteAsync($"/api/groups/{s.Group}/members/{s.Member}"), HttpStatusCode.OK);
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FlowzyDbContext>();
            (await db.TaskAssignees.AnyAsync(x => x.TaskId == activeId)).Should().BeFalse();
            (await db.TaskAssignees.AnyAsync(x => x.TaskId == archivedId)).Should().BeTrue();
            (await db.TaskActivities.CountAsync(x => x.TaskId == activeId && x.ActivityType == "ASSIGNEE_REMOVED_FROM_GROUP" && x.ActorAccountId == null)).Should().Be(1);
            var removed = await db.Notifications.SingleAsync(x => x.EventKey == $"GROUP_MEMBER_REMOVED:{s.Group}:{s.Member}");
            removed.ActionKey.Should().Be("OPEN_GROUP"); removed.Payload.Should().BeNull();
        }
        (await member.GetAsync(route + "/" + activeId)).StatusCode.Should().Be(HttpStatusCode.Forbidden);
        var restored = await Data(await leader.PostAsync(route + $"/{archivedId}/restore", null), HttpStatusCode.OK);
        restored.GetProperty("assignees").GetArrayLength().Should().Be(0);
        await using var check = factory.Services.CreateAsyncScope(); var store = check.ServiceProvider.GetRequiredService<FlowzyDbContext>();
        (await store.TaskActivities.CountAsync(x => x.TaskId == archivedId && x.ActivityType == "ASSIGNEE_REMOVED_FROM_GROUP" && x.ActorAccountId == null)).Should().Be(1);
    }

    [Fact]
    public async Task TaskNotificationsPreserveRecipientsEventKeysAndActionParameters()
    {
        var s = await TaskScope(); using var leader = s.Leader; using var member = s.Assignee; using var other = s.Other;
        long leaderId;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FlowzyDbContext>();
            leaderId = (await db.StudentGroups.FindAsync(s.Group))!.LeaderStudentId!.Value;
        }
        var route = $"/api/groups/{s.Group}/tasks";
        var task = await Data(await leader.PostAsJsonAsync(route, new { title = "Notifications", assigneeStudentIds = new[] { leaderId, s.Member } }), HttpStatusCode.Created);
        var id = task.GetProperty("id").GetInt64();
        // A no-op reorder logs activity but must not consume an optimistic concurrency version.
        var same = await Data(await leader.PatchAsJsonAsync(route + $"/{id}/move", new { status = "BACKLOG", position = 0, version = 0 }), HttpStatusCode.OK);
        same.GetProperty("version").GetInt64().Should().Be(0);
        await Data(await member.PatchAsJsonAsync(route + $"/{id}/move", new { status = "DONE", position = 0, version = 0 }), HttpStatusCode.OK);
        await using var verify = factory.Services.CreateAsyncScope(); var store = verify.ServiceProvider.GetRequiredService<FlowzyDbContext>();
        var assigned = await store.Notifications.Where(x => x.EventKey == $"TASK_ASSIGNED:{id}:0").ToListAsync();
        assigned.Should().HaveCount(2, "creation also notifies a leader who assigns themselves");
        assigned.Should().OnlyContain(x => x.Payload == null && x.ActionKey == "OPEN_TASK");
        var moved = await store.Notifications.SingleAsync(x => x.EventKey == $"TASK_STATUS_CHANGED:{id}:1");
        using var parameters = JsonDocument.Parse(moved.ActionParams!);
        parameters.RootElement.GetProperty("oldStatus").GetString().Should().Be("BACKLOG");
        parameters.RootElement.GetProperty("newStatus").GetString().Should().Be("DONE");
        moved.RecipientId.Should().Be(await store.Students.Where(x => x.Id == leaderId).Select(x => x.AccountId).SingleAsync());
    }

    [Fact]
    public async Task ImportsRunInBackgroundReportRowsAndReactivateWithoutGroups()
    {
        using var client = await Admin(); var code = "ROSTER" + Guid.NewGuid().ToString("N")[..8].ToUpperInvariant(); var email = code.ToLowerInvariant() + "@example.com";
        var csv = $"RollNumber,Fullname,Email,SubjectCode,GroupName\n{code},Test Student,{email},EXE101,SE01\nBAD,Invalid,bad-email,OTHER,SE02\n{code},Duplicate,{email},EXE101,SE01\n";
        var batch = await UploadAndWait(client, "student-accounts", "roster.csv", Encoding.UTF8.GetBytes(csv));
        batch.GetProperty("status").GetString().Should().Be("COMPLETED"); batch.GetProperty("successRows").GetInt32().Should().Be(1); batch.GetProperty("failedRows").GetInt32().Should().Be(2);
        var id = batch.GetProperty("id").GetInt64(); var errors = await Data(await client.GetAsync($"/api/imports/{id}/errors?errorCode=INVALID_EMAIL"), HttpStatusCode.OK);
        errors.GetProperty("totalElements").GetInt64().Should().Be(1); errors.GetProperty("content")[0].GetProperty("rowNumber").GetInt32().Should().Be(3);
        string originalHash;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FlowzyDbContext>(); var student = await db.Students.Include(x => x.Account).SingleAsync(x => x.StudentCode == code);
            originalHash = student.Account.PasswordHash; student.Account.MustChangePassword.Should().BeFalse(); student.ClassName.Should().Be("SE01");
            (await db.StudentGroupMembers.AnyAsync(x => x.StudentId == student.Id)).Should().BeFalse(); student.Status = "INACTIVE"; student.Account.Status = "INACTIVE"; await db.SaveChangesAsync();
        }
        var reactivated = await UploadAndWait(client, "student-accounts", "roster.csv", Encoding.UTF8.GetBytes($"RollNumber,Fullname,Email,SubjectCode,GroupName\n{code},Updated,{email},EXE201,SE03\n"));
        reactivated.GetProperty("successRows").GetInt32().Should().Be(1);
        await using var verifyScope = factory.Services.CreateAsyncScope(); var verify = verifyScope.ServiceProvider.GetRequiredService<FlowzyDbContext>();
        var updated = await verify.Students.Include(x => x.Account).SingleAsync(x => x.StudentCode == code); updated.Status.Should().Be("ACTIVE"); updated.FullName.Should().Be("Updated"); updated.Account.PasswordHash.Should().Be(originalHash);
        var malformed = await UploadAndWait(client, "student-accounts", "bad.csv", Encoding.UTF8.GetBytes("email,role\nx@example.com,ADMIN")); malformed.GetProperty("status").GetString().Should().Be("FAILED");
    }

    [Fact]
    public async Task ProblemImportUpsertsAndStudentWorkbookCreatesLeaderAndMentor()
    {
        using var client = await Admin(); var suffix = Guid.NewGuid().ToString("N")[..8];
        var csv = $"type,code,name,title,statement,difficulty_level,domain_code\ndomain,D-{suffix},Domain,,,,\nproblem,P-{suffix},,Problem,Question,beginer,D-{suffix}\n";
        var batch = await UploadAndWait(client, "problem-bank", "problems.csv", Encoding.UTF8.GetBytes(csv)); batch.GetProperty("successRows").GetInt32().Should().Be(2);
        var update = await UploadAndWait(client, "problem-bank", "problems.csv", Encoding.UTF8.GetBytes(csv.Replace("Problem,Question", "Updated,Revised", StringComparison.Ordinal))); update.GetProperty("successRows").GetInt32().Should().Be(2);
        var mentorCode = "M" + suffix;
        await UploadAndWait(client, "mentors", "mentors.csv", Encoding.UTF8.GetBytes($"mentor_code,full_name,email,years_of_experience\n{mentorCode},Mentor,{mentorCode}@example.com,Trên 10 năm\n"));
        var bytes = ImportTemplates.Create("STUDENT"); using var stream = new MemoryStream(bytes); using var workbook = WorkbookFactory.Create(stream);
        var sheet = workbook.GetSheetAt(0); var row = sheet.GetRow(1); row.GetCell(0).SetCellValue(suffix); row.GetCell(1).SetCellValue("Imported " + suffix);
        row.GetCell(6).SetCellValue("Nguyễn Văn An"); row.GetCell(7).SetCellValue("ST" + suffix); row.GetCell(10).SetCellValue("student" + suffix + "@example.com"); row.GetCell(12).SetCellValue("Nguyen Van An"); row.GetCell(17).SetCellValue("Mentor (" + mentorCode + ")");
        using var output = new MemoryStream(); workbook.Write(output, true);
        var imported = await UploadAndWait(client, "students", "groups.xlsx", output.ToArray()); imported.GetProperty("successRows").GetInt32().Should().Be(1);
        imported.GetProperty("status").GetString().Should().Be("COMPLETED");
        await using var scope = factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<FlowzyDbContext>();
        var group = await db.StudentGroups.Include(x => x.LeaderStudent).Include(x => x.Mentor).SingleAsync(x => x.Name == "Imported " + suffix);
        group.LeaderStudent!.FullName.Should().Be("Nguyễn Văn An"); group.Mentor!.MentorCode.Should().Be(mentorCode); group.Mentor.YearsOfExperience.Should().Be(10);
        var problem = await db.Problems.Include(x => x.Domain).SingleAsync(x => x.Code == "P-" + suffix); problem.Title.Should().Be("Updated"); problem.DifficultyLevel.Should().Be("BEGINNER"); problem.Domain!.Code.Should().Be("D-" + suffix);
        (await db.TaskBoards.CountAsync(x => x.GroupId == group.Id && x.DefaultBoard)).Should().Be(1);
    }

    [Fact]
    public async Task ImportTemplatesAndAuthorizationMatchContract()
    {
        using var admin = await Admin(); using var anonymous = factory.CreateClient();
        foreach (var name in new[] { "students", "mentors", "problem-bank", "student-accounts" })
        {
            (await anonymous.GetAsync("/api/imports/templates/" + name)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            var template = await admin.GetAsync("/api/imports/templates/" + name); template.StatusCode.Should().Be(HttpStatusCode.OK);
            template.Content.Headers.ContentType!.MediaType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
            template.Content.Headers.ContentDisposition!.DispositionType.Should().Be("attachment");
            using var input = new MemoryStream(await template.Content.ReadAsByteArrayAsync()); using var book = WorkbookFactory.Create(input); book.NumberOfSheets.Should().Be(name == "problem-bank" ? 2 : 1);
        }
        var s = await TaskScope(); using var student = s.Leader; using var member = s.Assignee; using var other = s.Other;
        (await student.GetAsync("/api/imports/templates/student-accounts")).StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task<HttpClient> Admin()
    {
        var client = factory.CreateClient(); await using var scope = factory.Services.CreateAsyncScope(); var db = scope.ServiceProvider.GetRequiredService<FlowzyDbContext>();
        var account = await db.Accounts.SingleAsync(x => x.Email == "admin.integration@local.test"); account.MustChangePassword = false; await db.SaveChangesAsync();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateAccessToken(account)); return client;
    }
    private static async Task<JsonElement> UploadAndWait(HttpClient client, string route, string name, byte[] bytes)
    {
        using var form = new MultipartFormDataContent(); form.Add(new ByteArrayContent(bytes), "file", name);
        var queued = await Data(await client.PostAsync("/api/imports/" + route, form), HttpStatusCode.Accepted);
        queued.GetProperty("status").GetString().Should().Be("QUEUED"); var id = queued.GetProperty("batchId").GetInt64();
        for (var i = 0; i < 100; i++)
        {
            var result = await Data(await client.GetAsync("/api/imports/" + id), HttpStatusCode.OK);
            if (result.GetProperty("status").GetString() is "COMPLETED" or "FAILED") return result;
            await Task.Delay(100);
        }
        throw new TimeoutException("Import did not finish");
    }
    private static async Task<JsonElement> Data(HttpResponseMessage response, HttpStatusCode expected)
    {
        var body = await response.Content.ReadAsStringAsync(); response.StatusCode.Should().Be(expected, body);
        using var doc = JsonDocument.Parse(body); return doc.RootElement.GetProperty("data").Clone();
    }
}
