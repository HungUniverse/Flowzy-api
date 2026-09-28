using System.Text.Json;
using Flowzy.Api.Compatibility;
using Flowzy.Service.Contracts;
using FluentAssertions;
using Xunit;

namespace Flowzy.Tests;

public sealed class ContractCompatibilityTests
{
    [Fact]
    public void ServedDocumentation_UsesFlowzyBranding()
    {
        using var document = JsonDocument.Parse(new LegacyContract().OpenApiJson);
        var info = document.RootElement.GetProperty("info");
        info.GetProperty("title").GetString().Should().Be("Flowzy API");
        info.GetProperty("description").GetString().Should()
            .Be("Flowzy Backend API Documentation with JWT Bearer Token Security");
        var servers = document.RootElement.GetProperty("servers");
        servers.GetArrayLength().Should().Be(1);
        servers[0].GetProperty("url").GetString().Should().Be("/");
    }

    [Fact]
    public void JavaContract_IsEmbeddedAndComplete()
    {
        var contract = new LegacyContract();
        using var document = JsonDocument.Parse(contract.OpenApiJson);
        document.RootElement.GetProperty("paths").EnumerateObject().Should().HaveCount(153);
        document.RootElement.GetProperty("components").GetProperty("schemas").EnumerateObject().Should().HaveCount(240);
    }

    [Theory]
    [InlineData("GET", "/api/dashboard/admin/overview")]
    [InlineData("PATCH", "/api/groups/{groupId}/tasks/{taskId}/move")]
    public void DocumentationOracle_ContainsExpectedOperations(string method, string path)
    {
        using var document = JsonDocument.Parse(new LegacyContract().OpenApiJson);
        document.RootElement.GetProperty("paths").GetProperty(path)
            .TryGetProperty(method.ToLowerInvariant(), out _).Should().BeTrue();
    }

    [Fact]
    public void PageResponse_MatchesSpringPaginationShape()
    {
        var response = PageResponse<int>.Create([1, 2], 1, 2, 5);
        response.Number.Should().Be(1);
        response.NumberOfElements.Should().Be(2);
        response.TotalPages.Should().Be(3);
        response.HasNext.Should().BeTrue();
        response.HasPrevious.Should().BeTrue();
    }
}
