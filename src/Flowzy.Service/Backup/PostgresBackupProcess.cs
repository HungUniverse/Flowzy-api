using System.Diagnostics;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace Flowzy.Service.Backup;

public sealed class PostgresBackupProcess(IConfiguration configuration) : IPostgresBackupProcess
{
    private NpgsqlConnectionStringBuilder Connection => new(configuration.GetConnectionString("Default"));
    public Task Dump(string output, CancellationToken ct) => Run(configuration["Backup:PgDumpPath"] ?? "pg_dump",
        ["--format=custom", "--blobs", "--no-owner", "--no-acl", .. ConnectionArgs(), "--file", output], ct);
    public Task Restore(string input, CancellationToken ct) => Run(configuration["Backup:PgRestorePath"] ?? "pg_restore",
        ["--clean", "--if-exists", "--no-owner", "--no-acl", "--single-transaction", "--exit-on-error", .. ConnectionArgs(), input], ct);
    private string[] ConnectionArgs() => ["--host", Connection.Host!, "--port", Connection.Port.ToString(CultureInfo.InvariantCulture),
        "--username", Connection.Username!, "--dbname", Connection.Database!];
    private async Task Run(string executable, string[] arguments, CancellationToken ct)
    {
        using var process = new Process { StartInfo = new(executable) { RedirectStandardOutput = true, RedirectStandardError = true,
            UseShellExecute = false, CreateNoWindow = true } };
        foreach (var argument in arguments) process.StartInfo.ArgumentList.Add(argument);
        var connection = Connection;
        process.StartInfo.Environment["PGPASSWORD"] = connection.Password;
        process.StartInfo.Environment["PGSSLMODE"] = connection.SslMode.ToString().ToLowerInvariant().Replace("verifyfull", "verify-full").Replace("verifyca", "verify-ca");
        // libpq does not read Npgsql's connection string; forward its TLS settings explicitly.
        if (!string.IsNullOrWhiteSpace(connection.RootCertificate))
            process.StartInfo.Environment["PGSSLROOTCERT"] = connection.RootCertificate;
        process.StartInfo.Environment["PGCONNECT_TIMEOUT"] = connection.Timeout.ToString(CultureInfo.InvariantCulture);
        process.Start();
        var stdout = ReadBounded(process.StandardOutput, ct); var stderr = ReadBounded(process.StandardError, ct);
        try { await process.WaitForExitAsync(ct); }
        catch (OperationCanceledException) { if (!process.HasExited) process.Kill(entireProcessTree: true); await process.WaitForExitAsync(CancellationToken.None); throw; }
        var output = (await stdout) + (await stderr);
        if (process.ExitCode != 0) throw new InvalidOperationException($"{Path.GetFileNameWithoutExtension(executable)} exited with code {process.ExitCode}: {output}");
    }
    private static async Task<string> ReadBounded(StreamReader reader, CancellationToken ct)
    {
        var output = new StringBuilder(); var buffer = new char[1024]; int count;
        while ((count = await reader.ReadAsync(buffer.AsMemory(), ct)) != 0)
            if (output.Length < 4000) output.Append(buffer, 0, Math.Min(count, 4000 - output.Length));
        return output.ToString();
    }
}
