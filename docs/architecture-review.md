# Architecture review — Stringly

A deep review of allocations, GC pressure, `ToString()` cost, thread-safety, memory leaks and
API durability, for a library imagined to run at scale (tens of thousands of consumers, the emoji
pack used in hot loops: logs, UI). Severity is tagged **[CRITICAL] / [IMPORTANT] / [MINOR]**.
Numbers are estimates for x64 .NET 10 (16-byte object header, 8-byte reference); confirm the order
of magnitude with BenchmarkDotNet `[MemoryDiagnoser]`.

> **Status:** the "do now" items are **implemented** (see *Implementation status*) and the estimates
> are **confirmed with BenchmarkDotNet** (see *Measured*). The body below is the original review,
> kept as the rationale of record.

## Summary: what's good

The design is sound; most findings are "right architecture, under-tuned implementation":

- Immutable cons-list with prefix sharing — snoc really is O(1), and `+`/`<<` can't corrupt operands.
- `Chars` pool instead of `string.Intern` — the correct call (no leak; see §4).
- The lazy `Frag` AST builds no intermediate strings along the chain.
- No hidden mutability, no growing static caches, no events, no long-lived closures.

The real pain was the hot `ToString()` path and the choice of `record` for `Frag`.

## 1. Per-token allocation profile

`Add(char)` → `Push(new Lit(Chars.Of(c)))` → `new Node(...)` → `new Phrase(...)`. Per token:
`Lit` (~24 B) + `Node` (~32 B) + `Phrase` (~24 B) = **~80 B / 3 objects**; the string is shared
from the pool (0 B). For emoji, `Add(string)` uses the assembly's interned literal `"🔥"` — no new
string, so still 3 objects. A chain of N tokens is `3·N` objects. The build profile is healthy;
the optimization target is rendering, not building.

## 2. `ToString()` — the main problem [CRITICAL]

Invoked implicitly via `operator string` — in interpolation, logging, every loop iteration — and
**nothing was cached**. Each call allocated: `new Stack<Frag>()` (plus array growth), a boxed
`IEnumerable<Frag>` enumerator, `new StringBuilder()` with no capacity; `Cased` additionally
allocated a scratch builder + intermediate strings. ~6–8 garbage objects per render.

- **2a. Lazy memoisation** into a single reference field `_rendered`. A Phrase is immutable, so the
  string is deterministic. The race between two threads rendering a shared Phrase is **benign**: a
  reference-sized write is atomic and both threads compute the identical string — no lock needed
  (`Lazy<T>`/lock would defeat the point). Pin it down with a comment.
- **2b. Pre-size the `StringBuilder`** with one pass over the AST. Caveat: a `Cased` length is only
  an estimate (casing can change length, e.g. 'ß'→"SS"), so it's a capacity *hint*, not exact.
- **2c. Drop the `Stack` + boxed enumerator** in `InOrder` — reverse into one right-sized `Frag[]`.

## 3. `Concat` / `+` — do we need a rope node? [recommendation: NO]

`Concat` is O(M) in the right operand plus an extra `Stack`. A rope node would give O(1) `+` but
complicate rendering (tree walk) and `<<` (right-spine descent). The load profile (snoc + rare `+`
with a short right operand) doesn't justify it; `phrase + "literal"` is already O(1) (`Add`). Just
drop the `Stack` from `Concat`. Nit: the docstring claimed "sharing both trees" — the right tree is
actually copied fragment-by-fragment.

## 4. Leaks / unbounded retention [verdict: NO real leaks]

`Chars.Ascii` and `MorseCode.Table` are fixed-size, never grow, and aren't `Intern`. No growing
static caches/events/long-lived closures. `<<`/Backspace doesn't retain the "newer" nodes; a live
original Phrase keeps its own chain — that's a correct persistent structure, not a leak.
Memoisation adds +len bytes per rendered phrase, living as long as the phrase — a deliberate
memory-for-CPU trade.

## 5. Thread-safety [verdict: safe today]

All fields are `readonly`/init-only with no hidden mutability, so a shared Phrase is thread-safe.
It only breaks under a naive cache: cache into **one** reference field with a single atomic write
and an idempotent computation (see 2a); a multi-field "lazy" flag would introduce a reordering race.

## 6. Emoji hot path — `Add(string)` [IMPORTANT]

`.Fire()` → `Add("🔥")`: 3 objects, zero string traversal, zero surrogate work; the literal isn't
allocated. Building is already optimal. For capacity use `string.Length` (UTF-16) — exactly the
right hint for `StringBuilder`; do NOT count "real" code points.

## 7. Micro

- **[IMPORTANT] `record` → `sealed class` for `Frag`/`Lit`/`Repeated`/`Cased`**: generated equality/
  hash/`ToString`/`EqualityContract` are dead weight on a hot type and never used. The types are
  `internal`, so consumers are unaffected.
- **[IMPORTANT] boxed enumerator** in `InOrder` (see 2c).
- **[MINOR]** `Repeat(0)`/negative → empty render (confirm intentional). Morse LINQ is not a hot
  path — readability wins, leave it.

## 8. API durability

`Phrase`/`Add`/"return a Phrase" is set in stone — don't break it. `Phrase` is already `sealed` and
internals are `internal`, which is why every perf change here fits the unchanged public contract.
Recommendations: SemVer discipline; consider **independent pack versioning** before third-party
authors arrive (a shared `Version` couples them today); `operator Phrase(string? s)` for nullability
strictness. Mismatch: `EmojiExtensions` was `partial` and promised a hand-written partial that
doesn't exist — fix the generator comment.

## Prioritised plan

**Now (high ROI, API-safe):** 2a memoisation · 2b capacity · 7 `record`→`class` · 2c drop
`Stack`/boxing · fix misleading comments.

**Later:** optimise `Cased` (rarely hit after memoisation) · BenchmarkDotNet `[MemoryDiagnoser]` ·
independent pack versioning policy.

**Do NOT (over-engineering):** rope/Concat O(1) node · merge `Lit`+`Node` · code-point emoji length
· rewriting Morse without LINQ.

**Bottom line:** no leaks, thread-safety is correct, building is healthy; all the value is in the hot
`ToString()`, and it fits inside the unchanged public contract.

---

## Implementation status (done)

All "do now" items are implemented, the public contract is untouched, 17/17 tests green:

- ✅ **2a** — `Phrase._rendered` lazy cache with a benign-race comment (`Phrase.cs`).
- ✅ **2b** — `Frag.CapacityHint` + pre-sized `StringBuilder` in `Render` (`Frag.cs`, `Phrase.cs`).
- ✅ **2c** — `InOrder` (Stack + IEnumerable) replaced by `OldestFirst` → one `Frag[]`, no boxing.
- ✅ **7** — `Frag`/`Lit`/`Repeated`/`Cased`: `record` → `sealed class`.
- ✅ Comments: `Concat` (copies the right tree, doesn't share) and the pack generator ("macros, if any").

Deliberately NOT done: rope node, `Lit`+`Node` merge, code-point length, Morse rewrite.

## Measured (BenchmarkDotNet, ShortRun)

`benchmarks/Stringly.Benchmarks` (`[MemoryDiagnoser]`). Run:
`dotnet run -c Release --project benchmarks/Stringly.Benchmarks -- --filter '*'`.

| Method               |       Mean | Allocated |
|----------------------|-----------:|----------:|
| Build_And_Render     |  160.8 ns  |    440 B  |
| Render_Prebuilt      |    1.5 ns  |      0 B  |
| Render_1000x         |  320.3 ns  |      0 B  |
| Build_Ascii_Chain    |  253.2 ns  |    840 B  |
| Concat_Two_Phrases   |  147.6 ns  |    536 B  |

Takeaways:
- **Memoisation confirmed**: re-rendering an already-built phrase is **~1.5 ns, 0 B**; rendering it
  1000× allocates **0 B** total. The "emoji in a logging loop" scenario no longer allocates.
- The ~80 B/token estimate holds: 840 B / 10 tokens = **84 B/token**.
- These are ShortRun (a quick pass); drop `--job short` for accurate figures.
