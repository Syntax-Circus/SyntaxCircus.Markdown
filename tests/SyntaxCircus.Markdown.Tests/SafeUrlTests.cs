using System.Text.RegularExpressions;
using SyntaxCircus.Markdown.Tests.Support;

namespace SyntaxCircus.Markdown.Tests;

public partial class SafeUrlTests
{
    private static readonly string[] HandPicked =
    [
        "https://example.com", "HTTP://EXAMPLE.COM/a?b#c", "https://Example.com/Path/", "https://user@example.com:8443/p", "https://[::1]/x",
        "mailto:a@example.com", "MAILTO:a@example.com?subject=x", "/path", "path/to", "./p", "../p", "#frag", "?q=1", "a.html", "/a:b", "a/b:c",
        "?next=http://x", "#a:b", "/p%20q", "https://example.com/%7Euser", "tel:+1555", "ftp://x.example", "data:text/html,x", "file:///x", "blob:https://x/y",
        "about:blank", "javascript:alert(1)", "JAVASCRIPT:alert(1)", "jAvAsCrIpT:alert(1)", "vbscript:x", "java\tscript:alert(1)", "java\u0001script:x",
        "java​script:alert(1)", " javascript:alert(1)", " https://example.com", "https://example.com ", "my file", "%6Aavascript:alert(1)",
        "ｊavascript:alert(1)", "／example.com", "：javascript:x", "1http://x", ":x", "a b:c", "//host/path", "//", "///x", "\\\\host", "/\\host",
        "https:\\\\host", "https:/host", "https:host", "https://", "https:///path", "http://", "mailto:", "x:y", "ja\u0000vascript:x", "/\u007F", "\u0085",
        new string('a', 100_000), "https://" + new string('a', 100_000), "https://example.com/" + new string('a', 100_000), new string(' ', 100_000), "/" + new string(':', 10_000),
    ];

    [GeneratedRegex(@"\]\(<?([^)<>]*)>?\)")]
    private static partial Regex Destination();

    private static HashSet<string> Urls()
    {
        var urls = new HashSet<string>(HandPicked, StringComparer.Ordinal);
        foreach (var item in XssCorpusTests.CorpusItems)
        {
            foreach (Match m in Destination().Matches(item))
            {
                urls.Add(m.Groups[1].Value);
            }
        }

        return urls;
    }

    private static MarkdownOptions Configure(MarkdownProfile profile, Action<MarkdownOptions>? configure = null)
    {
        var options = MarkdownOptions.For(profile);
        options.AutoLinkBareUrls = false;
        configure?.Invoke(options);
        return options;
    }

    public static TheoryData<string> Configurations() => new() { "restricted", "article", "no-relative", "extra-schemes", "relative-only" };

    private static MarkdownOptions Options(string name) => name switch
    {
        "restricted" => Configure(MarkdownProfile.Restricted),
        "article" => Configure(MarkdownProfile.Article),
        "no-relative" => Configure(MarkdownProfile.Restricted, o => o.AllowRelativeUrls = false),
        "extra-schemes" => Configure(MarkdownProfile.Article, o => o.AllowedLinkSchemes.UnionWith(["tel", "ftp"])),
        "relative-only" => Configure(MarkdownProfile.Restricted, o => o.AllowedLinkSchemes.Clear()),
        _ => throw new ArgumentOutOfRangeException(nameof(name)),
    };

    /// <summary>Renders <c>[x](&lt;url&gt;)</c> and returns the href (null when the renderer dropped the link). Not expressible when Markdown would treat the characters specially.</summary>
    private static (bool Expressible, string? Href) RenderedHref(MarkdownRenderer renderer, string url)
    {
        if (url.Length > 5000 || url.IndexOfAny(['<', '>', '\n', '\r', '\\', '&']) >= 0)
        {
            return (false, null);
        }

        var body = HtmlAssert.ParseBody(renderer.Render($"[x](<{url}>)"));
        return (true, body.QuerySelector("a")?.GetAttribute("href"));
    }

    [Theory]
    [MemberData(nameof(Configurations))]
    public void Api_and_renderer_agree_on_every_url(string configuration)
    {
        var options = Options(configuration);
        var renderer = new MarkdownRenderer(options);
        var compared = 0;

        foreach (var url in Urls())
        {
            var api = SafeUrl.IsSafe(url, options);
            var (expressible, href) = RenderedHref(renderer, url);
            if (!expressible)
            {
                continue;
            }

            compared++;
            (href is not null).ShouldBe(api, $"[{configuration}] {url.Replace("\0", "\\0")}");
            if (api && url.All(c => char.IsAsciiLetterOrDigit(c) || "/:.?#=%@~_-+;,".Contains(c)))
            {
                href.ShouldBe(url, "a safe URL is written exactly as given");
            }
        }

        compared.ShouldBeGreaterThan(80);
    }

    [Fact]
    public void Entity_obfuscation_is_judged_after_decoding_by_the_renderer_and_as_given_by_the_api()
    {
        // Markdig decodes entities before the policy runs; the API sees the string the caller passes, so decoded forms are rejected.
        var renderer = new MarkdownRenderer(MarkdownProfile.Restricted);
        renderer.Render("[x](&#106;avascript:alert(1))").ShouldNotContain("<a");
        renderer.Render("[x](&#x6A;avascript:alert(1))").ShouldNotContain("<a");
        SafeUrl.IsSafe("javascript:alert(1)").ShouldBeFalse();
    }

    [Theory]
    [InlineData(MarkdownProfile.Restricted)]
    [InlineData(MarkdownProfile.Article)]
    public void Profile_overload_matches_the_profile_options(MarkdownProfile profile)
    {
        var options = MarkdownOptions.For(profile);
        foreach (var url in Urls())
        {
            SafeUrl.IsSafe(url, profile).ShouldBe(SafeUrl.IsSafe(url, options), url.Replace("\0", "\\0"));
        }
    }

    [Fact]
    public void Default_profile_is_Restricted_and_common_urls_behave()
    {
        SafeUrl.IsSafe("https://example.com").ShouldBeTrue();
        SafeUrl.IsSafe("mailto:a@example.com").ShouldBeTrue();
        SafeUrl.IsSafe("/x").ShouldBeTrue();
        SafeUrl.IsSafe("#x").ShouldBeTrue();
        SafeUrl.IsSafe("javascript:alert(1)").ShouldBeFalse();
        SafeUrl.IsSafe("//evil.example").ShouldBeFalse();
        SafeUrl.IsSafe(" https://example.com").ShouldBeFalse("callers trim first");
    }

    [Fact]
    public void Safe_means_unchanged_so_the_original_string_is_the_output()
    {
        // ContentLink.SafeUrl returned Uri.AbsoluteUri, which turns this into "https://example.com/Path". We do not normalize.
        const string url = "https://Example.com/Path/../Path";
        SafeUrl.IsSafe(url).ShouldBeTrue();
        new Uri(url).AbsoluteUri.ShouldNotBe(url);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Null_empty_and_blank_are_not_safe(string? url)
    {
        SafeUrl.IsSafe(url).ShouldBeFalse();
        SafeUrl.IsSafe(url, MarkdownProfile.Article).ShouldBeFalse();
        SafeUrl.IsSafe(url, MarkdownOptions.For(MarkdownProfile.Restricted)).ShouldBeFalse();
    }

    [Fact]
    public void Huge_and_garbage_input_never_throws()
    {
        var random = new Random(7);
        var options = MarkdownOptions.For(MarkdownProfile.Article);
        for (var i = 0; i < 2000; i++)
        {
            var chars = new char[random.Next(0, 80)];
            for (var j = 0; j < chars.Length; j++)
            {
                chars[j] = (char)random.Next(0, 0x10000); // includes lone surrogates
            }

            var url = new string(chars);
            Should.NotThrow(() => SafeUrl.IsSafe(url));
            Should.NotThrow(() => SafeUrl.IsSafe(url, options));
        }

        SafeUrl.IsSafe(new string('a', 5_000_000)).ShouldBeTrue("a long relative path is still a relative path");
        SafeUrl.IsSafe("https://" + new string('a', 5_000_000)).ShouldBeFalse();
        SafeUrl.IsSafe(new string('\0', 5_000_000)).ShouldBeFalse();
    }

    [Fact]
    public void Options_are_validated_like_the_renderer_validates_them()
    {
        Should.Throw<ArgumentNullException>(() => SafeUrl.IsSafe("/x", (MarkdownOptions)null!));
        Should.Throw<ArgumentOutOfRangeException>(() => SafeUrl.IsSafe("/x", (MarkdownProfile)99));

        var script = new MarkdownOptions();
        script.AllowedLinkSchemes.Add("javascript");
        Should.Throw<ArgumentException>(() => SafeUrl.IsSafe("/x", script));
        Should.Throw<ArgumentException>(() => new MarkdownRenderer(script));

        var vb = new MarkdownOptions();
        vb.AllowedLinkSchemes.Add("VBScript");
        Should.Throw<ArgumentException>(() => SafeUrl.IsSafe("/x", vb));

        var bad = new MarkdownOptions();
        bad.AllowedLinkSchemes.Add("ht tp:");
        Should.Throw<ArgumentException>(() => SafeUrl.IsSafe("/x", bad));

        Should.Throw<ArgumentOutOfRangeException>(() => SafeUrl.IsSafe("/x", new MarkdownOptions { MinimumHeadingLevel = 9 }));
    }

    [Fact]
    public void Options_change_the_verdict_and_are_not_retained()
    {
        var options = new MarkdownOptions();
        SafeUrl.IsSafe("tel:+1555", options).ShouldBeFalse();
        options.AllowedLinkSchemes.Add("tel");
        SafeUrl.IsSafe("tel:+1555", options).ShouldBeTrue();
        options.AllowedLinkSchemes.Remove("tel");
        SafeUrl.IsSafe("tel:+1555", options).ShouldBeFalse();

        options.AllowRelativeUrls = false;
        SafeUrl.IsSafe("/x", options).ShouldBeFalse();
        SafeUrl.IsSafe("/x", MarkdownProfile.Restricted).ShouldBeTrue("presets are unaffected by caller options");
    }

    [Fact]
    public void Is_thread_safe()
    {
        var urls = Urls().ToArray();
        var expected = urls.Select(u => SafeUrl.IsSafe(u)).ToArray();
        Parallel.For(0, 64, _ =>
        {
            for (var i = 0; i < urls.Length; i++)
            {
                SafeUrl.IsSafe(urls[i]).ShouldBe(expected[i]);
                SafeUrl.IsSafe(urls[i], MarkdownProfile.Article).ShouldBe(SafeUrl.IsSafe(urls[i], MarkdownOptions.For(MarkdownProfile.Article)));
            }
        });
    }
}
