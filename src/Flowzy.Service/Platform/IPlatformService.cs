using Flowzy.Service.Contracts;

namespace Flowzy.Service.Platform;

public interface IPlatformService
{
    Task<IReadOnlyList<AcademicTermResponse>> GetAvailableTermsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<ProblemDomainResponse>> GetProblemDomainsAsync(string? search, string? status, CancellationToken cancellationToken);
    Task<IReadOnlyList<ProblemCriterionResponse>> GetActiveCriteriaAsync(CancellationToken cancellationToken);
    Task<PageResponse<ProblemSummaryResponse>> SearchProblemsAsync(int page, int size, string? search, string? domainCode,
        string? difficulty, string? expectedOutput, string? sourceType, string? status, CancellationToken cancellationToken);
    Task<ProblemDetailResponse> GetProblemAsync(long id, CancellationToken cancellationToken);
    Task<PageResponse<NotificationResponse>> GetNotificationsAsync(string email, bool unreadOnly, int page, int size, CancellationToken cancellationToken);
    Task<long> GetUnreadCountAsync(string email, CancellationToken cancellationToken);
    Task<NotificationResponse> MarkNotificationReadAsync(long id, string email, CancellationToken cancellationToken);
    Task<long> MarkAllNotificationsReadAsync(string email, CancellationToken cancellationToken);
    Task<SelfProfileResponse> GetProfileAsync(string email, CancellationToken cancellationToken);
    Task<SelfProfileResponse> UpdateProfileAsync(string email, UpdateSelfProfileRequest request, CancellationToken cancellationToken);
    Task ChangePasswordAsync(string email, ChangeOwnPasswordRequest request, CancellationToken cancellationToken);
}
