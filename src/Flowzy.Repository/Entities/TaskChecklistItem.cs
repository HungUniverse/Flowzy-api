using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Entities;

[Table("task_checklist_items")]
[Index("TaskId", "Position", Name = "idx_task_checklist_items_task_pos")]
public partial class TaskChecklistItem
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("task_id")]
    public long TaskId { get; set; }

    [Column("title")]
    [StringLength(500)]
    public string Title { get; set; } = null!;

    [Column("completed")]
    public bool Completed { get; set; }

    [Column("completed_by_account_id")]
    public long? CompletedByAccountId { get; set; }

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [Column("position")]
    public long Position { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("CompletedByAccountId")]
    [InverseProperty("TaskChecklistItems")]
    public virtual Account? CompletedByAccount { get; set; }

    [ForeignKey("TaskId")]
    [InverseProperty("TaskChecklistItems")]
    public virtual GroupTask Task { get; set; } = null!;
}
