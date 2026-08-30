using XTerm.Options;

namespace XTerm.Tests;

[TestClass]

public class BackgroundColorEraseScrollTests
{
    private const string Esc = "\u001b";

    [TestMethod]
    public void Scrolling_up_fills_the_new_row_with_the_current_background_only()
    {
        var terminal = new Terminal(new TerminalOptions { Cols = 5, Rows = 3 });
        terminal.Write($"{Esc}[1;31;44m{Esc}[3;1H\n");

        var line = terminal.Buffer.Lines[terminal.Buffer.YBase + 2]!;
        Enumerable.Range(0, line.Length).Should().AllSatisfy(column =>
        {
            (line[column].Attributes.GetBgColor()).Should().Be(4);
            (line[column].Attributes.GetFgColor()).Should().Be(256);
            (line[column].Attributes.IsBold()).Should().BeFalse();
        });
    }

    [TestMethod]
    public void Reverse_scrolling_uses_the_current_background()
    {
        var terminal = new Terminal(new TerminalOptions { Cols = 5, Rows = 3 });
        terminal.Write($"{Esc}[42m{Esc}[1;1H{Esc}M");

        var line = terminal.Buffer.Lines[terminal.Buffer.YBase]!;
        Enumerable.Range(0, line.Length).Should().AllSatisfy(column =>
            (line[column].Attributes.GetBgColor()).Should().Be(2));
    }

    [TestMethod]
    public void A_recycled_scrollback_line_is_reset_with_the_current_background()
    {
        var terminal = new Terminal(new TerminalOptions { Cols = 5, Rows = 2, Scrollback = 1 });
        terminal.Write($"{Esc}[46m{Esc}[2;1H\n\n");

        var line = terminal.Buffer.Lines[terminal.Buffer.YBase + 1]!;
        Enumerable.Range(0, line.Length).Should().AllSatisfy(column =>
            (line[column].Attributes.GetBgColor()).Should().Be(6));
    }

    [TestMethod]
    public void A_narrow_margin_scroll_fills_only_the_exposed_box_with_the_current_background()
    {
        var terminal = new Terminal(new TerminalOptions { Cols = 6, Rows = 3 });
        terminal.Write($"{Esc}[?69h{Esc}[2;5s{Esc}[44m{Esc}[3;2H\n");

        var line = terminal.Buffer.Lines[terminal.Buffer.YBase + 2]!;
        (line[0].Attributes.GetBgColor()).Should().Be(257);
        Enumerable.Range(1, 4).Should().AllSatisfy(column =>
            (line[column].Attributes.GetBgColor()).Should().Be(4));
        (line[5].Attributes.GetBgColor()).Should().Be(257);
    }

    [TestMethod]
    public void Alternate_screen_scrolling_pulls_the_same_current_background()
    {
        var terminal = new Terminal(new TerminalOptions { Cols = 5, Rows = 3 });
        terminal.Write($"{Esc}[45m{Esc}[?1049h{Esc}[3;1H\n");

        var line = terminal.Buffer.Lines[2]!;
        Enumerable.Range(0, line.Length).Should().AllSatisfy(column =>
            (line[column].Attributes.GetBgColor()).Should().Be(5));
    }

    [TestMethod]
    [DataRow("\u001b[2K", 0, 0)]
    [DataRow("\u001b[2J", 0, 0)]
    [DataRow("\u001b[3X", 0, 0)]
    [DataRow("\u001b[2@", 0, 0)]
    [DataRow("\u001b[2P", 0, 4)]
    [DataRow("\u001b[L", 0, 0)]
    [DataRow("\u001b[M", 2, 0)]
    public void Every_BCE_operation_keeps_only_the_current_background(
        string operation, int row, int column)
    {
        var terminal = new Terminal(new TerminalOptions { Cols = 5, Rows = 3 });
        terminal.Write($"{Esc}[1;31;44m{operation}");

        var attributes = terminal.Buffer.Lines[terminal.Buffer.YBase + row]![column].Attributes;
        attributes.GetBgColor().Should().Be(4);
        attributes.GetFgColor().Should().Be(256);
        attributes.IsBold().Should().BeFalse();
    }

    [TestMethod]
    public void Reset_clears_the_background_used_by_later_scrolls()
    {
        var terminal = new Terminal(new TerminalOptions { Cols = 5, Rows = 3 });
        terminal.Write($"{Esc}[44m");

        terminal.Reset();
        terminal.Write($"{Esc}[3;1H\n");

        var line = terminal.Buffer.Lines[terminal.Buffer.YBase + 2]!;
        Enumerable.Range(0, line.Length).Should().AllSatisfy(column =>
            (line[column].Attributes.GetBgColor()).Should().Be(257));
    }
}
