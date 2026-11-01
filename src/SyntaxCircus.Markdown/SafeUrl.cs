namespace SyntaxCircus.Markdown;

/// <summary>
/// The URL check the renderer applies to Markdown links, exposed for URLs that do not come from Markdown (a CTA <c>href</c>, a
/// "website" field). It is the same implementation, so a URL accepted here is one the renderer would write into an <c>href</c> under
/// the same options, and vice versa.
/// </summary>
/// <remarks>
/// The check judges the text exactly as given and never rewrites it: a URL that is safe is safe as written, so use the original
/// string. Nothing is trimmed or normalized (no <see cref="Uri.AbsoluteUri"/>, no host lower-casing, no added trailing slash), and
/// leading or trailing whitespace makes a URL unsafe; call <see cref="string.Trim()"/> first if your input may carry it.
/// Only the link policy is exposed (<see cref="MarkdownOptions.AllowedLinkSchemes"/> and <see cref="MarkdownOptions.AllowRelativeUrls"/>);
/// image schemes are not consulted. A safe URL is still the author's text: HTML-encode it when writing it into markup (Razor does).
/// All members are thread-safe and never throw for any <c>url</c>.
/// </remarks>
public static class SafeUrl
{
    private static readonly Internal.UrlPolicy Restricted = ForProfile(MarkdownProfile.Restricted);
    private static readonly Internal.UrlPolicy Article = ForProfile(MarkdownProfile.Article);

    /// <summary>
    /// True when <paramref name="url"/> may be written into an <c>href</c> under the built-in <paramref name="profile"/> (http, https
    /// and mailto, plus scheme-less relative URLs). Null, empty and malformed input is not safe. <c>javascript:</c> and
    /// <c>vbscript:</c> are never safe.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="profile"/> is not a defined value.</exception>
    public static bool IsSafe(string? url, MarkdownProfile profile = MarkdownProfile.Restricted) =>
        profile switch
        {
            MarkdownProfile.Restricted => Restricted.IsAllowed(url),
            MarkdownProfile.Article => Article.IsAllowed(url),
            _ => throw new ArgumentOutOfRangeException(nameof(profile), profile, "Unknown profile."),
        };

    /// <summary>
    /// True when <paramref name="url"/> may be written into an <c>href</c> under <paramref name="options"/>, using
    /// <see cref="MarkdownOptions.AllowedLinkSchemes"/> and <see cref="MarkdownOptions.AllowRelativeUrls"/>. Rejected: empty input; any
    /// whitespace, control character or backslash; protocol-relative <c>//host</c>; a colon-prefixed scheme that is not allowed
    /// (including percent-encoded or entity-obfuscated ones); http(s) without a host. The options are validated exactly as the
    /// renderer validates them and read once per call, so do not mutate them concurrently. The profile overload is cheaper when a
    /// preset is enough.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="options"/> is invalid (for example it allows <c>javascript</c>).</exception>
    /// <exception cref="ArgumentOutOfRangeException">A numeric option is out of range.</exception>
    public static bool IsSafe(string? url, MarkdownOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Validate();
        return Internal.UrlPolicy.Create(options.AllowedLinkSchemes, options.AllowRelativeUrls).IsAllowed(url);
    }

    private static Internal.UrlPolicy ForProfile(MarkdownProfile profile)
    {
        var options = MarkdownOptions.For(profile);
        return Internal.UrlPolicy.Create(options.AllowedLinkSchemes, options.AllowRelativeUrls);
    }
}
