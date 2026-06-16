namespace Stringly;

/// <summary>
/// Static entry points. <c>using static Stringly.Say;</c> lets you start a chain with a bare
/// call — <c>One()</c>, <c>H()</c>, <c>At()</c> — because an extension method can't be the
/// receiver-less head of a chain, but a static method can.
/// </summary>
/// <remarks>
/// The bulk of the entries (one per ASCII token) live in the generated <c>Say.Tokens.g.cs</c>.
/// This file holds only the hand-written entries that take arguments or don't map to a glyph.
/// </remarks>
public static partial class Say
{
    /// <summary>Starts an empty phrase you can build on.</summary>
    public static Phrase Nothing() => Phrase.Empty;

    /// <summary>Starts a phrase with arbitrary literal text.</summary>
    public static Phrase Text(string s) => Phrase.Empty.Add(s);
}
