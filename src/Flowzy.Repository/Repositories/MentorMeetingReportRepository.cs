using Flowzy.Repository.Data;
using Flowzy.Repository.Entities;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Repositories;

public sealed class MentorMeetingReportRepository(FlowzyDbContext db) : IMentorMeetingReportRepository
{
    public async Task<List<AcademicTerm>> FindTermsAsync(long mentorId,
        CancellationToken cancellationToken = default)
    {
        var groupTerms = db.StudentGroups.Where(x => x.MentorId == mentorId).Select(x => x.Term);
        var meetingTerms = db.MentorMeetings.Where(x => x.MentorId == mentorId).Select(x => x.Group.Term);
        var codes = await groupTerms.Union(meetingTerms).ToListAsync(cancellationToken);
        return await db.AcademicTerms.AsNoTracking().Where(x => codes.Contains(x.Code))
            .OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).ToListAsync(cancellationToken);
    }

    public Task<AcademicTerm?> FindTermAsync(string code, CancellationToken cancellationToken = default) =>
        db.AcademicTerms.AsNoTracking().SingleOrDefaultAsync(x => x.Code.ToLower() == code.ToLower(),
            cancellationToken);

    public Task<List<StudentGroup>> FindGroupsAsync(long mentorId, string term,
        CancellationToken cancellationToken = default) =>
        db.StudentGroups.AsNoTracking()
            .Where(x => x.Term.ToLower() == term.ToLower()
                && (x.MentorId == mentorId || db.MentorMeetings.Any(m => m.GroupId == x.Id && m.MentorId == mentorId)))
            .OrderBy(x => x.CourseCode.ToLower()).ThenBy(x => x.GroupNo.ToLower()).ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);

    public Task<List<MentorMeeting>> FindMeetingsAsync(long mentorId, string term,
        CancellationToken cancellationToken = default) =>
        db.MentorMeetings.AsNoTracking().Include(x => x.Group).Include(x => x.EvidenceSubmittedByStudent)
            .Where(x => x.MentorId == mentorId && x.Group.Term.ToLower() == term.ToLower())
            .OrderBy(x => x.Group.CourseCode.ToLower()).ThenBy(x => x.Group.GroupNo.ToLower())
            .ThenBy(x => x.StartAt).ThenBy(x => x.Id).ToListAsync(cancellationToken);
}
