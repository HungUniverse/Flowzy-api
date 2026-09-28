using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Entities;

[Table("milestone_member_scores")]
[Index("GroupGradeId", Name = "idx_milestone_member_scores_group_grade_id")]
[Index("GroupId", Name = "idx_milestone_member_scores_group_id")]
[Index("MilestoneId", Name = "idx_milestone_member_scores_milestone_id")]
[Index("StudentId", Name = "idx_milestone_member_scores_student_id")]
[Index("MilestoneId", "GroupId", "StudentId", Name = "uq_milestone_member_scores_milestone_group_student", IsUnique = true)]
public partial class MilestoneMemberScore
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("group_grade_id")]
    public long? GroupGradeId { get; set; }

    [Column("milestone_id")]
    public long MilestoneId { get; set; }

    [Column("group_id")]
    public long GroupId { get; set; }

    [Column("student_id")]
    public long StudentId { get; set; }

    [Column("contribution_percent")]
    [Precision(5, 2)]
    public decimal ContributionPercent { get; set; }

    [Column("calculated_score")]
    [Precision(7, 4)]
    public decimal? CalculatedScore { get; set; }

    [Column("max_score_snapshot")]
    [Precision(7, 2)]
    public decimal? MaxScoreSnapshot { get; set; }

    [Column("weight_snapshot")]
    public int? WeightSnapshot { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("GroupId")]
    [InverseProperty("MilestoneMemberScores")]
    public virtual StudentGroup Group { get; set; } = null!;

    [ForeignKey("GroupGradeId")]
    [InverseProperty("MilestoneMemberScores")]
    public virtual MilestoneGroupGrade? GroupGrade { get; set; }

    [ForeignKey("MilestoneId")]
    [InverseProperty("MilestoneMemberScores")]
    public virtual CourseMilestone Milestone { get; set; } = null!;

    [ForeignKey("StudentId")]
    [InverseProperty("MilestoneMemberScores")]
    public virtual Student Student { get; set; } = null!;
}
