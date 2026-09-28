using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Entities;

[Table("milestone_grades")]
[Index("InstructorId", Name = "idx_milestone_grades_instructor_id")]
[Index("SubmissionId", Name = "idx_milestone_grades_submission_id")]
[Index("SubmissionId", Name = "milestone_grades_submission_id_key", IsUnique = true)]
public partial class MilestoneGrade
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("submission_id")]
    public long SubmissionId { get; set; }

    [Column("instructor_id")]
    public long? InstructorId { get; set; }

    [Column("score")]
    [Precision(5, 2)]
    public decimal Score { get; set; }

    [Column("feedback")]
    public string? Feedback { get; set; }

    [Column("graded_at")]
    public DateTime GradedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("max_score")]
    [Precision(5, 2)]
    public decimal MaxScore { get; set; }

    [ForeignKey("InstructorId")]
    [InverseProperty("MilestoneGrades")]
    public virtual Instructor? Instructor { get; set; }

    [ForeignKey("SubmissionId")]
    [InverseProperty("MilestoneGrade")]
    public virtual MilestoneSubmission Submission { get; set; } = null!;
}
