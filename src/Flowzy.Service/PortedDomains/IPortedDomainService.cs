using Flowzy.Service.Contracts;

namespace Flowzy.Service.PortedDomains;

public interface IPortedDomainService
{
    Task<ProblemDomainResponse> CreateProblemDomainAsync(ProblemDomainRequest request, CancellationToken ct);
    Task<ProblemDomainResponse> UpdateProblemDomainAsync(long id, UpdateProblemDomainRequest request, CancellationToken ct);
    Task<ProblemDetailResponse> CreateProblemAsync(ProblemWriteRequest request, CancellationToken ct);
    Task<ProblemDetailResponse> UpdateProblemAsync(long id, ProblemPatchRequest request, CancellationToken ct);
    Task<ProblemDetailResponse> SetProblemStatusAsync(long id, string status, CancellationToken ct);
    Task<ProblemDetailResponse> ReviewProblemAsync(long id, ReviewProblemRequest request, string email, CancellationToken ct);
}
