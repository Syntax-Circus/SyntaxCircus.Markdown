using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace SyntaxCircus.Markdown.Tests.Support;

/// <summary>Parses rendered output with a real HTML parser and asserts it contains nothing a browser could execute or fetch unexpectedly.</summary>
internal static class HtmlAssert
{
    private static readonly HashSet<string> AllowedElements = new(StringComparer.Ordinal)
    {
        "html", "head", "body", "p", "br", "hr", "em", "strong", "del", "code", "pre", "blockquote", "ul", "ol", "li",
        "h1", "h2", "h3", "h4", "h5", "h6", "a", "img", "table", "thead", "tbody", "tr", "th", "td",
    };

    private static readonly HashSet<string> AllowedAttributes = new(StringComparer.Ordinal)
    {
        "href", "src", "alt", "title", "rel", "target", "id", "class", "loading", "decoding", "referrerpolicy", "style", "start", "align",
    };

    private static readonly HashSet<string> SafeSchemes = new(StringComparer.OrdinalIgnoreCase) { "http", "https", "mailto" };

    /// <summary>Parses <paramref name="html"/> as the contents of a body element, the way a browser parses an injected fragment.</summary>
    public static IElement ParseBody(string html)
    {
        var body = new HtmlParser().ParseDocument("<!doctype html><body></body>").Body!;
        body.InnerHtml = html;
        return body;
    }

    /// <summary>Fails when the output has an element, attribute or URL outside the allow-list.</summary>
    public static void IsSafe(string html, params string[] extraSchemes)
    {
        foreach (var element in ParseBody(html).QuerySelectorAll("*"))
        {
            AllowedElements.ShouldContain(element.LocalName, $"Unexpected <{element.LocalName}> in: {html}");
            foreach (var attribute in element.Attributes)
            {
                attribute.Name.ShouldNotStartWith("on", Case.Insensitive, $"Event handler {attribute.Name} in: {html}");
                AllowedAttributes.ShouldContain(attribute.Name, $"Unexpected attribute {attribute.Name} in: {html}");
            }

            if (element.GetAttribute("style") is { } style)
            {
                // Pipe-table column alignment is the only style Markdig emits.
                style.ShouldMatch("^text-align: (left|right|center);$", $"Unexpected style in: {html}");
            }

            foreach (var name in new[] { "href", "src" })
            {
                if (element.GetAttribute(name) is { } value)
                {
                    IsSafeUrl(value, html, extraSchemes);
                }
            }
        }
    }

    private static void IsSafeUrl(string value, string html, string[] extraSchemes)
    {
        value.ShouldNotBeNullOrWhiteSpace(html);
        value.Any(c => char.IsControl(c) || char.IsWhiteSpace(c) || c == '\\').ShouldBeFalse($"URL '{value}' has control/space/backslash in: {html}");
        value.StartsWith("//", StringComparison.Ordinal).ShouldBeFalse($"Protocol-relative URL in: {html}");
        var colon = value.IndexOf(':', StringComparison.Ordinal);
        var delimiter = value.AsSpan().IndexOfAny('/', '?', '#');
        if (colon >= 0 && (delimiter < 0 || colon < delimiter))
        {
            SafeSchemes.Concat(extraSchemes).ShouldContain(value[..colon], StringComparer.OrdinalIgnoreCase, $"Unsafe scheme in '{value}' in: {html}");
        }
    }
}
