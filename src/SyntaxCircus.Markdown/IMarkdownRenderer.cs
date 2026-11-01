namespace SyntaxCircus.Markdown;

/// <summary>Renders untrusted Markdown to HTML that is safe to emit unescaped (for example inside a Blazor <c>MarkupString</c>).</summary>
public interface IMarkdownRenderer
{
    /// <summary>
    /// Renders <paramref name="markdown"/> to an HTML fragment. Returns <see cref="string.Empty"/> for null, empty or whitespace-only input.
    /// The same input always yields the same output. Never throws for malformed or hostile input.
    /// </summary>
    string Render(string? markdown);

    /// <summary>Renders <paramref name="markdown"/> with per-document <paramref name="context"/>.</summary>
    string Render(string? markdown, MarkdownRenderContext context);
}
