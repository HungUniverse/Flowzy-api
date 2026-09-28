using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Flowzy.Api.Configuration;
using Flowzy.Repository.Data;
using Flowzy.Repository.Entities;
using Flowzy.Repository.Repositories;
using Flowzy.Service.Authentication;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Flowzy.Tests;

public sealed class DeploymentConfigurationTests
{
    private static IConfiguration Config(Dictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();

    [Theory]
    [InlineData("*")]
    [InlineData("https://*.example.test")]
    [InlineData("http://app.example.test")]
    [InlineData("https://app.example.test/login")]
    [InlineData("https://app.example.test/")]
    [InlineData("")]
    public void ProductionRejectsUnsafeOrigins(string origin)
    {
        Action validate = () => DeploymentConfiguration.Origins(Config(new() { ["Cors:AllowedOrigins"] = origin }), "Cors:AllowedOrigins", true);
        validate.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ExactMultipleOriginsAndLocalDevelopmentAreSupported()
    {
        DeploymentConfiguration.Origins(Config(new() { ["Cors:AllowedOrigins"] = "https://a.example.test, https://b.example.test" }), "Cors:AllowedOrigins", true)
            .Should().Equal("https://a.example.test", "https://b.example.test");
        DeploymentConfiguration.Origins(Config(new() { ["Cors:AllowedOrigins"] = "http://localhost:3000" }), "Cors:AllowedOrigins", false)
            .Should().ContainSingle();
    }

    [Theory]
    [InlineData("invalid-base64")]
    [InlineData("c2hvcnQ=")]
    public void StartupRejectsInvalidOrWeakSigningKeys(string key)
    {
        Action validate = () => DeploymentConfiguration.Validate(Config(new() { ["Jwt:SecretKey"] = key }), false);
        validate.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData("Require", "api.example.test")]
    [InlineData("Disable", "api.example.test")]
    [InlineData("VerifyFull", "*")]
    public void ProductionRequiresCertificateVerificationAndExplicitHosts(string sslMode, string hosts)
    {
        var settings = Config(new()
        {
            ["Jwt:SecretKey"] = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)),
            ["ConnectionStrings:Default"] = $"Host=db;Database=flowzy;Username=user;Password=random-test-password;SSL Mode={sslMode}",
            ["AllowedHosts"] = hosts
        });
        Action validate = () => DeploymentConfiguration.Validate(settings, true);
        validate.Should().Throw<InvalidOperationException>();
    }

    [Theory]
    [InlineData("172.30.60.2", "https")]
    [InlineData("192.0.2.15", "http")]
    public async Task ForwardedHeadersAreAcceptedOnlyFromConfiguredProxy(string remoteAddress, string expectedScheme)
    {
        var options = new ForwardedHeadersOptions();
        DeploymentConfiguration.ConfigureForwarding(options, Config(new() { ["ReverseProxy:KnownProxies"] = "172.30.60.2" }));
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(remoteAddress);
        context.Request.Scheme = "http";
        context.Request.Headers["X-Forwarded-Proto"] = "https";
        context.Request.Headers["X-Forwarded-For"] = "198.51.100.10";
        var middleware = new ForwardedHeadersMiddleware(_ => Task.CompletedTask, NullLoggerFactory.Instance, Options.Create(options));
        await middleware.Invoke(context);
        context.Request.Scheme.Should().Be(expectedScheme);
    }
}

public sealed class ProductionHardeningTests(FlowzyApiFactory factory) : IClassFixture<FlowzyApiFactory>
{
    [Fact]
    public async Task LogoutRemainsRevokedInFreshHostAndAllowsImmediateNewLogin()
    {
        using var client = factory.CreateClient();
        var login = await client.PostAsJsonAsync("/api/auth/login", new { email = "admin.integration@local.test", password = "Integration123" });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        var token = document.RootElement.GetProperty("data").GetProperty("accessToken").GetString()!;
        var refresh = document.RootElement.GetProperty("data").GetProperty("refreshToken").GetString()!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        (await client.PostAsync("/api/auth/logout", null)).StatusCode.Should().Be(HttpStatusCode.OK);

        // A new service provider cannot see state held in the previous host's RAM.
        using var freshHost = factory.WithWebHostBuilder(_ => { });
        using var freshClient = freshHost.CreateClient();
        freshClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        (await freshClient.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        freshClient.DefaultRequestHeaders.Authorization = null;
        (await freshClient.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = refresh })).StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        await using var scope = freshHost.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FlowzyDbContext>();
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        (await db.RevokedAccessTokens.SingleAsync(x => x.TokenHash == hash)).TokenHash.Should().HaveLength(64);

        var newLogin = await freshClient.PostAsJsonAsync("/api/auth/login", new { email = "admin.integration@local.test", password = "Integration123" });
        newLogin.StatusCode.Should().Be(HttpStatusCode.OK);
        using var newDocument = JsonDocument.Parse(await newLogin.Content.ReadAsStringAsync());
        var newToken = newDocument.RootElement.GetProperty("data").GetProperty("accessToken").GetString()!;
        newToken.Should().NotBe(token);
        freshClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", newToken);
        (await freshClient.GetAsync("/api/auth/me")).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ConcurrentRevocationIsIdempotentAndCleanupPreservesLiveTokens()
    {
        using var client = factory.CreateClient();
        var hash = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var expiry = DateTime.UtcNow.AddHours(1);
        await Task.WhenAll(Enumerable.Range(0, 8).Select(async i =>
        {
            await using var scope = factory.Services.CreateAsyncScope();
            await scope.ServiceProvider.GetRequiredService<ITokenBlacklistRepository>().RevokeAsync(hash, expiry.AddSeconds(i), default);
        }));
        await using var verify = factory.Services.CreateAsyncScope();
        var db = verify.ServiceProvider.GetRequiredService<FlowzyDbContext>();
        var expiredHash = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        db.RevokedAccessTokens.Add(new RevokedAccessToken { TokenHash = expiredHash, ExpiresAt = DateTime.UtcNow.AddMinutes(-1) });
        await db.SaveChangesAsync();
        var repository = verify.ServiceProvider.GetRequiredService<ITokenBlacklistRepository>();
        await repository.DeleteExpiredAsync(DateTime.UtcNow, default);
        (await db.RevokedAccessTokens.CountAsync(x => x.TokenHash == hash)).Should().Be(1);
        (await repository.IsRevokedAsync(hash, DateTime.UtcNow, default)).Should().BeTrue();
        (await db.RevokedAccessTokens.AnyAsync(x => x.TokenHash == expiredHash)).Should().BeFalse();
    }

    [Fact]
    public async Task CorsAllowsConfiguredFrontendAndRejectsOtherPreflights()
    {
        using var client = factory.CreateClient();
        foreach (var origin in new[] { "http://localhost:3000", "https://attacker.example.test" })
        {
            using var request = new HttpRequestMessage(HttpMethod.Options, "/api/auth/login");
            request.Headers.Add("Origin", origin);
            request.Headers.Add("Access-Control-Request-Method", "POST");
            request.Headers.Add("Access-Control-Request-Headers", "content-type,authorization");
            using var response = await client.SendAsync(request);
            if (origin == "http://localhost:3000")
                response.Headers.GetValues("Access-Control-Allow-Origin").Should().Equal(origin);
            else response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
        }
    }

    [Fact]
    public async Task WebSocketAcceptsAllowedOriginAndChecksPersistentRevocationAtConnect()
    {
        using var http = factory.CreateClient();
        var wsClient = factory.Server.CreateWebSocketClient();
        wsClient.ConfigureRequest = request => request.Headers.Add("Origin", "http://localhost:3000");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        using var socket = await wsClient.ConnectAsync(new Uri("ws://localhost/ws"), timeout.Token);
        string token;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            token = scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateAccessToken(new Account { Email = "admin.integration@local.test", Role = "ADMIN" });
            await scope.ServiceProvider.GetRequiredService<ITokenBlacklistService>().BlacklistAsync(token);
        }
        await socket.SendAsync(Encoding.UTF8.GetBytes($"CONNECT\nAuthorization:Bearer {token}\n\n\0"), WebSocketMessageType.Text, true, timeout.Token);
        var buffer = new byte[4096];
        var message = await socket.ReceiveAsync(buffer, timeout.Token);
        Encoding.UTF8.GetString(buffer, 0, message.Count).Should().Contain("ERROR").And.Contain("Token is blacklisted");
        socket.Abort();
    }

    [Fact]
    public async Task WebSocketRejectsUnlistedOriginBeforeUpgrade()
    {
        using var http = factory.CreateClient();
        var client = factory.Server.CreateWebSocketClient();
        client.ConfigureRequest = request => request.Headers.Add("Origin", "https://attacker.example.test");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        Func<Task> connect = async () => { using var socket = await client.ConnectAsync(new Uri("ws://localhost/ws"), timeout.Token); };
        await connect.Should().ThrowAsync<InvalidOperationException>();
    }
}

public sealed class HealthFailureApiFactory : FlowzyApiFactory
{
    public async Task SetDatabaseAvailableAsync(bool available)
    {
        // Disable this fixture's database without changing its randomly assigned Docker port.
        var result = await DatabaseContainer.ExecAsync(["psql", "-U", "flowzy_test", "-d", "postgres", "-v", "ON_ERROR_STOP=1", "-c",
            available ? "ALTER DATABASE flowzy_test ALLOW_CONNECTIONS true;" : "ALTER DATABASE flowzy_test ALLOW_CONNECTIONS false;"]);
        result.ExitCode.Should().Be(0, result.Stderr);
        if (!available)
        {
            result = await DatabaseContainer.ExecAsync(["psql", "-U", "flowzy_test", "-d", "postgres", "-v", "ON_ERROR_STOP=1", "-c",
                "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname='flowzy_test';"]);
            result.ExitCode.Should().Be(0, result.Stderr);
        }
    }
}

public sealed class DatabaseHealthTests(HealthFailureApiFactory factory) : IClassFixture<HealthFailureApiFactory>
{
    [Fact]
    public async Task HealthReturns503WhileDatabaseIsDownAndRecovers()
    {
        using var client = factory.CreateClient();
        (await client.GetStringAsync("/actuator/health")).Should().Be("{\"status\":\"UP\"}");
        await factory.SetDatabaseAvailableAsync(false);
        try
        {
            using var down = await client.GetAsync("/actuator/health");
            down.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
            (await down.Content.ReadAsStringAsync()).Should().Be("{\"status\":\"DOWN\"}");
        }
        finally { await factory.SetDatabaseAvailableAsync(true); }
        using var recovery = new CancellationTokenSource(TimeSpan.FromSeconds(15));
        while (true)
        {
            using var result = await client.GetAsync("/actuator/health", recovery.Token);
            if (result.IsSuccessStatusCode)
            {
                (await result.Content.ReadAsStringAsync()).Should().Be("{\"status\":\"UP\"}");
                break;
            }
            await Task.Delay(250, recovery.Token);
        }
    }
}
