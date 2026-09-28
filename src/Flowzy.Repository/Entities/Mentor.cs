using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Entities;

[Table("mentors")]
[Index("Company", Name = "idx_mentors_company")]
[Index("FullName", Name = "idx_mentors_full_name")]
[Index("AccountId", Name = "mentors_account_id_key", IsUnique = true)]
[Index("MentorCode", Name = "mentors_mentor_code_key", IsUnique = true)]
public partial class Mentor
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("account_id")]
    public long AccountId { get; set; }

    [Column("mentor_code")]
    [StringLength(50)]
    public string MentorCode { get; set; } = null!;

    [Column("full_name")]
    [StringLength(255)]
    public string FullName { get; set; } = null!;

    [Column("phone")]
    [StringLength(30)]
    public string? Phone { get; set; }

    [Column("job_title")]
    [StringLength(150)]
    public string? JobTitle { get; set; }

    [Column("company")]
    [StringLength(150)]
    public string? Company { get; set; }

    [Column("expertise")]
    public string? Expertise { get; set; }

    [Column("years_of_experience")]
    public int? YearsOfExperience { get; set; }

    [Column("linkedin_url")]
    [StringLength(500)]
    public string? LinkedinUrl { get; set; }

    [Column("status")]
    [StringLength(30)]
    public string Status { get; set; } = null!;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("AccountId")]
    [InverseProperty("Mentor")]
    public virtual Account Account { get; set; } = null!;

    [InverseProperty("Mentor")]
    public virtual ICollection<MentorAvailabilitySlot> MentorAvailabilitySlots { get; set; } = new List<MentorAvailabilitySlot>();

    [InverseProperty("Mentor")]
    public virtual ICollection<MentorMeeting> MentorMeetings { get; set; } = new List<MentorMeeting>();

    [InverseProperty("Mentor")]
    public virtual ICollection<StudentGroup> StudentGroups { get; set; } = new List<StudentGroup>();

    [InverseProperty("Mentor")]
    public virtual ICollection<TermFeedback> TermFeedbacks { get; set; } = new List<TermFeedback>();
}
