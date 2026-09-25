using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace WebScrapingAI.Tests;

public class DataEndpointTests
{
    private const string ApiKey = "sk-data-test-4f1c9e";

    private const string OkBody = "{\"request_parameters\":{\"url\":\"https://www.youtube.com/watch?v=dQw4w9WgXcQ\",\"provider\":\"youtube\",\"type\":\"video\"}," +
        "\"parse_status\":\"ok\",\"data\":{\"title\":\"Never Gonna Give You Up\",\"view_count\":1700000000,\"transcript\":null}}";

    private static (WebScrapingAIClient, StubHandler) NewClient(StubHandler handler) =>
        (new WebScrapingAIClient(new WebScrapingAIClientOptions { ApiKey = ApiKey, HttpHandler = handler }), handler);

    private static StubHandler Ok(string body = OkBody) => StubHandler.Returning(HttpStatusCode.OK, body, "application/json");

    private static string Query(StubHandler handler) => handler.Requests.Single().RequestUri!.Query.TrimStart('?');

    [Fact]
    public async Task DataAsync_encodes_url_country_transcript_and_language()
    {
        var (client, handler) = NewClient(Ok());
        await client.DataAsync(new DataRequest
        {
            Url = "https://www.youtube.com/watch?v=dQw4w9WgXcQ&t=10",
            Country = "de",
            Transcript = true,
            TranscriptLanguage = "en",
        });

        handler.Requests.Single().RequestUri!.AbsolutePath.Should().Be("/data");
        Query(handler).Should().Be(
            $"api_key={ApiKey}&url=https%3A%2F%2Fwww.youtube.com%2Fwatch%3Fv%3DdQw4w9WgXcQ%26t%3D10&country=de&transcript=true&transcript_language=en");
    }

    [Fact]
    public async Task DataAsync_sends_transcript_false_as_string_and_omits_unset_options()
    {
        var (client, handler) = NewClient(Ok());
        await client.DataAsync(new DataRequest { Url = "https://www.youtube.com/watch?v=x", Transcript = false });

        Query(handler).Should().Be($"api_key={ApiKey}&url=https%3A%2F%2Fwww.youtube.com%2Fwatch%3Fv%3Dx&transcript=false");
    }

    [Fact]
    public async Task DataAsync_sends_no_scraping_params()
    {
        var (client, handler) = NewClient(Ok());
        await client.DataAsync(new DataRequest { Url = "https://www.tiktok.com/@someone" });

        var q = Query(handler);
        foreach (var p in new[] { "js=", "proxy=", "headers", "timeout=", "device=", "wait_for=", "js_timeout=" })
            q.Should().NotContain(p);
    }

    [Fact]
    public async Task DataAsync_encodes_extra_params_with_the_same_encoder()
    {
        var (client, handler) = NewClient(Ok());
        await client.DataAsync(new DataRequest
        {
            Url = "https://www.reddit.com/r/dotnet/",
            ExtraParams = new Dictionary<string, string>
            {
                ["sort"] = "top",
                ["a&b=c"] = "x&y=z w",
            },
        });

        Query(handler).Should().Be($"api_key={ApiKey}&url=https%3A%2F%2Fwww.reddit.com%2Fr%2Fdotnet%2F&sort=top&a%26b%3Dc=x%26y%3Dz%20w");
    }

    [Theory]
    [InlineData("api_key")]
    [InlineData("url")]
    [InlineData("API_KEY")]
    [InlineData("Url")]
    public async Task DataAsync_rejects_api_key_or_url_in_extra_params_before_any_request(string key)
    {
        var (client, handler) = NewClient(Ok());
        var act = async () => await client.DataAsync(new DataRequest
        {
            Url = "https://www.youtube.com/watch?v=x",
            ExtraParams = new Dictionary<string, string> { [key] = "override" },
        });

        var ex = (await act.Should().ThrowAsync<ArgumentException>()).Which;
        ex.ParamName.Should().Be("ExtraParams");
        handler.Requests.Should().BeEmpty();
    }

    [Theory]
    [InlineData("country", false)]
    [InlineData("transcript", false)]
    [InlineData("transcript_language", false)]
    [InlineData("Country", true)]
    [InlineData("TRANSCRIPT", true)]
    [InlineData("transcript_language", true)]
    public async Task DataAsync_rejects_typed_option_names_in_extra_params_whether_or_not_set(string key, bool typedSet)
    {
        var (client, handler) = NewClient(Ok());
        var act = async () => await client.DataAsync(new DataRequest
        {
            Url = "https://www.youtube.com/watch?v=x",
            Country = typedSet ? "us" : null,
            Transcript = typedSet ? true : null,
            TranscriptLanguage = typedSet ? "en" : null,
            ExtraParams = new Dictionary<string, string> { [key] = "de" },
        });

        var ex = (await act.Should().ThrowAsync<ArgumentException>()).Which;
        ex.ParamName.Should().Be("ExtraParams");
        ex.Message.Should().Contain("use DataRequest.");
        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task DataAsync_drops_empty_extra_param_values()
    {
        var (client, handler) = NewClient(Ok());
        await client.DataAsync(new DataRequest
        {
            Url = "https://x.test/",
            ExtraParams = new Dictionary<string, string> { ["empty"] = "", ["nul"] = null!, ["kept"] = "1" },
        });

        Query(handler).Should().Be($"api_key={ApiKey}&url=https%3A%2F%2Fx.test%2F&kept=1");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t\n")]
    [InlineData(null)]
    public async Task DataAsync_rejects_blank_url_before_any_request(string? url)
    {
        var (client, handler) = NewClient(Ok());
        var act = async () => await client.DataAsync(new DataRequest { Url = url! });

        var ex = (await act.Should().ThrowAsync<ArgumentException>()).Which;
        ex.ParamName.Should().Be("Url");
        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task DataAsync_rejects_null_request()
    {
        var (client, _) = NewClient(Ok());
        await Assert.ThrowsAsync<ArgumentNullException>(() => client.DataAsync(null!));
    }

    [Theory]
    [InlineData("  https://Example.COM/A%2Fb/\u00fcn\u00ef?x=1&y=a b#Frag  ")]
    [InlineData("https://example.com/anything")]
    [InlineData("not even a url")]
    public async Task DataAsync_sends_unknown_site_urls_unmodified_without_client_side_error(string url)
    {
        // No client-side allowlist and no normalisation: no lower-casing, trimming,
        // fragment dropping or double-encoding. New sites are added on the server.
        var (client, handler) = NewClient(Ok());
        await client.DataAsync(new DataRequest { Url = url });

        var query = Query(handler);
        query.Should().Be($"api_key={ApiKey}&url={Uri.EscapeDataString(url)}");
        var sent = query.Split('&').Single(p => p.StartsWith("url=", StringComparison.Ordinal)).Substring("url=".Length);
        Uri.UnescapeDataString(sent).Should().Be(url);
    }

    [Fact]
    public async Task DataAsync_hostile_url_exact_wire_bytes()
    {
        const string url = "  https://Example.COM/A%2Fb/\u00fcn\u00ef?x=1&y=a b#Frag  ";
        var (client, handler) = NewClient(Ok());
        await client.DataAsync(new DataRequest { Url = url });

        Query(handler).Should().Be(
            $"api_key={ApiKey}&url=%20%20https%3A%2F%2FExample.COM%2FA%252Fb%2F%C3%BCn%C3%AF%3Fx%3D1%26y%3Da%20b%23Frag%20%20");
    }

    [Fact]
    public async Task DataAsync_parses_result()
    {
        var (client, _) = NewClient(Ok());
        var result = await client.DataAsync(new DataRequest { Url = "https://www.youtube.com/watch?v=dQw4w9WgXcQ" });

        result.RequestParameters.Url.Should().Be("https://www.youtube.com/watch?v=dQw4w9WgXcQ");
        result.RequestParameters.Provider.Should().Be("youtube");
        result.RequestParameters.Type.Should().Be("video");
        result.ParseStatus.Should().Be("ok");
        result.Data.Should().NotBeNull();
        var data = result.Data!.Value;
        data.ValueKind.Should().Be(JsonValueKind.Object);
        data.GetProperty("title").GetString().Should().Be("Never Gonna Give You Up");
        data.GetProperty("view_count").GetInt64().Should().Be(1700000000L);
        data.GetProperty("transcript").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task DataAsync_round_trips_unknown_provider_type_and_status_strings()
    {
        const string body = "{\"request_parameters\":{\"url\":\"https://future.test/x\",\"provider\":\"futuresite\",\"type\":\"hologram\"}," +
            "\"parse_status\":\"partially_parsed\",\"data\":[1,\"two\",{\"three\":3}]}";
        var (client, _) = NewClient(Ok(body));
        var result = await client.DataAsync(new DataRequest { Url = "https://future.test/x" });

        result.RequestParameters.Provider.Should().Be("futuresite");
        result.RequestParameters.Type.Should().Be("hologram");
        result.ParseStatus.Should().Be("partially_parsed");
        result.Data!.Value.ValueKind.Should().Be(JsonValueKind.Array);
        result.Data.Value.GetArrayLength().Should().Be(3);
    }

    [Fact]
    public async Task DataAsync_parses_null_data_with_parse_failed()
    {
        const string body = "{\"request_parameters\":{\"url\":\"https://www.instagram.com/p/abc/\",\"provider\":\"instagram\",\"type\":\"post\"}," +
            "\"parse_status\":\"parse_failed\",\"data\":null}";
        var (client, _) = NewClient(Ok(body));
        var result = await client.DataAsync(new DataRequest { Url = "https://www.instagram.com/p/abc/" });

        result.ParseStatus.Should().Be("parse_failed");
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task DataAsync_null_request_parameters_and_parse_status_read_as_empty()
    {
        var (client, _) = NewClient(Ok("{\"request_parameters\":null,\"parse_status\":null,\"data\":null}"));
        var result = await client.DataAsync(new DataRequest { Url = "https://x.test/" });

        result.RequestParameters.Should().NotBeNull();
        result.RequestParameters.Provider.Should().BeEmpty();
        result.ParseStatus.Should().BeEmpty();
        result.Data.Should().BeNull();
    }

    [Fact]
    public async Task DataAsync_missing_request_parameters_reads_as_empty()
    {
        var (client, _) = NewClient(Ok("{\"data\":{}}"));
        var result = await client.DataAsync(new DataRequest { Url = "https://x.test/" });

        result.RequestParameters.Should().NotBeNull();
        result.ParseStatus.Should().BeEmpty();
    }

    [Fact]
    public async Task DataAsync_maps_unsupported_url_400_to_BadRequestException()
    {
        const string message = "Unsupported URL for /data. Supported sites: youtube, tiktok, x, linkedin, instagram, reddit. For other sites, use /ai/fields";
        var (client, _) = NewClient(StubHandler.Returning(HttpStatusCode.BadRequest, "{\"message\":\"" + message + "\"}", "application/json"));

        var act = async () => await client.DataAsync(new DataRequest { Url = "https://example.com/" });
        var ex = (await act.Should().ThrowAsync<BadRequestException>()).Which;
        ex.Status.Should().Be(400);
        ex.Message.Should().Contain("Unsupported URL for /data");
        ex.ApiStatusCode.Should().BeNull();
        AssertKeyAbsent(ex);
    }

    [Theory]
    [InlineData(402, typeof(PaymentRequiredException))]
    [InlineData(403, typeof(AuthenticationException))]
    [InlineData(429, typeof(RateLimitException))]
    [InlineData(500, typeof(ServerException))]
    [InlineData(504, typeof(GatewayTimeoutException))]
    public async Task DataAsync_maps_other_statuses_and_keeps_key_out_of_errors(int status, Type expected)
    {
        var (client, _) = NewClient(StubHandler.Returning((HttpStatusCode)status, "{\"message\":\"boom\"}", "application/json"));

        var act = async () => await client.DataAsync(new DataRequest { Url = "https://www.youtube.com/watch?v=x" });
        var ex = (await act.Should().ThrowAsync<ApiException>()).Which;
        ex.GetType().Should().Be(expected);
        ex.Message.Should().Contain("boom");
        AssertKeyAbsent(ex);
    }

    [Fact]
    public async Task DataAsync_connection_error_does_not_leak_api_key()
    {
        // Realistic: the transport error embeds the full request URL, key included.
        var handler = new StubHandler
        {
            Responder = (req, _) => throw new HttpRequestException(
                $"An error occurred while sending the request to {req.RequestUri}",
                new System.IO.IOException($"Unable to read data from {req.RequestUri!.AbsoluteUri}")),
        };
        var (client, _) = NewClient(handler);

        var act = async () => await client.DataAsync(new DataRequest { Url = "https://www.youtube.com/watch?v=x" });
        var ex = (await act.Should().ThrowAsync<ApiConnectionException>()).Which;
        ex.Message.Should().Contain("api_key=[REDACTED]");
        ex.InnerException.Should().BeOfType<HttpRequestException>();
        ex.InnerException!.InnerException.Should().NotBeNull();
        AssertKeyAbsent(ex);
    }

    [Fact]
    public async Task DataAsync_timeout_does_not_leak_api_key()
    {
        // Realistic: the cancellation carries the request URL (key included) in its message.
        var handler = new StubHandler
        {
            Responder = async (req, ct) =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(10), ct);
                }
                catch (OperationCanceledException oce)
                {
                    throw new TaskCanceledException($"The request to {req.RequestUri} was canceled.", oce);
                }
                return new HttpResponseMessage(HttpStatusCode.OK);
            },
        };
        using var client = new WebScrapingAIClient(new WebScrapingAIClientOptions
        {
            ApiKey = ApiKey,
            HttpHandler = handler,
            Timeout = TimeSpan.FromMilliseconds(50),
        });

        var act = async () => await client.DataAsync(new DataRequest { Url = "https://www.youtube.com/watch?v=x" });
        var ex = (await act.Should().ThrowAsync<ApiTimeoutException>()).Which;
        ex.InnerException.Should().BeOfType<TaskCanceledException>();
        AssertKeyAbsent(ex);
    }

    [Fact]
    public async Task Connection_error_without_key_keeps_original_inner_exception()
    {
        var original = new HttpRequestException("No such host is known. (api.webscraping.ai:443)");
        var (client, _) = NewClient(StubHandler.Throwing(original));

        var act = async () => await client.DataAsync(new DataRequest { Url = "https://www.youtube.com/watch?v=x" });
        var ex = (await act.Should().ThrowAsync<ApiConnectionException>()).Which;
        ex.InnerException.Should().BeSameAs(original);
    }

    private static void AssertKeyAbsent(Exception ex)
    {
        for (Exception? e = ex; e is not null; e = e.InnerException)
        {
            e.Message.Should().NotContain(ApiKey);
        }
        ex.ToString().Should().NotContain(ApiKey);
    }
}
