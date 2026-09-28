using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Entities;

[Table("group_recruitment_needs")]
[Index("Role", "GroupId", Name = "idx_group_recruitment_needs_role_group")]
[Index("GroupId", "Role", Name = "uq_group_recruitment_needs_group_role", IsUnique = true)]
public partial class GroupRecruitmentNeed
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("group_id")]
    public long GroupId { get; set; }

    [Column("role")]
    [StringLength(100)]
    public string Role { get; set; } = null!;

    [Column("quantity")]
    public int Quantity { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("GroupId")]
    [InverseProperty("GroupRecruitmentNeeds")]
    public virtual StudentGroup Group { get; set; } = null!;
}
