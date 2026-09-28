using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Entities;

[Table("import_batches")]
[Index("CreatedBy", Name = "idx_import_batches_created_by")]
[Index("StartedAt", Name = "idx_import_batches_started_at", AllDescending = true)]
public partial class ImportBatch
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("target_type")]
    [StringLength(30)]
    public string TargetType { get; set; } = null!;

    [Column("file_name")]
    [StringLength(255)]
    public string FileName { get; set; } = null!;

    [Column("file_type")]
    [StringLength(20)]
    public string FileType { get; set; } = null!;

    [Column("status")]
    [StringLength(30)]
    public string Status { get; set; } = null!;

    [Column("total_rows")]
    public int TotalRows { get; set; }

    [Column("success_rows")]
    public int SuccessRows { get; set; }

    [Column("failed_rows")]
    public int FailedRows { get; set; }

    [Column("started_at")]
    public DateTime StartedAt { get; set; }

    [Column("finished_at")]
    public DateTime? FinishedAt { get; set; }

    [Column("created_by")]
    public long? CreatedBy { get; set; }

    [ForeignKey("CreatedBy")]
    [InverseProperty("ImportBatches")]
    public virtual Account? CreatedByNavigation { get; set; }

    [InverseProperty("Batch")]
    public virtual ICollection<ImportRowError> ImportRowErrors { get; set; } = new List<ImportRowError>();

    [InverseProperty("ImportBatch")]
    public virtual ICollection<StudentGroup> StudentGroups { get; set; } = new List<StudentGroup>();
}
