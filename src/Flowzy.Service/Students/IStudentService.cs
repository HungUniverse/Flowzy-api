using Flowzy.Service.Contracts;

namespace Flowzy.Service.Students;

public interface IStudentService
{
    Task<StudentProfileResponse> GetByIdAsync(long id, string currentUserEmail,
        CancellationToken cancellationToken = default);
    Task<PageResponse<StudentProfileResponse>> GetUngroupedAsync(string? term, string? courseCode, string? search,
        int page, int size, string currentUserEmail, CancellationToken cancellationToken = default);
}
