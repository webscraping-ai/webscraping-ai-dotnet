using System;

namespace WebScrapingAI;

/// <summary>
/// Base exception for every error produced by the WebScraping.AI SDK.
/// Catch this to handle any SDK failure uniformly.
/// </summary>
public class WebScrapingAIException : Exception
{
    public WebScrapingAIException() { }
    public WebScrapingAIException(string message) : base(message) { }
    public WebScrapingAIException(string message, Exception innerException) : base(message, innerException) { }
}
