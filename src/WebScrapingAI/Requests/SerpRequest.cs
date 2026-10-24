namespace WebScrapingAI;

/// <summary>
/// Request for <c>GET /serp</c>. Query-shaped, not URL-shaped, so it does
/// <b>not</b> extend <see cref="CommonRequest"/> — none of the page-scraping
/// options (js, proxy, country, headers, timeout, …) apply.
/// <para>
/// Every search runs from one fixed location: <see cref="Location"/>, <see cref="Lat"/> +
/// <see cref="Lon"/>, or <see cref="Uule"/>, otherwise the capital of the <see cref="Gl"/>
/// country, so repeats and later pages stay consistent. Which of these can be combined, and
/// their ranges, are checked by the server (a 400, not billed).
/// </para>
/// </summary>
public sealed class SerpRequest
{
    /// <summary>Search query. Required; null, empty or whitespace-only values throw <see cref="System.ArgumentException"/>. Sent as-is (not trimmed).</summary>
    public string Q { get; init; } = string.Empty;

    /// <summary>Search engine to query. Currently only <c>google</c> (the default).</summary>
    public string? Engine { get; init; }

    /// <summary>
    /// Two-letter country code for geolocation of the search. Defaults to the country of
    /// <see cref="Location"/>, <see cref="Lat"/>/<see cref="Lon"/> or <see cref="Uule"/>, otherwise <c>us</c>.
    /// </summary>
    public string? Gl { get; init; }

    /// <summary>Two-letter language code for the results (default <c>en</c>).</summary>
    public string? Hl { get; init; }

    /// <summary>
    /// Results page number, starting at 1 (default 1, 10 results per page). Values below 1 throw
    /// <see cref="System.ArgumentOutOfRangeException"/>. The server rejects a page above 100 with a 400 (not billed).
    /// </summary>
    public int? Page { get; init; }

    /// <summary>
    /// Number of consecutive results pages to fetch in one call, starting at <see cref="Page"/>
    /// (1–5, default 1). Duplicates across pages are removed, <see cref="SerpOrganicResult.Position"/>
    /// counts through the combined list and each result gets a <see cref="SerpOrganicResult.Page"/>.
    /// Billed per results page returned. Values below 1 throw <see cref="System.ArgumentOutOfRangeException"/>;
    /// the server rejects values above 5, or <c>Page + Pages - 1</c> above 100, with a 400 (not billed).
    /// </summary>
    public int? Pages { get; init; }

    /// <summary>
    /// Where the search runs from: a location name such as <c>Austin,Texas,United States</c> or
    /// <c>Austin, TX</c>, or an id from <see cref="WebScrapingAIClient.SerpLocationsAsync"/>. Large cities
    /// also match local names, e.g. <c>Москва</c>. Sent as-is. An unknown name returns a 400 (not billed).
    /// Cannot be combined with <see cref="Uule"/>, <see cref="Lat"/> or <see cref="Lon"/>.
    /// </summary>
    public string? Location { get; init; }

    /// <summary>
    /// Google-encoded location (<c>w+CAIQICI…</c> or <c>a+…</c>). Cannot be combined with
    /// <see cref="Location"/>, <see cref="Lat"/> or <see cref="Lon"/>.
    /// </summary>
    public string? Uule { get; init; }

    /// <summary>
    /// Latitude of the search origin in decimal degrees (-90 to 90). Requires <see cref="Lon"/>.
    /// Sent as a plain decimal with at most 7 fractional digits. NaN or infinity throws
    /// <see cref="System.ArgumentOutOfRangeException"/>.
    /// </summary>
    public double? Lat { get; init; }

    /// <summary>
    /// Longitude of the search origin in decimal degrees (-180 to 180). Requires <see cref="Lat"/>.
    /// Sent as a plain decimal with at most 7 fractional digits. NaN or infinity throws
    /// <see cref="System.ArgumentOutOfRangeException"/>.
    /// </summary>
    public double? Lon { get; init; }

    /// <summary>
    /// Radius in meters around <see cref="Location"/> or <see cref="Lat"/>/<see cref="Lon"/> (server range 1–1000).
    /// Values below 1 throw <see cref="System.ArgumentOutOfRangeException"/>.
    /// </summary>
    public int? Radius { get; init; }
}
