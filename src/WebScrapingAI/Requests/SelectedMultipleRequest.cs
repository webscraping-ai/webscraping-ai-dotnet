using System.Collections.Generic;

namespace WebScrapingAI;

/// <summary>Request for <c>GET /selected-multiple</c>.</summary>
public sealed class SelectedMultipleRequest : CommonRequest
{
    /// <summary>One or more CSS selectors. Optional — when omitted the API returns the whole-page HTML.</summary>
    public IReadOnlyList<string>? Selectors { get; init; }
}
