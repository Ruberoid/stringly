namespace Stringly.Morse;

/// <summary>
/// Chain-<b>starting</b> entries for Morse. Import with <c>using static Stringly.Morse.Start;</c>.
/// </summary>
/// <remarks>
/// Morse can't reuse the <c>A()</c>..<c>Z()</c> token names: those already exist as instance
/// members on <see cref="Phrase"/> and an instance method silently wins over an extension of the
/// same name. So this pack works two ways instead: hand-built <see cref="MorseExtensions.Dit"/> /
/// <see cref="MorseExtensions.Dah"/> primitives, or the <see cref="MorseExtensions.ToMorse"/>
/// converter that encodes whatever text the phrase has accumulated.
/// </remarks>
public static class Start
{
    /// <summary>Starts a phrase with a dit (<c>.</c>).</summary>
    public static Phrase Dit() => Phrase.Empty.Add('.');

    /// <summary>Starts a phrase with a dah (<c>-</c>).</summary>
    public static Phrase Dah() => Phrase.Empty.Add('-');

    /// <summary>Encodes <paramref name="text"/> to Morse and starts a phrase with it.</summary>
    public static Phrase Of(string text) => Phrase.Empty.Add(MorseCode.Encode(text));
}

/// <summary>
/// Chain-<b>continuing</b> Morse tokens. Available with <c>using Stringly.Morse;</c>.
/// </summary>
public static class MorseExtensions
{
    /// <summary>Appends a dit (<c>.</c>).</summary>
    public static Phrase Dit(this Phrase p) => p.Add('.');

    /// <summary>Appends a dah (<c>-</c>).</summary>
    public static Phrase Dah(this Phrase p) => p.Add('-');

    /// <summary>Appends a gap (a single space) between Morse letters.</summary>
    public static Phrase Gap(this Phrase p) => p.Add(' ');

    /// <summary>
    /// Encodes everything the phrase has accumulated so far into Morse, e.g.
    /// <c>Say.Text("SOS").ToMorse()</c> renders <c>"... --- ..."</c>. Letters are separated by a
    /// space and words by <c>" / "</c>; characters with no Morse mapping are dropped.
    /// </summary>
    public static Phrase ToMorse(this Phrase p) => Phrase.Empty.Add(MorseCode.Encode(p.ToString()));
}

/// <summary>The International Morse Code table and encoder.</summary>
internal static class MorseCode
{
    private static readonly Dictionary<char, string> Table = new()
    {
        ['A'] = ".-", ['B'] = "-...", ['C'] = "-.-.", ['D'] = "-..", ['E'] = ".",
        ['F'] = "..-.", ['G'] = "--.", ['H'] = "....", ['I'] = "..", ['J'] = ".---",
        ['K'] = "-.-", ['L'] = ".-..", ['M'] = "--", ['N'] = "-.", ['O'] = "---",
        ['P'] = ".--.", ['Q'] = "--.-", ['R'] = ".-.", ['S'] = "...", ['T'] = "-",
        ['U'] = "..-", ['V'] = "...-", ['W'] = ".--", ['X'] = "-..-", ['Y'] = "-.--",
        ['Z'] = "--..",
        ['0'] = "-----", ['1'] = ".----", ['2'] = "..---", ['3'] = "...--", ['4'] = "....-",
        ['5'] = ".....", ['6'] = "-....", ['7'] = "--...", ['8'] = "---..", ['9'] = "----.",
        ['.'] = ".-.-.-", [','] = "--..--", ['?'] = "..--..", ['!'] = "-.-.--",
        ['/'] = "-..-.", ['@'] = ".--.-.", ['='] = "-...-",
    };

    /// <summary>Encodes text: letters joined by spaces, words by <c>" / "</c>, unknowns dropped.</summary>
    public static string Encode(string text)
    {
        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var encoded = words
            .Select(EncodeWord)
            .Where(w => w.Length > 0);
        return string.Join(" / ", encoded);
    }

    private static string EncodeWord(string word)
    {
        var codes = new List<string>();
        foreach (var ch in word.ToUpperInvariant())
        {
            if (Table.TryGetValue(ch, out var code))
            {
                codes.Add(code);
            }
        }

        return string.Join(' ', codes);
    }
}
