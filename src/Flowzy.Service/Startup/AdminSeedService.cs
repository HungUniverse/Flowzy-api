using Flowzy.Repository.Entities;
using Flowzy.Repository.Repositories;
using Flowzy.Service.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Flowzy.Service.Startup;

public sealed class AdminSeedService(
    IAccountRepository accounts,
    IOptions<AdminOptions> options,
    ILogger<AdminSeedService> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var admin = options.Value;
        if (string.IsNullOrWhiteSpace(admin.Email) || string.IsNullOrWhiteSpace(admin.Password))
        {
            logger.LogWarning("Admin credentials are not configured. Skipping admin account seed");
            return;
        }
        if (await accounts.ExistsByEmailAsync(admin.Email, cancellationToken))
        {
            return;
        }
        var now = DateTime.UtcNow;
        await accounts.AddAsync(new Account
        {
            Email = admin.Email.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(admin.Password),
            Role = "ADMIN",
            Status = "ACTIVE",
            MustChangePassword = true,
            CreatedAt = now,
            UpdatedAt = now
        }, cancellationToken);
        await accounts.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Seeded admin account {Email}", admin.Email);
    }
}
