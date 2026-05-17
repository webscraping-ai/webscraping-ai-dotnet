using System;
using System.Net.Http;

namespace WebScrapingAI;

/// <summary>
/// Configuration for <see cref="WebScrapingAIClient"/>. Use the object
/// initializer syntax:
/// <code>
/// var options = new WebScrapingAIClientOptions { ApiKey = "..." };
/// </code>
/// </summary>
public sealed class WebScrapingAIClientOptions
{
    /// <summary>
    /// WebScraping.AI API key. If null, the client reads the
    /// <c>WEBSCRAPING_AI_API_KEY</c> environment variable. The constructor
    /// throws if neither is set.
    /// </summary>
    public string? ApiKey { get; init; }

    /// <summary>Base URL of the API. Defaults to <c>https://api.webscraping.ai</c>.</summary>
    public string BaseUrl { get; init; } = "https://api.webscraping.ai";

    /// <summary>Per-request timeout. Defaults to 60 seconds.</summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(60);

    /// <summary>User-Agent header sent with every request.</summary>
    public string UserAgent { get; init; } = "webscraping-ai-dotnet/" + SdkVersion.Value;

    /// <summary>
    /// Optional custom <see cref="HttpMessageHandler"/>. Useful for tests or
    /// for plugging in a custom transport (Polly, distributed tracing, etc.).
    /// Ownership: the client disposes the handler iff it was created internally;
    /// caller-supplied handlers are left alone.
    /// </summary>
    public HttpMessageHandler? HttpHandler { get; init; }
}
