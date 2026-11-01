# SyntaxCircus.Markdown

## Purpose

Safe-by-default Markdown to HTML for user- and CMS-authored content, on top of
Markdig. Raw HTML is disabled, every link and image URL is checked against a
scheme allow-list, external links get a configurable `rel`, and headings can
get deterministic ids. It returns a string; it does not provide a Blazor
component, an HTML sanitizer for arbitrary HTML, or a table-of-contents
generator.

## Selection Guide

- **Use it when:** An app renders Markdown that a CMS editor or visitor wrote (blog posts, help pages, legal copy, comments, messages) and the result is shown as markup (for example in a `MarkupString`).
- **Boundary:** Not for content that needs raw HTML or advanced Markdig extensions (trusted repository docs), and not a sanitizer for HTML that already exists. Authoring-time analysis of content (readability, keywords) is `SyntaxCircus.Seo.Analysis`.
- **Prerequisites:** None. No configuration section is read; options are set in code.
- **Target frameworks:** `net10.0`. Depends on Markdig and `Microsoft.Extensions.DependencyInjection.Abstractions` only.
- **Related packages:** Content usually comes from `SyntaxCircus.Cmsify.Client` (see [SyntaxCircus.Cmsify.Client.md](SyntaxCircus.Cmsify.Client.md)); page metadata is `SyntaxCircus.Blazor.Seo` (see [SyntaxCircus.Blazor.Seo.md](SyntaxCircus.Blazor.Seo.md)).

## Setup

```bash
dotnet add package SyntaxCircus.Markdown
```

```csharp
using SyntaxCircus.Markdown;

builder.Services.AddSyntaxCircusMarkdown(MarkdownProfile.Article, o =>
{
    o.MinimumHeadingLevel = 2;                  // the page owns the h1
    o.InternalHosts.Add("www.example.com");     // own-site links get no rel
});
```

```razor
@inject IMarkdownRenderer Markdown

<h1>@_post.Title</h1>
@((MarkupString)Markdown.Render(_post.Body, new MarkdownRenderContext(_post.Title)))
```

## Configuration

Options are code-only (`MarkdownOptions`); start from a profile.

| Profile | Images | External link `rel` | Heading ids |
| --- | --- | --- | --- |
| `Restricted` (default) | alt text only | `noopener noreferrer nofollow ugc` | no |
| `Article` | https and relative, lazy, no-referrer | `noopener noreferrer` | yes |

Other options: `AllowedLinkSchemes` (default http, https, mailto),
`AllowedImageSchemes` (https), `AllowRelativeUrls`, `UnsafeUrls`
(`RenderAsText` or `ReplaceWithPlaceholder`), `OpenExternalLinksInNewTab`,
`InternalHosts`, `HeadingIdPrefix`, `MinimumHeadingLevel`,
`AutoLinkBareUrls`, `SoftLineBreaksAsHardBreaks`, `MaxInputLength` (200,000).
`javascript` and `vbscript` can never be allowed.

## Two configurations in one app

```csharp
builder.Services.AddKeyedSyntaxCircusMarkdown("legal", MarkdownProfile.Restricted, o => o.MinimumHeadingLevel = 2);
builder.Services.AddKeyedSyntaxCircusMarkdown("blog", MarkdownProfile.Article);
// [FromKeyedServices("blog")] IMarkdownRenderer markdown
```

Without DI: `SafeMarkdown.ToHtml(text, MarkdownProfile.Restricted)`.

## Production Notes

- Rendering is deterministic and the renderer is immutable and thread-safe; register it as a singleton (the extension does) and cache rendered strings per document if pages are hot.
- Rejected links lose their anchor and keep their label. Set `UnsafeUrls = ReplaceWithPlaceholder` to keep `<a href="#">` for parity with older wrappers.
- Input over `MaxInputLength` is truncated, not rejected. Pathological nesting returns encoded plain text rather than throwing.
- Article images are `https` or relative only: make sure the site's `img-src` Content-Security-Policy allows the media host.
- Footnotes, task lists, math, and generic attributes are intentionally not enabled.

## Real Consumers

None yet. Planned adopters and the per-repo plan are in the package's `docs/ADOPTION.md`: gazoond (blog), the-button, dragon-poop, who-flung-pu, sinforgiver, syntax-circus-web, techstrap.

## Verification Links

[Package source and API examples](https://github.com/Syntax-Circus/SyntaxCircus.Markdown),
including `README.md`, `docs/USAGE.md` and `CHANGELOG.md`.
