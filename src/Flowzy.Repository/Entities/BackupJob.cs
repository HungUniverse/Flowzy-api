using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Entities;

[Table("backup_jobs")]
[Index("CreatedAt", "Id", Name = "idx_backup_jobs_created_at_id", AllDescending = true)]
[Index("RequestedByAccountId", Name = "idx_backup_jobs_requested_by")]
[Index("Status", Name = "idx_backup_jobs_status")]
public partial class BackupJob
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("trigger_type")]
    [StringLength(30)]
    public string TriggerType { get; set; } = null!;

    [Column("status")]
    [StringLength(30)]
    public string Status { get; set; } = null!;

    [Column("file_name")]
    [StringLength(255)]
    public string? FileName { get; set; }

    [Column("file_path")]
    [StringLength(1000)]
    public string? FilePath { get; set; }

    [Column("file_size_bytes")]
    public long? FileSizeBytes { get; set; }

    [Column("error_message")]
    public string? ErrorMessage { get; set; }

    [Column("requested_by_account_id")]
    public long? RequestedByAccountId { get; set; }

    [Column("started_at")]
    public DateTime? StartedAt { get; set; }

    [Column("finished_at")]
    public DateTime? FinishedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("RequestedByAccountId")]
    [InverseProperty("BackupJobs")]
    public virtual Account? RequestedByAccount { get; set; }
}
