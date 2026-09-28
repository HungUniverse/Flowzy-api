using Flowzy.Service.Contracts;

namespace Flowzy.Service.Groups;

public interface IGroupMembershipService
{
    Task<InvitationResponse> InviteAsync(long groupId, CreateInvitationRequest request, string email, CancellationToken ct);
    Task<IReadOnlyList<InvitationResponse>> InvitationsAsync(long? groupId, string email, CancellationToken ct);
    Task RespondInvitationAsync(long id, string action, string email, CancellationToken ct);
    Task<JoinRequestResponse> JoinAsync(long groupId, CreateJoinRequest request, string email, CancellationToken ct);
    Task<IReadOnlyList<JoinRequestResponse>> JoinRequestsAsync(long? groupId, string email, CancellationToken ct);
    Task RespondJoinAsync(long? groupId, long id, string action, string email, CancellationToken ct);
}
