namespace SyntaxCircus.Markdown;

/// <summary>Static convenience over cached <see cref="MarkdownRenderer"/> instances, for code that has no dependency injection (tests, static helpers).</summary>
public static class SafeMarkdown
{
    private static readonly MarkdownRenderer Restricted = new(MarkdownProfile.Restricted);
    private static readonly MarkdownRenderer Article = new(MarkdownProfile.Article);

    /// <summary>Renders <paramref name="markdown"/> with the built-in <paramref name="profile"/>. Null or whitespace yields an empty string.</summary>
    public static string ToHtml(string? markdown, MarkdownProfile profile = MarkdownProfile.Restricted) =>
        profile switch
        {
            MarkdownProfile.Restricted => Restricted.Render(markdown),
            MarkdownProfile.Article => Article.Render(markdown),
            _ => throw new ArgumentOutOfRangeException(nameof(profile), profile, "Unknown profile."),
        };
}
