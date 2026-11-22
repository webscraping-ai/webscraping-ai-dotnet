namespace WebScrapingAI;

/// <summary>Request for <c>GET /selected</c>.</summary>
public sealed class SelectedRequest : CommonRequest
{
    /// <summary>
    /// CSS selector. Not checked client-side (omitted from the query when empty), but the API answers a
    /// missing selector, or one that matches nothing, with a 400 "Element not found". It never returns the
    /// whole page.
    /// </summary>
    public string? Selector { get; init; }

    /// <summary>Response format: <c>json</c> or <c>text</c> (default).</summary>
    public string? Format { get; init; }
}
