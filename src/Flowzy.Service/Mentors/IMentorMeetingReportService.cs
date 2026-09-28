using Flowzy.Service.Contracts;

namespace Flowzy.Service.Mentors;

public interface IMentorMeetingReportService
{
    Task<IReadOnlyList<MentorReportTermResponse>> ListTermsAsync(string mentorEmail,
        CancellationToken cancellationToken = default);
    Task<byte[]> ExportAsync(string? term, string mentorEmail, CancellationToken cancellationToken = default);
}
