namespace WebScrapingAI;

/// <summary>Request for <c>GET /text</c>.</summary>
public sealed class TextRequest : CommonRequest
{
    /// <summary>Format of the text response: <c>plain</c> (default), <c>xml</c>, or <c>json</c>.</summary>
    public string? TextFormat { get; init; }

    /// <summary>Return links from the page body text (only with <c>TextFormat=json</c>).</summary>
    public bool? ReturnLinks { get; init; }
}
