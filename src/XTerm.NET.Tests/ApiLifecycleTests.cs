using XTerm.Options;

namespace XTerm.Tests;

/// <summary>
/// Lifecycle and state that outlives a single sequence: what a terminal still believes after a
/// reset, after a dispose, or after a program leaves a mode set.
/// </summary>
[TestClass]
public class ApiLifecycleTests
{
    private static readonly string Esc = ((char)0x1B).ToString();

    private static Terminal NewTerminal(int cols = 20, int rows = 6) =>
        new(new TerminalOptions { Cols = cols, Rows = rows });

    [TestMethod]
    public void Terminal_is_disposable_and_disposing_twice_is_harmless()
    {
        // It always had the method; without the interface no using statement, DI container or
        // analyzer could see it.
        using (var terminal = NewTerminal())
        {
            terminal.Write("hello");
        }

        var second = NewTerminal();
        second.Dispose();
        second.Dispose();
    }

    [TestMethod]
    public void Writing_to_a_disposed_terminal_is_ignored_rather_than_thrown()
    {
        // Deliberate: a host reads its pty on a background thread, and disposing the control while
        // a read is in flight is ordinary. Throwing there would kill the read loop.
        var terminal = NewTerminal();
        terminal.Dispose();

        var ex = Record.Exception(() => terminal.Write("after"));
        ex.Should().BeNull();
    }

    [TestMethod]
    public void Reset_restores_the_charset_designations()
    {
        // ResetCharsets existed for this and was called from nowhere, so a program that
        // designated line drawing into G0 and died left the next one printing box characters.
        var terminal = NewTerminal();
        terminal.Write($"{Esc}(0");        // G0 = line drawing
        terminal.Write($"{Esc}c");         // RIS
        terminal.Write("q");

        (terminal.Buffer.Lines[0]![0].Content).Should().Be("q");
    }

    [TestMethod]
    public void Reset_restores_the_sixel_modes()
    {
        // They survived RIS, so DECRQM went on reporting mode 80 as set after a reset.
        var terminal = NewTerminal();
        terminal.Write($"{Esc}[?80h");
        terminal.SixelDisplayMode.Should().BeTrue();

        terminal.Write($"{Esc}c");
        terminal.SixelDisplayMode.Should().BeFalse();
        terminal.SixelPrivateColorRegisters.Should().BeTrue();
    }

    [TestMethod]
    public void Reverse_wraparound_moves_the_cursor_back_over_the_wrap()
    {
        // DECSET 45 was stored and reported and nothing read it, so a shell erasing a wrapped
        // command line stopped at the wrap.
        var terminal = NewTerminal(cols: 10);
        terminal.Write($"{Esc}[?45h");
        terminal.Write($"{Esc}[2;1H");     // row 2, column 1
        terminal.Write("\b");

        terminal.Buffer.Y.Should().Be(0);
        terminal.Buffer.X.Should().Be(9);
    }

    [TestMethod]
    public void A_notification_that_fails_to_build_is_not_raised()
    {
        // Missing braces meant only the inner if was guarded, so a failed build raised the event
        // anyway with null title AND null body.
        var terminal = NewTerminal();
        terminal.Options.KittyNotificationsEnabled = true;
        var raised = new List<string?>();
        terminal.NotificationReceived += (_, e) => raised.Add(e.Title);

        terminal.Write($"{Esc}]99;i=x:d=1;{Esc}\\");   // done, but nothing to show

        raised.Should().AllSatisfy(t => string.IsNullOrEmpty(t).Should().BeFalse());
    }

    [TestMethod]
    public void A_reset_is_not_undone_by_a_restore_of_what_was_saved_before_it()
    {
        // RIS reset the live charset tables but left the SAVED cursor context alone, so the reset
        // was one DECRC away from being undone: a program that designated line drawing, saved the
        // cursor and died left its designations reachable through the next program's restore.
        var terminal = NewTerminal();
        terminal.Write($"{Esc}(0");        // G0 = line drawing
        terminal.Write($"{Esc}7");         // DECSC, with line drawing designated
        terminal.Write($"{Esc}c");         // RIS
        terminal.Write($"{Esc}8");         // DECRC
        terminal.Write("q");

        (terminal.Buffer.Lines[terminal.Buffer.YBase]![0].Content).Should().Be("q");
    }

    [TestMethod]
    public void A_reset_clears_the_saved_cursor_on_the_screen_it_is_not_looking_at()
    {
        // DECSC state is per-screen, so a reset that cleared only the active buffer would leave
        // the other one loaded and reachable as soon as the application switched to it.
        var terminal = NewTerminal();
        terminal.Write($"{Esc}[?1049h");   // alternate screen
        terminal.Write($"{Esc}(0");
        terminal.Write($"{Esc}7");         // saved on the ALTERNATE screen
        terminal.Write($"{Esc}[?1049l");   // back to normal

        terminal.Write($"{Esc}c");         // RIS

        terminal.Write($"{Esc}[?1049h");
        terminal.Write($"{Esc}8");
        terminal.Write("q");

        (terminal.Buffer.Lines[terminal.Buffer.YBase]![0].Content).Should().Be("q");
    }

    [TestMethod]
    public void Reverse_wrap_stops_at_the_top_of_the_scrolling_region()
    {
        // Bounded at row 0, backspacing off the left edge on the region's first row moved the
        // cursor into the row above it -- a row the region exists to protect.
        var terminal = NewTerminal();
        terminal.Write($"{Esc}[?45h");     // reverse wraparound
        terminal.Write($"{Esc}[3;5r");     // DECSTBM: rows 3..5
        terminal.Write($"{Esc}[3;1H");     // top row of the region, column 1
        terminal.Write("\b");

        terminal.Buffer.Y.Should().Be(2);   // still on row 3 (0-based 2)
        terminal.Buffer.X.Should().Be(0);
    }

    [TestMethod]
    public void Reverse_wrap_lands_on_the_right_margin_not_the_screen_edge()
    {
        // The row above ends where the pane ends. Landing at Cols - 1 put the cursor outside the
        // DECSLRM region, and the next character with it.
        var terminal = NewTerminal(cols: 20);
        terminal.Write($"{Esc}[?45h");
        terminal.Write($"{Esc}[?69h");     // DECLRMM, so DECSLRM is honoured
        terminal.Write($"{Esc}[5;12s");    // left margin col 5, right margin col 12
        terminal.Write($"{Esc}[3;5H");     // row 3, at the left margin
        terminal.Write("\b");

        terminal.Buffer.Y.Should().Be(1);
        terminal.Buffer.X.Should().Be(11);   // right margin (col 12, 0-based 11)
    }

    [TestMethod]
    public void An_osc_payload_keeps_its_non_ascii_characters()
    {
        // The OSC control block is entered only for C0 and C1, so the arm it documents as
        // unreachable cannot restrict the payload -- everything from 0xA0 up reaches OscPut
        // through the ordinary path. Pinned because the code around it reads as though it might
        // not, and a title losing its accents would be a silent, ugly failure.
        var terminal = NewTerminal();
        string? title = null;
        terminal.TitleChanged += (_, e) => title = e.Title;

        terminal.Write($"{Esc}]0;café 日本語 — ✓{Esc}\\");

        title.Should().Be("café 日本語 — ✓");
    }
}
