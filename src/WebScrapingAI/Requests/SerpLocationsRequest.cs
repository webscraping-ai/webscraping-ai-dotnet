namespace WebScrapingAI;

/// <summary>
/// Request for <c>GET /serp/locations</c>: looks up names for <see cref="SerpRequest.Location"/>.
/// Free on the server and needs no API key.
/// </summary>
public sealed class SerpLocationsRequest
{
    /// <summary>
    /// Part of a location name, e.g. <c>austin</c>. Matches English names. Without it, the most
    /// populous locations are returned.
    /// </summary>
    public string? Q { get; init; }

    /// <summary>
    /// Maximum number of locations to return (1–10, default 10). Values below 1 throw
    /// <see cref="System.ArgumentOutOfRangeException"/>; the server rejects values above 10 with a 400.
    /// </summary>
    public int? Limit { get; init; }
}
