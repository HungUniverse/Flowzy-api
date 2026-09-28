using Flowzy.Repository.Entities;

namespace Flowzy.Repository.Repositories;

public interface IFeedbackRepository
{
    Task<List<TermFeedback>> FindOwnAsync(long studentId, string? term, string? status,
        CancellationToken cancellationToken = default);
    Task<TermFeedback?> FindAsync(long id, CancellationToken cancellationToken = default);
    Task<List<TermFeedback>> FindReceivedAsync(long? mentorId, long? instructorId, string? term,
        string? courseCode, CancellationToken cancellationToken = default);
    Task<(List<TermFeedback> Items, long Total)> FindAdminAsync(int page, int size, string? term,
        string? courseCode, string? targetType, long? targetId, string? targetSearch, string? status,
        CancellationToken cancellationToken = default);
    Task<AcademicTerm?> FindTermAsync(string code, CancellationToken cancellationToken = default);
    Task<List<TermFeedback>> FindSubmittedForExportAsync(string term,
        CancellationToken cancellationToken = default);
    Task SaveAsync(CancellationToken cancellationToken = default);
}
