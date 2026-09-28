using System.Text;
using System.Text.RegularExpressions;
using Flowzy.Repository.Entities;
using Flowzy.Service.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Service.Imports;

public sealed partial class ImportService
{
    private async Task ProcessGroups(long batchId, IReadOnlyList<(ImportRow Row, IReadOnlyList<ImportIssue> Issues)> results, CancellationToken ct)
    {
        var groups = results.Where(x => x.Row.Sheet is not null && Regex.IsMatch(x.Row.Sheet, "([A-Za-z]{2}\\d{2})[_-]([A-Za-z0-9_]+)") && x.Row.Value("group_no").Length != 0)
            .GroupBy(x => (Term: SheetScope(x.Row).Term, Name: NormalizedGroupName(GroupName(x.Row))));
        await using var tx = await db.Database.BeginTransactionAsync(ct);
        foreach (var grouping in groups)
        {
            var first = grouping.First().Row; var (termCode, course) = SheetScope(first);
            var term = await db.AcademicTerms.FirstOrDefaultAsync(x => x.Code.ToUpper() == termCode, ct);
            if (term is not null && term.Status != "OPEN") throw new ConflictException("Academic term is already closed: " + termCode);
            if (term is null)
            {
                var open = await db.AcademicTerms.Where(x => x.Status == "OPEN").OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).FirstOrDefaultAsync(ct);
                if (open is not null) throw new ConflictException("Close academic term " + open.Code + " before importing a new term");
                term = new AcademicTerm { Code = termCode, Status = "OPEN", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow }; db.AcademicTerms.Add(term); await db.SaveChangesAsync(ct);
            }
            var name = Truncate(Regex.Replace(GroupName(first).Trim(), "\\s+", " "), 255);
            var mentor = await ResolveMentor(batchId, first, ct);
            var candidates = await db.StudentGroups.Where(x => x.Term == termCode).ToListAsync(ct);
            var existing = candidates.FirstOrDefault(x => NormalizedGroupName(x.Name) == NormalizedGroupName(name));
            if (existing is not null)
            {
                existing = await db.StudentGroups.FromSqlInterpolated($"SELECT * FROM student_groups WHERE id={existing.Id} FOR UPDATE").FirstAsync(ct);
                existing.MentorId = mentor?.Id; existing.UpdatedAt = DateTime.UtcNow;
            }
            var selected = new List<Student>();
            foreach (var result in grouping)
            {
                if (result.Issues.Any(x => x.Code != "ALREADY_EXISTS")) continue;
                var code = result.Row.Value("student_code").ToLowerInvariant();
                var student = await db.Students.FirstOrDefaultAsync(x => x.StudentCode.ToLower() == code, ct); if (student is null) continue;
                var persistedTerm = existing?.Term ?? termCode; var persistedCourse = existing?.CourseCode ?? course;
                var membership = await db.StudentGroupMembers.FirstOrDefaultAsync(x => x.StudentId == student.Id && x.Group.Term == persistedTerm && x.Group.CourseCode == persistedCourse, ct);
                if (existing is not null && membership?.GroupId == existing.Id) continue;
                if (membership is not null || selected.Any(x => x.Id == student.Id))
                { await SaveIssues(batchId, result.Row, [new("group_no", "INVALID_VALUE", "Student already in a group for the same term and course")], ct); continue; }
                if (existing is not null)
                {
                    if (existing.IsLocked) throw new ConflictException("Group membership is locked. Unlock the group before changing members");
                    db.StudentGroupMembers.Add(new StudentGroupMember { GroupId = existing.Id, StudentId = student.Id, MemberRole = "MEMBER", JoinedAt = DateTime.UtcNow });
                    await db.SaveChangesAsync(ct);
                }
                else selected.Add(student);
            }
            if (existing is not null) { await db.SaveChangesAsync(ct); continue; }
            if (selected.Count == 0) { await SaveIssues(batchId, first, [new("group_no", "GROUP_SKIPPED", "Group skipped because all members failed validation")], ct); continue; }
            var normalizedLeader = NormalizedName(first.Value("leader_full_name")); var leader = selected.FirstOrDefault(x => NormalizedName(x.FullName) == normalizedLeader);
            if (leader is null) { leader = selected[0]; await SaveIssues(batchId, first, [new("leader_full_name", "LEADER_FALLBACK_WARNING", "No exact leader match found; fallback to " + leader.FullName)], ct); }
            var now = DateTime.UtcNow;
            var group = new StudentGroup { Term = termCode, CourseCode = course, GroupNo = first.Value("group_no"), Name = name,
                ProjectName = Blank(Truncate(first.Value("project_name"), 255)), IdeaDescription = Blank(first.Value("idea_description")), ResearchDomain = Blank(first.Value("research_domain")),
                Status = "ACTIVE", ImportBatchId = batchId, MentorId = mentor?.Id, CreatedAt = now, UpdatedAt = now };
            db.StudentGroups.Add(group); await db.SaveChangesAsync(ct);
            db.TaskBoards.Add(new TaskBoard { GroupId = group.Id, Name = "Default", Description = "Default Board", Position = 0, DefaultBoard = true, CreatedByStudentId = leader.Id, CreatedAt = now, UpdatedAt = now });
            foreach (var student in selected) db.StudentGroupMembers.Add(new StudentGroupMember { GroupId = group.Id, StudentId = student.Id, MemberRole = student.Id == leader.Id ? "LEADER" : "MEMBER", JoinedAt = now });
            await db.SaveChangesAsync(ct); group.LeaderStudentId = leader.Id; await db.SaveChangesAsync(ct);
        }
        await tx.CommitAsync(ct);
    }
    private async Task<Mentor?> ResolveMentor(long batchId, ImportRow row, CancellationToken ct)
    {
        var value = row.Value("mentor_assignment"); if (value.Length == 0) return null;
        var paren = Regex.Match(value, "\\(([^)]+)\\)"); var word = Regex.Match(value, "\\b([Mm]\\d+)\\b");
        var code = paren.Success ? paren.Groups[1].Value.Trim() : word.Success ? word.Groups[1].Value.Trim() : !Regex.IsMatch(value, "[ \\t\\r\\n]") ? value : null;
        Mentor? mentor = null; if (code is not null) mentor = await db.Mentors.FirstOrDefaultAsync(x => x.MentorCode.ToLower() == code.ToLower(), ct);
        if (mentor is null) await SaveIssues(batchId, row, [new("mentor_assignment", "MENTOR_ASSIGNMENT_WARNING", code is null ? $"Mentor assignment warning: unable to parse mentor code from '{value}'" : $"Mentor assignment warning: mentor code '{code}' not found")], ct);
        return mentor;
    }
    private static (string Term, string Course) SheetScope(ImportRow row)
    {
        var match = Regex.Match(row.Sheet ?? "", "([A-Za-z]{2}\\d{2})[_-]([A-Za-z0-9_]+)");
        return (match.Groups[1].Value.ToUpperInvariant(), (Blank(row.Value("course_code")) ?? match.Groups[2].Value).ToUpperInvariant());
    }
    private static string GroupName(ImportRow row) => Blank(row.Value("group_name")) ?? SheetScope(row).Course + " Group " + row.Value("group_no");
    private static string NormalizedGroupName(string value) => Regex.Replace(value.Trim(), "\\s+", " ").ToLowerInvariant();
    private static string NormalizedName(string value) => Regex.Replace(Regex.Replace(value.Replace('\u00a0', ' ').Replace('Đ', 'D').Replace('đ', 'd').Normalize(NormalizationForm.FormD), "\\p{M}", ""), "\\s+", " ").ToLowerInvariant().Trim();
    private static string Truncate(string value, int length) => value.Length > length ? value[..length] : value;
}
