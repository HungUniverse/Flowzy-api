using Flowzy.Service.Contracts;

namespace Flowzy.Service.Feedback;

public interface IAdminFeedbackService
{
    Task<PageResponse<AdminFeedbackResponse>> SearchAsync(int page, int size, string? term, string? courseCode,
        FeedbackTargetType? targetType, long? targetId, string? targetSearch, FeedbackStatus? status,
        CancellationToken cancellationToken = default);
    Task<byte[]> ExportAsync(string? term, CancellationToken cancellationToken = default);
}
