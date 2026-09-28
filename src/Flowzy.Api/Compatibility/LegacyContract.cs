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
        // Resolve against the document's origin on localhost and behind the production proxy.
        root["servers"] = new JsonArray(new JsonObject
        {
            ["url"] = "/",
            ["description"] = "Flowzy API"
        });
        json = JsonSerializer.SerializeToUtf8Bytes(root);
    }

    public ReadOnlyMemory<byte> OpenApiJson => json;
}
