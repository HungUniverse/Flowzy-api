using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Flowzy.Api.Compatibility;

// Documentation oracle only. Never synthesize successful API responses from this schema.
public sealed class LegacyContract
{
    private readonly byte[] json;

    public LegacyContract()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("Flowzy.Api.Contracts.fspark-openapi.json")
            ?? throw new InvalidOperationException("Embedded F-Spark OpenAPI contract was not found");
        var root = JsonNode.Parse(stream)!.AsObject();
        // Rebrand the served documentation without modifying the Java contract oracle.
        var info = root["info"]!.AsObject();
        info["title"] = "Flowzy API";
        info["description"] = "Flowzy Backend API Documentation with JWT Bearer Token Security";
        if (root["servers"] is JsonArray servers && servers.FirstOrDefault() is JsonObject server)
        {
            server["url"] = "http://localhost:8080";
            server["description"] = "Local Flowzy API";
        }
        json = JsonSerializer.SerializeToUtf8Bytes(root);
    }

    public ReadOnlyMemory<byte> OpenApiJson => json;
}
