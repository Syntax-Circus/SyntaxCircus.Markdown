using AngleSharp.Html.Parser;
using SyntaxCircus.Markdown.Tests.Support;

namespace SyntaxCircus.Markdown.Tests;

/// <summary>
/// Reproduces the expectations of the per-app Markdig wrappers this package replaces (SinForgiver, the-button, dragon-poop, who-flung-pu,
/// syntax-circus-web, techstrap), each with the options that reproduce that app's configuration. Where the package deliberately behaves
/// differently the test says so; see docs/ADOPTION.md.
/// </summary>
public class SiblingParityTests
{
    private static MarkdownRenderer Build(Action<MarkdownOptions> configure)
    {
        var options = new MarkdownOptions();
        configure(options);
        return new MarkdownRenderer(options);
    }

    // ---- SinForgiver.Blazor.Shared MarkdownRenderer (the-button and dragon-poop share the same shape) ----

    private static readonly MarkdownRenderer SinForgiver = Build(o =>
    {
        o.UnsafeUrls = UnsafeUrlBehavior.ReplaceWithPlaceholder;
        o.ExternalLinkRel = null;
        o.AutoLinkBareUrls = false;
        o.Images = ImageMode.Allow;
        o.AllowRelativeUrls = true;
    });

    [Fact]
    public void SinForgiver_pipe_tables()
    {
        var html = SinForgiver.Render("| Information | How long we keep it |\n|---|---|\n| Certificate files | 30 days |");

        html.ShouldContain("<table>");
        html.ShouldContain("<th>Information</th>");
        html.ShouldContain("<td>30 days</td>");
    }

    [Fact]
    public void SinForgiver_raw_script_is_encoded_text()
    {
        var html = SinForgiver.Render("Hello <script>alert('xss')</script> world");

        html.ShouldNotContain("<script>");
        html.ShouldContain("&lt;script&gt;");
        html.ShouldContain("&lt;/script&gt;");
    }

    [Fact]
    public void SinForgiver_raw_html_blocks_are_encoded_text()
    {
        SinForgiver.Render("# Heading\n\n<img src=x onerror=\"alert(1)\">\n\nSome text.").ShouldContain("&lt;img src=x onerror=");
    }

    [Fact]
    public void SinForgiver_inline_raw_anchor_is_encoded_text()
    {
        SinForgiver.Render("Click <a href=\"javascript:alert(1)\">here</a> now.").ShouldContain("&lt;a href=&quot;javascript:alert(1)&quot;&gt;");
    }

    [Fact]
    public void SinForgiver_headings_lists_and_links()
    {
        var html = SinForgiver.Render("# Terms of Service\n\nPlease read the following:\n\n- First rule\n- Second rule\n\nSee our [privacy policy](https://example.com/privacy) for details.");

        html.ShouldContain("<h1>Terms of Service</h1>");
        html.ShouldContain("<ul>");
        html.ShouldContain("<li>First rule</li>");
        html.ShouldContain("<a href=\"https://example.com/privacy\">privacy policy</a>");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void SinForgiver_null_and_whitespace_are_empty(string? markdown)
    {
        SinForgiver.Render(markdown).ShouldBe(string.Empty);
    }

    [Fact]
    public void SinForgiver_javascript_link_becomes_hash()
    {
        SinForgiver.Render("[Click here](javascript:alert(1))").ShouldContain("<a href=\"#\">Click here</a>");
    }

    [Fact]
    public void Difference_unsafe_images_are_alt_text_not_an_img_with_a_hash_src()
    {
        // SinForgiver: <img src="#" alt="x" />. The package never emits an <img> it would not trust.
        SinForgiver.Render("![x](javascript:alert(1))").Trim().ShouldBe("<p>x</p>");
    }

    [Theory]
    [InlineData("data:text/html;base64,PHNjcmlwdD5hbGVydCgxKTwvc2NyaXB0Pg==")]
    [InlineData("vbscript:msgbox(1)")]
    [InlineData("//evil.example.com/phish")]
    [InlineData("/\\evil.com")]
    [InlineData("\\\\evil.com")]
    public void SinForgiver_dangerous_and_backslash_urls_become_hash(string url)
    {
        var html = SinForgiver.Render($"[link]({url})");

        html.ShouldNotContain($"href=\"{url}\"");
        html.ShouldContain("<a href=\"#\">link</a>");
    }

    [Fact]
    public void SinForgiver_escaped_slash_is_a_same_origin_path()
    {
        SinForgiver.Render("[link](\\/evil.com)").ShouldContain("<a href=\"/evil.com\">link</a>");
    }

    [Theory]
    [InlineData("https://example.com/terms")]
    [InlineData("http://example.com/terms")]
    [InlineData("mailto:legal@example.com")]
    [InlineData("/legal/privacy")]
    public void SinForgiver_legitimate_urls_are_preserved(string url)
    {
        SinForgiver.Render($"[link]({url})").ShouldContain($"<a href=\"{url}\">link</a>");
    }

    [Fact]
    public void SinForgiver_legitimate_https_image_is_preserved_with_extra_attributes()
    {
        // SinForgiver: <img src="..." alt="logo" />. The package adds lazy loading and no-referrer; the src/alt are unchanged.
        var html = SinForgiver.Render("![logo](https://example.com/logo.png)");

        html.ShouldContain("<img src=\"https://example.com/logo.png\" alt=\"logo\"");
    }

    // ---- the-button SafeMarkdown ----

    private static readonly MarkdownRenderer Button = Build(o =>
    {
        o.UnsafeUrls = UnsafeUrlBehavior.ReplaceWithPlaceholder;
        o.ExternalLinkRel = null;
        o.AutoLinkBareUrls = false;
        o.Images = ImageMode.AltTextOnly;
    });

    [Theory]
    [InlineData("https://example.com/x", "https://example.com/x")]
    [InlineData("http://example.com/x", "http://example.com/x")]
    [InlineData("mailto:a@example.com", "mailto:a@example.com")]
    [InlineData("#frag", "#frag")]
    [InlineData("/x", "/x")]
    [InlineData("x", "x")]
    [InlineData("./x", "./x")]
    [InlineData("../x", "../x")]
    [InlineData("?q=1", "?q=1")]
    [InlineData("//host", "#")]
    [InlineData("javascript:alert(1)", "#")]
    [InlineData("data:text/html,x", "#")]
    [InlineData("vbscript:x", "#")]
    [InlineData("%6Aavascript:alert(1)", "#")]
    [InlineData("a:b", "#")]
    public void Button_sanitize_url_table(string url, string expectedHref)
    {
        var html = Button.Render($"[t](<{url}>)");

        html.ShouldContain($"<a href=\"{expectedHref}\">t</a>");
    }

    [Fact]
    public void Button_images_render_as_plain_alt_text()
    {
        Button.Render("![alt text](https://example.com/a.png)").Trim().ShouldBe("<p>alt text</p>");
    }

    [Fact]
    public void Button_pipe_tables_and_scripts()
    {
        var html = Button.Render("plain text <script>alert(1)</script> more text\n\n| Information | How long |\n|---|---|\n| Consent records | 3 years |");

        html.ShouldNotContain("<script>");
        html.ShouldContain("<table>");
    }

    // ---- who-flung-pu SafeMarkdown: https and mailto only, images as their text ----

    private static readonly MarkdownRenderer WhoFlungPu = Build(o =>
    {
        o.AllowedLinkSchemes.Remove("http");
        o.UnsafeUrls = UnsafeUrlBehavior.ReplaceWithPlaceholder;
        o.ExternalLinkRel = null;
        o.AutoLinkBareUrls = false;
    });

    [Fact]
    public void WhoFlungPu_plain_http_links_are_rejected()
    {
        WhoFlungPu.Render("[t](http://example.com)").ShouldContain("<a href=\"#\">t</a>");
        WhoFlungPu.Render("[t](https://example.com)").ShouldContain("<a href=\"https://example.com\">t</a>");
    }

    [Fact]
    public void WhoFlungPu_images_render_their_text()
    {
        WhoFlungPu.Render("![pic](https://example.com/a.png)").Trim().ShouldBe("<p>pic</p>");
    }

    // ---- dragon-poop SafeMarkdown: no tables, http/https/mailto, '#' fallback ----

    [Fact]
    public void DragonPoop_links_use_hash_for_unsafe_urls()
    {
        var renderer = Build(o =>
        {
            o.UnsafeUrls = UnsafeUrlBehavior.ReplaceWithPlaceholder;
            o.ExternalLinkRel = null;
            o.AutoLinkBareUrls = false;
        });

        renderer.Render("[a](javascript:alert(1)) [b](https://example.com)").ShouldContain("<a href=\"#\">a</a> <a href=\"https://example.com\">b</a>");
    }

    // ---- syntax-circus-web MarkdownContent: autolinks, '#' fallback ----

    [Fact]
    public void SyntaxCircusWeb_bare_urls_are_linked_and_unsafe_links_neutralised()
    {
        var renderer = Build(o =>
        {
            o.UnsafeUrls = UnsafeUrlBehavior.ReplaceWithPlaceholder;
            o.ExternalLinkRel = null;
            o.Images = ImageMode.Allow;
        });

        var html = renderer.Render("Visit https://example.com or [x](javascript:alert(1))");

        html.ShouldContain("<a href=\"https://example.com\">https://example.com</a>");
        html.ShouldContain("<a href=\"#\">x</a>");
    }

    // ---- techstrap MarkdigMarkdownRenderer + KbHtmlSanitizer ----

    private static readonly MarkdownRenderer TechStrap = Build(o =>
    {
        o.SoftLineBreaksAsHardBreaks = true;
        o.ExternalLinkRel = "noopener noreferrer nofollow";
        o.Images = ImageMode.AltTextOnly;
    });

    [Fact]
    public void TechStrap_common_formatting_survives()
    {
        var html = TechStrap.Render("Hi **Ann**,\nline two\n\n- one\n- two\n\n`code` and ~~old~~\n\n> quoted\n\n---\n\n##### small heading");

        html.ShouldContain("<strong>Ann</strong>");
        html.ShouldContain("<br");
        html.ShouldContain("<ul>");
        html.ShouldContain("<code>code</code>");
        html.ShouldContain("<del>old</del>");
        html.ShouldContain("<blockquote>");
        html.ShouldContain("<hr");
        html.ShouldContain("<h5>");
    }

    [Fact]
    public void TechStrap_links_get_safe_rel_and_bare_urls_are_linked()
    {
        var html = TechStrap.Render("See [docs](https://example.com/docs) or https://example.com/faq");

        html.ShouldContain("href=\"https://example.com/docs\"");
        html.ShouldContain("href=\"https://example.com/faq\"");
        html.ShouldContain("rel=\"noopener noreferrer nofollow\"");
    }

    [Theory]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("[x](javascript:alert(1))")]
    [InlineData("<img src=x onerror=alert(1)>")]
    [InlineData("![x](javascript:alert(1))")]
    [InlineData("<a href=\"https://e.com\" onclick=\"alert(1)\">x</a>")]
    [InlineData("[x](data:text/html;base64,PHNjcmlwdD5hbGVydCgxKTwvc2NyaXB0Pg==)")]
    [InlineData("<iframe src=\"https://evil\"></iframe>")]
    [InlineData("<style>body{display:none}</style>")]
    public void TechStrap_hostile_input_needs_no_second_sanitizer_pass(string markdown)
    {
        // TechStrap runs HtmlSanitizer over the Markdig output as a second line of defence. The renderer output alone already
        // satisfies the same assertions, which is what lets an app drop the extra dependency.
        HtmlAssert.IsSafe(TechStrap.Render(markdown));
    }

    [Fact]
    public void TechStrap_escaped_html_keeps_the_text()
    {
        new HtmlParser().ParseDocument("<body>" + TechStrap.Render("<img src=x onerror=alert(1)>") + "</body>").Body!.TextContent.ShouldContain("onerror");
    }

    [Theory]
    [InlineData("x\u001b[0m y x\u001b[0m y x\u001b[0m y", "x\u001b[0m y")]
    [InlineData("values: [1, [1, [1, [1,", "values: [1, [1, ")]
    [InlineData(">>>>>>>>>> quoted", "quoted")]
    public void TechStrap_nested_text_keeps_the_agents_words(string markdown, string fragment)
    {
        new HtmlParser().ParseDocument("<body>" + TechStrap.Render(markdown) + "</body>").Body!.TextContent.ShouldContain(fragment);
    }

    [Fact]
    public void TechStrap_pathological_nesting_falls_back_to_encoded_plain_text()
    {
        TechStrap.Render(string.Concat(Enumerable.Repeat("[", 200)) + "<b>x</b> & y").ShouldContain("&lt;b&gt;x&lt;/b&gt; &amp; y");
        TechStrap.Render(string.Concat(Enumerable.Repeat("x\u001b[0m y ", 150))).ShouldContain("x\u001b[0m y");
        TechStrap.Render("values: " + string.Concat(Enumerable.Repeat("[1, ", 150))).ShouldContain("values: [1, [1, ");
        TechStrap.Render(new string('>', 128) + " quoted").ShouldContain("quoted");
    }

    [Fact]
    public void TechStrap_raw_html_is_shown_as_text()
    {
        TechStrap.Render("<b>bold?</b>").ShouldContain("&lt;b&gt;bold?&lt;/b&gt;");
    }

    [Fact]
    public void Difference_techstrap_http_images_are_not_available_in_chat_profile()
    {
        // TechStrap's KB profile allows absolute http(s) images. Reproduce it with ImageMode.Allow and both schemes.
        var kb = Build(o =>
        {
            o.Images = ImageMode.Allow;
            o.AllowedImageSchemes.Add("http");
            o.AllowRelativeUrls = false;
            o.ExternalLinkRel = "noopener noreferrer nofollow";
        });

        kb.Render("![a](http://example.com/a.png)").ShouldContain("<img src=\"http://example.com/a.png\"");
        kb.Render("![a](/relative.png)").Trim().ShouldBe("<p>a</p>");
        kb.Render("![a](data:image/png;base64,AAAA)").Trim().ShouldBe("<p>a</p>");
    }
}
