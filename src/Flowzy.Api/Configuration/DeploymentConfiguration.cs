using System.Net;
using Microsoft.AspNetCore.HttpOverrides;
using Npgsql;

namespace Flowzy.Api.Configuration;

public static class DeploymentConfiguration
{
    public static string[] Origins(IConfiguration configuration, string key, bool production)
    {
        var section = configuration.GetSection(key);
        var values = section.Value is not null
            ? section.Value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : section.GetChildren().Select(x => x.Value ?? string.Empty).ToArray();
        if (values.Length == 0) throw new InvalidOperationException($"{key} must contain at least one explicit frontend origin");
        foreach (var value in values)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) ||
                (uri.Scheme != "https" && uri.Scheme != "http") ||
                uri.GetLeftPart(UriPartial.Authority) != value || uri.Host.Contains('*') ||
                uri.UserInfo.Length != 0 || (production && uri.Scheme != "https"))
                throw new InvalidOperationException($"{key} must contain exact {(production ? "HTTPS" : "HTTP(S)")} origins without paths or wildcards");
        }
        return values.Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public static byte[] Validate(IConfiguration configuration, bool production)
    {
        byte[] secret;
        try { secret = Convert.FromBase64String(configuration["Jwt:SecretKey"] ?? string.Empty); }
        catch (FormatException) { throw new InvalidOperationException("Jwt:SecretKey must be Base64 encoded"); }
        if (secret.Length < 32) throw new InvalidOperationException("Jwt:SecretKey must contain at least 32 random bytes encoded as Base64");
        if (!production) return secret;

        var connection = new NpgsqlConnectionStringBuilder(configuration.GetConnectionString("Default"));
        if (string.IsNullOrWhiteSpace(connection.Host) || string.IsNullOrWhiteSpace(connection.Database) ||
            string.IsNullOrWhiteSpace(connection.Username) || string.IsNullOrWhiteSpace(connection.Password))
            throw new InvalidOperationException("Production database connection details are required");
        if (connection.SslMode != SslMode.VerifyFull)
            throw new InvalidOperationException("Production database requires SSL Mode=VerifyFull");
        if (connection.Password == "flowzy_local_password" ||
            configuration["Admin:Password"] == "AdminPassword123" ||
            configuration["Jwt:SecretKey"] == "dGhpcy1pcy1hLXZlcnktc2VjdXJlLWFuZC1sb25nLXNlY3JldC1rZXktZm9yLWYtc3BhcmstYXBpLTIwMjY=")
            throw new InvalidOperationException("Local demonstration credentials cannot be used in production");
        if (string.IsNullOrWhiteSpace(configuration["AllowedHosts"]) || configuration["AllowedHosts"]!.Contains('*'))
            throw new InvalidOperationException("AllowedHosts must explicitly list production API hostnames");
        return secret;
    }

    public static void ConfigureForwarding(ForwardedHeadersOptions options, IConfiguration configuration)
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = 1;
        // Keep framework loopback defaults. Never trust forwarded headers from arbitrary clients.
        foreach (var address in (configuration["ReverseProxy:KnownProxies"] ?? string.Empty)
                     .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (!IPAddress.TryParse(address, out var ip))
                throw new InvalidOperationException("ReverseProxy:KnownProxies must contain IP addresses");
            options.KnownProxies.Add(ip);
        }
    }
}
