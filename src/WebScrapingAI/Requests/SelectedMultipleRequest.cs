using System.Collections.Generic;

namespace WebScrapingAI;

/// <summary>Request for <c>GET /selected-multiple</c>.</summary>
public sealed class SelectedMultipleRequest : CommonRequest
{
    /// <summary>One or more CSS selectors. At least one is required.</summary>
    public IReadOnlyList<string> Selectors { get; init; } = System.Array.Empty<string>();
}
