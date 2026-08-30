using XTerm;
using XTerm.Common;
using XTerm.Options;

namespace XTerm.Tests;

[TestClass]

public class ModeHandlingTests
{
    private Terminal CreateTerminal(int cols = 80, int rows = 24)
    {
        var options = new TerminalOptions { Cols = cols, Rows = rows };
        return new Terminal(options);
    }

    [TestMethod]
    public void SetMode_InsertMode_EnablesInsertMode()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.InsertMode.Should().BeFalse();

        // Act
        terminal.Write($"\x1B[{(int)TerminalMode.InsertMode}h");

        // Assert
        terminal.InsertMode.Should().BeTrue();
    }

    [TestMethod]
    public void ResetMode_InsertMode_DisablesInsertMode()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.InsertMode = true;

        // Act
        terminal.Write($"\x1B[{(int)TerminalMode.InsertMode}l");

        // Assert
        terminal.InsertMode.Should().BeFalse();
    }

    [TestMethod]
    public void SetMode_ApplicationCursorKeys_EnablesAppCursorKeys()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.ApplicationCursorKeys.Should().BeFalse();

        // Act - DEC private mode
        terminal.Write($"\x1B[?{(int)TerminalMode.AppCursorKeys}h");

        // Assert
        terminal.ApplicationCursorKeys.Should().BeTrue();
    }

    [TestMethod]
    public void ResetMode_ApplicationCursorKeys_DisablesAppCursorKeys()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.ApplicationCursorKeys = true;

        // Act - DEC private mode
        terminal.Write($"\x1B[?{(int)TerminalMode.AppCursorKeys}l");

        // Assert
        terminal.ApplicationCursorKeys.Should().BeFalse();
    }

    [TestMethod]
    public void SetMode_ShowCursor_EnablesCursorVisibility()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.CursorVisible = false;

        // Act - DEC private mode
        terminal.Write($"\x1B[?{(int)TerminalMode.ShowCursor}h");

        // Assert
        terminal.CursorVisible.Should().BeTrue();
    }

    [TestMethod]
    public void ResetMode_ShowCursor_DisablesCursorVisibility()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.CursorVisible.Should().BeTrue(); // Default is true

        // Act - DEC private mode
        terminal.Write($"\x1B[?{(int)TerminalMode.ShowCursor}l");

        // Assert
        terminal.CursorVisible.Should().BeFalse();
    }

    [TestMethod]
    public void SetMode_ApplicationKeypad_EnablesAppKeypad()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.ApplicationKeypad.Should().BeFalse();

        // Act - DEC private mode
        terminal.Write($"\x1B[?{(int)TerminalMode.AppKeypad}h");

        // Assert
        terminal.ApplicationKeypad.Should().BeTrue();
    }

    [TestMethod]
    public void ResetMode_ApplicationKeypad_DisablesAppKeypad()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.ApplicationKeypad = true;

        // Act - DEC private mode
        terminal.Write($"\x1B[?{(int)TerminalMode.AppKeypad}l");

        // Assert
        terminal.ApplicationKeypad.Should().BeFalse();
    }

    [TestMethod]
    public void SetMode_BracketedPasteMode_EnablesBracketedPaste()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.BracketedPasteMode.Should().BeFalse();

        // Act - DEC private mode
        terminal.Write($"\x1B[?{(int)TerminalMode.BracketedPasteMode}h");

        // Assert
        terminal.BracketedPasteMode.Should().BeTrue();
    }

    [TestMethod]
    public void ResetMode_BracketedPasteMode_DisablesBracketedPaste()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.BracketedPasteMode = true;

        // Act - DEC private mode
        terminal.Write($"\x1B[?{(int)TerminalMode.BracketedPasteMode}l");

        // Assert
        terminal.BracketedPasteMode.Should().BeFalse();
    }

    [TestMethod]
    public void SetMode_OriginMode_EnablesOriginMode()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.Buffer.SetCursor(10, 10);
        terminal.OriginMode.Should().BeFalse();

        // Act - DEC private mode
        terminal.Write($"\x1B[?{(int)TerminalMode.Origin}h");

        // Assert
        terminal.OriginMode.Should().BeTrue();
        // Cursor should be reset to 0,0
        terminal.Buffer.X.Should().Be(0);
        terminal.Buffer.Y.Should().Be(0);
    }

    [TestMethod]
    public void SetMode_OriginMode_MovesCursorToTopMargin()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.Buffer.SetScrollRegion(4, 19);
        terminal.Buffer.SetCursor(10, 10);
        terminal.OriginMode.Should().BeFalse();

        // Act - DEC private mode
        terminal.Write($"\x1B[?{(int)TerminalMode.Origin}h");

        // Assert
        terminal.OriginMode.Should().BeTrue();
        terminal.Buffer.X.Should().Be(0);
        terminal.Buffer.Y.Should().Be(4);
    }

    [TestMethod]
    public void ResetMode_OriginMode_DisablesOriginMode()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.OriginMode = true;
        terminal.Buffer.SetCursor(5, 5);

        // Act - DEC private mode
        terminal.Write($"\x1B[?{(int)TerminalMode.Origin}l");

        // Assert
        terminal.OriginMode.Should().BeFalse();
        // Cursor should be reset to 0,0
        terminal.Buffer.X.Should().Be(0);
        terminal.Buffer.Y.Should().Be(0);
    }

    [TestMethod]
    public void SetMode_AltBuffer_SwitchesToAltBuffer()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.Write("Normal buffer content");
        var normalBufferContent = terminal.GetLine(0);

        // Act - DEC private mode
        terminal.Write($"\x1B[?{(int)TerminalMode.AltBuffer}h");

        // Assert
        terminal.Write("Alt buffer content");
        var altBufferContent = terminal.GetLine(0);
        altBufferContent.Should().Contain("Alt buffer");
        altBufferContent.Should().NotContain("Normal buffer");
    }

    [TestMethod]
    public void ResetMode_AltBuffer_SwitchesToNormalBuffer()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.Write("Normal content");
        terminal.Write($"\x1B[?{(int)TerminalMode.AltBuffer}h"); // Switch to alt
        terminal.Write("Alt content");

        // Act - DEC private mode
        terminal.Write($"\x1B[?{(int)TerminalMode.AltBuffer}l"); // Switch back

        // Assert
        var content = terminal.GetLine(0);
        content.Should().Contain("Normal content");
    }

    [TestMethod]
    public void SetMode_AltBufferWithCursor_SavesCursor()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.Buffer.SetCursor(20, 10);

        // Act - DEC private mode (saves cursor and switches)
        terminal.Write($"\x1B[?{(int)TerminalMode.AltBufferCursor}h");

        // Assert
        // Should be in alt buffer
        terminal.Write("Test");
        var content = terminal.GetLine(0);
        content.Should().Contain("Test");
    }

    [TestMethod]
    public void ResetMode_AltBufferWithCursor_RestoresCursor()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.Buffer.SetCursor(20, 10);
        var savedX = terminal.Buffer.X;
        var savedY = terminal.Buffer.Y;

        terminal.Write($"\x1B[?{(int)TerminalMode.AltBufferCursor}h"); // Save and switch
        terminal.Buffer.SetCursor(5, 5); // Move cursor in alt buffer

        // Act - DEC private mode (switches back and restores cursor)
        terminal.Write($"\x1B[?{(int)TerminalMode.AltBufferCursor}l");

        // Assert
        terminal.Buffer.X.Should().Be(savedX);
        terminal.Buffer.Y.Should().Be(savedY);
    }

    [TestMethod]
    public void SetMode_SendFocusEvents_EnablesFocusEvents()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.SendFocusEvents.Should().BeFalse();

        // Act - DEC private mode
        terminal.Write($"\x1B[?{(int)TerminalMode.SendFocusEvents}h");

        // Assert
        terminal.SendFocusEvents.Should().BeTrue();
    }

    [TestMethod]
    public void ResetMode_SendFocusEvents_DisablesFocusEvents()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.SendFocusEvents = true;

        // Act - DEC private mode
        terminal.Write($"\x1B[?{(int)TerminalMode.SendFocusEvents}l");

        // Assert
        terminal.SendFocusEvents.Should().BeFalse();
    }

    [TestMethod]
    public void SetMode_Wraparound_EnablesWraparound()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.Options.Wraparound = false;

        // Act - DEC private mode
        terminal.Write($"\x1B[?{(int)TerminalMode.Wraparound}h");

        // Assert
        terminal.Options.Wraparound.Should().BeTrue();
    }

    [TestMethod]
    public void ResetMode_Wraparound_DisablesWraparound()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.Options.Wraparound.Should().BeTrue(); // Default is true

        // Act - DEC private mode
        terminal.Write($"\x1B[?{(int)TerminalMode.Wraparound}l");

        // Assert
        terminal.Options.Wraparound.Should().BeFalse();
    }

    [TestMethod]
    public void SetMode_MultipleModes_EnablesAll()
    {
        // Arrange
        var terminal = CreateTerminal();

        // Act - Set multiple modes at once
        terminal.Write($"\x1B[?{(int)TerminalMode.AppCursorKeys};{(int)TerminalMode.ShowCursor};{(int)TerminalMode.AppKeypad}h");

        // Assert
        terminal.ApplicationCursorKeys.Should().BeTrue();
        terminal.CursorVisible.Should().BeTrue();
        terminal.ApplicationKeypad.Should().BeTrue();
    }

    [TestMethod]
    public void ResetMode_MultipleModes_DisablesAll()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.ApplicationCursorKeys = true;
        terminal.CursorVisible = true;
        terminal.ApplicationKeypad = true;

        // Act - Reset multiple modes at once
        terminal.Write($"\x1B[?{(int)TerminalMode.AppCursorKeys};{(int)TerminalMode.ShowCursor};{(int)TerminalMode.AppKeypad}l");

        // Assert
        terminal.ApplicationCursorKeys.Should().BeFalse();
        terminal.CursorVisible.Should().BeFalse();
        terminal.ApplicationKeypad.Should().BeFalse();
    }

    [TestMethod]
    public void TerminalReset_ResetsAllModes()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.InsertMode = true;
        terminal.ApplicationCursorKeys = true;
        terminal.ApplicationKeypad = true;
        terminal.BracketedPasteMode = true;
        terminal.OriginMode = true;
        terminal.CursorVisible = false;
        terminal.SendFocusEvents = true;
        terminal.Win32InputMode = true;
        terminal.MetaSendsEscape = true;
        terminal.AltSendsEscape = true;

        // Act
        terminal.Reset();

        // Assert
        terminal.InsertMode.Should().BeFalse();
        terminal.ApplicationCursorKeys.Should().BeFalse();
        terminal.ApplicationKeypad.Should().BeFalse();
        terminal.BracketedPasteMode.Should().BeFalse();
        terminal.OriginMode.Should().BeFalse();
        terminal.CursorVisible.Should().BeTrue(); // Default is true
        terminal.SendFocusEvents.Should().BeFalse();
        terminal.Win32InputMode.Should().BeFalse();
        terminal.MetaSendsEscape.Should().BeFalse();
        terminal.AltSendsEscape.Should().BeFalse();
    }

    #region Win32InputMode Tests

    [TestMethod]
    public void SetMode_Win32InputMode_EnablesWin32InputMode()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.Win32InputMode.Should().BeFalse();

        // Act - DEC private mode 9001
        terminal.Write($"\x1B[?{(int)TerminalMode.Win32InputMode}h");

        // Assert
        terminal.Win32InputMode.Should().BeTrue();
    }

    [TestMethod]
    public void ResetMode_Win32InputMode_DisablesWin32InputMode()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.Win32InputMode = true;

        // Act - DEC private mode 9001
        terminal.Write($"\x1B[?{(int)TerminalMode.Win32InputMode}l");

        // Assert
        terminal.Win32InputMode.Should().BeFalse();
    }

    [TestMethod]
    public void SetMode_Win32InputMode_DisablesMetaSendsEscape()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.MetaSendsEscape = true;
        terminal.MetaSendsEscape.Should().BeTrue();

        // Act - Enable Win32InputMode
        terminal.Write($"\x1B[?{(int)TerminalMode.Win32InputMode}h");

        // Assert - MetaSendsEscape should be disabled
        terminal.Win32InputMode.Should().BeTrue();
        terminal.MetaSendsEscape.Should().BeFalse();
    }

    [TestMethod]
    public void SetMode_Win32InputMode_DisablesAltSendsEscape()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.AltSendsEscape = true;
        terminal.AltSendsEscape.Should().BeTrue();

        // Act - Enable Win32InputMode
        terminal.Write($"\x1B[?{(int)TerminalMode.Win32InputMode}h");

        // Assert - AltSendsEscape should be disabled
        terminal.Win32InputMode.Should().BeTrue();
        terminal.AltSendsEscape.Should().BeFalse();
    }

    [TestMethod]
    public void SetMode_Win32InputMode_DisablesBothEscapeModes()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.MetaSendsEscape = true;
        terminal.AltSendsEscape = true;

        // Act - Enable Win32InputMode
        terminal.Write($"\x1B[?{(int)TerminalMode.Win32InputMode}h");

        // Assert - Both escape modes should be disabled
        terminal.Win32InputMode.Should().BeTrue();
        terminal.MetaSendsEscape.Should().BeFalse();
        terminal.AltSendsEscape.Should().BeFalse();
    }

    #endregion

    #region MetaSendsEscape Tests

    [TestMethod]
    public void SetMode_MetaSendsEscape_EnablesMetaSendsEscape()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.MetaSendsEscape.Should().BeFalse();

        // Act - DEC private mode 1036
        terminal.Write($"\x1B[?{(int)TerminalMode.MetaSendsEscape}h");

        // Assert
        terminal.MetaSendsEscape.Should().BeTrue();
    }

    [TestMethod]
    public void ResetMode_MetaSendsEscape_DisablesMetaSendsEscape()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.MetaSendsEscape = true;

        // Act - DEC private mode 1036
        terminal.Write($"\x1B[?{(int)TerminalMode.MetaSendsEscape}l");

        // Assert
        terminal.MetaSendsEscape.Should().BeFalse();
    }

    [TestMethod]
    public void SetMode_MetaSendsEscape_DisablesWin32InputMode()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.Win32InputMode = true;
        terminal.Win32InputMode.Should().BeTrue();

        // Act - Enable MetaSendsEscape
        terminal.Write($"\x1B[?{(int)TerminalMode.MetaSendsEscape}h");

        // Assert - Win32InputMode should be disabled
        terminal.MetaSendsEscape.Should().BeTrue();
        terminal.Win32InputMode.Should().BeFalse();
    }

    #endregion

    #region AltSendsEscape Tests

    [TestMethod]
    public void SetMode_AltSendsEscape_EnablesAltSendsEscape()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.AltSendsEscape.Should().BeFalse();

        // Act - DEC private mode 1039
        terminal.Write($"\x1B[?{(int)TerminalMode.AltSendsEscape}h");

        // Assert
        terminal.AltSendsEscape.Should().BeTrue();
    }

    [TestMethod]
    public void ResetMode_AltSendsEscape_DisablesAltSendsEscape()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.AltSendsEscape = true;

        // Act - DEC private mode 1039
        terminal.Write($"\x1B[?{(int)TerminalMode.AltSendsEscape}l");

        // Assert
        terminal.AltSendsEscape.Should().BeFalse();
    }

    [TestMethod]
    public void SetMode_AltSendsEscape_DisablesWin32InputMode()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.Win32InputMode = true;
        terminal.Win32InputMode.Should().BeTrue();

        // Act - Enable AltSendsEscape
        terminal.Write($"\x1B[?{(int)TerminalMode.AltSendsEscape}h");

        // Assert - Win32InputMode should be disabled
        terminal.AltSendsEscape.Should().BeTrue();
        terminal.Win32InputMode.Should().BeFalse();
    }

    #endregion

    #region Mode Switching Scenarios

    [TestMethod]
    public void ModeSwitching_Win32ToMeta_SwitchesCorrectly()
    {
        // Arrange - Start with Win32InputMode enabled (like cmd.exe)
        var terminal = CreateTerminal();
        terminal.Write($"\x1B[?{(int)TerminalMode.Win32InputMode}h");
        terminal.Win32InputMode.Should().BeTrue();

        // Act - Switch to MetaSendsEscape (like EDIT does)
        terminal.Write($"\x1B[?{(int)TerminalMode.MetaSendsEscape}h");

        // Assert
        terminal.Win32InputMode.Should().BeFalse();
        terminal.MetaSendsEscape.Should().BeTrue();
    }

    [TestMethod]
    public void ModeSwitching_MetaToWin32_SwitchesCorrectly()
    {
        // Arrange - Start with MetaSendsEscape enabled
        var terminal = CreateTerminal();
        terminal.Write($"\x1B[?{(int)TerminalMode.MetaSendsEscape}h");
        terminal.MetaSendsEscape.Should().BeTrue();

        // Act - Switch back to Win32InputMode (like when EDIT exits and cmd.exe regains control)
        terminal.Write($"\x1B[?{(int)TerminalMode.Win32InputMode}h");

        // Assert
        terminal.Win32InputMode.Should().BeTrue();
        terminal.MetaSendsEscape.Should().BeFalse();
    }

    [TestMethod]
    public void ModeSwitching_DisableMetaThenEnableWin32_WorksCorrectly()
    {
        // Arrange - Start with MetaSendsEscape enabled
        var terminal = CreateTerminal();
        terminal.Write($"\x1B[?{(int)TerminalMode.MetaSendsEscape}h");
        terminal.MetaSendsEscape.Should().BeTrue();

        // Act - First disable Meta, then enable Win32 (explicit cleanup scenario)
        terminal.Write($"\x1B[?{(int)TerminalMode.MetaSendsEscape}l");
        terminal.Write($"\x1B[?{(int)TerminalMode.Win32InputMode}h");

        // Assert
        terminal.Win32InputMode.Should().BeTrue();
        terminal.MetaSendsEscape.Should().BeFalse();
    }

    [TestMethod]
    public void ModeSwitching_ChildProcessScenario_CmdToEditAndBack()
    {
        // This simulates: cmd.exe -> user runs EDIT -> EDIT exits -> back to cmd.exe
        var terminal = CreateTerminal();

        // Step 1: cmd.exe starts and enables Win32InputMode
        terminal.Write($"\x1B[?{(int)TerminalMode.Win32InputMode}h");
        terminal.Win32InputMode.Should().BeTrue();
        terminal.MetaSendsEscape.Should().BeFalse();

        // Step 2: User runs EDIT, which enables MetaSendsEscape
        terminal.Write($"\x1B[?{(int)TerminalMode.MetaSendsEscape}h");
        terminal.Win32InputMode.Should().BeFalse();
        terminal.MetaSendsEscape.Should().BeTrue();

        // Step 3: User exits EDIT, which disables MetaSendsEscape
        terminal.Write($"\x1B[?{(int)TerminalMode.MetaSendsEscape}l");
        terminal.Win32InputMode.Should().BeFalse();
        terminal.MetaSendsEscape.Should().BeFalse();

        // Step 4: cmd.exe regains control and re-enables Win32InputMode
        terminal.Write($"\x1B[?{(int)TerminalMode.Win32InputMode}h");
        terminal.Win32InputMode.Should().BeTrue();
        terminal.MetaSendsEscape.Should().BeFalse();
    }

    [TestMethod]
    public void ModeSwitching_MultipleApps_ComplexScenario()
    {
        // Simulates: cmd.exe -> FAR Manager -> vim (nested child processes)
        var terminal = CreateTerminal();

        // cmd.exe starts
        terminal.Write($"\x1B[?{(int)TerminalMode.Win32InputMode}h");
        terminal.Win32InputMode.Should().BeTrue();

        // FAR Manager starts (also uses Win32, re-asserts mode)
        terminal.Write($"\x1B[?{(int)TerminalMode.Win32InputMode}h");
        terminal.Win32InputMode.Should().BeTrue();

        // vim starts from FAR, uses AltSendsEscape
        terminal.Write($"\x1B[?{(int)TerminalMode.AltSendsEscape}h");
        terminal.Win32InputMode.Should().BeFalse();
        terminal.AltSendsEscape.Should().BeTrue();

        // vim exits
        terminal.Write($"\x1B[?{(int)TerminalMode.AltSendsEscape}l");
        terminal.AltSendsEscape.Should().BeFalse();

        // FAR re-enables Win32
        terminal.Write($"\x1B[?{(int)TerminalMode.Win32InputMode}h");
        terminal.Win32InputMode.Should().BeTrue();

        // FAR exits, cmd.exe re-enables Win32
        terminal.Write($"\x1B[?{(int)TerminalMode.Win32InputMode}h");
        terminal.Win32InputMode.Should().BeTrue();
    }

    #endregion

    #region Default Values Tests

    [TestMethod]
    public void DefaultValues_Win32InputMode_IsFalse()
    {
        var terminal = CreateTerminal();
        terminal.Win32InputMode.Should().BeFalse();
    }

    [TestMethod]
    public void DefaultValues_MetaSendsEscape_IsFalse()
    {
        var terminal = CreateTerminal();
        terminal.MetaSendsEscape.Should().BeFalse();
    }

    [TestMethod]
    public void DefaultValues_AltSendsEscape_IsFalse()
    {
        var terminal = CreateTerminal();
        terminal.AltSendsEscape.Should().BeFalse();
    }

    #endregion
}
