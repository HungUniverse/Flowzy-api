using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Entities;

[Table("milestone_contribution_agreements")]
[Index("RevisionId", Name = "idx_contribution_agreements_revision_id")]
[Index("StudentId", Name = "idx_contribution_agreements_student_id")]
[Index("RevisionId", "StudentId", Name = "uq_contribution_agreements_revision_student", IsUnique = true)]
public partial class MilestoneContributionAgreement
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("revision_id")]
    public long RevisionId { get; set; }

    [Column("student_id")]
    public long StudentId { get; set; }

    [Column("decision")]
    [StringLength(30)]
    public string Decision { get; set; } = null!;

    [Column("reason")]
    [StringLength(1000)]
    public string? Reason { get; set; }

    [Column("responded_at")]
    public DateTime RespondedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("RevisionId")]
    [InverseProperty("MilestoneContributionAgreements")]
    public virtual MilestoneContributionRevision Revision { get; set; } = null!;

    [ForeignKey("StudentId")]
    [InverseProperty("MilestoneContributionAgreements")]
    public virtual Student Student { get; set; } = null!;
}
