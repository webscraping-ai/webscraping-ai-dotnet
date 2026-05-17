using FluentAssertions;
using Xunit;

namespace WebScrapingAI.Tests;

public class ExceptionHierarchyTests
{
    [Theory]
    [InlineData(400, typeof(BadRequestException))]
    [InlineData(402, typeof(PaymentRequiredException))]
    [InlineData(403, typeof(AuthenticationException))]
    [InlineData(429, typeof(RateLimitException))]
    [InlineData(500, typeof(ServerException))]
    [InlineData(504, typeof(GatewayTimeoutException))]
    public void ForStatus_returns_correct_subclass(int status, System.Type expected)
    {
        var ex = ApiException.ForStatus(status, "boom");
        ex.Should().BeOfType(expected);
        ex.Status.Should().Be(status);
    }

    [Fact]
    public void ForStatus_falls_back_to_base_for_unmapped_status()
    {
        var ex = ApiException.ForStatus(418, "I'm a teapot");
        ex.Should().BeOfType<ApiException>();
        ex.Status.Should().Be(418);
        ex.Message.Should().Contain("418").And.Contain("teapot");
    }

    [Fact]
    public void ApiException_subclasses_extend_WebScrapingAIException()
    {
        ApiException.ForStatus(429, "x").Should().BeAssignableTo<WebScrapingAIException>();
    }

    [Fact]
    public void ApiTimeoutException_extends_base_but_not_ApiException()
    {
        var ex = new ApiTimeoutException("timed out");
        ex.Should().BeAssignableTo<WebScrapingAIException>();
        ex.Should().NotBeAssignableTo<ApiException>();
    }

    [Fact]
    public void ApiConnectionException_extends_base_but_not_ApiException()
    {
        var ex = new ApiConnectionException("refused");
        ex.Should().BeAssignableTo<WebScrapingAIException>();
        ex.Should().NotBeAssignableTo<ApiException>();
    }

    [Fact]
    public void ApiException_carries_envelope_fields()
    {
        var ex = ApiException.ForStatus(500, "Target page error", apiStatusCode: 503, apiStatusMessage: "Service Unavailable", apiBody: "<html>down</html>", responseBody: "{\"message\":\"...\"}");
        ex.ApiStatusCode.Should().Be(503);
        ex.ApiStatusMessage.Should().Be("Service Unavailable");
        ex.ApiBody.Should().Be("<html>down</html>");
        ex.ResponseBody.Should().Contain("message");
    }

    [Fact]
    public void Message_includes_http_status()
    {
        var ex = ApiException.ForStatus(429, "Too Many Requests");
        ex.Message.Should().Contain("429").And.Contain("Too Many Requests");
    }

    [Fact]
    public void Message_falls_back_when_envelope_message_is_empty()
    {
        var ex = ApiException.ForStatus(500, "");
        ex.Message.Should().Contain("500");
    }
}
