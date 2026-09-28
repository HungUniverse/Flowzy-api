using Flowzy.Service.Contracts;
namespace Flowzy.Service.Groups;
public interface IInstructorBoardService{Task<InstructorBoardResponse>Get(string email,int page,int size,string? term,string? course,string? search,string assignment,CancellationToken ct);Task<InstructorBoardItem>Claim(long groupId,string email,CancellationToken ct);}
