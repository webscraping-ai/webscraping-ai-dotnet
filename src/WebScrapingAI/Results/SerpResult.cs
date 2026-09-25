using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace WebScrapingAI;

/// <summary>
/// Response from <c>GET /serp</c>. Field names follow the common SERP API
/// (SerpApi-family) naming. Fields the API may omit are nullable.
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
    /// <c>(page - 1) * 10 + Position</c>.
    /// </summary>
    [JsonPropertyName("position")]
    public int Position { get; init; }

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
}
