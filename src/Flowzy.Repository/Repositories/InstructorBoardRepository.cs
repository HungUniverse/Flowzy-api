using Flowzy.Repository.Data;
using Flowzy.Repository.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Flowzy.Repository.Repositories;

public sealed class InstructorBoardRepository(FlowzyDbContext db) : IInstructorBoardRepository
{
    private IQueryable<StudentGroup> Details => db.StudentGroups.AsNoTracking().Include(x => x.Mentor).Include(x => x.Instructor)
        .Include(x => x.StudentGroupMembers).ThenInclude(x => x.Student).ThenInclude(x => x.Account);
    private IQueryable<StudentGroup> Eligible(string term, string course = "") => db.StudentGroups.AsNoTracking()
        .Where(x => x.Status == "ACTIVE" && x.TermNavigation.Status == "OPEN" && x.StudentGroupMembers.Any()
            && (term == "" || x.Term.ToLower() == term) && (course == "" || x.CourseCode.ToLower() == course));
    private static IQueryable<StudentGroup> Assigned(IQueryable<StudentGroup> q, long instructor, string assignment) => assignment switch
    {
        "AVAILABLE" => q.Where(x => x.InstructorId == null), "MINE" => q.Where(x => x.InstructorId == instructor),
        "OTHER" => q.Where(x => x.InstructorId != null && x.InstructorId != instructor), _ => q
    };
    public Task<Instructor?> Instructor(string email, CancellationToken ct) => db.Instructors.AsNoTracking().Include(x => x.Account).FirstOrDefaultAsync(x => x.Account.Email == email, ct);
    public async Task<(List<StudentGroup> Items, long Total)> Search(long instructor, string term, string course, string search, string assignment, int page, int size, CancellationToken ct)
    {
        var q = Assigned(Eligible(term, course), instructor, assignment);
        if (search != "")
        {
            // Java uses SQL LIKE, including wildcard semantics for % and _ supplied by the caller.
            var pattern = "%" + search + "%";
            q = q.Where(x => EF.Functions.Like(x.Name.ToLower(), pattern) || EF.Functions.Like(x.GroupNo.ToLower(), pattern)
                || EF.Functions.Like((x.ProjectName ?? "").ToLower(), pattern) || x.StudentGroupMembers.Any(m =>
                    EF.Functions.Like(m.Student.FullName.ToLower(), pattern) || EF.Functions.Like(m.Student.StudentCode.ToLower(), pattern)
                    || EF.Functions.Like(m.Student.Account.Email.ToLower(), pattern) || EF.Functions.Like((m.Student.ClassName ?? "").ToLower(), pattern)));
        }
        var total = await q.LongCountAsync(ct);
        var ids = await q.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).Skip(page * size).Take(size).Select(x => x.Id).ToListAsync(ct);
        var details = await Details.Where(x => ids.Contains(x.Id)).ToDictionaryAsync(x => x.Id, ct);
        return (ids.Where(details.ContainsKey).Select(id => details[id]).ToList(), total);
    }
    public async Task<BoardCounts> Counts(long instructor, string term, string course, CancellationToken ct)
    {
        var q = Eligible(term, course);
        return new(await q.LongCountAsync(ct), await q.LongCountAsync(x => x.InstructorId == null, ct),
            await q.LongCountAsync(x => x.InstructorId == instructor, ct), await q.LongCountAsync(x => x.InstructorId != null && x.InstructorId != instructor, ct));
    }
    public async Task<List<BoardCourseCounts>> Courses(long instructor, string term, CancellationToken ct)
    {
        var rows = await Eligible(term).GroupBy(x => x.CourseCode).Select(g => new { Course = g.Key, Total = g.LongCount(),
            Available = g.LongCount(x => x.InstructorId == null), Mine = g.LongCount(x => x.InstructorId == instructor),
            Other = g.LongCount(x => x.InstructorId != null && x.InstructorId != instructor) }).OrderBy(x => x.Course).ToListAsync(ct);
        return rows.Select(x => new BoardCourseCounts(x.Course, new(x.Total, x.Available, x.Mine, x.Other))).ToList();
    }
    public Task<IDbContextTransaction> Begin(CancellationToken ct) => db.Database.BeginTransactionAsync(ct);
    public Task<StudentGroup?> LockGroup(long id, CancellationToken ct) => db.StudentGroups
        .FromSqlInterpolated($"SELECT * FROM student_groups WHERE id={id} FOR UPDATE").FirstOrDefaultAsync(ct);
    public Task<AcademicTerm?> LockTerm(string code, CancellationToken ct) => db.AcademicTerms
        .FromSqlInterpolated($"SELECT * FROM academic_terms WHERE code={code} FOR UPDATE").FirstOrDefaultAsync(ct);
    public Task<StudentGroup> Detail(long id, CancellationToken ct) => Details.FirstAsync(x => x.Id == id, ct);
    public Task Save(CancellationToken ct) => db.SaveChangesAsync(ct);
}
