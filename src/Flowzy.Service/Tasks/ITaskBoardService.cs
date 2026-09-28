using Flowzy.Service.Contracts;

namespace Flowzy.Service.Tasks;

public interface ITaskBoardService
{
    Task<IReadOnlyList<TaskBoardResponse>> GetBoardsAsync(long groupId, string email, CancellationToken ct);
    Task<TaskBoardResponse> CreateBoardAsync(long groupId, CreateTaskBoardRequest request, string email, CancellationToken ct);
    Task<TaskBoardResponse> UpdateBoardAsync(long groupId, long boardId, UpdateTaskBoardRequest request, string email, CancellationToken ct);
    Task DeleteBoardAsync(long groupId, long boardId, string email, CancellationToken ct);
}
