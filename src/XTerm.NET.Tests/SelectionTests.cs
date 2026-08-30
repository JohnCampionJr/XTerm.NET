using XTerm.Options;
using XTerm.Selection;

namespace XTerm.NET.Tests;

[TestClass]

public class SelectionTests
{
    [TestMethod]
    public void SelectionText_RemainsAnchored_WhenViewportScrolls()
    {
        var terminal = new Terminal(new TerminalOptions { Rows = 5, Cols = 80, Scrollback = 100 });

        for (int i = 0; i < 20; i++)
        {
            terminal.WriteLine($"Line{i:00}");
        }

        terminal.ScrollToTop();
        terminal.Selection.StartSelection(4, 2);
        terminal.Selection.UpdateSelection(5, 2);
        terminal.Selection.EndSelection();

        terminal.Selection.GetSelectionText().Should().Be("02");

        terminal.ScrollLines(1);

        terminal.Selection.GetSelectionText().Should().Be("02");
    }

    [TestMethod]
    public void IsCellSelected_TracksBufferSelectionAcrossViewportScroll()
    {
        var terminal = new Terminal(new TerminalOptions { Rows = 5, Cols = 80, Scrollback = 100 });

        for (int i = 0; i < 20; i++)
        {
            terminal.WriteLine($"Line{i:00}");
        }

        terminal.ScrollToTop();
        terminal.Selection.StartSelection(4, 2);
        terminal.Selection.UpdateSelection(5, 2);
        terminal.Selection.EndSelection();

        terminal.Selection.IsCellSelected(4, 2).Should().BeTrue();
        terminal.Selection.IsCellSelected(4, 1).Should().BeFalse();

        terminal.ScrollLines(1);

        terminal.Selection.IsCellSelected(4, 1).Should().BeTrue();
        terminal.Selection.IsCellSelected(4, 2).Should().BeFalse();
    }

    [TestMethod]
    public void SelectAll_IncludesScrollback_NotJustViewport()
    {
        var terminal = new Terminal(new TerminalOptions { Rows = 3, Cols = 80, Scrollback = 20 });

        for (int i = 0; i < 8; i++)
        {
            terminal.WriteLine($"Line{i}");
        }

        terminal.Selection.SelectAll();

        var selectedText = terminal.Selection.GetSelectionText();

        selectedText.Should().Contain("Line0");
        selectedText.Should().Contain("Line7");
    }

    [TestMethod]
    public void SelectionText_ClampsNegativeColumns()
    {
        var terminal = new Terminal(new TerminalOptions { Rows = 3, Cols = 10, Scrollback = 20 });
        terminal.Write("alpha");

        terminal.Selection.StartSelection(-3, 0);
        terminal.Selection.UpdateSelection(4, 0);
        terminal.Selection.EndSelection();

        terminal.Selection.GetSelectionText().Should().Be("alpha");
    }

    [TestMethod]
    public void SelectionText_ClampsColumnsPastRightEdge()
    {
        var terminal = new Terminal(new TerminalOptions { Rows = 3, Cols = 10, Scrollback = 20 });
        terminal.Write("alpha");

        terminal.Selection.StartSelection(0, 0);
        terminal.Selection.UpdateSelection(30, 0);
        terminal.Selection.EndSelection();

        terminal.Selection.GetSelectionText().Should().StartWith("alpha");
    }

    [TestMethod]
    public void SelectionText_ReturnsEmpty_WhenTerminalHasNoColumns()
    {
        var terminal = new Terminal(new TerminalOptions { Rows = 3, Cols = 0, Scrollback = 20 });

        terminal.Selection.StartSelection(0, 0);
        terminal.Selection.UpdateSelection(0, 0);
        terminal.Selection.EndSelection();

        terminal.Selection.GetSelectionText().Should().Be(string.Empty);
    }
    
    public void SelectionText_UsesLineFeedLineEndings()
    {
        var terminal = new Terminal(new TerminalOptions { Rows = 3, Cols = 80, Scrollback = 20 });
        terminal.Write("alpha\r\nbeta\r\ngamma");

        terminal.Selection.StartSelection(0, 0);
        terminal.Selection.UpdateSelection(4, 2);
        terminal.Selection.EndSelection();

        var selectedText = terminal.Selection.GetSelectionText();

        selectedText.Should().NotContain("\r");
        selectedText.Count(ch => ch == '\n').Should().Be(2);
        selectedText.Should().StartWith("alpha");
        selectedText.Should().Contain("\nbeta");
        selectedText.Should().EndWith("gamma");
    }

    [TestMethod]
    public void Selection_IsCleared_WhenTrimRemovesSelectedLines()
    {
        var terminal = new Terminal(new TerminalOptions { Rows = 3, Cols = 80, Scrollback = 2 });

        for (int i = 0; i < 5; i++)
        {
            terminal.WriteLine($"Line{i}");
        }

        terminal.ScrollToTop();
        var initialTopLine = terminal.GetVisibleLines()[0];
        var expectedSelectedText = initialTopLine[4].ToString();
        terminal.Selection.StartSelection(4, 0);
        terminal.Selection.UpdateSelection(4, 0);
        terminal.Selection.EndSelection();

        terminal.Selection.GetSelectionText().Should().Be(expectedSelectedText);

        for (int i = 5; i < 10; i++)
        {
            terminal.WriteLine($"Line{i}");
        }

        terminal.Selection.HasSelection.Should().BeFalse();
        terminal.Selection.GetSelectionText().Should().Be(string.Empty);
    }

    // ---------------------------------------------------------------- bounds

    [TestMethod]
    public void TryGetSelection_ReportsNothing_WhenNothingIsSelected()
    {
        var terminal = new Terminal(new TerminalOptions { Rows = 5, Cols = 80 });

        terminal.Selection.TryGetSelection(out var range).Should().BeFalse();
        range.Should().Be(default(SelectionRange));
    }

    /// <summary>
    /// The point of the type: a selection dragged BACKWARDS still reports its ends in order.
    /// </summary>
    /// <remarks>
    /// The two ends are stored in the order the user dragged them, so every caller that wanted to
    /// know what was selected had to know to swap them. This is that comparison, done once.
    /// </remarks>
    [TestMethod]
    public void TryGetSelection_OrdersTheEnds_HoweverTheDragWent()
    {
        var terminal = new Terminal(new TerminalOptions { Rows = 5, Cols = 80, Scrollback = 100 });
        for (int i = 0; i < 10; i++)
            terminal.WriteLine($"Line{i:00}");

        terminal.ScrollToTop();

        terminal.Selection.StartSelection(6, 3);
        terminal.Selection.UpdateSelection(2, 1);
        terminal.Selection.EndSelection();

        terminal.Selection.TryGetSelection(out var backwards).Should().BeTrue();

        terminal.Selection.ClearSelection();
        terminal.Selection.StartSelection(2, 1);
        terminal.Selection.UpdateSelection(6, 3);
        terminal.Selection.EndSelection();

        terminal.Selection.TryGetSelection(out var forwards).Should().BeTrue();

        backwards.Should().Be(forwards);
        (backwards.StartY < backwards.EndY).Should().BeTrue();
    }

    [TestMethod]
    public void TryGetSelection_ReportsAbsoluteRows_SoScrollingDoesNotMoveIt()
    {
        // Absolute rows are what makes a range outlive the viewport it was taken in -- the same
        // property IsCellSelected has always had, now visible to a caller.
        var terminal = new Terminal(new TerminalOptions { Rows = 5, Cols = 80, Scrollback = 100 });
        for (int i = 0; i < 20; i++)
            terminal.WriteLine($"Line{i:00}");

        terminal.ScrollToTop();
        terminal.Selection.StartSelection(4, 2);
        terminal.Selection.UpdateSelection(5, 2);
        terminal.Selection.EndSelection();

        terminal.Selection.TryGetSelection(out var before).Should().BeTrue();

        terminal.ScrollLines(1);

        terminal.Selection.TryGetSelection(out var after).Should().BeTrue();
        after.Should().Be(before);
    }

    // -------------------------------------------------------------- row spans

    [TestMethod]
    public void TryGetRowSpan_CoversTheWholeRow_BetweenTheEnds()
    {
        var range = new SelectionRange(StartX: 5, StartY: 2, EndX: 3, EndY: 6);

        range.TryGetRowSpan(4, cols: 80, out var startX, out var endX).Should().BeTrue();
        startX.Should().Be(0);
        endX.Should().Be(79);
    }

    [TestMethod]
    public void TryGetRowSpan_StartsAndEndsWhereTheSelectionDoes()
    {
        var range = new SelectionRange(StartX: 5, StartY: 2, EndX: 3, EndY: 6);

        range.TryGetRowSpan(2, cols: 80, out var firstStart, out var firstEnd).Should().BeTrue();
        firstStart.Should().Be(5);
        firstEnd.Should().Be(79);

        range.TryGetRowSpan(6, cols: 80, out var lastStart, out var lastEnd).Should().BeTrue();
        lastStart.Should().Be(0);
        lastEnd.Should().Be(3);
    }

    [TestMethod]
    public void TryGetRowSpan_IsOneSpan_WhenTheSelectionIsWithinOneRow()
    {
        var range = new SelectionRange(StartX: 10, StartY: 3, EndX: 20, EndY: 3);

        range.TryGetRowSpan(3, cols: 80, out var startX, out var endX).Should().BeTrue();
        startX.Should().Be(10);
        endX.Should().Be(20);
    }

    [TestMethod]
    public void TryGetRowSpan_DeclinesRowsOutsideTheSelection()
    {
        // The reason a renderer wants this: a row it can skip costs two comparisons rather than one
        // question per column.
        var range = new SelectionRange(StartX: 5, StartY: 2, EndX: 3, EndY: 6);

        range.TryGetRowSpan(1, cols: 80, out _, out _).Should().BeFalse();
        range.TryGetRowSpan(7, cols: 80, out _, out _).Should().BeFalse();
    }

    [TestMethod]
    public void TryGetRowSpan_ClampsToAGridThatHasSinceNarrowed()
    {
        // A range outlives the width it was made at. Asked about a narrower grid it reports the
        // columns that still exist, and declines the row when none of them do.
        var range = new SelectionRange(StartX: 70, StartY: 2, EndX: 75, EndY: 2);

        range.TryGetRowSpan(2, cols: 80, out var wideStart, out var wideEnd).Should().BeTrue();
        wideStart.Should().Be(70);
        wideEnd.Should().Be(75);

        range.TryGetRowSpan(2, cols: 40, out var narrowStart, out var narrowEnd).Should().BeTrue();
        narrowStart.Should().Be(39);
        narrowEnd.Should().Be(39);

        range.TryGetRowSpan(2, cols: 0, out _, out _).Should().BeFalse();
    }

    /// <summary>
    /// The bounds and the per-cell question have to give the same answers, because they are now two
    /// views of one rule and nothing else would notice them drifting.
    /// </summary>
    [TestMethod]
    public void TryGetRowSpan_AgreesWithIsCellSelected_AcrossTheGrid()
    {
        var terminal = new Terminal(new TerminalOptions { Rows = 5, Cols = 20, Scrollback = 100 });
        for (int i = 0; i < 10; i++)
            terminal.WriteLine($"Line{i:00}");

        terminal.ScrollToTop();
        terminal.Selection.StartSelection(7, 1);
        terminal.Selection.UpdateSelection(4, 3);
        terminal.Selection.EndSelection();

        terminal.Selection.TryGetSelection(out var range).Should().BeTrue();

        for (int row = 0; row < terminal.Rows; row++)
        {
            var absolute = terminal.Buffer.YDisp + row;
            var hasSpan = range.TryGetRowSpan(absolute, terminal.Cols, out var startX, out var endX);

            for (int x = 0; x < terminal.Cols; x++)
            {
                var fromSpan = hasSpan && x >= startX && x <= endX;
                fromSpan.Should().Be(terminal.Selection.IsCellSelected(x, row));
            }
        }
    }
}
