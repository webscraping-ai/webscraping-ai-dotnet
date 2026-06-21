using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace WebScrapingAI.Tests;

public class ClientEndpointTests
{
    private static (WebScrapingAIClient, StubHandler) NewClient(StubHandler handler) =>
        (new WebScrapingAIClient(new WebScrapingAIClientOptions { ApiKey = "test-key", HttpHandler = handler }), handler);

    private static System.Uri ReqUri(StubHandler handler) => handler.Requests.Single().RequestUri!;

    private static string Query(StubHandler handler) => ReqUri(handler).Query.TrimStart('?');

    [Fact]
    public async Task HtmlAsync_sends_url_and_returns_body()
    {
        var (client, handler) = NewClient(StubHandler.Returning(HttpStatusCode.OK, "<html>hi</html>"));
        var html = await client.HtmlAsync(new HtmlRequest { Url = "https://example.com" });

        html.Should().Be("<html>hi</html>");
        ReqUri(handler).AbsolutePath.Should().Be("/html");
        Query(handler).Should().StartWith("api_key=test-key&url=https%3A%2F%2Fexample.com");
    }

    [Fact]
    public async Task HtmlAsync_serializes_common_options()
    {
        var (client, handler) = NewClient(StubHandler.Returning(HttpStatusCode.OK, "ok"));
        await client.HtmlAsync(new HtmlRequest
        {
            Url = "https://example.com",
            Js = true,
            Country = "gb",
            Timeout = 15000,
            Headers = new Dictionary<string, string> { ["Cookie"] = "session=x" },
        });
        var q = Query(handler);
        q.Should().Contain("js=true");
        q.Should().Contain("country=gb");
        q.Should().Contain("timeout=15000");
        q.Should().Contain("headers%5BCookie%5D=session%3Dx");
    }

    [Fact]
    public async Task TextAsync_sends_text_format_and_return_links()
    {
        var (client, handler) = NewClient(StubHandler.Returning(HttpStatusCode.OK, "some text"));
        var text = await client.TextAsync(new TextRequest { Url = "https://example.com", TextFormat = "json", ReturnLinks = true });
        text.Should().Be("some text");
        ReqUri(handler).AbsolutePath.Should().Be("/text");
        Query(handler).Should().Contain("text_format=json").And.Contain("return_links=true");
    }

    [Fact]
    public async Task SelectedAsync_sends_selector_when_present()
    {
        var (client, handler) = NewClient(StubHandler.Returning(HttpStatusCode.OK, "<h1>x</h1>"));
        await client.SelectedAsync(new SelectedRequest { Url = "https://example.com", Selector = "h1" });
        ReqUri(handler).AbsolutePath.Should().Be("/selected");
        Query(handler).Should().Contain("selector=h1");
    }

    [Fact]
    public async Task SelectedMultipleAsync_emits_selectors_without_brackets()
    {
        var (client, handler) = NewClient(StubHandler.Returning(HttpStatusCode.OK, "[[\"<h1>Example</h1>\",\"<p>x</p>\"]]"));
        var result = await client.SelectedMultipleAsync(new SelectedMultipleRequest
        {
            Url = "https://example.com",
            Selectors = new[] { "h1", ".price" },
        });

        result.Results.Should().HaveCount(1);
        result.Results[0].Should().BeEquivalentTo(new[] { "<h1>Example</h1>", "<p>x</p>" });

        ReqUri(handler).AbsolutePath.Should().Be("/selected-multiple");
        var q = Query(handler);
        // Repeated key, no [], in insertion order.
        q.Should().Contain("selectors=h1&selectors=.price");
        q.Should().NotContain("selectors%5B");
    }

    [Fact]
    public async Task QuestionAsync_unwraps_JSON_string_body()
    {
        var (client, handler) = NewClient(StubHandler.Returning(HttpStatusCode.OK, "\"the answer\""));
        var answer = await client.QuestionAsync(new QuestionRequest { Url = "https://example.com", Question = "what is this?" });

        answer.Should().Be("the answer");
        ReqUri(handler).AbsolutePath.Should().Be("/ai/question");
        Query(handler).Should().Contain("question=what%20is%20this%3F");
    }

    [Fact]
    public async Task QuestionAsync_passes_format_json_unchanged()
    {
        var (client, _) = NewClient(StubHandler.Returning(HttpStatusCode.OK, "{\"answer\":\"x\"}"));
        var body = await client.QuestionAsync(new QuestionRequest { Url = "https://example.com", Question = "?", Format = "json" });
        body.Should().Be("{\"answer\":\"x\"}");
    }

    [Fact]
    public async Task FieldsAsync_emits_deepObject_fields_and_parses_result_wrapper()
    {
        var (client, handler) = NewClient(StubHandler.Returning(HttpStatusCode.OK, "{\"result\":{\"title\":\"Example\",\"price\":null}}"));
        var result = await client.FieldsAsync(new FieldsRequest
        {
            Url = "https://example.com",
            Fields = new Dictionary<string, string>
            {
                ["title"] = "Main product title",
                ["price"] = "Current price",
            },
        });

        result.Result.Should().NotBeNull();
        result.Result!["title"].Should().Be("Example");
        result.Result["price"].Should().BeNull();

        ReqUri(handler).AbsolutePath.Should().Be("/ai/fields");
        var q = Query(handler);
        q.Should().Contain("fields%5Bprice%5D=Current%20price");
        q.Should().Contain("fields%5Btitle%5D=Main%20product%20title");
    }

    [Fact]
    public async Task AccountAsync_returns_typed_info()
    {
        var (client, handler) = NewClient(StubHandler.Returning(HttpStatusCode.OK, "{\"email\":\"a@b.c\",\"remaining_api_calls\":500,\"resets_at\":1700000000,\"remaining_concurrency\":10}"));
        var info = await client.AccountAsync();

        info.Email.Should().Be("a@b.c");
        info.RemainingApiCalls.Should().Be(500);
        info.ResetsAt.Should().Be(1700000000);
        info.RemainingConcurrency.Should().Be(10);
        ReqUri(handler).AbsolutePath.Should().Be("/account");
    }

    [Fact]
    public async Task ApiKey_appears_first_in_query_string()
    {
        var (client, handler) = NewClient(StubHandler.Returning(HttpStatusCode.OK, "ok"));
        await client.HtmlAsync(new HtmlRequest { Url = "https://example.com" });
        Query(handler).Should().StartWith("api_key=test-key&");
    }

    [Fact]
    public async Task UserAgent_header_is_sent()
    {
        var (client, handler) = NewClient(StubHandler.Returning(HttpStatusCode.OK, "ok"));
        await client.HtmlAsync(new HtmlRequest { Url = "https://example.com" });
        var ua = handler.Requests.Single().Headers.UserAgent.ToString();
        ua.Should().StartWith("webscraping-ai-dotnet/");
    }
}
