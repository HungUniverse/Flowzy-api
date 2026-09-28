using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Entities;

[Table("mentor_availability_slots")]
[Index("MentorId", Name = "idx_mentor_availability_slots_mentor_id")]
[Index("StartAt", "EndAt", Name = "idx_mentor_availability_slots_start_end")]
[Index("Status", Name = "idx_mentor_availability_slots_status")]
public partial class MentorAvailabilitySlot
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("mentor_id")]
    public long MentorId { get; set; }

    [Column("start_at")]
    public DateTime StartAt { get; set; }

    [Column("end_at")]
    public DateTime EndAt { get; set; }

    [Column("meet_link")]
    [StringLength(500)]
    public string MeetLink { get; set; } = null!;

    [Column("note")]
    [StringLength(500)]
    public string? Note { get; set; }

    [Column("status")]
    [StringLength(30)]
    public string Status { get; set; } = null!;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("MentorId")]
    [InverseProperty("MentorAvailabilitySlots")]
    public virtual Mentor Mentor { get; set; } = null!;

    [InverseProperty("Slot")]
    public virtual MentorMeeting? MentorMeeting { get; set; }
}
