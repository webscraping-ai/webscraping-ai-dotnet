using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace WebScrapingAI.Tests;

public class ClientTransportTests
{
    [Fact]
    public async Task Hitting_per_request_timeout_throws_ApiTimeoutException()
    {
        var handler = new StubHandler
        {
            Responder = async (_, ct) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(10), ct);
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            },
        };
        var client = new WebScrapingAIClient(new WebScrapingAIClientOptions
        {
            ApiKey = "k",
            HttpHandler = handler,
            Timeout = TimeSpan.FromMilliseconds(50),
        });

        var act = async () => await client.HtmlAsync(new HtmlRequest { Url = "https://example.com" });
        await act.Should().ThrowAsync<ApiTimeoutException>();
    }

    [Fact]
    public async Task User_cancellation_propagates_as_OperationCanceledException()
    {
        var handler = new StubHandler
        {
            Responder = async (_, ct) =>
            {
                await Task.Delay(TimeSpan.FromSeconds(10), ct);
                return new HttpResponseMessage(System.Net.HttpStatusCode.OK);
            },
        };
        using var client = new WebScrapingAIClient(new WebScrapingAIClientOptions { ApiKey = "k", HttpHandler = handler });

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(20));
        var act = async () => await client.HtmlAsync(new HtmlRequest { Url = "https://example.com" }, cts.Token);
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task Connection_failure_throws_ApiConnectionException()
    {
        var handler = StubHandler.Throwing(new HttpRequestException("connection refused"));
        using var client = new WebScrapingAIClient(new WebScrapingAIClientOptions { ApiKey = "k", HttpHandler = handler });

        var act = async () => await client.HtmlAsync(new HtmlRequest { Url = "https://example.com" });
        var ex = (await act.Should().ThrowAsync<ApiConnectionException>()).Which;
        ex.InnerException.Should().BeOfType<HttpRequestException>();
    }

    [Fact]
    public void Missing_ApiKey_throws_with_helpful_message()
    {
        var previous = Environment.GetEnvironmentVariable("WEBSCRAPING_AI_API_KEY");
        Environment.SetEnvironmentVariable("WEBSCRAPING_AI_API_KEY", null);
        try
        {
            var act = () => new WebScrapingAIClient(new WebScrapingAIClientOptions());
            act.Should().Throw<InvalidOperationException>().WithMessage("*API key*");
        }
        finally
        {
            Environment.SetEnvironmentVariable("WEBSCRAPING_AI_API_KEY", previous);
        }
    }

    [Fact]
    public void Env_var_supplies_api_key_when_options_omits_it()
    {
        var previous = Environment.GetEnvironmentVariable("WEBSCRAPING_AI_API_KEY");
        Environment.SetEnvironmentVariable("WEBSCRAPING_AI_API_KEY", "env-key");
        try
        {
            using var client = new WebScrapingAIClient(new WebScrapingAIClientOptions());
            client.ResolvedApiKey.Should().Be("env-key");
        }
        finally
        {
            Environment.SetEnvironmentVariable("WEBSCRAPING_AI_API_KEY", previous);
        }
    }

    [Fact]
    public void Explicit_options_api_key_wins_over_env_var()
    {
        var previous = Environment.GetEnvironmentVariable("WEBSCRAPING_AI_API_KEY");
        Environment.SetEnvironmentVariable("WEBSCRAPING_AI_API_KEY", "env-key");
        try
        {
            using var client = new WebScrapingAIClient(new WebScrapingAIClientOptions { ApiKey = "explicit-key" });
            client.ResolvedApiKey.Should().Be("explicit-key");
        }
        finally
        {
            Environment.SetEnvironmentVariable("WEBSCRAPING_AI_API_KEY", previous);
        }
    }
}
