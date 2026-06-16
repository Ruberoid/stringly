# Stringly

[![CI](https://github.com/Ruberoid/stringly/actions/workflows/ci.yml/badge.svg)](https://github.com/Ruberoid/stringly/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Stringly.svg?logo=nuget)](https://www.nuget.org/packages/Stringly)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

> Build a string the most over-engineered way imaginable — one token at a time.

```csharp
using static Stringly.Say;

Console.WriteLine(One().Three().Three().Seven());   // 1337
Console.WriteLine(H().e().l().l().o());              // Hello
Console.WriteLine(Four().Two());                     // 42, the answer to everything
```

`"strongly typed"` is good engineering. `"stringly typed"` — shoving everything into strings
instead of types — is the opposite. **Stringly** commits to it entirely: a fluent API that
spells strings out token by token, designed to grow a whole ecosystem of community
"vocabulary" packs.

The engineering under it is real.

## Repository layout

| Path | What it is |
|---|---|
| `src/Stringly` | Core library: the `Phrase` accumulator + `Say` entries + generated ASCII tokens + `string` twins |
| `src/Stringly.Nato` | Sample pack — full NATO phonetic alphabet + radio macros (`TenFour`, `DoYouCopy`) |
| `src/Stringly.Greek` | Sample pack — Greek alphabet in real Unicode glyphs (`Alpha()` → `α`) |
| `src/Stringly.Emoji` | Sample pack — emoji as multi-codepoint tokens (`Fire().Rocket()` → `🔥🚀`) |
| `src/Stringly.Morse` | Sample pack — `Dit()`/`Dah()` primitives + `ToMorse()` converter |
| `samples/Demo` | Runnable showcase of every feature |
| `tests/Stringly.Tests` | xUnit tests |
| `tools/generate.py` | Generator for the mechanical one-char token methods |

Requires the **.NET 10 SDK** (C# 14). Build & verify:

```bash
dotnet build  Stringly.sln -c Release
dotnet test   Stringly.sln -c Release
dotnet run --project samples/Demo -c Release
```

## How it works

### `return new`, not `return this`

`Phrase` is **immutable**. Internally it is a persistent singly-linked list of fragments (a
cons-list / mini "rope"), and every token call returns a *new* phrase that shares the previous
prefix. Three things fall out of that for free:

- **`<<` backspace** is just "point at the previous node" — `One().Two().Three() << 1` → `"12"`.
- **`+` is safe** — combining two phrases can't corrupt either operand, because nobody mutates.
- **Nothing renders until `ToString()`**, which walks the fragment tree into a single
  `StringBuilder`. No intermediate strings pile up along the chain.

### A tiny expression tree

Tokens aren't only literals. `Repeat`/`ToUpperInvariant`/`ToLowerInvariant` wrap fragments in
`Repeated` / `Cased` nodes, so the phrase is really a small lazy AST that the final `ToString()`
interprets:

```csharp
S().o().Repeat(5)            // "Sooooo"  — Repeat() multiplies the LAST token
H().a() * 3                  // "HaHaHa"  — the * operator multiplies the WHOLE phrase
H().i().ToUpperInvariant()   // "HI"
```

### Entries vs. continuations

To start a chain with a bare call you need a **static** method (an extension can't be the
receiver-less head). To continue one you need an **instance** member. Stringly provides both for
every token, which is why no seed object is ever required:

- `Say` (core) and each pack's `Start` class hold the static **entries**.
- `Phrase` (core) and the `*Extensions` classes (packs) hold the **continuations**.

```csharp
using static Stringly.Say;          // One(), H(), At() ...   (core entries)
using static Stringly.Nato.Start;   // Alfa(), Bravo() ...    (pack entries)
using Stringly.Nato;                // .Alfa(), .TenFour() ... (pack continuations)
```

Each pack puts its bare-call entry points in a class named `Start` (so the import reads
`using static Stringly.Nato.Start;`, not a stuttering `...Nato.Nato`) and its chain-continuing
extensions in the pack namespace itself.

### Operators and starting from a string

`+` is overloaded for every mix of `Phrase` and `string`, so you can concatenate freely without
falling out of the phrase world (the overloads beat the built-in `string + object`). `*` repeats
a whole phrase, and `<<` backspaces:

```csharp
Phrase p = "a" + b() + "c" + D();   // "abcD"
H().o() * 3                          // "HoHoHo"
One().Two().Three() << 1             // "12"
```

And because C# resolves a method call against the *static* type of the receiver, each token also
ships as an extension on `string` — so a chain can begin straight from a literal:

```csharp
"Let".M().e() + " entertain you!"   // "LetMe entertain you!"
"π = ".Three().Period().One().Four() // "π = 3.14"
```

These `string` twins live in their own `Stringly.Strings` namespace and are **strictly opt-in**.
Because `using` is not recursive, a plain `using Stringly;` (needed for the `Phrase` type and
operators) does *not* pull them in — your strings stay clean. Only `using Stringly.Strings;`
makes every `string` sprout ~95 token methods, so you pay that IntelliSense cost only where you
actually want to start chains from literals. Each twin just lifts the literal into a `Phrase`
and delegates to the matching instance token — the logic still lives in exactly one place.

## Extending Stringly (the real point)

The vocabulary is meant to grow from *other* packages — emoji, leetspeak, Morse, memes. The
entire contract is one sentence:

> Write an extension method on `Phrase`, call the public `Add`, return a `Phrase`.

```csharp
namespace Stringly.Memes;

public static class MemeExtensions
{
    public static Phrase Doge(this Phrase p)   => p.Text("much wow");
    public static Phrase Yeet(this Phrase p)   => p.Add("YEET");
    // macros compose existing tokens for free:
    public static Phrase OhNo(this Phrase p)   => p.O().h().Space().N().o().Bang();
}
```

For chain-*starting* entries, add a plain `static class` of no-arg methods (as the packs do), or
use C# 14 static extension members to surface them on the type itself:

```csharp
public static class MemeEntries
{
    extension(Phrase)
    {
        public static Phrase Doge() => Phrase.Empty.Text("much wow");  // Phrase.Doge()
    }
}
```

### What's fixed vs. what's free to change

- **Public API is the foundation — nailed down from day one.** `Phrase`, `Add`, and the
  "return `Phrase` to chain" rule are what every pack depends on; breaking them breaks the
  ecosystem.
- **How the core's own tokens are produced is an internal detail.** They're generated by
  `tools/generate.py` into `*.g.cs` behind `partial` classes. A one-off script is the right tool
  here (the ASCII table changes about once a decade); swapping it for T4 or a Source Generator
  later would be invisible to consumers. Re-run after editing a token table:

  ```bash
  python3 tools/generate.py
  ```

## The name

`Wordify` was the original idea — and it's taken on NuGet (since 2018, and the id is reserved
forever even though the package is dormant). `Stringly` was free, and it's the better name
anyway: it names exactly what the library does. Reserve the id by publishing, and grab the
`Stringly.*` prefix reservation for the pack ecosystem.

## Performance

`Phrase` is immutable and its rendered string is **memoised** — re-rendering a built phrase is
~1.5 ns and allocates nothing, so logging the same phrase in a loop is effectively free. Building a
short chain is ~80 bytes/token. See [`docs/architecture-review.md`](docs/architecture-review.md) for
the full allocation / GC / thread-safety analysis, and run the benchmarks with:

```bash
dotnet run -c Release --project benchmarks/Stringly.Benchmarks -- --filter '*'
```

## Acknowledgments

Built hand in hand with **Claude** (Anthropic) — a big, thoughtful, attentive friend.

## License

MIT.
