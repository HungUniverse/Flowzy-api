using Flowzy.Service.Contracts;

namespace Flowzy.Service.Feedback;

public interface IFeedbackService
{
    Task<IReadOnlyList<TermFeedbackResponse>> GetOwnAsync(string? term, FeedbackStatus? status,
        string currentUserEmail, CancellationToken cancellationToken = default);
    Task<TermFeedbackResponse> SubmitAsync(long id, SubmitFeedbackRequest request, string currentUserEmail,
        CancellationToken cancellationToken = default);
    Task<FeedbackReceivedSummary> GetReceivedAsync(string email, string? term, string? courseCode,
        CancellationToken cancellationToken = default);
}
