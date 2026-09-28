using Flowzy.Service.Platform;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Flowzy.Api.Health;

public sealed class DatabaseHealthCheck(IDatabaseHealthService database) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default) =>
        await database.IsAvailableAsync(cancellationToken)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Database is unavailable");
}
