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
<PackageReference Include="WebScrapingAI" Version="4.3.0" />
```

## Quickstart

[Sign up](https://webscraping.ai/auth/sign_up) to get an API key — a free
trial, no credit card required. Your key lives in the
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

### Search engine results (SERP)

`SerpAsync` returns parsed Google results for a query. It is query-shaped, not
URL-shaped: `SerpRequest` takes `Q` (required), `Engine` (`google`, the
default), `Gl` (country), `Hl` (language, default `en`), `Page` (1–100, 10
results per page; the server rejects values above 100 with a 400), `Pages`,
and the location options `Location`, `Uule`, `Lat`/`Lon` and `Radius`
(meters, 1–1000).

Every search runs from one fixed location: `Location`, `Lat` + `Lon`, or
`Uule`, otherwise the capital of the `Gl` country, so repeats and later pages
stay consistent. `Gl` defaults to the location's country (otherwise `us`).
`Location` takes a name such as `Austin,Texas,United States` or `Austin, TX`,
or an id from `SerpLocationsAsync`; an unknown name is a 400 (not billed).
`Lat`/`Lon` are sent as plain decimals (at most 7 fractional digits) whatever
the current culture. `Pages` (1–5) fetches consecutive pages, starting at
`Page`, as one de-duplicated list: `Position` counts through the combined list
and each result gets a `Page`. Which location options can be combined and the
upper bounds are checked by the server (a 400, not billed).

A blank or whitespace-only `Q` throws `ArgumentException`; `Page`, `Pages` or
`Radius` below 1 and a NaN or infinite `Lat`/`Lon` throw
`ArgumentOutOfRangeException`, before any request is sent (the server also
rejects these with a 400, not billed; checking client-side saves the round
trip). The page-scraping options (`Js`, `Proxy`, `Country`, …) don't apply.
Billed per results page returned (see
[pricing](https://webscraping.ai/docs#serp)); failed searches are not charged.

```csharp
SerpResult serp = await client.SerpAsync(new SerpRequest
{
    Q = "coffee machines",
    Gl = "us",
    Hl = "en",
    Page = 1,
});

foreach (var r in serp.OrganicResults)
{
    Console.WriteLine($"{r.Position}. {r.Title} — {r.Link}");
}
Console.WriteLine(serp.SearchInformation.OrganicResultsState); // e.g. "Results for exact spelling"
Console.WriteLine(serp.Pagination.Next);                       // null on the last page
```

`Position` restarts at 1 on every page; the absolute rank is
`(page - 1) * 10 + Position`. Optional fields (`Snippet`, `Date`,
`ShowingResultsFor`, `TotalResults`, `RelatedSearches`, `Pagination.Next`) are
null when the API omits them.

Search from a location and fetch two pages in one call:

```csharp
SerpResult local = await client.SerpAsync(new SerpRequest
{
    Q = "coffee machines",
    Location = "Austin, TX",
    Pages = 2,
});

Console.WriteLine(local.SearchParameters.LocationUsed); // "Austin,Texas,United States"
Console.WriteLine(local.Pagination.PagesReturned);      // 2
foreach (var r in local.OrganicResults)
{
    Console.WriteLine($"{r.Position} (page {r.Page}). {r.Title}");
}
```

The location fields of `SearchParameters` (`Pages`, `LocationRequested`,
`LocationUsed`, `Lat`, `Lon`, `Radius`), `SerpOrganicResult.Page` and
`Pagination.PagesReturned` are null when the API omits them (`Pages`, `Page`
and `PagesReturned` appear only when `Pages` is above 1). `LocationUsed` is the
matched location's canonical name (or the `Gl` capital's when no location was
given) and is null for `Lat`/`Lon` searches; `Lat`/`Lon` are echoed on almost
every response.

#### Looking up locations

`SerpLocationsAsync` finds names for `Location`: up to 10 matches, most
populous first. It is free and needs no API key on the server. `Q` is part of a
location name (English names, e.g. `petersburg`; without it the most populous
locations come back) and `Limit` caps the count (1–10; below 1 throws
`ArgumentOutOfRangeException`). Pass a result's `CanonicalName` (or `Id`) as
`Location`.

```csharp
IReadOnlyList<SerpLocation> locations = await client.SerpLocationsAsync(new SerpLocationsRequest
{
    Q = "austin",
});

var austin = locations[0];
Console.WriteLine($"{austin.CanonicalName} ({austin.TargetType}, {austin.CountryCode})"); // Austin,Texas,United States (City, US)
Console.WriteLine($"lon={austin.Gps[0]} lat={austin.Gps[1]}");                           // gps is [longitude, latitude]

await client.SerpAsync(new SerpRequest { Q = "coffee machines", Location = austin.CanonicalName });
```

`Id` is a string, `GoogleId`, `GoogleParentId` and `Reach` are `long`
(`GoogleParentId` is null for countries), and `TargetType` is a plain string
(e.g. `City`, `State`, `Country`).

### Structured data for any web page

`DataAsync` returns structured JSON for any public web page. Pass the page's
normal URL; the server works out the site the page belongs to (`Provider`, e.g.
`toscrape.com`) and the kind of page (`Type`, e.g. `product`). Some popular
sites have dedicated parsers with fixed fields. For other sites, an AI model
learns the structure of each new kind of page once, and from then on pages of
that kind return data straight away for every user. **The SDK doesn't check
URLs itself**, so this works without an SDK upgrade. `DataRequest` takes:

- `Url` (required; a blank `Url` throws `ArgumentException`). Sent as-is.
- `Country`: two-letter country code of the proxy used to fetch the page, `us`
  by default.
- `Transcript`: video pages on sites with a dedicated parser only. Also fetch
  the video's transcript into `data.transcript`. It's null when no matching
  captions are available. If the transcript fetch itself fails, the whole
  request fails with a 500 and is not charged.
- `TranscriptLanguage`: with `Transcript = true`. Caption language to pick,
  e.g. `en` or `de`. Without it, English is preferred, then the first available
  track. If the video has no captions in that language, `data.transcript` is
  null.

`DataRequest` has no typed page-scraping options (`Headers`, `Timeout`, …).
There are no `js` or `proxy` options: `/data` renders pages with JavaScript
where the site needs it and always uses `proxy=auto`.
Priced per request, including results that come back `parse_failed` or
`not_found`. Failed fetches aren't charged. See
[pricing](https://webscraping.ai/docs#data).

```csharp
DataResult result = await client.DataAsync(new DataRequest
{
    Url = "https://books.toscrape.com/catalogue/a-light-in-the-attic_1000/index.html",
});

Console.WriteLine($"{result.RequestParameters.Provider}/{result.RequestParameters.Type}"); // toscrape.com/product
Console.WriteLine(result.ParseStatus);                                                      // ok, parse_failed or not_found
if (result.Data is { } data)                                                                // System.Text.Json.JsonElement
{
    Console.WriteLine(data.GetProperty("title").GetString());               // A Light in the Attic
    Console.WriteLine(data.GetProperty("price").GetDecimal());              // 51.77
}
```

`Data` is an untyped `JsonElement?` because its shape depends on the site and
the kind of page. Fields are snake_case, and fields a page doesn't expose are
null. `Data` itself is null when the API returns `null`, for example on
`parse_failed`. `Provider`, `Type` and `ParseStatus` are plain strings, so
values added later still come through. For options the SDK doesn't model yet,
use `ExtraParams`. Its entries are sent as-is, and entries
with a null or empty value are dropped. A key that belongs to a dedicated option
throws `ArgumentException`, whether or not that option is set: `api_key`, `url`,
`country`, `transcript` or `transcript_language`.

```csharp
await client.DataAsync(new DataRequest
{
    Url = "https://books.toscrape.com/catalogue/a-light-in-the-attic_1000/index.html",
    ExtraParams = new Dictionary<string, string> { ["some_option"] = "value" },
});
```

#### Structure learning (503) and no structured data (422)

On a site without a dedicated parser, the first request for a new kind of page
also learns the page's structure, so it can take up to about 45 seconds; keep
`WebScrapingAIClientOptions.Timeout` (60 seconds by default) above that. When
learning takes longer, or another request is already learning that site, the API
returns HTTP **503** with a body like
`{"message": "...", "error_code": "structure_processing", "retry_after": 30}`
(and a `Retry-After` header). It isn't charged. It's thrown as the base
`ApiException` with `Status == 503` (the API's message is in `Message`, the raw
JSON, including `error_code` and `retry_after`, in `ResponseBody`). Retry after
`retry_after` seconds; the retry can return data (if learning failed
meanwhile, it may learn again or return the 422 below):

```csharp
var request = new DataRequest { Url = url };
DataResult result;
try
{
    result = await client.DataAsync(request);
}
catch (ApiException ex) when (ex.Status == 503) // structure_processing
{
    using var body = JsonDocument.Parse(ex.ResponseBody!);
    await Task.Delay(TimeSpan.FromSeconds(body.RootElement.GetProperty("retry_after").GetInt32()));
    result = await client.DataAsync(request);
}
```

If no structured data can be found on the page (an empty page, a login wall),
the API returns HTTP **422** with `error_code` `structure_unavailable`, not
charged. It's thrown as the base `ApiException` with `Status == 422` (the API's
message is in `Message`, the raw JSON in `ResponseBody`). Use `FieldsAsync` to
name the values you want instead.

Redirects within the same site (http→https, `www`, trailing slash) are
followed; a redirect to another site throws a `ServerException` (HTTP 500,
`error_code` `target_redirect`, the target in the message), not charged.
Starting too many new kinds of pages in an hour throws a `RateLimitException`
(HTTP 429, `error_code` `rate_limited`).

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
| `SerpAsync(SerpRequest)` | `GET /serp` | `SerpResult` |
| `SerpLocationsAsync(SerpLocationsRequest?)` | `GET /serp/locations` | `IReadOnlyList<SerpLocation>` |
| `DataAsync(DataRequest)` | `GET /data` | `DataResult` |
| `AccountAsync()` | `GET /account` | `AccountInfo` |

### Common request options

Every request type except `SerpRequest`, `SerpLocationsRequest` and `DataRequest` extends `CommonRequest`, which exposes shared options like `Js`, `Country`, `Proxy`, `Timeout`, `WaitFor`, `Headers`, `Device`, `JsScript`, etc. See [the API reference](https://webscraping.ai/docs/api) for the full list.

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
| `ApiException` (base) | Any other non-2xx (e.g. 503 `structure_processing` or 422 `structure_unavailable` from `DataAsync`) |
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
    UserAgent = "webscraping-ai-dotnet/4.3.0",               // overridable
    HttpHandler = null,                                      // plug in a custom HttpMessageHandler
};
```

## Targets

- `netstandard2.0` — covers .NET Framework 4.6.1+, Mono, Xamarin, Unity, every modern .NET.
- `net8.0` — current LTS; enables nullable annotations and modern BCL features at the call site.

## Smoke test

The `samples/Smoke` console app exercises all 10 endpoints against the live API and asserts on each result (e.g. non-empty organic results with the expected query, a two-page `Austin, TX` search whose `location_used` contains `Austin`, a location lookup whose first `canonical_name` contains `Austin`, at least one selector match, a `/data` result for a page on a site with a dedicated parser with `parse_status` `ok` and a title, and a `/data` call on `https://example.com/` that must return a success envelope, a 503 `structure_processing` or a 422 `structure_unavailable`). Page tools run with `Js = false` and `Proxy = "datacenter"` to keep the cost down, but a run uses real credits (see [pricing](https://webscraping.ai/docs); the two-page search is billed per page returned, the location lookup is free, and the any-site check isn't charged when it returns a 503 or 422). It prints a `FAIL` line per failing step (with the API key redacted) and exits non-zero if any step failed.

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
