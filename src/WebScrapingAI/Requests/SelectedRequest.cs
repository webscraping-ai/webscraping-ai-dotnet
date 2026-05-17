namespace WebScrapingAI;

/// <summary>Request for <c>GET /selected</c>.</summary>
public sealed class SelectedRequest : CommonRequest
{
    /// <summary>CSS selector. Required.</summary>
    public string Selector { get; init; } = string.Empty;

    /// <summary>Response format: <c>json</c> or <c>text</c> (default).</summary>
    public string? Format { get; init; }
}
