using SyntaxCircus.Markdown.Tests.Support;

namespace SyntaxCircus.Markdown.Tests;

public class LinkRenderingTests
{
    private static MarkdownRenderer Restricted => new(MarkdownProfile.Restricted);

    private static MarkdownRenderer Article => new(MarkdownProfile.Article);

    private static MarkdownRenderer With(Action<MarkdownOptions> configure, MarkdownProfile profile = MarkdownProfile.Restricted)
    {
        var options = MarkdownOptions.For(profile);
        configure(options);
        return new MarkdownRenderer(options);
    }

    [Fact]
    public void The_safety_assertion_helper_really_rejects_dangerous_html()
    {
        Should.Throw<ShouldAssertException>(() => HtmlAssert.IsSafe("<a href=\"javascript:alert(1)\">x</a>"));
        Should.Throw<ShouldAssertException>(() => HtmlAssert.IsSafe("<p onclick=\"x()\">x</p>"));
        Should.Throw<ShouldAssertException>(() => HtmlAssert.IsSafe("<script>x()</script>"));
        Should.Throw<ShouldAssertException>(() => HtmlAssert.IsSafe("<a href=\"java&#9;script:x\">x</a>"));
    }

    [Theory]
    [InlineData("[t](https://example.com/a?b=1&c=2)", "<a href=\"https://example.com/a?b=1&amp;c=2\" rel=\"noopener noreferrer nofollow ugc\">t</a>")]
    [InlineData("[t](http://example.com)", "<a href=\"http://example.com\" rel=\"noopener noreferrer nofollow ugc\">t</a>")]
    [InlineData("[t](mailto:a@example.com)", "<a href=\"mailto:a@example.com\">t</a>")]
    [InlineData("[t](/legal/privacy)", "<a href=\"/legal/privacy\">t</a>")]
    [InlineData("[t](relative/page)", "<a href=\"relative/page\">t</a>")]
    [InlineData("[t](./page)", "<a href=\"./page\">t</a>")]
    [InlineData("[t](../page)", "<a href=\"../page\">t</a>")]
    [InlineData("[t](#section)", "<a href=\"#section\">t</a>")]
    [InlineData("[t](?q=1)", "<a href=\"?q=1\">t</a>")]
    [InlineData("[t](/a:b)", "<a href=\"/a:b\">t</a>")]
    [InlineData("[t](HTTPS://Example.com)", "<a href=\"HTTPS://Example.com\" rel=\"noopener noreferrer nofollow ugc\">t</a>")]
    [InlineData("[t](https://example.com \"Title\")", "<a href=\"https://example.com\" title=\"Title\" rel=\"noopener noreferrer nofollow ugc\">t</a>")]
    public void Allowed_links_render_with_expected_attributes(string markdown, string expected)
    {
        Restricted.Render(markdown).ShouldContain(expected);
    }

    [Theory]
    [InlineData("[t](javascript:alert(1))", "<p>t</p>")]
    [InlineData("[**t**](javascript:alert(1))", "<p><strong>t</strong></p>")]
    [InlineData("[t](//evil.example)", "<p>t</p>")]
    [InlineData("[t](data:text/html,x)", "<p>t</p>")]
    [InlineData("[t]()", "<p>t</p>")]
    public void Rejected_links_keep_their_label_by_default(string markdown, string expected)
    {
        Restricted.Render(markdown).Trim().ShouldBe(expected);
    }

    [Fact]
    public void Rejected_links_can_become_a_hash_placeholder()
    {
        var html = With(o => o.UnsafeUrls = UnsafeUrlBehavior.ReplaceWithPlaceholder).Render("[t](javascript:alert(1))");

        html.Trim().ShouldBe("<p><a href=\"#\">t</a></p>");
    }

    [Fact]
    public void Relative_urls_can_be_disabled()
    {
        var renderer = With(o => o.AllowRelativeUrls = false);

        renderer.Render("[t](/x)").Trim().ShouldBe("<p>t</p>");
        renderer.Render("[t](#x)").Trim().ShouldBe("<p>t</p>");
        renderer.Render("[t](https://example.com)").ShouldContain("href=\"https://example.com\"");
    }

    [Fact]
    public void Allowed_schemes_are_configurable_and_case_insensitive()
    {
        var renderer = With(o => o.AllowedLinkSchemes.Add("TEL"));

        renderer.Render("[call](tel:+15555550100)").ShouldContain("<a href=\"tel:+15555550100\">call</a>");
        renderer.Render("[call](TeL:+15555550100)").ShouldContain("<a href=\"TeL:+15555550100\">call</a>");
        renderer.Render("[x](ftp://example.com)").ShouldNotContain("<a");
    }

    [Fact]
    public void Mailto_can_be_removed_from_the_allow_list()
    {
        var renderer = With(o => o.AllowedLinkSchemes.Remove("mailto"));

        renderer.Render("[t](mailto:a@example.com)").ShouldNotContain("<a");
        renderer.Render("<a@example.com>").ShouldNotContain("<a ");
    }

    [Fact]
    public void Query_and_encoding_in_urls_are_attribute_escaped()
    {
        var html = Restricted.Render("[t](https://example.com/?a=1&b=<2>\"3\")");

        HtmlAssert.IsSafe(html);
        HtmlAssert.ParseBody(html).QuerySelector("a")!.GetAttribute("href").ShouldStartWith("https://example.com/");
    }

    // ---- rel and target ----

    [Fact]
    public void Restricted_external_links_are_nofollow_ugc()
    {
        Restricted.Render("[t](https://example.com)").ShouldContain("rel=\"noopener noreferrer nofollow ugc\"");
    }

    [Fact]
    public void Article_external_links_are_not_nofollow()
    {
        var html = Article.Render("[t](https://example.com)");

        html.ShouldContain("rel=\"noopener noreferrer\"");
        html.ShouldNotContain("nofollow");
    }

    [Theory]
    [InlineData("[t](/internal)")]
    [InlineData("[t](#frag)")]
    [InlineData("[t](mailto:a@example.com)")]
    public void Internal_and_mailto_links_get_no_rel_or_target(string markdown)
    {
        var html = With(o => o.OpenExternalLinksInNewTab = true).Render(markdown);

        html.ShouldNotContain("rel=");
        html.ShouldNotContain("target=");
    }

    [Fact]
    public void Links_to_internal_hosts_are_treated_as_internal()
    {
        var renderer = With(o =>
        {
            o.InternalHosts.Add("gazoond.com");
            o.InternalHosts.Add("WWW.gazoond.com");
        });

        renderer.Render("[a](https://gazoond.com/blog)").ShouldNotContain("rel=");
        renderer.Render("[a](https://www.gazoond.com/blog)").ShouldNotContain("rel=");
        renderer.Render("[a](https://other.example/blog)").ShouldContain("rel=");
    }

    [Fact]
    public void New_tab_links_always_carry_noopener_and_noreferrer()
    {
        var html = With(o =>
        {
            o.OpenExternalLinksInNewTab = true;
            o.ExternalLinkRel = null;
        }).Render("[t](https://example.com)");

        html.ShouldContain("target=\"_blank\"");
        html.ShouldContain("rel=\"noopener noreferrer\"");
    }

    [Fact]
    public void New_tab_does_not_duplicate_existing_rel_tokens()
    {
        var html = With(o =>
        {
            o.OpenExternalLinksInNewTab = true;
            o.ExternalLinkRel = "nofollow noopener";
        }).Render("[t](https://example.com)");

        html.ShouldContain("rel=\"noreferrer nofollow noopener\"");
    }

    [Fact]
    public void An_empty_rel_policy_omits_the_attribute()
    {
        With(o => o.ExternalLinkRel = "").Render("[t](https://example.com)").ShouldNotContain("rel=");
    }

    // ---- autolinks ----

    [Fact]
    public void Angle_bracket_autolinks_get_the_external_rel()
    {
        Restricted.Render("<https://example.com/x>").Trim()
            .ShouldBe("<p><a href=\"https://example.com/x\" rel=\"noopener noreferrer nofollow ugc\">https://example.com/x</a></p>");
    }

    [Fact]
    public void Email_autolinks_become_mailto_links()
    {
        Restricted.Render("<me@example.com>").Trim().ShouldBe("<p><a href=\"mailto:me@example.com\">me@example.com</a></p>");
    }

    [Fact]
    public void Bare_urls_are_linked_by_default()
    {
        var html = Restricted.Render("See https://example.com/a and www.example.org now.");

        html.ShouldContain("<a href=\"https://example.com/a\"");
        html.ShouldContain("<a href=\"http://www.example.org\"");
    }

    [Fact]
    public void Bare_urls_stay_text_when_autolinking_is_off()
    {
        With(o => o.AutoLinkBareUrls = false).Render("See https://example.com/a now.").ShouldNotContain("<a");
    }

    [Fact]
    public void Disallowed_bare_urls_are_left_as_text()
    {
        var html = Restricted.Render("ftp://example.com/x");

        html.ShouldNotContain("<a");
        html.ShouldContain("ftp://example.com/x");
    }

    [Fact]
    public void Reference_style_links_use_the_same_policy()
    {
        Restricted.Render("[ok][r]\n\n[r]: https://example.com").ShouldContain("<a href=\"https://example.com\"");
        Restricted.Render("[bad][r]\n\n[r]: javascript:alert(1)").ShouldNotContain("<a");
    }

    // ---- images ----

    [Fact]
    public void Restricted_images_render_as_alt_text_only()
    {
        Restricted.Render("![A cat](https://example.com/cat.png)").Trim().ShouldBe("<p>A cat</p>");
    }

    [Fact]
    public void Restricted_image_alt_text_is_plain_text_even_with_formatting_and_nested_links()
    {
        Restricted.Render("![a **b** [c](https://example.com)](https://example.com/x.png)").Trim().ShouldBe("<p>a b c</p>");
    }

    [Fact]
    public void Article_images_render_with_privacy_and_performance_attributes()
    {
        Article.Render("![A cat](https://example.com/cat.png \"Cute\")").Trim().ShouldBe(
            "<p><img src=\"https://example.com/cat.png\" alt=\"A cat\" title=\"Cute\" loading=\"lazy\" decoding=\"async\" referrerpolicy=\"no-referrer\" /></p>");
    }

    [Fact]
    public void Article_relative_images_are_allowed()
    {
        Article.Render("![a](/img/a.png)").ShouldContain("<img src=\"/img/a.png\"");
    }

    [Theory]
    [InlineData("![a](http://example.com/a.png)")]
    [InlineData("![a](//example.com/a.png)")]
    [InlineData("![a](data:image/png;base64,AAAA)")]
    [InlineData("![a](javascript:alert(1))")]
    [InlineData("![a](mailto:a@example.com)")]
    [InlineData("![a]()")]
    public void Article_images_outside_the_image_allow_list_fall_back_to_alt_text(string markdown)
    {
        Article.Render(markdown).Trim().ShouldBe("<p>a</p>");
    }

    [Fact]
    public void Http_images_can_be_allowed()
    {
        With(o => o.AllowedImageSchemes.Add("http"), MarkdownProfile.Article).Render("![a](http://example.com/a.png)").ShouldContain("<img src=\"http://example.com/a.png\"");
    }

    [Fact]
    public void Image_mode_can_be_turned_on_for_restricted_options()
    {
        With(o => o.Images = ImageMode.Allow).Render("![a](https://example.com/a.png)").ShouldContain("<img ");
    }

    [Fact]
    public void An_image_inside_a_link_keeps_the_link_and_follows_the_image_policy()
    {
        Article.Render("[![a](https://example.com/a.png)](https://example.com)").ShouldContain("<a href=\"https://example.com\"");
        Restricted.Render("[![a](https://example.com/a.png)](https://example.com)").Trim()
            .ShouldBe("<p><a href=\"https://example.com\" rel=\"noopener noreferrer nofollow ugc\">a</a></p>");
    }

    [Fact]
    public void Images_do_not_get_the_external_link_rel()
    {
        Article.Render("![a](https://example.com/a.png)").ShouldNotContain("rel=");
    }

    // ---- other markdown features still work ----

    [Fact]
    public void Tables_strikethrough_code_and_lists_render()
    {
        var html = Restricted.Render("| A | B |\n|:--|--:|\n| 1 | 2 |\n\n~~gone~~ `code`\n\n1. one\n2. two\n\n```csharp\nvar x = 1;\n```\n\n> quote");

        html.ShouldContain("<table>");
        html.ShouldContain("<th style=\"text-align: left;\">A</th>");
        html.ShouldContain("<del>gone</del>");
        html.ShouldContain("<code>code</code>");
        html.ShouldContain("<ol>");
        html.ShouldContain("<code class=\"language-csharp\">var x = 1;");
        html.ShouldContain("<blockquote>");
        HtmlAssert.IsSafe(html);
    }

    [Fact]
    public void Soft_line_breaks_can_become_hard_breaks()
    {
        Restricted.Render("a\nb").ShouldNotContain("<br");
        With(o => o.SoftLineBreaksAsHardBreaks = true).Render("a\nb").ShouldContain("a<br />");
    }

    [Fact]
    public void Task_lists_footnotes_and_other_advanced_extensions_are_not_enabled()
    {
        var html = Restricted.Render("- [x] done\n\nText[^1]\n\n[^1]: note\n\nterm\n: definition");

        html.ShouldNotContain("<input");
        html.ShouldNotContain("footnote");
        html.ShouldNotContain("<dl");
    }
}
