using SyntaxCircus.Markdown.Internal;

namespace SyntaxCircus.Markdown.Tests;

public class UrlPolicyTests
{
    private static readonly UrlPolicy Policy = new(new HashSet<string>(["http", "https", "mailto"]), allowRelative: true);

    [Theory]
    [InlineData("https://example.com", "https")]
    [InlineData("HTTP://EXAMPLE.COM/a?b#c", "http")]
    [InlineData("https://user@example.com:8443/p", "https")]
    [InlineData("https://[::1]/x", "https")]
    [InlineData("mailto:a@example.com", "mailto")]
    [InlineData("MAILTO:a@example.com?subject=x", "mailto")]
    [InlineData("/path", "")]
    [InlineData("path/to", "")]
    [InlineData("./p", "")]
    [InlineData("../p", "")]
    [InlineData("#frag", "")]
    [InlineData("?q=1", "")]
    [InlineData("a.html", "")]
    [InlineData("/a:b", "")]
    [InlineData("a/b:c", "")]
    [InlineData("?next=http://x", "")]
    [InlineData("#a:b", "")]
    public void Allowed(string url, string expected)
    {
        Policy.Classify(url).ShouldBe(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("javascript:alert(1)")]
    [InlineData("JAVASCRIPT:alert(1)")]
    [InlineData("jAvAsCrIpT:alert(1)")]
    [InlineData("vbscript:x")]
    [InlineData("data:text/html,x")]
    [InlineData("file:///x")]
    [InlineData("ftp://x.example")]
    [InlineData("tel:+1555")]
    [InlineData("blob:https://x/y")]
    [InlineData("about:blank")]
    [InlineData(" javascript:alert(1)")]
    [InlineData("javascript:alert(1) ")]
    [InlineData("java\tscript:alert(1)")]
    [InlineData("java\nscript:alert(1)")]
    [InlineData("java\rscript:alert(1)")]
    [InlineData("\u0001javascript:alert(1)")]
    [InlineData(" javascript:alert(1)")]
    [InlineData("java​script:alert(1)")]
    [InlineData("%6Aavascript:alert(1)")]
    [InlineData("1http://x")]
    [InlineData("-http://x")]
    [InlineData(":x")]
    [InlineData("a b:c")]
    [InlineData("//host/path")]
    [InlineData("//")]
    [InlineData("///x")]
    [InlineData("\\\\host")]
    [InlineData("/\\host")]
    [InlineData("\\/host")]
    [InlineData("https:\\\\host")]
    [InlineData("https:/host")]
    [InlineData("https:host")]
    [InlineData("https://")]
    [InlineData("https:///path")]
    [InlineData("http://")]
    [InlineData("mailto:")]
    [InlineData("x:y")]
    [InlineData("ja\u0000vascript:x")]
    public void Rejected(string? url)
    {
        Policy.Classify(url).ShouldBeNull();
        Policy.IsAllowed(url).ShouldBeFalse();
    }

    [Fact]
    public void Relative_urls_can_be_disallowed()
    {
        var strict = new UrlPolicy(new HashSet<string>(["https"]), allowRelative: false);

        strict.IsAllowed("/x").ShouldBeFalse();
        strict.IsAllowed("#x").ShouldBeFalse();
        strict.IsAllowed("x").ShouldBeFalse();
        strict.IsAllowed("https://example.com").ShouldBeTrue();
        strict.IsAllowed("mailto:a@b.example").ShouldBeFalse();
    }

    [Fact]
    public void An_empty_scheme_list_allows_only_relative_urls()
    {
        var relativeOnly = new UrlPolicy(new HashSet<string>(), allowRelative: true);

        relativeOnly.IsAllowed("/x").ShouldBeTrue();
        relativeOnly.IsAllowed("https://example.com").ShouldBeFalse();
    }

    [Fact]
    public void Every_ascii_control_and_whitespace_character_is_rejected_anywhere_in_the_url()
    {
        for (var code = 0; code <= 0x20; code++)
        {
            var c = (char)code;
            Policy.IsAllowed($"/a{c}b").ShouldBeFalse($"U+{code:X4}");
            Policy.IsAllowed($"https://example.com/a{c}b").ShouldBeFalse($"U+{code:X4}");
        }

        Policy.IsAllowed("/a\u007Fb").ShouldBeFalse("DEL");
        Policy.IsAllowed("/a\u0085b").ShouldBeFalse("NEL");
        Policy.IsAllowed("/a" + (char)0x2028 + "b").ShouldBeFalse("LS");
    }
}
