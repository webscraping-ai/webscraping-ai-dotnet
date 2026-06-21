namespace WebScrapingAI;

/// <summary>Request for <c>GET /selected</c>.</summary>
public sealed class SelectedRequest : CommonRequest
{
    /// <summary>CSS selector. Optional — when omitted the API returns the whole-page HTML.</summary>
    public string? Selector { get; init; }

    /// <summary>Response format: <c>json</c> or <c>text</c> (default).</summary>
    public string? Format { get; init; }
}
