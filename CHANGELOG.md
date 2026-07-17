# Changelog

All notable changes to this project will be documented in this file. The format follows [Keep a Changelog](https://keepachangelog.com/) and the project adheres to [Semantic Versioning](https://semver.org/).

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
