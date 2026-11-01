namespace SyntaxCircus.Markdown.Internal;

/// <summary>An immutable snapshot of <see cref="MarkdownOptions"/> plus the derived URL decisions used while rendering.</summary>
internal sealed class RenderPolicy
{
    private readonly UrlPolicy _links;
    private readonly UrlPolicy _images;
    private readonly HashSet<string> _internalHosts;
    private static readonly string[] NewTabRel = ["noopener", "noreferrer"];

    private readonly string? _rel;

    public RenderPolicy(MarkdownOptions options)
    {
        options.Validate();
        _links = UrlPolicy.Create(options.AllowedLinkSchemes, options.AllowRelativeUrls);
        _images = UrlPolicy.Create(options.AllowedImageSchemes, options.AllowRelativeUrls);
        _internalHosts = new HashSet<string>(options.InternalHosts, StringComparer.OrdinalIgnoreCase);
        _rel = BuildRel(options);
        Options = Snapshot(options);
    }

    public MarkdownOptions Options { get; }

    public bool IsAllowedLink(string? url) => _links.IsAllowed(url);

    public bool IsAllowedImage(string? url) => _images.IsAllowed(url);

    /// <summary>Last-line guard installed as Markdig's <c>LinkRewriter</c>: anything either policy accepts passes, everything else becomes "#".</summary>
    public string Guard(string? url) => url is not null && (_links.IsAllowed(url) || _images.IsAllowed(url)) ? url : "#";

    /// <summary>The rel to write for an allowed link, or null for none.</summary>
    public string? RelFor(string url)
    {
        return IsExternal(url) ? _rel : null;
    }

    public bool OpensInNewTab(string url) => Options.OpenExternalLinksInNewTab && IsExternal(url);

    private bool IsExternal(string url) =>
        _links.Classify(url) is "http" or "https"
        && Uri.TryCreate(url, UriKind.Absolute, out var uri)
        && !_internalHosts.Contains(uri.Host);

    private static string? BuildRel(MarkdownOptions options)
    {
        var tokens = (options.ExternalLinkRel ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        if (options.OpenExternalLinksInNewTab)
        {
            var missing = NewTabRel.Where(required => !tokens.Contains(required, StringComparer.OrdinalIgnoreCase)).ToList();
            tokens.InsertRange(0, missing);
        }

        return tokens.Count == 0 ? null : string.Join(' ', tokens);
    }

    private static MarkdownOptions Snapshot(MarkdownOptions source)
    {
        var copy = new MarkdownOptions
        {
            AllowRelativeUrls = source.AllowRelativeUrls,
            Images = source.Images,
            UnsafeUrls = source.UnsafeUrls,
            ExternalLinkRel = source.ExternalLinkRel,
            OpenExternalLinksInNewTab = source.OpenExternalLinksInNewTab,
            GenerateHeadingIds = source.GenerateHeadingIds,
            HeadingIdPrefix = source.HeadingIdPrefix ?? string.Empty,
            MinimumHeadingLevel = source.MinimumHeadingLevel,
            AutoLinkBareUrls = source.AutoLinkBareUrls,
            SoftLineBreaksAsHardBreaks = source.SoftLineBreaksAsHardBreaks,
            MaxInputLength = source.MaxInputLength,
        };
        copy.AllowedLinkSchemes.Clear();
        copy.AllowedLinkSchemes.UnionWith(source.AllowedLinkSchemes);
        copy.AllowedImageSchemes.Clear();
        copy.AllowedImageSchemes.UnionWith(source.AllowedImageSchemes);
        copy.InternalHosts.UnionWith(source.InternalHosts);
        return copy;
    }
}
