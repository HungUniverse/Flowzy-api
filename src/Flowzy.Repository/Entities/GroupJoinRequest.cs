using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Entities;

[Table("group_join_requests")]
[Index("GroupId", Name = "idx_group_join_requests_group_id")]
[Index("RespondedByStudentId", Name = "idx_group_join_requests_responded_by")]
[Index("Status", Name = "idx_group_join_requests_status")]
[Index("StudentId", Name = "idx_group_join_requests_student_id")]
public partial class GroupJoinRequest
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("group_id")]
    public long GroupId { get; set; }

    [Column("student_id")]
    public long StudentId { get; set; }

    [Column("status")]
    [StringLength(30)]
    public string Status { get; set; } = null!;

    [Column("message")]
    [StringLength(500)]
    public string? Message { get; set; }

    [Column("responded_by_student_id")]
    public long? RespondedByStudentId { get; set; }

    [Column("responded_at")]
    public DateTime? RespondedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("GroupId")]
    [InverseProperty("GroupJoinRequests")]
    public virtual StudentGroup Group { get; set; } = null!;

    [ForeignKey("RespondedByStudentId")]
    [InverseProperty("GroupJoinRequestRespondedByStudents")]
    public virtual Student? RespondedByStudent { get; set; }

    [ForeignKey("StudentId")]
    [InverseProperty("GroupJoinRequestStudents")]
    public virtual Student Student { get; set; } = null!;
}
