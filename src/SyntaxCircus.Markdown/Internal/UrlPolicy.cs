namespace SyntaxCircus.Markdown.Internal;

/// <summary>Decides whether a URL taken from Markdown may be written into an <c>href</c> or <c>src</c> attribute.</summary>
internal sealed class UrlPolicy(IReadOnlySet<string> allowedSchemes, bool allowRelative)
{
    /// <summary>Builds a policy from a snapshot of <paramref name="schemes"/>. The renderer and <see cref="SyntaxCircus.Markdown.SafeUrl"/> both go through here.</summary>
    public static UrlPolicy Create(IEnumerable<string> schemes, bool allowRelative) =>
        new(new HashSet<string>(schemes, StringComparer.OrdinalIgnoreCase), allowRelative);

    /// <summary>True when <paramref name="url"/> is acceptable. See <see cref="Classify"/>.</summary>
    public bool IsAllowed(string? url) => Classify(url) is not null;

    /// <summary>
    /// Returns the lower-case scheme, <see cref="string.Empty"/> for an allowed relative URL, or null when rejected.
    /// Rejected: empty; any whitespace, control character or backslash (browsers strip or reinterpret these, which is how
    /// <c>java&#9;script:</c> and <c>/\evil.com</c> bypass naive checks); protocol-relative <c>//host</c>; a colon before the first
    /// <c>/</c>, <c>?</c> or <c>#</c> unless the text before it is an allowed scheme; and http(s) without a host.
    /// Markdig has already decoded character entities, so <c>&amp;#106;avascript:</c> arrives here as <c>javascript:</c>.
    /// </summary>
    public string? Classify(string? url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return null;
        }

        foreach (var c in url)
        {
            if (char.IsControl(c) || char.IsWhiteSpace(c) || c == '\\')
            {
                return null;
            }
        }

        if (url.StartsWith("//", StringComparison.Ordinal))
        {
            return null;
        }

        var colon = url.IndexOf(':', StringComparison.Ordinal);
        var delimiter = url.AsSpan().IndexOfAny('/', '?', '#');
        if (colon < 0 || (delimiter >= 0 && delimiter < colon))
        {
            return allowRelative ? string.Empty : null;
        }

        if (colon == 0 || !char.IsAsciiLetter(url[0]))
        {
            return null;
        }

        for (var i = 1; i < colon; i++)
        {
            if (!(char.IsAsciiLetterOrDigit(url[i]) || url[i] is '+' or '-' or '.'))
            {
                return null;
            }
        }

        var scheme = url[..colon].ToLowerInvariant();
        if (!allowedSchemes.Contains(scheme))
        {
            return null;
        }

        if (scheme is "http" or "https")
        {
            return url.AsSpan(colon).StartsWith("://", StringComparison.Ordinal)
                && Uri.TryCreate(url, UriKind.Absolute, out var uri) && uri.Host.Length > 0
                ? scheme
                : null;
        }

        return url.Length > colon + 1 ? scheme : null;
    }
}
