using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Entities;

[Table("import_row_errors")]
[Index("BatchId", Name = "idx_import_row_errors_batch_id")]
[Index("BatchId", "RowNumber", Name = "idx_import_row_errors_batch_row")]
public partial class ImportRowError
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("batch_id")]
    public long BatchId { get; set; }

    [Column("row_number")]
    public int RowNumber { get; set; }

    [Column("field_name")]
    [StringLength(100)]
    public string? FieldName { get; set; }

    [Column("error_code")]
    [StringLength(80)]
    public string ErrorCode { get; set; } = null!;

    [Column("error_message")]
    public string ErrorMessage { get; set; } = null!;

    [Column("raw_data", TypeName = "jsonb")]
    public string? RawData { get; set; }

    [ForeignKey("BatchId")]
    [InverseProperty("ImportRowErrors")]
    public virtual ImportBatch Batch { get; set; } = null!;
}
