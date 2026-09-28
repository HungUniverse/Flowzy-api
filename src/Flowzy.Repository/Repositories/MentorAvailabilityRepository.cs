using Flowzy.Repository.Data;
using Flowzy.Repository.Entities;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Repositories;

public sealed class MentorAvailabilityRepository(FlowzyDbContext db) : IMentorAvailabilityRepository
{
    public Task<List<MentorAvailabilitySlot>> ListAsync(long mentorId,
        CancellationToken cancellationToken = default) =>
        db.MentorAvailabilitySlots.AsNoTracking().Include(x => x.Mentor)
            .Where(x => x.MentorId == mentorId).OrderByDescending(x => x.StartAt)
            .ToListAsync(cancellationToken);

    public Task<MentorAvailabilitySlot?> FindAsync(long slotId, CancellationToken cancellationToken = default) =>
        db.MentorAvailabilitySlots.Include(x => x.Mentor)
            .SingleOrDefaultAsync(x => x.Id == slotId, cancellationToken);

    public Task<bool> HasOverlapAsync(long mentorId, DateTime startAt, DateTime endAt, long? excludeSlotId,
        CancellationToken cancellationToken = default) =>
        db.MentorAvailabilitySlots.AnyAsync(x => x.MentorId == mentorId
            && x.Status != "CANCELED"
            && (!excludeSlotId.HasValue || x.Id != excludeSlotId.Value)
            && x.StartAt < endAt && x.EndAt > startAt, cancellationToken);

    public async Task AddAsync(MentorAvailabilitySlot slot, CancellationToken cancellationToken = default) =>
        await db.MentorAvailabilitySlots.AddAsync(slot, cancellationToken);

    public Task SaveAsync(CancellationToken cancellationToken = default) => db.SaveChangesAsync(cancellationToken);
}
