using System.Text;

namespace Stringly;

/// <summary>
/// A fragment of a <see cref="Phrase"/>. A phrase is not a buffer of text but a tiny tree of
/// these nodes — nothing is materialised until <see cref="Phrase.ToString"/> walks the tree.
/// That laziness is what makes <c>+</c>, <c>&lt;&lt;</c> and the "expression math"
/// (<see cref="Phrase.Repeat"/>, case transforms) cheap and allocation-light: intermediate
/// strings are never built, only described.
/// </summary>
internal abstract record Frag
{
    /// <summary>Append this fragment's materialised text to <paramref name="sb"/>.</summary>
    internal abstract void Write(StringBuilder sb);
}

/// <summary>A literal chunk of text — the leaf of the tree.</summary>
internal sealed record Lit(string Value) : Frag
{
    internal override void Write(StringBuilder sb) => sb.Append(Value);
}

/// <summary>Repeats <paramref name="Inner"/> <paramref name="N"/> times (e.g. <c>TenTimes()</c>).</summary>
internal sealed record Repeated(Frag Inner, int N) : Frag
{
    internal override void Write(StringBuilder sb)
    {
        for (var i = 0; i < N; i++)
        {
            Inner.Write(sb);
        }
    }
}

/// <summary>Applies a case transform to whatever <paramref name="Inner"/> renders to.</summary>
internal sealed record Cased(Frag Inner, CaseMode Mode) : Frag
{
    internal override void Write(StringBuilder sb)
    {
        var scratch = new StringBuilder();
        Inner.Write(scratch);
        var text = scratch.ToString();
        sb.Append(Mode == CaseMode.Lower
            ? text.ToLowerInvariant()
            : text.ToUpperInvariant());
    }
}

internal enum CaseMode
{
    Lower,
    Upper,
}
