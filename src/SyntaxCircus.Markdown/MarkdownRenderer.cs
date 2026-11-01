using System.Net;
using Markdig;
using Markdig.Extensions.EmphasisExtras;
using SyntaxCircus.Markdown.Internal;

namespace SyntaxCircus.Markdown;

/// <summary>
/// The default <see cref="IMarkdownRenderer"/>: CommonMark plus pipe tables, strikethrough and (optionally) bare-URL autolinks, with raw HTML
/// disabled so any tag in the source is shown as encoded text, and every link and image URL checked against the configured policy.
/// Instances are immutable and thread-safe; create one per distinct <see cref="MarkdownOptions"/> and reuse it.
/// </summary>
public sealed class MarkdownRenderer : IMarkdownRenderer
{
    private readonly RenderPolicy _policy;
    private readonly MarkdownPipeline _pipeline;

    /// <summary>Creates a renderer. <paramref name="options"/> is copied; later changes to it have no effect. Null means <see cref="MarkdownProfile.Restricted"/>.</summary>
    /// <exception cref="ArgumentException">An option value is invalid (for example a scheme containing a colon, or an attempt to allow <c>javascript</c>).</exception>
    public MarkdownRenderer(MarkdownOptions? options = null)
    {
        _policy = new RenderPolicy(options ?? new MarkdownOptions());
        var builder = new MarkdownPipelineBuilder()
            .DisableHtml()
            .UsePipeTables()
            .UseEmphasisExtras(EmphasisExtraOptions.Strikethrough);
        if (_policy.Options.AutoLinkBareUrls)
        {
            builder.UseAutoLinks();
        }

        if (_policy.Options.SoftLineBreaksAsHardBreaks)
        {
            builder.UseSoftlineBreakAsHardlineBreak();
        }

        builder.Extensions.Add(new SafeHtmlExtension(_policy));
        _pipeline = builder.Build();
    }

    /// <summary>Creates a renderer for a built-in <paramref name="profile"/>.</summary>
    public MarkdownRenderer(MarkdownProfile profile)
        : this(MarkdownOptions.For(profile))
    {
    }

    /// <inheritdoc />
    public string Render(string? markdown) => Render(markdown, null);

    /// <inheritdoc />
    public string Render(string? markdown, MarkdownRenderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return Render(markdown, (MarkdownRenderContext?)context);
    }

    private string Render(string? markdown, MarkdownRenderContext? context)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return string.Empty;
        }

        var text = Truncate(markdown, _policy.Options.MaxInputLength);
        try
        {
            var document = Markdig.Markdown.Parse(text, _pipeline);
            DocumentProcessor.Process(document, _policy.Options, context);
            return Markdig.Markdown.ToHtml(document, _pipeline);
        }
        catch (ArgumentException)
        {
            // Markdig refuses pathological nesting (for example hundreds of unclosed "["). The text is kept, as encoded plain text.
            return "<p>" + WebUtility.HtmlEncode(text.ReplaceLineEndings("\n")).Replace("\n", "<br />\n", StringComparison.Ordinal) + "</p>\n";
        }
    }

    private static string Truncate(string text, int maxLength)
    {
        if (text.Length <= maxLength)
        {
            return text;
        }

        var length = char.IsHighSurrogate(text[maxLength - 1]) ? maxLength - 1 : maxLength;
        return text[..length];
    }
}
