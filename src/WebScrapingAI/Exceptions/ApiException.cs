using System;

namespace WebScrapingAI;

/// <summary>
/// Thrown when the API returns a non-2xx HTTP response.
/// The transport-level exceptions (<see cref="ApiTimeoutException"/>,
/// <see cref="ApiConnectionException"/>) do <b>not</b> extend this type,
/// so <c>catch (ApiException)</c> reliably narrows to "got an HTTP response back".
/// </summary>
public class ApiException : WebScrapingAIException
{
    /// <summary>HTTP status returned by the API.</summary>
    public int Status { get; }

    /// <summary>API-side status code from the error envelope, when present (e.g. target page HTTP status on a 500).</summary>
    public int? ApiStatusCode { get; }

    /// <summary>API-side status message from the error envelope, when present.</summary>
    public string? ApiStatusMessage { get; }

    /// <summary>Target-page response body forwarded by the API on a 500, when present.</summary>
    public string? ApiBody { get; }

    /// <summary>Raw response body returned by the API.</summary>
    public string? ResponseBody { get; }

    public ApiException(
        int status,
        string message,
        int? apiStatusCode = null,
        string? apiStatusMessage = null,
        string? apiBody = null,
        string? responseBody = null)
        : base(message)
    {
        Status = status;
        ApiStatusCode = apiStatusCode;
        ApiStatusMessage = apiStatusMessage;
        ApiBody = apiBody;
        ResponseBody = responseBody;
    }

    /// <summary>
    /// Factory that picks the right subclass for a given HTTP status, falling
    /// back to <see cref="ApiException"/> for un-mapped statuses.
    /// </summary>
    public static ApiException ForStatus(
        int status,
        string? message,
        int? apiStatusCode = null,
        string? apiStatusMessage = null,
        string? apiBody = null,
        string? responseBody = null)
    {
        var msg = string.IsNullOrEmpty(message) ? $"HTTP {status}" : $"HTTP {status}: {message}";
        return status switch
        {
            400 => new BadRequestException(msg, apiStatusCode, apiStatusMessage, apiBody, responseBody),
            402 => new PaymentRequiredException(msg, apiStatusCode, apiStatusMessage, apiBody, responseBody),
            403 => new AuthenticationException(msg, apiStatusCode, apiStatusMessage, apiBody, responseBody),
            429 => new RateLimitException(msg, apiStatusCode, apiStatusMessage, apiBody, responseBody),
            500 => new ServerException(msg, apiStatusCode, apiStatusMessage, apiBody, responseBody),
            504 => new GatewayTimeoutException(msg, apiStatusCode, apiStatusMessage, apiBody, responseBody),
            _ => new ApiException(status, msg, apiStatusCode, apiStatusMessage, apiBody, responseBody),
        };
    }
}

public sealed class BadRequestException : ApiException
{
    public BadRequestException(string message, int? apiStatusCode = null, string? apiStatusMessage = null, string? apiBody = null, string? responseBody = null)
        : base(400, message, apiStatusCode, apiStatusMessage, apiBody, responseBody) { }
}

public sealed class PaymentRequiredException : ApiException
{
    public PaymentRequiredException(string message, int? apiStatusCode = null, string? apiStatusMessage = null, string? apiBody = null, string? responseBody = null)
        : base(402, message, apiStatusCode, apiStatusMessage, apiBody, responseBody) { }
}

public sealed class AuthenticationException : ApiException
{
    public AuthenticationException(string message, int? apiStatusCode = null, string? apiStatusMessage = null, string? apiBody = null, string? responseBody = null)
        : base(403, message, apiStatusCode, apiStatusMessage, apiBody, responseBody) { }
}

public sealed class RateLimitException : ApiException
{
    public RateLimitException(string message, int? apiStatusCode = null, string? apiStatusMessage = null, string? apiBody = null, string? responseBody = null)
        : base(429, message, apiStatusCode, apiStatusMessage, apiBody, responseBody) { }
}

public sealed class ServerException : ApiException
{
    public ServerException(string message, int? apiStatusCode = null, string? apiStatusMessage = null, string? apiBody = null, string? responseBody = null)
        : base(500, message, apiStatusCode, apiStatusMessage, apiBody, responseBody) { }
}

public sealed class GatewayTimeoutException : ApiException
{
    public GatewayTimeoutException(string message, int? apiStatusCode = null, string? apiStatusMessage = null, string? apiBody = null, string? responseBody = null)
        : base(504, message, apiStatusCode, apiStatusMessage, apiBody, responseBody) { }
}

/// <summary>Transport-level timeout (request didn't complete before the deadline).</summary>
public sealed class ApiTimeoutException : WebScrapingAIException
{
    public ApiTimeoutException(string message) : base(message) { }
    public ApiTimeoutException(string message, Exception innerException) : base(message, innerException) { }
}

/// <summary>Transport-level connection failure (couldn't establish a connection or it dropped mid-request).</summary>
public sealed class ApiConnectionException : WebScrapingAIException
{
    public ApiConnectionException(string message) : base(message) { }
    public ApiConnectionException(string message, Exception innerException) : base(message, innerException) { }
}
