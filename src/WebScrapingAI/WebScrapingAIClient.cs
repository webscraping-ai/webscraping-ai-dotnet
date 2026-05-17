using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using WebScrapingAI.Internal;

namespace WebScrapingAI;

/// <summary>
/// Official .NET client for the WebScraping.AI API.
/// <para>
/// All methods are async. Non-2xx HTTP responses throw a typed
/// <see cref="ApiException"/> subclass; transport failures throw
/// <see cref="ApiTimeoutException"/> or <see cref="ApiConnectionException"/>.
/// </para>
/// <example>
/// <code>
/// var client = new WebScrapingAIClient(new WebScrapingAIClientOptions {
///     ApiKey = "YOUR_KEY",
/// });
/// string html = await client.HtmlAsync(new HtmlRequest {
///     Url = "https://example.com",
///     Js = true,
/// });
/// </code>
/// </example>
/// </summary>
public sealed class WebScrapingAIClient : IDisposable
{
    private readonly WebScrapingAIClientOptions _options;
    private readonly HttpClient _http;
    private readonly bool _ownsHandler;
    private readonly HttpMessageHandler? _handler;
    private bool _disposed;

    /// <summary>
    /// Builds a client. Reads <c>WEBSCRAPING_AI_API_KEY</c> from the
    /// environment when <see cref="WebScrapingAIClientOptions.ApiKey"/> is
    /// null.
    /// </summary>
    /// <exception cref="ArgumentNullException">When <paramref name="options"/> is null.</exception>
    /// <exception cref="InvalidOperationException">When no API key is configured.</exception>
    public WebScrapingAIClient(WebScrapingAIClientOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));

        var apiKey = options.ApiKey ?? Environment.GetEnvironmentVariable("WEBSCRAPING_AI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException(
                "API key is required. Pass it via WebScrapingAIClientOptions.ApiKey or set WEBSCRAPING_AI_API_KEY in the environment.");
        }
        ResolvedApiKey = apiKey!;

        _handler = options.HttpHandler;
        _ownsHandler = _handler is null;
        _http = _handler is null ? new HttpClient() : new HttpClient(_handler, disposeHandler: false);
        // HttpClient.Timeout governs the overall request. The infinite
        // timeout below means the per-request CancellationToken we plumb
        // through is the single source of truth — keeps timeout-vs-cancel
        // behavior predictable.
        _http.Timeout = System.Threading.Timeout.InfiniteTimeSpan;
    }

    /// <summary>Constructs a client reading the API key from <c>WEBSCRAPING_AI_API_KEY</c>.</summary>
    public WebScrapingAIClient() : this(new WebScrapingAIClientOptions()) { }

    internal string ResolvedApiKey { get; }

    public WebScrapingAIClientOptions Options => _options;

    // ---------- /html ----------
    public Task<string> HtmlAsync(HtmlRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        Require(request.Url, nameof(request.Url));
        var q = CommonParams(request)
            .Set("url", request.Url);
        if (!string.IsNullOrEmpty(request.Format)) q.Set("format", request.Format);
        if (request.ReturnScriptResult.HasValue) q.Set("return_script_result", request.ReturnScriptResult.Value);
        return RequestStringAsync("/html", q, cancellationToken);
    }

    // ---------- /text ----------
    public Task<string> TextAsync(TextRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        Require(request.Url, nameof(request.Url));
        var q = CommonParams(request)
            .Set("url", request.Url);
        if (!string.IsNullOrEmpty(request.TextFormat)) q.Set("text_format", request.TextFormat);
        if (request.ReturnLinks.HasValue) q.Set("return_links", request.ReturnLinks.Value);
        return RequestStringAsync("/text", q, cancellationToken);
    }

    // ---------- /selected ----------
    public Task<string> SelectedAsync(SelectedRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        Require(request.Url, nameof(request.Url));
        Require(request.Selector, nameof(request.Selector));
        var q = CommonParams(request)
            .Set("url", request.Url)
            .Set("selector", request.Selector);
        if (!string.IsNullOrEmpty(request.Format)) q.Set("format", request.Format);
        return RequestStringAsync("/selected", q, cancellationToken);
    }

    // ---------- /selected-multiple ----------
    public async Task<SelectedMultipleResult> SelectedMultipleAsync(SelectedMultipleRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        Require(request.Url, nameof(request.Url));
        if (request.Selectors is null || request.Selectors.Count == 0)
        {
            throw new ArgumentException($"{nameof(request.Selectors)} must contain at least one selector", nameof(request));
        }
        var q = CommonParams(request)
            .Set("url", request.Url)
            .Set("selectors", request.Selectors);

        var body = await RequestStringAsync("/selected-multiple", q, cancellationToken).ConfigureAwait(false);
        var parsed = Json.Read<List<List<string>>>(body);
        var wrapped = new List<IReadOnlyList<string>>(parsed.Count);
        foreach (var inner in parsed) wrapped.Add(inner);
        return new SelectedMultipleResult(wrapped);
    }

    // ---------- /ai/question ----------
    public async Task<string> QuestionAsync(QuestionRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        Require(request.Url, nameof(request.Url));
        Require(request.Question, nameof(request.Question));
        var q = CommonParams(request)
            .Set("url", request.Url)
            .Set("question", request.Question);
        if (!string.IsNullOrEmpty(request.Format)) q.Set("format", request.Format);

        var body = await RequestStringAsync("/ai/question", q, cancellationToken).ConfigureAwait(false);
        return UnwrapJsonString(body);
    }

    // ---------- /ai/fields ----------
    public async Task<FieldsResult> FieldsAsync(FieldsRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));
        Require(request.Url, nameof(request.Url));
        if (request.Fields is null || request.Fields.Count == 0)
        {
            throw new ArgumentException($"{nameof(request.Fields)} must contain at least one field", nameof(request));
        }
        var q = CommonParams(request)
            .Set("url", request.Url)
            .Set("fields", request.Fields);

        var body = await RequestStringAsync("/ai/fields", q, cancellationToken).ConfigureAwait(false);
        return Json.Read<FieldsResult>(body);
    }

    // ---------- /account ----------
    public async Task<AccountInfo> AccountAsync(CancellationToken cancellationToken = default)
    {
        var body = await RequestStringAsync("/account", new QueryEncoder(), cancellationToken).ConfigureAwait(false);
        return Json.Read<AccountInfo>(body);
    }

    // ---------- internals ----------

    private async Task<string> RequestStringAsync(string path, QueryEncoder query, CancellationToken cancellationToken)
    {
        // api_key is prepended by concatenating encoded strings rather than
        // re-setting via QueryEncoder.Set — Set is single-value replace, and
        // would collapse the repeated selectors=... pairs that
        // SelectedMultiple emits. (Same bug we hit in Java; preserved as a
        // test signature.)
        var apiKeySegment = new QueryEncoder().Set("api_key", ResolvedApiKey).Encode();
        var rest = query.Encode();
        var fullQuery = string.IsNullOrEmpty(rest) ? apiKeySegment : apiKeySegment + "&" + rest;

        var url = _options.BaseUrl + path + "?" + fullQuery;

        using var message = new HttpRequestMessage(HttpMethod.Get, url);
        message.Headers.UserAgent.ParseAdd(_options.UserAgent);
        message.Headers.Accept.ParseAdd("application/json, text/html, */*");

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (_options.Timeout > TimeSpan.Zero) timeoutCts.CancelAfter(_options.Timeout);

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(message, HttpCompletionOption.ResponseContentRead, timeoutCts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ApiTimeoutException($"Request timed out after {_options.Timeout.TotalSeconds:0.###}s", ex);
        }
        catch (HttpRequestException ex)
        {
            throw new ApiConnectionException("Connection error: " + ex.Message, ex);
        }

        using (response)
        {
#if NET8_0_OR_GREATER
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
#else
            var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
#endif

            var status = (int)response.StatusCode;
            if (status >= 200 && status < 300) return body;

            throw ParseApiException(status, body);
        }
    }

    private static ApiException ParseApiException(int status, string body)
    {
        var env = Json.TryRead<ErrorEnvelope>(body);
        string? message = env?.Message;
        if (string.IsNullOrEmpty(message))
        {
            message = string.IsNullOrEmpty(body) ? null : body;
        }
        return ApiException.ForStatus(status, message ?? string.Empty, env?.StatusCode, env?.StatusMessage, env?.Body, body);
    }

    private static QueryEncoder CommonParams(CommonRequest c)
    {
        var q = new QueryEncoder();
        if (c.Headers is { Count: > 0 }) q.Set("headers", c.Headers);
        if (c.Timeout.HasValue) q.Set("timeout", c.Timeout.Value);
        if (c.Js.HasValue) q.Set("js", c.Js.Value);
        if (c.JsTimeout.HasValue) q.Set("js_timeout", c.JsTimeout.Value);
        if (!string.IsNullOrEmpty(c.WaitFor)) q.Set("wait_for", c.WaitFor);
        if (!string.IsNullOrEmpty(c.Proxy)) q.Set("proxy", c.Proxy);
        if (!string.IsNullOrEmpty(c.Country)) q.Set("country", c.Country);
        if (!string.IsNullOrEmpty(c.CustomProxy)) q.Set("custom_proxy", c.CustomProxy);
        if (!string.IsNullOrEmpty(c.Device)) q.Set("device", c.Device);
        if (c.ErrorOn404.HasValue) q.Set("error_on_404", c.ErrorOn404.Value);
        if (c.ErrorOnRedirect.HasValue) q.Set("error_on_redirect", c.ErrorOnRedirect.Value);
        if (!string.IsNullOrEmpty(c.JsScript)) q.Set("js_script", c.JsScript);
        return q;
    }

    private static void Require(string value, string name)
    {
        if (string.IsNullOrEmpty(value)) throw new ArgumentException($"{name} is required", name);
    }

    private static string UnwrapJsonString(string body)
    {
        if (string.IsNullOrEmpty(body) || body.Length < 2) return body ?? string.Empty;
        if (body[0] != '"' || body[body.Length - 1] != '"') return body;
        var unwrapped = Json.TryRead<string>(body);
        return unwrapped ?? body;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _http.Dispose();
        if (_ownsHandler && _handler is not null) _handler.Dispose();
    }
}
