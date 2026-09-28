using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Entities;

[Table("notifications")]
[Index("RecipientId", "CreatedAt", "Id", Name = "idx_notifications_recipient_created_id", IsDescending = new[] { false, true, true })]
[Index("RecipientId", "ReadAt", "CreatedAt", Name = "idx_notifications_recipient_read_created", IsDescending = new[] { false, false, true })]
public partial class Notification
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("recipient_id")]
    public long RecipientId { get; set; }

    [Column("type")]
    [StringLength(100)]
    public string Type { get; set; } = null!;

    [Column("title")]
    [StringLength(255)]
    public string Title { get; set; } = null!;

    [Column("body")]
    public string Body { get; set; } = null!;

    [Column("action_url")]
    [StringLength(500)]
    public string? ActionUrl { get; set; }

    [Column("entity_type")]
    [StringLength(100)]
    public string? EntityType { get; set; }

    [Column("entity_id")]
    [StringLength(100)]
    public string? EntityId { get; set; }

    [Column("payload")]
    public string? Payload { get; set; }

    [Column("read_at")]
    public DateTime? ReadAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [Column("action_key")]
    [StringLength(100)]
    public string? ActionKey { get; set; }

    [Column("action_params")]
    public string? ActionParams { get; set; }

    [Column("event_key")]
    [StringLength(255)]
    public string? EventKey { get; set; }

    [ForeignKey("RecipientId")]
    [InverseProperty("Notifications")]
    public virtual Account Recipient { get; set; } = null!;
}
