using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace WebScrapingAI.Tests;

public class ClientErrorMappingTests
{
    private static WebScrapingAIClient Client(StubHandler handler) =>
        new(new WebScrapingAIClientOptions { ApiKey = "k", HttpHandler = handler });

    private static StubHandler Json(HttpStatusCode status, string body) =>
        StubHandler.Returning(status, body, "application/json");

    [Theory]
    [InlineData(400, typeof(BadRequestException))]
    [InlineData(402, typeof(PaymentRequiredException))]
    [InlineData(403, typeof(AuthenticationException))]
    [InlineData(429, typeof(RateLimitException))]
    [InlineData(504, typeof(GatewayTimeoutException))]
    public async Task Maps_status_to_typed_subclass(int status, System.Type expected)
    {
        var client = Client(Json((HttpStatusCode)status, "{\"message\":\"boom\"}"));
        var act = async () => await client.HtmlAsync(new HtmlRequest { Url = "https://example.com" });
        var ex = (await act.Should().ThrowAsync<ApiException>()).Which;
        ex.GetType().Should().Be(expected);
        ex.Message.Should().Contain("boom");
    }

    [Fact]
    public async Task Maps_500_with_envelope_to_ServerException_with_fields()
    {
        var body = "{\"message\":\"Unexpected HTTP code\",\"status_code\":502,\"status_message\":\"Bad Gateway\",\"body\":\"<html>oops</html>\"}";
        var client = Client(Json(HttpStatusCode.InternalServerError, body));

        var ex = await Assert.ThrowsAsync<ServerException>(() =>
            client.HtmlAsync(new HtmlRequest { Url = "https://example.com" }));

        ex.ApiStatusCode.Should().Be(502);
        ex.ApiStatusMessage.Should().Be("Bad Gateway");
        ex.ApiBody.Should().Contain("oops");
        ex.ResponseBody.Should().Contain("status_code");
    }

    [Fact]
    public async Task Maps_unmapped_status_to_base_ApiException()
    {
        var client = Client(StubHandler.Returning((HttpStatusCode)418, "{\"message\":\"teapot\"}", "application/json"));
        var ex = await Assert.ThrowsAsync<ApiException>(() =>
            client.HtmlAsync(new HtmlRequest { Url = "https://example.com" }));
        ex.Status.Should().Be(418);
        ex.Message.Should().Contain("teapot");
    }

    [Fact]
    public async Task Non_json_body_falls_back_to_raw_text_as_message()
    {
        var client = Client(StubHandler.Returning(HttpStatusCode.BadRequest, "plain text error", "text/plain"));
        var ex = await Assert.ThrowsAsync<BadRequestException>(() =>
            client.HtmlAsync(new HtmlRequest { Url = "https://example.com" }));
        ex.Message.Should().Contain("plain text error");
    }
}
