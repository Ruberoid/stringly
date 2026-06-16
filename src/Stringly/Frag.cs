using System.Text;

namespace Stringly;

/// <summary>
/// A fragment of a <see cref="Phrase"/>. A phrase is not a buffer of text but a tiny tree of
/// these nodes — nothing is materialised until <see cref="Phrase.ToString"/> walks the tree.
/// That laziness is what makes <c>+</c>, <c>&lt;&lt;</c> and the "expression math"
/// (<see cref="Phrase.Repeat"/>, case transforms) cheap and allocation-light: intermediate
/// strings are never built, only described.
/// </summary>
/// <remarks>
/// These are <see langword="sealed class"/>es, not records, on purpose: fragments are compared by
/// reference inside the list and never by value, so a record's generated equality / hash code /
/// <c>ToString</c> would be dead weight on a hot, allocation-sensitive type.
/// </remarks>
internal abstract class Frag
{
    /// <summary>Append this fragment's materialised text to <paramref name="sb"/>.</summary>
    internal abstract void Write(StringBuilder sb);

    /// <summary>
    /// A lower-bound estimate of the rendered length, in UTF-16 code units, used only to pre-size
    /// the <see cref="StringBuilder"/>. It is a hint, not an exact length: case transforms can change
    /// length for some Unicode (e.g. 'ß' → "SS"), so never treat this as authoritative.
    /// </summary>
    internal abstract int CapacityHint { get; }
}

/// <summary>A literal chunk of text — the leaf of the tree.</summary>
internal sealed class Lit(string value) : Frag
{
    private readonly string _value = value;

    internal override void Write(StringBuilder sb) => sb.Append(_value);

    internal override int CapacityHint => _value.Length;
}

/// <summary>Repeats <paramref name="inner"/> <paramref name="n"/> times (e.g. <c>TenTimes()</c>).</summary>
internal sealed class Repeated(Frag inner, int n) : Frag
{
    internal override void Write(StringBuilder sb)
    {
        for (var i = 0; i < n; i++)
        {
            inner.Write(sb);
        }
    }

    internal override int CapacityHint => n <= 0 ? 0 : inner.CapacityHint * n;
}

/// <summary>Applies a case transform to whatever <paramref name="inner"/> renders to.</summary>
internal sealed class Cased(Frag inner, CaseMode mode) : Frag
{
    internal override void Write(StringBuilder sb)
    {
        var scratch = new StringBuilder(inner.CapacityHint);
        inner.Write(scratch);
        var text = scratch.ToString();
        sb.Append(mode == CaseMode.Lower
            ? text.ToLowerInvariant()
            : text.ToUpperInvariant());
    }

    internal override int CapacityHint => inner.CapacityHint;
}

internal enum CaseMode
{
    Lower,
    Upper,
}
