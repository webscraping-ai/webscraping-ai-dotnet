using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace WebScrapingAI.Tests;

public class ClientValidationTests
{
    private static WebScrapingAIClient Client() =>
        new(new WebScrapingAIClientOptions { ApiKey = "k", HttpHandler = StubHandler.Returning(System.Net.HttpStatusCode.OK, "ok") });

    [Fact]
    public async Task HtmlAsync_requires_url()
    {
        var client = Client();
        await Assert.ThrowsAsync<System.ArgumentException>(() => client.HtmlAsync(new HtmlRequest()));
    }

    [Fact]
    public async Task TextAsync_requires_url()
    {
        var client = Client();
        await Assert.ThrowsAsync<System.ArgumentException>(() => client.TextAsync(new TextRequest()));
    }

    [Fact]
    public async Task SelectedAsync_allows_missing_selector()
    {
        // selector is optional per the API: omitting it returns whole-page HTML.
        var handler = StubHandler.Returning(System.Net.HttpStatusCode.OK, "<h1>x</h1>");
        var client = new WebScrapingAIClient(new WebScrapingAIClientOptions { ApiKey = "k", HttpHandler = handler });
        await client.SelectedAsync(new SelectedRequest { Url = "https://example.com" });
        handler.Requests[0].RequestUri!.Query.Should().NotContain("selector=");
    }

    [Fact]
    public async Task SelectedMultipleAsync_allows_missing_selectors()
    {
        // selectors are optional per the API: omitting them returns whole-page HTML.
        var handler = StubHandler.Returning(System.Net.HttpStatusCode.OK, "[[\"<h1>x</h1>\"]]");
        var client = new WebScrapingAIClient(new WebScrapingAIClientOptions { ApiKey = "k", HttpHandler = handler });
        await client.SelectedMultipleAsync(new SelectedMultipleRequest { Url = "https://example.com" });
        handler.Requests[0].RequestUri!.Query.Should().NotContain("selectors=");
    }

    [Fact]
    public async Task QuestionAsync_requires_question()
    {
        var client = Client();
        await Assert.ThrowsAsync<System.ArgumentException>(() => client.QuestionAsync(new QuestionRequest { Url = "https://example.com" }));
    }

    [Fact]
    public async Task SerpAsync_requires_q()
    {
        var client = Client();
        var act = async () => await client.SerpAsync(new SerpRequest());
        await act.Should().ThrowAsync<System.ArgumentException>().WithMessage("*Q*");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public async Task SerpAsync_rejects_blank_q_before_any_request(string q)
    {
        var handler = StubHandler.Returning(System.Net.HttpStatusCode.OK, "{}");
        var client = new WebScrapingAIClient(new WebScrapingAIClientOptions { ApiKey = "k", HttpHandler = handler });
        var act = async () => await client.SerpAsync(new SerpRequest { Q = q });
        await act.Should().ThrowAsync<System.ArgumentException>().WithMessage("*Q*");
        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task SerpAsync_rejects_null_q()
    {
        var handler = StubHandler.Returning(System.Net.HttpStatusCode.OK, "{}");
        var client = new WebScrapingAIClient(new WebScrapingAIClientOptions { ApiKey = "k", HttpHandler = handler });
        var act = async () => await client.SerpAsync(new SerpRequest { Q = null! });
        await act.Should().ThrowAsync<System.ArgumentException>();
        handler.Requests.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public async Task SerpAsync_rejects_page_below_one_before_any_request(int page)
    {
        var handler = StubHandler.Returning(System.Net.HttpStatusCode.OK, "{}");
        var client = new WebScrapingAIClient(new WebScrapingAIClientOptions { ApiKey = "k", HttpHandler = handler });
        var act = async () => await client.SerpAsync(new SerpRequest { Q = "coffee", Page = page });
        var ex = (await act.Should().ThrowAsync<System.ArgumentOutOfRangeException>()).Which;
        ex.ParamName.Should().Be("Page");
        handler.Requests.Should().BeEmpty();
    }

    [Fact]
    public async Task SerpAsync_sends_q_untrimmed_and_accepts_page_one()
    {
        var handler = StubHandler.Returning(System.Net.HttpStatusCode.OK, "{}", "application/json");
        var client = new WebScrapingAIClient(new WebScrapingAIClientOptions { ApiKey = "k", HttpHandler = handler });
        await client.SerpAsync(new SerpRequest { Q = "  coffee ", Page = 1 });
        var query = handler.Requests[0].RequestUri!.Query;
        query.Should().Contain("q=%20%20coffee%20");
        query.Should().Contain("page=1");
    }

    [Fact]
    public async Task FieldsAsync_requires_at_least_one_field()
    {
        var client = Client();
        var act = async () => await client.FieldsAsync(new FieldsRequest { Url = "https://example.com", Fields = new Dictionary<string, string>() });
        await act.Should().ThrowAsync<System.ArgumentException>().WithMessage("*field*");
    }
}
