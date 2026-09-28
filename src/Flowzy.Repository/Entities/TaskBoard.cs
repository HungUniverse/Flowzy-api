using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Entities;

[Table("task_boards")]
[Index("CreatedByStudentId", Name = "idx_task_boards_created_by")]
[Index("GroupId", Name = "idx_task_boards_group_id")]
public partial class TaskBoard
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("group_id")]
    public long GroupId { get; set; }

    [Column("name")]
    [StringLength(255)]
    public string Name { get; set; } = null!;

    [Column("description")]
    public string? Description { get; set; }

    [Column("position")]
    public long Position { get; set; }

    [Column("default_board")]
    public bool DefaultBoard { get; set; }

    [Column("created_by_student_id")]
    public long? CreatedByStudentId { get; set; }

    [Column("archived_at")]
    public DateTime? ArchivedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("CreatedByStudentId")]
    [InverseProperty("TaskBoards")]
    public virtual Student? CreatedByStudent { get; set; }

    [ForeignKey("GroupId")]
    [InverseProperty("TaskBoards")]
    public virtual StudentGroup Group { get; set; } = null!;

    [InverseProperty("Board")]
    public virtual ICollection<GroupTask> GroupTasks { get; set; } = new List<GroupTask>();
}
