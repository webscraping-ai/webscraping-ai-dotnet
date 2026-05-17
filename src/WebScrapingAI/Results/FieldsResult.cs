using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WebScrapingAI;

/// <summary>
/// Response from <c>GET /ai/fields</c>. The API wraps the extracted fields
/// under a <c>result</c> key — this drift is preserved in the typed shape so
/// the wire layout stays observable, matching the Go/Java/Python SDKs.
/// </summary>
public sealed class FieldsResult
{
    [JsonPropertyName("result")]
    public IReadOnlyDictionary<string, string?>? Result { get; init; }
}
