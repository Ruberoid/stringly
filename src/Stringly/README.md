# Stringly

> The most over-engineered way to build a string. On purpose.

```csharp
using static Stringly.Say;

Console.WriteLine(One().Three().Three().Seven());   // 1337
Console.WriteLine(H().e().l().l().o());              // Hello
Console.WriteLine(Four().Two());                     // 42, the answer to everything
```

`"strongly typed"` is good engineering. `"stringly typed"` — shoving everything into
strings — is the running joke of the type-system world. **Stringly** takes that joke and
commits to it completely: a fluent API where you spell strings out one token at a time.

## How it reads

Each token is a method that appends one character and returns a `Phrase`, so it keeps chaining:

```csharp
using static Stringly.Say;

string motd = H().i().Bang();        // "Hi!"
string pi   = "3" + Period() + "14159".One().Four();   // operator math: "3.1415914"... (see below)
```

## What you can do

| Feature | Example | Result |
|---|---|---|
| Digits & letters | `One().Two().Three()` / `H().e().y()` | `"123"` / `"Hey"` |
| Symbols | `At().Hash().Star()` | `"@#*"` |
| Splice a word | `Say.Text("copy").Question()` | `"copy?"` |
| Repeat last token | `S().o().Repeat(5)` | `"Soooooo"` |
| Case math | `H().i().ToUpperInvariant()` | `"HI"` |
| Backspace (`<<`) | `One().Two().Three() << 1` | `"12"` |
| Concatenation | `Four().Two() + " " + ("o".O().k())` | `"42 ok"` |

## The extension contract (this is the real point)

The vocabulary is meant to grow from *other* packages. The whole contract is:

> Write an extension method on `Phrase`, call the public `Add`, return a `Phrase`.

```csharp
namespace Stringly.Nato;

public static class NatoExtensions
{
    public static Phrase Alpha(this Phrase p) => p.Add('A');
    public static Phrase Bravo(this Phrase p) => p.Add('B');

    // macros compose existing tokens for free:
    public static Phrase TenFour(this Phrase p) => p.Add("10-4");
}
```

Consumers reference `Stringly` + your pack and everything chains in one expression. With
C# 14 `extension` members a pack can even contribute its own chain-starting entry points.

## Notes

- `Phrase` is **immutable**: every call returns a new phrase, nothing is rendered until
  `ToString()`. That makes `+` and `<<` safe and keeps intermediate strings from piling up.
- `using static Stringly.Say;` is what unlocks the bare `One()` entry calls.
- Requires .NET 10 / C# 14.

MIT. Built for fun.
