using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Entities;

[Table("milestone_group_grades")]
[Index("GroupId", Name = "idx_milestone_group_grades_group_id")]
[Index("InstructorId", Name = "idx_milestone_group_grades_instructor_id")]
[Index("MilestoneId", Name = "idx_milestone_group_grades_milestone_id")]
[Index("MilestoneId", "GroupId", Name = "uq_milestone_group_grades_milestone_group", IsUnique = true)]
public partial class MilestoneGroupGrade
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("milestone_id")]
    public long MilestoneId { get; set; }

    [Column("group_id")]
    public long GroupId { get; set; }

    [Column("instructor_id")]
    public long? InstructorId { get; set; }

    [Column("score")]
    [Precision(7, 2)]
    public decimal Score { get; set; }

    [Column("max_score_snapshot")]
    [Precision(7, 2)]
    public decimal MaxScoreSnapshot { get; set; }

    [Column("weight_snapshot")]
    public int WeightSnapshot { get; set; }

    [Column("feedback")]
    public string? Feedback { get; set; }

    [Column("graded_at")]
    public DateTime GradedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("GroupId")]
    [InverseProperty("MilestoneGroupGrades")]
    public virtual StudentGroup Group { get; set; } = null!;

    [ForeignKey("InstructorId")]
    [InverseProperty("MilestoneGroupGrades")]
    public virtual Instructor? Instructor { get; set; }

    [ForeignKey("MilestoneId")]
    [InverseProperty("MilestoneGroupGrades")]
    public virtual CourseMilestone Milestone { get; set; } = null!;

    [InverseProperty("GroupGrade")]
    public virtual ICollection<MilestoneMemberScore> MilestoneMemberScores { get; set; } = new List<MilestoneMemberScore>();
}
