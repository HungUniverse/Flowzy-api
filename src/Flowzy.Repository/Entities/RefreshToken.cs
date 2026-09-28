using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Entities;

[Table("refresh_tokens")]
[Index("AccountId", Name = "idx_refresh_tokens_account_id")]
[Index("TokenHash", Name = "refresh_tokens_token_hash_key", IsUnique = true)]
public partial class RefreshToken
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("account_id")]
    public long AccountId { get; set; }

    [Column("token_hash")]
    [StringLength(255)]
    public string TokenHash { get; set; } = null!;

    [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

    [Column("revoked_at")]
    public DateTime? RevokedAt { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey("AccountId")]
    [InverseProperty("RefreshTokens")]
    public virtual Account Account { get; set; } = null!;
}
