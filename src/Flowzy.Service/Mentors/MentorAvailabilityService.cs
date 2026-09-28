using System.Text.RegularExpressions;
using Flowzy.Repository.Entities;
using Flowzy.Repository.Repositories;
using Flowzy.Service.Contracts;
using Flowzy.Service.Exceptions;

namespace Flowzy.Service.Mentors;

public sealed partial class MentorAvailabilityService(IAccountRepository accounts,
    IMentorAvailabilityRepository slots) : IMentorAvailabilityService
{
    private static readonly TimeZoneInfo BusinessTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Ho_Chi_Minh");

    public async Task<MentorAvailabilitySlotResponse> CreateAsync(CreateAvailabilitySlotRequest request,
        string mentorEmail, CancellationToken cancellationToken = default)
    {
        var mentor = await FindMentorAsync(mentorEmail, cancellationToken);
        var start = request.StartAt!.Value.UtcDateTime;
        var end = request.EndAt!.Value.UtcDateTime;
        ValidateTimes(start, end);
        ValidateMeetLink(request.MeetLink);
        if (await slots.HasOverlapAsync(mentor.Id, start, end, null, cancellationToken))
            throw new BadRequestException("Slot overlaps with an existing availability slot");

        var slot = new MentorAvailabilitySlot
        {
            MentorId = mentor.Id, Mentor = mentor, StartAt = start, EndAt = end,
            MeetLink = request.MeetLink!, Note = request.Note, Status = "AVAILABLE"
        };
        await slots.AddAsync(slot, cancellationToken);
        await slots.SaveAsync(cancellationToken);
        return Map(slot);
    }

    public async Task<IReadOnlyList<MentorAvailabilitySlotResponse>> ListAsync(string mentorEmail,
        CancellationToken cancellationToken = default)
    {
        var mentor = await FindMentorAsync(mentorEmail, cancellationToken);
        return (await slots.ListAsync(mentor.Id, cancellationToken)).Select(Map).ToList();
    }

    public async Task<MentorAvailabilitySlotResponse> UpdateAsync(long slotId, UpdateAvailabilitySlotRequest request,
        string mentorEmail, CancellationToken cancellationToken = default)
    {
        var mentor = await FindMentorAsync(mentorEmail, cancellationToken);
        var slot = await slots.FindAsync(slotId, cancellationToken)
            ?? throw new NotFoundException("Availability slot not found");
        if (slot.MentorId != mentor.Id) throw new ForbiddenException("You do not own this slot");
        if (slot.Status == "BOOKED") throw new BadRequestException("Cannot update a booked slot");

        var start = request.StartAt?.UtcDateTime ?? slot.StartAt;
        var end = request.EndAt?.UtcDateTime ?? slot.EndAt;
        ValidateTimes(start, end);
        if (request.MeetLink is not null)
        {
            ValidateMeetLink(request.MeetLink);
            slot.MeetLink = request.MeetLink;
        }
        if (await slots.HasOverlapAsync(mentor.Id, start, end, slotId, cancellationToken))
            throw new BadRequestException("Slot overlaps with an existing availability slot");

        slot.StartAt = start;
        slot.EndAt = end;
        if (request.Note is not null) slot.Note = request.Note;
        await slots.SaveAsync(cancellationToken);
        return Map(slot);
    }

    public async Task CancelAsync(long slotId, string mentorEmail, CancellationToken cancellationToken = default)
    {
        var mentor = await FindMentorAsync(mentorEmail, cancellationToken);
        var slot = await slots.FindAsync(slotId, cancellationToken)
            ?? throw new NotFoundException("Availability slot not found");
        if (slot.MentorId != mentor.Id) throw new ForbiddenException("You do not own this slot");
        if (slot.Status == "BOOKED") throw new BadRequestException("Cannot cancel a booked slot");
        slot.Status = "CANCELED";
        await slots.SaveAsync(cancellationToken);
    }

    private async Task<Mentor> FindMentorAsync(string email, CancellationToken cancellationToken)
    {
        var account = await accounts.FindByEmailAsync(email, cancellationToken);
        return account?.Mentor ?? throw new NotFoundException("Mentor profile not found");
    }

    private static void ValidateMeetLink(string? meetLink)
    {
        if (meetLink is null || !MeetLinkPattern().IsMatch(meetLink))
            throw new BadRequestException(
                "Google Meet link must strictly match format https://meet.google.com/xxx-xxxx-xxx (lowercase letters)");
    }

    private static void ValidateTimes(DateTime startAt, DateTime endAt)
    {
        if ((startAt.Minute != 0 && startAt.Minute != 30) || startAt.Second != 0 || startAt.Millisecond != 0
            || startAt.Ticks % TimeSpan.TicksPerMillisecond != 0)
            throw new BadRequestException("Availability slot must start on the hour or half hour with no seconds");
        if (endAt - startAt != TimeSpan.FromHours(1))
            throw new BadRequestException("Availability slot must last exactly 60 minutes");
        var localStart = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(startAt, DateTimeKind.Utc), BusinessTimeZone);
        var localEnd = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(endAt, DateTimeKind.Utc), BusinessTimeZone);
        if (localStart.Date != localEnd.Date)
            throw new BadRequestException(
                "Availability slots must start and end on the same day (Asia/Ho_Chi_Minh)");
    }

    private static MentorAvailabilitySlotResponse Map(MentorAvailabilitySlot slot) => new(slot.Id, slot.MentorId,
        slot.Mentor.MentorCode, slot.Mentor.FullName, slot.StartAt, slot.EndAt, slot.MeetLink, slot.Note,
        slot.Status, slot.CreatedAt, slot.UpdatedAt);

    [GeneratedRegex("^https://meet\\.google\\.com/[a-z]{3}-[a-z]{4}-[a-z]{3}$")]
    private static partial Regex MeetLinkPattern();
}
