using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace WebScrapingAI.Tests;

/// <summary>
/// Minimal <see cref="HttpMessageHandler"/> stub. Captures every request and
/// returns a queued response. Lighter than WireMock.Net and good enough for
/// the surface we exercise.
/// </summary>
internal sealed class StubHandler : HttpMessageHandler
{
    public List<HttpRequestMessage> Requests { get; } = new();
    public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? Responder { get; set; }

    public static StubHandler Returning(HttpStatusCode status, string body, string contentType = "text/html")
    {
        return new StubHandler
        {
            Responder = (_, __) => Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(body, System.Text.Encoding.UTF8, contentType),
            }),
        };
    }

    public static StubHandler Throwing(Exception exception)
    {
        return new StubHandler
        {
            Responder = (_, __) => throw exception,
        };
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(request);
        if (Responder is null) throw new InvalidOperationException("StubHandler.Responder not set");
        return await Responder(request, cancellationToken).ConfigureAwait(false);
    }
}
