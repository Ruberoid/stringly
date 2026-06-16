using Stringly;
using Stringly.Nato;
using Stringly.Greek;
using Stringly.Emoji;
using Stringly.Morse;
using Stringly.Strings;          // opt-in: brings the "Let".M() string-start twins into scope
using static Stringly.Say;
using static Stringly.Nato.Start;
using static Stringly.Greek.Start;
using static Stringly.Emoji.Start;
using static Stringly.Morse.Start;

Console.OutputEncoding = System.Text.Encoding.UTF8;

// --- The core idea: spell a string out one token at a time -----------------------------------
Console.WriteLine(One().Three().Three().Seven());   // 1337
Console.WriteLine(Four().Two());                     // 42
Console.WriteLine(H().e().l().l().o());              // Hello

// --- Symbols and spliced words ---------------------------------------------------------------
Console.WriteLine(Text("user").At().Text("example").Period().c().o().m());   // user@example.com

// --- Expression math: repeat last vs. repeat whole, case, backspace --------------------------
Console.WriteLine(S().o().Repeat(5).Text(" good"));  // Sooooo good  (Repeat = last token only)
Console.WriteLine(H().a() * 3);                       // HaHaHa       (* = whole phrase)
Console.WriteLine(3 * Say.Text("ho"));                // hohoho       (commutative form)
Console.WriteLine(H().i().ToUpperInvariant());        // HI
Console.WriteLine(One().Two().Three() << 1);          // 12  (<< erases the last token)

// --- Operators: + concatenates strings and phrases freely, staying inside Phrase -------------
Console.WriteLine("a" + b() + "c" + D());             // abcD
Phrase pi = "3" + Period() + Say.Text("14159");       // 3.14159
Console.WriteLine(pi);

// --- Start a chain straight from a string literal (string-extension twins) -------------------
Console.WriteLine("Let".M().e() + " entertain you!"); // LetMe entertain you!
Console.WriteLine("π = ".Three().Period().One().Four());   // π = 3.14

// --- NATO pack: its tokens start and continue the very same chain, no seed needed ------------
Console.WriteLine(Alfa().Bravo().Charlie());          // ABC
Console.WriteLine(Say.Text("base, ").DoYouCopy());    // base, do you copy?
Console.WriteLine(Say.Text("roger, ").TenFour());     // roger, 10-4

// --- Greek pack: tokens aren't limited to ASCII ----------------------------------------------
Console.WriteLine(Alpha().Beta().Gamma());            // αβγ
Console.WriteLine(Pi().Eq().Text("3.14159"));         // π=3.14159
Console.WriteLine(Omega().ToUpperInvariant());        // Ω  (case math over Unicode)

// --- Emoji pack: multi-codepoint tokens ------------------------------------------------------
Console.WriteLine(Fire().Rocket().Hundred());         // 🔥🚀💯
Console.WriteLine(Say.Text("LGTM ").ThumbsUp());      // LGTM 👍
Console.WriteLine(Heart().Repeat(3));                 // ❤️❤️❤️

// --- Morse pack: dits & dahs by hand, or convert accumulated text ----------------------------
Console.WriteLine(Of("SOS"));                         // ... --- ...
Console.WriteLine(Say.Text("Hello World").ToMorse()); // .... . .-.. .-.. --- / .-- --- .-. .-.. -..
Console.WriteLine(Dit().Dit().Dit());                 // ...
