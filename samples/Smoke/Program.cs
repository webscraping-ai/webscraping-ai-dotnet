using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebScrapingAI;

namespace WebScrapingAI.Samples.Smoke;

/// <summary>
/// Hits the live WebScraping.AI API across all 7 endpoints. Costs ~17 credits.
/// Run with: WEBSCRAPING_AI_API_KEY=... dotnet run --project samples/Smoke
/// </summary>
internal static class Program
{
    private const string TargetUrl = "https://example.com";

    private static async Task<int> Main(string[] args)
    {
        var apiKey = Environment.GetEnvironmentVariable("WEBSCRAPING_AI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            Console.Error.WriteLine("WEBSCRAPING_AI_API_KEY env var must be set.");
            return 2;
        }

        using var client = new WebScrapingAIClient(new WebScrapingAIClientOptions { ApiKey = apiKey });

        var failed = 0;

        await Step("account", async () =>
        {
            var info = await client.AccountAsync();
            return $"email={info.Email} remaining={info.RemainingApiCalls}";
        }, ref failed);

        await Step("html", async () =>
        {
            var html = await client.HtmlAsync(new HtmlRequest { Url = TargetUrl });
            return Preview(html);
        }, ref failed);

        await Step("text", async () =>
        {
            var text = await client.TextAsync(new TextRequest { Url = TargetUrl, TextFormat = "plain" });
            return Preview(text);
        }, ref failed);

        await Step("selected", async () =>
        {
            var sel = await client.SelectedAsync(new SelectedRequest { Url = TargetUrl, Selector = "h1" });
            return Preview(sel);
        }, ref failed);

        await Step("selected_multiple", async () =>
        {
            var result = await client.SelectedMultipleAsync(new SelectedMultipleRequest
            {
                Url = TargetUrl,
                Selectors = new[] { "h1", "p" },
            });
            return $"{result.Results.Count} group(s)";
        }, ref failed);

        await Step("question", async () =>
        {
            var answer = await client.QuestionAsync(new QuestionRequest
            {
                Url = TargetUrl,
                Question = "What is this page about?",
            });
            return Preview(answer);
        }, ref failed);

        await Step("fields", async () =>
        {
            var fields = await client.FieldsAsync(new FieldsRequest
            {
                Url = TargetUrl,
                Fields = new Dictionary<string, string>
                {
                    ["title"] = "Main page title",
                    ["description"] = "Page description",
                },
            });
            return fields.Result is null ? "(no result)" : string.Join(", ", FormatFields(fields.Result));
        }, ref failed);

        Console.WriteLine();
        Console.WriteLine(failed == 0 ? "All 7 endpoints OK." : $"{failed} endpoint(s) failed.");
        return failed == 0 ? 0 : 1;
    }

    private static async Task Step(string name, Func<Task<string>> action, ref int failed)
    {
        try
        {
            var info = await action();
            Console.WriteLine($"ok   {name,-20} {info}");
        }
        catch (Exception ex)
        {
            failed++;
            Console.WriteLine($"fail {name,-20} {ex.GetType().Name}: {ex.Message}");
        }
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
