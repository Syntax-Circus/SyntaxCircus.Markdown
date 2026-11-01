namespace SyntaxCircus.Markdown;

/// <summary>Per-document rendering hints that are not part of the long-lived <see cref="MarkdownOptions"/>.</summary>
/// <param name="PageTitle">
/// The title the host page already shows as its own heading. When the document's first block is a level-1 heading whose text equals
/// this title (case-insensitive, trimmed), that heading is omitted so the page does not repeat it.
/// </param>
public sealed record MarkdownRenderContext(string? PageTitle = null);
