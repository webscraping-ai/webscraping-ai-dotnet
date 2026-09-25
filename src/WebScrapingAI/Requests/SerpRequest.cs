namespace WebScrapingAI;

/// <summary>
/// Request for <c>GET /serp</c>. Query-shaped, not URL-shaped, so it does
/// <b>not</b> extend <see cref="CommonRequest"/> — none of the page-scraping
/// options (js, proxy, country, headers, timeout, …) apply.
/// </summary>
public sealed class SerpRequest
{
    /// <summary>Search query. Required.</summary>
    public string Q { get; init; } = string.Empty;

    /// <summary>Search engine to query. Currently only <c>google</c> (the default).</summary>
    public string? Engine { get; init; }

    /// <summary>Two-letter country code for geolocation of the search (default <c>us</c>).</summary>
    public string? Gl { get; init; }

    /// <summary>Two-letter language code for the results (default <c>en</c>).</summary>
    public string? Hl { get; init; }

    /// <summary>Results page number, starting at 1 (default 1, 10 results per page).</summary>
    public int? Page { get; init; }
}
