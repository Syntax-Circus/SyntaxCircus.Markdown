# Draft: gazoond `docs/syntaxcircus/PACKAGE_CATALOG.md` entry

Add this row to the table, after `SyntaxCircus.Http.Resilience` (the table is grouped by concern, not strictly alphabetical):

```markdown
| `SyntaxCircus.Markdown` | Safe Markdown to HTML for user- and CMS-authored content (`IMarkdownRenderer`): raw HTML disabled, link/image URL allow-list, external-link `rel` policy, heading ids; replaces per-app Markdig wrappers. Headless; wrap the result in a `MarkupString` | [Source](https://github.com/Syntax-Circus/SyntaxCircus.Markdown) | [Guide](usage-examples/SyntaxCircus.Markdown.md) |
```

Copy `SyntaxCircus.Markdown.usage-example.md` (same folder) to `docs/syntaxcircus/usage-examples/SyntaxCircus.Markdown.md`.

Also update, in the same gazoond change, `docs/architecture/03-PACKAGE-MAP.md` (Selected, exact version) and `04-DECISION-LOG.md` (blog articles render through the package; legal documents keep `LegalMarkdownRenderer` until stored text is reviewed).
