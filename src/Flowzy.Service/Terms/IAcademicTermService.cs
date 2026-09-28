using Flowzy.Service.Contracts;

namespace Flowzy.Service.Terms;

public interface IAcademicTermService
{
    Task<PageResponse<AcademicTermResponse>> ListTermsAsync(int page, int size, CancellationToken ct);
    Task<AcademicTermResponse> CreateTermAsync(CreateAcademicTermRequest request, CancellationToken ct);
    Task<AcademicTermResponse> CloseTermAsync(string term, string email, CancellationToken ct);
    Task<ArchiveTermStudentsResponse> ArchiveStudentsAsync(string term, CancellationToken ct);
    Task DeleteTermAsync(string term, CancellationToken ct);
}
