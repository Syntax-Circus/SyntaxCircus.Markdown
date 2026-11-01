using System.Diagnostics;
using SyntaxCircus.Markdown.Tests.Support;

namespace SyntaxCircus.Markdown.Tests;

public class InputHandlingTests
{
    private static readonly MarkdownRenderer Renderer = new(MarkdownProfile.Article);

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   \t\r\n  \n")]
    public void Null_empty_and_whitespace_render_to_an_empty_string(string? markdown)
    {
        Renderer.Render(markdown).ShouldBe(string.Empty);
        Renderer.Render(markdown, new MarkdownRenderContext("Title")).ShouldBe(string.Empty);
    }

    [Fact]
    public void A_null_context_is_rejected()
    {
        Should.Throw<ArgumentNullException>(() => Renderer.Render("x", null!));
    }

    [Fact]
    public void Plain_text_is_wrapped_in_a_paragraph_and_encoded()
    {
        Renderer.Render("a < b & c > d \"q\"").Trim().ShouldBe("<p>a &lt; b &amp; c &gt; d &quot;q&quot;</p>");
    }

    [Fact]
    public void Windows_and_unix_line_endings_render_identically()
    {
        Renderer.Render("# A\r\n\r\ntext\r\n\r\n- x\r\n- y").ShouldBe(Renderer.Render("# A\n\ntext\n\n- x\n- y"));
    }

    [Fact]
    public void Input_over_the_limit_is_truncated_not_rejected()
    {
        var renderer = new MarkdownRenderer(new MarkdownOptions { MaxInputLength = 10 });

        renderer.Render("0123456789ABCDEF").Trim().ShouldBe("<p>0123456789</p>");
    }

    [Fact]
    public void Truncation_never_splits_a_surrogate_pair()
    {
        var renderer = new MarkdownRenderer(new MarkdownOptions { MaxInputLength = 3 });

        var html = renderer.Render("ab\U0001F600cd");

        html.Trim().ShouldBe("<p>ab</p>");
    }

    [Fact]
    public void Truncating_inside_a_link_still_produces_safe_markup()
    {
        var renderer = new MarkdownRenderer(new MarkdownOptions { MaxInputLength = 20 });

        var html = renderer.Render("[label](https://example.com/very/long/path)");

        HtmlAssert.IsSafe(html);
    }

    [Fact]
    public void Huge_input_renders_within_a_reasonable_time()
    {
        var markdown = string.Concat(Enumerable.Repeat("# Heading\n\nSome *emphasis*, **strong**, `code` and [a link](https://example.com).\n\n- one\n- two\n\n", 2_000));

        var clock = Stopwatch.StartNew();
        var html = new MarkdownRenderer(new MarkdownOptions { MaxInputLength = int.MaxValue }).Render(markdown);

        clock.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(10));
        html.ShouldContain("<h1>Heading</h1>");
        HtmlAssert.IsSafe(html);
    }

    [Fact]
    public void The_default_limit_bounds_the_work_done_for_oversized_input()
    {
        var markdown = new string('a', 5_000_000);

        var html = new MarkdownRenderer().Render(markdown);

        html.Length.ShouldBeLessThan(250_000);
    }

    [Theory]
    [InlineData("[", 20_000)]
    [InlineData("![", 20_000)]
    [InlineData("*", 20_000)]
    [InlineData("_", 20_000)]
    [InlineData("`", 20_000)]
    [InlineData("<", 20_000)]
    [InlineData("](", 20_000)]
    [InlineData("[a](", 20_000)]
    [InlineData("~~", 20_000)]
    [InlineData("> ", 20_000)]
    [InlineData("- ", 20_000)]
    [InlineData("1. ", 20_000)]
    [InlineData("    ", 20_000)]
    [InlineData("| a ", 20_000)]
    [InlineData("#", 20_000)]
    [InlineData("\\", 20_000)]
    [InlineData("&", 20_000)]
    [InlineData("http://", 5_000)]
    [InlineData("> ", 150_000)]
    [InlineData("[", 150_000)]
    [InlineData("*a", 100_000)]
    [InlineData("- ", 150_000)]
    public void Pathological_repetition_completes_quickly_and_safely(string unit, int count)
    {
        var markdown = string.Concat(Enumerable.Repeat(unit, count));

        var clock = Stopwatch.StartNew();
        var html = Renderer.Render(markdown);

        clock.Elapsed.ShouldBeLessThan(TimeSpan.FromSeconds(10));
        HtmlAssert.IsSafe(html);
    }

    [Fact]
    public void Deeply_nested_unclosed_brackets_fall_back_to_encoded_plain_text()
    {
        var markdown = string.Concat(Enumerable.Repeat("[", 500)) + "<script>alert(1)</script>";

        var html = Renderer.Render(markdown);

        HtmlAssert.IsSafe(html);
        html.ShouldContain("&lt;script&gt;");
        html.ShouldContain("[[[[");
    }

    [Fact]
    public void Deeply_nested_block_quotes_and_lists_are_safe()
    {
        HtmlAssert.IsSafe(Renderer.Render(string.Concat(Enumerable.Repeat(">", 3_000)) + " deep"));
        HtmlAssert.IsSafe(Renderer.Render(string.Concat(Enumerable.Range(0, 300).Select(i => new string(' ', i * 2) + "- item\n"))));
    }

    [Fact]
    public void Concurrent_rendering_is_deterministic()
    {
        var markdown = "# Title\n\n[a](https://example.com) ![i](https://example.com/i.png)\n\n## Title\n\n| a | b |\n|---|---|\n| 1 | 2 |";
        var expected = Renderer.Render(markdown);

        var results = new string[200];
        Parallel.For(0, results.Length, i => results[i] = Renderer.Render(markdown));

        results.ShouldAllBe(result => result == expected);
    }

    [Fact]
    public void Control_characters_and_odd_unicode_do_not_throw()
    {
        var markdown = "\u0000\u0001 ￿ \uD800 lone surrogate \U0001F600 ‮ rtl ​ zwsp [x](\u0000)";

        var html = Should.NotThrow(() => Renderer.Render(markdown));

        HtmlAssert.IsSafe(html);
    }

    [Fact]
    public void The_renderer_is_not_affected_by_later_changes_to_the_options_it_was_built_from()
    {
        var options = MarkdownOptions.For(MarkdownProfile.Restricted);
        var renderer = new MarkdownRenderer(options);

        options.Images = ImageMode.Allow;
        options.AllowedLinkSchemes.Add("ftp");
        options.AllowedImageSchemes.Add("http");

        renderer.Render("![a](https://example.com/a.png) [b](ftp://example.com)").ShouldNotContain("<img");
        renderer.Render("[b](ftp://example.com)").ShouldNotContain("<a");
    }
}
