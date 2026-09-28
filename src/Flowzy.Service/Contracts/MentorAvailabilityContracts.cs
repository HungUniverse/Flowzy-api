using System.ComponentModel.DataAnnotations;

namespace Flowzy.Service.Contracts;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
public sealed class FutureDateTimeAttribute : ValidationAttribute
{
    public override bool IsValid(object? value) => value is null
        || value is DateTimeOffset instant && instant > DateTimeOffset.UtcNow;
}

public sealed record CreateAvailabilitySlotRequest(
    [Required(ErrorMessage = "Start time is required"), FutureDateTime(ErrorMessage = "Start time must be in the future")]
    DateTimeOffset? StartAt,
    [Required(ErrorMessage = "End time is required"), FutureDateTime(ErrorMessage = "End time must be in the future")]
    DateTimeOffset? EndAt,
    [Required(ErrorMessage = "Google Meet link is required"),
     RegularExpression("^https://meet\\.google\\.com/[a-z]{3}-[a-z]{4}-[a-z]{3}$",
         ErrorMessage = "Google Meet link must strictly match format https://meet.google.com/xxx-xxxx-xxx (lowercase letters)")]
    string? MeetLink,
    [StringLength(500, ErrorMessage = "Note must not exceed 500 characters")] string? Note);

public sealed record UpdateAvailabilitySlotRequest(
    [FutureDateTime(ErrorMessage = "Start time must be in the future")] DateTimeOffset? StartAt,
    [FutureDateTime(ErrorMessage = "End time must be in the future")] DateTimeOffset? EndAt,
    [RegularExpression("^https://meet\\.google\\.com/[a-z]{3}-[a-z]{4}-[a-z]{3}$",
        ErrorMessage = "Google Meet link must strictly match format https://meet.google.com/xxx-xxxx-xxx (lowercase letters)")]
    string? MeetLink,
    [StringLength(500, ErrorMessage = "Note must not exceed 500 characters")] string? Note);

public sealed record MentorAvailabilitySlotResponse(long Id, long MentorId, string MentorCode, string MentorName,
    DateTime StartAt, DateTime EndAt, string MeetLink, string? Note, string Status, DateTime CreatedAt,
    DateTime UpdatedAt);
