using Microsoft.Extensions.DependencyInjection;

namespace SyntaxCircus.Markdown.Tests;

public class OptionsAndRegistrationTests
{
    [Fact]
    public void Restricted_defaults()
    {
        var o = MarkdownOptions.For(MarkdownProfile.Restricted);

        o.Images.ShouldBe(ImageMode.AltTextOnly);
        o.GenerateHeadingIds.ShouldBeFalse();
        o.ExternalLinkRel.ShouldBe("noopener noreferrer nofollow ugc");
        o.UnsafeUrls.ShouldBe(UnsafeUrlBehavior.RenderAsText);
        o.AllowedLinkSchemes.ShouldBe(["http", "https", "mailto"], ignoreOrder: true);
        o.AllowedImageSchemes.ShouldBe(["https"]);
        o.AllowRelativeUrls.ShouldBeTrue();
        o.OpenExternalLinksInNewTab.ShouldBeFalse();
        o.AutoLinkBareUrls.ShouldBeTrue();
        o.MinimumHeadingLevel.ShouldBe(1);
        o.MaxInputLength.ShouldBe(200_000);
    }

    [Fact]
    public void Article_defaults()
    {
        var o = MarkdownOptions.For(MarkdownProfile.Article);

        o.Images.ShouldBe(ImageMode.Allow);
        o.GenerateHeadingIds.ShouldBeTrue();
        o.ExternalLinkRel.ShouldBe("noopener noreferrer");
    }

    [Theory]
    [InlineData("java:script")]
    [InlineData("")]
    [InlineData("1abc")]
    [InlineData("a b")]
    [InlineData("javascript")]
    [InlineData("VBScript")]
    public void Invalid_or_forbidden_schemes_are_rejected_at_construction(string scheme)
    {
        var options = new MarkdownOptions();
        options.AllowedLinkSchemes.Add(scheme);

        Should.Throw<ArgumentException>(() => new MarkdownRenderer(options));
    }

    [Fact]
    public void Forbidden_image_schemes_are_rejected_at_construction()
    {
        var options = new MarkdownOptions();
        options.AllowedImageSchemes.Add("javascript");

        Should.Throw<ArgumentException>(() => new MarkdownRenderer(options));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(7)]
    [InlineData(-1)]
    public void Heading_level_must_be_between_one_and_six(int level)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new MarkdownRenderer(new MarkdownOptions { MinimumHeadingLevel = level }));
    }

    [Fact]
    public void Max_input_length_must_be_positive()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new MarkdownRenderer(new MarkdownOptions { MaxInputLength = 0 }));
    }

    [Theory]
    [InlineData("noopener\"onclick=")]
    [InlineData("a>b")]
    public void Rel_must_be_plain_tokens(string rel)
    {
        Should.Throw<ArgumentException>(() => new MarkdownRenderer(new MarkdownOptions { ExternalLinkRel = rel }));
    }

    [Fact]
    public void The_default_constructor_is_restricted()
    {
        new MarkdownRenderer().Render("![a](https://example.com/a.png)").Trim().ShouldBe("<p>a</p>");
    }

    [Fact]
    public void Static_helper_uses_the_requested_profile()
    {
        SafeMarkdown.ToHtml("# T").Trim().ShouldBe("<h1>T</h1>");
        SafeMarkdown.ToHtml("# T", MarkdownProfile.Article).Trim().ShouldBe("<h1 id=\"t\">T</h1>");
        SafeMarkdown.ToHtml(null).ShouldBe(string.Empty);
        Should.Throw<ArgumentOutOfRangeException>(() => SafeMarkdown.ToHtml("x", (MarkdownProfile)99));
    }

    [Fact]
    public void Add_registers_a_singleton_renderer()
    {
        var services = new ServiceCollection().AddSyntaxCircusMarkdown(MarkdownProfile.Article);
        using var provider = services.BuildServiceProvider();

        var first = provider.GetRequiredService<IMarkdownRenderer>();

        first.ShouldBeOfType<MarkdownRenderer>();
        provider.GetRequiredService<IMarkdownRenderer>().ShouldBeSameAs(first);
        first.Render("# T").ShouldContain("id=\"t\"");
    }

    [Fact]
    public void Add_defaults_to_restricted_and_applies_configure_on_top_of_the_profile()
    {
        using var provider = new ServiceCollection()
            .AddSyntaxCircusMarkdown(configure: o => o.InternalHosts.Add("example.com"))
            .BuildServiceProvider();

        var renderer = provider.GetRequiredService<IMarkdownRenderer>();

        renderer.Render("[a](https://example.com)").ShouldNotContain("rel=");
        renderer.Render("[a](https://other.example)").ShouldContain("nofollow ugc");
    }

    [Fact]
    public void Add_does_not_replace_an_existing_registration()
    {
        var custom = Substitute.For<IMarkdownRenderer>();
        using var provider = new ServiceCollection()
            .AddSingleton(custom)
            .AddSyntaxCircusMarkdown()
            .BuildServiceProvider();

        provider.GetRequiredService<IMarkdownRenderer>().ShouldBeSameAs(custom);
    }

    [Fact]
    public void Add_fails_fast_on_invalid_options()
    {
        Should.Throw<ArgumentException>(() => new ServiceCollection().AddSyntaxCircusMarkdown(configure: o => o.AllowedLinkSchemes.Add("javascript")));
    }

    [Fact]
    public void Keyed_registrations_give_each_profile_its_own_renderer()
    {
        using var provider = new ServiceCollection()
            .AddKeyedSyntaxCircusMarkdown("legal", MarkdownProfile.Restricted)
            .AddKeyedSyntaxCircusMarkdown("blog", MarkdownProfile.Article)
            .BuildServiceProvider();

        provider.GetRequiredKeyedService<IMarkdownRenderer>("legal").Render("# T").Trim().ShouldBe("<h1>T</h1>");
        provider.GetRequiredKeyedService<IMarkdownRenderer>("blog").Render("# T").Trim().ShouldBe("<h1 id=\"t\">T</h1>");
    }

    [Fact]
    public void Registration_guards_against_nulls()
    {
        Should.Throw<ArgumentNullException>(() => MarkdownServiceCollectionExtensions.AddSyntaxCircusMarkdown(null!));
        Should.Throw<ArgumentNullException>(() => new ServiceCollection().AddKeyedSyntaxCircusMarkdown(null!));
    }
}
