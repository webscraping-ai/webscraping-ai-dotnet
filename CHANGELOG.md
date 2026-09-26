# Changelog

All notable changes to this project will be documented in this file. The format follows [Keep a Changelog](https://keepachangelog.com/) and the project adheres to [Semantic Versioning](https://semver.org/).

## [Unreleased]
### Changed

- Docs: stop stating credit prices (they're set server-side and change); link to https://webscraping.ai/docs pricing instead.

## [4.2.0] — 2026-09-25
### Added

- `DataAsync(DataRequest)` for the new `GET /data` endpoint. It returns structured JSON for a page on a supported site, e.g. YouTube, TikTok, X, LinkedIn, Instagram or Reddit. `DataRequest` takes `Url` (required), `Country`, `Transcript` and `TranscriptLanguage`, plus `ExtraParams` for provider-specific options sent as-is. Like `SerpRequest`, it doesn't extend `CommonRequest`. The result is a `DataResult` with `RequestParameters` (`Url`, `Provider`, `Type`), `ParseStatus` and an untyped `Data` (`JsonElement?`, null on `parse_failed`/`not_found`). `Provider`, `Type` and `ParseStatus` are plain strings. Each request costs 15 credits.
- `DataAsync` checks only that `Url` isn't blank. It never checks the URL against a site list: sites are added on the server, and an unsupported URL or page type returns a 400 (`BadRequestException`) that is not charged. Its message lists what is supported. An `ExtraParams` key that belongs to a dedicated option (`api_key`, `url`, `country`, `transcript`, `transcript_language`) throws `ArgumentException`, whether or not that option is set. `ExtraParams` entries with a null or empty value are dropped.
- The smoke sample (`samples/Smoke`) now also checks `/data`. It asserts a YouTube video returns `parse_status` `ok`, provider `youtube` and a non-empty `title`, and that the server rejects `https://example.com/` with a 400 whose message contains `Unsupported URL`.

## [4.1.0] — 2026-09-25

### Added

- `SerpAsync(SerpRequest)` for the new `GET /serp` endpoint — parsed search engine (Google) results for a query. `SerpRequest` takes `Q` (required), `Engine`, `Gl`, `Hl` and `Page`; it does not extend `CommonRequest` since the page-scraping options don't apply. Returns a typed `SerpResult` (`SearchParameters`, `SearchInformation`, `OrganicResults`, `RelatedSearches`, `Pagination`), with optional fields nullable. Flat 15 credits per search.
- `SerpAsync` validates input before any request: a null, empty or whitespace-only `Q` throws `ArgumentException` (`Q` is otherwise sent untrimmed), and `Page < 1` throws `ArgumentOutOfRangeException` — the server also rejects it with a 400 (not billed), so checking client-side saves the round trip. Pages are 1–100: the server rejects a `Page` above 100 with a 400.

### Fixed

- Smoke sample (`samples/Smoke`) now asserts on results instead of only catching exceptions: SERP requires non-empty `organic_results` and `search_parameters.q == "coffee machines"`, `selected_multiple` requires at least one non-empty group, text endpoints fail on empty output and `fields` fails without a `result`. Page tools run with `js=false` and `proxy=datacenter` so the documented ~31-credit cost holds, and FAIL lines redact the API key.

## [4.0.2] — 2026-07-17

### Changed

- Documentation: expanded README — API docs, signup/dashboard links, badges, and links to the other official clients.

## [4.0.1] — 2026-06-21

### Fixed

- `SelectedAsync` and `SelectedMultipleAsync` no longer require a selector/selectors; omitting them returns whole-page HTML, matching the API. `SelectedRequest.Selector` and `SelectedMultipleRequest.Selectors` are now optional.

## [4.0.0] — 2026-05-17

Initial release of the hand-authored .NET SDK. Version `4.0.0` aligns with the rest of the WebScraping.AI SDK family (Ruby, Python, PHP, JS, Go, Java).

### Added

- `WebScrapingAIClient` with 7 async methods covering the full API surface:
  `AccountAsync`, `HtmlAsync`, `TextAsync`, `SelectedAsync`, `SelectedMultipleAsync`,
  `QuestionAsync`, `FieldsAsync`.
- Per-endpoint request records with `init`-only properties:
  `HtmlRequest`, `TextRequest`, `SelectedRequest`, `SelectedMultipleRequest`,
  `QuestionRequest`, `FieldsRequest`, all extending `CommonRequest`.
- Typed result records: `AccountInfo`, `FieldsResult`, `SelectedMultipleResult`.
- Typed exception hierarchy: `WebScrapingAIException` (base), `ApiException`
  with 6 status-specific subclasses, plus transport-level `ApiTimeoutException`
  and `ApiConnectionException`.
- Multi-targets `netstandard2.0` and `net8.0`. Zero runtime dependencies on
  net8.0; on netstandard2.0 only `System.Text.Json` is pulled in.
- Custom `HttpMessageHandler` injection point for testing or wrapping behavior.
- Configurable per-request timeout, base URL, and user agent.
- Reads `WEBSCRAPING_AI_API_KEY` from the environment when `ApiKey` is omitted.
- ~54 unit tests across query encoding, exception mapping, endpoint wire shape,
  error envelope parsing, transport (timeout/cancellation/connection),
  and required-arg validation.
- Live smoke sample at `samples/Smoke` exercising every endpoint.
- CI matrix: .NET 8 / 10 on Ubuntu, .NET 10 on macOS and Windows.

[4.0.0]: https://github.com/webscraping-ai/webscraping-ai-dotnet/releases/tag/v4.0.0
