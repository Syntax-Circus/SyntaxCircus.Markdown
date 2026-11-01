namespace SyntaxCircus.Markdown.Tests;

public class HeadingTests
{
    private static MarkdownRenderer Article => new(MarkdownProfile.Article);

    private static MarkdownRenderer With(Action<MarkdownOptions> configure, MarkdownProfile profile = MarkdownProfile.Article)
    {
        var options = MarkdownOptions.For(profile);
        configure(options);
        return new MarkdownRenderer(options);
    }

    [Theory]
    [InlineData("# Hello World", "<h1 id=\"hello-world\">Hello World</h1>")]
    [InlineData("## What's New in v2.0?", "<h2 id=\"whats-new-in-v20\">What's New in v2.0?</h2>")]
    [InlineData("### **Bold** and `code`", "<h3 id=\"bold-and-code\"><strong>Bold</strong> and <code>code</code></h3>")]
    [InlineData("#### Ünïcödé Héading", "<h4 id=\"ünïcödé-héading\">Ünïcödé Héading</h4>")]
    [InlineData("##### 日本語 の 見出し", "<h5 id=\"日本語-の-見出し\">日本語 の 見出し</h5>")]
    [InlineData("###### Trailing ### hashes ###", "<h6 id=\"trailing-hashes\">Trailing ### hashes</h6>")]
    [InlineData("# [Linked](https://example.com) heading", "id=\"linked-heading\"")]
    [InlineData("# !!!", "<h1 id=\"section\">!!!</h1>")]
    [InlineData("Setext\n======", "<h1 id=\"setext\">Setext</h1>")]
    [InlineData("Setext two\n----------", "<h2 id=\"setext-two\">Setext two</h2>")]
    public void Headings_get_github_style_ids(string markdown, string expected)
    {
        Article.Render(markdown).ShouldContain(expected);
    }

    [Fact]
    public void Duplicate_headings_get_numeric_suffixes()
    {
        var html = Article.Render("# Intro\n\n## Intro\n\n### Intro\n\n## Other");

        html.ShouldContain("<h1 id=\"intro\">");
        html.ShouldContain("<h2 id=\"intro-1\">");
        html.ShouldContain("<h3 id=\"intro-2\">");
        html.ShouldContain("<h2 id=\"other\">");
    }

    [Fact]
    public void A_generated_suffix_never_collides_with_a_real_heading()
    {
        var html = Article.Render("# A\n\n# A 1\n\n# A");

        var ids = System.Text.RegularExpressions.Regex.Matches(html, "id=\"([^\"]+)\"").Select(m => m.Groups[1].Value).ToList();
        ids.Count.ShouldBe(3);
        ids.Distinct().Count().ShouldBe(3);
    }

    [Fact]
    public void Ids_are_deterministic_across_renders_and_instances()
    {
        const string markdown = "# One\n\n## Two\n\n## Two\n\n# One";

        Article.Render(markdown).ShouldBe(Article.Render(markdown));
    }

    [Fact]
    public void Restricted_profile_emits_no_ids()
    {
        new MarkdownRenderer(MarkdownProfile.Restricted).Render("# Title").Trim().ShouldBe("<h1>Title</h1>");
    }

    [Fact]
    public void Ids_can_be_turned_off_or_prefixed()
    {
        With(o => o.GenerateHeadingIds = false).Render("# Title").Trim().ShouldBe("<h1>Title</h1>");
        With(o => o.HeadingIdPrefix = "post-").Render("# Title").ShouldContain("<h1 id=\"post-title\">");
    }

    [Fact]
    public void Ids_are_attribute_safe_whatever_the_heading_text()
    {
        var html = Article.Render("# \" onclick=\"alert(1)\n\n## <script>alert(1)</script>\n\n### a&b<c>d");

        html.ShouldNotContain("onclick=\"alert");
        System.Text.RegularExpressions.Regex.Matches(html, "id=\"([^\"]*)\"")
            .Select(m => m.Groups[1].Value)
            .ShouldAllBe(id => id.All(c => char.IsLetterOrDigit(c) || c == '-'));
    }

    [Fact]
    public void Minimum_heading_level_demotes_and_clamps()
    {
        var renderer = With(o => o.MinimumHeadingLevel = 2);

        var html = renderer.Render("# One\n\n## Two\n\n###### Six");

        html.ShouldContain("<h2 id=\"one\">One</h2>");
        html.ShouldContain("<h2 id=\"two\">Two</h2>");
        html.ShouldContain("<h6 id=\"six\">Six</h6>");
        html.ShouldNotContain("<h1");
    }

    [Fact]
    public void Minimum_heading_level_of_six_puts_every_heading_at_h6()
    {
        With(o => o.MinimumHeadingLevel = 6).Render("# A\n\n### B").ShouldNotContain("<h1");
    }

    [Fact]
    public void A_leading_h1_that_repeats_the_page_title_is_dropped()
    {
        var html = Article.Render("# Terms of Service\n\nBody", new MarkdownRenderContext("  terms of service "));

        html.ShouldNotContain("Terms of Service");
        html.ShouldContain("<p>Body</p>");
    }

    [Fact]
    public void A_dropped_title_does_not_consume_a_heading_id()
    {
        var html = Article.Render("# Title\n\n## Title", new MarkdownRenderContext("Title"));

        html.ShouldContain("<h2 id=\"title\">Title</h2>");
    }

    [Theory]
    [InlineData("Intro\n\n# Terms", "Terms")]
    [InlineData("# Other heading\n\nBody", "Terms")]
    [InlineData("## Terms\n\nBody", "Terms")]
    public void Only_a_leading_matching_h1_is_dropped(string markdown, string title)
    {
        var plain = Article.Render(markdown);

        Article.Render(markdown, new MarkdownRenderContext(title)).ShouldBe(plain);
    }

    [Fact]
    public void Title_drop_and_demotion_combine_like_the_legal_page_renderer()
    {
        var renderer = With(o => o.MinimumHeadingLevel = 2, MarkdownProfile.Restricted);

        var html = renderer.Render("# Privacy Policy\n\n# Second\n\n## Third", new MarkdownRenderContext("Privacy Policy"));

        html.ShouldNotContain("Privacy Policy");
        html.ShouldContain("<h2>Second</h2>");
        html.ShouldContain("<h2>Third</h2>");
    }

    [Fact]
    public void A_null_page_title_drops_nothing()
    {
        Article.Render("# Title", new MarkdownRenderContext()).ShouldContain("<h1 id=\"title\">Title</h1>");
    }
}
