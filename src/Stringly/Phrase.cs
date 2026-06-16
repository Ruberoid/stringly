using System.Text;

namespace Stringly;

/// <summary>
/// The accumulator at the heart of Stringly — an <b>immutable</b> fluent string builder.
/// </summary>
/// <remarks>
/// <para>
/// Internally a phrase is a persistent singly-linked list of <see cref="Frag"/> segments
/// (newest at the head). Appending pushes a node and shares the whole previous prefix, so every
/// operation is O(1) and never mutates an existing phrase — which is exactly why <c>+</c> can
/// combine two phrases without corrupting either operand, and why <c>&lt;&lt;</c> (backspace) is
/// a one-liner: just point at the previous node.
/// </para>
/// <para>
/// Nothing is rendered until <see cref="ToString"/>, which reverses the segments and walks the
/// fragment tree into a single <see cref="StringBuilder"/>. No intermediate strings are produced
/// along the chain.
/// </para>
/// <para>
/// <b>Extension contract.</b> To grow the vocabulary from another package, write extension methods
/// on <see cref="Phrase"/> that call the public <see cref="Add(char)"/> / <see cref="Add(string)"/>
/// and return a <see cref="Phrase"/>. That is the entire surface a community pack needs.
/// </para>
/// </remarks>
public sealed partial class Phrase
{
    private sealed record Node(Frag Value, Node? Prev);

    private readonly Node? _head;

    private Phrase(Node? head) => _head = head;

    /// <summary>The empty phrase — the root every chain ultimately grows from.</summary>
    public static readonly Phrase Empty = new((Node?)null);

    /// <summary><see langword="true"/> when no fragments have been added yet.</summary>
    public bool IsEmpty => _head is null;

    private Phrase Push(Frag fragment) => new(new Node(fragment, _head));

    // ---- Public extension contract -------------------------------------------------------------

    /// <summary>Appends a single character and returns a new phrase. The primitive for char tokens.</summary>
    public Phrase Add(char c) => Push(new Lit(Chars.Of(c)));

    /// <summary>Appends a run of text and returns a new phrase. The primitive for word/string tokens.</summary>
    public Phrase Add(string? s) => string.IsNullOrEmpty(s) ? this : Push(new Lit(s));

    /// <summary>Appends arbitrary literal text mid-chain — the idiomatic way to splice a string in.</summary>
    public Phrase Text(string? s) => Add(s);

    // ---- Editing / expression math -------------------------------------------------------------

    /// <summary>Removes the last <paramref name="count"/> fragments (the <c>&lt;&lt;</c> backspace).</summary>
    public Phrase Backspace(int count = 1)
    {
        var head = _head;
        for (var i = 0; i < count && head is not null; i++)
        {
            head = head.Prev;
        }

        return new Phrase(head);
    }

    /// <summary>Repeats the most recently added fragment <paramref name="times"/> times.</summary>
    public Phrase Repeat(int times)
    {
        if (_head is null || times == 1)
        {
            return this;
        }

        return new Phrase(_head with { Value = new Repeated(_head.Value, times) });
    }

    /// <summary>Repeats the <b>whole</b> phrase <paramref name="times"/> times (see the <c>*</c> operator).</summary>
    public Phrase Times(int times)
    {
        if (_head is null || times == 1)
        {
            return this;
        }

        return new Phrase(new Node(new Repeated(Flatten(_head), times), null));
    }

    /// <summary>Lowercases the whole accumulated phrase (lazily, at render time).</summary>
    public Phrase ToLowerInvariant() => MapAll(CaseMode.Lower);

    /// <summary>Uppercases the whole accumulated phrase (lazily, at render time).</summary>
    public Phrase ToUpperInvariant() => MapAll(CaseMode.Upper);

    private Phrase MapAll(CaseMode mode)
    {
        if (_head is null)
        {
            return this;
        }

        // Collapse the current chain into one transformed fragment.
        return new Phrase(new Node(new Cased(Flatten(_head), mode), null));
    }

    /// <summary>Concatenates another phrase onto this one, sharing both fragment trees.</summary>
    public Phrase Concat(Phrase other)
    {
        var result = this;
        foreach (var fragment in InOrder(other._head))
        {
            result = result.Push(fragment);
        }

        return result;
    }

    // ---- Rendering -----------------------------------------------------------------------------

    /// <summary>Materialises the phrase. This is the single place where text is actually built.</summary>
    public override string ToString()
    {
        var sb = new StringBuilder();
        foreach (var fragment in InOrder(_head))
        {
            fragment.Write(sb);
        }

        return sb.ToString();
    }

    private static Frag Flatten(Node? head)
    {
        var sb = new StringBuilder();
        foreach (var fragment in InOrder(head))
        {
            fragment.Write(sb);
        }

        return new Lit(sb.ToString());
    }

    private static IEnumerable<Frag> InOrder(Node? head)
    {
        // Segments are stored newest-first; reverse them to oldest-first for rendering.
        var stack = new Stack<Frag>();
        for (var node = head; node is not null; node = node.Prev)
        {
            stack.Push(node.Value);
        }

        return stack;
    }

    // ---- Operators -----------------------------------------------------------------------------

    /// <summary>Implicit render to <see cref="string"/> — lets a phrase be used wherever a string is.</summary>
    public static implicit operator string(Phrase phrase) => phrase.ToString();

    /// <summary>Implicit lift of a <see cref="string"/> into a phrase, so a literal can start a chain.</summary>
    public static implicit operator Phrase(string s) => Empty.Add(s);

    /// <summary>Glue two phrases. More specific than the built-in <c>string + object</c>, so it wins.</summary>
    public static Phrase operator +(Phrase a, Phrase b) => a.Concat(b);

    /// <summary>Glue a trailing literal without dropping out of the phrase world.</summary>
    public static Phrase operator +(Phrase a, string s) => a.Add(s);

    /// <summary>Glue a leading literal; beats <c>string + object</c> because the param type is exact.</summary>
    public static Phrase operator +(string s, Phrase b) => Empty.Add(s).Concat(b);

    /// <summary>Backspace operator: <c>phrase &lt;&lt; n</c> erases the last <c>n</c> fragments.</summary>
    public static Phrase operator <<(Phrase phrase, int count) => phrase.Backspace(count);

    /// <summary>Repeat operator: <c>phrase * n</c> repeats the whole phrase, e.g. <c>H().a() * 3</c> == "HaHaHa".</summary>
    public static Phrase operator *(Phrase phrase, int times) => phrase.Times(times);

    /// <summary>Repeat operator (commutative form): <c>n * phrase</c>.</summary>
    public static Phrase operator *(int times, Phrase phrase) => phrase.Times(times);
}
