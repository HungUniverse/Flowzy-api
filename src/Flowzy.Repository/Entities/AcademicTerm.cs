using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Entities;

[Table("academic_terms")]
[Index("Code", Name = "academic_terms_code_key", IsUnique = true)]
[Index("ClosedByAccountId", Name = "idx_academic_terms_closed_by")]
public partial class AcademicTerm
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("code")]
    [StringLength(30)]
    public string Code { get; set; } = null!;

    [Column("status")]
    [StringLength(30)]
    public string Status { get; set; } = null!;

    [Column("closed_at")]
    public DateTime? ClosedAt { get; set; }

    [Column("closed_by_account_id")]
    public long? ClosedByAccountId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("ClosedByAccountId")]
    [InverseProperty("AcademicTerms")]
    public virtual Account? ClosedByAccount { get; set; }

    [InverseProperty("TermNavigation")]
    public virtual ICollection<StudentGroup> StudentGroups { get; set; } = new List<StudentGroup>();

    [InverseProperty("AcademicTerm")]
    public virtual ICollection<TermFeedback> TermFeedbacks { get; set; } = new List<TermFeedback>();
}
