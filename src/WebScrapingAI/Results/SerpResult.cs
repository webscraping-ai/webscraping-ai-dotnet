using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WebScrapingAI;

/// <summary>
/// Response from <c>GET /serp</c>. Field names follow the common SERP API
/// naming. Fields the API may omit are nullable.
/// </summary>
public sealed class SerpResult
{
    /// <summary>The normalized parameters the search was run with.</summary>
    [JsonPropertyName("search_parameters")]
    public SerpSearchParameters SearchParameters { get; init; } = new();

    /// <summary>What the engine reported about the search itself.</summary>
    [JsonPropertyName("search_information")]
    public SerpSearchInformation SearchInformation { get; init; } = new();

    /// <summary>Organic (non-ad) results, in rank order. Empty when the page had none.</summary>
    [JsonPropertyName("organic_results")]
    public IReadOnlyList<SerpOrganicResult> OrganicResults { get; init; } = System.Array.Empty<SerpOrganicResult>();

    /// <summary>"Related searches" suggestions. Null when the page shows none.</summary>
    [JsonPropertyName("related_searches")]
    public IReadOnlyList<SerpRelatedSearch>? RelatedSearches { get; init; }

    [JsonPropertyName("pagination")]
    public SerpPagination Pagination { get; init; } = new();
}

/// <summary>The <c>search_parameters</c> block of <see cref="SerpResult"/>.</summary>
public sealed class SerpSearchParameters
{
    [JsonPropertyName("engine")]
    public string Engine { get; init; } = string.Empty;

    [JsonPropertyName("q")]
    public string Q { get; init; } = string.Empty;

    [JsonPropertyName("gl")]
    public string Gl { get; init; } = string.Empty;

    [JsonPropertyName("hl")]
    public string Hl { get; init; } = string.Empty;

    [JsonPropertyName("page")]
    public int Page { get; init; }

    /// <summary>The number of pages requested, present only when <see cref="SerpRequest.Pages"/> was above 1.</summary>
    [JsonPropertyName("pages")]
    public int? Pages { get; init; }

    /// <summary>The <c>location</c> or <c>uule</c> as sent, when one was given.</summary>
    [JsonPropertyName("location_requested")]
    public string? LocationRequested { get; init; }

    /// <summary>
    /// Canonical name of the matched location, or of the <c>gl</c> country's capital when no location
    /// was given. Omitted (null) for <c>lat</c>/<c>lon</c> and coordinate-<c>uule</c> searches.
    /// </summary>
    [JsonPropertyName("location_used")]
    public string? LocationUsed { get; init; }

    /// <summary>
    /// Latitude the search ran from: the given <c>lat</c>, or that of the location, the <c>uule</c> or the
    /// <c>gl</c> country's capital. Null only when <c>gl</c> is outside the location list.
    /// </summary>
    [JsonPropertyName("lat")]
    public double? Lat { get; init; }

    /// <summary>
    /// Longitude the search ran from: the given <c>lon</c>, or that of the location, the <c>uule</c> or the
    /// <c>gl</c> country's capital. Null only when <c>gl</c> is outside the location list.
    /// </summary>
    [JsonPropertyName("lon")]
    public double? Lon { get; init; }

    /// <summary>The radius in meters, when one was given.</summary>
    [JsonPropertyName("radius")]
    public int? Radius { get; init; }
}

/// <summary>The <c>search_information</c> block of <see cref="SerpResult"/>.</summary>
public sealed class SerpSearchInformation
{
    /// <summary>The query the results are for. Equals <c>q</c> unless a spelling fix was applied.</summary>
    [JsonPropertyName("query_displayed")]
    public string QueryDisplayed { get; init; } = string.Empty;

    /// <summary>
    /// <c>Results for exact spelling</c>, <c>Empty showing fixed spelling results</c>
    /// (see <see cref="ShowingResultsFor"/>), or <c>Fully empty</c>.
    /// </summary>
    [JsonPropertyName("organic_results_state")]
    public string OrganicResultsState { get; init; } = string.Empty;

    /// <summary>The auto-corrected query, present only when a spelling fix was applied.</summary>
    [JsonPropertyName("showing_results_for")]
    public string? ShowingResultsFor { get; init; }

    /// <summary>Estimated total result count, present only when the upstream page reports it.</summary>
    [JsonPropertyName("total_results")]
    public long? TotalResults { get; init; }
}

/// <summary>One entry of <see cref="SerpResult.OrganicResults"/>.</summary>
public sealed class SerpOrganicResult
{
    /// <summary>
    /// Rank within this page, starting at 1 on every page. Absolute rank is
    /// <c>(page - 1) * 10 + Position</c>. With <see cref="SerpRequest.Pages"/> above 1,
    /// it counts through the combined de-duplicated list (1..N) instead.
    /// </summary>
    [JsonPropertyName("position")]
    public int Position { get; init; }

    /// <summary>The results page this entry came from, present only when <see cref="SerpRequest.Pages"/> was above 1.</summary>
    [JsonPropertyName("page")]
    public int? Page { get; init; }

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("link")]
    public string Link { get; init; } = string.Empty;

    /// <summary>Hostname of <see cref="Link"/> without a leading <c>www.</c>.</summary>
    [JsonPropertyName("domain")]
    public string Domain { get; init; } = string.Empty;

    /// <summary>The breadcrumb-style URL shown under the title. Falls back to <see cref="Domain"/>.</summary>
    [JsonPropertyName("displayed_link")]
    public string DisplayedLink { get; init; } = string.Empty;

    /// <summary>Result description snippet, when shown.</summary>
    [JsonPropertyName("snippet")]
    public string? Snippet { get; init; }

    /// <summary>The date shown next to the snippet, as displayed (absolute or relative), when present.</summary>
    [JsonPropertyName("date")]
    public string? Date { get; init; }
}

/// <summary>One entry of <see cref="SerpResult.RelatedSearches"/>.</summary>
public sealed class SerpRelatedSearch
{
    [JsonPropertyName("query")]
    public string Query { get; init; } = string.Empty;
}

/// <summary>The <c>pagination</c> block of <see cref="SerpResult"/>.</summary>
public sealed class SerpPagination
{
    [JsonPropertyName("current")]
    public int Current { get; init; }

    /// <summary>The next page number. Null when no further page is offered.</summary>
    [JsonPropertyName("next")]
    public int? Next { get; init; }

    /// <summary>
    /// How many pages came back, present only when <see cref="SerpRequest.Pages"/> was above 1. Can be
    /// fewer than requested when Google has no further page or a later page couldn't be fetched (not billed).
    /// </summary>
    [JsonPropertyName("pages_returned")]
    public int? PagesReturned { get; init; }
}
