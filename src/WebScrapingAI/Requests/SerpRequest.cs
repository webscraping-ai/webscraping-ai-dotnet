namespace WebScrapingAI;

/// <summary>
/// Request for <c>GET /serp</c>. Query-shaped, not URL-shaped, so it does
/// <b>not</b> extend <see cref="CommonRequest"/> — none of the page-scraping
/// options (js, proxy, country, headers, timeout, …) apply.
/// </summary>
public sealed class SerpRequest
{
    /// <summary>Search query. Required; null, empty or whitespace-only values throw <see cref="System.ArgumentException"/>. Sent as-is (not trimmed).</summary>
    public string Q { get; init; } = string.Empty;

    /// <summary>Search engine to query. Currently only <c>google</c> (the default).</summary>
    public string? Engine { get; init; }

    /// <summary>Two-letter country code for geolocation of the search (default <c>us</c>).</summary>
    public string? Gl { get; init; }

    /// <summary>Two-letter language code for the results (default <c>en</c>).</summary>
    public string? Hl { get; init; }

    /// <summary>
    /// Results page number, starting at 1 (default 1, 10 results per page). Values below 1 throw
    /// <see cref="System.ArgumentOutOfRangeException"/>. The server caps the page at 100.
    /// </summary>
    public int? Page { get; init; }
}
