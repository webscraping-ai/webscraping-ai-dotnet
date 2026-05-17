using System.Text.Json.Serialization;

namespace WebScrapingAI;

/// <summary>Response from <c>GET /account</c>.</summary>
public sealed class AccountInfo
{
    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("remaining_api_calls")]
    public int RemainingApiCalls { get; init; }

    /// <summary>UNIX timestamp of the next billing cycle start.</summary>
    [JsonPropertyName("resets_at")]
    public long ResetsAt { get; init; }

    [JsonPropertyName("remaining_concurrency")]
    public int RemainingConcurrency { get; init; }
}
