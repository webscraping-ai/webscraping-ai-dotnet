namespace WebScrapingAI;

/// <summary>Request for <c>GET /ai/question</c>.</summary>
public sealed class QuestionRequest : CommonRequest
{
    /// <summary>Question or instruction to ask the LLM about the target page. Required.</summary>
    public string Question { get; init; } = string.Empty;

    /// <summary>Response format: <c>json</c> or <c>text</c> (default).</summary>
    public string? Format { get; init; }
}
