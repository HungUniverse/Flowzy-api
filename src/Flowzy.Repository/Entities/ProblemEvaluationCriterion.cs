using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Entities;

[Table("problem_evaluation_criteria")]
[Index("Active", "DisplayOrder", Name = "idx_problem_eval_criteria_active_order")]
[Index("Code", Name = "uq_problem_evaluation_criteria_code", IsUnique = true)]
public partial class ProblemEvaluationCriterion
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("category")]
    [StringLength(100)]
    public string Category { get; set; } = null!;

    [Column("question")]
    public string Question { get; set; } = null!;

    [Column("suggestion")]
    public string? Suggestion { get; set; }

    [Column("max_score")]
    public int MaxScore { get; set; }

    [Column("display_order")]
    public int DisplayOrder { get; set; }

    [Column("active")]
    public bool Active { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("code")]
    [StringLength(30)]
    public string Code { get; set; } = null!;
}
