using Flowzy.Repository.Entities;

namespace Flowzy.Repository.Repositories;

public interface IPlatformReadRepository
{
    Task<Account?> FindProfileAsync(string email, CancellationToken cancellationToken);
    Task SaveAsync(CancellationToken cancellationToken);
    Task<List<AcademicTerm>> GetOpenTermsAsync(CancellationToken cancellationToken);
    Task<(long Groups, long ExpectedFeedback, long SubmittedFeedback)> GetTermCountsAsync(long termId, string termCode, CancellationToken cancellationToken);
    Task<List<ProblemDomain>> GetProblemDomainsAsync(string? search, string? status, CancellationToken cancellationToken);
    Task<List<ProblemEvaluationCriterion>> GetActiveCriteriaAsync(CancellationToken cancellationToken);
    Task<(List<Problem> Items, long Total)> SearchProblemsAsync(int page, int size, string? search, string? domainCode,
        string? difficulty, string? expectedOutput, string? sourceType, string? status, CancellationToken cancellationToken);
    Task<Problem?> FindProblemAsync(long id, CancellationToken cancellationToken);
    Task<(List<Notification> Items, long Total)> GetNotificationsAsync(long recipientId, bool unreadOnly, int page, int size, CancellationToken cancellationToken);
    Task<long> GetUnreadCountAsync(long recipientId, CancellationToken cancellationToken);
    Task<Notification?> FindNotificationAsync(long id, CancellationToken cancellationToken);
    Task MarkAllNotificationsReadAsync(long recipientId, DateTime readAt, CancellationToken cancellationToken);
}
