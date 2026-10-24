using System.Collections.Generic;
using System.Globalization;
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
    public async Task SerpAsync_sends_query_params_and_parses_result()
    {
        const string body = "{" +
            "\"search_parameters\":{\"engine\":\"google\",\"q\":\"coffee machines\",\"gl\":\"de\",\"hl\":\"de\",\"page\":2}," +
            "\"search_information\":{\"query_displayed\":\"coffee machines\",\"organic_results_state\":\"Results for exact spelling\",\"total_results\":160000000}," +
            "\"organic_results\":[" +
            "{\"position\":1,\"title\":\"Best Coffee Machines\",\"link\":\"https://www.example.com/best\",\"domain\":\"example.com\",\"displayed_link\":\"www.example.com › Reviews\",\"snippet\":\"We tested 20\",\"date\":\"Apr 13, 2026\"}," +
            "{\"position\":2,\"title\":\"Shop\",\"link\":\"https://shop.test/\",\"domain\":\"shop.test\",\"displayed_link\":\"shop.test\"}]," +
            "\"related_searches\":[{\"query\":\"best espresso machine\"}]," +
            "\"pagination\":{\"current\":2,\"next\":3}}";
        var (client, handler) = NewClient(StubHandler.Returning(HttpStatusCode.OK, body, "application/json"));
        var result = await client.SerpAsync(new SerpRequest { Q = "coffee machines", Engine = "google", Gl = "de", Hl = "de", Page = 2 });

        ReqUri(handler).AbsolutePath.Should().Be("/serp");
        Query(handler).Should().Be("api_key=test-key&q=coffee%20machines&engine=google&gl=de&hl=de&page=2");

        result.SearchParameters.Q.Should().Be("coffee machines");
        result.SearchParameters.Page.Should().Be(2);
        result.SearchInformation.OrganicResultsState.Should().Be("Results for exact spelling");
        result.SearchInformation.ShowingResultsFor.Should().BeNull();
        result.SearchInformation.TotalResults.Should().Be(160000000);
        result.OrganicResults.Should().HaveCount(2);
        result.OrganicResults[0].Position.Should().Be(1);
        result.OrganicResults[0].Domain.Should().Be("example.com");
        result.OrganicResults[0].DisplayedLink.Should().Be("www.example.com › Reviews");
        result.OrganicResults[0].Snippet.Should().Be("We tested 20");
        result.OrganicResults[0].Date.Should().Be("Apr 13, 2026");
        result.OrganicResults[1].Snippet.Should().BeNull();
        result.OrganicResults[1].Date.Should().BeNull();
        result.RelatedSearches.Should().ContainSingle().Which.Query.Should().Be("best espresso machine");
        result.Pagination.Current.Should().Be(2);
        result.Pagination.Next.Should().Be(3);
    }

    [Fact]
    public async Task SerpAsync_omits_unset_options_and_scraping_params()
    {
        const string body = "{\"search_parameters\":{\"engine\":\"google\",\"q\":\"nothing\",\"gl\":\"us\",\"hl\":\"en\",\"page\":1}," +
            "\"search_information\":{\"query_displayed\":\"nothing\",\"organic_results_state\":\"Fully empty\"}," +
            "\"organic_results\":[],\"pagination\":{\"current\":1}}";
        var (client, handler) = NewClient(StubHandler.Returning(HttpStatusCode.OK, body, "application/json"));
        var result = await client.SerpAsync(new SerpRequest { Q = "nothing" });

        Query(handler).Should().Be("api_key=test-key&q=nothing");
        result.OrganicResults.Should().BeEmpty();
        result.RelatedSearches.Should().BeNull();
        result.SearchInformation.TotalResults.Should().BeNull();
        result.Pagination.Next.Should().BeNull();
    }

    [Fact]
    public async Task SerpAsync_sends_location_params_in_order_and_parses_multi_page_result()
    {
        const string body = "{" +
            "\"search_parameters\":{\"engine\":\"google\",\"q\":\"coffee machines\",\"gl\":\"us\",\"hl\":\"en\",\"page\":1,\"pages\":2," +
            "\"location_requested\":\"Austin, TX\",\"location_used\":\"Austin,Texas,United States\",\"lat\":30.2672,\"lon\":-97.7431,\"radius\":500}," +
            "\"search_information\":{\"query_displayed\":\"coffee machines\",\"organic_results_state\":\"Results for exact spelling\"}," +
            "\"organic_results\":[" +
            "{\"position\":1,\"page\":1,\"title\":\"A\",\"link\":\"https://a.test/\",\"domain\":\"a.test\",\"displayed_link\":\"a.test\"}," +
            "{\"position\":11,\"page\":2,\"title\":\"B\",\"link\":\"https://b.test/\",\"domain\":\"b.test\",\"displayed_link\":\"b.test\"}]," +
            "\"pagination\":{\"current\":2,\"next\":3,\"pages_returned\":2}}";
        var (client, handler) = NewClient(StubHandler.Returning(HttpStatusCode.OK, body, "application/json"));
        var result = await client.SerpAsync(new SerpRequest
        {
            Q = "coffee machines",
            Page = 1,
            Pages = 2,
            Location = " Austin, TX",
            Uule = "w+CAIQICI",
            Lat = 30.2672,
            Lon = -97.7431,
            Radius = 500,
        });

        // Combinations are the server's to reject; the client sends whatever it is given, untrimmed.
        Query(handler).Should().Be(
            "api_key=test-key&q=coffee%20machines&page=1&pages=2&location=%20Austin%2C%20TX&uule=w%2BCAIQICI" +
            "&lat=30.2672&lon=-97.7431&radius=500");

        result.SearchParameters.Pages.Should().Be(2);
        result.SearchParameters.LocationRequested.Should().Be("Austin, TX");
        result.SearchParameters.LocationUsed.Should().Be("Austin,Texas,United States");
        result.SearchParameters.Radius.Should().Be(500);
        result.SearchParameters.Lat.Should().Be(30.2672);
        result.SearchParameters.Lon.Should().Be(-97.7431);
        result.OrganicResults.Select(r => r.Page).Should().Equal(1, 2);
        result.OrganicResults[1].Position.Should().Be(11);
        result.Pagination.PagesReturned.Should().Be(2);
    }

    [Fact]
    public async Task SerpAsync_leaves_new_result_fields_null_when_absent()
    {
        const string body = "{\"search_parameters\":{\"engine\":\"google\",\"q\":\"x\",\"gl\":\"us\",\"hl\":\"en\",\"page\":1,\"lat\":48,\"lon\":-0.5}," +
            "\"search_information\":{\"query_displayed\":\"x\",\"organic_results_state\":\"Results for exact spelling\"}," +
            "\"organic_results\":[{\"position\":1,\"title\":\"A\",\"link\":\"https://a.test/\",\"domain\":\"a.test\",\"displayed_link\":\"a.test\"}]," +
            "\"pagination\":{\"current\":1}}";
        var (client, _) = NewClient(StubHandler.Returning(HttpStatusCode.OK, body, "application/json"));
        var result = await client.SerpAsync(new SerpRequest { Q = "x" });

        result.SearchParameters.Pages.Should().BeNull();
        result.SearchParameters.LocationRequested.Should().BeNull();
        result.SearchParameters.LocationUsed.Should().BeNull();
        result.SearchParameters.Radius.Should().BeNull();
        result.SearchParameters.Lat.Should().Be(48);
        result.SearchParameters.Lon.Should().Be(-0.5);
        result.OrganicResults[0].Page.Should().BeNull();
        result.Pagination.PagesReturned.Should().BeNull();
    }

    [Theory]
    [InlineData(30.2672, "30.2672")]
    [InlineData(-97.7431, "-97.7431")]
    [InlineData(1e-7, "0.0000001")]
    [InlineData(-1e-7, "-0.0000001")]
    [InlineData(1e-9, "0")]
    [InlineData(-1e-9, "0")]
    [InlineData(1e-5, "0.00001")]
    [InlineData(48.0, "48")]
    [InlineData(-180.0, "-180")]
    [InlineData(0.0, "0")]
    [InlineData(12.123456789, "12.1234568")]
    [InlineData(90.10000000001, "90.1")]
    public void FormatCoordinate_writes_plain_decimal(double value, string expected)
    {
        WebScrapingAIClient.FormatCoordinate(value).Should().Be(expected);
        WebScrapingAIClient.FormatCoordinate(value).Should().MatchRegex(@"^-?\d+(\.\d+)?$");
    }

    [Fact]
    public void FormatCoordinate_normalizes_negative_zero()
    {
        // A separate fact: xunit treats InlineData(0.0) and InlineData(-0.0) as duplicates.
        WebScrapingAIClient.FormatCoordinate(-0.0).Should().Be("0");
        WebScrapingAIClient.FormatCoordinate(-0.00000001).Should().Be("0");
    }

    [Theory]
    [InlineData("de-DE")]
    [InlineData("fr-FR")]
    [InlineData("en-US")]
    public async Task SerpAsync_sends_lat_lon_as_plain_decimal_under_any_culture(string culture)
    {
        var previous = CultureInfo.CurrentCulture;
        var previousUi = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo(culture);
            // Guard against globalization-invariant mode silently turning this into an en-US test.
            1.5.ToString(CultureInfo.CurrentCulture).Should().Be(culture == "en-US" ? "1.5" : "1,5");
            var (client, handler) = NewClient(StubHandler.Returning(HttpStatusCode.OK, "{}", "application/json"));
            await client.SerpAsync(new SerpRequest { Q = "coffee", Lat = 1e-7, Lon = -1234.5678901234, Radius = 1000 });

            Query(handler).Should().Be("api_key=test-key&q=coffee&lat=0.0000001&lon=-1234.5678901&radius=1000");
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
            CultureInfo.CurrentUICulture = previousUi;
        }
    }

    [Fact]
    public async Task SerpLocationsAsync_sends_q_and_limit_and_parses_locations()
    {
        const string body = "[" +
            "{\"id\":\"1026201\",\"google_id\":1026201,\"google_parent_id\":21176,\"name\":\"Austin\"," +
            "\"canonical_name\":\"Austin,Texas,United States\",\"country_code\":\"US\",\"target_type\":\"City\"," +
            "\"reach\":974447,\"gps\":[-97.7431,30.2672]}," +
            "{\"id\":\"2840\",\"google_id\":2840,\"name\":\"United States\",\"canonical_name\":\"United States\"," +
            "\"country_code\":\"US\",\"target_type\":\"Neighborhood Watch\",\"reach\":9000000000,\"gps\":[-77.0369,38.9072]}]";
        var (client, handler) = NewClient(StubHandler.Returning(HttpStatusCode.OK, body, "application/json"));
        var locations = await client.SerpLocationsAsync(new SerpLocationsRequest { Q = "austin", Limit = 3 });

        ReqUri(handler).AbsolutePath.Should().Be("/serp/locations");
        Query(handler).Should().Be("api_key=test-key&q=austin&limit=3");

        locations.Should().HaveCount(2);
        var austin = locations[0];
        austin.Id.Should().Be("1026201");
        austin.GoogleId.Should().Be(1026201L);
        austin.GoogleParentId.Should().Be(21176L);
        austin.Name.Should().Be("Austin");
        austin.CanonicalName.Should().Be("Austin,Texas,United States");
        austin.CountryCode.Should().Be("US");
        austin.TargetType.Should().Be("City");
        austin.Reach.Should().Be(974447L);
        austin.Gps.Should().Equal(-97.7431, 30.2672);
        // Countries have no parent; target_type is an open string; reach can exceed int32.
        locations[1].GoogleParentId.Should().BeNull();
        locations[1].TargetType.Should().Be("Neighborhood Watch");
        locations[1].Reach.Should().Be(9000000000L);
    }

    [Fact]
    public async Task SerpLocationsAsync_without_request_sends_only_the_key()
    {
        var (client, handler) = NewClient(StubHandler.Returning(HttpStatusCode.OK, "[]", "application/json"));
        var locations = await client.SerpLocationsAsync();

        ReqUri(handler).AbsolutePath.Should().Be("/serp/locations");
        Query(handler).Should().Be("api_key=test-key");
        locations.Should().BeEmpty();
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
