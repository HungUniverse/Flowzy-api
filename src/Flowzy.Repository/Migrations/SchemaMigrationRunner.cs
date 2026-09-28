using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Flowzy.Repository.Migrations;

public sealed partial class SchemaMigrationRunner(
    string connectionString,
    ILogger<SchemaMigrationRunner> logger)
{
    public async Task MigrateAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await EnsureJournalAsync(connection, cancellationToken);
        await ImportFlywayHistoryAsync(connection, cancellationToken);

        var applied = await GetAppliedVersionsAsync(connection, cancellationToken);
        foreach (var migration in LoadMigrations())
        {
            if (applied.Contains(migration.Version))
            {
                continue;
            }

            logger.LogInformation("Applying database migration {Migration}", migration.Script);
            await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                await using (var command = new NpgsqlCommand(migration.Sql, connection, transaction))
                {
                    command.CommandTimeout = 180;
                    await command.ExecuteNonQueryAsync(cancellationToken);
                }

                await using (var journal = new NpgsqlCommand(
                    """
                    INSERT INTO flowzy_schema_history(version, script, checksum)
                    VALUES (@version, @script, @checksum)
                    """, connection, transaction))
                {
                    journal.Parameters.AddWithValue("version", migration.Version);
                    journal.Parameters.AddWithValue("script", migration.Script);
                    journal.Parameters.AddWithValue("checksum", migration.Checksum);
                    await journal.ExecuteNonQueryAsync(cancellationToken);
                }

                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        }
    }

    private static async Task EnsureJournalAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        await using var command = new NpgsqlCommand(
            """
            CREATE TABLE IF NOT EXISTS flowzy_schema_history (
                version INTEGER PRIMARY KEY,
                script VARCHAR(255) NOT NULL,
                checksum VARCHAR(64) NOT NULL,
                installed_on TIMESTAMPTZ NOT NULL DEFAULT CURRENT_TIMESTAMP
            )
            """, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task ImportFlywayHistoryAsync(NpgsqlConnection connection, CancellationToken cancellationToken)
    {
        await using var exists = new NpgsqlCommand("SELECT to_regclass('public.flyway_schema_history') IS NOT NULL", connection);
        if (!Convert.ToBoolean(await exists.ExecuteScalarAsync(cancellationToken)))
        {
            return;
        }

        var migrations = LoadMigrations().ToDictionary(item => item.Version);
        await using var command = new NpgsqlCommand(
            "SELECT version FROM flyway_schema_history WHERE success = TRUE AND version ~ '^[0-9]+$'", connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        var flywayVersions = new List<int>();
        while (await reader.ReadAsync(cancellationToken))
        {
            flywayVersions.Add(int.Parse(reader.GetString(0)));
        }
        await reader.CloseAsync();

        foreach (var version in flywayVersions)
        {
            // Only V1-V33 are shared with Java; subsequent migrations are owned by Flowzy.
            if (version > 33) continue;
            if (!migrations.TryGetValue(version, out var migration))
            {
                continue;
            }

            await using var insert = new NpgsqlCommand(
                """
                INSERT INTO flowzy_schema_history(version, script, checksum)
                VALUES (@version, @script, @checksum)
                ON CONFLICT (version) DO NOTHING
                """, connection);
            insert.Parameters.AddWithValue("version", version);
            insert.Parameters.AddWithValue("script", migration.Script);
            insert.Parameters.AddWithValue("checksum", migration.Checksum);
            await insert.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task<HashSet<int>> GetAppliedVersionsAsync(
        NpgsqlConnection connection,
        CancellationToken cancellationToken)
    {
        var result = new HashSet<int>();
        await using var command = new NpgsqlCommand("SELECT version FROM flowzy_schema_history", connection);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(reader.GetInt32(0));
        }
        return result;
    }

    private static IReadOnlyList<MigrationScript> LoadMigrations()
    {
        var assembly = typeof(SchemaMigrationRunner).Assembly;
        return assembly.GetManifestResourceNames()
            .Select(name => (Name: name, Match: MigrationResourceRegex().Match(name)))
            .Where(item => item.Match.Success)
            .Select(item =>
            {
                using var stream = assembly.GetManifestResourceStream(item.Name)
                    ?? throw new InvalidOperationException($"Migration resource not found: {item.Name}");
                using var reader = new StreamReader(stream, Encoding.UTF8);
                var sql = reader.ReadToEnd();
                var script = item.Match.Groups["script"].Value;
                var checksum = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(sql)));
                return new MigrationScript(int.Parse(item.Match.Groups["version"].Value), script, sql, checksum);
            })
            .OrderBy(item => item.Version)
            .ToArray();
    }

    [GeneratedRegex(@"\.Migrations\.(?<script>V(?<version>\d+)__.+\.sql)$", RegexOptions.IgnoreCase)]
    private static partial Regex MigrationResourceRegex();

    private sealed record MigrationScript(int Version, string Script, string Sql, string Checksum);
}
