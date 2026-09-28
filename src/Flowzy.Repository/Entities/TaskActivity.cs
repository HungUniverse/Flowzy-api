using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Entities;

[Table("task_activities")]
[Index("GroupId", "CreatedAt", Name = "idx_task_activities_group_created", IsDescending = new[] { false, true })]
[Index("TaskId", "CreatedAt", Name = "idx_task_activities_task_created", IsDescending = new[] { false, true })]
public partial class TaskActivity
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("task_id")]
    public long TaskId { get; set; }

    [Column("group_id")]
    public long GroupId { get; set; }

    [Column("actor_account_id")]
    public long? ActorAccountId { get; set; }

    [Column("activity_type")]
    [StringLength(50)]
    public string ActivityType { get; set; } = null!;

    [Column("details", TypeName = "jsonb")]
    public string Details { get; set; } = null!;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("ActorAccountId")]
    [InverseProperty("TaskActivities")]
    public virtual Account? ActorAccount { get; set; }

    [ForeignKey("GroupId")]
    [InverseProperty("TaskActivities")]
    public virtual StudentGroup Group { get; set; } = null!;

    [ForeignKey("TaskId")]
    [InverseProperty("TaskActivities")]
    public virtual GroupTask Task { get; set; } = null!;
}
