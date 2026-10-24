using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WebScrapingAI;

/// <summary>
/// One entry of the <c>GET /serp/locations</c> response. Pass <see cref="CanonicalName"/>
/// (or <see cref="Id"/>) as <see cref="SerpRequest.Location"/>.
/// </summary>
public sealed class SerpLocation
{
    /// <summary>Location id (the Google geo target id, as a string), usable as <see cref="SerpRequest.Location"/>.</summary>
    [JsonPropertyName("id")]
    public string Id { get; init; } = string.Empty;

    [JsonPropertyName("google_id")]
    public long GoogleId { get; init; }

    /// <summary>The parent location's Google id. Null when the API omits it, e.g. for countries.</summary>
    [JsonPropertyName("google_parent_id")]
    public long? GoogleParentId { get; init; }

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    /// <summary>Full name to pass as <see cref="SerpRequest.Location"/>, e.g. <c>Austin,Texas,United States</c>.</summary>
    [JsonPropertyName("canonical_name")]
    public string CanonicalName { get; init; } = string.Empty;

    [JsonPropertyName("country_code")]
    public string CountryCode { get; init; } = string.Empty;

    /// <summary>
    /// Google's target type, e.g. <c>City</c>, <c>District</c>, <c>State</c>, <c>Region</c> or
    /// <c>Country</c>. A plain string, so new values still come through.
    /// </summary>
    [JsonPropertyName("target_type")]
    public string TargetType { get; init; } = string.Empty;

    /// <summary>Approximate population, used for ordering (most populous first).</summary>
    [JsonPropertyName("reach")]
    public long Reach { get; init; }

    /// <summary>Coordinates the search is sent from, as <c>[longitude, latitude]</c>.</summary>
    [JsonPropertyName("gps")]
    public IReadOnlyList<double> Gps { get; init; } = System.Array.Empty<double>();
}
