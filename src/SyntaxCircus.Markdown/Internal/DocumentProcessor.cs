using System.Globalization;
using System.Text;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace SyntaxCircus.Markdown.Internal;

/// <summary>AST-level adjustments applied after parsing and before rendering: duplicate-title removal, heading demotion, heading ids.</summary>
internal static class DocumentProcessor
{
    public static void Process(MarkdownDocument document, MarkdownOptions options, MarkdownRenderContext? context)
    {
        var headings = document.Descendants<HeadingBlock>().ToList();

        if (context?.PageTitle is { } title && document.Count > 0 && document[0] is HeadingBlock { Level: 1 } first
            && string.Equals(PlainText(first).Trim(), title.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            document.Remove(first);
            headings.Remove(first);
        }

        var taken = new HashSet<string>(StringComparer.Ordinal);
        var counters = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var heading in headings)
        {
            heading.Level = Math.Clamp(Math.Max(heading.Level, options.MinimumHeadingLevel), 1, 6);
            if (options.GenerateHeadingIds)
            {
                heading.GetAttributes().Id = UniqueId(options.HeadingIdPrefix + Slugify(PlainText(heading)), taken, counters);
            }
        }
    }

    internal static string PlainText(LeafBlock block)
    {
        var text = new StringBuilder();
        if (block.Inline is not null)
        {
            foreach (var node in block.Inline.Descendants())
            {
                switch (node)
                {
                    case LiteralInline literal:
                        text.Append(literal.Content.ToString());
                        break;
                    case CodeInline code:
                        text.Append(code.Content);
                        break;
                    case LineBreakInline:
                        text.Append(' ');
                        break;
                }
            }
        }

        return text.ToString();
    }

    /// <summary>GitHub-style: lower-cases, keeps letters and digits (any script), turns each run of whitespace, "-" or "_" into one hyphen, drops all other punctuation. Empty becomes "section".</summary>
    internal static string Slugify(string text)
    {
        var slug = new StringBuilder(text.Length);
        var pendingHyphen = false;
        foreach (var rune in text.EnumerateRunes())
        {
            if (Rune.IsLetterOrDigit(rune))
            {
                if (pendingHyphen && slug.Length > 0)
                {
                    slug.Append('-');
                }

                pendingHyphen = false;
                slug.Append(Rune.ToLowerInvariant(rune).ToString());
            }
            else if (Rune.IsWhiteSpace(rune) || rune.Value is '-' or '_')
            {
                pendingHyphen = true;
            }
        }

        return slug.Length == 0 ? "section" : slug.ToString();
    }

    private static string UniqueId(string candidate, HashSet<string> taken, Dictionary<string, int> counters)
    {
        var id = candidate;
        counters.TryGetValue(candidate, out var n);
        while (!taken.Add(id))
        {
            n++;
            id = string.Create(CultureInfo.InvariantCulture, $"{candidate}-{n}");
        }

        counters[candidate] = n;
        return id;
    }
}
