# WebScraping.AI .NET Client

[![NuGet](https://img.shields.io/nuget/v/WebScrapingAI.svg)](https://www.nuget.org/packages/WebScrapingAI/)
[![CI](https://github.com/webscraping-ai/webscraping-ai-dotnet/actions/workflows/ci.yml/badge.svg)](https://github.com/webscraping-ai/webscraping-ai-dotnet/actions/workflows/ci.yml)

Official .NET client for the [WebScraping.AI](https://webscraping.ai) API —
web scraping with Chromium JavaScript rendering, rotating
datacenter/residential/stealth proxies, and AI-powered question answering and
structured field extraction on any page. Async, zero runtime dependencies,
multi-targets `netstandard2.0` and `net8.0`. See the
[API documentation](https://webscraping.ai/docs) for the full parameter reference.

## Install

```sh
dotnet add package WebScrapingAI
```

Or in your `.csproj`:

```xml
<PackageReference Include="WebScrapingAI" Version="4.0.0" />
```

## Quickstart

[Sign up](https://webscraping.ai/auth/sign_up) to get an API key — the free
trial includes 2,000 credits, no credit card required. Your key lives in the
[dashboard](https://webscraping.ai/dashboard).

```csharp
using WebScrapingAI;

using var client = new WebScrapingAIClient(new WebScrapingAIClientOptions
{
    ApiKey = "YOUR_API_KEY",
});

// Or read from the WEBSCRAPING_AI_API_KEY environment variable:
using var client2 = new WebScrapingAIClient();

// Get the HTML of a page
string html = await client.HtmlAsync(new HtmlRequest
{
    Url = "https://example.com",
    Js = true,
});

// Ask a question about a page
string answer = await client.QuestionAsync(new QuestionRequest
{
    Url = "https://example.com",
    Question = "What is this page about?",
});

// Extract structured fields
FieldsResult result = await client.FieldsAsync(new FieldsRequest
{
    Url = "https://example.com/product/123",
    Fields = new Dictionary<string, string>
    {
        ["title"] = "Main product title",
        ["price"] = "Current price",
    },
});
Console.WriteLine(result.Result?["price"]);
```

## API

All methods are async and accept an optional `CancellationToken`.

| Method | Endpoint | Returns |
| --- | --- | --- |
| `HtmlAsync(HtmlRequest)` | `GET /html` | `string` |
| `TextAsync(TextRequest)` | `GET /text` | `string` |
| `SelectedAsync(SelectedRequest)` | `GET /selected` | `string` |
| `SelectedMultipleAsync(SelectedMultipleRequest)` | `GET /selected-multiple` | `SelectedMultipleResult` |
| `QuestionAsync(QuestionRequest)` | `GET /ai/question` | `string` |
| `FieldsAsync(FieldsRequest)` | `GET /ai/fields` | `FieldsResult` |
| `AccountAsync()` | `GET /account` | `AccountInfo` |

### Common request options

Every request type extends `CommonRequest`, which exposes shared options like `Js`, `Country`, `Proxy`, `Timeout`, `WaitFor`, `Headers`, `Device`, `JsScript`, etc. See [the API reference](https://webscraping.ai/docs/api) for the full list.

### Errors

The SDK throws unchecked exceptions:

| Exception | When |
| --- | --- |
| `BadRequestException` | HTTP 400 |
| `PaymentRequiredException` | HTTP 402 — out of credits |
| `AuthenticationException` | HTTP 403 — bad API key |
| `RateLimitException` | HTTP 429 |
| `ServerException` | HTTP 500 |
| `GatewayTimeoutException` | HTTP 504 |
| `ApiException` (base) | Any other non-2xx |
| `ApiTimeoutException` | Per-request timeout exceeded |
| `ApiConnectionException` | Transport/connection failure |

All of the above extend `WebScrapingAIException` (a `RuntimeException`). The transport-level pair (`ApiTimeoutException`, `ApiConnectionException`) do **not** extend `ApiException`, so `catch (ApiException)` reliably means "we got an HTTP response back".

```csharp
try
{
    var html = await client.HtmlAsync(new HtmlRequest { Url = "..." });
}
catch (RateLimitException ex)
{
    // back off
}
catch (ApiException ex)
{
    // any other non-2xx; ex.Status has the HTTP code
}
catch (WebScrapingAIException ex)
{
    // transport failure (timeout or connection)
}
```

### Response-shape notes

Two endpoints return shapes that differ slightly from the OpenAPI spec examples; the SDK preserves them verbatim so the wire layout stays observable:

- **`FieldsAsync`** returns `FieldsResult` whose `.Result` holds the extracted fields under the API's `result` key.
- **`SelectedMultipleAsync`** returns `SelectedMultipleResult` whose `.Results` is `IReadOnlyList<IReadOnlyList<string>>` (the API wraps matches in an outer array).

## Configuration

```csharp
new WebScrapingAIClientOptions
{
    ApiKey = "...",                                          // or WEBSCRAPING_AI_API_KEY env var
    BaseUrl = "https://api.webscraping.ai",                  // override for staging/test
    Timeout = TimeSpan.FromSeconds(60),                      // per-request
    UserAgent = "webscraping-ai-dotnet/4.0.0",               // overridable
    HttpHandler = null,                                      // plug in a custom HttpMessageHandler
};
```

## Targets

- `netstandard2.0` — covers .NET Framework 4.6.1+, Mono, Xamarin, Unity, every modern .NET.
- `net8.0` — current LTS; enables nullable annotations and modern BCL features at the call site.

## Smoke test

The `samples/Smoke` console app exercises all 7 endpoints against the live API. Costs ~17 credits per run.

```sh
WEBSCRAPING_AI_API_KEY=... dotnet run --project samples/Smoke
```

## Contributing

```sh
dotnet build
dotnet test
dotnet format
```

The repo pins .NET 10 via `mise.toml`; if you use `mise`, `cd` into the repo and run `mise install` to pick it up.

## Links

- [WebScraping.AI](https://webscraping.ai) — features, pricing, signup
- [API documentation](https://webscraping.ai/docs)
- [Dashboard](https://webscraping.ai/dashboard) — API key, usage, request builder
- Other official clients: [Python](https://github.com/webscraping-ai/webscraping-ai-python) · [JavaScript](https://github.com/webscraping-ai/webscraping-ai-js) · [Ruby](https://github.com/webscraping-ai/webscraping-ai-ruby) · [PHP](https://github.com/webscraping-ai/webscraping-ai-php) · [Go](https://github.com/webscraping-ai/webscraping-ai-go) · [Java](https://github.com/webscraping-ai/webscraping-ai-java) · [CLI](https://github.com/webscraping-ai/webscraping-ai-cli) · [MCP server](https://github.com/webscraping-ai/webscraping-ai-mcp-server) · [n8n node](https://github.com/webscraping-ai/webscraping-ai-n8n)
- Support: [support@webscraping.ai](mailto:support@webscraping.ai)

## License

MIT — see [LICENSE](LICENSE).
