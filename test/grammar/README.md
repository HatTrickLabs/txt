# Grammar test templates

Sample `.httt` files for **eyeballing the VS Code syntax highlighting** by hand.
These are highlighting fixtures, not rendering tests — they exercise the grammar's
tag families so you can confirm colors look right after a grammar change.

The grammar/extension itself lives in **`txt/editors/vscode/`**.

| File | What it shows |
|:---|:---|
| `sample.httt` | Every tag family on a plain-text host — binds, `{#if}`/`{#each}`/`{#with}` blocks, scope walks, variables, lambda, literals, trim markers, partial, debug, escaped `{{ }}` |
| `sample.html.httt` | httt tags layered on an HTML host (`*.html.httt` variant) |
| `sample.sql.httt` | httt tags layered on a SQL host (`*.sql.httt` variant) |

## How to test in VS Code

1. Open the extension folder **`txt/editors/vscode/`** in VS Code
   (File → Open Folder — open *that* folder, not the repo root).
2. Press **F5**. A second VS Code window opens — the **Extension Development
   Host** — with the `.httt` language loaded. (F5 works because of
   `editors/vscode/.vscode/launch.json`.)
3. In that new window, open one of these sample files
   (`txt/test/grammar/sample.httt`, `sample.html.httt`, `sample.sql.httt`) and
   look at the highlighting. The `*.html.httt` / `*.sql.httt` filename patterns
   route those files to the host-aware grammars automatically.

## After editing a grammar

Grammar changes are **not** hot-reloaded. In the Extension Development Host,
run **Developer: Reload Window** (`Ctrl+R`) from the Command Palette to pick up
edits to anything under `editors/vscode/syntaxes/`.

## Debugging a specific color

Put the cursor on a token and run **Developer: Inspect Editor Tokens and Scopes**
(Command Palette). It shows the TextMate scope stack under the cursor — e.g.
`punctuation.definition.tag.begin.httt` — so you can see exactly which grammar
rule colored it.

## Notes

- A tag inside a host **string literal** (an HTML attribute value, a SQL `'...'`
  string) keeps the host's string color rather than the tag color. That's a known
  limitation of flat embedding — see `editors/vscode/NOTES.md`.
- `.cs.httt` and `.json.httt` have **no** host highlighting on purpose: C#/JSON
  use `{ }` structurally, which collides with httt's `{ }` tags. Details in
  `editors/vscode/NOTES.md`.
