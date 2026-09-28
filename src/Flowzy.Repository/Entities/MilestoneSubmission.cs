using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Entities;

[Table("milestone_submissions")]
[Index("GroupId", Name = "idx_milestone_submissions_group_id")]
[Index("MilestoneId", Name = "idx_milestone_submissions_milestone_id")]
[Index("MilestoneId", "GroupId", Name = "uq_milestone_submissions_milestone_group", IsUnique = true)]
public partial class MilestoneSubmission
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("milestone_id")]
    public long MilestoneId { get; set; }

    [Column("group_id")]
    public long GroupId { get; set; }

    [Column("submitted_by")]
    [StringLength(255)]
    public string? SubmittedBy { get; set; }

    [Column("file_url")]
    [StringLength(1000)]
    public string? FileUrl { get; set; }

    [Column("comments")]
    public string? Comments { get; set; }

    [Column("submitted_at")]
    public DateTime SubmittedAt { get; set; }

    [Column("late")]
    public bool Late { get; set; }

    [Column("status")]
    [StringLength(30)]
    public string Status { get; set; } = null!;

    [Column("version")]
    public long Version { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("submitted_by_student_id")]
    public long? SubmittedByStudentId { get; set; }

    [ForeignKey("GroupId")]
    [InverseProperty("MilestoneSubmissions")]
    public virtual StudentGroup Group { get; set; } = null!;

    [ForeignKey("MilestoneId")]
    [InverseProperty("MilestoneSubmissions")]
    public virtual CourseMilestone Milestone { get; set; } = null!;

    [InverseProperty("Submission")]
    public virtual MilestoneGrade? MilestoneGrade { get; set; }

    [ForeignKey("SubmittedByStudentId")]
    [InverseProperty("MilestoneSubmissions")]
    public virtual Student? SubmittedByStudent { get; set; }
}
