namespace Flowzy.Service.Contracts;
public sealed record InstructorBoardMember(long StudentId,string StudentCode,string FullName,string Email,string? ClassName,string? Major,string Role);
public sealed record InstructorBoardItem(long Id,string Term,string CourseCode,string GroupNo,string Name,string? ProjectName,string? IdeaDescription,string? ResearchDomain,bool IsLock,int MemberCount,long? MentorId,string? MentorCode,string? MentorName,long? InstructorId,string? InstructorCode,string? InstructorName,string AssignmentState,IReadOnlyList<InstructorBoardMember> Members);
public sealed record InstructorBoardSummary(long TotalGroups,long AvailableGroups,long MyGroups,long OtherGroups);
public sealed record InstructorCourseCount(string CourseCode,long TotalGroups,long AvailableGroups,long MyGroups,long OtherGroups);
public sealed record InstructorBoardResponse(InstructorBoardSummary Summary,IReadOnlyList<InstructorCourseCount> Courses,PageResponse<InstructorBoardItem> Groups);
