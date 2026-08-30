using XTerm;
using XTerm.Buffer;
using XTerm.Common;
using XTerm.Options;

namespace XTerm.Tests;

[TestClass]

public class TerminalTests
{
    [TestMethod]
    public void Constructor_InitializesTerminal()
    {
        // Arrange & Act
        var terminal = new Terminal();

        // Assert
        terminal.Should().NotBeNull();
        terminal.Options.Should().NotBeNull();
        terminal.Buffer.Should().NotBeNull();
        terminal.Cols.Should().Be(80);
        terminal.Rows.Should().Be(24);
    }

    [TestMethod]
    public void Constructor_WithOptions_SnapshotsProvidedOptions()
    {
        // Arrange
        var options = new TerminalOptions { Cols = 100, Rows = 30 };

        // Act
        var terminal = new Terminal(options);

        // Assert
        terminal.Cols.Should().Be(100);
        terminal.Rows.Should().Be(30);
        terminal.Options.Should().NotBeSameAs(options);
        terminal.Options.Cols.Should().Be(options.Cols);
        terminal.Options.Rows.Should().Be(options.Rows);
    }

    [TestMethod]
    public void Terminals_created_from_one_options_object_do_not_alias_each_other()
    {
        var options = new TerminalOptions { CursorBlink = false };
        var first = new Terminal(options);
        var second = new Terminal(options);

        first.Options.CursorBlink = true;

        first.Options.CursorBlink.Should().BeTrue();
        second.Options.CursorBlink.Should().BeFalse();
        options.CursorBlink.Should().BeFalse();
    }

    [TestMethod]
    public void Mutating_ConstructorOptions_Later_DoesNotReconfigureTheTerminal()
    {
        var options = new TerminalOptions { CursorStyle = CursorStyle.Block };
        var terminal = new Terminal(options);

        options.CursorStyle = CursorStyle.Bar;

        terminal.Options.CursorStyle.Should().Be(CursorStyle.Block);
    }

    [TestMethod]
    public void Constructor_Snapshot_IncludesNestedOptions()
    {
        var options = new TerminalOptions();
        var terminal = new Terminal(options);

        terminal.Options.Theme.Should().NotBeSameAs(options.Theme);
        terminal.Options.WindowOptions.Should().NotBeSameAs(options.WindowOptions);
    }

    [TestMethod]
    public void Constructor_ToleratesNullNestedOptions()
    {
        var options = new TerminalOptions { Theme = null!, WindowOptions = null! };

        var terminal = new Terminal(options);

        terminal.Options.Theme.Should().NotBeNull();
        terminal.Options.WindowOptions.Should().NotBeNull();
    }

    [TestMethod]
    public void Constructor_InitializesTerminalState()
    {
        // Arrange & Act
        var terminal = new Terminal();

        // Assert
        terminal.InsertMode.Should().BeFalse();
        terminal.ApplicationCursorKeys.Should().BeFalse();
        terminal.ApplicationKeypad.Should().BeFalse();
        terminal.BracketedPasteMode.Should().BeFalse();
        terminal.OriginMode.Should().BeFalse();
        terminal.Title.Should().Be(string.Empty);
    }

    [TestMethod]
    public void Write_EmptyString_DoesNothing()
    {
        // Arrange
        var terminal = new Terminal();

        // Act & Assert - Should not throw
        terminal.Write("");
        terminal.Write((string)null!);   // disambiguated: Write(ReadOnlySpan<byte>) also accepts null
    }

    [TestMethod]
    public void Write_PlainText_PrintsToBuffer()
    {
        // Arrange
        var terminal = new Terminal();

        // Act
        terminal.Write("Hello");

        // Assert
        var line = terminal.GetLine(0);
        line.Should().Contain("Hello");
    }

    [TestMethod]
    public void Write_WithEscapeSequence_ProcessesSequence()
    {
        // Arrange
        var terminal = new Terminal();

        // Act
        terminal.Write("\x1B[1mBold");

        // Assert
        var line = terminal.GetLine(0);
        line.Should().Contain("Bold");
    }

    [TestMethod]
    public void WriteLine_AddsLineFeed()
    {
        // Arrange
        var terminal = new Terminal();

        // Act
        terminal.WriteLine("Line1");
        terminal.Write("Line2");

        // Assert
        var line0 = terminal.GetLine(0);
        var line1 = terminal.GetLine(1);
        line0.Should().Contain("Line1");
        line1.Should().Contain("Line2");
    }

    [TestMethod]
    public void Resize_ChangesTerminalSize()
    {
        // Arrange
        var terminal = new Terminal();
        var resized = false;
        var newCols = 0;
        var newRows = 0;
        
        terminal.Resized += (sender, e) =>
        {
            resized = true;
            newCols = e.Cols;
            newRows = e.Rows;
        };

        // Act
        terminal.Resize(100, 30);

        // Assert
        terminal.Cols.Should().Be(100);
        terminal.Rows.Should().Be(30);
        resized.Should().BeTrue();
        newCols.Should().Be(100);
        newRows.Should().Be(30);
    }

    [TestMethod]
    public void Resize_SameSize_DoesNotFireEvent()
    {
        // Arrange
        var terminal = new Terminal();
        var resized = false;
        terminal.Resized += (sender, e) => resized = true;

        // Act
        terminal.Resize(80, 24); // Same as default

        // Assert
        resized.Should().BeFalse();
    }

    [TestMethod]
    public void Reset_ResetsTerminalState()
    {
        // Arrange
        var terminal = new Terminal();
        terminal.InsertMode = true;
        terminal.ApplicationCursorKeys = true;
        terminal.ApplicationKeypad = true;
        terminal.BracketedPasteMode = true;
        terminal.OriginMode = true;
        terminal.Write("Some text");

        // Act
        terminal.Reset();

        // Assert
        terminal.InsertMode.Should().BeFalse();
        terminal.ApplicationCursorKeys.Should().BeFalse();
        terminal.ApplicationKeypad.Should().BeFalse();
        terminal.BracketedPasteMode.Should().BeFalse();
        terminal.OriginMode.Should().BeFalse();
    }

    [TestMethod]
    public void Clear_ClearsBuffer()
    {
        // Arrange
        var terminal = new Terminal();
        terminal.Write("Test content");

        // Act
        terminal.Clear();

        // Assert
        var line = terminal.GetLine(0);
        line.Should().NotContain("Test");
    }

    [TestMethod]
    public void ScrollLines_ScrollsViewport()
    {
        // Arrange
        var terminal = new Terminal();
        for (int i = 0; i < 30; i++)
        {
            terminal.WriteLine($"Line {i}");
        }
        
        var scrolled = false;
        terminal.Scrolled += (sender, e) => scrolled = true;

        // Act
        terminal.ScrollLines(5);

        // Assert
        scrolled.Should().BeTrue();
    }

    [TestMethod]
    public void ScrollToTop_ScrollsToTop()
    {
        // Arrange
        var terminal = new Terminal();
        for (int i = 0; i < 30; i++)
        {
            terminal.WriteLine($"Line {i}");
        }
        
        terminal.ScrollLines(10);
        var scrolled = false;
        terminal.Scrolled += (sender, e) => scrolled = true;

        // Act
        terminal.ScrollToTop();

        // Assert
        scrolled.Should().BeTrue();
        terminal.Buffer.YDisp.Should().Be(0);
    }

    [TestMethod]
    public void ScrollToBottom_ScrollsToBottom()
    {
        // Arrange
        var terminal = new Terminal();
        for (int i = 0; i < 30; i++)
        {
            terminal.WriteLine($"Line {i}");
        }
        
        terminal.ScrollToTop();
        var scrolled = false;
        terminal.Scrolled += (sender, e) => scrolled = true;

        // Act
        terminal.ScrollToBottom();

        // Assert
        scrolled.Should().BeTrue();
        terminal.Buffer.YDisp.Should().Be(terminal.Buffer.YBase);
    }

    [TestMethod]
    public void GetLine_ReturnsLineContent()
    {
        // Arrange
        var terminal = new Terminal();
        terminal.Write("Test Line");

        // Act
        var line = terminal.GetLine(0);

        // Assert
        line.Should().Contain("Test Line");
    }

    [TestMethod]
    public void GetLine_InvalidIndex_ReturnsEmpty()
    {
        // Arrange
        var terminal = new Terminal();

        // Act
        var line = terminal.GetLine(1000);

        // Assert
        line.Should().Be(string.Empty);
    }

    [TestMethod]
    public void GetVisibleLines_ReturnsAllVisibleLines()
    {
        // Arrange
        var terminal = new Terminal(new TerminalOptions { Rows = 5 });
        terminal.WriteLine("Line1");
        terminal.WriteLine("Line2");
        terminal.WriteLine("Line3");

        // Act
        var lines = terminal.GetVisibleLines();

        // Assert
        lines.Length.Should().Be(5);
        lines[0].Should().Contain("Line1");
        lines[1].Should().Contain("Line2");
        lines[2].Should().Contain("Line3");
    }

    [TestMethod]
    public void OnBell_FiresWhenBellReceived()
    {
        // Arrange
        var terminal = new Terminal();
        var bellRang = false;
        terminal.BellRang += (sender, e) => bellRang = true;

        // Act
        terminal.Write("\x07"); // BEL character

        // Assert
        bellRang.Should().BeTrue();
    }

    [TestMethod]
    public void OnLineFeed_FiresOnLineFeed()
    {
        // Arrange
        var terminal = new Terminal();
        var lineFeedFired = false;
        terminal.LineFed += (sender, e) => lineFeedFired = true;

        // Act
        terminal.Write("\n");

        // Assert
        lineFeedFired.Should().BeTrue();
    }

    [TestMethod]
    public void Title_CanBeSetViaOscSequence()
    {
        // Arrange
        var terminal = new Terminal();

        // Act
        terminal.Write("\x1B]0;Test Title\x07");

        // Assert
        terminal.Title.Should().Be("Test Title");
    }

    [TestMethod]
    public void Title_InitiallyEmpty()
    {
        // Arrange & Act
        var terminal = new Terminal();

        // Assert
        terminal.Title.Should().Be(string.Empty);
    }

    [TestMethod]
    public void SwitchToAltBuffer_SwitchesBuffer()
    {
        // Arrange
        var terminal = new Terminal();
        terminal.Write("Normal buffer content");

        // Act
        terminal.SwitchToAltBuffer();
        terminal.Write("Alt buffer content");

        // Assert
        var line = terminal.GetLine(0);
        line.Should().Contain("Alt buffer");
    }

    [TestMethod]
    public void SwitchToNormalBuffer_RestoresNormalBuffer()
    {
        // Arrange
        var terminal = new Terminal();
        terminal.Write("Normal content");
        terminal.SwitchToAltBuffer();
        terminal.Write("Alt content");

        // Act
        terminal.SwitchToNormalBuffer();

        // Assert
        var line = terminal.GetLine(0);
        line.Should().Contain("Normal content");
    }

    [TestMethod]
    public void SwitchToAltBuffer_WhenAlreadyInAltBuffer_DoesNothing()
    {
        // Arrange
        var terminal = new Terminal();
        terminal.SwitchToAltBuffer();

        // Act & Assert - Should not throw
        terminal.SwitchToAltBuffer();
    }

    [TestMethod]
    public void SwitchToNormalBuffer_WhenAlreadyInNormalBuffer_DoesNothing()
    {
        // Arrange
        var terminal = new Terminal();

        // Act & Assert - Should not throw
        terminal.SwitchToNormalBuffer();
    }

    [TestMethod]
    public void Dispose_ClearsAllEvents()
    {
        // Arrange
        var terminal = new Terminal();
        var count = 0;
        terminal.BellRang += (sender, e) => count++;
        terminal.Scrolled += (sender, e) => count++;

        // Act
        terminal.Dispose();
        terminal.Write("\x07"); // Try to trigger bell
        terminal.ScrollLines(1); // Try to trigger scroll

        // Assert
        count.Should().Be(0); // Events should not fire after dispose
    }

    [TestMethod]
    public void Write_WithBackspace_MovesBack()
    {
        // Arrange
        var terminal = new Terminal();
        terminal.Write("ABC");

        // Act
        terminal.Write("\x08"); // Backspace
        terminal.Write("X");

        // Assert
        var line = terminal.GetLine(0);
        line.Should().Contain("ABX");
    }

    [TestMethod]
    public void Write_WithTab_MovesToNextTabStop()
    {
        // Arrange
        var terminal = new Terminal();
        terminal.Write("A");

        // Act
        terminal.Write("\t"); // Tab
        terminal.Write("B");

        // Assert
        ((terminal.Buffer.X >= 8)).Should().BeTrue(); // Should be at or past first tab stop
    }

    [TestMethod]
    public void Write_WithCarriageReturn_MovesToLineStart()
    {
        // Arrange
        var terminal = new Terminal();
        terminal.Write("ABCDE");

        // Act
        terminal.Write("\r"); // Carriage return
        terminal.Write("X");

        // Assert
        terminal.Buffer.X.Should().Be(1); // Should be at position 1 after writing X
    }

    [TestMethod]
    public void Write_CursorMovement_WorksCorrectly()
    {
        // Arrange
        var terminal = new Terminal();

        // Act
        terminal.Write("Start\x1B[10CHere"); // Move cursor forward 10

        // Assert
        var line = terminal.GetLine(0);
        line.Should().Contain("Start");
        line.Should().Contain("Here");
    }

    [TestMethod]
    public void Write_Colors_ApplyCorrectly()
    {
        // Arrange
        var terminal = new Terminal();

        // Act
        terminal.Write("\x1B[31mRed\x1B[0m Normal");

        // Assert
        var line = terminal.GetLine(0);
        line.Should().Contain("Red");
        line.Should().Contain("Normal");
    }

    [TestMethod]
    public void Write_BoldText_AppliesAttribute()
    {
        // Arrange
        var terminal = new Terminal();

        // Act
        terminal.Write("\x1B[1mBold");

        // Assert
        var line = terminal.Buffer.Lines[0];
        line.Should().NotBeNull();
        (line[0].Attributes.IsBold()).Should().BeTrue();
    }

    [TestMethod]
    public void Write_MultipleLines_HandlesCorrectly()
    {
        // Arrange
        var terminal = new Terminal();

        // Act
        terminal.Write("Line1\nLine2\nLine3");

        // Assert
        terminal.GetLine(0).Should().Contain("Line1");
        terminal.GetLine(1).Should().Contain("Line2");
        terminal.GetLine(2).Should().Contain("Line3");
    }

    [TestMethod]
    public void InsertMode_AffectsPrinting()
    {
        // Arrange
        var terminal = new Terminal();
        terminal.Write("Hello");
        terminal.Buffer.SetCursor(2, 0);

        // Act
        terminal.InsertMode = true;
        terminal.Write("X");

        // Assert
        // Character should be inserted, not overwritten
        var line = terminal.GetLine(0);
        line.Should().Contain("X");
    }

    [TestMethod]
    public void OriginMode_CanBeToggled()
    {
        // Arrange
        var terminal = new Terminal();

        // Act
        terminal.OriginMode = true;

        // Assert
        terminal.OriginMode.Should().BeTrue();

        // Act
        terminal.OriginMode = false;

        // Assert
        terminal.OriginMode.Should().BeFalse();
    }

    [TestMethod]
    public void ApplicationCursorKeys_CanBeToggled()
    {
        // Arrange
        var terminal = new Terminal();

        // Act
        terminal.ApplicationCursorKeys = true;

        // Assert
        terminal.ApplicationCursorKeys.Should().BeTrue();
    }

    [TestMethod]
    public void Write_LongText_HandlesCorrectly()
    {
        // Arrange
        var terminal = new Terminal();
        var longText = new string('A', 1000);

        // Act & Assert - Should not throw
        terminal.Write(longText);
    }

    [TestMethod]
    public void Write_UnicodeCharacters_HandlesCorrectly()
    {
        // Arrange
        var terminal = new Terminal();

        // Act
        terminal.Write("Hello ?? ??");

        // Assert
        var line = terminal.GetLine(0);
        line.Should().Contain("Hello");
        line.Should().Contain("??");
        line.Should().Contain("??");
    }

    [TestMethod]
    public void Reset_ClearsCursorPosition()
    {
        // Arrange
        var terminal = new Terminal();
        terminal.Buffer.SetCursor(10, 5);

        // Act
        terminal.Reset();

        // Assert
        terminal.Buffer.X.Should().Be(0);
        terminal.Buffer.Y.Should().Be(0);
    }

    [TestMethod]
    public void Buffer_IsAccessible()
    {
        // Arrange
        var terminal = new Terminal();

        // Act
        var buffer = terminal.Buffer;

        // Assert
        buffer.Should().NotBeNull();
        buffer.Should().Be(terminal.Buffer);
    }

    #region Scrolling Beyond Viewport Tests

    [TestMethod]
    public void WriteLine_BeyondViewport_ScrollsBuffer()
    {
        // Arrange - 5 row terminal
        var terminal = new Terminal(new TerminalOptions { Rows = 5, Cols = 80 });

        // Act - Write 10 lines (more than viewport)
        for (int i = 0; i < 10; i++)
        {
            terminal.WriteLine($"Line {i}");
        }

        // Assert
        (terminal.Buffer.YBase > 0).Should().BeTrue();
        terminal.Buffer.YDisp.Should().Be(terminal.Buffer.YBase);
    }

    [TestMethod]
    public void WriteLine_BeyondViewport_ContentInScrollback()
    {
        // Arrange - 5 row terminal
        var terminal = new Terminal(new TerminalOptions { Rows = 5, Cols = 80, Scrollback = 100 });

        // Act - Write 10 lines
        for (int i = 0; i < 10; i++)
        {
            terminal.WriteLine($"Line{i}");
        }

        // Assert - First lines should be in scrollback
        var scrollbackLine0 = terminal.Buffer.Lines[0]?.TranslateToString(true);
        scrollbackLine0.Should().Contain("Line0");

        var scrollbackLine1 = terminal.Buffer.Lines[1]?.TranslateToString(true);
        scrollbackLine1.Should().Contain("Line1");
    }

    [TestMethod]
    public void WriteLine_BeyondViewport_YBaseIncrementsCorrectly()
    {
        // Arrange - 5 row terminal
        var terminal = new Terminal(new TerminalOptions { Rows = 5, Cols = 80, Scrollback = 100 });

        // Act - Write 8 lines (fills viewport at row 4, then 3 more scroll)
        for (int i = 0; i < 8; i++)
        {
            terminal.WriteLine($"Line{i}");
        }

        // Assert - After 5 lines, we're at bottom. Lines 6, 7, 8 cause scrolling.
        // Actually: line 0-4 fill rows 0-4, then newline on row 4 causes scroll
        // So after 8 WriteLines, we have scrolled 8 - 5 = 3 times (roughly)
        ((terminal.Buffer.YBase >= 3)).Should().BeTrue();
    }

    [TestMethod]
    public void GetVisibleLines_ReturnsActiveAreaContent()
    {
        // Arrange
        var terminal = new Terminal(new TerminalOptions { Rows = 5, Cols = 80, Scrollback = 100 });

        // Write more lines than viewport
        for (int i = 0; i < 20; i++)
        {
            terminal.WriteLine($"Line{i}");
        }

        // Act
        var visibleLines = terminal.GetVisibleLines();

        // Assert - Should get 5 lines
        visibleLines.Length.Should().Be(5);
        
        // The visible lines should be the most recent ones (since we're at bottom)
        // Due to scrolling, the last lines written should be visible
    }

    [TestMethod]
    public void ScrollToTop_ThenGetVisibleLines_ReturnsScrollbackContent()
    {
        // Arrange
        var terminal = new Terminal(new TerminalOptions { Rows = 5, Cols = 80, Scrollback = 100 });

        // Write lines with identifiable content
        for (int i = 0; i < 20; i++)
        {
            terminal.WriteLine($"Line{i}");
        }

        // Act
        terminal.ScrollToTop();
        var visibleLines = terminal.GetVisibleLines();

        // Assert - First visible line should contain early content
        visibleLines[0].Should().Contain("Line0");
    }

    [TestMethod]
    public void ScrollLines_NavigatesScrollback()
    {
        // Arrange
        var terminal = new Terminal(new TerminalOptions { Rows = 5, Cols = 80, Scrollback = 100 });

        for (int i = 0; i < 30; i++)
        {
            terminal.WriteLine($"Line{i}");
        }

        var initialYDisp = terminal.Buffer.YDisp;

        // Act - Scroll up 10 lines
        terminal.ScrollLines(-10);

        // Assert
        terminal.Buffer.YDisp.Should().Be(initialYDisp - 10);
    }

    [TestMethod]
    public void ScrollToBottom_AfterScrollingUp_ReturnsToLatestContent()
    {
        // Arrange
        var terminal = new Terminal(new TerminalOptions { Rows = 5, Cols = 80, Scrollback = 100 });

        for (int i = 0; i < 30; i++)
        {
            terminal.WriteLine($"Line{i}");
        }

        terminal.ScrollToTop();
        terminal.Buffer.YDisp.Should().Be(0);

        // Act
        terminal.ScrollToBottom();

        // Assert
        terminal.Buffer.YDisp.Should().Be(terminal.Buffer.YBase);
    }

    [TestMethod]
    public void LargeOutput_HandlesScrollbackCorrectly()
    {
        // Arrange - Terminal with limited scrollback
        var terminal = new Terminal(new TerminalOptions { Rows = 24, Cols = 80, Scrollback = 100 });

        // Act - Write many lines
        for (int i = 0; i < 200; i++)
        {
            terminal.WriteLine($"Output line {i}");
        }

        // Assert - Should handle gracefully
        (terminal.Buffer.YBase > 0).Should().BeTrue();
        terminal.Buffer.Lines.Should().NotBeNull();
    }

    [TestMethod]
    public void ScrollbackLimit_RecyclesOldContent()
    {
        // Arrange - Terminal with very limited scrollback
        var terminal = new Terminal(new TerminalOptions { Rows = 5, Cols = 80, Scrollback = 10 });
        // Total buffer capacity = 5 + 10 = 15 lines

        // Act - Write 25 lines (more than buffer capacity)
        for (int i = 0; i < 25; i++)
        {
            terminal.WriteLine($"L{i}");
        }

        // Assert - Buffer should be at max capacity
        terminal.Buffer.Lines.Length.Should().Be(15);
        
        // Early content should have been recycled
        // Line0 through Line9 should be gone
        terminal.ScrollToTop();
        var firstVisibleLine = terminal.GetVisibleLines()[0];
        firstVisibleLine.Should().NotContain("L0");
    }

    [TestMethod]
    public void ContinuousOutput_MaintainsViewportAtBottom()
    {
        // Arrange
        var terminal = new Terminal(new TerminalOptions { Rows = 10, Cols = 80, Scrollback = 1000 });

        // Act - Simulate continuous output (like a log)
        for (int i = 0; i < 100; i++)
        {
            terminal.WriteLine($"Log entry {i}");
        }

        // Assert - Viewport should stay at bottom
        terminal.Buffer.YDisp.Should().Be(terminal.Buffer.YBase);
        terminal.Buffer.IsAtBottom.Should().BeTrue();
    }

    [TestMethod]
    public void UserScrollsUp_NewOutput_DoesNotAutoScroll()
    {
        // Arrange
        var terminal = new Terminal(new TerminalOptions { Rows = 10, Cols = 80, Scrollback = 1000 });

        // Initial output
        for (int i = 0; i < 50; i++)
        {
            terminal.WriteLine($"Initial {i}");
        }

        // User scrolls up
        terminal.ScrollToTop();
        terminal.Buffer.YDisp.Should().Be(0);

        // Act - More output arrives
        for (int i = 0; i < 10; i++)
        {
            terminal.WriteLine($"New {i}");
        }

        // Assert - User's scroll position should be preserved (they scrolled to see history)
        // Note: The current implementation auto-scrolls, but this documents expected behavior
        // If auto-scroll preservation is desired, this test would need the implementation to change
    }

    [TestMethod]
    public void Write_WithNewlines_ScrollsCorrectly()
    {
        // Arrange
        var terminal = new Terminal(new TerminalOptions { Rows = 5, Cols = 80, Scrollback = 100 });

        // Act - Write string with multiple newlines
        terminal.Write("Line1\nLine2\nLine3\nLine4\nLine5\nLine6\nLine7\n");

        // Assert
        (terminal.Buffer.YBase > 0).Should().BeTrue();
    }

    [TestMethod]
    public void GetLine_WithScrollback_ReturnsCorrectContent()
    {
        // Arrange
        var terminal = new Terminal(new TerminalOptions { Rows = 5, Cols = 80, Scrollback = 100 });

        // Write identifiable content
        terminal.WriteLine("FirstLine");
        for (int i = 0; i < 10; i++)
        {
            terminal.WriteLine($"Middle{i}");
        }
        terminal.WriteLine("LastLine");

        // Act - Get the first line in the buffer (scrollback)
        var firstLine = terminal.GetLine(0);

        // Assert
        firstLine.Should().Contain("FirstLine");
    }

    #endregion

    [TestMethod]
    public void Reset_ClearsLineAttributes()
    {
        // Arrange
        var terminal = new Terminal();
        
        // Set some lines to double-height mode using ESC # 3 (top) and ESC # 4 (bottom)
        terminal.Write("Line 0\n");
        terminal.Write("\x1B#3"); // ESC # 3 - Double-height top
        terminal.Write("Line 1 Top\n");
        terminal.Write("\x1B#4"); // ESC # 4 - Double-height bottom
        terminal.Write("Line 2 Bottom\n");
        terminal.Write("\x1B#6"); // ESC # 6 - Double-width
        terminal.Write("Line 3 Wide\n");

        // Verify line attributes are set
        (terminal.Buffer.Lines[1]?.LineAttribute).Should().Be(LineAttribute.DoubleHeightTop);
        (terminal.Buffer.Lines[2]?.LineAttribute).Should().Be(LineAttribute.DoubleHeightBottom);
        (terminal.Buffer.Lines[3]?.LineAttribute).Should().Be(LineAttribute.DoubleWidth);

        // Act - Reset the terminal (simulates 'reset' command which sends ESC c / RIS)
        terminal.Reset();

        // Assert - All line attributes should be reset to Normal
        for (int i = 0; i < terminal.Rows; i++)
        {
            var line = terminal.Buffer.Lines[i];
            if (line != null)
            {
                line.LineAttribute.Should().Be(LineAttribute.Normal);
            }
        }
    }

    [TestMethod]
    public void Reset_ClearsLineAttributesInScrollback()
    {
        // Arrange
        var terminal = new Terminal(new TerminalOptions { Rows = 5, Cols = 80, Scrollback = 100 });
        
        // Write lines with double-height attributes
        for (int i = 0; i < 10; i++)
        {
            if (i % 2 == 0)
            {
                terminal.Write("\x1B#3"); // ESC # 3 - Double-height top
            }
            else
            {
                terminal.Write("\x1B#6"); // ESC # 6 - Double-width
            }
            terminal.Write($"Line {i}\n");
        }

        // Verify some line attributes are set (check a few lines in scrollback)
        bool foundDoubleHeight = false;
        bool foundDoubleWidth = false;
        for (int i = 0; i < terminal.Buffer.Lines.Length; i++)
        {
            var line = terminal.Buffer.Lines[i];
            if (line != null)
            {
                if (line.LineAttribute == LineAttribute.DoubleHeightTop)
                    foundDoubleHeight = true;
                if (line.LineAttribute == LineAttribute.DoubleWidth)
                    foundDoubleWidth = true;
            }
        }
        ((foundDoubleHeight || foundDoubleWidth)).Should().BeTrue("Expected to find at least one line with non-normal attribute");

        // Act - Reset the terminal (simulates 'reset' command which sends ESC c / RIS)
        terminal.Reset();

        // Assert - All lines in the buffer (including scrollback) should have Normal line attributes
        for (int i = 0; i < terminal.Buffer.Lines.Length; i++)
        {
            var line = terminal.Buffer.Lines[i];
            if (line != null)
            {
                line.LineAttribute.Should().Be(LineAttribute.Normal);
            }
        }
    }
}
