using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Entities;

[Table("student_group_members")]
[Index("GroupId", Name = "idx_student_group_members_group_id")]
[Index("StudentId", Name = "idx_student_group_members_student_id")]
[Index("GroupId", "StudentId", Name = "uq_student_group_members_group_student", IsUnique = true)]
public partial class StudentGroupMember
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("group_id")]
    public long GroupId { get; set; }

    [Column("student_id")]
    public long StudentId { get; set; }

    [Column("member_role")]
    [StringLength(30)]
    public string MemberRole { get; set; } = null!;

    [Column("joined_at")]
    public DateTime JoinedAt { get; set; }

    [ForeignKey("GroupId")]
    [InverseProperty("StudentGroupMembers")]
    public virtual StudentGroup Group { get; set; } = null!;

    [ForeignKey("StudentId")]
    [InverseProperty("StudentGroupMembers")]
    public virtual Student Student { get; set; } = null!;
}
