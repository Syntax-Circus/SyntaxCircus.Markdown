# Changelog

All notable changes to this project are documented here. Format loosely follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## 0.1.0

Initial release.

### Decisions

- Rejected links render as plain text with no anchor (`UnsafeUrlBehavior.RenderAsText`, the default).
- Input over `MaxInputLength` (200,000) is truncated silently, never rejected.
- The URL-safety check is public in 0.1.0 (`SafeUrl`) so apps can reuse it outside Markdown.

### Added

- `IMarkdownRenderer` / `MarkdownRenderer`: Markdig-based, raw HTML disabled, pipe tables, strikethrough, optional bare-URL autolinks.
- `MarkdownProfile.Restricted` and `MarkdownProfile.Article` presets and `MarkdownOptions` for scheme allow-lists, image mode, unsafe-URL behaviour, external-link `rel`/`target`, internal hosts, heading ids and demotion, input length.
- `MarkdownRenderContext.PageTitle` to drop a duplicated leading `# Title`.
- `SafeUrl.IsSafe(url, profile)` / `SafeUrl.IsSafe(url, options)`: the renderer's link-URL check as a public, non-throwing, thread-safe yes/no for non-Markdown fields. Judges the text as given and never normalizes. The renderer and the API share one internal policy.
- `AddSyntaxCircusMarkdown` / `AddKeyedSyntaxCircusMarkdown` and the static `SafeMarkdown.ToHtml`.
- Test suite with a hostile-input corpus verified through an HTML parser, and parity tests for the per-app wrappers this replaces.
