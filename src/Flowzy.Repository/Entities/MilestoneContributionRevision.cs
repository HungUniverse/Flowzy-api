using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Entities;

[Table("milestone_contribution_revisions")]
[Index("MilestoneId", "GroupId", Name = "uq_contribution_revisions_scope", IsUnique = true)]
public partial class MilestoneContributionRevision
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("milestone_id")]
    public long MilestoneId { get; set; }

    [Column("group_id")]
    public long GroupId { get; set; }

    [Column("revision")]
    public int Revision { get; set; }

    [Column("submitted_by_student_id")]
    public long SubmittedByStudentId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("GroupId")]
    [InverseProperty("MilestoneContributionRevisions")]
    public virtual StudentGroup Group { get; set; } = null!;

    [ForeignKey("MilestoneId")]
    [InverseProperty("MilestoneContributionRevisions")]
    public virtual CourseMilestone Milestone { get; set; } = null!;

    [InverseProperty("Revision")]
    public virtual ICollection<MilestoneContributionAgreement> MilestoneContributionAgreements { get; set; } = new List<MilestoneContributionAgreement>();

    [ForeignKey("SubmittedByStudentId")]
    [InverseProperty("MilestoneContributionRevisions")]
    public virtual Student SubmittedByStudent { get; set; } = null!;
}
