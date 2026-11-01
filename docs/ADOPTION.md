# Adoption plan

Status: plan only. No sibling repository has been modified. Versions below assume `SyntaxCircus.Markdown` 0.1.0 is published and Markdig 1.5.1 (the newest stable release) is acceptable everywhere; the package pins it and central package management in each app will need `Markdig` removed or raised to 1.5.1 if the app still references it directly.

## Survey summary

Markdig usage found under `D:\dev\SyntaxCircus\*` (worktree and `artifacts/` copies ignored):

| Repo | Markdig | File(s) | Pipeline | URL policy | Untrusted input? |
| --- | --- | --- | --- | --- | --- |
| sinforgiver | 1.4.1 | `src/SinForgiver.Blazor.Shared/Services/MarkdownRenderer.cs` (+ `tests/.../MarkdownRendererTests.cs`) | pipe tables, DisableHtml | http, https, mailto, `/x`; rest `#`; backslash and `//` rejected | CMS legal copy |
| the-button | 1.4.1 | `src/Shared/SafeMarkdown.cs` (source-linked into Api, Web, Admin, Web.Tests) | pipe tables, DisableHtml; images replaced by alt text | strictest: no whitespace/control/backslash, no `//`, colon-before-delimiter rule | CMS legal/marketing |
| dragon-poop | 1.5.1 | `src/DragonPoop.Web/Content/SafeMarkdown.cs` | DisableHtml only | http, https, mailto, `/x`; rest `#` | CMS content |
| who-flung-pu | 1.3.2 | `src/WhoFlungPu.Web/Content/SafeMarkdown.cs` | DisableHtml; images rendered as their children | **https and mailto only**, `/x`, `#x` | CMS content |
| syntax-circus-web | 1.4.1 | `src/SyntaxCircus.Content.Rendering/MarkdownContent.cs` + `SyntaxCircus.Content.Abstractions/ContentLink.cs` | autolinks, DisableHtml; AST rewrite of `LinkInline.Url` | `ContentLink.SafeUrl`: http, https, mailto, `/x`, `#x`; **returns `Uri.AbsoluteUri`** (package equivalent: `SafeUrl.IsSafe`) | CMS content |
| techstrap | 1.4.0 | `src/TechStrap.Infrastructure/Content/MarkdigMarkdownRenderer.cs`, `HtmlSanitizerAdapter.cs`, `KbHtmlSanitizer.cs`, `KbContentRenderer.cs` | DisableHtml, soft break as hard break, autolinks, strikethrough; **then Ganss HtmlSanitizer** | URL policy delegated to the sanitizer (http, https, mailto); `rel="noopener noreferrer nofollow"` added post-hoc; catches Markdig `ArgumentException` for deep nesting | customer/agent messages and KB |
| core-musica-web | 1.3.2 | `Services/Content/DocContentService.cs` | `UseAdvancedExtensions`, **raw HTML enabled** | none | repo-checked-in docs (trusted) |
| site-spelunkr | 1.1.2 | `Controls/AiMessageBubbleControl.axaml.cs` | `Markdig.Markdown.ToPlainText` | n/a | desktop, plain text, not HTML |
| puppies-plus | none | n/a | n/a | n/a | n/a (the earlier claim that it pins Markdig is wrong; no reference exists) |

### Decisions

Resolved by the owner for 0.1.0 (previously open questions):

1. A link whose URL is rejected renders as plain text with no anchor (`UnsafeUrlBehavior.RenderAsText`, the default). Apps that need parity with the old `href="#"` behaviour opt in to `ReplaceWithPlaceholder`.
2. Input longer than `MaxInputLength` (200,000) is truncated silently, not rejected. Validate length at the authoring boundary if the author must be told.
3. The URL-safety check is exposed publicly as `SafeUrl.IsSafe` (profile and options overloads) so apps reuse one policy for non-Markdown fields. It judges and never normalizes, unlike syntax-circus-web's `ContentLink.SafeUrl`, which returned `Uri.AbsoluteUri`.

### Differences and defects found

| # | Finding | Where | Package behaviour |
| --- | --- | --- | --- |
| 1 | Five hand-written copies of the same LinkRewriter policy that already diverge (http vs https-only, `#x` accepted or not, whitespace and control chars checked or not, `AbsoluteUri` normalisation or not). | all | One policy, tested once. |
| 2 | **Images**: SinForgiver, dragon-poop and syntax-circus-web emit `<img>` for any allowed URL (including remote hosts, so a CMS editor can embed a tracking pixel); SinForgiver turns an unsafe image into `<img src="#">` (a request for the current page). the-button and who-flung-pu render alt text only. | | Restricted: alt text only. Article: https and relative only, lazy, no-referrer. Unsafe image: alt text. |
| 3 | dragon-poop has no pipe tables (a table in CMS copy renders as a paragraph of pipes); SinForgiver and the-button have them. | | Tables always on. |
| 4 | `LinkRewriter` runs on every written URL but cannot change the element, so a rejected link becomes a clickable `<a href="#">`. | all | Default `RenderAsText`; `ReplaceWithPlaceholder` for parity. |
| 5 | SinForgiver, dragon-poop and who-flung-pu accept only `/x` as a relative URL; `page`, `./page`, `?q` become `#`. the-button accepts them. | | Relative URLs accepted (`AllowRelativeUrls`). |
| 6 | The wrappers differ on whitespace and control characters in URLs (`[a](<x y>)`): SinForgiver and dragon-poop allow both, who-flung-pu rejects control characters only, the-button rejects both. | | Rejected everywhere. |
| 7 | syntax-circus-web rewrites to `Uri.AbsoluteUri`, normalising host case, adding a trailing `/`, and percent-encoding; the others keep the authored text. | | Authored text kept. |
| 8 | Only techstrap handles Markdig's `ArgumentException` on deep nesting; the other renderers would be expected to throw (a 500 on a public page) for a CMS body with ~128 unclosed `[`. | | Contained: encoded plain text. |
| 9 | No sibling limits input size. | | `MaxInputLength` 200,000. |
| 10 | No external-link `rel` except techstrap (post-sanitizer). Links from user-generated text had no `nofollow ugc`. | | Per-profile `rel`; `target=_blank` optional and always paired with `noopener noreferrer`. |
| 11 | Heading ids: none of the five render headings with ids; Gazoond's article pages will need them for anchors/TOC. | | `Article` profile. |
| 12 | Null handling differs: who-flung-pu renders `""` through Markdig, the others short-circuit; syntax-circus-web and SinForgiver use `IsNullOrWhiteSpace`. | | Null/whitespace gives `""`. |
| 13 | SinForgiver's renderer is registered as its own `IMarkdownRenderer` interface; the-button source-links one file into three assemblies. | | One package, `Markdown.IMarkdownRenderer`. |

## Per-repo plan

For every repo: add `<PackageVersion Include="SyntaxCircus.Markdown" Version="0.1.0" />`, delete the `Markdig` `PackageVersion` and `PackageReference` once nothing else uses it, replace the file(s), run the existing tests, adjust the listed expectations.

### sinforgiver

- Replace `src/SinForgiver.Blazor.Shared/Services/MarkdownRenderer.cs` (class and its `IMarkdownRenderer.ToSafeHtml`). Callers inject the package `IMarkdownRenderer` and call `Render`; or keep a one-line adapter with the old name to avoid touching callers.
- Registration: `AddSyntaxCircusMarkdown(MarkdownProfile.Restricted, o => o.UnsafeUrls = UnsafeUrlBehavior.ReplaceWithPlaceholder)` for exact link parity.
- Expected differences: external links gain `rel="noopener noreferrer nofollow ugc"` (set `o.ExternalLinkRel = null` to avoid); bare URLs become links (`o.AutoLinkBareUrls = false` to avoid); images become alt text (their tests `NeutralizesJavaScriptSchemeImage` and `PreservesLegitimateImageUrl` change; use Article-style `Images = ImageMode.Allow` for the latter); relative `page` links now allowed.
- Port `MarkdownRendererTests.cs` to a thin test or delete it: `SiblingParityTests.SinForgiver_*` already reproduces it.

### the-button

- Delete `src/Shared/SafeMarkdown.cs` and the three `<Compile Include>` links (Api, Web, Admin) plus the test link in `Button.Web.Tests`.
- Replace `SafeMarkdown.ToHtml(x)` call sites with an injected `IMarkdownRenderer`. (The package static `SafeMarkdown.ToHtml` uses the stock profiles only, so it cannot carry the parity options below.)
- Config for parity: `Restricted` + `ReplaceWithPlaceholder`, `ExternalLinkRel = null`, `AutoLinkBareUrls = false`. Images already render as alt text, so no difference.
- `SafeMarkdown.SanitizeUrl` is internal and tested in `WebContentServiceMarkdownTests`; the equivalent table is `SiblingParityTests.Button_sanitize_url_table`.

### dragon-poop

- Replace `src/DragonPoop.Web/Content/SafeMarkdown.cs` (public static `SafeMarkdown.ToHtml`; the package ships a static of the same name with stock profiles, so for a drop-in only the `using` changes).
- Expected differences: pipe tables now render; images alt-only under Restricted (use Article if CMS copy has images); `page`/`./page`/`#x` links allowed; rejected links lose the anchor unless `ReplaceWithPlaceholder`.

### who-flung-pu

- Replace `src/WhoFlungPu.Web/Content/SafeMarkdown.cs`.
- Config: `o.AllowedLinkSchemes.Remove("http")` to keep https-only; `ReplaceWithPlaceholder`, `ExternalLinkRel = null`, `AutoLinkBareUrls = false` for parity. Images already alt text.
- Expected differences: tables render; relative links beyond `/x` allowed; whitespace URLs rejected.

### syntax-circus-web

- `MarkdownContent.ToSafeHtml` becomes a thin call to an injected or static renderer (`Article` profile if the CMS bodies have images, which the current code allows). Replace the body of `ContentLink.SafeUrl` (used for non-Markdown fields such as CTA hrefs) with the package's public check, so Markdown links and plain fields share one policy:

  ```csharp
  public static string? SafeUrl(string? value)
  {
      var candidate = value?.Trim();                       // IsSafe does not trim; the old code did
      return SyntaxCircus.Markdown.SafeUrl.IsSafe(candidate) ? candidate : null; // fully qualified: the method name shadows the type
  }
  ```

  Behaviour differences to accept or test for: (a) the result is the trimmed input, **not** `Uri.AbsoluteUri` (no host lower-casing, no added trailing `/`, no percent-encoding), so output is stable and equals what the editor typed; (b) relative URLs beyond `/x` and `#x` (`page`, `./page`, `?q`) are now allowed, set `AllowRelativeUrls = false` via the options overload if root-relative-only is required; (c) `//host`, whitespace inside the URL, and http(s) without a host are rejected (the old code relied on `Uri.TryCreate` and the leading-`//` check); (d) `mailto:` with an empty address is rejected. Update the `ContentLink` tests that assert `AbsoluteUri` output.
- Expected differences: URLs no longer normalised through `Uri.AbsoluteUri` (so no trailing-slash or host-lowercase rewriting; tests asserting normalised hrefs change); external `rel` added; remote images need `https`; `UseAutoLinks` parity is the default.
- `HeroAndBandTests` that check `MarkdownContent.ToSafeHtml` output remain valid for the cases shown (script text encoded, markdown intro rendered).

### techstrap

- Replace `MarkdigMarkdownRenderer.cs` with the package, configured: `SoftLineBreaksAsHardBreaks = true`, `ExternalLinkRel = "noopener noreferrer nofollow"`; KB renderer (`KbContentRenderer`) adds `Images = Allow` with `AllowedImageSchemes = { https, http }` and `AllowRelativeUrls = false` to match `KbHtmlSanitizer`.
- Recommendation: **keep** `HtmlSanitizerAdapter`/`KbHtmlSanitizer` (Ganss) as the second line of defence, as decisions D-014 and D-044 require. The package makes it redundant functionally (`SiblingParityTests.TechStrap_hostile_input_needs_no_second_sanitizer_pass`), but removing it is a security-posture decision for techstrap's owner, not part of this swap. The `CustomerText.ToHtml` fallback for deep nesting is no longer needed (the package contains it) and can be deleted together with its test.
- Expected differences: none intentional in the formatting tests; `rel` attribute order and the `decoding="async"` attribute on images.

### core-musica-web, site-spelunkr, puppies-plus

Not candidates. core-musica-web renders trusted repo documents with raw HTML and `UseAdvancedExtensions`, which this package intentionally cannot do. site-spelunkr only calls `ToPlainText`. puppies-plus has no Markdig.

## Gazoond

Decision: **adopt for blog articles now; keep `LegalMarkdownRenderer` for legal documents; swap `ContentMarkdownRenderer` for the analysis preview in a second, optional step.**

Why legal stays: `LegalMarkdownRenderer` is deliberately a plain-text subset (headings, paragraphs, lists, rules; everything else HTML-encoded literally, no inline emphasis, links or tables). The legal documents carry versioned text that users consent to; moving them to a full Markdown renderer changes what existing stored text looks like (`*word*`, `_x_`, `1)`, `<30 days>` and links all start to render) and that needs a diff of the stored documents and a visual review, not a library swap. When that review is done, the package covers it: `Restricted`, `MinimumHeadingLevel = 2`, `MarkdownRenderContext(document.Title)` reproduces the "drop the repeated h1, demote other h1s to h2" behaviour exactly. It is a follow-up, not part of this adoption.

Wiring for articles:

1. `Directory.Packages.props`: `<PackageVersion Include="SyntaxCircus.Markdown" Version="0.1.0" />`; `src/Gazoond.Web/Gazoond.Web.csproj`: `<PackageReference Include="SyntaxCircus.Markdown" />`. Record the version in `docs/architecture/03-PACKAGE-MAP.md` and the decision in `04-DECISION-LOG.md`.
2. `src/Gazoond.Web/Program.cs`, next to `AddGazoondSeo`:

   ```csharp
   builder.Services.AddSyntaxCircusMarkdown(MarkdownProfile.Article, o =>
   {
       o.MinimumHeadingLevel = 2;                 // BlogPost.razor owns the h1
       o.HeadingIdPrefix = "post-";               // avoid clashing with layout ids
       var origin = new Uri(builder.Configuration["Seo:BaseUrl"]!);
       o.InternalHosts.Add(origin.Host);          // own-site links get no rel
   });
   ```

   Register as keyed (`AddKeyedSyntaxCircusMarkdown("article", ...)`) only if `LegalMarkdownRenderer` is later replaced with a second `Restricted` registration.
3. `src/Gazoond.Web/Components/Pages/Public/BlogPost.razor` line 43: replace `@LegalMarkdownRenderer.Render(_post.Body, _post.Title)` with

   ```razor
   @inject IMarkdownRenderer Markdown
   ...
   @((MarkupString)Markdown.Render(_post.Body, new MarkdownRenderContext(_post.Title)))
   ```

   The wrapper `div.g-legal-markdown.pub-prose` already styles prose; add styles for `table`, `blockquote`, `pre`, `img` if the CSS does not cover them (check `pub-prose`).
4. Check the Content-Security-Policy / security headers (`SyntaxCircus.AspNetCore.Common`) allow `img-src` for the Cmsify media host; Article images are https or relative only.
5. Tests: add a `BlogPost` rendering test for a body containing a table, a link, an image and a `<script>`; `Gazoond.Tests.*` for `LegalMarkdownRenderer` are unchanged. The `data-testid="blog-body"` selector stays.
6. The admin SEO analysis projector (`Gazoond.Admin/Features/Content/Seo/ContentMarkdownRenderer.cs`) renders a Markdown subset for **analysis only**. Once the public page uses the package, the analyser should see the same HTML visitors get, so replace `ContentMarkdownRenderer.Render` with the same `Article` renderer (the `SplitAtHeadings` helper is independent and stays). Note analysis then sees `<img>`, `<table>` and heading ids; check `SeoDraftProjectorTests` expectations. Optional for the first adoption PR.
7. Phase rules from `CLAUDE.md` apply: do this only inside an approved phase of `docs/architecture/00-DISCOVERY-INDEX.md`.

## Rollout order

1. Publish the package (see README of the hand-off, or the report).
2. Gazoond blog (new surface, smallest risk).
3. the-button, who-flung-pu, dragon-poop (single-file replacement, parity config).
4. sinforgiver, syntax-circus-web (public API/test changes).
5. techstrap last, with its sanitizer kept.
