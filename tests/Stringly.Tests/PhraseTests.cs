using Stringly;
using Stringly.Nato;
using Stringly.Greek;
using Stringly.Emoji;
using Stringly.Morse;
using Stringly.Strings;
using Xunit;
using static Stringly.Say;
using static Stringly.Nato.Start;
using static Stringly.Greek.Start;
using static Stringly.Emoji.Start;
using static Stringly.Morse.Start;

namespace Stringly.Tests;

public class PhraseTests
{
    [Fact]
    public void Digits_chain_into_a_number()
    {
        Assert.Equal("1337", One().Three().Three().Seven().ToString());
        Assert.Equal("42", Four().Two().ToString());
    }

    [Fact]
    public void Letters_are_case_sensitive_and_chain_into_words()
    {
        Assert.Equal("Hello", H().e().l().l().o().ToString());
    }

    [Fact]
    public void Symbols_and_text_splice_together()
    {
        Assert.Equal("user@x.com", Text("user").At().Text("x").Period().c().o().m().ToString());
    }

    [Fact]
    public void Implicit_conversion_to_string_works()
    {
        string s = One().Two().Three();
        Assert.Equal("123", s);
    }

    [Fact]
    public void Repeat_repeats_only_the_last_token()
    {
        Assert.Equal("Soooo", S().o().Repeat(4).ToString());
    }

    [Fact]
    public void Case_transforms_apply_to_the_whole_phrase()
    {
        Assert.Equal("HI", H().i().ToUpperInvariant().ToString());
        Assert.Equal("hi", H().I().ToLowerInvariant().ToString());
    }

    [Fact]
    public void Backspace_operator_erases_trailing_tokens()
    {
        Assert.Equal("12", (One().Two().Three() << 1).ToString());
        Assert.Equal("1", (One().Two().Three() << 2).ToString());
        Assert.Equal("", (One().Two() << 5).ToString());
    }

    [Fact]
    public void Phrases_are_immutable_so_operands_are_not_mutated()
    {
        var ab = A().B();
        var abc = ab.C();

        Assert.Equal("AB", ab.ToString());   // unchanged by the C() call
        Assert.Equal("ABC", abc.ToString());
    }

    [Fact]
    public void Operators_keep_you_inside_the_phrase_world()
    {
        Phrase pi = "3" + Period() + Say.Text("14159");
        Assert.Equal("3.14159", pi.ToString());

        Phrase joined = Four().Two() + " " + (Phrase)"ok";
        Assert.Equal("42 ok", joined.ToString());
    }

    [Fact]
    public void Plus_concatenates_strings_and_phrases_in_any_order()
    {
        Phrase p = "a" + b() + "c" + D();
        Assert.Equal("abcD", p.ToString());
    }

    [Fact]
    public void Chains_can_start_from_a_string_literal()
    {
        Assert.Equal("LetMe entertain you!", ("Let".M().e() + " entertain you!").ToString());
        Assert.Equal("π = 3.14", "π = ".Three().Period().One().Four().ToString());
    }

    [Fact]
    public void Nato_pack_starts_and_continues_a_chain_without_a_seed()
    {
        Assert.Equal("ABC", Alfa().Bravo().Charlie().ToString());
        Assert.Equal("10-4", Phrase.Empty.TenFour().ToString());
        Assert.Equal("do you copy?", Phrase.Empty.DoYouCopy().ToString());
    }

    [Fact]
    public void Greek_pack_emits_unicode_glyphs()
    {
        Assert.Equal("αβγ", Alpha().Beta().Gamma().ToString());
        Assert.Equal("π", Pi().ToString());
        Assert.Equal("Ω", Omega().ToUpperInvariant().ToString());
    }

    [Fact]
    public void Star_operator_repeats_the_whole_phrase_unlike_Repeat()
    {
        Assert.Equal("HaHaHa", (H().a() * 3).ToString());   // whole phrase
        Assert.Equal("hoho", (2 * Say.Text("ho")).ToString());
        Assert.Equal("ABAB", A().B().Times(2).ToString());

        Assert.Equal("ABB", A().B().Repeat(2).ToString());  // last token only — the contrast
    }

    [Fact]
    public void Emoji_pack_appends_multicodepoint_tokens()
    {
        Assert.Equal("🔥🚀💯", Fire().Rocket().Hundred().ToString());
        Assert.Equal("❤️❤️❤️", Heart().Repeat(3).ToString());
    }

    [Fact]
    public void Morse_pack_encodes_text_and_builds_by_hand()
    {
        Assert.Equal("... --- ...", Of("SOS").ToString());
        Assert.Equal("... --- ...", Say.Text("SOS").ToMorse().ToString());
        Assert.Equal(".... .. / .- .-.. .-..", Say.Text("HI ALL").ToMorse().ToString());
        Assert.Equal("...", Dit().Dit().Dit().ToString());
    }

    [Fact]
    public void Empty_phrase_renders_to_empty_string()
    {
        Assert.Equal("", Nothing().ToString());
        Assert.True(Phrase.Empty.IsEmpty);
    }
}
