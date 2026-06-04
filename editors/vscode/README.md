# Hat Trick Text Template — VS Code language support

Syntax highlighting for `.httt` files, the
[HatTrick.Text.Templating](https://github.com/HatTrickLabs/txt) language.

## Features

Highlights the template tags embedded in any text body; the surrounding host
text (HTML, SQL, plain text, …) is left uncolored.

Recognized constructs:

- **Block tags** — `{#if}` / `{#each}` / `{#with}` and their `{/if}` / `{/each}` /
  `{/with}` closers
- **Partial** `{>name}`, **comment** `{! … }`, **debug** `{@ … }`
- **Variables** — declare `{?var:name = value}`, reassign `{?:name = value}`,
  read `{:name}`
- **Simple binds** — `{Name.First}`, `{$}`, `{$.Path}`
- **Trim markers** — leading/trailing `-` / `+` on block tags
- **Scope walks** — `{..\Title}`, `{..\..\$.Name.First}`
- **Lambda calls** — `{(arg1, arg2) => funcName}`
- **Literals** — `"string"`, `'string'`, numeric, `true` / `false`, `null`
- **Escaped braces** — `{{` and `}}` rendered literally

## Install (from source)

```bash
# from this folder
npm install -g @vscode/vsce   # one-time
vsce package                  # produces httt-language-0.1.0.vsix
code --install-extension httt-language-0.1.0.vsix
```

Or, for live iteration, open this folder in VS Code and press **F5** to launch an
Extension Development Host with the language loaded.

## Testing a grammar change

Open this folder in VS Code and press **F5** to launch an Extension Development
Host with the language loaded, then open a `.httt` file (or `*.html.httt`,
`*.sql.httt`, etc.) and inspect the highlighting. After editing a grammar, run
**Developer: Reload Window** (`Ctrl+R`) in the dev host to pick up the change.

To see the exact scope under the cursor (handy for debugging a mis-color), run
**Developer: Inspect Editor Tokens and Scopes** from the Command Palette.

## Layout

| Path | Purpose |
|:---|:---|
| `package.json` | Extension manifest (`contributes.languages` + `grammars`) |
| `language-configuration.json` | Brackets, auto-close pairs, `{!`…`}` block comment |
| `syntaxes/httt.tmLanguage.json` | The base TextMate grammar (`scopeName: text.httt`) |
| `syntaxes/httt.<host>.tmLanguage.json` | Per-host embedding grammars (html, sql, xml, markdown, yaml) |
| `icon.png`, `icons/` | Marketplace icon + theme-aware file-tree glyphs |

## Future work

This is a **standalone** grammar — it highlights tags but does not color the host
text. A future version could ship an **injection grammar** (`injectionSelector`
with an `L:` prefix) that overlays httt tags on top of an existing host language
(e.g. HTML or SQL) so both are highlighted at once. That is a separate grammar
artifact, not a change to this one.
