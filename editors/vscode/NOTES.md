# .httt VS Code extension — status & next steps

_Handoff note for review. Not shipped in the package (excluded via `.vscodeignore`)._

## Status: complete and packaging clean

The extension is fully assembled — grammar, manifest, language config, both icon
types, license, docs — and `npx @vscode/vsce package` runs with no warnings
(→ `httt-language-0.1.0.vsix`, 16 files).

**Location:** lives in the `txt` repo at `editors/vscode/` (moved out of the
htl.web `support/` folder, 2026-06-03). The checked-in tree is now extension-only
— the Node/Shiki test harness and its `node_modules` were removed; testing is
manual via the F5 dev host (below).

| Piece | State |
|:---|:---|
| Grammar (`syntaxes/httt.tmLanguage.json`) | written + token-validated (Node/Shiki harness, since removed), visually verified in the F5 dev host |
| Manifest, `language-configuration.json` | done |
| `.vscode/launch.json` (extensionHost) | added — F5 launches the Extension Development Host |
| Extension icon (`icon.png`, HatTrick top-hat) | Marketplace + Extensions list |
| Language file icons (`icons/httt-light.svg` / `httt-dark.svg`) | theme-aware Explorer glyph |
| LICENSE (Apache-2.0, matches the txt repo), README, CHANGELOG | done |

## Caveat on the file-tree glyph

The Explorer glyph (`icons/*.svg`) only shows for users whose **File Icon Theme**
doesn't already supply an icon — Seti, Material, etc. will override it. The F5 dev
host defaults to Seti, so to actually *see* the SVGs, switch to
**File Icon Theme → Minimal** (or None). (Don't go looking for it under Seti and
think it's broken.)

## Remaining moves

1. ~~**F5 / install** to see it live~~ — done. F5 (with `.vscode/launch.json`)
   launches the Extension Development Host. To re-run: open `editors/vscode/`
   (this folder) in VS Code, press F5, then open any `.httt` file (or
   `*.html.httt` / `*.sql.httt` to exercise a host variant). Editing the grammar
   requires **Developer: Reload Window** (`Ctrl+R`) in the dev host.
2. **Commit** this folder — now lives in the `txt` repo (`editors/vscode/`),
   uncommitted there (the `.vsix` is gitignored).
3. **Publish** to the Marketplace — only if it should be public. Needs an Azure
   DevOps publisher account + PAT for `vsce publish` (manifest publisher is
   `hattricklabs`).

## Multi-host embedding (shipped: 5 `{`-free hosts; C#/JSON dropped)

Compound variants so `Foo.html.httt` etc. highlight both the host language and
httt tags. Each is a tiny grammar that `include`s the shared `text.httt`
repository rules then the host scope; routing is by `filenamePatterns` in
`package.json` (more specific than the bare `.httt` extension, so they win the
match). All hosts are **built-in** to VS Code — no `extensionDependencies`.

| Pattern | Language id | Grammar | Host scope |
|:---|:---|:---|:---|
| `*.sql.httt` | `httt-sql` | `httt.sql.tmLanguage.json` | `source.sql` |
| `*.html.httt` | `httt-html` | `httt.html.tmLanguage.json` | `text.html.basic` |
| `*.xml.httt` | `httt-xml` | `httt.xml.tmLanguage.json` | `text.xml` |
| `*.md.httt` | `httt-markdown` | `httt.markdown.tmLanguage.json` | `text.html.markdown` |
| `*.yaml.httt` / `*.yml.httt` | `httt-yaml` | `httt.yaml.tmLanguage.json` | `source.yaml` |

These use flat embedding (tags first, then `include: <host>`).

### Why C# and JSON were dropped (resolved empirically, not F5)

The decision was settled before the test harness was removed: a Node/Shiki
token-dump loaded the base grammar + the bundled host grammar + the compound
grammar into the *same* vscode-textmate/Oniguruma engine VS Code uses and printed
per-token scopes with positive controls. It established two things:

1. **`{`-free hosts work, including nesting.** Flat embedding colors tags inside
   HTML element bodies and SQL clauses, including tags inside `{#each}`…`{/each}`
   blocks. HTML and SQL were token-verified; XML/YAML/Markdown follow by the same
   mechanism (no `{` in their syntax). *Known limitation:* a tag inside a host
   **string literal**
   (`href="{:url}"`, SQL `'{:val}'`) isn't highlighted — the host's string rule
   claims it and httt is unreachable inside. Minor; the tag is still plain-readable.

2. **C# and JSON broke irreducibly** and were removed. httt's `{`-starts-a-tag
   rule and the host's own structural `{}` (C# blocks/interpolation, JSON objects)
   fight over every brace; httt wins greedily, so a bare host `{` opens a phantom
   tag that swallows real host code. The dumps showed C#'s `public class` scoped
   as httt tag content and a whole JSON object rendered as one httt tag.

The root cause is the language's single-brace `{ }` tag delimiter (only escape is
`{{`/`}}`). Any host that uses `{ }` structurally (C, C#, JSON, CSS, Rust, …)
collides: one `{` can't mean both "tag" and "host block." This was investigated
fully before deciding:

- **Escaping every host brace** (`{{`/`}}`, which a `.cs`/`.json` template needs
  anyway to be *valid* httt) makes it worse — the escape rule eats the host's
  structural braces (host loses object/block context) and the host's `$self`
  recursion still locks httt out of nested content (`{#each}` → `invalid.illegal`).
- **`{{ }}` tags + an injection grammar** was verified to fully fix it (tags at all
  depths, host stays valid) — but it requires changing the **engine's** delimiter,
  a breaking change to every existing template, the lexer, the docs, and the escape
  semantics.

**Decision (2026-06-03, user):** keep `{ }` — the engine is 5+ years open-sourced
with live users (incl. HTL's CLI help output); a delimiter change would be too
disruptive. So C#/JSON simply don't get host highlighting. Authoring a `.cs`/`.json`
template still works at runtime: start from a baseline host file, search-replace
`{`→`{{` and `}`→`}}` to escape its literal braces, then add httt `{ … }` tags.

## Other options (not started)

- ~~Move the folder into the `txt` repo to ship alongside the engine~~ — done
  (2026-06-03), now at `txt/editors/vscode/`.
