using Flowzy.Service.Contracts;

namespace Flowzy.Service.Mentors;

public interface IMentorAvailabilityService
{
    Task<MentorAvailabilitySlotResponse> CreateAsync(CreateAvailabilitySlotRequest request, string mentorEmail,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MentorAvailabilitySlotResponse>> ListAsync(string mentorEmail,
        CancellationToken cancellationToken = default);
    Task<MentorAvailabilitySlotResponse> UpdateAsync(long slotId, UpdateAvailabilitySlotRequest request,
        string mentorEmail, CancellationToken cancellationToken = default);
    Task CancelAsync(long slotId, string mentorEmail, CancellationToken cancellationToken = default);
}
