using Flowzy.Repository.Entities;

namespace Flowzy.Repository.Repositories;

public interface IMentorMeetingReportRepository
{
    Task<List<AcademicTerm>> FindTermsAsync(long mentorId, CancellationToken cancellationToken = default);
    Task<AcademicTerm?> FindTermAsync(string code, CancellationToken cancellationToken = default);
    Task<List<StudentGroup>> FindGroupsAsync(long mentorId, string term,
        CancellationToken cancellationToken = default);
    Task<List<MentorMeeting>> FindMeetingsAsync(long mentorId, string term,
        CancellationToken cancellationToken = default);
}
