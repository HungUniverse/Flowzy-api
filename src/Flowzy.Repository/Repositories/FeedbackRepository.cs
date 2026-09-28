using Flowzy.Repository.Data;
using Flowzy.Repository.Entities;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Repositories;

public sealed class FeedbackRepository(FlowzyDbContext db) : IFeedbackRepository
{
    private IQueryable<TermFeedback> WithDetails(bool tracking = false)
    {
        var query = db.TermFeedbacks
            .Include(x => x.AcademicTerm)
            .Include(x => x.Group)
            .Include(x => x.Student).ThenInclude(x => x.Account)
            .Include(x => x.Mentor).ThenInclude(x => x!.Account)
            .Include(x => x.Instructor).ThenInclude(x => x!.Account);
        return tracking ? query : query.AsNoTracking();
    }

    public Task<List<TermFeedback>> FindOwnAsync(long studentId, string? term, string? status,
        CancellationToken cancellationToken = default)
    {
        var query = WithDetails().Where(x => x.StudentId == studentId);
        if (term is not null) query = query.Where(x => x.AcademicTerm.Code.ToLower() == term.ToLower());
        if (status is not null) query = query.Where(x => x.Status == status);
        return query.OrderByDescending(x => x.AcademicTerm.Code).ThenBy(x => x.Group.GroupNo)
            .ThenBy(x => x.TargetType).ToListAsync(cancellationToken);
    }

    public Task<TermFeedback?> FindAsync(long id, CancellationToken cancellationToken = default) =>
        WithDetails(true).SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public Task<List<TermFeedback>> FindReceivedAsync(long? mentorId, long? instructorId, string? term,
        string? courseCode, CancellationToken cancellationToken = default)
    {
        var query = WithDetails().Where(x => x.Status == "SUBMITTED");
        if (mentorId.HasValue)
            query = query.Where(x => x.MentorId == mentorId.Value && x.TargetType == "MENTOR");
        if (instructorId.HasValue)
            query = query.Where(x => x.InstructorId == instructorId.Value && x.TargetType == "INSTRUCTOR");
        if (term is not null) query = query.Where(x => x.AcademicTerm.Code.ToLower() == term.ToLower());
        if (courseCode is not null) query = query.Where(x => x.Group.CourseCode.ToLower() == courseCode.ToLower());
        return query.OrderByDescending(x => x.SubmittedAt).ToListAsync(cancellationToken);
    }

    public async Task<(List<TermFeedback> Items, long Total)> FindAdminAsync(int page, int size, string? term,
        string? courseCode, string? targetType, long? targetId, string? targetSearch, string? status,
        CancellationToken cancellationToken = default)
    {
        var query = WithDetails();
        if (!string.IsNullOrWhiteSpace(term))
            query = query.Where(x => x.AcademicTerm.Code.ToLower() == term.Trim().ToLower());
        if (!string.IsNullOrWhiteSpace(courseCode))
            query = query.Where(x => x.Group.CourseCode.ToLower() == courseCode.Trim().ToLower());
        if (targetType is not null) query = query.Where(x => x.TargetType == targetType);
        if (status is not null) query = query.Where(x => x.Status == status);
        if (targetId.HasValue)
            query = targetType switch
            {
                "MENTOR" => query.Where(x => x.MentorId == targetId.Value),
                "INSTRUCTOR" => query.Where(x => x.InstructorId == targetId.Value),
                _ => query.Where(x => (x.TargetType == "MENTOR" && x.MentorId == targetId.Value)
                    || (x.TargetType == "INSTRUCTOR" && x.InstructorId == targetId.Value))
            };
        if (!string.IsNullOrWhiteSpace(targetSearch))
        {
            var pattern = $"%{targetSearch.Trim()}%";
            query = targetType switch
            {
                "MENTOR" => query.Where(x => x.Mentor != null && (EF.Functions.ILike(x.Mentor.FullName, pattern)
                    || EF.Functions.ILike(x.Mentor.MentorCode, pattern)
                    || EF.Functions.ILike(x.Mentor.Account.Email, pattern))),
                "INSTRUCTOR" => query.Where(x => x.Instructor != null
                    && (EF.Functions.ILike(x.Instructor.FullName, pattern)
                        || EF.Functions.ILike(x.Instructor.InstructorCode, pattern)
                        || EF.Functions.ILike(x.Instructor.Account.Email, pattern))),
                _ => query.Where(x => (x.TargetType == "MENTOR" && x.Mentor != null
                        && (EF.Functions.ILike(x.Mentor.FullName, pattern)
                            || EF.Functions.ILike(x.Mentor.MentorCode, pattern)
                            || EF.Functions.ILike(x.Mentor.Account.Email, pattern)))
                    || (x.TargetType == "INSTRUCTOR" && x.Instructor != null
                        && (EF.Functions.ILike(x.Instructor.FullName, pattern)
                            || EF.Functions.ILike(x.Instructor.InstructorCode, pattern)
                            || EF.Functions.ILike(x.Instructor.Account.Email, pattern))))
            };
        }
        var total = await query.LongCountAsync(cancellationToken);
        var items = await query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id)
            .Skip(page * size).Take(size).ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<AcademicTerm?> FindTermAsync(string code, CancellationToken cancellationToken = default) =>
        db.AcademicTerms.AsNoTracking().SingleOrDefaultAsync(x => x.Code.ToLower() == code.ToLower(),
            cancellationToken);

    public Task<List<TermFeedback>> FindSubmittedForExportAsync(string term,
        CancellationToken cancellationToken = default) => WithDetails()
        .Where(x => x.Status == "SUBMITTED" && x.AcademicTerm.Code.ToLower() == term.ToLower())
        .ToListAsync(cancellationToken);

    public Task SaveAsync(CancellationToken cancellationToken = default) => db.SaveChangesAsync(cancellationToken);
}
