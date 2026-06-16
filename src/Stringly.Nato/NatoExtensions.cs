namespace Stringly.Nato;

/// <summary>
/// Hand-written NATO macros — the part that isn't a mechanical one-char token. A "complex
/// expression" in Stringly is just composition of simpler tokens, so these read like sentences.
/// The per-letter tokens (both entries and continuations) are generated into <c>Nato.g.cs</c>;
/// this is a separate partial of the same continuation class.
/// </summary>
public static partial class NatoExtensions
{
    /// <summary>Radio code "10-4" (message received).</summary>
    public static Phrase TenFour(this Phrase p) => p.Add("10-4");

    /// <summary>Radio prompt "do you copy?" spliced in as text.</summary>
    public static Phrase DoYouCopy(this Phrase p) => p.Text("do you copy").Question();
}
