using Markdig.Renderers;
using Markdig.Renderers.Html.Inlines;
using Markdig.Syntax.Inlines;

namespace SyntaxCircus.Markdown.Internal;

/// <summary>
/// Replaces Markdig's link/image renderer. It is the only code that writes <c>&lt;a href&gt;</c> or <c>&lt;img src&gt;</c> for Markdown links, and it
/// checks every URL against <see cref="RenderPolicy"/> first. It never calls <c>WriteAttributes</c>, so no parsed attribute can reach the output.
/// </summary>
internal sealed class SafeLinkInlineRenderer(RenderPolicy policy) : LinkInlineRenderer
{
    protected override void Write(HtmlRenderer renderer, LinkInline link)
    {
        // Inside an image's alt text (EnableHtmlForInline is false) everything is plain text: no nested anchors or images.
        if (!renderer.EnableHtmlForInline)
        {
            renderer.WriteChildren(link);
            return;
        }

        if (link.IsImage)
        {
            WriteImage(renderer, link);
        }
        else
        {
            WriteAnchor(renderer, link);
        }
    }

    private void WriteAnchor(HtmlRenderer renderer, LinkInline link)
    {
        var url = link.Url;
        if (!policy.IsAllowedLink(url))
        {
            if (policy.Options.UnsafeUrls == UnsafeUrlBehavior.ReplaceWithPlaceholder)
            {
                renderer.Write("<a href=\"#\">");
                renderer.WriteChildren(link);
                renderer.Write("</a>");
            }
            else
            {
                renderer.WriteChildren(link);
            }

            return;
        }

        renderer.Write("<a href=\"");
        renderer.WriteEscapeUrl(url);
        renderer.Write('"');
        if (!string.IsNullOrEmpty(link.Title))
        {
            renderer.Write(" title=\"");
            renderer.WriteEscape(link.Title);
            renderer.Write('"');
        }

        WriteExternalAttributes(renderer, policy, url!);
        renderer.Write('>');
        renderer.WriteChildren(link);
        renderer.Write("</a>");
    }

    private void WriteImage(HtmlRenderer renderer, LinkInline link)
    {
        var url = link.Url;
        if (policy.Options.Images != ImageMode.Allow || !policy.IsAllowedImage(url))
        {
            WriteAltText(renderer, link);
            return;
        }

        renderer.Write("<img src=\"");
        renderer.WriteEscapeUrl(url);
        renderer.Write("\" alt=\"");
        WriteAltText(renderer, link);
        renderer.Write('"');
        if (!string.IsNullOrEmpty(link.Title))
        {
            renderer.Write(" title=\"");
            renderer.WriteEscape(link.Title);
            renderer.Write('"');
        }

        renderer.Write(" loading=\"lazy\" decoding=\"async\" referrerpolicy=\"no-referrer\" />");
    }

    private static void WriteAltText(HtmlRenderer renderer, LinkInline link)
    {
        var previous = renderer.EnableHtmlForInline;
        renderer.EnableHtmlForInline = false;
        try
        {
            renderer.WriteChildren(link);
        }
        finally
        {
            renderer.EnableHtmlForInline = previous;
        }
    }

    internal static void WriteExternalAttributes(HtmlRenderer renderer, RenderPolicy policy, string url)
    {
        if (policy.RelFor(url) is { } rel)
        {
            renderer.Write(" rel=\"").Write(rel).Write('"');
        }

        if (policy.OpensInNewTab(url))
        {
            renderer.Write(" target=\"_blank\"");
        }
    }
}

/// <summary>Renders <c>&lt;https://example.com&gt;</c> and <c>&lt;me@example.com&gt;</c> autolinks through the same URL policy and rel handling.</summary>
internal sealed class SafeAutolinkInlineRenderer(RenderPolicy policy) : AutolinkInlineRenderer
{
    protected override void Write(HtmlRenderer renderer, AutolinkInline link)
    {
        var url = link.IsEmail ? "mailto:" + link.Url : link.Url;
        if (!renderer.EnableHtmlForInline || !policy.IsAllowedLink(url))
        {
            renderer.WriteEscape(link.Url);
            return;
        }

        renderer.Write("<a href=\"");
        renderer.WriteEscapeUrl(url);
        renderer.Write('"');
        SafeLinkInlineRenderer.WriteExternalAttributes(renderer, policy, url);
        renderer.Write('>');
        renderer.WriteEscape(link.Url);
        renderer.Write("</a>");
    }
}
