using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WebScrapingAI;

namespace WebScrapingAI.Samples.Smoke;

/// <summary>
/// Hits the live WebScraping.AI API across all 8 endpoints. Page tools run with
/// js=false and proxy=datacenter, so a run costs ~31 credits: 4 page calls x 1
/// (html, text, selected, selected_multiple) + question/fields 2 x 6 + serp 15.
/// Each step asserts on the result shape, not just the absence of exceptions.
/// Run with: WEBSCRAPING_AI_API_KEY=... dotnet run --project samples/Smoke
/// </summary>
internal static class Program
{
    private const string TargetUrl = "https://example.com";
    private const string SerpQuery = "coffee machines";
    private const string DatacenterProxy = "datacenter";

    private static string? _apiKey;

    private static async Task<int> Main(string[] args)
    {
        var apiKey = Environment.GetEnvironmentVariable("WEBSCRAPING_AI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            Console.Error.WriteLine("WEBSCRAPING_AI_API_KEY env var must be set.");
            return 2;
        }

        _apiKey = apiKey;
        using var client = new WebScrapingAIClient(new WebScrapingAIClientOptions { ApiKey = apiKey });

        var failed = 0;

        failed += await Step("account", async () =>
        {
            var info = await client.AccountAsync();
            return $"email={info.Email} remaining={info.RemainingApiCalls}";
        });

        failed += await Step("html", async () =>
        {
            var html = await client.HtmlAsync(new HtmlRequest { Url = TargetUrl, Js = false, Proxy = DatacenterProxy });
            return Preview(RequireNonEmpty(html, "html"));
        });

        failed += await Step("text", async () =>
        {
            var text = await client.TextAsync(new TextRequest { Url = TargetUrl, TextFormat = "plain", Js = false, Proxy = DatacenterProxy });
            return Preview(RequireNonEmpty(text, "text"));
        });

        failed += await Step("selected", async () =>
        {
            var sel = await client.SelectedAsync(new SelectedRequest { Url = TargetUrl, Selector = "h1", Js = false, Proxy = DatacenterProxy });
            return Preview(RequireNonEmpty(sel, "selected"));
        });

        failed += await Step("selected_multiple", async () =>
        {
            var result = await client.SelectedMultipleAsync(new SelectedMultipleRequest
            {
                Url = TargetUrl,
                Js = false,
                Proxy = DatacenterProxy,
                Selectors = new[] { "h1", "p" },
            });
            // The API returns 200 [[]] for mis-encoded selectors, so require a match.
            if (!result.Results.Any(group => group.Count > 0))
                throw new SmokeAssertionException($"all selector groups empty ({result.Results.Count} group(s))");
            return $"{result.Results.Count} group(s)";
        });

        failed += await Step("question", async () =>
        {
            var answer = await client.QuestionAsync(new QuestionRequest
            {
                Url = TargetUrl,
                Js = false,
                Proxy = DatacenterProxy,
                Question = "What is this page about?",
            });
            return Preview(RequireNonEmpty(answer, "answer"));
        });

        failed += await Step("fields", async () =>
        {
            var fields = await client.FieldsAsync(new FieldsRequest
            {
                Url = TargetUrl,
                Js = false,
                Proxy = DatacenterProxy,
                Fields = new Dictionary<string, string>
                {
                    ["title"] = "Main page title",
                    ["description"] = "Page description",
                },
            });
            if (fields.Result is null) throw new SmokeAssertionException("fields response has no result");
            return string.Join(", ", FormatFields(fields.Result));
        });

        failed += await Step("serp", async () =>
        {
            var serp = await client.SerpAsync(new SerpRequest { Q = SerpQuery });
            if (serp.OrganicResults.Count == 0) throw new SmokeAssertionException("organic_results is empty");
            if (serp.SearchParameters.Q != SerpQuery)
                throw new SmokeAssertionException($"search_parameters.q was '{serp.SearchParameters.Q}', expected '{SerpQuery}'");
            var first = serp.OrganicResults.Count > 0 ? serp.OrganicResults[0].Title : "(none)";
            return $"{serp.OrganicResults.Count} result(s), first={Preview(first)}";
        });

        Console.WriteLine();
        Console.WriteLine(failed == 0 ? "All 8 endpoints OK." : $"{failed} endpoint(s) failed.");
        return failed == 0 ? 0 : 1;
    }

    /// <summary>Returns 0 on success, 1 on failure — caller sums into the failure count.</summary>
    private static async Task<int> Step(string name, Func<Task<string>> action)
    {
        try
        {
            var info = await action();
            Console.WriteLine($"ok   {name,-20} {info}");
            return 0;
        }
        catch (Exception ex)
        {
            // Catch everything (not only SDK errors) so the sweep continues.
            Console.WriteLine($"FAIL {name,-20} {Redact($"{ex.GetType().Name}: {ex.Message}")}");
            return 1;
        }
    }

    private static string RequireNonEmpty(string? value, string what)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new SmokeAssertionException($"{what} is empty");
        return value!;
    }

    /// <summary>Strips the API key (and any api_key=... pair) from anything we print.</summary>
    private static string Redact(string message)
    {
        if (!string.IsNullOrEmpty(_apiKey)) message = message.Replace(_apiKey, "[REDACTED]");
        return Regex.Replace(message, @"(api_key=)[^&\s""']*", "$1[REDACTED]", RegexOptions.IgnoreCase);
    }

    private static string Preview(string s)
    {
        if (string.IsNullOrEmpty(s)) return "(empty)";
        var oneLine = s.Replace('\n', ' ').Replace('\r', ' ');
        return oneLine.Length <= 80 ? oneLine : oneLine.Substring(0, 80) + "…";
    }

    private static IEnumerable<string> FormatFields(IReadOnlyDictionary<string, string?> fields)
    {
        foreach (var kv in fields)
        {
            yield return $"{kv.Key}={Preview(kv.Value ?? "(null)")}";
        }
    }
}

internal sealed class SmokeAssertionException : Exception
{
    public SmokeAssertionException(string message) : base(message) { }
}
