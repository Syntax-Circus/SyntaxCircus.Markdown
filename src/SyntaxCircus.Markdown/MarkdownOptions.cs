namespace SyntaxCircus.Markdown;

/// <summary>Built-in option presets. Start from one and adjust individual <see cref="MarkdownOptions"/> properties as needed.</summary>
public enum MarkdownProfile
{
    /// <summary>
    /// For untrusted or user-generated text (comments, messages) and legal copy: images render as alt text only, external links get
    /// <c>rel="noopener noreferrer nofollow ugc"</c>, and headings get no ids.
    /// </summary>
    Restricted,

    /// <summary>
    /// For editor-authored articles: https images (and relative images) are allowed, external links get <c>rel="noopener noreferrer"</c>
    /// (no nofollow), and headings get deterministic ids.
    /// </summary>
    Article,
}

/// <summary>What happens to a link whose URL fails the policy.</summary>
public enum UnsafeUrlBehavior
{
    /// <summary>The link is dropped and only its label is rendered as plain content.</summary>
    RenderAsText,

    /// <summary>The anchor is kept with <c>href="#"</c> (the behaviour of the per-app wrappers this package replaces).</summary>
    ReplaceWithPlaceholder,
}

/// <summary>Whether Markdown images may produce <c>&lt;img&gt;</c> elements.</summary>
public enum ImageMode
{
    /// <summary>Images are never emitted; the alt text is rendered instead.</summary>
    AltTextOnly,

    /// <summary>
    /// Images whose URL passes <see cref="MarkdownOptions.AllowedImageSchemes"/> (or is relative) are emitted with
    /// <c>loading="lazy"</c>, <c>decoding="async"</c> and <c>referrerpolicy="no-referrer"</c>; other images fall back to alt text.
    /// </summary>
    Allow,
}

/// <summary>Configuration of a <see cref="MarkdownRenderer"/>. The renderer snapshots these values when it is constructed.</summary>
public sealed class MarkdownOptions
{
    /// <summary>URL schemes permitted for links (case-insensitive, without the colon). Default: http, https, mailto.</summary>
    public ISet<string> AllowedLinkSchemes { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "http", "https", "mailto" };

    /// <summary>URL schemes permitted for image sources when <see cref="Images"/> is <see cref="ImageMode.Allow"/>. Default: https.</summary>
    public ISet<string> AllowedImageSchemes { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "https" };

    /// <summary>
    /// Whether scheme-less URLs are allowed: <c>/path</c>, <c>path</c>, <c>./path</c>, <c>../path</c>, <c>?query</c> and <c>#fragment</c>.
    /// Protocol-relative URLs (<c>//host</c>) and anything containing a backslash, whitespace or control character are always rejected.
    /// </summary>
    public bool AllowRelativeUrls { get; set; } = true;

    /// <summary>Whether Markdown images may produce <c>&lt;img&gt;</c> elements. Default: <see cref="ImageMode.AltTextOnly"/>.</summary>
    public ImageMode Images { get; set; } = ImageMode.AltTextOnly;

    /// <summary>What to do with a link whose URL is rejected. Default: <see cref="UnsafeUrlBehavior.RenderAsText"/>.</summary>
    public UnsafeUrlBehavior UnsafeUrls { get; set; } = UnsafeUrlBehavior.RenderAsText;

    /// <summary>
    /// The <c>rel</c> value written on external (absolute http/https, host not in <see cref="InternalHosts"/>) links.
    /// Null or empty omits the attribute. Must be a space-separated list of rel tokens.
    /// </summary>
    public string? ExternalLinkRel { get; set; } = "noopener noreferrer nofollow ugc";

    /// <summary>When true, external links also get <c>target="_blank"</c> (and <c>noopener noreferrer</c> is forced into rel).</summary>
    public bool OpenExternalLinksInNewTab { get; set; }

    /// <summary>Hosts (for example <c>gazoond.com</c>, <c>www.gazoond.com</c>) whose absolute links are treated as internal: no rel or target.</summary>
    public ISet<string> InternalHosts { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Whether headings get a deterministic, document-unique <c>id</c> (GitHub-style slug). Default: false.</summary>
    public bool GenerateHeadingIds { get; set; }

    /// <summary>Text prepended to every generated heading id, to avoid clashing with ids elsewhere on the page.</summary>
    public string HeadingIdPrefix { get; set; } = string.Empty;

    /// <summary>The smallest heading level emitted (1-6). Set to 2 when the page already owns the h1: level-1 headings become h2.</summary>
    public int MinimumHeadingLevel { get; set; } = 1;

    /// <summary>Whether bare URLs (<c>https://example.com</c>, <c>www.example.com</c>) become links. Default: true.</summary>
    public bool AutoLinkBareUrls { get; set; } = true;

    /// <summary>Whether a single newline inside a paragraph renders as <c>&lt;br /&gt;</c> (chat-style). Default: false.</summary>
    public bool SoftLineBreaksAsHardBreaks { get; set; }

    /// <summary>Input longer than this many characters is truncated before parsing. Default: 200,000.</summary>
    public int MaxInputLength { get; set; } = 200_000;

    /// <summary>Creates the options for a built-in <paramref name="profile"/>.</summary>
    public static MarkdownOptions For(MarkdownProfile profile)
    {
        var options = new MarkdownOptions();
        if (profile == MarkdownProfile.Article)
        {
            options.Images = ImageMode.Allow;
            options.ExternalLinkRel = "noopener noreferrer";
            options.GenerateHeadingIds = true;
        }

        return options;
    }

    internal void Validate()
    {
        ValidateSchemes(AllowedLinkSchemes, nameof(AllowedLinkSchemes));
        ValidateSchemes(AllowedImageSchemes, nameof(AllowedImageSchemes));
        if (MinimumHeadingLevel is < 1 or > 6)
        {
            throw new ArgumentOutOfRangeException(nameof(MinimumHeadingLevel), MinimumHeadingLevel, "Must be between 1 and 6.");
        }

        if (MaxInputLength < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(MaxInputLength), MaxInputLength, "Must be at least 1.");
        }

        if (ExternalLinkRel is { } rel && rel.Any(c => !(char.IsAsciiLetterOrDigit(c) || c is '-' or ' ')))
        {
            throw new ArgumentException("Must be a space-separated list of rel tokens.", nameof(ExternalLinkRel));
        }
    }

    private static void ValidateSchemes(ISet<string> schemes, string name)
    {
        foreach (var scheme in schemes)
        {
            if (string.IsNullOrEmpty(scheme) || !char.IsAsciiLetter(scheme[0]) || scheme.Any(c => !(char.IsAsciiLetterOrDigit(c) || c is '+' or '-' or '.')))
            {
                throw new ArgumentException($"'{scheme}' is not a valid URL scheme (letters, digits, '+', '-', '.'; no colon).", name);
            }

            if (scheme.Equals("javascript", StringComparison.OrdinalIgnoreCase) || scheme.Equals("vbscript", StringComparison.OrdinalIgnoreCase))
            {
                throw new ArgumentException($"The '{scheme}' scheme can never be allowed.", name);
            }
        }
    }
}
