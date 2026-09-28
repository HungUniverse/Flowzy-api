namespace Flowzy.Service.Contracts;

public sealed record SelectedProblemSummaryResponse(long Id, string? Code, string Title, string SourceType,
    string Status);
public sealed record GroupRecruitmentNeedResponse(string Role, string? Category, string? DisplayNameVi,
    string? DisplayNameEn, int Quantity);
public sealed record GroupSummaryResponse(long Id, string Term, string TermStatus, DateTime? TermClosedAt,
    bool StudentReadOnly, string CourseCode, string GroupNo, string Name, string? ProjectName,
    string? LeaderName, int MemberCount, decimal? RequiredGpa, decimal? TargetGrade, string Status,
    long? MentorId, long? MentorAccountId, string? MentorCode, string? MentorName, long? InstructorId,
    long? InstructorAccountId, string? InstructorCode, string? InstructorName, bool IsLock,
    SelectedProblemSummaryResponse? SelectedProblem, IReadOnlyList<GroupRecruitmentNeedResponse> RecruitmentNeeds);
