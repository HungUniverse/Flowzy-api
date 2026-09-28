using Flowzy.Service.Contracts;

namespace Flowzy.Service.Imports;

public interface IImportService
{
    Task<ImportResultResponse> QueueAsync(Stream stream, string filename, long length, string target, string creatorEmail, CancellationToken ct);
    Task<ImportBatchResponse> GetBatchAsync(long id, CancellationToken ct);
    Task<PageResponse<ImportRowErrorResponse>> GetErrorsAsync(long id, int page, int size, string? search, int? rowNumber, string? fieldName, string? errorCode, CancellationToken ct);
    Task ProcessAsync(ImportWorkItem item, CancellationToken ct);
    Task FailInterruptedAsync(CancellationToken ct);
}
