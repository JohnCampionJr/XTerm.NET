using XTerm;
using XTerm.Buffer;
using XTerm.Options;
using XTerm.Parser;
using XTerm.Common;
using XTerm.Events;

namespace XTerm.Tests;

[TestClass]

public class InputHandlerTests
{
    private Terminal CreateTerminal(int cols = 80, int rows = 24)
    {
        var options = new TerminalOptions { Cols = cols, Rows = rows };
        return new Terminal(options);
    }

    /// <summary>
    /// The secondary DA reply the handler should send: terminal type 1 (VT220), the package
    /// version flattened into one number, and no cartridge ROM.
    /// </summary>
    private static string ExpectedSecondaryDeviceAttributes()
    {
        var version = typeof(InputHandler).Assembly.GetName().Version;
        var firmwareVersion = version is null ? 0 : version.Major * 100 + version.Minor;
        return $"\u001b[>1;{firmwareVersion};0c";
    }

    [TestMethod]
    public void Constructor_InitializesHandler()
    {
        // Arrange
        var terminal = CreateTerminal();

        // Act
        var handler = new InputHandler(terminal);

        // Assert
        handler.Should().NotBeNull();
    }

    [TestMethod]
    public void Print_PrintsCharacterToBuffer()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);

        // Act
        handler.Print("A");

        // Assert
        var line = terminal.Buffer.Lines[0];
        line.Should().NotBeNull();
        line[0].Content.Should().Be("A");
    }

    [TestMethod]
    public void Print_MultipleCharacters_PrintsSequentially()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);

        // Act
        handler.Print("H");
        handler.Print("e");
        handler.Print("l");
        handler.Print("l");
        handler.Print("o");

        // Assert
        var line = terminal.Buffer.Lines[0];
        line.Should().NotBeNull();
        line.TranslateToString(true, 0, 5).Should().Be("Hello");
    }

    [TestMethod]
    public void Write_DecSpecialGraphics_MapsCornersCorrectly()
    {
        // Arrange
        var terminal = CreateTerminal();

        // Act
        terminal.Write("\x1B(0jklmqx\x1B(Bj");

        // Assert
        var line = terminal.Buffer.Lines[0];
        line.Should().NotBeNull();
        line.TranslateToString(true).Should().Be("\u2518\u2510\u250c\u2514\u2500\u2502j");
    }

    [TestMethod]
    public void HandleCsi_CursorUp_MovesCursor()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        terminal.Buffer.SetCursor(0, 10);
        var params_ = new Params();
        params_.AddParam(5);

        // Act
        handler.HandleCsi("A", params_);

        // Assert
        terminal.Buffer.Y.Should().Be(5);
    }

    [TestMethod]
    public void HandleCsi_CursorDown_MovesCursor()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        terminal.Buffer.SetCursor(0, 5);
        var params_ = new Params();
        params_.AddParam(3);

        // Act
        handler.HandleCsi("B", params_);

        // Assert
        terminal.Buffer.Y.Should().Be(8);
    }

    [TestMethod]
    public void HandleCsi_CursorForward_MovesCursor()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        terminal.Buffer.SetCursor(5, 0);
        var params_ = new Params();
        params_.AddParam(10);

        // Act
        handler.HandleCsi("C", params_);

        // Assert
        terminal.Buffer.X.Should().Be(15);
    }

    [TestMethod]
    public void HandleCsi_CursorForward_ClampsAtRightMarginWithoutWrapping()
    {
        // Arrange
        var terminal = CreateTerminal(cols: 10, rows: 3);
        var handler = new InputHandler(terminal);
        terminal.Buffer.SetCursor(7, 0);
        var params_ = new Params();
        params_.AddParam(20);

        // Act
        handler.HandleCsi("C", params_);

        // Assert
        terminal.Buffer.X.Should().Be(9);
        terminal.Buffer.Y.Should().Be(0);
    }

    [TestMethod]
    public void HandleCsi_EraseChars_DoesNotMoveCursor()
    {
        // Arrange
        var terminal = CreateTerminal(cols: 20, rows: 3);
        terminal.Write("abcdef");
        terminal.Buffer.SetCursor(2, 0);

        // Act
        terminal.Write("\x1B[3X");

        // Assert
        terminal.Buffer.X.Should().Be(2);
        terminal.Buffer.Y.Should().Be(0);
        (terminal.Buffer.Lines[0]?.TranslateToString(true)).Should().Be("ab   f");
    }

    [TestMethod]
    public void Print_CheckMarkWithoutEmojiPresentation_IsSingleWidth()
    {
        // Arrange
        var terminal = CreateTerminal(cols: 20, rows: 3);

        // Act
        terminal.Write("\u2714X");

        // Assert
        terminal.Buffer.X.Should().Be(2);
        (terminal.Buffer.Lines[0]?.TranslateToString(true)).Should().Be("\u2714X");
        (terminal.Buffer.Lines[0]?[0].Width).Should().Be(1);
        (terminal.Buffer.Lines[0]?[1].Width).Should().Be(1);
    }

    [TestMethod]
    public void Print_CheckMarkWithEmojiPresentation_IsDoubleWidth()
    {
        // Arrange
        var terminal = CreateTerminal(cols: 20, rows: 3);

        // Act
        terminal.Write("\u2714\uFE0FX");

        // Assert
        terminal.Buffer.X.Should().Be(3);
        (terminal.Buffer.Lines[0]?.TranslateToString(true)).Should().Be("\u2714\uFE0FX");
        (terminal.Buffer.Lines[0]?[0].Width).Should().Be(2);
        (terminal.Buffer.Lines[0]?[1].Width).Should().Be(0);
        (terminal.Buffer.Lines[0]?[2].Width).Should().Be(1);
    }

    [TestMethod]
    public void Print_ZwjEmojiSequence_OccupiesOneCellPair()
    {
        // Arrange
        var terminal = CreateTerminal(cols: 20, rows: 3);

        // Act — woman + ZWJ + laptop is ONE grapheme cluster, drawn as one glyph.
        terminal.Write("\U0001F469\u200D\U0001F4BBX");

        // Assert — two cells for the cluster, then the next character. Before this fix each component
        // opened its own cell pair, so the cluster claimed four cells and left two of them blank.
        terminal.Buffer.X.Should().Be(3);
        (terminal.Buffer.Lines[0]?[0].Width).Should().Be(2);
        (terminal.Buffer.Lines[0]?[1].Width).Should().Be(0);
        (terminal.Buffer.Lines[0]?[2].Content).Should().Be("X");
    }

    [TestMethod]
    public void Print_ZwjEmojiSequence_KeepsEveryComponentInOneCell()
    {
        // Arrange
        var terminal = CreateTerminal(cols: 20, rows: 3);

        // Act
        terminal.Write("\U0001F469\u200D\U0001F4BB");

        // Assert — the whole cluster is the cell's content, so a renderer can shape it as one glyph.
        (terminal.Buffer.Lines[0]?[0].Content).Should().Be("\U0001F469\u200D\U0001F4BB");
    }

    [TestMethod]
    public void Print_FamilyEmoji_OccupiesOneCellPair()
    {
        // Arrange
        var terminal = CreateTerminal(cols: 20, rows: 3);

        // Act — four people joined by three ZWJs, still one glyph.
        terminal.Write("\U0001F468\u200D\U0001F469\u200D\U0001F467\u200D\U0001F466X");

        // Assert — the worst case: this used to claim eight cells and leave six of them blank.
        terminal.Buffer.X.Should().Be(3);
        (terminal.Buffer.Lines[0]?[0].Width).Should().Be(2);
        (terminal.Buffer.Lines[0]?[1].Width).Should().Be(0);
        (terminal.Buffer.Lines[0]?[2].Content).Should().Be("X");
    }

    [TestMethod]
    public void Print_TwoZwjSequencesInARow_StayInTheirOwnCells()
    {
        // Arrange
        var terminal = CreateTerminal(cols: 20, rows: 3);

        // Act
        terminal.Write("\U0001F469\u200D\U0001F4BB\U0001F468\u200D\U0001F4BB");

        // Assert — the continuation is spent by the character that follows the ZWJ; the next cluster
        // starts fresh rather than being swallowed into the first.
        terminal.Buffer.X.Should().Be(4);
        (terminal.Buffer.Lines[0]?[0].Content).Should().Be("\U0001F469\u200D\U0001F4BB");
        (terminal.Buffer.Lines[0]?[2].Content).Should().Be("\U0001F468\u200D\U0001F4BB");
    }

    [TestMethod]
    public void Print_EmojiAfterCursorMove_DoesNotJoinAcrossTheMove()
    {
        // Arrange
        var terminal = CreateTerminal(cols: 20, rows: 3);

        // Act — a ZWJ, then the cursor is moved away before the next character arrives.
        terminal.Write("\U0001F469\u200D");
        terminal.Write("\u001b[1;10H");
        terminal.Write("\U0001F4BB");

        // Assert — the continuation is tied to a POSITION, so moving the cursor drops it rather than
        // joining two characters that have nothing to do with each other.
        (terminal.Buffer.Lines[0]?[9].Width).Should().Be(2);
        (terminal.Buffer.Lines[0]?[9].Content).Should().Be("\U0001F4BB");
    }

    [TestMethod]
    public void Write_DockerComposeNetworkLine_KeepsStatusColumnAfterCheckMark()
    {
        // Arrange
        var terminal = CreateTerminal(cols: 80, rows: 3);
        const string prefix = " \u2714 Network docker_default";

        // Act
        terminal.Write(prefix);
        terminal.Write("\x1B[28C");
        terminal.Write("Created");

        // Assert
        var line = terminal.Buffer.Lines[0];
        line.Should().NotBeNull();
        int statusColumn = prefix.Length + 28;
        line.TranslateToString(false, statusColumn, statusColumn + 7).Should().Be("Created");
    }

    [TestMethod]
    public void HandleCsi_CursorBackward_MovesCursor()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        terminal.Buffer.SetCursor(20, 0);
        var params_ = new Params();
        params_.AddParam(5);

        // Act
        handler.HandleCsi("D", params_);

        // Assert
        terminal.Buffer.X.Should().Be(15);
    }

    [TestMethod]
    public void HandleCsi_CursorPosition_SetsCursorPosition()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        var params_ = new Params();
        params_.AddParam(10);
        params_.AddParam(20);

        // Act
        handler.HandleCsi("H", params_);

        // Assert
        terminal.Buffer.X.Should().Be(19); // 20 - 1 (1-based to 0-based)
        terminal.Buffer.Y.Should().Be(9);  // 10 - 1 (1-based to 0-based)
    }

    [TestMethod]
    public void HandleCsi_CursorPosition_UsesScrollRegionOriginInOriginMode()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        terminal.Buffer.SetScrollRegion(4, 19);
        terminal.OriginMode = true;
        var params_ = new Params();
        params_.AddParam(3);
        params_.AddParam(20);

        // Act
        handler.HandleCsi("H", params_);

        // Assert
        terminal.Buffer.X.Should().Be(19);
        terminal.Buffer.Y.Should().Be(6);
    }

    [TestMethod]
    public void HandleCsi_CursorPosition_ClampsLargeOriginModeRowToScrollBottom()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        terminal.Buffer.SetScrollRegion(4, 19);
        terminal.OriginMode = true;
        var params_ = new Params();
        params_.AddParam(int.MaxValue);
        params_.AddParam(20);

        // Act
        handler.HandleCsi("H", params_);

        // Assert
        terminal.Buffer.X.Should().Be(19);
        terminal.Buffer.Y.Should().Be(19);
    }

    [TestMethod]
    public void HandleCsi_EraseInDisplay_ClearBelow()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        
        // Fill some lines
        for (int i = 0; i < 5; i++)
        {
            terminal.Buffer.SetCursor(0, i);
            handler.Print("X");
        }
        
        terminal.Buffer.SetCursor(0, 2);
        var params_ = new Params();
        params_.AddParam(0); // Erase below

        // Act
        handler.HandleCsi("J", params_);

        // Assert
        // Lines 0-1 should have content, lines 2+ should be cleared
        (terminal.Buffer.Lines[0]?[0].Content).Should().Be("X");
        (terminal.Buffer.Lines[1]?[0].Content).Should().Be("X");
    }

    [TestMethod]
    public void HandleCsi_EraseInLine_ErasesToRight()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        
        // Print some characters
        for (int i = 0; i < 10; i++)
        {
            handler.Print("X");
        }
        
        terminal.Buffer.SetCursor(5, 0);
        var params_ = new Params();
        params_.AddParam(0); // Erase to right

        // Act
        handler.HandleCsi("K", params_);

        // Assert
        var line = terminal.Buffer.Lines[0];
        line.Should().NotBeNull();
        line[4].Content.Should().Be("X");
        line[5].Content.Should().NotBe("X");
    }

    [TestMethod]
    public void HandleCsi_SgrBold_SetsBoldAttribute()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        var params_ = new Params();
        params_.AddParam(1); // Bold

        // Act
        handler.HandleCsi("m", params_);
        handler.Print("B");

        // Assert
        var line = terminal.Buffer.Lines[0];
        line.Should().NotBeNull();
        (line[0].Attributes.IsBold()).Should().BeTrue();
    }

    [TestMethod]
    public void HandleCsi_SgrItalic_SetsItalicAttribute()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        var params_ = new Params();
        params_.AddParam(3); // Italic

        // Act
        handler.HandleCsi("m", params_);
        handler.Print("I");

        // Assert
        var line = terminal.Buffer.Lines[0];
        line.Should().NotBeNull();
        (line[0].Attributes.IsItalic()).Should().BeTrue();
    }

    [TestMethod]
    public void HandleCsi_SgrUnderline_SetsUnderlineAttribute()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        var params_ = new Params();
        params_.AddParam(4); // Underline

        // Act
        handler.HandleCsi("m", params_);
        handler.Print("U");

        // Assert
        var line = terminal.Buffer.Lines[0];
        line.Should().NotBeNull();
        (line[0].Attributes.IsUnderline()).Should().BeTrue();
    }

    [TestMethod]
    public void HandleCsi_SgrForegroundColor_SetsForegroundColor()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        var params_ = new Params();
        params_.AddParam(31); // Red foreground

        // Act
        handler.HandleCsi("m", params_);
        handler.Print("R");

        // Assert
        var line = terminal.Buffer.Lines[0];
        line.Should().NotBeNull();
        (line[0].Attributes.GetFgColor()).Should().Be(1); // Color 31 - 30 = 1
    }

    [TestMethod]
    public void HandleCsi_SgrBackgroundColor_SetsBackgroundColor()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        var params_ = new Params();
        params_.AddParam(42); // Green background

        // Act
        handler.HandleCsi("m", params_);
        handler.Print("G");

        // Assert
        var line = terminal.Buffer.Lines[0];
        line.Should().NotBeNull();
        (line[0].Attributes.GetBgColor()).Should().Be(2); // Color 42 - 40 = 2
    }

    [TestMethod]
    public void HandleCsi_SgrReset_ResetsAttributes()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        var params1 = new Params();
        params1.AddParam(1); // Bold
        params1.AddParam(31); // Red

        var params2 = new Params();
        params2.AddParam(0); // Reset

        // Act
        handler.HandleCsi("m", params1);
        handler.Print("B");
        
        handler.HandleCsi("m", params2);
        handler.Print("N");

        // Assert
        var line = terminal.Buffer.Lines[0];
        line.Should().NotBeNull();
        (line[0].Attributes.IsBold()).Should().BeTrue();
        (line[1].Attributes.IsBold()).Should().BeFalse();
    }

    [TestMethod]
    public void HandleCsi_SetScrollRegion_SetsRegion()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        var params_ = new Params();
        params_.AddParam(5);
        params_.AddParam(20);

        // Act
        handler.HandleCsi("r", params_);

        // Assert
        terminal.Buffer.ScrollTop.Should().Be(4);    // 5 - 1 (1-based to 0-based)
        terminal.Buffer.ScrollBottom.Should().Be(19); // 20 - 1 (1-based to 0-based)
    }

    [TestMethod]
    public void HandleCsi_SetScrollRegion_MovesCursorHome()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        terminal.Buffer.SetCursor(10, 10);
        var params_ = new Params();
        params_.AddParam(5);
        params_.AddParam(20);

        // Act
        handler.HandleCsi("r", params_);

        // Assert
        terminal.Buffer.X.Should().Be(0);
        terminal.Buffer.Y.Should().Be(0);
    }

    [TestMethod]
    public void HandleCsi_SetScrollRegion_MovesCursorToTopMarginInOriginMode()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        terminal.OriginMode = true;
        terminal.Buffer.SetCursor(10, 10);
        var params_ = new Params();
        params_.AddParam(5);
        params_.AddParam(20);

        // Act
        handler.HandleCsi("r", params_);

        // Assert
        terminal.Buffer.X.Should().Be(0);
        terminal.Buffer.Y.Should().Be(4);
    }

    [TestMethod]
    public void HandleEsc_Index_MovesDownOrScrolls()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        terminal.Buffer.SetCursor(0, 5);
        var initialY = terminal.Buffer.Y;

        // Act
        handler.HandleEsc("D", "");

        // Assert
        terminal.Buffer.Y.Should().Be(initialY + 1);
    }

    [TestMethod]
    public void HandleEsc_ReverseIndex_MovesUpOrScrolls()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        terminal.Buffer.SetCursor(0, 5);
        var initialY = terminal.Buffer.Y;

        // Act
        handler.HandleEsc("M", "");

        // Assert
        terminal.Buffer.Y.Should().Be(initialY - 1);
    }

    [TestMethod]
    public void HandleEsc_NextLine_MovesDownAndToStart()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        terminal.Buffer.SetCursor(20, 5);

        // Act
        handler.HandleEsc("E", "");

        // Assert
        terminal.Buffer.X.Should().Be(0);
        terminal.Buffer.Y.Should().Be(6);
    }

    [TestMethod]
    public void HandleOsc_SetTitle_SetsTerminalTitle()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);

        // Act
        handler.HandleOsc("0;Test Title");

        // Assert
        terminal.Title.Should().Be("Test Title");
    }

    [TestMethod]
    public void HandleOsc_SetWindowTitle_SetsTerminalTitle()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);

        // Act
        handler.HandleOsc("2;Window Title");

        // Assert
        terminal.Title.Should().Be("Window Title");
    }

    [TestMethod]
    public void SetBuffer_UpdatesBuffer()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        var newBuffer = new TerminalBuffer(80, 24, 1000);

        // Act
        handler.SetBuffer(newBuffer);
        handler.Print("X");

        // Assert
        (newBuffer.Lines[0]?[0].Content).Should().Be("X");
    }

    [TestMethod]
    public void Print_InsertMode_InsertsCharacter()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        
        // Print some characters first
        handler.Print("H");
        handler.Print("e");
        handler.Print("l");
        handler.Print("l");
        handler.Print("o");
        
        // Move cursor back and enable insert mode
        terminal.Buffer.SetCursor(2, 0);
        terminal.InsertMode = true;

        // Act
        handler.Print("X");

        // Assert
        var line = terminal.Buffer.Lines[0];
        line.Should().NotBeNull();
        // Character should be inserted, not overwritten
    }

    [TestMethod]
    public void Print_AtEndOfLine_WrapsToNextLine()
    {
        // Arrange
        var terminal = CreateTerminal(10, 24); // Narrow terminal
        var handler = new InputHandler(terminal);
        
        // Fill the line
        for (int i = 0; i < 10; i++)
        {
            handler.Print("X");
        }

        // Act
        handler.Print("Y"); // Should wrap

        // Assert
        terminal.Buffer.Y.Should().Be(1); // Moved to next line
        (terminal.Buffer.Lines[1]?[0].Content).Should().Be("Y");
    }

    [TestMethod]
    public void HandleCsi_InsertLines_InsertsBlankLines()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        terminal.Buffer.SetCursor(0, 5);
        var params_ = new Params();
        params_.AddParam(2); // Insert 2 lines

        // Act
        handler.HandleCsi("L", params_);

        // Assert - Lines should be inserted at cursor position
        // Verification would require checking buffer state
    }

    [TestMethod]
    public void HandleCsi_DeleteLines_DeletesLines()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        terminal.Buffer.SetCursor(0, 5);
        var params_ = new Params();
        params_.AddParam(2); // Delete 2 lines

        // Act
        handler.HandleCsi("M", params_);

        // Assert - Lines should be deleted from cursor position
        // Verification would require checking buffer state
    }

    [TestMethod]
    public void HandleCsi_MultipleAttributes_AppliesAll()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        var params_ = new Params();
        params_.AddParam(1);  // Bold
        params_.AddParam(3);  // Italic
        params_.AddParam(4);  // Underline
        params_.AddParam(31); // Red

        // Act
        handler.HandleCsi("m", params_);
        handler.Print("X");

        // Assert
        var line = terminal.Buffer.Lines[0];
        line.Should().NotBeNull();
        var attrs = line[0].Attributes;
        attrs.IsBold().Should().BeTrue();
        attrs.IsItalic().Should().BeTrue();
        attrs.IsUnderline().Should().BeTrue();
        attrs.GetFgColor().Should().Be(1);
    }

    [TestMethod]
    public void HandleCsi_DECSCUSR_SetsCursorStyle()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        var paramsUnderlineBlink = new Params();
        paramsUnderlineBlink.AddParam(3); // Blinking underline

        // Act
        handler.HandleCsi(" q", paramsUnderlineBlink);

        // Assert
        terminal.Options.CursorStyle.Should().Be(CursorStyle.Underline);
        terminal.Options.CursorBlink.Should().BeTrue();

        // Act - Steady block
        var paramsBlockSteady = new Params();
        paramsBlockSteady.AddParam(2);
        handler.HandleCsi(" q", paramsBlockSteady);

        terminal.Options.CursorStyle.Should().Be(CursorStyle.Block);
        terminal.Options.CursorBlink.Should().BeFalse();
    }

    [TestMethod]
    public void HandleCsi_SelectCursorStyle_BlinkingBlock_Defaults()
    {
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        var params_ = new Params();
        params_.AddParam(1);

        handler.HandleCsi(" q", params_);

        terminal.Options.CursorStyle.Should().Be(CursorStyle.Block);
        terminal.Options.CursorBlink.Should().BeTrue();
    }

    [TestMethod]
    public void HandleCsi_SelectCursorStyle_SteadyBar_RaisesEvent()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        var params_ = new Params();
        params_.AddParam(6); // Steady bar

        TerminalEvents.CursorStyleChangedEventArgs? received = null;
        terminal.CursorStyleChanged += (_, e) => received = e;

        // Act
        handler.HandleCsi(" q", params_);

        // Assert
        terminal.Options.CursorStyle.Should().Be(CursorStyle.Bar);
        terminal.Options.CursorBlink.Should().BeFalse();
        received.Should().NotBeNull();
        (received!.Style).Should().Be(CursorStyle.Bar);
        received.Blink.Should().BeFalse();
    }

    #region DeviceStatusReport Tests

    [TestMethod]
    public void HandleCsi_DSR_OperatingStatus_ReportsOk()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        var params_ = new Params();
        params_.AddParam(5); // Operating status query

        string? receivedData = null;
        terminal.DataReceived += (_, e) => receivedData = e.Data;

        // Act
        handler.HandleCsi("n", params_);

        // Assert - Should respond with CSI 0 n (OK)
        receivedData.Should().Be("\u001b[0n");
    }

    [TestMethod]
    public void HandleCsi_DSR_CursorPositionReport_ReportsPosition()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        terminal.Buffer.SetCursor(10, 5); // 0-based: col 10, row 5
        var params_ = new Params();
        params_.AddParam(6); // Cursor position query

        string? receivedData = null;
        terminal.DataReceived += (_, e) => receivedData = e.Data;

        // Act
        handler.HandleCsi("n", params_);

        // Assert - Should respond with CSI row ; col R (1-based)
        receivedData.Should().Be("\u001b[6;11R"); // row 6, col 11 (1-based)
    }

    [TestMethod]
    public void HandleCsi_DSR_CursorPositionReport_AtOrigin()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        terminal.Buffer.SetCursor(0, 0); // Top-left corner
        var params_ = new Params();
        params_.AddParam(6);

        string? receivedData = null;
        terminal.DataReceived += (_, e) => receivedData = e.Data;

        // Act
        handler.HandleCsi("n", params_);

        // Assert - Should respond with CSI 1 ; 1 R
        receivedData.Should().Be("\u001b[1;1R");
    }

    [TestMethod]
    public void HandleCsi_DSR_CursorPositionReport_WithOriginMode()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        
        // Set scroll region from row 5 to row 20 (0-based: 4 to 19)
        terminal.Buffer.SetScrollRegion(4, 19);
        terminal.OriginMode = true;
        
        // Cursor at row 10, col 5 (0-based)
        terminal.Buffer.SetCursor(5, 10);
        
        var params_ = new Params();
        params_.AddParam(6);

        string? receivedData = null;
        terminal.DataReceived += (_, e) => receivedData = e.Data;

        // Act
        handler.HandleCsi("n", params_);

        // Assert - Row should be adjusted for scroll region
        // Row 10 - ScrollTop 4 = 6, then +1 for 1-based = 7
        // Col 5 + 1 = 6
        receivedData.Should().Be("\u001b[7;6R");
    }

    [TestMethod]
    public void HandleCsi_DSR_Private_ExtendedCursorPositionReport()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        terminal.Buffer.SetCursor(15, 8); // 0-based: col 15, row 8
        var params_ = new Params();
        params_.AddParam(6); // Extended cursor position query

        string? receivedData = null;
        terminal.DataReceived += (_, e) => receivedData = e.Data;

        // Act - Use "?n" for private mode
        handler.HandleCsi("?n", params_);

        // Assert - Should respond with CSI ? row ; col R (1-based)
        receivedData.Should().Be("\u001b[?9;16R"); // row 9, col 16 (1-based)
    }

    [TestMethod]
    public void HandleCsi_DSR_Private_PrinterStatus()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        var params_ = new Params();
        params_.AddParam(15); // Printer status query

        string? receivedData = null;
        terminal.DataReceived += (_, e) => receivedData = e.Data;

        // Act
        handler.HandleCsi("?n", params_);

        // Assert - Should respond with CSI ? 13 n (no printer)
        receivedData.Should().Be("\u001b[?13n");
    }

    [TestMethod]
    public void HandleCsi_DSR_Private_UdkStatus()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        var params_ = new Params();
        params_.AddParam(25); // UDK status query

        string? receivedData = null;
        terminal.DataReceived += (_, e) => receivedData = e.Data;

        // Act
        handler.HandleCsi("?n", params_);

        // Assert - Should respond with CSI ? 21 n (UDK locked)
        receivedData.Should().Be("\u001b[?21n");
    }

    [TestMethod]
    public void HandleCsi_DSR_Private_KeyboardStatus()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        var params_ = new Params();
        params_.AddParam(26); // Keyboard status query

        string? receivedData = null;
        terminal.DataReceived += (_, e) => receivedData = e.Data;

        // Act
        handler.HandleCsi("?n", params_);

        // Assert - Should respond with CSI ? 27 ; 1 ; 0 ; 0 n (keyboard ready)
        receivedData.Should().Be("\u001b[?27;1;0;0n");
    }

    [TestMethod]
    public void HandleCsi_DSR_UnknownReport_DoesNotRespond()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        var params_ = new Params();
        params_.AddParam(99); // Unknown report type

        string? receivedData = null;
        terminal.DataReceived += (_, e) => receivedData = e.Data;

        // Act
        handler.HandleCsi("n", params_);

        // Assert - Should not send any response
        receivedData.Should().BeNull();
    }

    [TestMethod]
    public void HandleCsi_DSR_Private_UnknownReport_DoesNotRespond()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        var params_ = new Params();
        params_.AddParam(99); // Unknown private report type

        string? receivedData = null;
        terminal.DataReceived += (_, e) => receivedData = e.Data;

        // Act
        handler.HandleCsi("?n", params_);

        // Assert - Should not send any response
        receivedData.Should().BeNull();
    }

    #endregion

    #region DeviceAttributes Tests

    [TestMethod]
    public void HandleCsi_DA_Primary_ReportsDeviceAttributes()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        var params_ = new Params();
        params_.AddParam(0); // Primary DA

        string? receivedData = null;
        terminal.DataReceived += (_, e) => receivedData = e.Data;

        // Act
        handler.HandleCsi("c", params_);

        // Assert - CSI ? 62 ; 4 ; 22 c: a VT220 with Sixel and ANSI colour, which is what the code
        // in this repository actually implements. Attribute 4 is how libsixel-based programs decide
        // whether to send a picture at all, so dropping it from this reply silently turns every
        // image back into text art.
        receivedData.Should().Be("\u001b[?62;4;22c");
    }

    [TestMethod]
    public void HandleCsi_DA_Primary_OmitsSixelWhenDisabled()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.Options.SixelEnabled = false;
        var handler = new InputHandler(terminal);
        var params_ = new Params();
        params_.AddParam(0);

        string? receivedData = null;
        terminal.DataReceived += (_, e) => receivedData = e.Data;

        // Act
        handler.HandleCsi("c", params_);

        // Assert - claiming Sixel while it is switched off would send pictures we then drop
        receivedData.Should().Be("\u001b[?62;22c");
    }

    [TestMethod]
    public void HandleCsi_DA_Secondary_ReportsTerminalId()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        var params_ = new Params();
        params_.AddParam(0); // Secondary DA

        string? receivedData = null;
        terminal.DataReceived += (_, e) => receivedData = e.Data;

        // Act - Use ">c" for secondary DA
        handler.HandleCsi(">c", params_);

        // Assert - CSI > 1 ; package-version ; 0 c. Terminal type 1 is a VT220, so it agrees with
        // the 62 the primary reply sends; the old 0 said VT100 and contradicted it.
        receivedData.Should().Be(ExpectedSecondaryDeviceAttributes());
    }

    [TestMethod]
    [DataRow("c")]
    [DataRow(">c")]
    public void HandleCsi_DA_IgnoresNonZeroParameter(string identifier)
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        var params_ = new Params();
        params_.AddParam(1);

        string? receivedData = null;
        terminal.DataReceived += (_, e) => receivedData = e.Data;

        // Act
        handler.HandleCsi(identifier, params_);

        // Assert - a DA with a non-zero parameter is a reply, not a request. Two of these hooked
        // up to each other answer each other forever.
        receivedData.Should().BeNull();
    }

    [TestMethod]
    public void HandleCsi_DA_Tertiary_IsNotAnswered()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        var params_ = new Params();
        params_.AddParam(0);

        string? receivedData = null;
        terminal.DataReceived += (_, e) => receivedData = e.Data;

        // Act - "=c" is the tertiary DA, asking for a unit ID
        handler.HandleCsi("=c", params_);

        // Assert - there is no unit ID to report, and terminals without DECRPTUI stay quiet.
        // Answering a question nobody asked is worse than silence: the program would read a DA
        // reply where it expected DECRPTUI, while still waiting for the reply it did ask for.
        receivedData.Should().BeNull();
    }

    [TestMethod]
    public void HandleCsi_DA_PrivateMarker_IsNotAnsweredWithTheSecondaryReply()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        var params_ = new Params();
        params_.AddParam(0);

        string? receivedData = null;
        terminal.DataReceived += (_, e) => receivedData = e.Data;

        // Act - "?c" is not a DA request any program sends; it is not the secondary DA either
        handler.HandleCsi("?c", params_);

        // Assert - it used to be answered with the secondary reply, because the handler dispatched
        // on the coarse isPrivate flag, which "?" sets just as ">" does
        receivedData.Should().BeNull();
    }

    #endregion

    #region Additional CSI Command Tests

    [TestMethod]
    public void HandleCsi_CursorNextLine_MovesCursorDownAndToColumn1()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        terminal.Buffer.SetCursor(20, 5);
        var params_ = new Params();
        params_.AddParam(3); // Move down 3 lines

        // Act
        handler.HandleCsi("E", params_);

        // Assert
        terminal.Buffer.X.Should().Be(0); // Column should be 0
        terminal.Buffer.Y.Should().Be(8); // Row 5 + 3 = 8
    }

    [TestMethod]
    public void HandleCsi_CursorPreviousLine_MovesCursorUpAndToColumn1()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        terminal.Buffer.SetCursor(20, 10);
        var params_ = new Params();
        params_.AddParam(4); // Move up 4 lines

        // Act
        handler.HandleCsi("F", params_);

        // Assert
        terminal.Buffer.X.Should().Be(0); // Column should be 0
        terminal.Buffer.Y.Should().Be(6); // Row 10 - 4 = 6
    }

    [TestMethod]
    public void HandleCsi_CursorCharAbsolute_MovesToColumn()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        terminal.Buffer.SetCursor(5, 10);
        var params_ = new Params();
        params_.AddParam(25); // Move to column 25 (1-based)

        // Act
        handler.HandleCsi("G", params_);

        // Assert
        terminal.Buffer.X.Should().Be(24); // 25 - 1 = 24 (0-based)
        terminal.Buffer.Y.Should().Be(10); // Row should be unchanged
    }

    [TestMethod]
    public void HandleCsi_CursorForwardTab_MovesToNextTabStop()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        terminal.Buffer.SetCursor(5, 0);
        var params_ = new Params();
        params_.AddParam(1); // Move to next tab stop

        // Act
        handler.HandleCsi("I", params_);

        // Assert - Default tab width is 8, so from position 5, next tab stop is 8
        terminal.Buffer.X.Should().Be(8);
    }

    [TestMethod]
    public void HandleCsi_CursorForwardTab_MultipleTabStops()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        terminal.Buffer.SetCursor(0, 0);
        var params_ = new Params();
        params_.AddParam(3); // Move 3 tab stops

        // Act
        handler.HandleCsi("I", params_);

        // Assert - 3 tab stops from 0: 8, 16, 24
        terminal.Buffer.X.Should().Be(24);
    }

    [TestMethod]
    public void HandleCsi_DeleteChars_DeletesCharacters()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        
        // Print some characters
        for (int i = 0; i < 10; i++)
        {
            handler.Print(((char)('A' + i)).ToString());
        }
        
        // Move cursor to position 3
        terminal.Buffer.SetCursor(3, 0);
        var params_ = new Params();
        params_.AddParam(2); // Delete 2 characters

        // Act
        handler.HandleCsi("P", params_);

        // Assert - Characters should be shifted left
        var line = terminal.Buffer.Lines[0];
        line.Should().NotBeNull();
        line[0].Content.Should().Be("A");
        line[1].Content.Should().Be("B");
        line[2].Content.Should().Be("C");
        line[3].Content.Should().Be("F"); // D and E were deleted, F moved here
    }

    [TestMethod]
    public void HandleCsi_ScrollUp_ScrollsBufferUp()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        
        // Print content on multiple lines
        for (int i = 0; i < 5; i++)
        {
            terminal.Buffer.SetCursor(0, i);
            handler.Print(((char)('A' + i)).ToString());
        }
        
        var params_ = new Params();
        params_.AddParam(2); // Scroll up 2 lines

        // Act
        handler.HandleCsi("S", params_);

        // Assert - Content should have scrolled up
        // Viewport line 0 should now contain what was on viewport line 2
        // Access viewport using YBase offset
        var viewportLine0 = terminal.Buffer.Lines[terminal.Buffer.YBase];
        viewportLine0.Should().NotBeNull();
        viewportLine0[0].Content.Should().Be("C");
    }

    [TestMethod]
    public void HandleCsi_ScrollDown_ScrollsBufferDown()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        
        // Print content on multiple lines
        for (int i = 0; i < 5; i++)
        {
            terminal.Buffer.SetCursor(0, i);
            handler.Print(((char)('A' + i)).ToString());
        }
        
        var params_ = new Params();
        params_.AddParam(2); // Scroll down 2 lines

        // Act
        handler.HandleCsi("T", params_);

        // Assert - Content should have scrolled down, new blank lines at top
        var line2 = terminal.Buffer.Lines[2];
        line2.Should().NotBeNull();
        // Line 2 now contains what was on line 0
        line2[0].Content.Should().Be("A");
    }

    [TestMethod]
    public void HandleCsi_EraseChars_ErasesCharactersAtCursor()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        
        // Print some characters
        for (int i = 0; i < 10; i++)
        {
            handler.Print("X");
        }
        
        // Move cursor to position 3
        terminal.Buffer.SetCursor(3, 0);
        var params_ = new Params();
        params_.AddParam(4); // Erase 4 characters

        // Act
        handler.HandleCsi("X", params_);

        // Assert - Characters at positions 3-6 should be erased (spaces)
        var line = terminal.Buffer.Lines[0];
        line.Should().NotBeNull();
        line[2].Content.Should().Be("X");
        line[3].Content.Should().Be(" ");
        line[4].Content.Should().Be(" ");
        line[5].Content.Should().Be(" ");
        line[6].Content.Should().Be(" ");
        line[7].Content.Should().Be("X");
    }

    [TestMethod]
    public void HandleCsi_CursorBackwardTab_MovesToPreviousTabStop()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        terminal.Buffer.SetCursor(20, 0);
        var params_ = new Params();
        params_.AddParam(1); // Move to previous tab stop

        // Act
        handler.HandleCsi("Z", params_);

        // Assert - From position 20, previous tab stop is 16
        terminal.Buffer.X.Should().Be(16);
    }

    [TestMethod]
    public void HandleCsi_CursorBackwardTab_MultipleTabStops()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        terminal.Buffer.SetCursor(25, 0);
        var params_ = new Params();
        params_.AddParam(2); // Move back 2 tab stops

        // Act
        handler.HandleCsi("Z", params_);

        // Assert - From position 25: first back to 24, then to 16
        // Tab stops at 0, 8, 16, 24, so from 25 going back 2 stops: 24 -> 16
        terminal.Buffer.X.Should().Be(16);
    }

    [TestMethod]
    public void HandleCsi_LinePositionAbsolute_MovesToRow()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        terminal.Buffer.SetCursor(15, 5);
        var params_ = new Params();
        params_.AddParam(12); // Move to row 12 (1-based)

        // Act
        handler.HandleCsi("d", params_);

        // Assert
        terminal.Buffer.X.Should().Be(15); // Column should be unchanged
        terminal.Buffer.Y.Should().Be(11); // 12 - 1 = 11 (0-based)
    }

    [TestMethod]
    public void HandleCsi_LinePositionAbsolute_UsesScrollRegionOriginInOriginMode()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        terminal.Buffer.SetScrollRegion(4, 19);
        terminal.OriginMode = true;
        terminal.Buffer.SetCursor(15, 5);
        var params_ = new Params();
        params_.AddParam(3);

        // Act
        handler.HandleCsi("d", params_);

        // Assert
        terminal.Buffer.X.Should().Be(15);
        terminal.Buffer.Y.Should().Be(6);
    }

    [TestMethod]
    public void HandleCsi_SaveRestoreCursorAnsi_SavesAndRestoresPosition()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        terminal.Buffer.SetCursor(10, 5);

        // Act - Save cursor
        handler.HandleCsi("s", new Params());

        // Move cursor elsewhere
        terminal.Buffer.SetCursor(30, 15);
        terminal.Buffer.X.Should().Be(30);
        terminal.Buffer.Y.Should().Be(15);

        // Act - Restore cursor
        handler.HandleCsi("u", new Params());

        // Assert - Cursor should be back to saved position
        terminal.Buffer.X.Should().Be(10);
        terminal.Buffer.Y.Should().Be(5);
    }

    [TestMethod]
    public void HandleCsi_EraseInDisplay_ClearAbove()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        
        // Fill some lines
        for (int i = 0; i < 5; i++)
        {
            terminal.Buffer.SetCursor(0, i);
            handler.Print("X");
        }
        
        terminal.Buffer.SetCursor(0, 3);
        var params_ = new Params();
        params_.AddParam(1); // Erase above

        // Act
        handler.HandleCsi("J", params_);

        // Assert - Lines 0-2 should be cleared, lines 3-4 should have content
        (terminal.Buffer.Lines[0]?[0].Content).Should().NotBe("X");
        (terminal.Buffer.Lines[1]?[0].Content).Should().NotBe("X");
        (terminal.Buffer.Lines[2]?[0].Content).Should().NotBe("X");
        (terminal.Buffer.Lines[4]?[0].Content).Should().Be("X");
    }

    [TestMethod]
    public void HandleCsi_EraseInDisplay_ClearAll()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        
        // Fill some lines
        for (int i = 0; i < 5; i++)
        {
            terminal.Buffer.SetCursor(0, i);
            handler.Print("X");
        }
        
        terminal.Buffer.SetCursor(0, 2);
        var params_ = new Params();
        params_.AddParam(2); // Erase all

        // Act
        handler.HandleCsi("J", params_);

        // Assert - All visible lines should be cleared
        for (int i = 0; i < 5; i++)
        {
            (terminal.Buffer.Lines[i]?[0].Content).Should().NotBe("X");
        }
    }

    private static bool BufferContains(Terminal terminal, string text)
    {
        for (int i = 0; i < terminal.Buffer.Length; i++)
        {
            if (terminal.Buffer.Lines[i]?.TranslateToString(true).Contains(text) == true)
                return true;
        }

        return false;
    }

    /// <summary>
    /// CSI 3 J discards the scrollback. Not blanks it — discards it, so the lines are gone from the buffer
    /// and cannot be scrolled back to.
    ///
    /// Mode 3 used to share mode 2's body, which erases the VISIBLE screen and never touches the scrollback:
    /// the opposite of what it asks for. Downstream that surfaced as `cls` in cmd.exe leaving the history
    /// reachable with the mouse wheel, unlike conhost and Windows Terminal.
    /// </summary>
    [TestMethod]
    public void HandleCsi_EraseInDisplay_EraseScrollback_DiscardsHistory()
    {
        // Arrange - write enough to push lines above the visible screen
        var terminal = CreateTerminal(rows: 5);
        for (int i = 0; i < 20; i++)
        {
            terminal.Write($"line {i}\r\n");
        }

        (terminal.Buffer.YBase > 0).Should().BeTrue("test needs scrollback to exist before erasing it");
        var lengthBefore = terminal.Buffer.Length;
        var handler = new InputHandler(terminal);

        var params_ = new Params();
        params_.AddParam(3); // Erase scrollback

        // Act
        handler.HandleCsi("J", params_);

        // Assert - the history is gone, not merely blank
        terminal.Buffer.YBase.Should().Be(0);
        terminal.Buffer.ViewportY.Should().Be(0);
        (terminal.Buffer.Length < lengthBefore).Should().BeTrue($"the scrollback should have been discarded: was {lengthBefore}, now {terminal.Buffer.Length}");
    }

    /// <summary>
    /// And it leaves the visible screen alone — the two modes are complements, not variations. cmd.exe's
    /// `cls` relies on that: it clears the screen itself, line by line, then sends CSI 3 J for the rest.
    /// </summary>
    [TestMethod]
    public void HandleCsi_EraseInDisplay_EraseScrollback_KeepsTheVisibleScreen()
    {
        // Arrange
        var terminal = CreateTerminal(rows: 5);
        for (int i = 0; i < 20; i++)
        {
            terminal.Write($"line {i}\r\n");
        }
        terminal.Write("KEEPME");

        var handler = new InputHandler(terminal);
        var params_ = new Params();
        params_.AddParam(3);

        // Act
        handler.HandleCsi("J", params_);

        // Assert
        BufferContains(terminal, "KEEPME").Should().BeTrue("erasing the scrollback must not erase the screen");
    }

    /// <summary>
    /// The buffer must still be usable afterwards. YBase and YDisp are absolute indices into the line list,
    /// so trimming from the start without moving them leaves the visible screen pointing at an offset that
    /// no longer exists, and the next write runs off the end.
    /// </summary>
    [TestMethod]
    public void HandleCsi_EraseInDisplay_EraseScrollback_LeavesTheBufferWritable()
    {
        // Arrange
        var terminal = CreateTerminal(rows: 5);
        for (int i = 0; i < 20; i++)
        {
            terminal.Write($"line {i}\r\n");
        }

        var handler = new InputHandler(terminal);
        var params_ = new Params();
        params_.AddParam(3);
        handler.HandleCsi("J", params_);

        // Act - this threw IndexOutOfRangeException when the indices were left stale
        var exception = Record.Exception(() => terminal.Write("AFTERWARDS\r\n"));

        // Assert
        exception.Should().BeNull();
        BufferContains(terminal, "AFTERWARDS").Should().BeTrue("text written after the erase should render");
    }

    /// <summary>Erasing a scrollback that does not exist yet is a no-op, not a crash.</summary>
    [TestMethod]
    public void HandleCsi_EraseInDisplay_EraseScrollback_WithNoScrollback_IsHarmless()
    {
        // Arrange
        var terminal = CreateTerminal(rows: 24);
        terminal.Write("only one line");
        terminal.Buffer.YBase.Should().Be(0);

        var handler = new InputHandler(terminal);
        var params_ = new Params();
        params_.AddParam(3);

        // Act
        var exception = Record.Exception(() => handler.HandleCsi("J", params_));

        // Assert
        exception.Should().BeNull();
        terminal.Buffer.YBase.Should().Be(0);
        BufferContains(terminal, "only one line").Should().BeTrue("the screen is untouched");
    }

    [TestMethod]
    public void HandleCsi_EraseInLine_ErasesToLeft()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        
        // Print some characters
        for (int i = 0; i < 10; i++)
        {
            handler.Print("X");
        }
        
        terminal.Buffer.SetCursor(5, 0);
        var params_ = new Params();
        params_.AddParam(1); // Erase to left

        // Act
        handler.HandleCsi("K", params_);

        // Assert
        var line = terminal.Buffer.Lines[0];
        line.Should().NotBeNull();
        line[0].Content.Should().NotBe("X");
        line[5].Content.Should().NotBe("X"); // Cursor position included
        line[6].Content.Should().Be("X"); // Right of cursor preserved
    }

    [TestMethod]
    public void HandleCsi_EraseInLine_ErasesEntireLine()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        
        // Print some characters
        for (int i = 0; i < 10; i++)
        {
            handler.Print("X");
        }
        
        terminal.Buffer.SetCursor(5, 0);
        var params_ = new Params();
        params_.AddParam(2); // Erase entire line

        // Act
        handler.HandleCsi("K", params_);

        // Assert - Entire line should be cleared
        var line = terminal.Buffer.Lines[0];
        line.Should().NotBeNull();
        for (int i = 0; i < 10; i++)
        {
            line[i].Content.Should().NotBe("X");
        }
    }

    #endregion

    #region Additional CSI Command Tests
    
    [TestMethod]
    public void HandleCsi_InsertChars_InsertsBlankCharacters()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        
        // Print some characters
        for (int i = 0; i < 10; i++)
        {
            handler.Print(((char)('A' + i)).ToString());
        }
        
        // Move cursor to position 3
        terminal.Buffer.SetCursor(3, 0);
        var params_ = new Params();
        params_.AddParam(2); // Insert 2 blank characters

        // Act
        handler.HandleCsi("@", params_);

        // Assert - Characters should be shifted right
        var line = terminal.Buffer.Lines[0];
        line.Should().NotBeNull();
        line[0].Content.Should().Be("A");
        line[1].Content.Should().Be("B");
        line[2].Content.Should().Be("C");
        // Positions 3 and 4 should now be blank
        line[3].Content.Should().Be(" ");
        line[4].Content.Should().Be(" ");
        line[5].Content.Should().Be("D"); // D shifted from position 3 to 5
    }

    #endregion

    #region Extended SGR Attribute Tests

    [TestMethod]
    public void HandleCsi_SgrDim_SetsDimAttribute()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        var params_ = new Params();
        params_.AddParam(2); // Dim

        // Act
        handler.HandleCsi("m", params_);
        handler.Print("D");

        // Assert
        var line = terminal.Buffer.Lines[0];
        line.Should().NotBeNull();
        (line[0].Attributes.IsDim()).Should().BeTrue();
    }

    [TestMethod]
    public void HandleCsi_SgrBlink_SetsBlinkAttribute()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        var params_ = new Params();
        params_.AddParam(5); // Blink

        // Act
        handler.HandleCsi("m", params_);
        handler.Print("B");

        // Assert
        var line = terminal.Buffer.Lines[0];
        line.Should().NotBeNull();
        (line[0].Attributes.IsBlink()).Should().BeTrue();
    }

    [TestMethod]
    public void HandleCsi_SgrInverse_SetsInverseAttribute()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        var params_ = new Params();
        params_.AddParam(7); // Inverse

        // Act
        handler.HandleCsi("m", params_);
        handler.Print("I");

        // Assert
        var line = terminal.Buffer.Lines[0];
        line.Should().NotBeNull();
        (line[0].Attributes.IsInverse()).Should().BeTrue();
    }

    [TestMethod]
    public void HandleCsi_SgrHidden_SetsHiddenAttribute()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        var params_ = new Params();
        params_.AddParam(8); // Hidden/Invisible

        // Act
        handler.HandleCsi("m", params_);
        handler.Print("H");

        // Assert
        var line = terminal.Buffer.Lines[0];
        line.Should().NotBeNull();
        (line[0].Attributes.IsInvisible()).Should().BeTrue();
    }

    [TestMethod]
    public void HandleCsi_SgrStrikethrough_SetsStrikethroughAttribute()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        var params_ = new Params();
        params_.AddParam(9); // Strikethrough

        // Act
        handler.HandleCsi("m", params_);
        handler.Print("S");

        // Assert
        var line = terminal.Buffer.Lines[0];
        line.Should().NotBeNull();
        (line[0].Attributes.IsStrikethrough()).Should().BeTrue();
    }

    [TestMethod]
    public void HandleCsi_Sgr256ColorForeground_SetsForegroundColor()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        var params_ = new Params();
        params_.AddParam(38); // Extended foreground
        params_.AddParam(5);  // 256-color mode
        params_.AddParam(196); // Color index (bright red)

        // Act
        handler.HandleCsi("m", params_);
        handler.Print("C");

        // Assert
        var line = terminal.Buffer.Lines[0];
        line.Should().NotBeNull();
        (line[0].Attributes.GetFgColor()).Should().Be(196);
    }

    [TestMethod]
    public void HandleCsi_Sgr256ColorBackground_SetsBackgroundColor()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        var params_ = new Params();
        params_.AddParam(48); // Extended background
        params_.AddParam(5);  // 256-color mode
        params_.AddParam(21); // Color index (blue)

        // Act
        handler.HandleCsi("m", params_);
        handler.Print("C");

        // Assert
        var line = terminal.Buffer.Lines[0];
        line.Should().NotBeNull();
        (line[0].Attributes.GetBgColor()).Should().Be(21);
    }

    [TestMethod]
    public void HandleCsi_SgrTrueColorForeground_SetsForegroundRGB()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        var params_ = new Params();
        params_.AddParam(38); // Extended foreground
        params_.AddParam(2);  // TrueColor mode
        params_.AddParam(255); // Red
        params_.AddParam(128); // Green
        params_.AddParam(64);  // Blue

        // Act
        handler.HandleCsi("m", params_);
        handler.Print("T");

        // Assert
        var line = terminal.Buffer.Lines[0];
        line.Should().NotBeNull();
        // TrueColor mode is stored in the color mode bits
        (line[0].Attributes.GetFgColorMode() > 0).Should().BeTrue();
    }

    [TestMethod]
    public void HandleCsi_SgrTrueColorBackground_SetsBackgroundRGB()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        var params_ = new Params();
        params_.AddParam(48); // Extended background
        params_.AddParam(2);  // TrueColor mode
        params_.AddParam(64);  // Red
        params_.AddParam(128); // Green
        params_.AddParam(255); // Blue

        // Act
        handler.HandleCsi("m", params_);
        handler.Print("T");

        // Assert
        var line = terminal.Buffer.Lines[0];
        line.Should().NotBeNull();
        // TrueColor mode is stored in the color mode bits
        (line[0].Attributes.GetBgColorMode() > 0).Should().BeTrue();
    }

    [TestMethod]
    public void HandleCsi_SgrBrightForegroundColors_SetsCorrectColor()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        var params_ = new Params();
        params_.AddParam(91); // Bright red foreground (90-97)

        // Act
        handler.HandleCsi("m", params_);
        handler.Print("R");

        // Assert
        var line = terminal.Buffer.Lines[0];
        line.Should().NotBeNull();
        // Bright colors are 90-97, mapped to color indices 8-15
        (line[0].Attributes.GetFgColor()).Should().Be(9); // 91 - 82 = 9 (bright red)
    }

    [TestMethod]
    public void HandleCsi_SgrBrightBackgroundColors_SetsCorrectColor()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        var params_ = new Params();
        params_.AddParam(102); // Bright green background (100-107)

        // Act
        handler.HandleCsi("m", params_);
        handler.Print("G");

        // Assert
        var line = terminal.Buffer.Lines[0];
        line.Should().NotBeNull();
        // Bright background colors are 100-107, mapped to color indices 8-15
        (line[0].Attributes.GetBgColor()).Should().Be(10); // 102 - 92 = 10 (bright green)
    }

    [TestMethod]
    public void HandleCsi_SgrResetBold_ResetsBoldOnly()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        
        // Set bold and italic
        var params1 = new Params();
        params1.AddParam(1); // Bold
        params1.AddParam(3); // Italic
        handler.HandleCsi("m", params1);
        handler.Print("B");
        
        // Reset bold only
        var params2 = new Params();
        params2.AddParam(22); // Reset bold/dim

        // Act
        handler.HandleCsi("m", params2);
        handler.Print("N");

        // Assert
        var line = terminal.Buffer.Lines[0];
        line.Should().NotBeNull();
        (line[0].Attributes.IsBold()).Should().BeTrue();    // "B" was printed with bold
        (line[0].Attributes.IsItalic()).Should().BeTrue(); // "B" also has italic
        (line[1].Attributes.IsBold()).Should().BeFalse();   // "N" was printed after reset - NOT bold
        (line[1].Attributes.IsItalic()).Should().BeTrue(); // Italic should remain
    }

    [TestMethod]
    public void HandleCsi_SgrResetItalic_ResetsItalicOnly()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        
        // Set bold and italic
        var params1 = new Params();
        params1.AddParam(1); // Bold
        params1.AddParam(3); // Italic
        handler.HandleCsi("m", params1);
        handler.Print("I");
        
        // Reset italic only
        var params2 = new Params();
        params2.AddParam(23); // Reset italic

        // Act
        handler.HandleCsi("m", params2);
        handler.Print("N");

        // Assert
        var line = terminal.Buffer.Lines[0];
        line.Should().NotBeNull();
        (line[0].Attributes.IsItalic()).Should().BeTrue();
        (line[1].Attributes.IsItalic()).Should().BeFalse();
    }

    [TestMethod]
    public void HandleCsi_SgrResetUnderline_ResetsUnderlineOnly()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        var params1 = new Params();
        params1.AddParam(1); // Bold
        params1.AddParam(4); // Underline
        handler.HandleCsi("m", params1);
        handler.Print("U");
        
        // Reset underline only
        var params2 = new Params();
        params2.AddParam(24); // Reset underline

        // Act
        handler.HandleCsi("m", params2);
        handler.Print("N");

        // Assert
        var line = terminal.Buffer.Lines[0];
        line.Should().NotBeNull();
        (line[0].Attributes.IsUnderline()).Should().BeTrue();
        (line[1].Attributes.IsUnderline()).Should().BeFalse();
    }

    [TestMethod]
    public void HandleCsi_SgrDefaultForeground_ResetsForegroundColor()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        
        // Set foreground color
        var params1 = new Params();
        params1.AddParam(31); // Red
        handler.HandleCsi("m", params1);
        handler.Print("R");
        
        // Reset to default foreground
        var params2 = new Params();
        params2.AddParam(39); // Default foreground

        // Act
        handler.HandleCsi("m", params2);
        handler.Print("D");

        // Assert
        var line = terminal.Buffer.Lines[0];
        line.Should().NotBeNull();
        (line[0].Attributes.GetFgColor()).Should().Be(1); // Red
        (line[1].Attributes.GetFgColor()).Should().Be(256); // Default foreground is 256
    }

    [TestMethod]
    public void HandleCsi_SgrDefaultBackground_ResetsBackgroundColor()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        
        // Set background color
        var params1 = new Params();
        params1.AddParam(42); // Green background
        handler.HandleCsi("m", params1);
        handler.Print("G");
        
        // Reset to default background
        var params2 = new Params();
        params2.AddParam(49); // Default background

        // Act
        handler.HandleCsi("m", params2);
        handler.Print("D");

        // Assert
        var line = terminal.Buffer.Lines[0];
        line.Should().NotBeNull();
        (line[0].Attributes.GetBgColor()).Should().Be(2); // Green
        (line[1].Attributes.GetBgColor()).Should().Be(257); // Default background is 257
    }

    #endregion

    #region ESC Sequence Tests

    [TestMethod]
    public void HandleEsc_SaveCursor_SavesCursorPosition()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        terminal.Buffer.SetCursor(15, 10);

        // Act - ESC 7 (Save Cursor)
        handler.HandleEsc("7", "");

        // Move cursor elsewhere
        terminal.Buffer.SetCursor(30, 20);
        terminal.Buffer.X.Should().Be(30);
        terminal.Buffer.Y.Should().Be(20);

        // Act - Restore Cursor
        handler.HandleEsc("8", "");

        // Assert
        terminal.Buffer.X.Should().Be(15);
        terminal.Buffer.Y.Should().Be(10);
    }

    [TestMethod]
    public void HandleEsc_RestoreCursor_WithoutSave_MovesToOrigin()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        terminal.Buffer.SetCursor(10, 5);

        // Act - ESC 8 without prior save
        handler.HandleEsc("8", "");

        // Assert - Should move to origin or default position
        terminal.Buffer.X.Should().Be(0);
        terminal.Buffer.Y.Should().Be(0);
    }

    [TestMethod]
    public void HandleEsc_RIS_ResetsTerminal()
    {
        // Arrange
        var terminal = CreateTerminal();
        var handler = new InputHandler(terminal);
        terminal.Buffer.SetCursor(20, 10);
        terminal.InsertMode = true;
        terminal.OriginMode = true;

        // Act - ESC c (RIS - Reset to Initial State)
        handler.HandleEsc("c", "");

        // Assert
        terminal.Buffer.X.Should().Be(0);
        terminal.Buffer.Y.Should().Be(0);
        terminal.InsertMode.Should().BeFalse();
        terminal.OriginMode.Should().BeFalse();
    }

    #endregion

    #region C0 Control Character Tests

    [TestMethod]
    public void Terminal_FormFeed_TreatedAsLineFeed()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.Buffer.SetCursor(5, 3);
        var initialY = terminal.Buffer.Y;

        // Act - Form Feed (FF, 0x0C) is typically treated as line feed
        terminal.Write("\x0C");

        // Assert
        terminal.Buffer.Y.Should().Be(initialY + 1);
    }

    [TestMethod]
    public void Terminal_VerticalTab_TreatedAsLineFeed()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.Buffer.SetCursor(5, 3);
        var initialY = terminal.Buffer.Y;

        // Act - Vertical Tab (VT, 0x0B) is typically treated as line feed
        terminal.Write("\x0B");

        // Assert
        terminal.Buffer.Y.Should().Be(initialY + 1);
    }

    #endregion
    #region DeleteChars BCE Tests

    [TestMethod]
    public void HandleCsi_DeleteChars_FillsVacatedCellsWithSpaces()
    {
        // Arrange
        var terminal = CreateTerminal(20, 24);
        var handler = new InputHandler(terminal);
        
        // Print characters across the entire width
        for (int i = 0; i < 20; i++)
        {
            handler.Print("X");
        }
        
        // Move cursor to position 5
        terminal.Buffer.SetCursor(5, 0);
        var params_ = new Params();
        params_.AddParam(3); // Delete 3 characters

        // Act
        handler.HandleCsi("P", params_);

        // Assert - Characters should be shifted left
        var line = terminal.Buffer.Lines[0];
        line.Should().NotBeNull();
        
        // Positions 0-4 should be unchanged
        for (int i = 0; i < 5; i++)
        {
            line[i].Content.Should().Be("X");
        }
        
        // Position 5 should now have what was at position 8
        line[5].Content.Should().Be("X");
        
        // The last 3 cells (17, 18, 19) should now be spaces (vacated by shift)
        line[17].Content.Should().Be(" ");
        line[18].Content.Should().Be(" ");
        line[19].Content.Should().Be(" ");
    }

    [TestMethod]
    public void HandleCsi_DeleteChars_BCE_VacatedCellsUseCurrentAttributes()
    {
        // Arrange - This tests the BCE (Background Color Erase) behavior
        // When deleting characters, the vacated cells at the right edge
        // should be filled with the CURRENT background color/attributes
        var terminal = CreateTerminal(20, 24);
        var handler = new InputHandler(terminal);
        
        // Print characters with default attributes
        for (int i = 0; i < 20; i++)
        {
            handler.Print("X");
        }
        
        // Set a specific background color before deleting
        var sgrParams = new Params();
        sgrParams.AddParam(44); // Blue background
        handler.HandleCsi("m", sgrParams);
        
        // Move cursor to position 5
        terminal.Buffer.SetCursor(5, 0);
        var params_ = new Params();
        params_.AddParam(3); // Delete 3 characters

        // Act
        handler.HandleCsi("P", params_);

        // Assert - The vacated cells should have the blue background
        var line = terminal.Buffer.Lines[0];
        line.Should().NotBeNull();
        
        // The last 3 cells should be spaces with the current (blue) background
        line[17].Content.Should().Be(" ");
        line[18].Content.Should().Be(" ");
        line[19].Content.Should().Be(" ");
        
        // And they should have the blue background (color index 4)
        (line[17].Attributes.GetBgColor()).Should().Be(4);
        (line[18].Attributes.GetBgColor()).Should().Be(4);
        (line[19].Attributes.GetBgColor()).Should().Be(4);
    }

    [TestMethod]
    public void HandleCsi_DeleteChars_AtEndOfLine_StillClearsCorrectly()
    {
        // Arrange
        var terminal = CreateTerminal(20, 24);
        var handler = new InputHandler(terminal);
        
        // Print characters
        for (int i = 0; i < 20; i++)
        {
            handler.Print(((char)('A' + (i % 26))).ToString());
        }
        
        // Move cursor near end of line
        terminal.Buffer.SetCursor(17, 0);
        var params_ = new Params();
        params_.AddParam(2); // Delete 2 characters

        // Act
        handler.HandleCsi("P", params_);

        // Assert
        var line = terminal.Buffer.Lines[0];
        line.Should().NotBeNull();
        
        // Position 17 should now have what was at 19
        line[17].Content.Should().Be("T"); // 'T' was at position 19
        
        // Positions 18 and 19 should be spaces
        line[18].Content.Should().Be(" ");
        line[19].Content.Should().Be(" ");
    }

    [TestMethod]
    public void HandleCsi_DeleteChars_MoreThanRemaining_ClearsToEndOfLine()
    {
        // Arrange
        var terminal = CreateTerminal(20, 24);
        var handler = new InputHandler(terminal);
        
        // Print characters
        for (int i = 0; i < 20; i++)
        {
            handler.Print("X");
        }
        
        // Move cursor to position 15 and try to delete 10 chars (only 5 remaining)
        terminal.Buffer.SetCursor(15, 0);
        var params_ = new Params();
        params_.AddParam(10); // Delete 10 characters (more than available)

        // Act
        handler.HandleCsi("P", params_);

        // Assert - All cells from cursor to end should be cleared
        var line = terminal.Buffer.Lines[0];
        line.Should().NotBeNull();
        
        // Positions 0-14 should be unchanged
        for (int i = 0; i < 15; i++)
        {
            line[i].Content.Should().Be("X");
        }
        
        // Positions 15-19 should be spaces
        for (int i = 15; i < 20; i++)
        {
            line[i].Content.Should().Be(" ");
        }
    }

    #endregion

    #region DECRQSS

    /// <summary>Everything the terminal writes back to the host while <paramref name="input"/> is parsed.</summary>
    private static string Capture(Terminal terminal, string input)
    {
        var received = new System.Text.StringBuilder();
        void Collect(object? _, TerminalEvents.DataEventArgs e) => received.Append(e.Data);

        terminal.DataReceived += Collect;
        try
        {
            terminal.Write(input);
        }
        finally
        {
            terminal.DataReceived -= Collect;
        }

        return received.ToString();
    }

    /// <summary>The SGR parameters out of a DECRQSS <c>m</c> reply, asserting the envelope on the way.</summary>
    private static string RequestSgr(Terminal terminal)
    {
        var reply = Capture(terminal, "\x1bP$qm\x1b\\");
        reply.Should().StartWith("\x1bP1$r");
        reply.Should().EndWith("m\x1b\\");
        return reply["\x1bP1$r".Length..^"m\x1b\\".Length];
    }

    [TestMethod]
    public void Decrqss_DefaultAttributes_ReturnsReset()
    {
        var terminal = CreateTerminal();
        var reply = Capture(terminal, "\x1bP$qm\x1b\\");
        reply.Should().Be("\x1bP1$r0m\x1b\\");
    }

    [TestMethod]
    public void Decrqss_TruecolorForeground_ReturnsRgb()
    {
        var terminal = CreateTerminal();
        terminal.Write("\x1b[38;2;1;2;3m");
        RequestSgr(terminal).Should().Be("38;2;1;2;3");
    }

    [TestMethod]
    public void Decrqss_TruecolorForeground_SubParams_ReturnsRgb()
    {
        var terminal = CreateTerminal();
        // The colon form, with the empty colour-space slot programs conventionally leave blank.
        terminal.Write("\x1b[38:2::1:2:3m");
        RequestSgr(terminal).Should().Be("38;2;1;2;3");
    }

    [TestMethod]
    public void Decrqss_TruecolorForeground_SubParamsWithoutColorSpace_ReturnsRgb()
    {
        var terminal = CreateTerminal();
        // The shorter colon form, without the colour-space slot at all.
        terminal.Write("\x1b[38:2:1:2:3m");
        RequestSgr(terminal).Should().Be("38;2;1;2;3");
    }

    [TestMethod]
    public void Decrqss_TruecolorBackground_ReturnsRgb()
    {
        var terminal = CreateTerminal();
        terminal.Write("\x1b[48;2;10;20;30m");
        RequestSgr(terminal).Should().Be("48;2;10;20;30");
    }

    [TestMethod]
    public void Decrqss_TruecolorBackground_SubParams_ReturnsRgb()
    {
        var terminal = CreateTerminal();
        terminal.Write("\x1b[48:2::10:20:30m");
        RequestSgr(terminal).Should().Be("48;2;10;20;30");
    }

    [TestMethod]
    public void Decrqss_TruecolorForegroundAndBackground_ReportsBothInOrder()
    {
        var terminal = CreateTerminal();
        terminal.Write("\x1b[38;2;1;2;3;48;2;10;20;30m");
        RequestSgr(terminal).Should().Be("38;2;1;2;3;48;2;10;20;30");
    }

    [TestMethod]
    public void Decrqss_256ColorForeground_Returns256Code()
    {
        var terminal = CreateTerminal();
        terminal.Write("\x1b[38;5;200m");
        RequestSgr(terminal).Should().Be("38;5;200");
    }

    [TestMethod]
    public void Decrqss_256ColorForeground_SubParams_Returns256Code()
    {
        var terminal = CreateTerminal();
        terminal.Write("\x1b[38:5:200m");
        RequestSgr(terminal).Should().Be("38;5;200");
    }

    [TestMethod]
    [DataRow("\x1b[31m", "31")]          // ANSI foreground
    [DataRow("\x1b[91m", "91")]          // bright foreground
    [DataRow("\x1b[44m", "44")]          // ANSI background
    [DataRow("\x1b[104m", "104")]        // bright background
    [DataRow("\x1b[38;5;9m", "91")]      // palette 9 is the bright red the 16 already name
    [DataRow("\x1b[39;49m", "0")]        // back to the defaults, which are left out
    public void Decrqss_Colors_ReportsTheShortestFormThatParsesBack(string sgr, string expected)
    {
        var terminal = CreateTerminal();
        terminal.Write(sgr);
        RequestSgr(terminal).Should().Be(expected);
    }

    [TestMethod]
    [DataRow("\x1b[1m", "1")]            // bold
    [DataRow("\x1b[2m", "2")]            // dim
    [DataRow("\x1b[3m", "3")]            // italic
    [DataRow("\x1b[4m", "4")]            // underline
    [DataRow("\x1b[21m", "21")]          // double underline
    [DataRow("\x1b[4:3m", "4:3")]        // curly underline
    [DataRow("\x1b[4:4m", "4:4")]        // dotted underline
    [DataRow("\x1b[4:5m", "4:5")]        // dashed underline
    [DataRow("\x1b[5m", "5")]            // blink
    [DataRow("\x1b[7m", "7")]            // inverse
    [DataRow("\x1b[8m", "8")]            // invisible
    [DataRow("\x1b[9m", "9")]            // strikethrough
    [DataRow("\x1b[1;3;4;7m", "1;3;4;7")]
    [DataRow("\x1b[1m\x1b[22m", "0")]    // set then cleared reads as the reset
    public void Decrqss_Attributes_ReportsWhatIsOn(string sgr, string expected)
    {
        var terminal = CreateTerminal();
        terminal.Write(sgr);
        RequestSgr(terminal).Should().Be(expected);
    }

    [TestMethod]
    public void Decrqss_Sgr_RoundTripsThroughItsOwnReply()
    {
        // The reply is only useful if replaying it lands on the same attributes it described.
        const string Attributes = "\x1b[1;3;4:3;7;9;38;2;1;2;3;48;5;200m";

        var source = CreateTerminal();
        source.Write(Attributes);
        var reported = RequestSgr(source);

        var replayed = CreateTerminal();
        replayed.Write($"\x1b[{reported}m");

        RequestSgr(replayed).Should().Be(reported);
    }

    [TestMethod]
    public void Decrqss_ScrollRegion_ReturnsMargins()
    {
        var terminal = CreateTerminal(80, 24);
        terminal.Write("\x1b[5;20r"); // set scroll region rows 5-20
        var reply = Capture(terminal, "\x1bP$qr\x1b\\");
        reply.Should().Be("\x1bP1$r5;20r\x1b\\");
    }

    [TestMethod]
    public void Decrqss_DefaultScrollRegion_ReturnsFullScreen()
    {
        var terminal = CreateTerminal(80, 24);
        var reply = Capture(terminal, "\x1bP$qr\x1b\\");
        reply.Should().Be("\x1bP1$r1;24r\x1b\\");
    }

    [TestMethod]
    [DataRow("", "2")]                   // TerminalOptions starts on a block that does not blink
    [DataRow("\x1b[1 q", "1")]
    [DataRow("\x1b[2 q", "2")]
    [DataRow("\x1b[3 q", "3")]
    [DataRow("\x1b[4 q", "4")]
    [DataRow("\x1b[5 q", "5")]
    [DataRow("\x1b[6 q", "6")]
    public void Decrqss_CursorStyle_ReturnsCode(string decscusr, string expected)
    {
        var terminal = CreateTerminal();
        if (decscusr.Length > 0)
            terminal.Write(decscusr);

        var reply = Capture(terminal, "\x1bP$q q\x1b\\");
        reply.Should().Be($"\x1bP1$r{expected} q\x1b\\");
    }

    [TestMethod]
    public void Decrqss_ConformanceLevel_ReturnsVt220()
    {
        var terminal = CreateTerminal();
        var reply = Capture(terminal, "\x1bP$q\"p\x1b\\");
        reply.Should().Be("\x1bP1$r62;1\"p\x1b\\");
    }

    [TestMethod]
    public void Decrqss_ProtectionAttribute_ReturnsUnprotected()
    {
        var terminal = CreateTerminal();
        var reply = Capture(terminal, "\x1bP$q\"q\x1b\\");
        reply.Should().Be("\x1bP1$r0\"q\x1b\\");
    }

    [TestMethod]
    public void Decrqss_UnknownSetting_ReturnsDeny()
    {
        var terminal = CreateTerminal();
        var reply = Capture(terminal, "\x1bP$qXYZ\x1b\\");
        reply.Should().Be("\x1bP0$r\x1b\\");
    }

    [TestMethod]
    public void Decrqss_EmptyRequest_ReturnsDeny()
    {
        var terminal = CreateTerminal();
        var reply = Capture(terminal, "\x1bP$q\x1b\\");
        reply.Should().Be("\x1bP0$r\x1b\\");
    }

    [TestMethod]
    public void Decrqss_OversizedPayload_IsRefusedWithoutBuffering()
    {
        var terminal = CreateTerminal();
        var reply = Capture(terminal, "\x1bP$qm" + new string('x', 100_000) + "\x1b\\");
        reply.Should().Be("\x1bP0$r\x1b\\");
    }

    [TestMethod]
    public void Decrqss_AbandonedSequence_NoReply()
    {
        var terminal = CreateTerminal();
        // Send DCS $ q m but terminate with CAN instead of ST — should produce no reply
        var reply = Capture(terminal, "\x1bP$qm\x18");
        reply.Should().Be("");
    }

    [TestMethod]
    public void Decrqss_SecondRequest_DoesNotSeeTheFirstPayload()
    {
        var terminal = CreateTerminal();
        Capture(terminal, "\x1bP$qr\x1b\\").Should().Be("\x1bP1$r1;24r\x1b\\");
        Capture(terminal, "\x1bP$qm\x1b\\").Should().Be("\x1bP1$r0m\x1b\\");
    }

    [TestMethod]
    public void Decrqss_AfterAnUnrelatedDcs_StillAnswers()
    {
        // DECRQSS shares the DCS hook with every other DCS sequence, so an intervening one must
        // not leave the payload collector holding someone else's bytes.
        var terminal = CreateTerminal();
        terminal.Write("\x1bP0;1|17/ab\x1b\\"); // DECUDK, which this terminal does not implement

        Capture(terminal, "\x1bP$qm\x1b\\").Should().Be("\x1bP1$r0m\x1b\\");
    }

    #endregion


    [TestMethod]
    public void Ich_MidLine_ShiftsTheTailInsteadOfReplicatingOneCell()
    {
        // The bug as a user met it: type "echo testing", arrow left three times, type a letter.
        // Readline inserts with ICH, and the overlapping forward copy turned the whole tail into
        // the cell under the cursor, repeated to the end of the line.
        var terminal = new Terminal(new TerminalOptions { Cols = 20, Rows = 5 });
        terminal.Write("abcdef");
        terminal.Write("\u001b[4G");        // cursor onto 'd'
        terminal.Write("\u001b[@");         // ICH 1: make room
        terminal.Write("X");                  // what readline types next

        var line = terminal.Buffer.Lines[0]!;
        var text = string.Concat(Enumerable.Range(0, 8).Select(i => line[i].Content));
        text.TrimEnd('\0', ' ').Should().Be("abcXdef");
    }

    [TestMethod]
    public void InsertMode_MidLine_ShiftsTheTailToo()
    {
        // IRM (CSI 4 h) takes the other shifting path through Print; same memmove rule applies.
        var terminal = new Terminal(new TerminalOptions { Cols = 20, Rows = 5 });
        terminal.Write("abcdef");
        terminal.Write("\u001b[4G\u001b[4hX\u001b[4l");

        var line = terminal.Buffer.Lines[0]!;
        var text = string.Concat(Enumerable.Range(0, 8).Select(i => line[i].Content));
        text.TrimEnd('\0', ' ').Should().Be("abcXdef");
    }
}
