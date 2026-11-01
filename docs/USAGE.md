# SyntaxCircus.Markdown usage guide

Recipes for the common cases. The option reference is in the [README](../README.md).

## Pick a profile

- **Anything a visitor can type** (comments, messages, support replies): `Restricted`. No images, `rel="... nofollow ugc"`.
- **Content your editors write** (blog posts, help pages): `Article`. HTTPS and relative images, no nofollow, heading ids.
- **Legal documents**: `Restricted`, with `MinimumHeadingLevel = 2` and the page title passed in the context. Check the stored documents first: this renders inline Markdown (bold, links, tables) that a plain-text renderer would have shown literally.

## Blazor Server page

```csharp
builder.Services.AddSyntaxCircusMarkdown(MarkdownProfile.Article, o =>
{
    o.MinimumHeadingLevel = 2;
    o.InternalHosts.Add("www.example.com");
    o.InternalHosts.Add("example.com");
});
```

```razor
@using SyntaxCircus.Markdown
@inject IMarkdownRenderer Markdown

<h1>@_post.Title</h1>
<div class="prose">
    @((MarkupString)Markdown.Render(_post.Body, new MarkdownRenderContext(_post.Title)))
</div>
```

Rendering is synchronous and allocation-light enough to run in `OnParametersSet`; cache the resulting string per post if you render the same body on every request.

## Two configurations in one app

```csharp
builder.Services.AddKeyedSyntaxCircusMarkdown("legal", MarkdownProfile.Restricted, o => o.MinimumHeadingLevel = 2);
builder.Services.AddKeyedSyntaxCircusMarkdown("blog", MarkdownProfile.Article);
```

```razor
@inject IServiceProvider Services
@code { private IMarkdownRenderer Blog => Services.GetRequiredKeyedService<IMarkdownRenderer>("blog"); }
```

Or in a service constructor: `([FromKeyedServices("blog")] IMarkdownRenderer markdown)`.

## No dependency injection

```csharp
var html = SafeMarkdown.ToHtml(userText);                          // Restricted
var html2 = SafeMarkdown.ToHtml(postBody, MarkdownProfile.Article);

var custom = new MarkdownRenderer(new MarkdownOptions { UnsafeUrls = UnsafeUrlBehavior.ReplaceWithPlaceholder });
```

A `MarkdownRenderer` is immutable and thread-safe. Build it once; building one per call re-creates the Markdig pipeline.

## Allowing another scheme

```csharp
o.AllowedLinkSchemes.Add("tel");
```

`javascript` and `vbscript` throw at construction. `data` is not special-cased because it can only get in if you add it; do not.

## Migrating a wrapper that rendered `href="#"` for bad links

```csharp
o.UnsafeUrls = UnsafeUrlBehavior.ReplaceWithPlaceholder;
```

The default drops the anchor and keeps the label, which avoids dead `#` links in the page.

## Testing your own pages

Assert on the rendered string, or parse it. The package's own tests use AngleSharp to check that no unexpected element, attribute or URL scheme appears; copy `tests/SyntaxCircus.Markdown.Tests/Support/HtmlAssert.cs` if you want the same check in your suite.

## Behaviours worth knowing

- A URL containing a space is rejected (`[a](<my file.md>)` renders as text). Use `%20`.
- `[a](page)` is allowed (relative); `[a](a:b)` is rejected because `a:` looks like a scheme.
- Link URLs are written as authored (not normalized through `Uri`), so `https://Example.com` stays as typed.
- Input over `MaxInputLength` is truncated silently. Validate length at your authoring boundary if you need to tell the author.
- Autolinked text such as `ftp://x` stays plain text when the scheme is not allowed.
