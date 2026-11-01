# Changelog

All notable changes to this project are documented here. Format loosely follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## 0.1.0

Initial release.

### Added

- `IMarkdownRenderer` / `MarkdownRenderer`: Markdig-based, raw HTML disabled, pipe tables, strikethrough, optional bare-URL autolinks.
- `MarkdownProfile.Restricted` and `MarkdownProfile.Article` presets and `MarkdownOptions` for scheme allow-lists, image mode, unsafe-URL behaviour, external-link `rel`/`target`, internal hosts, heading ids and demotion, input length.
- `MarkdownRenderContext.PageTitle` to drop a duplicated leading `# Title`.
- `AddSyntaxCircusMarkdown` / `AddKeyedSyntaxCircusMarkdown` and the static `SafeMarkdown.ToHtml`.
- Test suite with a hostile-input corpus verified through an HTML parser, and parity tests for the per-app wrappers this replaces.
