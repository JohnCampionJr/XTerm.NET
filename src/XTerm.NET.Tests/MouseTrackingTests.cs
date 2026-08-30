using XTerm;
using XTerm.Input;
using XTerm.Options;

namespace XTerm.Tests;

[TestClass]

public class MouseTrackingTests
{
    private Terminal CreateTerminal(int cols = 80, int rows = 24)
    {
        var options = new TerminalOptions { Cols = cols, Rows = rows };
        return new Terminal(options);
    }

    #region Mouse Mode Activation

    [TestMethod]
    public void MouseMode_X10_EnablesClickTracking()
    {
        // Arrange
        var terminal = CreateTerminal();

        // Act
        terminal.Write("\x1B[?9h"); // Enable X10 mouse

        // Assert
        terminal.MouseTrackingMode.Should().Be(MouseTrackingMode.X10);
    }

    [TestMethod]
    public void MouseMode_VT200_EnablesNormalTracking()
    {
        // Arrange
        var terminal = CreateTerminal();

        // Act
        terminal.Write("\x1B[?1000h"); // Enable VT200 mouse

        // Assert
        terminal.MouseTrackingMode.Should().Be(MouseTrackingMode.VT200);
    }

    [TestMethod]
    public void MouseMode_ButtonEvent_EnablesButtonTracking()
    {
        // Arrange
        var terminal = CreateTerminal();

        // Act
        terminal.Write("\x1B[?1002h"); // Enable button event tracking

        // Assert
        terminal.MouseTrackingMode.Should().Be(MouseTrackingMode.ButtonEvent);
    }

    [TestMethod]
    public void MouseMode_AnyEvent_EnablesAllTracking()
    {
        // Arrange
        var terminal = CreateTerminal();

        // Act
        terminal.Write("\x1B[?1003h"); // Enable any event tracking

        // Assert
        terminal.MouseTrackingMode.Should().Be(MouseTrackingMode.AnyEvent);
    }

    [TestMethod]
    public void MouseMode_SGR_EnablesSGREncoding()
    {
        // Arrange
        var terminal = CreateTerminal();

        // Act
        terminal.Write("\x1B[?1006h"); // Enable SGR encoding

        // Assert
        terminal.MouseEncoding.Should().Be(MouseEncoding.SGR);
    }

    [TestMethod]
    public void MouseMode_Disable_ResetsToNone()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.Write("\x1B[?1000h"); // Enable

        // Act
        terminal.Write("\x1B[?1000l"); // Disable

        // Assert
        terminal.MouseTrackingMode.Should().Be(MouseTrackingMode.None);
    }

    #endregion

    #region Mouse Event Generation - Default Format

    [TestMethod]
    public void MouseEvent_LeftClick_GeneratesCorrectSequence()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.Write("\x1B[?1000h"); // Enable VT200 mouse

        // Act
        var sequence = terminal.GenerateMouseEvent(MouseButton.Left, 5, 10, MouseEventType.Down);

        // Assert - Default format: ESC [ M Cb Cx Cy
        // ESC (1) + [ (1) + M (1) + Cb (1) + Cx (1) + Cy (1) = 6 chars
        // Cb = 32 + button (0) = 32
        // Cx = 32 + x + 1 = 32 + 5 + 1 = 38 ('&')
        // Cy = 32 + y + 1 = 32 + 10 + 1 = 43 ('+')
        sequence.Should().StartWith("\x1B[M");
        sequence.Length.Should().Be(6);
    }

    [TestMethod]
    public void MouseEvent_NoMode_ReturnsEmpty()
    {
        // Arrange
        var terminal = CreateTerminal();
        // No mouse mode enabled

        // Act
        var sequence = terminal.GenerateMouseEvent(MouseButton.Left, 5, 10, MouseEventType.Down);

        // Assert
        sequence.Should().Be(string.Empty);
    }

    [TestMethod]
    public void MouseEvent_X10Mode_OnlyReportsDown()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.Write("\x1B[?9h"); // X10 mode

        // Act
        var downSeq = terminal.GenerateMouseEvent(MouseButton.Left, 5, 10, MouseEventType.Down);
        var upSeq = terminal.GenerateMouseEvent(MouseButton.Left, 5, 10, MouseEventType.Up);
        var moveSeq = terminal.GenerateMouseEvent(MouseButton.Left, 6, 10, MouseEventType.Move);

        // Assert
        downSeq.Should().NotBeEmpty();
        upSeq.Should().BeEmpty();
        moveSeq.Should().BeEmpty();
    }

    [TestMethod]
    public void MouseEvent_VT200Mode_ReportsDownAndUp()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.Write("\x1B[?1000h"); // VT200 mode

        // Act
        var downSeq = terminal.GenerateMouseEvent(MouseButton.Left, 5, 10, MouseEventType.Down);
        var upSeq = terminal.GenerateMouseEvent(MouseButton.Left, 5, 10, MouseEventType.Up);
        var moveSeq = terminal.GenerateMouseEvent(MouseButton.Left, 6, 10, MouseEventType.Move);

        // Assert
        downSeq.Should().NotBeEmpty();
        upSeq.Should().NotBeEmpty();
        moveSeq.Should().BeEmpty(); // Motion not reported in VT200
    }

    [TestMethod]
    public void MouseEvent_ButtonEventMode_ReportsDrag()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.Write("\x1B[?1002h"); // Button event mode

        // Act
        var dragSeq = terminal.GenerateMouseEvent(MouseButton.Left, 6, 10, MouseEventType.Drag);

        // Assert
        dragSeq.Should().NotBeEmpty();
    }

    [TestMethod]
    public void MouseEvent_AnyEventMode_ReportsMotion()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.Write("\x1B[?1003h"); // Any event mode

        // Act
        var moveSeq = terminal.GenerateMouseEvent(MouseButton.None, 6, 10, MouseEventType.Move);

        // Assert
        moveSeq.Should().NotBeEmpty();
    }

    #endregion

    #region Mouse Buttons

    [TestMethod]
    public void MouseEvent_MiddleButton_GeneratesCorrectCode()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.Write("\x1B[?1000h");

        // Act
        var sequence = terminal.GenerateMouseEvent(MouseButton.Middle, 5, 10, MouseEventType.Down);

        // Assert
        sequence.Should().NotBeEmpty();
        sequence.Should().StartWith("\x1B[M");
    }

    [TestMethod]
    public void MouseEvent_RightButton_GeneratesCorrectCode()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.Write("\x1B[?1000h");

        // Act
        var sequence = terminal.GenerateMouseEvent(MouseButton.Right, 5, 10, MouseEventType.Down);

        // Assert
        sequence.Should().NotBeEmpty();
        sequence.Should().StartWith("\x1B[M");
    }

    [TestMethod]
    public void MouseEvent_WheelUp_GeneratesCorrectSequence()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.Write("\x1B[?1000h");

        // Act
        var sequence = terminal.GenerateMouseEvent(MouseButton.WheelUp, 5, 10, MouseEventType.WheelUp);

        // Assert
        sequence.Should().NotBeEmpty();
    }

    [TestMethod]
    public void MouseEvent_WheelDown_GeneratesCorrectSequence()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.Write("\x1B[?1000h");

        // Act
        var sequence = terminal.GenerateMouseEvent(MouseButton.WheelDown, 5, 10, MouseEventType.WheelDown);

        // Assert
        sequence.Should().NotBeEmpty();
    }

    #endregion

    #region SGR Format

    [TestMethod]
    public void MouseEvent_SGRFormat_GeneratesCorrectSequence()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.Write("\x1B[?1000h"); // Enable tracking
        terminal.Write("\x1B[?1006h"); // Enable SGR format

        // Act
        var sequence = terminal.GenerateMouseEvent(MouseButton.Left, 5, 10, MouseEventType.Down);

        // Assert - SGR format: ESC [ < Cb ; Cx ; Cy M
        sequence.Should().StartWith("\x1B[<");
        sequence.Should().Contain(";");
        sequence.Should().EndWith("M");
    }

    [TestMethod]
    public void MouseEvent_SGRFormat_Release_UsesLowercaseM()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.Write("\x1B[?1000h");
        terminal.Write("\x1B[?1006h"); // SGR format

        // Act
        var downSeq = terminal.GenerateMouseEvent(MouseButton.Left, 5, 10, MouseEventType.Down);
        var upSeq = terminal.GenerateMouseEvent(MouseButton.Left, 5, 10, MouseEventType.Up);

        // Assert
        downSeq.Should().EndWith("M"); // Uppercase for press
        upSeq.Should().EndWith("m");   // Lowercase for release
    }

    #endregion

    #region Modifiers

    [TestMethod]
    public void MouseEvent_WithShift_IncludesModifier()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.Write("\x1B[?1000h");

        // Act
        var normalSeq = terminal.GenerateMouseEvent(MouseButton.Left, 5, 10, MouseEventType.Down);
        var shiftSeq = terminal.GenerateMouseEvent(MouseButton.Left, 5, 10, MouseEventType.Down, KeyModifiers.Shift);

        // Assert
        shiftSeq.Should().NotBe(normalSeq);
    }

    [TestMethod]
    public void MouseEvent_WithControl_IncludesModifier()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.Write("\x1B[?1000h");

        // Act
        var normalSeq = terminal.GenerateMouseEvent(MouseButton.Left, 5, 10, MouseEventType.Down);
        var ctrlSeq = terminal.GenerateMouseEvent(MouseButton.Left, 5, 10, MouseEventType.Down, KeyModifiers.Control);

        // Assert
        ctrlSeq.Should().NotBe(normalSeq);
    }

    [TestMethod]
    public void MouseEvent_WithAlt_IncludesModifier()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.Write("\x1B[?1000h");

        // Act
        var normalSeq = terminal.GenerateMouseEvent(MouseButton.Left, 5, 10, MouseEventType.Down);
        var altSeq = terminal.GenerateMouseEvent(MouseButton.Left, 5, 10, MouseEventType.Down, KeyModifiers.Alt);

        // Assert
        altSeq.Should().NotBe(normalSeq);
    }

    #endregion

    #region Focus Events

    [TestMethod]
    public void FocusEvent_Enabled_GeneratesSequence()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.Write("\x1B[?1004h"); // Enable focus events

        // Act
        var focusIn = terminal.GenerateFocusEvent(true);
        var focusOut = terminal.GenerateFocusEvent(false);

        // Assert
        focusIn.Should().Be("\x1B[I");
        focusOut.Should().Be("\x1B[O");
    }

    [TestMethod]
    public void FocusEvent_Disabled_ReturnsEmpty()
    {
        // Arrange
        var terminal = CreateTerminal();
        // Focus events not enabled

        // Act
        var focusIn = terminal.GenerateFocusEvent(true);

        // Assert
        focusIn.Should().Be(string.Empty);
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void MouseEvent_AtBoundaries_HandlesCorrectly()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.Write("\x1B[?1000h");

        // Act - Test at (0,0) and (max,max)
        var topLeft = terminal.GenerateMouseEvent(MouseButton.Left, 0, 0, MouseEventType.Down);
        var bottomRight = terminal.GenerateMouseEvent(MouseButton.Left, 79, 23, MouseEventType.Down);

        // Assert
        topLeft.Should().NotBeEmpty();
        bottomRight.Should().NotBeEmpty();
    }

    [TestMethod]
    public void MouseEvent_LargeCoordinates_AreDroppedRatherThanMisreported()
    {
        // This asserted only "generates something", which the clamp satisfied by reporting the
        // last addressable column for every click beyond it. The single-byte encoding cannot
        // carry these coordinates through a UTF-8 transport at all, so the report is dropped:
        // a click that does nothing beats a click attributed to the wrong column.
        var terminal = CreateTerminal();
        terminal.Write("\x1B[?1000h");

        var sequence = terminal.GenerateMouseEvent(MouseButton.Left, 300, 300, MouseEventType.Down);

        sequence.Should().BeEmpty();
    }

    [TestMethod]
    public void MouseMode_ModeSwitch_UpdatesCorrectly()
    {
        // Arrange
        var terminal = CreateTerminal();

        // Act - Switch between modes
        terminal.Write("\x1B[?9h"); // X10
        var mode1 = terminal.MouseTrackingMode;

        terminal.Write("\x1B[?1000h"); // VT200
        var mode2 = terminal.MouseTrackingMode;

        terminal.Write("\x1B[?1003h"); // Any event
        var mode3 = terminal.MouseTrackingMode;

        // Assert
        mode1.Should().Be(MouseTrackingMode.X10);
        mode2.Should().Be(MouseTrackingMode.VT200);
        mode3.Should().Be(MouseTrackingMode.AnyEvent);
    }

    [TestMethod]
    public void MouseEncoding_Switch_UpdatesCorrectly()
    {
        // Arrange
        var terminal = CreateTerminal();

        // Act - Switch encodings
        terminal.Write("\x1B[?1006h"); // SGR
        var enc1 = terminal.MouseEncoding;

        terminal.Write("\x1B[?1005h"); // UTF-8
        var enc2 = terminal.MouseEncoding;

        terminal.Write("\x1B[?1006l"); // Disable
        var enc3 = terminal.MouseEncoding;

        // Assert
        enc1.Should().Be(MouseEncoding.SGR);
        enc2.Should().Be(MouseEncoding.Utf8);
        enc3.Should().Be(MouseEncoding.Default);
    }

    #endregion
}
