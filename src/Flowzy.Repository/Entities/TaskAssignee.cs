using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Entities;

[Table("task_assignees")]
[Index("StudentId", "TaskId", Name = "idx_task_assignees_student_task")]
[Index("TaskId", "StudentId", Name = "uq_task_assignees_task_student", IsUnique = true)]
public partial class TaskAssignee
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("task_id")]
    public long TaskId { get; set; }

    [Column("student_id")]
    public long StudentId { get; set; }

    [Column("assigned_by_student_id")]
    public long AssignedByStudentId { get; set; }

    [Column("assigned_at")]
    public DateTime AssignedAt { get; set; }

    [ForeignKey("AssignedByStudentId")]
    [InverseProperty("TaskAssigneeAssignedByStudents")]
    public virtual Student AssignedByStudent { get; set; } = null!;

    [ForeignKey("StudentId")]
    [InverseProperty("TaskAssigneeStudents")]
    public virtual Student Student { get; set; } = null!;

    [ForeignKey("TaskId")]
    [InverseProperty("TaskAssignees")]
    public virtual GroupTask Task { get; set; } = null!;
}
