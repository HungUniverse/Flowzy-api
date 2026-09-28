using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Entities;

[Table("group_invitations")]
[Index("GroupId", Name = "idx_group_invitations_group_id")]
[Index("InviteeStudentId", Name = "idx_group_invitations_invitee_student_id")]
[Index("InviterStudentId", Name = "idx_group_invitations_inviter_student_id")]
[Index("Status", Name = "idx_group_invitations_status")]
public partial class GroupInvitation
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("group_id")]
    public long GroupId { get; set; }

    [Column("inviter_student_id")]
    public long InviterStudentId { get; set; }

    [Column("invitee_student_id")]
    public long InviteeStudentId { get; set; }

    [Column("status")]
    [StringLength(30)]
    public string Status { get; set; } = null!;

    [Column("message")]
    [StringLength(500)]
    public string? Message { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("responded_at")]
    public DateTime? RespondedAt { get; set; }

    [ForeignKey("GroupId")]
    [InverseProperty("GroupInvitations")]
    public virtual StudentGroup Group { get; set; } = null!;

    [ForeignKey("InviteeStudentId")]
    [InverseProperty("GroupInvitationInviteeStudents")]
    public virtual Student InviteeStudent { get; set; } = null!;

    [ForeignKey("InviterStudentId")]
    [InverseProperty("GroupInvitationInviterStudents")]
    public virtual Student InviterStudent { get; set; } = null!;
}
