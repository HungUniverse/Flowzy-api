namespace Flowzy.Service.Contracts;

public sealed record ImportResultResponse(long BatchId, string TargetType, string Status, string FileName,
    string FileType, int TotalRows, int SuccessRows, int FailedRows, DateTime StartedAt, DateTime? FinishedAt,
    int? CreatedGroups = null, int? SkippedGroups = null, int? LeaderFallbackWarnings = null,
    int? AssignedMentors = null, int? MentorAssignmentWarnings = null);
public sealed record ImportBatchResponse(long Id, string TargetType, string FileName, string FileType,
    string Status, int TotalRows, int SuccessRows, int FailedRows, DateTime StartedAt, DateTime? FinishedAt);
public sealed record ImportRowErrorResponse(int RowNumber, string? FieldName, string ErrorCode, string ErrorMessage);
