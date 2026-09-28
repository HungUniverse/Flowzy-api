using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Entities;

[Table("group_tasks")]
[Index("BoardId", Name = "idx_group_tasks_board_id")]
[Index("BoardId", "ArchivedAt", "Status", "Position", Name = "idx_group_tasks_board_load")]
[Index("DueAt", "Status", Name = "idx_group_tasks_due_status")]
[Index("Priority", Name = "idx_group_tasks_priority")]
public partial class GroupTask
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("group_id")]
    public long GroupId { get; set; }

    [Column("title")]
    [StringLength(255)]
    public string Title { get; set; } = null!;

    [Column("description")]
    public string? Description { get; set; }

    [Column("status")]
    [StringLength(30)]
    public string Status { get; set; } = null!;

    [Column("priority")]
    [StringLength(30)]
    public string Priority { get; set; } = null!;

    [Column("due_at")]
    public DateTime? DueAt { get; set; }

    [Column("position")]
    public long Position { get; set; }

    [Column("created_by_student_id")]
    public long CreatedByStudentId { get; set; }

    [Column("archived_at")]
    public DateTime? ArchivedAt { get; set; }

    [Column("version")]
    public long Version { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("board_id")]
    public long BoardId { get; set; }

    [ForeignKey("BoardId")]
    [InverseProperty("GroupTasks")]
    public virtual TaskBoard Board { get; set; } = null!;

    [ForeignKey("CreatedByStudentId")]
    [InverseProperty("GroupTasks")]
    public virtual Student CreatedByStudent { get; set; } = null!;

    [ForeignKey("GroupId")]
    [InverseProperty("GroupTasks")]
    public virtual StudentGroup Group { get; set; } = null!;

    [InverseProperty("Task")]
    public virtual ICollection<TaskActivity> TaskActivities { get; set; } = new List<TaskActivity>();

    [InverseProperty("Task")]
    public virtual ICollection<TaskAssignee> TaskAssignees { get; set; } = new List<TaskAssignee>();

    [InverseProperty("Task")]
    public virtual ICollection<TaskChecklistItem> TaskChecklistItems { get; set; } = new List<TaskChecklistItem>();

    [InverseProperty("Task")]
    public virtual ICollection<TaskComment> TaskComments { get; set; } = new List<TaskComment>();
}
