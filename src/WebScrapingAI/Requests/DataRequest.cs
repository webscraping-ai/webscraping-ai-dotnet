using System.Collections.Generic;

namespace WebScrapingAI;

/// <summary>
/// Request for <c>GET /data</c>: structured JSON for a page on a supported site
/// (e.g. YouTube, TikTok, X, LinkedIn, Instagram, Reddit). Like
/// <see cref="SerpRequest"/>, it does <b>not</b> extend <see cref="CommonRequest"/>
/// — the page-scraping options (js, proxy, headers, timeout, device, …) don't apply.
/// <para>
/// The SDK deliberately does not check which site <see cref="Url"/> points to:
/// supported sites and page types are added on the server. An unsupported URL or
/// page type returns a 400 (<see cref="BadRequestException"/>) that is not charged.
/// Its message lists what is supported.
/// </para>
/// </summary>
public sealed class DataRequest
{
    /// <summary>
    /// The page's normal URL. Required; null, empty or whitespace-only values throw
    /// <see cref="System.ArgumentException"/>. Sent as-is — never validated against a site list.
    /// </summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>Two-letter country code of the proxy used to fetch the page, <c>us</c> by default.</summary>
    public string? Country { get; init; }

    /// <summary>
    /// YouTube videos only. Also fetch the video's transcript into <c>data.transcript</c>. It's null when
    /// no matching captions are available. If the transcript fetch itself fails, the whole request fails
    /// with a 500 and is not charged.
    /// </summary>
    public bool? Transcript { get; init; }

    /// <summary>
    /// Caption language to pick, e.g. <c>en</c> or <c>de</c>. Without it, English is preferred, then the
    /// first available track. If the video has no captions in that language, <c>data.transcript</c> is null.
    /// </summary>
    public string? TranscriptLanguage { get; init; }

    /// <summary>
    /// Extra query parameters sent as-is, for provider-specific options the SDK doesn't model yet.
    /// Keys and values are encoded like every other parameter. Entries with a null or empty value
    /// (or an empty key) are dropped, not sent. Keys that belong to a dedicated option throw
    /// <see cref="System.ArgumentException"/> (case-insensitive), whether or not that option is set:
    /// <c>api_key</c>, <c>url</c>, <c>country</c>, <c>transcript</c> and <c>transcript_language</c>.
    /// </summary>
    public IDictionary<string, string>? ExtraParams { get; init; }
}
