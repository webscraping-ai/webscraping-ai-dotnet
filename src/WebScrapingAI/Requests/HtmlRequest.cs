namespace WebScrapingAI;

/// <summary>Request for <c>GET /html</c>.</summary>
public sealed class HtmlRequest : CommonRequest
{
    /// <summary>Response format: <c>json</c> or <c>text</c> (default).</summary>
    public string? Format { get; init; }

    /// <summary>Return the result of <c>JsScript</c> execution instead of the page HTML (default false).</summary>
    public bool? ReturnScriptResult { get; init; }
}
