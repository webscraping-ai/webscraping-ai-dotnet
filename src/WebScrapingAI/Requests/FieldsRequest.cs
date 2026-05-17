using System.Collections.Generic;

namespace WebScrapingAI;

/// <summary>Request for <c>GET /ai/fields</c>.</summary>
public sealed class FieldsRequest : CommonRequest
{
    /// <summary>
    /// Field name → description map. Must contain at least one entry.
    /// Sent as a deepObject query: <c>fields[title]=Main+product+title</c>.
    /// </summary>
    public IReadOnlyDictionary<string, string> Fields { get; init; } = new Dictionary<string, string>();
}
