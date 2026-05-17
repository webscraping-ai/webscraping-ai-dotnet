using System.Text.Json;
using System.Text.Json.Serialization;

namespace WebScrapingAI.Internal;

internal static class Json
{
    internal static readonly JsonSerializerOptions Options = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Best-effort JSON deserialize. Returns <c>default</c> if the body is
    /// null, empty, or not valid JSON for <typeparamref name="T"/>. Used in
    /// error-envelope parsing where the body may not be JSON at all.
    /// </summary>
    internal static T? TryRead<T>(string? body)
    {
        if (string.IsNullOrEmpty(body)) return default;
        try
        {
            return JsonSerializer.Deserialize<T>(body!, Options);
        }
        catch (JsonException)
        {
            return default;
        }
    }

    internal static T Read<T>(string body)
    {
        var result = JsonSerializer.Deserialize<T>(body, Options);
        return result ?? throw new JsonException("Deserialization returned null");
    }
}

/// <summary>Internal DTO for the API's error envelope.</summary>
internal sealed class ErrorEnvelope
{
    [JsonPropertyName("message")] public string? Message { get; init; }
    [JsonPropertyName("status_code")] public int? StatusCode { get; init; }
    [JsonPropertyName("status_message")] public string? StatusMessage { get; init; }
    [JsonPropertyName("body")] public string? Body { get; init; }
}
