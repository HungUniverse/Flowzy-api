using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Entities;

[Table("mentor_meetings")]
[Index("BookedByStudentId", Name = "idx_mentor_meetings_booked_by_student_id")]
[Index("GroupId", Name = "idx_mentor_meetings_group_id")]
[Index("MentorId", Name = "idx_mentor_meetings_mentor_id")]
[Index("MentorId", "StartAt", "EndAt", Name = "idx_mentor_meetings_mentor_start_end")]
[Index("Status", Name = "idx_mentor_meetings_status")]
[Index("SlotId", Name = "mentor_meetings_slot_id_key", IsUnique = true)]
public partial class MentorMeeting
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("slot_id")]
    public long? SlotId { get; set; }

    [Column("group_id")]
    public long GroupId { get; set; }

    [Column("mentor_id")]
    public long MentorId { get; set; }

    [Column("booked_by_student_id")]
    public long? BookedByStudentId { get; set; }

    [Column("meet_link")]
    [StringLength(500)]
    public string? MeetLink { get; set; }

    [Column("status")]
    [StringLength(30)]
    public string Status { get; set; } = null!;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("canceled_at")]
    public DateTime? CanceledAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("leader_confirmed_by_student_id")]
    public long? LeaderConfirmedByStudentId { get; set; }

    [Column("leader_confirmed_at")]
    public DateTime? LeaderConfirmedAt { get; set; }

    [Column("mentor_confirmed_at")]
    public DateTime? MentorConfirmedAt { get; set; }

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [Column("cancel_reason")]
    public string? CancelReason { get; set; }

    [Column("start_at")]
    public DateTime StartAt { get; set; }

    [Column("end_at")]
    public DateTime EndAt { get; set; }

    [Column("note")]
    [StringLength(500)]
    public string? Note { get; set; }

    [Column("evidence_image_url")]
    [StringLength(2000)]
    public string? EvidenceImageUrl { get; set; }

    [Column("evidence_submitted_by_student_id")]
    public long? EvidenceSubmittedByStudentId { get; set; }

    [Column("evidence_submitted_at")]
    public DateTime? EvidenceSubmittedAt { get; set; }

    [ForeignKey("BookedByStudentId")]
    [InverseProperty("MentorMeetingBookedByStudents")]
    public virtual Student? BookedByStudent { get; set; }

    [ForeignKey("EvidenceSubmittedByStudentId")]
    [InverseProperty("MentorMeetingEvidenceSubmittedByStudents")]
    public virtual Student? EvidenceSubmittedByStudent { get; set; }

    [ForeignKey("GroupId")]
    [InverseProperty("MentorMeetings")]
    public virtual StudentGroup Group { get; set; } = null!;

    [ForeignKey("LeaderConfirmedByStudentId")]
    [InverseProperty("MentorMeetingLeaderConfirmedByStudents")]
    public virtual Student? LeaderConfirmedByStudent { get; set; }

    [ForeignKey("MentorId")]
    [InverseProperty("MentorMeetings")]
    public virtual Mentor Mentor { get; set; } = null!;

    [ForeignKey("SlotId")]
    [InverseProperty("MentorMeeting")]
    public virtual MentorAvailabilitySlot? Slot { get; set; }
}
