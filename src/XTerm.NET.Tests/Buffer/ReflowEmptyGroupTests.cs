using XTerm;
using XTerm.Buffer;
using XTerm.Options;

namespace XTerm.Tests.Buffer;

/// <summary>
/// Regression tests for shrinking a buffer that contains a wrapped group with no content in it.
/// </summary>
/// <remarks>
/// Such a group produces an empty result from ReflowSmallerGetNewLineLengths, which the reflow then
/// indexed at [Length - 1]. Only a ONE-ROW group can be empty, because every row of a group except
/// the last counts as a full row of cells regardless of what is in it -- so this needs a
/// continuation row at index 0 with an unwrapped row beneath, which is what the scrollback leaves
/// behind once the row being continued is trimmed away.
/// </remarks>
[TestClass]
public class ReflowEmptyGroupTests
{
    [TestMethod]
    public void Shrink_WithBlankWrappedRowAtTop_DoesNotThrow()
    {
        // Twelve spaces at six columns wrap, so the tail row is both blank and wrapped. Two more
        // lines push the head of that pair out of a one-row scrollback, leaving the blank
        // continuation at index 0 with an unwrapped line under it.
        var terminal = new Terminal(new TerminalOptions { Cols = 6, Rows = 2, Scrollback = 1 });

        terminal.Write(new string(' ', 12));
        terminal.Write("\r\nx");
        terminal.Write("\r\ny");

        (terminal.Buffer.Lines[0]!.IsWrapped).Should().BeTrue("precondition: the top row is a continuation");
        (terminal.Buffer.Lines[0]!.GetTrimmedLength()).Should().Be(0);
        (terminal.Buffer.Lines[1]!.IsWrapped).Should().BeFalse("precondition: the row beneath starts fresh");

        Record.Exception(() => terminal.Resize(4, 2)).Should().BeNull();
    }

    [TestMethod]
    public void Shrink_WithBlankWrappedRowAtTop_KeepsTheRemainingContent()
    {
        // Not throwing is not enough: the rows that DO have content still have to survive.
        var terminal = new Terminal(new TerminalOptions { Cols = 6, Rows = 2, Scrollback = 1 });

        terminal.Write(new string(' ', 12));
        terminal.Write("\r\nx");
        terminal.Write("\r\ny");

        terminal.Resize(4, 2);

        var text = new List<string>();
        for (var i = 0; i < terminal.Buffer.Lines.Length; i++)
        {
            text.Add(terminal.Buffer.Lines[i]!.TranslateToString(trimRight: true));
        }

        text.Should().Contain("x");
        text.Should().Contain("y");
    }

    [TestMethod]
    public void Shrink_WithBlankWrappedRowAtTop_DoesNotThrow_ConstructedDirectly()
    {
        // The same shape built by hand, so the regression stays pinned even if the terminal-level
        // route above stops producing this layout.
        var buffer = new TerminalBuffer(80, 24, 1000);
        buffer.Resize(6, 10);
        buffer.SetCursorRaw(0, 5);
        buffer.Lines[0]!.IsWrapped = true;

        Record.Exception(() => buffer.Resize(4, 10)).Should().BeNull();
    }
}
