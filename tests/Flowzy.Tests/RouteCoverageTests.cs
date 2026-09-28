using System.Text.Json;
using System.Text.RegularExpressions;
using Flowzy.Api.Compatibility;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Flowzy.Tests;

public sealed class RouteCoverageTests(FlowzyApiFactory factory) : IClassFixture<FlowzyApiFactory>
{
    [Fact]
    public void EveryJavaOperationHasARealControllerAction()
    {
        using var client = factory.CreateClient();
        var source = factory.Services.GetRequiredService<EndpointDataSource>();
        var actual = source.Endpoints.OfType<RouteEndpoint>()
            .Where(e => e.Metadata.GetMetadata<ControllerActionDescriptor>() is not null)
            .SelectMany(e => (e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? [])
                .Select(method => method + " " + Normalize(e.RoutePattern.RawText!))).ToHashSet();
        using var oracle = JsonDocument.Parse(factory.Services.GetRequiredService<LegacyContract>().OpenApiJson);
        var expected = oracle.RootElement.GetProperty("paths").EnumerateObject()
            .SelectMany(path => path.Value.EnumerateObject()
                .Where(op => new[] { "get", "post", "put", "patch", "delete" }.Contains(op.Name))
                .Select(op => op.Name.ToUpperInvariant() + " " + Normalize(path.Name))).ToArray();
        expected.Where(op => !actual.Contains(op)).Should().BeEmpty("every Java operation needs real behavior, not a fallback");
        expected.Should().HaveCount(192, "the captured Java oracle has 192 HTTP operations across 153 paths");
    }

    private static string Normalize(string route) => Regex.Replace(route.Trim('/'), @"\{[^}]+\}", "{}").ToLowerInvariant();

    [Fact]
    public async Task UnknownApiDoesNotReturnSyntheticSuccess()
    {
        using var client = factory.CreateClient();
        var response = await client.GetAsync("/api/not-a-real-route");
        response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
        factory.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>()
            .Should().NotContain(e => e.RoutePattern.Parameters.Any(p => p.IsCatchAll));
    }
}
