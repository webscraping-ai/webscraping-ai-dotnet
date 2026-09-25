using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebScrapingAI;

/// <summary>
/// Response from <c>GET /data</c>. <see cref="Data"/> is untyped JSON because its
/// shape depends on <see cref="DataRequestParameters.Provider"/> and
/// <see cref="DataRequestParameters.Type"/>, and new providers are added on the
/// server. <c>Provider</c>, <c>Type</c> and <see cref="ParseStatus"/> are plain
/// strings so values this SDK version doesn't know about still round-trip.
/// </summary>
public sealed class DataResult
{
    /// <summary>The URL and the detected provider and page type.</summary>
    /// <remarks>Never null: an absent or JSON-<c>null</c> block reads as empty strings.</remarks>
    [JsonPropertyName("request_parameters")]
    public DataRequestParameters RequestParameters
    {
        get => _requestParameters;
        init => _requestParameters = value ?? new DataRequestParameters();
    }

    private readonly DataRequestParameters _requestParameters = new();

    /// <summary>
    /// <c>ok</c>, <c>parse_failed</c> or <c>not_found</c> today (all three are billed
    /// successes). Kept as a string so future values don't break deserialization.
    /// </summary>
    [JsonPropertyName("parse_status")]
    public string ParseStatus
    {
        get => _parseStatus;
        init => _parseStatus = value ?? string.Empty;
    }

    private readonly string _parseStatus = string.Empty;

    /// <summary>
    /// The structured page data (snake_case fields), or null when the API returned
    /// <c>null</c> (e.g. <c>parse_failed</c> / <c>not_found</c>).
    /// </summary>
    [JsonPropertyName("data")]
    public JsonElement? Data { get; init; }
}

/// <summary>The <c>request_parameters</c> block of <see cref="DataResult"/>.</summary>
public sealed class DataRequestParameters
{
    [JsonPropertyName("url")]
    public string Url { get; init; } = string.Empty;

    /// <summary>Detected site, e.g. <c>youtube</c>. Plain string; unknown values round-trip.</summary>
    [JsonPropertyName("provider")]
    public string Provider { get; init; } = string.Empty;

    /// <summary>Detected page kind, e.g. <c>video</c>. Plain string; unknown values round-trip.</summary>
    [JsonPropertyName("type")]
    public string Type { get; init; } = string.Empty;
}
