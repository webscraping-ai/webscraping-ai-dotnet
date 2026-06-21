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
    public async Task FieldsAsync_requires_at_least_one_field()
    {
        var client = Client();
        var act = async () => await client.FieldsAsync(new FieldsRequest { Url = "https://example.com", Fields = new Dictionary<string, string>() });
        await act.Should().ThrowAsync<System.ArgumentException>().WithMessage("*field*");
    }
}
