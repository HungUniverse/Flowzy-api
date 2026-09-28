using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Entities;

[Table("milestone_outcomes")]
[Index("MilestoneId", Name = "idx_milestone_outcomes_milestone_id")]
public partial class MilestoneOutcome
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("milestone_id")]
    public long MilestoneId { get; set; }

    [Column("title")]
    [StringLength(255)]
    public string Title { get; set; } = null!;

    [Column("description")]
    public string? Description { get; set; }

    [Column("required_type")]
    [StringLength(50)]
    public string RequiredType { get; set; } = null!;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("MilestoneId")]
    [InverseProperty("MilestoneOutcomes")]
    public virtual CourseMilestone Milestone { get; set; } = null!;
}
