using SyntaxCircus.Markdown.Tests.Support;

namespace SyntaxCircus.Markdown.Tests;

/// <summary>
/// Hostile inputs. Every one must render, under every configuration, to HTML whose parsed DOM contains only allow-listed elements,
/// attributes and URLs (see <see cref="HtmlAssert.IsSafe"/>) and never a live script, handler, or dangerous scheme.
/// </summary>
public class XssCorpusTests
{
    internal static readonly string[] CorpusItems =
    [
        // Direct dangerous schemes
        "[x](javascript:alert(1))",
        "[x](JaVaScRiPt:alert(1))",
        "[x](vbscript:msgbox(1))",
        "[x](data:text/html;base64,PHNjcmlwdD5hbGVydCgxKTwvc2NyaXB0Pg==)",
        "[x](data:image/svg+xml,<svg onload=alert(1)>)",
        "[x](file:///etc/passwd)",
        "[x](blob:https://example.com/abc)",
        "[x](ftp://example.com/x)",
        "[x](tel:+15555550100)",
        "[x](about:blank)",
        "[x](livescript:alert(1))",
        "[x](view-source:https://example.com)",
        "[x](jar:https://example.com!/x)",

        // Obfuscation: whitespace, control chars, entities, encoding, case, unicode
        "[x](<java\tscript:alert(1)>)",
        "[x](<java\nscript:alert(1)>)",
        "[x](<java\rscript:alert(1)>)",
        "[x](<\u0001javascript:alert(1)>)",
        "[x](< javascript:alert(1)>)",
        "[x](<javascript:alert(1) >)",
        "[x](< javascript:alert(1)>)",
        "[x](<java​script:alert(1)>)",
        "[x](&#106;avascript:alert(1))",
        "[x](&#x6A;avascript:alert(1))",
        "[x](&#106avascript:alert(1))",
        "[x](jav&#x09;ascript:alert(1))",
        "[x](jav&#x0A;ascript:alert(1))",
        "[x](&#0000106&#0000097&#0000118&#0000097&#0000115&#0000099&#0000114&#0000105&#0000112&#0000116&#0000058alert(1))",
        "[x](javascript&colon;alert(1))",
        "[x](%6Aavascript:alert(1))",
        "[x](\\javascript:alert(1))",
        "[x](javascript\\:alert(1))",
        "[x](ja\\vascript:alert(1))",
        "[x](javascript%3Aalert(1))",
        "[x](ｊavascript:alert(1))",
        "[x](javascript：alert(1))",
        "[x](/\\evil.example)",
        "[x](\\\\evil.example)",
        "[x](//evil.example/phish)",
        "[x](///evil.example)",
        "[x](http:evil.example)",
        "[x](http:///)",
        "[x](https://)",
        "[x](:alert(1))",
        "[x](1javascript:alert(1))",
        "[x](<>)",
        "[x]()",

        // Attribute breakout attempts
        "[x](<https://example.com/\" onmouseover=\"alert(1)>)",
        "[x](https://example.com/\"onmouseover=\"alert(1))",
        "[x](https://example.com/' onmouseover='alert(1))",
        "[x](https://example.com \"title\" onmouseover=\"alert(1)\")",
        "[x](https://example.com \"t\\\" onmouseover=\\\"alert(1)\")",
        "[x](https://example.com/><script>alert(1)</script>)",
        "[\"onmouseover=\"alert(1)](https://example.com)",
        "[x](https://example.com \"<img src=x onerror=alert(1)>\")",
        "![\" onerror=\"alert(1)](https://example.com/a.png)",
        "![x](https://example.com/a.png \"\" onerror=\"alert(1))",
        "![x](https://example.com/a.png\" onerror=\"alert(1))",
        "![x](<https://example.com/a.png\" onerror=\"alert(1)>)",

        // Images
        "![x](javascript:alert(1))",
        "![x](data:image/svg+xml;base64,PHN2ZyBvbmxvYWQ9YWxlcnQoMSk+)",
        "![x](data:text/html,<script>alert(1)</script>)",
        "![x](http://tracker.example/pixel.gif)",
        "![x](//tracker.example/pixel.gif)",
        "![x](file:///etc/passwd)",
        "![<script>alert(1)</script>](https://example.com/a.png)",
        "![x](https://example.com/a.svg)",

        // Nested structures
        "![[a](javascript:alert(1))](https://example.com/a.png)",
        "[![a](javascript:alert(1))](javascript:alert(2))",
        "[![a](https://example.com/a.png)](javascript:alert(1))",
        "[[a](javascript:alert(1))](https://example.com)",
        "[**[a](javascript:alert(1))**](https://example.com)",
        "![![a](https://example.com/a.png)](https://example.com/b.png)",
        "[a](<javascript:alert(1)>)",
        "> [x](javascript:alert(1))",
        "- [x](javascript:alert(1))",
        "| a |\n|---|\n| [x](javascript:alert(1)) |",
        "# [x](javascript:alert(1))",
        "**[x](javascript:alert(1))**",

        // Reference-style and autolinks
        "[x][r]\n\n[r]: javascript:alert(1)",
        "[x][r]\n\n[r]: <javascript:alert(1)>",
        "[x][r]\n\n[r]: javascript:alert(1) \"title\"",
        "[r]\n\n[r]: javascript:alert(1)",
        "[x][]\n\n[x]: JAVASCRIPT:alert(1)",
        "![x][r]\n\n[r]: javascript:alert(1)",
        "![x][r]\n\n[r]: data:text/html,<script>alert(1)</script>",
        "[x][r]\n\n[r]: https://example.com \"\" onmouseover=\"alert(1)\"",
        "<javascript:alert(1)>",
        "<JAVASCRIPT:alert(1)>",
        "<data:text/html;base64,PHNjcmlwdD5hbGVydCgxKTwvc2NyaXB0Pg==>",
        "<vbscript:msgbox(1)>",
        "<https://example.com/\"onmouseover=\"alert(1)>",
        "<https://example.com>",
        "<me@example.com>",
        "<\"><script>alert(1)</script>@example.com>",
        "javascript:alert(1)",
        "www.example.com/\"onmouseover=\"alert(1)",
        "https://example.com/\"onmouseover=\"alert(1)",
        "https://example.com/<script>alert(1)</script>",
        "ftp://example.com/x tel:+15555550100 mailto:a@b.example",
        "javascript://%0Aalert(1)",
        "https://good.example@evil.example/",

        // Raw HTML
        "<script>alert(1)</script>",
        "<SCRIPT SRC=//evil.example/x.js></SCRIPT>",
        "<script\n>alert(1)</script\n>",
        "<scr<script>ipt>alert(1)</scr</script>ipt>",
        "text <script>alert(1)</script> text",
        "<img src=x onerror=alert(1)>",
        "<img/src=x/onerror=alert(1)>",
        "<IMG SRC=\"javascript:alert(1)\">",
        "<img src=\"x\" onerror=\"alert(1)\"",
        "<a href=\"javascript:alert(1)\">x</a>",
        "<a href=\"&#106;avascript:alert(1)\">x</a>",
        "<iframe src=\"javascript:alert(1)\"></iframe>",
        "<iframe srcdoc=\"<script>alert(1)</script>\"></iframe>",
        "<object data=\"javascript:alert(1)\"></object>",
        "<embed src=\"javascript:alert(1)\">",
        "<body onload=alert(1)>",
        "<div onmouseover=\"alert(1)\">hover</div>",
        "<div style=\"background:url(javascript:alert(1))\">x</div>",
        "<style>@import 'javascript:alert(1)';</style>",
        "<link rel=stylesheet href=\"javascript:alert(1)\">",
        "<meta http-equiv=\"refresh\" content=\"0;url=javascript:alert(1)\">",
        "<base href=\"javascript:alert(1)//\">",
        "<form action=\"javascript:alert(1)\"><input type=submit></form>",
        "<button formaction=\"javascript:alert(1)\">x</button>",
        "<details open ontoggle=alert(1)>",
        "<video><source onerror=\"alert(1)\"></video>",
        "<input autofocus onfocus=alert(1)>",
        "<marquee onstart=alert(1)>",
        "<!--><script>alert(1)</script>-->",
        "<!-- <script>alert(1)</script> -->",
        "<![CDATA[<script>alert(1)</script>]]>",
        "<?php echo 1; ?>",
        "<noscript><p title=\"</noscript><img src=x onerror=alert(1)>\">",
        "<textarea></textarea><script>alert(1)</script>",
        "<title></title><script>alert(1)</script>",
        "</p><script>alert(1)</script>",
        "<p>unclosed <b>bold",
        "<svg onload=alert(1)>",
        "<svg><script>alert(1)</script></svg>",
        "<svg><a xlink:href=\"javascript:alert(1)\"><text>x</text></a></svg>",
        "<svg><animate onbegin=alert(1) attributeName=x dur=1s>",
        "<svg><use href=\"data:image/svg+xml,<svg id='x' xmlns='http://www.w3.org/2000/svg'><script>alert(1)</script></svg>#x\">",
        "<math><mtext><table><mglyph><style><!--</style><img title=\"--&gt;&lt;img src=1 onerror=alert(1)&gt;\">",
        "<math href=\"javascript:alert(1)\">x</math>",
        "<math><maction actiontype=\"statusline#\" xlink:href=\"javascript:alert(1)\">x</maction></math>",
        "<a href=\"https://example.com\" target=_blank>x</a>",
        "&lt;script&gt;alert(1)&lt;/script&gt;",
        "&#60;script&#62;alert(1)&#60;/script&#62;",
        "&#x3C;img src=x onerror=alert(1)&#x3E;",

        // Code, tables, headings, attributes syntax
        "```\n<script>alert(1)</script>\n```",
        "```\"><script>alert(1)</script>\nx\n```",
        "```js onmouseover=alert(1)\nx\n```",
        "```\"onmouseover=\"alert(1)\nx\n```",
        "~~~html\n<img src=x onerror=alert(1)>\n~~~",
        "    <script>alert(1)</script>",
        "`<script>alert(1)</script>`",
        "``<img src=x onerror=alert(1)>``",
        "# <script>alert(1)</script>",
        "# x {#id onclick=alert(1)}",
        "# x {onclick=alert(1)}",
        "[x](https://example.com){onclick=alert(1)}",
        "[x](https://example.com){target=_blank onclick=alert(1)}",
        "![x](https://example.com/a.png){onerror=alert(1)}",
        "{onclick=alert(1)}\nparagraph",
        "paragraph\n{.cls onclick=alert(1)}",
        "| <script>alert(1)</script> |\n|---|\n| <img src=x onerror=alert(1)> |",
        "| a |\n|:--\"onmouseover=\"alert(1)|\n| b |",
        "Term\n: <script>alert(1)</script>",
        "[^1]: <script>alert(1)</script>\n\n[^1]",
        "- [ ] <script>alert(1)</script>",
        "$$ <script>alert(1)</script> $$",
        "$<script>alert(1)</script>$",
        "[[wikilink]](javascript:alert(1))",
        "@import url(javascript:alert(1))",
        "<<script>script>alert(1)</script>",
        "\u0000<script>alert(1)</script>",
        "<scr\u0000ipt>alert(1)</script>",
        "﻿<script>alert(1)</script>",
    ];

    public static TheoryData<string, string> Configurations()
    {
        var data = new TheoryData<string, string>();
        foreach (var markdown in CorpusItems)
        {
            foreach (var name in ConfigurationNames)
            {
                data.Add(name, markdown);
            }
        }

        return data;
    }

    private static readonly string[] ConfigurationNames = ["restricted", "article", "article-placeholder-newtab", "everything-allowed"];

    private static MarkdownRenderer Create(string name) => name switch
    {
        "restricted" => new MarkdownRenderer(MarkdownProfile.Restricted),
        "article" => new MarkdownRenderer(MarkdownProfile.Article),
        "article-placeholder-newtab" => new MarkdownRenderer(Configure(MarkdownOptions.For(MarkdownProfile.Article), o =>
        {
            o.UnsafeUrls = UnsafeUrlBehavior.ReplaceWithPlaceholder;
            o.OpenExternalLinksInNewTab = true;
            o.SoftLineBreaksAsHardBreaks = true;
        })),
        "everything-allowed" => new MarkdownRenderer(Configure(MarkdownOptions.For(MarkdownProfile.Article), o =>
        {
            o.AllowedLinkSchemes.UnionWith(["tel", "ftp"]);
            o.AllowedImageSchemes.Add("http");
            o.AllowRelativeUrls = true;
            o.HeadingIdPrefix = "p-";
        })),
        _ => throw new ArgumentOutOfRangeException(nameof(name)),
    };

    private static MarkdownOptions Configure(MarkdownOptions options, Action<MarkdownOptions> configure)
    {
        configure(options);
        return options;
    }

    [Theory]
    [MemberData(nameof(Configurations))]
    public void Hostile_input_renders_only_allow_listed_markup(string configuration, string markdown)
    {
        var html = Create(configuration).Render(markdown);

        HtmlAssert.IsSafe(html, configuration == "everything-allowed" ? ["tel", "ftp"] : []);
        html.ShouldNotContain("<script", Case.Insensitive);
        html.ShouldNotContain("<iframe", Case.Insensitive);
        html.ShouldNotContain("<svg", Case.Insensitive);
        html.ShouldNotContain("<math", Case.Insensitive);
        html.ShouldNotContain("<style", Case.Insensitive);
        html.ShouldNotContain("<object", Case.Insensitive);
        html.ShouldNotContain("<embed", Case.Insensitive);
        html.ShouldNotContain("<form", Case.Insensitive);
        html.ShouldNotContain("<meta", Case.Insensitive);
        html.ShouldNotContain("<link", Case.Insensitive);
        html.ShouldNotContain("<base", Case.Insensitive);
        html.ShouldNotContain("href=\"javascript:", Case.Insensitive);
        html.ShouldNotContain("src=\"javascript:", Case.Insensitive);
    }

    [Theory]
    [MemberData(nameof(Configurations))]
    public void Rendering_is_deterministic(string configuration, string markdown)
    {
        var renderer = Create(configuration);

        renderer.Render(markdown).ShouldBe(renderer.Render(markdown));
        Create(configuration).Render(markdown).ShouldBe(renderer.Render(markdown));
    }

    [Theory]
    [InlineData("<script>alert(1)</script>", "&lt;script&gt;alert(1)&lt;/script&gt;")]
    [InlineData("<img src=x onerror=alert(1)>", "&lt;img src=x onerror=alert(1)&gt;")]
    [InlineData("<a href=\"javascript:alert(1)\">x</a>", "&lt;a href=&quot;javascript:alert(1)&quot;&gt;x&lt;/a&gt;")]
    [InlineData("<svg onload=alert(1)>", "&lt;svg onload=alert(1)&gt;")]
    [InlineData("<math><mi>x</mi></math>", "&lt;math&gt;&lt;mi&gt;x&lt;/mi&gt;&lt;/math&gt;")]
    public void Raw_html_is_shown_as_encoded_text(string markdown, string expected)
    {
        new MarkdownRenderer().Render(markdown).ShouldContain(expected);
    }

    [Theory]
    [InlineData("```\"><script>alert(1)</script>\nx\n```")]
    [InlineData("```js onmouseover=alert(1)\nx\n```")]
    public void Fenced_code_info_strings_cannot_break_out_of_the_class_attribute(string markdown)
    {
        var html = new MarkdownRenderer(MarkdownProfile.Article).Render(markdown);

        var code = HtmlAssert.ParseBody(html).QuerySelector("code");
        code.ShouldNotBeNull();
        code.Attributes.Select(a => a.Name).ShouldAllBe(name => name == "class");
    }
}
