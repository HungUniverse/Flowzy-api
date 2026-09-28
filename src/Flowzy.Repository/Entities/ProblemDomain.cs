using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Entities;

[Table("problem_domains")]
[Index("Code", Name = "idx_problem_domains_code")]
[Index("Status", Name = "idx_problem_domains_status")]
[Index("Code", Name = "problem_domains_code_key", IsUnique = true)]
public partial class ProblemDomain
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("code")]
    [StringLength(50)]
    public string Code { get; set; } = null!;

    [Column("name")]
    [StringLength(255)]
    public string Name { get; set; } = null!;

    [Column("description")]
    public string? Description { get; set; }

    [Column("status")]
    [StringLength(30)]
    public string Status { get; set; } = null!;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("macro_domain")]
    [StringLength(255)]
    public string? MacroDomain { get; set; }

    [Column("sub_domain")]
    public string? SubDomain { get; set; }

    [Column("typical_examples")]
    public string? TypicalExamples { get; set; }

    [Column("primary_discipline")]
    [StringLength(255)]
    public string? PrimaryDiscipline { get; set; }

    [Column("supporting_disciplines")]
    public string? SupportingDisciplines { get; set; }

    [Column("best_sources")]
    public string? BestSources { get; set; }

    [Column("student_capabilities")]
    public string? StudentCapabilities { get; set; }

    [Column("potential_outputs")]
    public string? PotentialOutputs { get; set; }

    [Column("notes")]
    public string? Notes { get; set; }

    [InverseProperty("Domain")]
    public virtual ICollection<Problem> Problems { get; set; } = new List<Problem>();
}
