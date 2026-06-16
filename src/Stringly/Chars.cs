namespace Stringly;

/// <summary>
/// Cache of single-character strings for the ASCII range.
/// Every '1' produced anywhere in a program shares one <see cref="string"/> instance,
/// so single-token chains don't allocate a fresh string per call.
/// </summary>
/// <remarks>
/// We deliberately do <b>not</b> use <c>string.Intern</c>: CLR-interned strings live for the
/// lifetime of the AppDomain, which would be a leak for anything dynamic. This table is a fixed,
/// bounded 128-entry pool — interning by hand, only for ASCII.
/// </remarks>
internal static class Chars
{
    private static readonly string[] Ascii = BuildAscii();

    private static string[] BuildAscii()
    {
        var table = new string[128];
        for (var i = 0; i < table.Length; i++)
        {
            table[i] = ((char)i).ToString();
        }

        return table;
    }

    /// <summary>Returns a shared string for ASCII chars, or a fresh one for everything else.</summary>
    public static string Of(char c) => c < 128 ? Ascii[c] : c.ToString();
}
