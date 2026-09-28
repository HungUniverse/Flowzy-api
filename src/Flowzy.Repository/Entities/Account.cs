using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Entities;

[Table("accounts")]
[Index("Email", Name = "accounts_email_key", IsUnique = true)]
[Index("Role", "Status", Name = "idx_accounts_role_status")]
public partial class Account
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("email")]
    [StringLength(255)]
    public string Email { get; set; } = null!;

    [Column("password_hash")]
    [StringLength(255)]
    public string PasswordHash { get; set; } = null!;

    [Column("role")]
    [StringLength(30)]
    public string Role { get; set; } = null!;

    [Column("status")]
    [StringLength(30)]
    public string Status { get; set; } = null!;

    [Column("must_change_password")]
    public bool MustChangePassword { get; set; }

    [Column("last_login_at")]
    public DateTime? LastLoginAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [InverseProperty("ClosedByAccount")]
    public virtual ICollection<AcademicTerm> AcademicTerms { get; set; } = new List<AcademicTerm>();

    [InverseProperty("RequestedByAccount")]
    public virtual ICollection<BackupJob> BackupJobs { get; set; } = new List<BackupJob>();

    [InverseProperty("UpdatedByAccount")]
    public virtual ICollection<BackupScheduleSetting> BackupScheduleSettings { get; set; } = new List<BackupScheduleSetting>();

    [InverseProperty("CreatedByNavigation")]
    public virtual ICollection<ImportBatch> ImportBatches { get; set; } = new List<ImportBatch>();

    [InverseProperty("Account")]
    public virtual Instructor? Instructor { get; set; }

    [InverseProperty("Account")]
    public virtual Mentor? Mentor { get; set; }

    [InverseProperty("Recipient")]
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    [InverseProperty("ReviewedByAccount")]
    public virtual ICollection<Problem> Problems { get; set; } = new List<Problem>();

    [InverseProperty("Account")]
    public virtual ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    [InverseProperty("Account")]
    public virtual Student? Student { get; set; }

    [InverseProperty("ActorAccount")]
    public virtual ICollection<TaskActivity> TaskActivities { get; set; } = new List<TaskActivity>();

    [InverseProperty("CompletedByAccount")]
    public virtual ICollection<TaskChecklistItem> TaskChecklistItems { get; set; } = new List<TaskChecklistItem>();

    [InverseProperty("AuthorAccount")]
    public virtual ICollection<TaskComment> TaskComments { get; set; } = new List<TaskComment>();
}
