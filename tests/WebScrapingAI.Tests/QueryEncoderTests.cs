using System.Collections.Generic;
using FluentAssertions;
using WebScrapingAI.Internal;
using Xunit;

namespace WebScrapingAI.Tests;

public class QueryEncoderTests
{
    [Fact]
    public void Encodes_flat_string_pair()
    {
        new QueryEncoder().Set("url", "https://example.com").Encode()
            .Should().Be("url=https%3A%2F%2Fexample.com");
    }

    [Fact]
    public void Encodes_spaces_as_percent20_not_plus()
    {
        new QueryEncoder().Set("q", "a b c").Encode()
            .Should().Be("q=a%20b%20c");
    }

    [Fact]
    public void Encodes_booleans_as_lowercase_strings()
    {
        new QueryEncoder().Set("js", true).Set("error_on_404", false).Encode()
            .Should().Be("js=true&error_on_404=false");
    }

    [Fact]
    public void Encodes_integers_in_invariant_culture()
    {
        new QueryEncoder().Set("timeout", 10000).Encode()
            .Should().Be("timeout=10000");
    }

    [Fact]
    public void Drops_nulls_silently()
    {
        new QueryEncoder().Set("url", "https://example.com").Set("missing", null).Encode()
            .Should().Be("url=https%3A%2F%2Fexample.com");
    }

    [Fact]
    public void Drops_empty_strings()
    {
        new QueryEncoder().Set("url", "https://example.com").Set("empty", "").Encode()
            .Should().Be("url=https%3A%2F%2Fexample.com");
    }

    [Fact]
    public void Preserves_insertion_order()
    {
        new QueryEncoder()
            .Set("c", "3").Set("a", "1").Set("b", "2")
            .Encode()
            .Should().Be("c=3&a=1&b=2");
    }

    [Fact]
    public void Set_replaces_in_place_for_existing_key()
    {
        new QueryEncoder()
            .Set("a", "1").Set("b", "2").Set("a", "9")
            .Encode()
            .Should().Be("a=9&b=2");
    }

    [Fact]
    public void Encodes_dictionary_as_deepObject_with_sorted_subkeys()
    {
        var headers = new Dictionary<string, string>
        {
            ["Z-Header"] = "z-value",
            ["Cookie"] = "session=abc",
            ["A-Header"] = "a-value",
        };
        new QueryEncoder().Set("headers", headers).Encode()
            .Should().Be("headers%5BA-Header%5D=a-value&headers%5BCookie%5D=session%3Dabc&headers%5BZ-Header%5D=z-value");
    }

    [Fact]
    public void Encodes_string_list_as_repeated_key_without_brackets()
    {
        new QueryEncoder().Set("selectors", new List<string> { "h1", ".price", "#title" }).Encode()
            .Should().Be("selectors=h1&selectors=.price&selectors=%23title");
    }

    [Fact]
    public void Drops_empty_dictionary_silently()
    {
        new QueryEncoder().Set("url", "x").Set("headers", new Dictionary<string, string>()).Encode()
            .Should().Be("url=x");
    }

    [Fact]
    public void Drops_empty_list_silently()
    {
        new QueryEncoder().Set("url", "x").Set("selectors", new List<string>()).Encode()
            .Should().Be("url=x");
    }

    [Fact]
    public void Encodes_unicode_characters_correctly()
    {
        new QueryEncoder().Set("q", "café 北京").Encode()
            .Should().Be("q=caf%C3%A9%20%E5%8C%97%E4%BA%AC");
    }

    [Fact]
    public void Returns_empty_string_when_nothing_was_set()
    {
        new QueryEncoder().Encode().Should().Be(string.Empty);
    }

    [Fact]
    public void Mixed_encoding_styles_in_one_pass()
    {
        var encoder = new QueryEncoder()
            .Set("api_key", "secret")
            .Set("url", "https://example.com")
            .Set("headers", new Dictionary<string, string> { ["Cookie"] = "x" })
            .Set("selectors", new List<string> { "h1", "p" })
            .Set("js", true);

        encoder.Encode()
            .Should().Be("api_key=secret&url=https%3A%2F%2Fexample.com&headers%5BCookie%5D=x&selectors=h1&selectors=p&js=true");
    }
}
