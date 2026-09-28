using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Entities;

[Table("backup_schedule_settings")]
public partial class BackupScheduleSetting
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("enabled")]
    public bool Enabled { get; set; }

    [Column("cron_expression")]
    [StringLength(120)]
    public string CronExpression { get; set; } = null!;

    [Column("timezone")]
    [StringLength(120)]
    public string Timezone { get; set; } = null!;

    [Column("backup_dir")]
    [StringLength(1000)]
    public string BackupDir { get; set; } = null!;

    [Column("retention_days")]
    public int RetentionDays { get; set; }

    [Column("last_triggered_at")]
    public DateTime? LastTriggeredAt { get; set; }

    [Column("next_run_at")]
    public DateTime? NextRunAt { get; set; }

    [Column("updated_by_account_id")]
    public long? UpdatedByAccountId { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [ForeignKey("UpdatedByAccountId")]
    [InverseProperty("BackupScheduleSettings")]
    public virtual Account? UpdatedByAccount { get; set; }
}
