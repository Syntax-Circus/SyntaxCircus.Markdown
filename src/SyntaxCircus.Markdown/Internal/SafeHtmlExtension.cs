using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html.Inlines;

namespace SyntaxCircus.Markdown.Internal;

/// <summary>Installs the policy-checking link renderers and the final <c>LinkRewriter</c> guard on Markdig's HTML renderer.</summary>
internal sealed class SafeHtmlExtension(RenderPolicy policy) : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
    }

    public void Setup(MarkdownPipeline pipeline, Markdig.Renderers.IMarkdownRenderer renderer)
    {
        if (renderer is not HtmlRenderer html)
        {
            return;
        }

        html.ObjectRenderers.ReplaceOrAdd<LinkInlineRenderer>(new SafeLinkInlineRenderer(policy));
        html.ObjectRenderers.ReplaceOrAdd<AutolinkInlineRenderer>(new SafeAutolinkInlineRenderer(policy));
        html.LinkRewriter = policy.Guard;
    }
}
