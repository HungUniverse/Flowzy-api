using Flowzy.Repository.Entities;
using Microsoft.EntityFrameworkCore;

namespace Flowzy.Repository.Data;

public partial class FlowzyDbContext
{
    public DbSet<RevokedAccessToken> RevokedAccessTokens => Set<RevokedAccessToken>();

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RevokedAccessToken>(entity =>
        {
            entity.ToTable("revoked_access_token");
            entity.HasKey(x => x.TokenHash);
            entity.Property(x => x.TokenHash).HasColumnName("token_hash").HasMaxLength(64);
            entity.Property(x => x.ExpiresAt).HasColumnName("expires_at");
            entity.HasIndex(x => x.ExpiresAt).HasDatabaseName("idx_revoked_access_token_expires_at");
        });
    }
}
