# SyntaxCircus.Markdown

[![Build](https://github.com/Syntax-Circus/SyntaxCircus.Markdown/actions/workflows/build.yml/badge.svg)](https://github.com/Syntax-Circus/SyntaxCircus.Markdown/actions/workflows/build.yml)
[![NuGet](https://img.shields.io/nuget/v/SyntaxCircus.Markdown.svg)](https://www.nuget.org/packages/SyntaxCircus.Markdown)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE.txt)

Safe-by-default Markdown to HTML for user- and CMS-authored content, built on [Markdig](https://github.com/xoofx/markdig). It replaces the near-identical `SafeMarkdown` / `MarkdownRenderer` wrappers that each app used to carry: raw HTML is disabled, every link and image URL is checked against a scheme allow-list inside Markdig's renderer, external links get a `rel` policy, and the output is deterministic. The result is a plain `string` that is safe to hand to a Blazor `MarkupString`.

> **No support guaranteed.** Published as-is and maintained on a best-effort basis. Issues and PRs are welcome, but there's no SLA; fork it or vendor what you need if that's not enough.

## Setup

```bash
dotnet add package SyntaxCircus.Markdown
```

```csharp
using SyntaxCircus.Markdown;

builder.Services.AddSyntaxCircusMarkdown(MarkdownProfile.Article, o =>
{
    o.InternalHosts.Add("example.com");   // absolute links to your own site get no rel/target
    o.MinimumHeadingLevel = 2;            // the page already owns the h1
});
```

```razor
@inject IMarkdownRenderer Markdown

<article>
    @((MarkupString)Markdown.Render(Post.Body, new MarkdownRenderContext(PageTitle: Post.Title)))
</article>
```

No DI? `SafeMarkdown.ToHtml(markdown, MarkdownProfile.Restricted)` uses cached renderers, and `new MarkdownRenderer(options)` builds one directly. Need two configurations in one app (for example legal copy and blog posts)? Register them with `AddKeyedSyntaxCircusMarkdown("blog", MarkdownProfile.Article)` and inject with `[FromKeyedServices("blog")]`.

## Profiles

| | `Restricted` (default) | `Article` |
| --- | --- | --- |
| Intended for | comments, messages, legal copy, anything a user can type | editor-authored posts and pages |
| Images | alt text only, never an `<img>` | `https` and relative images, `loading="lazy"`, `decoding="async"`, `referrerpolicy="no-referrer"` |
| External link `rel` | `noopener noreferrer nofollow ugc` | `noopener noreferrer` |
| Heading ids | none | GitHub-style, unique within the document |

Everything else is identical. Both profiles support CommonMark, pipe tables, `~~strikethrough~~`, fenced code (`language-xxx` class) and bare-URL autolinks. They deliberately do **not** enable footnotes, task lists, definition lists, math, generic attributes (`{.class onclick=...}`), or any other Markdig extension. Add one only after checking what markup it emits.

Every property can be adjusted after picking a profile; `MarkdownOptions.For(profile)` returns the preset.

## Options

| Option | Default | Meaning |
| --- | --- | --- |
| `AllowedLinkSchemes` | `http`, `https`, `mailto` | Permitted link schemes. `javascript` and `vbscript` can never be added (construction throws). |
| `AllowedImageSchemes` | `https` | Permitted image schemes when `Images` is `Allow`. |
| `AllowRelativeUrls` | `true` | `/x`, `x`, `./x`, `../x`, `?q`, `#frag`. `//host` is always rejected. |
| `Images` | `AltTextOnly` (`Allow` for Article) | Whether `<img>` can be produced. A disallowed image falls back to alt text. |
| `UnsafeUrls` | `RenderAsText` | A rejected link keeps its label as plain content. `ReplaceWithPlaceholder` keeps the anchor with `href="#"`. |
| `ExternalLinkRel` | `noopener noreferrer nofollow ugc` | Applied to absolute http(s) links whose host is not in `InternalHosts`. |
| `OpenExternalLinksInNewTab` | `false` | Adds `target="_blank"` and forces `noopener noreferrer` into `rel`. |
| `InternalHosts` | empty | Hosts treated as your own site. |
| `GenerateHeadingIds`, `HeadingIdPrefix` | off (on for Article), `""` | Deterministic heading ids; the prefix avoids page-level id clashes. |
| `MinimumHeadingLevel` | `1` | `2` turns `#` into `h2`, and so on (clamped to h6). |
| `AutoLinkBareUrls` | `true` | `https://example.com` and `www.example.com` become links. |
| `SoftLineBreaksAsHardBreaks` | `false` | Chat-style newlines. |
| `MaxInputLength` | `200000` | Longer input is truncated (never rejected) before parsing. |

`MarkdownRenderContext(PageTitle)` drops a leading `# Title` that repeats the title the page already shows.

## How the safety works

- **Raw HTML is off** (`DisableHtml`): `<script>`, `<img onerror>`, `<svg>`, `<math>`, comments and the rest come out as HTML-encoded text.
- **URLs are checked where they are written.** A custom Markdig renderer for links, images and autolinks is the only code that emits `href`/`src`, and it asks one policy before every write. Markdig has already decoded character entities by then, so `&#106;avascript:` is seen as `javascript:`. The policy rejects any whitespace, control character or backslash anywhere in the URL (this defeats `java<TAB>script:` and `/\evil.com`), protocol-relative `//host`, any colon-prefixed text that is not an allowed scheme (including `%6Aavascript:`), and http(s) URLs with no host. A `LinkRewriter` using the same policy is installed as a last-line guard.
- **No attribute pass-through.** The link renderer never calls Markdig's attribute writer and generic attributes are not enabled, so parsed `{...}` text cannot become attributes.
- **Alt text is plain text.** Inside an image's alt, formatting and nested links are flattened, so `![[a](javascript:x)](...)` cannot nest an anchor.
- **Failure is contained.** Markdig refuses pathological nesting (hundreds of unclosed `[`); that case returns the input as encoded plain text instead of throwing.

No separate HTML sanitizer is used or needed: the set of elements is fixed by the Markdig renderers (p, h1-h6, ul, ol, li, blockquote, pre, code, em, strong, del, hr, br, a, img, table parts) and the only attributes are `href`, `src`, `alt`, `title`, `rel`, `target`, `id`, `class` (`language-*` on code), `loading`, `decoding`, `referrerpolicy`, `start` (ordered lists) and table `style="text-align: ..."`. The test suite renders a 179-entry hostile corpus under four configurations, parses the output with a real HTML parser (AngleSharp, test-only) and asserts exactly that allow-list, and a mutation check (disabling the URL policy) fails 246 of the tests.

## What this is not

- Not a sanitizer for arbitrary HTML. If you must allow some raw HTML, this is the wrong package.
- Not a Blazor component library. `MarkupString` is a one-line cast in your own component.
- Not a table-of-contents or excerpt generator.

## More

- [docs/USAGE.md](docs/USAGE.md) for recipes (legal pages, comments, blog with images, testing).
- [CHANGELOG.md](CHANGELOG.md)
