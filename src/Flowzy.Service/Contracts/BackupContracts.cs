using System.ComponentModel.DataAnnotations;
namespace Flowzy.Service.Contracts;
public sealed record BackupJobResponse(long Id,string TriggerType,string Status,string? FileName,long? FileSizeBytes,string? ErrorMessage,long? RequestedByAccountId,string? RequestedByEmail,DateTime? StartedAt,DateTime? FinishedAt,DateTime CreatedAt);
public sealed record RestoreBackupResponse(string FileName,long FileSizeBytes,DateTime RestoredAt);
public sealed record BackupScheduleResponse(bool Enabled,string CronExpression,string Timezone,string BackupDir,int RetentionDays,DateTime? LastTriggeredAt,DateTime? NextRunAt,long? UpdatedByAccountId,string? UpdatedByEmail,DateTime UpdatedAt);
public sealed record UpdateBackupScheduleRequest(
    [Required(ErrorMessage = "Enabled flag is required")] bool? Enabled,
    [Required(ErrorMessage = "Cron expression is required")] string CronExpression,
    [Required(ErrorMessage = "Timezone is required")] string Timezone,
    [BackupRetention] int? RetentionDays);

public sealed class BackupRetentionAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext) => value switch
    {
        int days when days < 1 => new("Retention days must be at least 1"),
        int days when days > 3650 => new("Retention days must not exceed 3650"),
        _ => ValidationResult.Success
    };
}
