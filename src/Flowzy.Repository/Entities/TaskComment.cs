using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Entities;

[Table("task_comments")]
[Index("TaskId", "CreatedAt", Name = "idx_task_comments_task_created", IsDescending = new[] { false, true })]
public partial class TaskComment
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("task_id")]
    public long TaskId { get; set; }

    [Column("author_account_id")]
    public long AuthorAccountId { get; set; }

    [Column("content")]
    public string Content { get; set; } = null!;

    [Column("edited_at")]
    public DateTime? EditedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("AuthorAccountId")]
    [InverseProperty("TaskComments")]
    public virtual Account AuthorAccount { get; set; } = null!;

    [ForeignKey("TaskId")]
    [InverseProperty("TaskComments")]
    public virtual GroupTask Task { get; set; } = null!;
}
