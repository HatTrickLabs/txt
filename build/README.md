# HatTrick.Text.Templating

[![NuGet](https://img.shields.io/nuget/v/HatTrick.Text.Templating.svg)](https://www.nuget.org/packages/HatTrick.Text.Templating/)
[![License: Apache-2.0](https://img.shields.io/badge/license-Apache--2.0-blue.svg)](LICENSE)

A small, allocation-conscious text templating engine for .NET.

**[Full documentation](https://hattricklabs.com/docs/text-templates/)** | **[hattricklabs.com](https://hattricklabs.com)**

---

## Installation

The package targets *net9.0*.

```bash
dotnet add package HatTrick.Text.Templating
```

```c#
using HatTrick.Text.Templating;
```

## Quick Start

```c#
var fullName = new { FirstName = "John", LastName = "Doe" };

string template = "Hello {FirstName} {LastName}, this is just a test.";

TemplateEngine ngin = new TemplateEngine(template);

string result = ngin.Merge(fullName);

//result = Hello John Doe, this is just a test.
```

---

## Features

- Simple `{tag}` and compound bind-expression (`{Name.First}`) replacement
- Conditional blocks (`{#if}`) and iteration blocks (`{#each}`) with falsy-value rules
- Scope shifting (`{#with}`) and backward scope-chain walks (`..\`)
- Local variable declaration, reassignment, and block-scoped lifetime (`{?var:}`, `{?:}`, `{:name}`)
- Partial template injection (`{>tag}`) for composable sub-templates
- Lambda expression calls for formatting, sorting, joining, and custom logic
- Whitespace control (`-`/`+` trim markers) and a debug tag (`{@}`) for trace-based troubleshooting
- A `MergeException` with line/column/char-index context for precise error location

See the [full documentation](https://hattricklabs.com/docs/text-templates/) for all of the above in depth.

---

## License

Apache-2.0 — see [LICENSE](LICENSE).
