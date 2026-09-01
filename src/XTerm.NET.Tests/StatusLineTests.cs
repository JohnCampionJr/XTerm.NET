using XTerm.Options;

namespace XTerm.Tests;

/// <summary>
/// The DEC status line: an extra row a program selects with DECSSDT and writes to with DECSASD.
/// </summary>
/// <remarks>
/// <para>Both controls used to be parsed, stored for DECRQSS, and otherwise ignored — which was
/// worse than not implementing them. DECRQSS answered with the stored value, so a program asking
/// whether its status line had been accepted was told yes, wrote to it, and the text went wherever
/// the cursor happened to be: into the middle of the application's own display. vttest's simple
/// status-line test shows it as <c>TEXT IN THE STATUS LINEThere should be TEXT IN THE STATUS LINE</c>
/// on a single row.</para>
/// <para>The row is deliberately NOT one of the terminal's <c>Rows</c>. An application told it has
/// N rows must have N rows it can write to, and every size report is computed from that count.</para>
/// </remarks>
public class StatusLineTests
{
    private static Terminal Fresh() => new(new TerminalOptions { Cols = 40, Rows = 5 });

    private static string Csi(string body) => "\u001b[" + body;

    /// <summary>DECSSDT: 0 none, 1 indicator, 2 host-writable.</summary>
    private static string Type(int t) => Csi($"{t}$~");

    /// <summary>DECSASD: 0 main display, 1 status line.</summary>
    private static string Select(int d) => Csi($"{d}$}}");

    private static string Row(Terminal t, int row) =>
        (t.Buffer.GetLine(t.Buffer.ViewportY + row)?.TranslateToString(true) ?? "").TrimEnd();

    private static string StatusText(Terminal t) =>
        (t.StatusLine?.TranslateToString(true) ?? "").TrimEnd();

    [Fact]
    public void There_is_no_status_line_until_a_program_asks_for_one()
    {
        var terminal = Fresh();

        Assert.Null(terminal.StatusLine);
        Assert.Equal(0, terminal.StatusDisplayType);
        Assert.False(terminal.StatusLineActive);
    }

    [Fact]
    public void Text_written_to_the_status_line_stays_out_of_the_display()
    {
        // vttest's own sequence, and the bug it exposed: the capitals belong on a row of their own.
        var terminal = Fresh();

        terminal.Write("There should be TEXT IN THE STATUS LINE");
        terminal.Write(Type(2));
        terminal.Write(Select(1));
        terminal.Write("TEXT IN THE STATUS LINE");
        terminal.Write(Select(0));

        Assert.Equal("There should be TEXT IN THE STATUS LINE", Row(terminal, 0));
        Assert.Equal("TEXT IN THE STATUS LINE", StatusText(terminal));
    }

    [Fact]
    public void The_cursor_comes_back_to_where_the_program_left_it()
    {
        // Each display has its own cursor. Losing the application's is how a program that writes a
        // status message finds itself continuing in the wrong place.
        var terminal = Fresh();

        terminal.Write("abc");
        var x = terminal.Buffer.X;
        var y = terminal.Buffer.Y;

        terminal.Write(Type(2) + Select(1) + "status" + Select(0));

        Assert.Equal(x, terminal.Buffer.X);
        Assert.Equal(y, terminal.Buffer.Y);

        terminal.Write("def");
        Assert.Equal("abcdef", Row(terminal, 0));
    }

    [Fact]
    public void Selecting_the_status_line_is_refused_when_there_is_not_one()
    {
        // The failure this control exists to stop. Honouring the selection with no row to write to
        // puts the text in the display; refusing keeps the program's own screen intact.
        var terminal = Fresh();

        terminal.Write("intact");
        terminal.Write(Select(1));

        Assert.False(terminal.StatusLineActive);

        terminal.Write("!");
        Assert.Equal("intact!", Row(terminal, 0));
    }

    [Fact]
    public void The_indicator_type_is_not_writable_by_the_program()
    {
        // Type 1 is the terminal's own indicator; its contents are not the application's to set.
        var terminal = Fresh();

        terminal.Write("intact");
        terminal.Write(Type(1));
        terminal.Write(Select(1));

        Assert.False(terminal.StatusLineActive);
        Assert.NotNull(terminal.StatusLine);

        terminal.Write("!");
        Assert.Equal("intact!", Row(terminal, 0));
    }

    [Fact]
    public void Removing_the_status_line_hands_the_cursor_back_first()
    {
        // Otherwise the row stops existing while it still has the cursor, and everything written
        // afterwards goes nowhere with no way to recover.
        var terminal = Fresh();

        terminal.Write("abc" + Type(2) + Select(1) + "status");
        Assert.True(terminal.StatusLineActive);

        terminal.Write(Type(0));

        Assert.False(terminal.StatusLineActive);
        Assert.Null(terminal.StatusLine);

        terminal.Write("def");
        Assert.Equal("abcdef", Row(terminal, 0));
    }

    [Fact]
    public void The_status_row_is_not_one_of_the_terminals_rows()
    {
        var terminal = Fresh();
        var rows = terminal.Rows;

        terminal.Write(Type(2));

        Assert.Equal(rows, terminal.Rows);
    }

    [Fact]
    public void The_row_follows_the_screens_width()
    {
        var terminal = Fresh();
        terminal.Write(Type(2));

        terminal.Resize(100, 5);

        Assert.Equal(100, terminal.StatusLine!.Length);
    }

    [Fact]
    public void A_change_is_announced_once_per_batch()
    {
        // A status line is written a character at a time like anything else; a host that repaints
        // on the event must not repaint once per character of one message.
        var terminal = Fresh();
        terminal.Write(Type(2) + Select(1));

        var changes = 0;
        terminal.StatusLineChanged += (_, _) => changes++;

        terminal.Write("hello");

        Assert.Equal(1, changes);
    }

    [Fact]
    public void RIS_takes_the_status_line_with_everything_else()
    {
        var terminal = Fresh();
        terminal.Write(Type(2) + Select(1) + "status");

        // \u001b, not \x1b: a \x escape is VARIABLE length, so "\x1bc" is the single
        // character U+01BC rather than ESC followed by c. The sequence gets printed instead
        // of dispatched, and the assertion then fails for a reason that is not the code.
        terminal.Write("\u001bc");

        Assert.Null(terminal.StatusLine);
        Assert.Equal(0, terminal.StatusDisplayType);
        Assert.False(terminal.StatusLineActive);
    }
}
