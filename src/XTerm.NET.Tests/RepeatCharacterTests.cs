using XTerm.Options;

namespace XTerm.Tests;

/// <summary>
/// REP (<c>CSI Pn b</c>) — repeat the preceding graphic character.
///
/// <para>"Preceding" is meant literally, and that is most of the behaviour: it repeats the last
/// character printed, and only while the cursor is still where printing it left the cursor.</para>
/// </summary>
[TestClass]
public class RepeatCharacterTests
{
    private const string Esc = "\u001b";

    private static Terminal Fresh(int cols = 20, int rows = 5)
        => new(new TerminalOptions { Cols = cols, Rows = rows });

    private static string Row(Terminal terminal, int row = 0)
    {
        var line = terminal.Buffer.Lines[terminal.Buffer.YBase + row]!;
        var text = string.Concat(Enumerable.Range(0, terminal.Cols).Select(c => line[c].Content));
        return text.TrimEnd('\0', ' ');
    }

    [TestMethod]
    public void It_repeats_the_character_before_it()
    {
        var terminal = Fresh();
        terminal.Write($"a{Esc}[4b");

        Row(terminal).Should().Be("aaaaa");
    }

    /// <summary>No parameter means once, as every CSI with an omitted count does.</summary>
    [TestMethod]
    public void An_omitted_count_repeats_once()
    {
        var terminal = Fresh();
        terminal.Write($"x{Esc}[b");

        Row(terminal).Should().Be("xx");
    }

    /// <summary>It repeats only the last character, not the run before it.</summary>
    [TestMethod]
    public void Only_the_last_character_repeats()
    {
        var terminal = Fresh();
        terminal.Write($"ab{Esc}[3b");

        Row(terminal).Should().Be("abbbb");
    }

    /// <summary>
    /// A cursor move in between means there is no preceding character, so this does nothing rather
    /// than repeating whatever happens to be nearby.
    /// </summary>
    [TestMethod]
    public void A_cursor_move_in_between_cancels_it()
    {
        var terminal = Fresh();
        terminal.Write($"abc{Esc}[1;1H{Esc}[5b");

        Row(terminal).Should().Be("abc");
    }

    [TestMethod]
    public void With_nothing_printed_yet_it_does_nothing()
    {
        var terminal = Fresh();
        terminal.Write($"{Esc}[5b");

        Row(terminal).Should().Be("");
    }

    /// <summary>A newline moves the cursor, so it cancels it too.</summary>
    [TestMethod]
    public void A_newline_cancels_it()
    {
        var terminal = Fresh();
        terminal.Write($"a\r\n{Esc}[3b");

        Row(terminal, 0).Should().Be("a");
        Row(terminal, 1).Should().Be("");
    }

    /// <summary>The repeated character carries the attributes in force, as printing it again would.</summary>
    [TestMethod]
    public void The_repeat_takes_the_current_attributes()
    {
        var terminal = Fresh();
        terminal.Write($"{Esc}[31mr{Esc}[2b");

        var line = terminal.Buffer.Lines[terminal.Buffer.YBase]!;
        Row(terminal).Should().Be("rrr");
        for (var c = 0; c < 3; c++)
            line[c].Attributes.Should().Be(line[0].Attributes);
    }

    /// <summary>It wraps and scrolls like ordinary printing, because it goes through the same path.</summary>
    [TestMethod]
    public void It_wraps_at_the_edge()
    {
        var terminal = Fresh(cols: 5, rows: 3);
        terminal.Write($"z{Esc}[6b");

        Row(terminal, 0).Should().Be("zzzzz");
        Row(terminal, 1).Should().Be("zz");
    }

    /// <summary>
    /// A count from a hosted program is untrusted, so it is clamped to a screenful — past which
    /// every extra repeat only scrolls the earlier ones away and the screen looks identical.
    /// </summary>
    /// <remarks>
    /// The assertion is the clamp, stated exactly: 10 x 4 gives 40 repeats, which with the original
    /// character is 41 cells. That is four full rows and one over, so the screen scrolls once and
    /// the last row holds the single leftover. Reaching this assertion at all is the real subject —
    /// unclamped, the write never returns.
    /// </remarks>
    [TestMethod]
    public void An_enormous_count_is_clamped_to_a_screenful()
    {
        var terminal = Fresh(cols: 10, rows: 4);
        terminal.Write($"q{Esc}[2000000000b");

        Row(terminal, 0).Should().Be("qqqqqqqqqq");
        Row(terminal, 2).Should().Be("qqqqqqqqqq");
        Row(terminal, 3).Should().Be("q");
    }

    /// <summary>
    /// A multi-codepoint cluster repeats whole. It is stored as an interned id rather than in the
    /// cell, so this is the case that would come back empty if REP read the wrong field.
    /// </summary>
    [TestMethod]
    public void A_combining_cluster_repeats_whole()
    {
        var terminal = Fresh();
        terminal.Write($"e\u0301{Esc}[2b");

        Row(terminal).Should().Be("e\u0301e\u0301e\u0301");
    }

    /// <summary>
    /// The batched writer bypasses Print, so it keeps REP's record itself. Without that, the same
    /// input would repeat or not depending on which writer took it.
    /// </summary>
    [TestMethod]
    public void It_works_after_a_batched_run_and_agrees_with_the_slow_path()
    {
        var batched = Fresh();
        batched.Write($"hello{Esc}[3b");

        var perCharacter = Fresh();
        perCharacter.UseRunPrinting = false;
        perCharacter.Write($"hello{Esc}[3b");

        Row(batched).Should().Be("helloooo");
        Row(batched).Should().Be(Row(perCharacter));
    }

    /// <summary>And the same through the byte entry, which is a third writer again.</summary>
    /// <summary>
    /// A cursor moved away and BACK still cancels it. Position equality alone cannot see the
    /// excursion — this is what the dispatch-point clearing exists for, and it is how xterm.js
    /// behaves: any sequence between the print and the REP means there is no preceding
    /// character, whether or not the cursor ended up where it started.
    /// </summary>
    [TestMethod]
    public void A_cursor_move_that_returns_still_cancels_it()
    {
        var terminal = Fresh();
        terminal.Write($"a{Esc}[D{Esc}[C{Esc}[5b");

        Row(terminal).Should().Be("a");
    }

    /// <summary>Any sequence cancels it, not just movement — SGR leaves the cursor alone and still counts.</summary>
    [TestMethod]
    public void An_SGR_in_between_cancels_it()
    {
        var terminal = Fresh();
        terminal.Write($"a{Esc}[31m{Esc}[5b");

        Row(terminal).Should().Be("a");
    }

    /// <summary>An OSC in between cancels it too — the rule is any sequence, not any CSI.</summary>
    [TestMethod]
    public void An_OSC_in_between_cancels_it()
    {
        var terminal = Fresh();
        terminal.Write($"a{Esc}]0;title{Esc}\\{Esc}[5b");

        Row(terminal).Should().Be("a");
    }

    /// <summary>
    /// A chain of REPs keeps repeating: after a REP, the character it printed IS the preceding
    /// graphic character. This is ECMA-48's reading and the one deliberate divergence from
    /// xterm.js, whose parser happens to clear its record after every handler including REP's.
    /// </summary>
    [TestMethod]
    public void A_second_REP_repeats_again()
    {
        var terminal = Fresh();
        terminal.Write($"a{Esc}[2b{Esc}[2b");

        Row(terminal).Should().Be("aaaaa");
    }

    /// <summary>
    /// An emoji-presentation selector widens the previous cell and moves the cursor with it —
    /// and the record has to be taken AFTER that adjustment. Taken before, the saved position
    /// went stale and REP right after the selector silently did nothing.
    /// </summary>
    [TestMethod]
    public void It_repeats_a_cluster_widened_by_a_variation_selector()
    {
        var terminal = Fresh();
        terminal.Write("❤️");          // heart + VS16: width 1 becomes width 2
        terminal.Write($"{Esc}[2b");

        var line = terminal.Buffer.Lines[terminal.Buffer.YBase]!;
        line[0].Content.Should().Be("❤️");
        line[2].Content.Should().Be("❤️");
        line[4].Content.Should().Be("❤️");
        line[0].Width.Should().Be(2);
        terminal.Buffer.X.Should().Be(6);
    }

    [TestMethod]
    public void It_works_after_a_byte_run()
    {
        var terminal = Fresh();
        terminal.Write(System.Text.Encoding.UTF8.GetBytes($"hi{Esc}[3b"));

        Row(terminal).Should().Be("hiiii");
    }
}
