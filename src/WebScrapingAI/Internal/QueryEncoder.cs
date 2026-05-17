using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebScrapingAI.Internal;

/// <summary>
/// Builds query strings for the WebScraping.AI API. The API mixes three encoding
/// styles that no single off-the-shelf encoder gets right in combination:
/// <list type="bullet">
///   <item><c>headers</c> and <c>fields</c> → deepObject + explode (<c>headers[Cookie]=foo</c>)</item>
///   <item><c>selectors</c> → form + explode <b>without</b> brackets (<c>selectors=h1&amp;selectors=.price</c>)</item>
///   <item>Everything else → flat <c>key=value</c></item>
/// </list>
/// Booleans serialize as the strings <c>"true"</c>/<c>"false"</c>, nulls are
/// dropped at every level, and spaces are encoded as <c>%20</c> (via
/// <see cref="Uri.EscapeDataString(string)"/>).
/// </summary>
internal sealed class QueryEncoder
{
    /// <summary>
    /// Ordered pairs preserved insertion order. <c>Put</c> replaces in place
    /// when the key already exists, so api_key prepending keeps its slot.
    /// </summary>
    private readonly List<KeyValuePair<string, object?>> _pairs = new();

    /// <summary>Sets or replaces <paramref name="key"/> in place.</summary>
    public QueryEncoder Set(string key, object? value)
    {
        if (key is null) throw new ArgumentNullException(nameof(key));
        if (value is null) return this;

        for (var i = 0; i < _pairs.Count; i++)
        {
            if (_pairs[i].Key == key)
            {
                _pairs[i] = new KeyValuePair<string, object?>(key, value);
                return this;
            }
        }
        _pairs.Add(new KeyValuePair<string, object?>(key, value));
        return this;
    }

    public string Encode()
    {
        var parts = new List<string>();
        foreach (var pair in _pairs)
        {
            EmitValue(pair.Key, pair.Value, parts);
        }
        return string.Join("&", parts);
    }

    private static void EmitValue(string key, object? value, List<string> parts)
    {
        switch (value)
        {
            case null:
                return;

            case IReadOnlyDictionary<string, string> dict:
                EmitDictionary(key, dict, parts);
                return;

            case IEnumerable<string> strings:
                EmitListNoBrackets(key, strings, parts);
                return;

            case bool b:
                parts.Add(Encode(key) + "=" + (b ? "true" : "false"));
                return;

            case string s:
                if (s.Length == 0) return;
                parts.Add(Encode(key) + "=" + Encode(s));
                return;

            default:
                // Numbers (int, long, double, etc.) and any other primitive
                // stringify cleanly with InvariantCulture.
                var formatted = ToInvariantString(value);
                if (formatted.Length == 0) return;
                parts.Add(Encode(key) + "=" + Encode(formatted));
                return;
        }
    }

    private static void EmitDictionary(string key, IReadOnlyDictionary<string, string> entries, List<string> parts)
    {
        // Sort sub-keys for deterministic wire output. HTTP doesn't care but
        // humans and tests do.
        var ordered = entries
            .Where(kv => kv.Key != null && kv.Value != null)
            .OrderBy(kv => kv.Key, StringComparer.Ordinal);

        foreach (var kv in ordered)
        {
            parts.Add(Encode(key) + "%5B" + Encode(kv.Key) + "%5D=" + Encode(kv.Value));
        }
    }

    private static void EmitListNoBrackets(string key, IEnumerable<string> values, List<string> parts)
    {
        foreach (var v in values)
        {
            if (v is null) continue;
            parts.Add(Encode(key) + "=" + Encode(v));
        }
    }

    private static string Encode(string s) => Uri.EscapeDataString(s);

    private static string ToInvariantString(object value)
    {
        return value switch
        {
            IFormattable f => f.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };
    }
}
