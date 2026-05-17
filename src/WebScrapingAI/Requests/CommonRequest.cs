using System.Collections.Generic;

namespace WebScrapingAI;

/// <summary>
/// Shared options accepted by every endpoint except <c>account</c>.
/// </summary>
public abstract class CommonRequest
{
    /// <summary>Target URL to scrape. Required.</summary>
    public string Url { get; init; } = string.Empty;

    /// <summary>HTTP headers to pass to the target page.</summary>
    public IReadOnlyDictionary<string, string>? Headers { get; init; }

    /// <summary>Maximum web page retrieval time in ms (default 10000, max 30000).</summary>
    public int? Timeout { get; init; }

    /// <summary>Execute on-page JavaScript using a headless browser (default true).</summary>
    public bool? Js { get; init; }

    /// <summary>Maximum JavaScript rendering time in ms (default 2000, max 20000).</summary>
    public int? JsTimeout { get; init; }

    /// <summary>CSS selector to wait for before returning the page content. Overrides JsTimeout.</summary>
    public string? WaitFor { get; init; }

    /// <summary>Proxy type: <c>datacenter</c>, <c>residential</c>, or <c>stealth</c>.</summary>
    public string? Proxy { get; init; }

    /// <summary>Country code (us, gb, de, …). Defaults to <c>us</c>.</summary>
    public string? Country { get; init; }

    /// <summary>Your own proxy URL in <c>http://user:password@host:port</c> format.</summary>
    public string? CustomProxy { get; init; }

    /// <summary>Device emulation: <c>desktop</c>, <c>mobile</c>, or <c>tablet</c>.</summary>
    public string? Device { get; init; }

    /// <summary>Return error on 404 HTTP status on the target page (default false).</summary>
    public bool? ErrorOn404 { get; init; }

    /// <summary>Return error on redirect on the target page (default false).</summary>
    public bool? ErrorOnRedirect { get; init; }

    /// <summary>Custom JavaScript code to execute on the target page.</summary>
    public string? JsScript { get; init; }
}
