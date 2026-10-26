using System.Collections.Generic;

namespace WebScrapingAI;

/// <summary>
/// Request for <c>GET /data</c>: structured JSON for any public web page. Like
/// <see cref="SerpRequest"/>, it does <b>not</b> extend <see cref="CommonRequest"/>,
/// so the SDK has no typed page-scraping options (headers, timeout, device, …) for it. There are
/// no js or proxy options: <c>/data</c> renders pages with JavaScript where the site needs it and
/// always uses <c>proxy=auto</c>.
/// <para>
/// The SDK deliberately does not check which site <see cref="Url"/> points to: the server
/// works out the site and the kind of page. Some popular sites have dedicated parsers; for
/// other sites the first request for a new kind of page also learns its structure (up to about
/// 45 seconds; a 503 if it takes longer), and a page with no structured data answers 422 (see
/// <see cref="WebScrapingAIClient.DataAsync"/>).
/// </para>
/// </summary>
public sealed class DataRequest
{
    /// <summary>
    /// URL of any public web page. Required; null, empty or whitespace-only values throw
    /// <see cref="System.ArgumentException"/>. Sent as-is — never validated against a site list.
    /// </summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>Two-letter country code of the proxy used to fetch the page, <c>us</c> by default.</summary>
    public string? Country { get; init; }

    /// <summary>
    /// Video pages on sites with a dedicated parser only. Also fetch the video's transcript into <c>data.transcript</c>. It's null when
    /// no matching captions are available. If the transcript fetch itself fails, the whole request fails
    /// with a 500 and is not charged.
    /// </summary>
    public bool? Transcript { get; init; }

    /// <summary>
    /// With <c>Transcript = true</c>. Caption language to pick, e.g. <c>en</c> or <c>de</c>. Without it, English is preferred, then the
    /// first available track. If the video has no captions in that language, <c>data.transcript</c> is null.
    /// </summary>
    public string? TranscriptLanguage { get; init; }

    /// <summary>
    /// Extra query parameters sent as-is, for options the SDK doesn't model yet.
    /// Keys and values are encoded like every other parameter. Entries with a null or empty value
    /// (or an empty key) are dropped, not sent. Keys that belong to a dedicated option throw
    /// <see cref="System.ArgumentException"/> (case-insensitive), whether or not that option is set:
    /// <c>api_key</c>, <c>url</c>, <c>country</c>, <c>transcript</c> and <c>transcript_language</c>.
    /// </summary>
    public IDictionary<string, string>? ExtraParams { get; init; }
}
