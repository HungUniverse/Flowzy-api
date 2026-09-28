using System.Globalization;
using System.Text.Json;
using Flowzy.Repository.Entities;
using Flowzy.Repository.Repositories;
using Flowzy.Service.Contracts;
using Flowzy.Service.Exceptions;
using Flowzy.Service.Groups;

namespace Flowzy.Service.Grades;

public sealed partial class GradeMatrixService(IGradeMatrixRepository repository, TimeProvider clock) : IGradeMatrixService
{
    private DateTime Now => clock.GetUtcNow().UtcDateTime;
    public async Task<object> Grade(long mid, long gid, UpsertGroupGradeRequest request, string email, CancellationToken ct)
    {
        await using var tx = await repository.Begin(ct);
        // Coordinate with timeline maxScore/weight edits; group lock also serializes first-grade versus revision creation.
        var instructor = await Instructor(email, true, ct); var milestone = await WritableMilestone(mid, ct);
        var group = await Group(gid, true, ct); Applies(milestone, group);
        if (milestone.InstructorId != instructor.Id || group.InstructorId != instructor.Id) throw new ForbiddenException("You do not manage this milestone and group");
        var score = request.Score!.Value;
        if (score > milestone.MaxScore) throw new BadRequestException("Score must be between 0.00 and " + Number(milestone.MaxScore));
        var grade = (await repository.Grades(gid, ct)).FirstOrDefault(x => x.MilestoneId == mid);
        var members = await ActiveMembers(gid, ct); var scores = (await repository.Scores(gid, ct)).Where(x => x.MilestoneId == mid).ToList();
        if (!members.Select(x => x.StudentId).ToHashSet().SetEquals(scores.Select(x => x.StudentId)))
            throw new BadRequestException("Contribution must be provided for every active group member before grading");
        if (grade is null)
        {
            var revision = await repository.LockRevision(mid, gid, ct);
            if (revision is null || Summary(revision, members, await repository.Agreements(revision.Id, ct), false).Status != "AGREED")
                throw new ConflictException("Every active member must agree to the contribution revision before grading");
        }
        var changed = grade is null || grade.Score != score || (decimal.GetBits(grade.Score)[3] & 0x00FF0000) != (decimal.GetBits(score)[3] & 0x00FF0000) || grade.Feedback != request.Feedback;
        if (grade is null) { grade = new MilestoneGroupGrade { MilestoneId = mid, GroupId = gid, CreatedAt = Now }; repository.Add(grade); }
        grade.InstructorId = instructor.Id; grade.Score = score; grade.MaxScoreSnapshot = milestone.MaxScore; grade.WeightSnapshot = milestone.Weight ?? 0;
        grade.Feedback = request.Feedback; grade.GradedAt = Now; grade.UpdatedAt = Now; await repository.Save(ct);
        foreach (var row in scores)
        {
            row.GroupGradeId = grade.Id; row.MaxScoreSnapshot = grade.MaxScoreSnapshot; row.WeightSnapshot = grade.WeightSnapshot;
            row.CalculatedScore = Round(Round(score * row.ContributionPercent / 100m, 8), 4); row.UpdatedAt = Now;
        }
        if (changed) await Notify(members.Select(x => x.Student.AccountId), "MILESTONE_GROUP_GRADED", "Milestone Graded",
            $"Milestone '{milestone.Title}' for group {group.Name} has been graded: {Number(score)}/{Number(milestone.MaxScore)}.",
            gid, mid, "MilestoneGroupGrade", grade.Id, $"MILESTONE_GROUP_GRADED:{grade.Id}:{Stamp(grade.GradedAt)}", ct, grade.Id);
        await repository.Save(ct); await tx.CommitAsync(ct); return GradeDto(grade, true, true);
    }

    public async Task<object> Contributions(long gid, long mid, UpsertContributionsRequest request, string email, CancellationToken ct)
    {
        await using var tx = await repository.Begin(ct);
        var leader = await Student(email, ct); var milestone = await WritableMilestone(mid, ct);
        var group = await Group(gid, true, ct); StudentTermWriteGuard.RequireWritable(group); Applies(milestone, group);
        if (group.LeaderStudentId != leader.Id) throw new ForbiddenException("Only the group leader can update milestone contributions");
        if ((await repository.Grades(gid, ct)).Any(x => x.MilestoneId == mid)) throw new ConflictException("Contributions are locked after the instructor grades this milestone");
        var members = await ActiveMembers(gid, ct); var items = new Dictionary<long, ContributionItem>();
        foreach (var item in request.Items)
            if (!items.TryAdd(item.StudentId!.Value, item)) throw new BadRequestException("Duplicate contribution for student id: " + item.StudentId);
        if (!members.Select(x => x.StudentId).ToHashSet().SetEquals(items.Keys)) throw new BadRequestException("Contribution must be provided for every active group member and no unrelated students");
        var existing = (await repository.Scores(gid, ct)).Where(x => x.MilestoneId == mid).ToList(); var saved = new List<MilestoneMemberScore>();
        foreach (var member in members)
        {
            var row = existing.FirstOrDefault(x => x.StudentId == member.StudentId);
            if (row is null) { row = new MilestoneMemberScore { MilestoneId = mid, GroupId = gid, StudentId = member.StudentId, Student = member.Student, CreatedAt = Now }; repository.Add(row); }
            row.GroupGradeId = null; row.ContributionPercent = items[member.StudentId].ContributionPercent!.Value;
            row.CalculatedScore = null; row.MaxScoreSnapshot = null; row.WeightSnapshot = null; row.UpdatedAt = Now; saved.Add(row);
        }
        // Do not silently delete stale rows: Java later rejects a grade if its contribution set contains unrelated students.
        var revision = await repository.LockRevision(mid, gid, ct);
        if (revision is null) { revision = new MilestoneContributionRevision { MilestoneId = mid, GroupId = gid, Revision = 1, CreatedAt = Now }; repository.Add(revision); }
        else revision.Revision++;
        revision.SubmittedByStudentId = leader.Id; revision.UpdatedAt = Now; await repository.Save(ct);
        repository.ClearAgreements(await repository.Agreements(revision.Id, ct));
        await Notify(members.Select(x => x.Student.AccountId), "CONTRIBUTION_REVISION_CREATED", "Contribution Review Required",
            $"Review contribution revision {revision.Revision} for {milestone.Title}.", gid, mid, "MilestoneContributionRevision", revision.Id,
            $"CONTRIBUTION_REVISION_CREATED:{revision.Id}:{revision.Revision}", ct);
        await repository.Save(ct); await tx.CommitAsync(ct);
        return saved.Select(x => new { x.Id, x.MilestoneId, x.GroupId, x.StudentId, x.Student.StudentCode, StudentName = x.Student.FullName,
            x.ContributionPercent, x.CalculatedScore, x.MaxScoreSnapshot, x.WeightSnapshot }).ToList();
    }

    public async Task<object> Agreement(long gid, long mid, ContributionAgreementRequest request, string email, CancellationToken ct)
    {
        await using var tx = await repository.Begin(ct);
        var student = await Student(email, ct); var milestone = await WritableMilestone(mid, ct);
        var group = await Group(gid, true, ct); StudentTermWriteGuard.RequireWritable(group); Applies(milestone, group);
        var members = await ActiveMembers(gid, ct);
        if (members.All(x => x.StudentId != student.Id)) throw new ForbiddenException("Only active group members can respond to contributions");
        if ((await repository.Grades(gid, ct)).Any(x => x.MilestoneId == mid)) throw new ConflictException("Contribution agreements are locked after grading");
        var revision = await repository.LockRevision(mid, gid, ct) ?? throw new ConflictException("The group leader must submit contributions first");
        var agreements = await repository.Agreements(revision.Id, ct);
        if (agreements.Any(x => x.Decision == "REQUEST_CHANGES")) throw new ConflictException("The leader must submit a new revision before more responses are accepted");
        var reason = request.Reason?.Trim();
        if (request.Decision == "REQUEST_CHANGES" && string.IsNullOrEmpty(reason)) throw new BadRequestException("A reason is required when requesting changes");
        var agreement = agreements.FirstOrDefault(x => x.StudentId == student.Id);
        if (agreement is null) { agreement = new MilestoneContributionAgreement { RevisionId = revision.Id, StudentId = student.Id, CreatedAt = Now }; repository.Add(agreement); agreements.Add(agreement); }
        agreement.Decision = request.Decision; agreement.Reason = request.Decision == "REQUEST_CHANGES" ? reason : null; agreement.RespondedAt = Now; agreement.UpdatedAt = Now;
        await repository.Save(ct); var summary = Summary(revision, members, agreements, false);
        if (agreement.Decision == "REQUEST_CHANGES" && group.LeaderStudent is not null)
            await Notify([group.LeaderStudent.AccountId], "CONTRIBUTION_CHANGES_REQUESTED", "Contribution Changes Requested",
                $"{student.FullName} requested changes for {milestone.Title}.", gid, mid, "MilestoneContributionAgreement", agreement.Id,
                "CONTRIBUTION_CHANGES_REQUESTED:" + agreement.Id, ct);
        else if (summary.Status == "AGREED")
        {
            var recipients = new List<long>(); if (group.LeaderStudent is not null) recipients.Add(group.LeaderStudent.AccountId); if (group.Instructor is not null) recipients.Add(group.Instructor.AccountId);
            await Notify(recipients, "CONTRIBUTION_AGREED", "Contributions Agreed", $"All members agreed to contributions for {milestone.Title}.",
                gid, mid, "MilestoneContributionRevision", revision.Id, "CONTRIBUTION_AGREED:" + revision.Id, ct);
        }
        await repository.Save(ct); await tx.CommitAsync(ct);
        return new { revision.Revision, summary.Status, summary.ApprovedCount, summary.RequiredCount, StudentId = student.Id, agreement.Decision, agreement.Reason, agreement.RespondedAt };
    }

    public async Task<object> Matrix(long gid, string email, CancellationToken ct)
    { var group = await Group(gid, false, ct); await View(group, email, ct); return await Build(group, ct); }

    private async Task<GroupGradeMatrix> Build(StudentGroup group, CancellationToken ct)
    {
        if (group.InstructorId is null) throw new ConflictException("Group must be assigned an instructor before grading milestones");
        var milestones = await Visible(group.InstructorId.Value, group.Term.Trim(), group.CourseCode.Trim(), ct);
        if (milestones.Count == 0 && (await repository.ScopeMilestones(group.Term.Trim(), group.CourseCode.Trim(), ct)).Any(x => x.Status is not ("ARCHIVED" or "INACTIVE")))
            throw new ConflictException("No active milestones are owned by this group's assigned instructor; check group instructor assignment or milestone owner");
        var members = await ActiveMembers(group.Id, ct); var grades = await repository.Grades(group.Id, ct); var scores = await repository.Scores(group.Id, ct);
        var revisions = await repository.Revisions(group.Id, ct); var agreements = new Dictionary<long, List<MilestoneContributionAgreement>>();
        foreach (var revision in revisions) agreements[revision.Id] = await repository.Agreements(revision.Id, ct);
        var columns = new List<MatrixColumn>();
        foreach (var milestone in milestones)
        {
            var grade = grades.FirstOrDefault(x => x.MilestoneId == milestone.Id); var rows = scores.Where(x => x.MilestoneId == milestone.Id).ToList();
            var contributionComplete = members.All(x => rows.Any(s => s.StudentId == x.StudentId));
            var gradeComplete = grade is not null && members.All(x => rows.Any(s => s.StudentId == x.StudentId && s.CalculatedScore is not null));
            var revision = revisions.FirstOrDefault(x => x.MilestoneId == milestone.Id);
            var summary = Summary(revision, members, revision is null ? [] : agreements[revision.Id], grade is not null);
            columns.Add(new(milestone.Id, milestone.Title, milestone.Weight ?? 0, milestone.MaxScore, grade is null ? null : GradeDto(grade, contributionComplete, gradeComplete),
                grade is not null, contributionComplete, summary.Status, revision?.Revision ?? 0, summary.ApprovedCount, summary.RequiredCount, gradeComplete));
        }
        var memberRows = new List<MatrixMember>();
        foreach (var member in members)
        {
            decimal total = 0.0000m; var complete = true; var memberScores = new List<MatrixScore>();
            foreach (var milestone in milestones)
            {
                var row = scores.FirstOrDefault(x => x.MilestoneId == milestone.Id && x.StudentId == member.StudentId);
                var revision = revisions.FirstOrDefault(x => x.MilestoneId == milestone.Id);
                var agreement = revision is null ? null : agreements[revision.Id].FirstOrDefault(x => x.StudentId == member.StudentId);
                var scoreComplete = row?.CalculatedScore is not null;
                if (!scoreComplete) complete = false;
                else total += Weighted(row!.CalculatedScore!.Value, row.MaxScoreSnapshot!.Value, row.WeightSnapshot!.Value);
                memberScores.Add(new(milestone.Id, row?.ContributionPercent, row?.CalculatedScore, agreement?.Decision, agreement?.Reason, agreement?.RespondedAt, scoreComplete));
            }
            memberRows.Add(new(member.StudentId, member.Student.StudentCode, member.Student.FullName, Round(total, 4), complete, memberScores));
        }
        return new(group.Id, group.Name, group.GroupNo, group.Term, group.CourseCode, columns, memberRows, columns.Count > 0 && columns.All(x => x.GradeComplete));
    }

    private sealed record AgreementSummary(string Status, int ApprovedCount, int RequiredCount);
    private static AgreementSummary Summary(MilestoneContributionRevision? revision, List<StudentGroupMember> members, List<MilestoneContributionAgreement> agreements, bool graded)
    {
        var required = members.Count; if (graded) return new("AGREED", required, required); if (revision is null) return new("NOT_SUBMITTED", 0, required);
        var active = agreements.Where(x => members.Any(m => m.StudentId == x.StudentId)).ToList(); var approved = active.Count(x => x.Decision == "AGREE");
        return new(active.Any(x => x.Decision == "REQUEST_CHANGES") ? "CHANGES_REQUESTED" : approved == required && required > 0 ? "AGREED" : "PENDING", approved, required);
    }
    private async Task<List<CourseMilestone>> Visible(long instructor, string? term, string? course, CancellationToken ct) => (await repository.OwnedMilestones(instructor, ct))
        .Where(x => (term is null || Same(x.Term, term)) && (course is null || Same(x.CourseCode, course)) && x.Status is not ("ARCHIVED" or "INACTIVE"))
        .OrderBy(x => x.Term, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.CourseCode, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Position).ThenBy(x => x.Id).ToList();
    private async Task<List<StudentGroupMember>> ActiveMembers(long group, CancellationToken ct) => (await repository.Members(group, ct))
        .Where(x => x.Student.Status == "ACTIVE" && x.Student.Account.Status == "ACTIVE").OrderBy(x => x.Student.StudentCode, StringComparer.OrdinalIgnoreCase).ToList();
    private async Task<CourseMilestone> WritableMilestone(long id, CancellationToken ct)
    {
        var m = await repository.Milestone(id, ct) ?? throw new NotFoundException("Timeline milestone not found");
        if (m.Status != "ACTIVE") throw new BadRequestException("Only active milestones can be graded");
        if ((m.Weight ?? 0) <= 0) throw new BadRequestException("Milestone weight must be greater than 0 to grade"); return m;
    }
    private async Task<StudentGroup> Group(long id, bool locked, CancellationToken ct) => await repository.Group(id, locked, ct) ?? throw new NotFoundException("Student group not found");
    private static void Applies(CourseMilestone m, StudentGroup g)
    {
        if (g.Status != "ACTIVE") throw new BadRequestException("Student group is not active");
        if (!Same(m.Term, g.Term) || !Same(m.CourseCode, g.CourseCode) || m.InstructorId is null || m.InstructorId != g.InstructorId) throw new BadRequestException("Milestone does not apply to this student group");
    }
    private async Task<Account> Account(string email, CancellationToken ct)
    { var a = await repository.Account(email, ct) ?? throw new UnauthorizedException("User not found"); if (a.Status != "ACTIVE") throw new ForbiddenException("Account is inactive"); return a; }
    private async Task<Instructor> Instructor(string email, bool locked, CancellationToken ct)
    {
        if ((await Account(email, ct)).Role != "INSTRUCTOR") throw new ForbiddenException("Only instructors can perform this action");
        var i = await repository.Instructor(email, locked, ct) ?? throw new ForbiddenException("Instructor profile not found");
        if (i.Status != "ACTIVE") throw new ForbiddenException("Instructor profile is inactive"); return i;
    }
    private async Task<Student> Student(string email, CancellationToken ct)
    { if ((await Account(email, ct)).Role != "STUDENT") throw new ForbiddenException("Only students can perform this action"); return await repository.Student(email, ct) ?? throw new ForbiddenException("Student profile not found"); }
    private async Task View(StudentGroup group, string email, CancellationToken ct)
    {
        var account = await Account(email, ct); if (account.Role == "ADMIN") return;
        if (account.Role == "INSTRUCTOR")
        { var instructor = await repository.Instructor(email, false, ct) ?? throw new ForbiddenException("Instructor profile not found"); if (group.InstructorId == instructor.Id) return; }
        if (account.Role == "STUDENT")
        { var student = await repository.Student(email, ct) ?? throw new ForbiddenException("Student profile not found"); if ((await repository.Members(group.Id, ct)).Any(x => x.StudentId == student.Id)) return; }
        throw new ForbiddenException("You do not have permission to view grades for this group");
    }
    private async Task Notify(IEnumerable<long> recipients, string type, string title, string body, long group, long milestone, string entityType, long entityId, string key, CancellationToken ct, long? grade = null)
    {
        var args = new Dictionary<string, string> { ["groupId"] = group.ToString(CultureInfo.InvariantCulture), ["milestoneId"] = milestone.ToString(CultureInfo.InvariantCulture) };
        if (grade is not null) args["gradeId"] = grade.Value.ToString(CultureInfo.InvariantCulture);
        foreach (var recipient in recipients.Distinct()) await repository.Notify(new Notification { RecipientId = recipient, Type = type, Title = title, Body = body,
            ActionKey = "OPEN_GRADES", ActionParams = JsonSerializer.Serialize(args), EntityType = entityType, EntityId = entityId.ToString(CultureInfo.InvariantCulture), EventKey = key, CreatedAt = Now, UpdatedAt = Now }, ct);
    }
    private static MatrixGrade GradeDto(MilestoneGroupGrade g, bool c, bool done) => new(g.Id, g.MilestoneId, g.GroupId, g.Score, g.MaxScoreSnapshot, g.WeightSnapshot, g.Feedback, g.InstructorId, g.GradedAt, c, done);
    private static bool Same(string? a, string? b) => a is not null && b is not null && string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
    private static decimal Round(decimal value, int scale) => decimal.Round(value, scale, MidpointRounding.AwayFromZero);
    private static decimal Weighted(decimal individual, decimal max, int weight) => Round(Round(individual / max, 8) * 10m * weight / 100m, 4);
    private static string Number(decimal value) => value.ToString(CultureInfo.InvariantCulture);
    private static string Stamp(DateTime value) => value.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fffffff'Z'", CultureInfo.InvariantCulture);
}
