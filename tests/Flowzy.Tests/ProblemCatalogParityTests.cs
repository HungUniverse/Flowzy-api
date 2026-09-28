using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Flowzy.Repository.Data;
using Flowzy.Repository.Entities;
using Flowzy.Service.Authentication;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Flowzy.Tests;

public sealed class ProblemCatalogParityTests(FlowzyApiFactory factory) : IClassFixture<FlowzyApiFactory>
{
    private async Task<HttpClient> Admin()
    {
        using var bootstrap = factory.CreateClient();
        await using var scope = factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<FlowzyDbContext>();
        var account = new Account { Email = Guid.NewGuid().ToString("N") + "@local.test", PasswordHash = "unused", Role = "ADMIN", Status = "ACTIVE" };
        db.Accounts.Add(account); await db.SaveChangesAsync();
        var client = factory.CreateClient(); client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateAccessToken(account));
        return client;
    }
    private static async Task<JsonElement> Body(HttpResponseMessage response, HttpStatusCode status = HttpStatusCode.OK)
    {
        var raw = await response.Content.ReadAsStringAsync(); response.StatusCode.Should().Be(status, raw);
        using var doc = JsonDocument.Parse(raw); return doc.RootElement.Clone();
    }

    [Theory]
    [InlineData("problems", "difficulty", "invalid")]
    [InlineData("problems", "difficulty", "beginner")]
    [InlineData("problems", "difficulty", "0")]
    [InlineData("problems", "sourceType", "official")]
    [InlineData("problems", "sourceType", "invalid")]
    [InlineData("problems", "status", "active")]
    [InlineData("problems", "status", "invalid")]
    [InlineData("problem-domains", "status", "active")]
    [InlineData("problem-domains", "status", "invalid")]
    public async Task UnknownOrLowercaseQueryEnumsAreRejected(string route, string parameter, string value)
    {
        using var client = await Admin();
        var body = await Body(await client.GetAsync($"/api/{route}?{parameter}={value}"), HttpStatusCode.BadRequest);
        body.GetProperty("message").GetString().Should().Be("Invalid parameter format: " + parameter);
    }

    [Fact]
    public async Task DomainSortUsesCreatedAtThenIdDescendingAndAcceptsValidFilters()
    {
        using var client = await Admin(); var key = Guid.NewGuid().ToString("N");
        long[] expected;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FlowzyDbContext>(); var now = DateTime.UtcNow;
            var old = new ProblemDomain { Code = "A" + key, Name = key, Status = "ACTIVE", CreatedAt = now.AddDays(-1) };
            var recent = new ProblemDomain { Code = "Z" + key, Name = key, Status = "ACTIVE", CreatedAt = now };
            var tie = new ProblemDomain { Code = "B" + key, Name = key, Status = "ACTIVE", CreatedAt = now };
            db.ProblemDomains.Add(old); await db.SaveChangesAsync(); db.ProblemDomains.Add(recent); await db.SaveChangesAsync(); db.ProblemDomains.Add(tie); await db.SaveChangesAsync();
            expected = [tie.Id, recent.Id, old.Id];
        }
        var body = await Body(await client.GetAsync($"/api/problem-domains?search={key}&status=ACTIVE"));
        body.GetProperty("data").EnumerateArray().Select(x => x.GetProperty("id").GetInt64()).Should().Equal(expected);
        await Body(await client.GetAsync("/api/problems?difficulty=BEGINNER&sourceType=OFFICIAL&status=PENDING_REVIEW"));
        await Body(await client.GetAsync("/api/problems?difficulty=&status=&sourceType="));
    }

    [Fact]
    public async Task AdminCanRetainInactiveDomainButCannotSwitchToAnotherInactiveDomain()
    {
        using var client = await Admin(); var key = Guid.NewGuid().ToString("N"); long id;
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<FlowzyDbContext>();
            var original = new ProblemDomain { Code = key, Name = "Original", Status = "INACTIVE" };
            var other = new ProblemDomain { Code = key + "X", Name = "Other", Status = "INACTIVE" };
            var p = new Problem { Domain = original, Title = "Title", Statement = "Statement", DifficultyLevel = "BEGINNER", SourceType = "OFFICIAL", Status = "ACTIVE" };
            db.ProblemDomains.Add(other); db.Problems.Add(p); await db.SaveChangesAsync(); id = p.Id;
        }
        var kept = await Body(await client.PatchAsJsonAsync($"/api/admin/problems/{id}", new { domainCode = key, title = "Updated" }));
        kept.GetProperty("data").GetProperty("domain").GetProperty("code").GetString().Should().Be(key);
        var rejected = await Body(await client.PatchAsJsonAsync($"/api/admin/problems/{id}", new { domainCode = key + "X", title = "Must not save" }), HttpStatusCode.BadRequest);
        rejected.GetProperty("message").GetString().Should().Be("Cannot assign problem to an INACTIVE domain");
        var saved = await Body(await client.GetAsync($"/api/problems/{id}"));
        saved.GetProperty("data").GetProperty("title").GetString().Should().Be("Updated");
        var cleared = await Body(await client.PatchAsJsonAsync($"/api/admin/problems/{id}", new { domainCode = " " }));
        cleared.GetProperty("data").GetProperty("domain").ValueKind.Should().Be(JsonValueKind.Null);
    }
}
