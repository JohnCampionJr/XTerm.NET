using XTerm.Buffer;
using XTerm.Common;
using XTerm.Options;

namespace XTerm.Tests.Buffer;

[TestClass]

public class BufferTests
{
    [TestMethod]
    public void Constructor_InitializesBuffer()
    {
        // Arrange & Act
        var buffer = new TerminalBuffer(80, 24, 1000);

        // Assert
        buffer.YDisp.Should().Be(0);
        buffer.YBase.Should().Be(0);
        buffer.Y.Should().Be(0);
        buffer.X.Should().Be(0);
        buffer.ScrollTop.Should().Be(0);
        buffer.ScrollBottom.Should().Be(23);
        buffer.Lines.Should().NotBeNull();
        buffer.SavedCursorState.Should().NotBeNull();
    }

    [TestMethod]
    public void Constructor_CreatesLinesForRows()
    {
        // Arrange & Act
        var buffer = new TerminalBuffer(80, 24, 1000);

        // Assert
        ((buffer.Lines.Length >= 24)).Should().BeTrue();
    }

    [TestMethod]
    public void SetCursor_SetsCursorPosition()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24, 1000);

        // Act
        buffer.SetCursor(10, 5);

        // Assert
        buffer.X.Should().Be(10);
        buffer.Y.Should().Be(5);
    }

    [TestMethod]
    public void SetCursor_ClampsToBufferBounds()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24, 1000);

        // Act
        buffer.SetCursor(-5, -3);

        // Assert
        buffer.X.Should().Be(0);
        buffer.Y.Should().Be(0);

        // Act
        buffer.SetCursor(100, 50);

        // Assert
        buffer.X.Should().Be(79);
        buffer.Y.Should().Be(23);
    }

    [TestMethod]
    public void MoveCursor_MovesCursorWithoutClamping()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24, 1000);

        // Act
        buffer.SetCursorRaw(10, 5);

        // Assert
        buffer.X.Should().Be(10);
        buffer.Y.Should().Be(5);
    }

    [TestMethod]
    public void GetLine_ReturnsLine()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24, 1000);

        // Act
        var line = buffer.GetLine(0);

        // Assert
        line.Should().NotBeNull();
        line.Length.Should().Be(80);
    }

    [TestMethod]
    public void GetBlankLine_ReturnsBlankLine()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24, 1000);
        var attr = AttributeData.Default;

        // Act
        var line = buffer.GetBlankLine(attr);

        // Assert
        line.Should().NotBeNull();
        line.Length.Should().Be(80);
        line.IsWrapped.Should().BeFalse();
    }

    [TestMethod]
    public void GetBlankLine_WithWrapped_SetsWrapped()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24, 1000);
        var attr = AttributeData.Default;

        // Act
        var line = buffer.GetBlankLine(attr, isWrapped: true);

        // Assert
        line.IsWrapped.Should().BeTrue();
    }

    [TestMethod]
    public void ScrollUp_ScrollsBuffer()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24, 1000);
        var initialYBase = buffer.YBase;

        // Act
        buffer.ScrollUp(1);

        // Assert
        ((buffer.YBase >= initialYBase)).Should().BeTrue();
    }

    [TestMethod]
    public void ScrollUp_MultipleLines_ScrollsMultipleTimes()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24, 1000);
        var initialYBase = buffer.YBase;

        // Act
        buffer.ScrollUp(3);

        // Assert
        ((buffer.YBase >= initialYBase)).Should().BeTrue();
    }

    [TestMethod]
    public void ScrollDown_ScrollsBufferDown()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24, 1000);
        buffer.ScrollUp(3); // Scroll up first to have content to scroll down

        // Act
        buffer.ScrollDown(1);

        // Assert - Should have lines in buffer
        buffer.Lines.Should().NotBeNull();
    }

    [TestMethod]
    public void ScrollDisp_ScrollsDisplay()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24, 1000);
        buffer.ScrollUp(5); // Create scrollback
        var initialYDisp = buffer.YDisp;

        // Act
        buffer.ScrollDisp(2);

        // Assert
        ((buffer.YDisp >= initialYDisp)).Should().BeTrue();
    }

    [TestMethod]
    public void ScrollDisp_ClampsToYBase()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24, 1000);
        buffer.ScrollUp(3);

        // Act
        buffer.ScrollDisp(100); // Try to scroll way beyond

        // Assert
        buffer.YDisp.Should().Be(buffer.YBase);
    }

    [TestMethod]
    public void ScrollDisp_ClampsToZero()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24, 1000);
        buffer.ScrollUp(3);
        buffer.ScrollDisp(2);

        // Act
        buffer.ScrollDisp(-100); // Try to scroll way before

        // Assert
        buffer.YDisp.Should().Be(0);
    }

    [TestMethod]
    public void ScrollToBottom_ScrollsToBottom()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24, 1000);
        buffer.ScrollUp(5);
        buffer.ScrollDisp(-3); // Scroll up in display

        // Act
        buffer.ScrollToBottom();

        // Assert
        buffer.YDisp.Should().Be(buffer.YBase);
    }

    [TestMethod]
    public void ScrollToTop_ScrollsToTop()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24, 1000);
        buffer.ScrollUp(5);

        // Act
        buffer.ScrollToTop();

        // Assert
        buffer.YDisp.Should().Be(0);
    }

    [TestMethod]
    public void SetScrollRegion_SetsRegion()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24, 1000);

        // Act
        buffer.SetScrollRegion(5, 20);

        // Assert
        buffer.ScrollTop.Should().Be(5);
        buffer.ScrollBottom.Should().Be(20);
    }

    [TestMethod]
    public void SetScrollRegion_ClampsToBufferBounds()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24, 1000);

        // Act
        buffer.SetScrollRegion(-5, 100);

        // Assert
        buffer.ScrollTop.Should().Be(0);
        buffer.ScrollBottom.Should().Be(23);
    }

    [TestMethod]
    public void SetScrollRegion_TopGreaterThanBottom_ClampsCorrectly()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24, 1000);

        // Act
        buffer.SetScrollRegion(20, 5);

        // Assert
        buffer.ScrollTop.Should().Be(20);
        ((buffer.ScrollBottom >= buffer.ScrollTop)).Should().BeTrue();
    }

    [TestMethod]
    public void ResetScrollRegion_ResetsToFullScreen()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24, 1000);
        buffer.SetScrollRegion(5, 20);

        // Act
        buffer.ResetScrollRegion();

        // Assert
        buffer.ScrollTop.Should().Be(0);
        buffer.ScrollBottom.Should().Be(23);
    }

    [TestMethod]
    public void GetAbsoluteY_ReturnsAbsolutePosition()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24, 1000);
        buffer.ScrollUp(5); // YBase becomes 5

        // Act
        var absolute = buffer.GetAbsoluteY(10);

        // Assert
        absolute.Should().Be(buffer.YBase + 10);
    }

    [TestMethod]
    public void Resize_ResizesBuffer()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24, 1000);

        // Act
        buffer.Resize(100, 30);

        // Assert
        // Lines should exist and be accessible
        for (int i = 0; i < 30; i++)
        {
            var line = buffer.Lines[i];
            line.Should().NotBeNull();
        }
    }

    [TestMethod]
    public void Resize_GrowsColumns_UpdatesLineLengths()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24, 1000);

        // Act
        buffer.Resize(120, 24);

        // Assert - every line should have the new column count
        for (int i = 0; i < buffer.Lines.Length; i++)
        {
            var line = buffer.Lines[i];
            line.Should().NotBeNull();
            (line!.Length).Should().Be(120);
        }
    }

    [TestMethod]
    public void Resize_ShrinksColumns_UpdatesLineLengths()
    {
        // Arrange
        var buffer = new TerminalBuffer(120, 24, 1000);

        // Act
        buffer.Resize(60, 24);

        // Assert - every line should have the new column count
        for (int i = 0; i < buffer.Lines.Length; i++)
        {
            var line = buffer.Lines[i];
            line.Should().NotBeNull();
            (line!.Length).Should().Be(60);
        }
    }

    [TestMethod]
    public void SavedCursorState_InitializesCorrectly()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24, 1000);

        // Assert
        buffer.SavedCursorState.Should().NotBeNull();
        buffer.SavedCursorState.X.Should().Be(0);
        buffer.SavedCursorState.Y.Should().Be(0);
        buffer.SavedCursorState.Attr.Should().Be(AttributeData.Default);
        buffer.SavedCursorState.Charset.Should().Be(CharsetMode.G0);
    }

    [TestMethod]
    public void SavedCursorState_CanBeModified()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24, 1000);

        // Act
        buffer.SavedCursorState.X = 10;
        buffer.SavedCursorState.Y = 5;
        
        // To modify a struct field, we need to get it, modify it, and set it back
        var attr = buffer.SavedCursorState.Attr;
        attr.SetBold(true);
        buffer.SavedCursorState.Attr = attr;
        
        buffer.SavedCursorState.Charset = CharsetMode.G1;

        // Assert
        buffer.SavedCursorState.X.Should().Be(10);
        buffer.SavedCursorState.Y.Should().Be(5);
        buffer.SavedCursorState.Attr.IsBold().Should().BeTrue();
        buffer.SavedCursorState.Charset.Should().Be(CharsetMode.G1);
    }

    [TestMethod]
    [DataRow(20, 10, 0)]
    [DataRow(40, 20, 500)]
    [DataRow(100, 50, 2000)]
    public void Constructor_VariousSizes_WorksCorrectly(int cols, int rows, int scrollback)
    {
        // Act
        var buffer = new TerminalBuffer(cols, rows, scrollback);

        // Assert
        buffer.Should().NotBeNull();
        buffer.ScrollTop.Should().Be(0);
        buffer.ScrollBottom.Should().Be(rows - 1);
    }

    [TestMethod]
    public void ScrollUp_WithWrapped_SetsWrappedFlag()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24, 1000);

        // Act
        buffer.ScrollUp(1, isWrapped: true);

        // Assert
        // The newly added line is at the bottom of the active screen area.
        // After scroll, the new line is at position YBase + Rows - 1 in the buffer.
        // Since YBase becomes 1 after scroll (when scrollTop is 0), the new line is at index 24 (1 + 24 - 1).
        var lastActiveRow = buffer.YBase + buffer.Rows - 1;
        var bottomLine = buffer.Lines[lastActiveRow];
        bottomLine.Should().NotBeNull();
        bottomLine.IsWrapped.Should().BeTrue();
    }

    [TestMethod]
    public void Lines_Property_IsAccessible()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24, 1000);

        // Act
        var lines = buffer.Lines;

        // Assert
        lines.Should().NotBeNull();
        (lines.Length > 0).Should().BeTrue();
    }

    [TestMethod]
    public void MultipleScrollOperations_MaintainConsistency()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24, 1000);

        // Act
        buffer.ScrollUp(2);
        buffer.ScrollDown(1);
        buffer.ScrollUp(3);
        buffer.ScrollDisp(-2);
        buffer.ScrollToBottom();

        // Assert
        buffer.YDisp.Should().Be(buffer.YBase);
        ((buffer.YBase >= 0)).Should().BeTrue();
    }

    [TestMethod]
    public void CursorMovement_ComplexScenario()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24, 1000);

        // Act
        buffer.SetCursor(40, 12);
        var x1 = buffer.X;
        var y1 = buffer.Y;

        buffer.SetCursorRaw(50, 20);
        var x2 = buffer.X;
        var y2 = buffer.Y;

        buffer.SetCursor(0, 0);
        var x3 = buffer.X;
        var y3 = buffer.Y;

        // Assert
        x1.Should().Be(40);
        y1.Should().Be(12);
        x2.Should().Be(50);
        y2.Should().Be(20);
        x3.Should().Be(0);
        y3.Should().Be(0);
    }

    [TestMethod]
    public void ScrollRegion_AffectsScrolling()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24, 1000);
        buffer.SetScrollRegion(5, 15);

        // Act
        var scrollTop = buffer.ScrollTop;
        var scrollBottom = buffer.ScrollBottom;

        buffer.ScrollUp(1);

        // Assert
        scrollTop.Should().Be(5);
        scrollBottom.Should().Be(15);
    }

    #region Scrolling Beyond Viewport Tests

    [TestMethod]
    public void ScrollUp_BeyondViewport_IncrementsYBase()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24, 1000);
        buffer.YBase.Should().Be(0);

        // Act - Scroll up 10 times (simulating 10 new lines at bottom of screen)
        buffer.ScrollUp(10);

        // Assert
        buffer.YBase.Should().Be(10);
        buffer.YDisp.Should().Be(10); // Should auto-scroll to bottom
    }

    [TestMethod]
    public void ScrollUp_YDispFollowsYBase_WhenAtBottom()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24, 1000);

        // Act
        buffer.ScrollUp(5);

        // Assert - yDisp should follow yBase when user hasn't scrolled up
        buffer.YDisp.Should().Be(buffer.YBase);
        buffer.IsAtBottom.Should().BeTrue();
    }

    [TestMethod]
    public void ScrollUp_YDispDoesNotFollow_WhenScrolledUp()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24, 1000);
        buffer.ScrollUp(10); // Create some scrollback
        buffer.ScrollToTop(); // Scroll viewport to top

        // Act
        buffer.ScrollUp(5); // More content added

        // Assert - yDisp should stay at 0 (user scrolled up)
        buffer.YDisp.Should().Be(0);
        buffer.YBase.Should().Be(15);
        buffer.IsAtBottom.Should().BeFalse();
    }

    [TestMethod]
    public void ScrollUp_BufferLengthGrows_UntilMaxCapacity()
    {
        // Arrange - Small buffer with 10 row viewport and 20 scrollback (30 total)
        var buffer = new TerminalBuffer(80, 10, 20);
        buffer.Lines.Length.Should().Be(10); // Initially just viewport rows

        // Act - Scroll up 15 times
        buffer.ScrollUp(15);

        // Assert - Buffer should have grown
        buffer.Lines.Length.Should().Be(25); // 10 initial + 15 scrolled
        buffer.YBase.Should().Be(15);
    }

    [TestMethod]
    public void ScrollUp_AtMaxCapacity_RecyclesOldestLines()
    {
        // Arrange - Small buffer: 5 rows viewport, 5 scrollback = 10 max
        var buffer = new TerminalBuffer(80, 5, 5);
        
        // Fill buffer to capacity
        buffer.ScrollUp(5); // Now at 10 lines (max)
        buffer.Lines.Length.Should().Be(10);
        buffer.YBase.Should().Be(5);

        // Act - Scroll more, should recycle
        buffer.ScrollUp(3);

        // Assert - Length should stay at max, yBase should still be 5 (recycled)
        buffer.Lines.Length.Should().Be(10);
        buffer.YBase.Should().Be(5); // Stays at 5 because oldest lines are recycled
    }

    [TestMethod]
    public void ScrollUp_ContentPlacedCorrectly_InActiveArea()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 5, 100);
        
        // Write content to first line
        var line0 = buffer.Lines[0];
        var cell = new BufferCell("A", 1, AttributeData.Default);
        line0?.SetCell(0, ref cell);

        // Act - Scroll up once
        buffer.ScrollUp(1);

        // Assert - Original line 0 is now in scrollback (at index 0)
        // Active area starts at yBase (1), new blank line is at yBase + rows - 1 (5)
        var scrollbackLine = buffer.Lines[0];
        scrollbackLine.Should().NotBeNull();
        scrollbackLine[0].Content.Should().Be("A");

        // The active area's last line should be blank
        var lastActiveLine = buffer.Lines[buffer.YBase + buffer.Rows - 1];
        lastActiveLine.Should().NotBeNull();
        ((lastActiveLine[0].IsSpace() || lastActiveLine[0].Content == " ")).Should().BeTrue();
    }

    [TestMethod]
    public void ScrollUp_PreservesScrollbackContent()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 5, 100);
        
        // Mark each initial line with a unique identifier
        for (int i = 0; i < 5; i++)
        {
            var line = buffer.Lines[i]!;
            var cell = new BufferCell(((char)('A' + i)).ToString(), 1, AttributeData.Default);
            line.SetCell(0, ref cell);
        }

        // Act - Scroll up 3 times
        buffer.ScrollUp(3);

        // Assert - First 3 original lines should be in scrollback
        (buffer.Lines[0]?[0].Content).Should().Be("A");
        (buffer.Lines[1]?[0].Content).Should().Be("B");
        (buffer.Lines[2]?[0].Content).Should().Be("C");
        
        // Lines D and E should now be in active area (at yBase + 0 and yBase + 1)
        (buffer.Lines[buffer.YBase]?[0].Content).Should().Be("D");
        (buffer.Lines[buffer.YBase + 1]?[0].Content).Should().Be("E");
    }

    [TestMethod]
    public void ScrollToTop_ShowsScrollbackContent()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 5, 100);
        
        // Mark line 0
        var cell = new BufferCell("X", 1, AttributeData.Default);
        buffer.Lines[0]?.SetCell(0, ref cell);
        
        // Scroll up to create scrollback
        buffer.ScrollUp(10);
        buffer.YBase.Should().Be(10);
        buffer.YDisp.Should().Be(10);

        // Act
        buffer.ScrollToTop();

        // Assert
        buffer.YDisp.Should().Be(0);
        // Line at yDisp (0) should be the original line with "X"
        var visibleLine = buffer.Lines[buffer.YDisp];
        (visibleLine?[0].Content).Should().Be("X");
    }

    [TestMethod]
    public void ScrollToBottom_AfterScrollingUp_ReturnsToActiveArea()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 5, 100);
        buffer.ScrollUp(20);
        buffer.ScrollToTop();
        buffer.YDisp.Should().Be(0);

        // Act
        buffer.ScrollToBottom();

        // Assert
        buffer.YDisp.Should().Be(buffer.YBase);
        buffer.IsAtBottom.Should().BeTrue();
    }

    [TestMethod]
    public void IsAtBottom_TrueInitially()
    {
        // Arrange & Act
        var buffer = new TerminalBuffer(80, 24, 1000);

        // Assert
        buffer.IsAtBottom.Should().BeTrue();
    }

    [TestMethod]
    public void IsAtBottom_TrueAfterScrollUp()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24, 1000);

        // Act
        buffer.ScrollUp(10);

        // Assert - Should still be at bottom (auto-followed)
        buffer.IsAtBottom.Should().BeTrue();
    }

    [TestMethod]
    public void IsAtBottom_FalseAfterScrollToTop()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24, 1000);
        buffer.ScrollUp(10);

        // Act
        buffer.ScrollToTop();

        // Assert
        buffer.IsAtBottom.Should().BeFalse();
    }

    [TestMethod]
    public void ScrollLines_RelativeScrolling_Works()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 10, 100);
        buffer.ScrollUp(50); // Create lots of scrollback
        buffer.ScrollToTop();
        buffer.YDisp.Should().Be(0);

        // Act - Scroll down 25 lines
        buffer.ScrollLines(25);

        // Assert
        buffer.YDisp.Should().Be(25);

        // Act - Scroll up 10 lines
        buffer.ScrollLines(-10);

        // Assert
        buffer.YDisp.Should().Be(15);
    }

    [TestMethod]
    public void ScrollLines_ClampsToValidRange()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 10, 100);
        buffer.ScrollUp(20);

        // Act - Try to scroll way past bottom
        buffer.ScrollLines(1000);

        // Assert - Should be clamped to yBase
        buffer.YDisp.Should().Be(buffer.YBase);

        // Act - Try to scroll way past top
        buffer.ScrollLines(-1000);

        // Assert - Should be clamped to 0
        buffer.YDisp.Should().Be(0);
    }

    [TestMethod]
    public void ViewportY_Property_ReadsAndWritesYDisp()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 10, 100);
        buffer.ScrollUp(30);

        // Act & Assert - Read
        buffer.ViewportY.Should().Be(buffer.YDisp);

        // Act - Write
        buffer.ViewportY = 15;

        // Assert
        buffer.YDisp.Should().Be(15);
        buffer.ViewportY.Should().Be(15);
    }

    [TestMethod]
    public void ViewportY_ClampedToValidRange()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 10, 100);
        buffer.ScrollUp(20);

        // Act - Try to set beyond yBase
        buffer.ViewportY = 100;

        // Assert
        buffer.ViewportY.Should().Be(buffer.YBase);

        // Act - Try to set negative
        buffer.ViewportY = -10;

        // Assert
        buffer.ViewportY.Should().Be(0);
    }

    [TestMethod]
    public void GetAbsoluteY_CorrectAfterScrolling()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 10, 100);
        buffer.ScrollUp(25);

        // Act & Assert
        // GetAbsoluteY converts viewport-relative Y to buffer-absolute Y
        // For viewport row 0, absolute should be yBase + 0 = 25
        buffer.GetAbsoluteY(0).Should().Be(25);
        buffer.GetAbsoluteY(5).Should().Be(30);
        buffer.GetAbsoluteY(9).Should().Be(34); // Last viewport row
    }

    [TestMethod]
    public void BufferLength_MatchesExpectedSize()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 10, 100);
        buffer.Length.Should().Be(10); // Initially just viewport rows

        // Act
        buffer.ScrollUp(50);

        // Assert - Should have grown to rows + scrollback used
        buffer.Length.Should().Be(60); // 10 initial + 50 scrolled
    }

    [TestMethod]
    public void LargeScrollback_HandlesCorrectly()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24, 10000);

        // Act - Simulate a lot of output
        buffer.ScrollUp(5000);

        // Assert
        buffer.YBase.Should().Be(5000);
        buffer.YDisp.Should().Be(5000);
        buffer.Length.Should().Be(5024); // 24 rows + 5000 scrollback
        buffer.IsAtBottom.Should().BeTrue();
    }

    [TestMethod]
    public void ScrollUp_WithScrollRegion_DoesNotAffectYBase()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24, 1000);
        buffer.SetScrollRegion(5, 15); // Scroll region in middle of screen

        // Act
        buffer.ScrollUp(3);

        // Assert - yBase should not change when scroll region is set
        buffer.YBase.Should().Be(0);
        buffer.YDisp.Should().Be(0);
    }

    [TestMethod]
    public void ScrollDown_WithScrollRegion_DoesNotAffectYBase()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24, 1000);
        buffer.SetScrollRegion(5, 15);

        // Act
        buffer.ScrollDown(3);

        // Assert
        buffer.YBase.Should().Be(0);
        buffer.YDisp.Should().Be(0);
    }

    [TestMethod]
    public void Cols_And_Rows_Properties_ReturnCorrectValues()
    {
        // Arrange
        var buffer = new TerminalBuffer(100, 50, 500);

        // Assert
        buffer.Cols.Should().Be(100);
        buffer.Rows.Should().Be(50);
    }

    [TestMethod]
    public void Resize_UpdatesColsAndRows()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24, 1000);

        // Act
        buffer.Resize(120, 40);

        // Assert
        buffer.Cols.Should().Be(120);
        buffer.Rows.Should().Be(40);
    }

    [TestMethod]
    public void Resize_AdjustsScrollBottom()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24, 1000);
        buffer.ScrollBottom.Should().Be(23);

        // Act
        buffer.Resize(80, 30);

        // Assert - ScrollBottom should be updated to new rows - 1
        buffer.ScrollBottom.Should().Be(29);
    }

    #endregion
    #region Alternate Buffer (No Scrollback) Tests

    [TestMethod]
    public void AlternateBuffer_NoScrollback_YBaseRemainsZero()
    {
        // Arrange - Create buffer with NO scrollback (like alternate buffer)
        var buffer = new TerminalBuffer(80, 24, 0);

        // Act - Scroll up multiple times
        buffer.ScrollUp(10);

        // Assert - YBase should remain 0 since there's no scrollback
        buffer.YBase.Should().Be(0);
        buffer.YDisp.Should().Be(0);
    }

    [TestMethod]
    public void AlternateBuffer_NoScrollback_ViewportYRemainsZero()
    {
        // Arrange - Create buffer with NO scrollback (like alternate buffer)
        var buffer = new TerminalBuffer(80, 24, 0);

        // Act - Scroll up
        buffer.ScrollUp(5);

        // Assert - ViewportY should remain 0
        buffer.ViewportY.Should().Be(0);
    }

    [TestMethod]
    public void AlternateBuffer_NoScrollback_ScrollUpWithScrollRegion_YBaseRemainsZero()
    {
        // Arrange - Create buffer with NO scrollback (like alternate buffer)
        var buffer = new TerminalBuffer(80, 24, 0);
        
        // Set a scroll region (as asciiquarium does with DECSTBM)
        buffer.SetScrollRegion(0, 10);

        // Act - Scroll within the region
        buffer.ScrollUp(3);

        // Assert - YBase and YDisp should remain 0
        buffer.YBase.Should().Be(0);
        buffer.YDisp.Should().Be(0);
        buffer.ViewportY.Should().Be(0);
    }

    [TestMethod]
    public void AlternateBuffer_NoScrollback_ScrollUpAtTopOfScreen_YBaseRemainsZero()
    {
        // Arrange - Create buffer with NO scrollback (like alternate buffer)
        // This simulates the exact case that caused the asciiquarium bug
        var buffer = new TerminalBuffer(80, 50, 0);
        
        // Scroll region starting at top (row 0) - this is the case that was broken
        buffer.SetScrollRegion(0, 5);

        // Act - Scroll up (e.g., when content exits the scroll region at top)
        buffer.ScrollUp(1);

        // Assert - YBase should NOT increment because there's no scrollback capacity
        buffer.YBase.Should().Be(0);
        buffer.YDisp.Should().Be(0);
    }

    [TestMethod]
    public void ScrollUp_TopAnchoredPartialRegion_PreservesRowsBelowRegion()
    {
        var buffer = new TerminalBuffer(10, 5, 100);

        SetCell(buffer, 0, "A");
        SetCell(buffer, 1, "B");
        SetCell(buffer, 2, "C");
        SetCell(buffer, 3, "D");
        SetCell(buffer, 4, ">");

        buffer.SetScrollRegion(0, 3);
        buffer.ScrollUp(1);

        buffer.YBase.Should().Be(0);
        (buffer.GetLine(0)?[0].Content).Should().Be("B");
        (buffer.GetLine(1)?[0].Content).Should().Be("C");
        (buffer.GetLine(2)?[0].Content).Should().Be("D");
        (buffer.GetLine(3)?[0].IsSpace() ?? false).Should().BeTrue();
        (buffer.GetLine(4)?[0].Content).Should().Be(">");
    }

    [TestMethod]
    public void InsertLines_WithScrollback_UsesActiveBufferCoordinates()
    {
        var terminal = new Terminal(new TerminalOptions { Cols = 10, Rows = 5, Scrollback = 100 });

        terminal.Write("s1\r\ns2\r\ns3\r\ns4\r\ns5\r\n");
        var yBase = terminal.Buffer.YBase;

        SetCell(terminal.Buffer, yBase + 0, "A");
        SetCell(terminal.Buffer, yBase + 1, "B");
        SetCell(terminal.Buffer, yBase + 2, "C");
        SetCell(terminal.Buffer, yBase + 3, "D");
        SetCell(terminal.Buffer, yBase + 4, ">");

        terminal.Write("\x1b[1;4r\x1b[1;1H\x1b[1L");

        terminal.Buffer.YBase.Should().Be(yBase);
        (terminal.Buffer.GetLine(yBase + 0)?[0].IsSpace() ?? false).Should().BeTrue();
        (terminal.Buffer.GetLine(yBase + 1)?[0].Content).Should().Be("A");
        (terminal.Buffer.GetLine(yBase + 2)?[0].Content).Should().Be("B");
        (terminal.Buffer.GetLine(yBase + 3)?[0].Content).Should().Be("C");
        (terminal.Buffer.GetLine(yBase + 4)?[0].Content).Should().Be(">");
    }

    [TestMethod]
    public void DeleteLines_WithScrollback_UsesActiveBufferCoordinates()
    {
        var terminal = new Terminal(new TerminalOptions { Cols = 10, Rows = 5, Scrollback = 100 });

        terminal.Write("s1\r\ns2\r\ns3\r\ns4\r\ns5\r\n");
        var yBase = terminal.Buffer.YBase;

        SetCell(terminal.Buffer, yBase + 0, "A");
        SetCell(terminal.Buffer, yBase + 1, "B");
        SetCell(terminal.Buffer, yBase + 2, "C");
        SetCell(terminal.Buffer, yBase + 3, "D");
        SetCell(terminal.Buffer, yBase + 4, ">");

        terminal.Write("\x1b[1;4r\x1b[1;1H\x1b[1M");

        terminal.Buffer.YBase.Should().Be(yBase);
        (terminal.Buffer.GetLine(yBase + 0)?[0].Content).Should().Be("B");
        (terminal.Buffer.GetLine(yBase + 1)?[0].Content).Should().Be("C");
        (terminal.Buffer.GetLine(yBase + 2)?[0].Content).Should().Be("D");
        (terminal.Buffer.GetLine(yBase + 3)?[0].IsSpace() ?? false).Should().BeTrue();
        (terminal.Buffer.GetLine(yBase + 4)?[0].Content).Should().Be(">");
    }

    [TestMethod]
    public void DeleteLines_OutsideScrollRegion_PreservesReservedPromptRow()
    {
        var terminal = new Terminal(new TerminalOptions { Cols = 10, Rows = 5, Scrollback = 100 });

        terminal.Write("s1\r\ns2\r\ns3\r\ns4\r\ns5\r\n");
        var yBase = terminal.Buffer.YBase;

        SetCell(terminal.Buffer, yBase + 0, "A");
        SetCell(terminal.Buffer, yBase + 1, "B");
        SetCell(terminal.Buffer, yBase + 2, "C");
        SetCell(terminal.Buffer, yBase + 3, "D");
        SetCell(terminal.Buffer, yBase + 4, ">");

        terminal.Write("\x1b[1;4r\x1b[5;1H\x1b[1M");

        terminal.Buffer.YBase.Should().Be(yBase);
        (terminal.Buffer.GetLine(yBase + 0)?[0].Content).Should().Be("A");
        (terminal.Buffer.GetLine(yBase + 1)?[0].Content).Should().Be("B");
        (terminal.Buffer.GetLine(yBase + 2)?[0].Content).Should().Be("C");
        (terminal.Buffer.GetLine(yBase + 3)?[0].Content).Should().Be("D");
        (terminal.Buffer.GetLine(yBase + 4)?[0].Content).Should().Be(">");
    }

    [TestMethod]
    public void AlternateBuffer_NoScrollback_MultipleScrollOperations_YBaseRemainsZero()
    {
        // Arrange
        var buffer = new TerminalBuffer(80, 24, 0);

        // Act - Multiple scroll operations with different regions
        buffer.SetScrollRegion(0, 5);
        buffer.ScrollUp(2);
        
        buffer.SetScrollRegion(10, 20);
        buffer.ScrollUp(3);
        buffer.ScrollDown(1);
        
        buffer.ResetScrollRegion();
        buffer.ScrollUp(5);

        // Assert - YBase should still be 0
        buffer.YBase.Should().Be(0);
        buffer.YDisp.Should().Be(0);
    }

    [TestMethod]
    public void AlternateBuffer_NoScrollback_LinesStillShift()
    {
        // Arrange
        var buffer = new TerminalBuffer(10, 5, 0);
        
        // Put some content in the buffer
        var line0 = buffer.GetLine(0);
        var cell = new BufferCell { Content = "A", Width = 1 };
        line0?.SetCell(0, ref cell);
        
        var line1 = buffer.GetLine(1);
        cell = new BufferCell { Content = "B", Width = 1 };
        line1?.SetCell(0, ref cell);

        // Act - Scroll up (should shift lines, not add to scrollback)
        buffer.ScrollUp(1);

        // Assert - Line with "B" should now be at position 0
        var newLine0 = buffer.GetLine(0);
        (newLine0?[0].Content).Should().Be("B");
        
        // YBase should remain 0
        buffer.YBase.Should().Be(0);
    }

    [TestMethod]
    public void AlternateBuffer_NoScrollback_ScrollRegionAtTop_ContentScrollsCorrectly()
    {
        // Arrange - This tests the DECSTBM case like [1;5r
        var buffer = new TerminalBuffer(10, 10, 0);
        
        // Set scroll region from row 0 to row 4 (5 rows)
        buffer.SetScrollRegion(0, 4);
        
        // Put content in the scroll region
        for (int i = 0; i <= 4; i++)
        {
            var line = buffer.GetLine(i);
            var cell = new BufferCell { Content = ((char)('A' + i)).ToString(), Width = 1 };
            line?.SetCell(0, ref cell);
        }
        
        // Put content below scroll region (should not be affected)
        var lineBelow = buffer.GetLine(5);
        var bellowCell = new BufferCell { Content = "X", Width = 1 };
        lineBelow?.SetCell(0, ref bellowCell);

        // Act - Scroll up within the region
        buffer.ScrollUp(1);

        // Assert - Content should have scrolled within the region
        (buffer.GetLine(0)?[0].Content).Should().Be("B"); // A scrolled out
        (buffer.GetLine(1)?[0].Content).Should().Be("C");
        (buffer.GetLine(2)?[0].Content).Should().Be("D");
        (buffer.GetLine(3)?[0].Content).Should().Be("E");
        // Line 4 should be blank (new line inserted at bottom of scroll region)
        
        // Content below scroll region should be unchanged
        (buffer.GetLine(5)?[0].Content).Should().Be("X");
        
        // YBase should remain 0
        buffer.YBase.Should().Be(0);
        buffer.YDisp.Should().Be(0);
    }

    [TestMethod]
    public void NormalBuffer_WithScrollback_ScrollUpAtTop_YBaseIncrements()
    {
        // Arrange - Create buffer WITH scrollback (normal buffer)
        var buffer = new TerminalBuffer(80, 24, 1000);

        // Act - Scroll up with scroll region at top
        buffer.SetScrollRegion(0, 23);
        buffer.ScrollUp(5);

        // Assert - YBase SHOULD increment because we have scrollback
        buffer.YBase.Should().Be(5);
        buffer.YDisp.Should().Be(5);
    }

    #endregion

    #region Reflow Tests

    [TestMethod]
    public void Reflow_DoesNotWrapEmptyLines()
    {
        var buffer = new TerminalBuffer(80, 24, 1000);
        var initialLength = buffer.Lines.Length;

        buffer.Resize(75, 24);

        buffer.Lines.Length.Should().Be(initialLength);
    }

    [TestMethod]
    public void Reflow_ShrinksRowLength()
    {
        var buffer = new TerminalBuffer(80, 24, 1000);
        buffer.Resize(5, 10);

        for (var i = 0; i < 10; i++)
        {
            (buffer.Lines[i]!.Length).Should().Be(5);
        }
    }

    [TestMethod]
    public void Reflow_WrapsAndUnwrapsLines()
    {
        var buffer = new TerminalBuffer(80, 24, 1000);
        buffer.Resize(5, 10);

        var firstLine = buffer.Lines[0]!;
        for (var i = 0; i < 5; i++)
        {
            SetCell(firstLine, i, ((char)('a' + i)).ToString());
        }

        buffer.SetCursorRaw(0, 1);
        (buffer.Lines[0]!.TranslateToString()).Should().Be("abcde");

        buffer.Resize(1, 10);
        (buffer.Lines[0]!.TranslateToString()).Should().Be("a");
        (buffer.Lines[1]!.TranslateToString()).Should().Be("b");
        (buffer.Lines[2]!.TranslateToString()).Should().Be("c");
        (buffer.Lines[3]!.TranslateToString()).Should().Be("d");
        (buffer.Lines[4]!.TranslateToString()).Should().Be("e");

        buffer.Resize(5, 10);
        (buffer.Lines[0]!.TranslateToString()).Should().Be("abcde");
    }

    [TestMethod]
    public void Reflow_RemovesCorrectRowsWhenGrowingLarger()
    {
        var buffer = new TerminalBuffer(80, 24, 1000);
        buffer.Resize(10, 10);
        buffer.SetCursorRaw(0, 2);

        for (var i = 0; i < 10; i++)
        {
            SetCell(buffer.Lines[0]!, i, ((char)('a' + i)).ToString());
            SetCell(buffer.Lines[1]!, i, ((char)('0' + i)).ToString());
        }

        buffer.Resize(2, 10);
        (buffer.Lines[0]!.TranslateToString()).Should().Be("ab");
        (buffer.Lines[1]!.TranslateToString()).Should().Be("cd");
        (buffer.Lines[2]!.TranslateToString()).Should().Be("ef");
        (buffer.Lines[3]!.TranslateToString()).Should().Be("gh");
        (buffer.Lines[4]!.TranslateToString()).Should().Be("ij");
        (buffer.Lines[5]!.TranslateToString()).Should().Be("01");
        (buffer.Lines[6]!.TranslateToString()).Should().Be("23");
        (buffer.Lines[7]!.TranslateToString()).Should().Be("45");
        (buffer.Lines[8]!.TranslateToString()).Should().Be("67");
        (buffer.Lines[9]!.TranslateToString()).Should().Be("89");

        buffer.Resize(10, 10);
        (buffer.Lines[0]!.TranslateToString()).Should().Be("abcdefghij");
        (buffer.Lines[1]!.TranslateToString()).Should().Be("0123456789");
    }

    [TestMethod]
    public void Reflow_TransfersCombinedCharData()
    {
        var buffer = new TerminalBuffer(80, 24, 1000);
        buffer.Resize(4, 3);
        buffer.SetCursorRaw(0, 2);

        SetCell(buffer.Lines[0]!, 0, "a");
        SetCell(buffer.Lines[0]!, 1, "b");
        SetCell(buffer.Lines[0]!, 2, "c");
        SetCell(buffer.Lines[0]!, 3, "😁");

        buffer.Resize(2, 3);
        (buffer.Lines[0]!.TranslateToString()).Should().Be("ab");
        (buffer.Lines[1]!.TranslateToString()).Should().Be("c😁");
    }

    [TestMethod]
    public void Reflow_WideCharactersWhenShrinking()
    {
        var buffer = new TerminalBuffer(80, 24, 1000);
        buffer.Resize(12, 10);
        buffer.SetCursorRaw(0, 2);

        for (var i = 0; i < 12; i += 4)
        {
            SetWideCell(buffer.Lines[0]!, i, "汉");
            SetWideCell(buffer.Lines[1]!, i, "汉");
        }
        for (var i = 2; i < 12; i += 4)
        {
            SetWideCell(buffer.Lines[0]!, i, "语");
            SetWideCell(buffer.Lines[1]!, i, "语");
        }
        buffer.Lines[1]!.IsWrapped = true;

        buffer.Resize(11, 10);
        (buffer.Lines[0]!.TranslateToString(trimRight: true)).Should().Be("汉语汉语汉");
        (buffer.Lines[1]!.TranslateToString(trimRight: true)).Should().Be("语汉语汉语");
        (buffer.Lines[2]!.TranslateToString(trimRight: true)).Should().Be("汉语");

        buffer.Resize(7, 10);
        (buffer.Lines[0]!.TranslateToString(trimRight: true)).Should().Be("汉语汉");
        (buffer.Lines[1]!.TranslateToString(trimRight: true)).Should().Be("语汉语");
        (buffer.Lines[2]!.TranslateToString(trimRight: true)).Should().Be("汉语汉");
        (buffer.Lines[3]!.TranslateToString(trimRight: true)).Should().Be("语汉语");
    }

    [TestMethod]
    public void Reflow_SkipsGroupsWithNonNormalLineAttribute()
    {
        var buffer = new TerminalBuffer(10, 5, 100);
        buffer.Resize(4, 5);

        SetCell(buffer.Lines[0]!, 0, "a");
        SetCell(buffer.Lines[0]!, 1, "b");
        SetCell(buffer.Lines[0]!, 2, "c");
        SetCell(buffer.Lines[0]!, 3, "d");
        buffer.Lines[1]!.IsWrapped = true;
        buffer.Lines[1]!.LineAttribute = LineAttribute.DoubleWidth;
        SetCell(buffer.Lines[1]!, 0, "e");
        SetCell(buffer.Lines[1]!, 1, "f");
        SetCell(buffer.Lines[1]!, 2, "g");
        SetCell(buffer.Lines[1]!, 3, "h");

        buffer.Resize(2, 5);

        (buffer.Lines[0]!.TranslateToString()).Should().Be("ab");
        (buffer.Lines[1]!.TranslateToString(trimRight: false)).Should().Be("ef");
        (buffer.Lines[1]!.LineAttribute).Should().Be(LineAttribute.DoubleWidth);
    }

    [TestMethod]
    public void Reflow_RaisesTrimmedWhenLinesRemovedFromTop()
    {
        var buffer = new TerminalBuffer(80, 5, 1);
        buffer.Resize(10, 5);

        for (var i = 0; i < 10; i++)
        {
            SetCell(buffer.Lines[3]!, i, ((char)('a' + i)).ToString());
        }

        buffer.SetCursorRaw(0, 4);
        var trimmedTotal = 0;
        buffer.Trimmed += amount => trimmedTotal += amount;

        buffer.Resize(2, 5);

        (trimmedTotal > 0).Should().BeTrue();
    }

    [TestMethod]
    public void Reflow_CursorStaysOnSameCharacterThroughShrinkGrow()
    {
        var buffer = new TerminalBuffer(20, 5, 100);
        buffer.Resize(20, 5);

        for (var i = 0; i < 20; i++)
        {
            SetCell(buffer.Lines[0]!, i, ((char)('a' + i)).ToString());
        }

        buffer.SetCursorRaw(9, 2);
        buffer.Resize(10, 5);
        buffer.Resize(20, 5);

        buffer.X.Should().Be(9);
        buffer.Y.Should().Be(2);
        (buffer.Lines[0]!.TranslateToString(trimRight: true)).Should().Be("abcdefghijklmnopqrst");
    }

    [TestMethod]
    public void Reflow_PendingWrapXEqualsColsDoesNotCrash()
    {
        var buffer = new TerminalBuffer(10, 5, 100);
        buffer.Resize(10, 5);
        buffer.SetCursorRaw(10, 0);

        buffer.Resize(5, 5);
        buffer.Resize(10, 5);

        ((buffer.X >= 0)).Should().BeTrue();
    }

    [TestMethod]
    public void Reflow_AtCapacityShrinkingRows_KeepsNewestLines()
    {
        var buffer = new TerminalBuffer(10, 5, 5);
        buffer.ScrollUp(5);
        buffer.Lines.Length.Should().Be(10);

        SetCell(buffer.Lines[buffer.YBase + 4]!, 0, ">");
        buffer.SetCursorRaw(0, 4);

        buffer.Resize(10, 3);

        (buffer.Lines[7]![0].Content).Should().Be(">");
    }

    [TestMethod]
    public void Reflow_AltBufferTruncatesWithoutReflow()
    {
        var buffer = new TerminalBuffer(10, 5, 0, hasScrollback: false);
        for (var i = 0; i < 10; i++)
        {
            SetCell(buffer.Lines[0]!, i, ((char)('a' + i)).ToString());
        }

        buffer.Resize(5, 5);

        (buffer.Lines[0]!.TranslateToString(trimRight: true)).Should().Be("abcde");
        buffer.Lines.Length.Should().Be(5);
    }

    [TestMethod]
    public void Reflow_GrowWhenLastBufferRowIsWrappedContinuation()
    {
        var buffer = new TerminalBuffer(5, 4, 100);
        buffer.Resize(5, 4);
        for (var i = 0; i < 5; i++)
        {
            SetCell(buffer.Lines[2]!, i, ((char)('a' + i)).ToString());
            SetCell(buffer.Lines[3]!, i, ((char)('A' + i)).ToString());
        }
        buffer.Lines[3]!.IsWrapped = true;   // last buffer row is a continuation
        buffer.SetCursorRaw(0, 0);           // cursor parked away so the group is not skipped

        buffer.Resize(10, 4);

        (buffer.Lines[2]!.TranslateToString(trimRight: true)).Should().Be("abcdeABCDE");
    }

    [TestMethod]
    public void Reflow_WideCharactersWhenGrowing()
    {
        var buffer = new TerminalBuffer(80, 24, 1000);
        buffer.Resize(6, 12);

        var glyphs = new[] { "汉", "语", "測", "試" };
        for (var row = 0; row < 4; row++)
        {
            for (var col = 0; col < 6; col += 2)
            {
                var glyphIndex = (row * 3 + col / 2) % glyphs.Length;
                SetWideCell(buffer.Lines[row]!, col, glyphs[glyphIndex]);
            }
        }
        buffer.Lines[1]!.IsWrapped = true;
        buffer.Lines[2]!.IsWrapped = true;
        buffer.Lines[3]!.IsWrapped = true;

        (buffer.Lines[0]!.TranslateToString(trimRight: true)).Should().Be("汉语測");
        (buffer.Lines[1]!.TranslateToString(trimRight: true)).Should().Be("試汉语");
        (buffer.Lines[2]!.TranslateToString(trimRight: true)).Should().Be("測試汉");
        (buffer.Lines[3]!.TranslateToString(trimRight: true)).Should().Be("语測試");

        buffer.SetCursorRaw(0, 5);

        buffer.Resize(7, 12);

        var combined = buffer.Lines[0]!.TranslateToString(trimRight: true)
            + buffer.Lines[1]!.TranslateToString(trimRight: true)
            + buffer.Lines[2]!.TranslateToString(trimRight: true)
            + buffer.Lines[3]!.TranslateToString(trimRight: true);
        combined.Should().Be("汉语測試汉语測試汉语測試");
    }

    [TestMethod]
    public void ReflowSmaller_MovesCursorDownWhenViewportNotFilled()
    {
        var buffer = new TerminalBuffer(80, 24, 1000);
        buffer.Resize(4, 10);

        SetCell(buffer.Lines[0]!, 0, "a");
        SetCell(buffer.Lines[0]!, 1, "b");
        SetCell(buffer.Lines[0]!, 2, "c");
        SetCell(buffer.Lines[0]!, 3, "d");
        SetCell(buffer.Lines[1]!, 0, "e");
        SetCell(buffer.Lines[1]!, 1, "f");
        SetCell(buffer.Lines[1]!, 2, "g");
        SetCell(buffer.Lines[1]!, 3, "h");
        buffer.Lines[1]!.IsWrapped = true;
        SetCell(buffer.Lines[2]!, 0, "i");
        SetCell(buffer.Lines[2]!, 1, "j");
        SetCell(buffer.Lines[2]!, 2, "k");
        SetCell(buffer.Lines[2]!, 3, "l");

        buffer.SetCursorRaw(0, 3);
        buffer.Resize(2, 10);

        buffer.Y.Should().Be(6);
        buffer.YDisp.Should().Be(0);
        buffer.YBase.Should().Be(0);
        (buffer.Lines[0]!.TranslateToString()).Should().Be("ab");
        (buffer.Lines[1]!.TranslateToString()).Should().Be("cd");
        (buffer.Lines[2]!.TranslateToString()).Should().Be("ef");
        (buffer.Lines[3]!.TranslateToString()).Should().Be("gh");
        (buffer.Lines[1]!.IsWrapped).Should().BeTrue();
        (buffer.Lines[3]!.IsWrapped).Should().BeTrue();
    }

    [TestMethod]
    public void ReflowLarger_MovesCursorUpWhenViewportNotFilled()
    {
        var buffer = new TerminalBuffer(80, 24, 1000);
        buffer.Resize(2, 10);

        SetCell(buffer.Lines[0]!, 0, "a");
        SetCell(buffer.Lines[0]!, 1, "b");
        SetCell(buffer.Lines[1]!, 0, "c");
        SetCell(buffer.Lines[1]!, 1, "d");
        buffer.Lines[1]!.IsWrapped = true;
        SetCell(buffer.Lines[2]!, 0, "e");
        SetCell(buffer.Lines[2]!, 1, "f");
        SetCell(buffer.Lines[3]!, 0, "g");
        SetCell(buffer.Lines[3]!, 1, "h");
        buffer.Lines[3]!.IsWrapped = true;
        SetCell(buffer.Lines[4]!, 0, "i");
        SetCell(buffer.Lines[4]!, 1, "j");
        SetCell(buffer.Lines[5]!, 0, "k");
        SetCell(buffer.Lines[5]!, 1, "l");
        buffer.Lines[5]!.IsWrapped = true;

        buffer.SetCursorRaw(0, 6);
        buffer.Resize(4, 10);

        buffer.Y.Should().Be(3);
        buffer.YDisp.Should().Be(0);
        buffer.YBase.Should().Be(0);
        (buffer.Lines[0]!.TranslateToString()).Should().Be("abcd");
        (buffer.Lines[1]!.TranslateToString()).Should().Be("efgh");
        (buffer.Lines[2]!.TranslateToString()).Should().Be("ijkl");
    }

    private static void SetCell(TerminalBuffer buffer, int row, string content)
    {
        var line = buffer.GetLine(row);
        var cell = new BufferCell { Content = content, Width = 1 };
        line?.SetCell(0, ref cell);
    }

    private static void SetCell(BufferLine line, int col, string content, int width = 1)
    {
        var cell = new BufferCell(content, width, AttributeData.Default);
        line.SetCell(col, ref cell);
    }

    private static void SetWideCell(BufferLine line, int col, string content)
    {
        SetCell(line, col, content, 2);
        SetCell(line, col + 1, "", 0);
    }

    #endregion
}
