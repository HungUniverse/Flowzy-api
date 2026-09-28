using System.ComponentModel.DataAnnotations;
namespace Flowzy.Service.Contracts;
public sealed record CreateOrBookMeetingRequest(long? SlotId,DateTime? StartAt,DateTime? EndAt,
    [RegularExpression("^https?://.+", ErrorMessage = "Meet link must use HTTP or HTTPS"), StringLength(500, ErrorMessage = "size must be between 0 and 500")] string? MeetLink,
    [StringLength(500, ErrorMessage = "size must be between 0 and 500")] string? Note) : IValidatableObject
{
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (SlotId is not null ? StartAt is not null || EndAt is not null : StartAt is null || EndAt is null)
            yield return new("Provide either slotId or both startAt and endAt", ["validRequestShape"]);
        if (SlotId is null && string.IsNullOrWhiteSpace(MeetLink))
            yield return new("Meet link is required when creating a meeting directly", ["meetLinkProvidedForDirectMeeting"]);
    }
}
public sealed record UpdateMeetingRequest(DateTime? StartAt,DateTime? EndAt,
    [Required(ErrorMessage = "Meet link is required"), RegularExpression("^https?://.+", ErrorMessage = "Meet link must use HTTP or HTTPS"), StringLength(500, ErrorMessage = "size must be between 0 and 500")] string MeetLink,
    [StringLength(500, ErrorMessage = "size must be between 0 and 500")] string? Note);
public sealed record SubmitMeetingEvidenceRequest([Required(ErrorMessage = "must not be blank"), StringLength(2000, ErrorMessage = "size must be between 0 and 2000"), RegularExpression("^https?://.+", ErrorMessage = "Evidence URL must use HTTP or HTTPS")] string ImageUrl);
public sealed record CancelMeetingRequest([Required(ErrorMessage = "Cancellation reason is required"), StringLength(500, ErrorMessage = "Cancellation reason cannot exceed 500 characters")] string Reason);
public sealed record MeetingResponse(long Id,long? SlotId,long GroupId,string GroupName,string GroupNo,string? ProjectName,long? SelectedProblemId,string? SelectedProblemTitle,long MentorId,string MentorCode,string MentorName,long? BookedByStudentId,string? BookedByStudentCode,string? BookedByStudentName,DateTime StartAt,DateTime EndAt,string? MeetLink,string? Note,string Status,long? LeaderConfirmedByStudentId,DateTime? LeaderConfirmedAt,DateTime? MentorConfirmedAt,DateTime? CompletedAt,DateTime? CanceledAt,string? CancelReason,string? EvidenceImageUrl,long? EvidenceSubmittedByStudentId,string? EvidenceSubmittedByStudentName,DateTime? EvidenceSubmittedAt,DateTime CreatedAt,DateTime UpdatedAt);
