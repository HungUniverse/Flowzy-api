using Flowzy.Repository.Entities;

namespace Flowzy.Repository.Repositories;

public interface IMentorAvailabilityRepository
{
    Task<List<MentorAvailabilitySlot>> ListAsync(long mentorId, CancellationToken cancellationToken = default);
    Task<MentorAvailabilitySlot?> FindAsync(long slotId, CancellationToken cancellationToken = default);
    Task<bool> HasOverlapAsync(long mentorId, DateTime startAt, DateTime endAt, long? excludeSlotId,
        CancellationToken cancellationToken = default);
    Task AddAsync(MentorAvailabilitySlot slot, CancellationToken cancellationToken = default);
    Task SaveAsync(CancellationToken cancellationToken = default);
}
