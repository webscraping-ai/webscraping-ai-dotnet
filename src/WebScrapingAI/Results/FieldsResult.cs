using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WebScrapingAI;

/// <summary>
/// Response from <c>GET /ai/fields</c>. The API wraps the extracted fields
/// under a <c>result</c> key; every value is a string or null. Matches the
/// Go/Java/Python SDKs.
/// </summary>
public sealed class FieldsResult
{
    [JsonPropertyName("result")]
    public IReadOnlyDictionary<string, string?>? Result { get; init; }
}
