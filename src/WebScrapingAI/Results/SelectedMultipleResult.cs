using System.Collections.Generic;

namespace WebScrapingAI;

/// <summary>
/// Response from <c>GET /selected-multiple</c>. The API returns an outer
/// wrapper array containing per-page matches concatenated — preserved here
/// so the shape stays observable (same drift as Go/Java/Python).
/// </summary>
public sealed class SelectedMultipleResult
{
    public IReadOnlyList<IReadOnlyList<string>> Results { get; }

    public SelectedMultipleResult(IReadOnlyList<IReadOnlyList<string>> results)
    {
        Results = results ?? System.Array.Empty<IReadOnlyList<string>>();
    }
}
