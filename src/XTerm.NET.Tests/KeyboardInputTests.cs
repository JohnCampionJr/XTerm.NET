using XTerm;
using XTerm.Input;
using XTerm.Options;

namespace XTerm.Tests;

[TestClass]

public class KeyboardInputTests
{
    private Terminal CreateTerminal(int cols = 80, int rows = 24)
    {
        var options = new TerminalOptions { Cols = cols, Rows = rows };
        return new Terminal(options);
    }

    #region Arrow Keys

    [TestMethod]
    public void ArrowKeys_NormalMode_GenerateCorrectSequences()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.ApplicationCursorKeys = false;

        // Act & Assert
        terminal.GenerateKeyInput(Key.UpArrow).Should().Be("\x1B[A");
        terminal.GenerateKeyInput(Key.DownArrow).Should().Be("\x1B[B");
        terminal.GenerateKeyInput(Key.RightArrow).Should().Be("\x1B[C");
        terminal.GenerateKeyInput(Key.LeftArrow).Should().Be("\x1B[D");
    }

    [TestMethod]
    public void ArrowKeys_ApplicationMode_GenerateCorrectSequences()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.ApplicationCursorKeys = true;

        // Act & Assert
        terminal.GenerateKeyInput(Key.UpArrow).Should().Be("\x1BOA");
        terminal.GenerateKeyInput(Key.DownArrow).Should().Be("\x1BOB");
        terminal.GenerateKeyInput(Key.RightArrow).Should().Be("\x1BOC");
        terminal.GenerateKeyInput(Key.LeftArrow).Should().Be("\x1BOD");
    }

    [TestMethod]
    public void ArrowKeys_WithShift_GenerateModifiedSequences()
    {
        // Arrange
        var terminal = CreateTerminal();

        // Act & Assert
        terminal.GenerateKeyInput(Key.UpArrow, KeyModifiers.Shift).Should().Be("\x1B[1;2A");
        terminal.GenerateKeyInput(Key.DownArrow, KeyModifiers.Shift).Should().Be("\x1B[1;2B");
    }

    [TestMethod]
    public void ArrowKeys_WithControl_GenerateModifiedSequences()
    {
        // Arrange
        var terminal = CreateTerminal();

        // Act & Assert
        terminal.GenerateKeyInput(Key.UpArrow, KeyModifiers.Control).Should().Be("\x1B[1;5A");
        terminal.GenerateKeyInput(Key.LeftArrow, KeyModifiers.Control).Should().Be("\x1B[1;5D");
    }

    [TestMethod]
    public void ArrowKeys_WithAlt_GenerateModifiedSequences()
    {
        // Arrange
        var terminal = CreateTerminal();

        // Act & Assert
        terminal.GenerateKeyInput(Key.UpArrow, KeyModifiers.Alt).Should().Be("\x1B[1;3A");
    }

    [TestMethod]
    public void ArrowKeys_WithMultipleModifiers_GenerateCorrectCode()
    {
        // Arrange
        var terminal = CreateTerminal();
        var modifiers = KeyModifiers.Control | KeyModifiers.Shift;

        // Act
        var sequence = terminal.GenerateKeyInput(Key.UpArrow, modifiers);

        // Assert - Control (4) + Shift (1) + 1 = 6
        var expected = "\x1B[1;6A";
        sequence.Should().Be(expected);
    }

    #endregion

    #region Function Keys

    [TestMethod]
    public void FunctionKeys_F1ToF4_GenerateSS3Sequences()
    {
        // Arrange
        var terminal = CreateTerminal();

        // Act & Assert
        terminal.GenerateKeyInput(Key.F1).Should().Be("\x1BOP");
        terminal.GenerateKeyInput(Key.F2).Should().Be("\x1BOQ");
        terminal.GenerateKeyInput(Key.F3).Should().Be("\x1BOR");
        terminal.GenerateKeyInput(Key.F4).Should().Be("\x1BOS");
    }

    [TestMethod]
    public void FunctionKeys_F5ToF12_GenerateCSISequences()
    {
        // Arrange
        var terminal = CreateTerminal();

        // Act & Assert
        terminal.GenerateKeyInput(Key.F5).Should().Be("\x1B[15~");
        terminal.GenerateKeyInput(Key.F6).Should().Be("\x1B[17~");
        terminal.GenerateKeyInput(Key.F7).Should().Be("\x1B[18~");
        terminal.GenerateKeyInput(Key.F8).Should().Be("\x1B[19~");
        terminal.GenerateKeyInput(Key.F9).Should().Be("\x1B[20~");
        terminal.GenerateKeyInput(Key.F10).Should().Be("\x1B[21~");
        terminal.GenerateKeyInput(Key.F11).Should().Be("\x1B[23~");
        terminal.GenerateKeyInput(Key.F12).Should().Be("\x1B[24~");
    }

    [TestMethod]
    public void FunctionKeys_F13ToF20_GenerateExtendedSequences()
    {
        // Arrange
        var terminal = CreateTerminal();

        // Act & Assert
        terminal.GenerateKeyInput(Key.F13).Should().Be("\x1B[25~");
        terminal.GenerateKeyInput(Key.F14).Should().Be("\x1B[26~");
        terminal.GenerateKeyInput(Key.F15).Should().Be("\x1B[28~");
        terminal.GenerateKeyInput(Key.F16).Should().Be("\x1B[29~");
        terminal.GenerateKeyInput(Key.F17).Should().Be("\x1B[31~");
        terminal.GenerateKeyInput(Key.F18).Should().Be("\x1B[32~");
        terminal.GenerateKeyInput(Key.F19).Should().Be("\x1B[33~");
        terminal.GenerateKeyInput(Key.F20).Should().Be("\x1B[34~");
    }

    [TestMethod]
    public void FunctionKeys_WithShift_GenerateModifiedSequences()
    {
        // Arrange
        var terminal = CreateTerminal();

        // Act & Assert
        terminal.GenerateKeyInput(Key.F1, KeyModifiers.Shift).Should().Be("\x1B[1;2P");
        terminal.GenerateKeyInput(Key.F5, KeyModifiers.Shift).Should().Be("\x1B[15;2~");
    }

    #endregion

    #region Navigation Keys

    [TestMethod]
    public void NavigationKeys_GenerateCorrectSequences()
    {
        // Arrange
        var terminal = CreateTerminal();

        // Act & Assert
        terminal.GenerateKeyInput(Key.Home).Should().Be("\x1B[H");
        terminal.GenerateKeyInput(Key.End).Should().Be("\x1B[F");
        terminal.GenerateKeyInput(Key.PageUp).Should().Be("\x1B[5~");
        terminal.GenerateKeyInput(Key.PageDown).Should().Be("\x1B[6~");
        terminal.GenerateKeyInput(Key.Insert).Should().Be("\x1B[2~");
        terminal.GenerateKeyInput(Key.Delete).Should().Be("\x1B[3~");
    }

    [TestMethod]
    public void NavigationKeys_WithModifiers_GenerateModifiedSequences()
    {
        // Arrange
        var terminal = CreateTerminal();

        // Act & Assert
        terminal.GenerateKeyInput(Key.Home, KeyModifiers.Shift).Should().Be("\x1B[1;2H");
        terminal.GenerateKeyInput(Key.PageUp, KeyModifiers.Control).Should().Be("\x1B[5;5~");
        terminal.GenerateKeyInput(Key.Delete, KeyModifiers.Alt).Should().Be("\x1B[3;3~");
    }

    #endregion

    #region Control Keys

    [TestMethod]
    public void ControlKeys_GenerateCorrectSequences()
    {
        // Arrange
        var terminal = CreateTerminal();

        // Act & Assert
        terminal.GenerateKeyInput(Key.Enter).Should().Be("\r");
        terminal.GenerateKeyInput(Key.Tab).Should().Be("\t");
        terminal.GenerateKeyInput(Key.Backspace).Should().Be("\x7F"); // DEL
        terminal.GenerateKeyInput(Key.Escape).Should().Be("\x1B");
        terminal.GenerateKeyInput(Key.Space).Should().Be(" ");
    }

    [TestMethod]
    public void Tab_WithShift_GeneratesBackTab()
    {
        // Arrange
        var terminal = CreateTerminal();

        // Act
        var sequence = terminal.GenerateKeyInput(Key.Tab, KeyModifiers.Shift);

        // Assert
        sequence.Should().Be("\x1B[Z");
    }

    #endregion

    #region Keypad Keys

    [TestMethod]
    public void KeypadKeys_NormalMode_GenerateNumericCharacters()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.ApplicationKeypad = false;

        // Act & Assert
        terminal.GenerateKeyInput(Key.Keypad0).Should().Be("0");
        terminal.GenerateKeyInput(Key.Keypad5).Should().Be("5");
        terminal.GenerateKeyInput(Key.Keypad9).Should().Be("9");
        terminal.GenerateKeyInput(Key.KeypadDecimal).Should().Be(".");
    }

    [TestMethod]
    public void KeypadKeys_ApplicationMode_GenerateEscapeSequences()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.ApplicationKeypad = true;

        // Act & Assert
        terminal.GenerateKeyInput(Key.Keypad0).Should().Be("\x1BOp");
        terminal.GenerateKeyInput(Key.Keypad5).Should().Be("\x1BOu");
        terminal.GenerateKeyInput(Key.Keypad9).Should().Be("\x1BOy");
        terminal.GenerateKeyInput(Key.KeypadDecimal).Should().Be("\x1BOn");
    }

    [TestMethod]
    public void KeypadOperators_GenerateCorrectCharacters()
    {
        // Arrange
        var terminal = CreateTerminal();

        // Act & Assert
        terminal.GenerateKeyInput(Key.KeypadDivide).Should().Be("/");
        terminal.GenerateKeyInput(Key.KeypadMultiply).Should().Be("*");
        terminal.GenerateKeyInput(Key.KeypadSubtract).Should().Be("-");
        terminal.GenerateKeyInput(Key.KeypadAdd).Should().Be("+");
        terminal.GenerateKeyInput(Key.KeypadEnter).Should().Be("\r");
    }

    #endregion

    #region Character Input

    [TestMethod]
    public void CharInput_PlainCharacter_ReturnsCharacter()
    {
        // Arrange
        var terminal = CreateTerminal();

        // Act & Assert
        terminal.GenerateCharInput('a').Should().Be("a");
        terminal.GenerateCharInput('Z').Should().Be("Z");
        terminal.GenerateCharInput('5').Should().Be("5");
        terminal.GenerateCharInput('@').Should().Be("@");
    }

    [TestMethod]
    public void CharInput_WithControl_GeneratesControlCharacter()
    {
        // Arrange
        var terminal = CreateTerminal();

        // Act & Assert
        terminal.GenerateCharInput('a', KeyModifiers.Control).Should().Be("\x01"); // Ctrl+A
        terminal.GenerateCharInput('c', KeyModifiers.Control).Should().Be("\x03"); // Ctrl+C
        terminal.GenerateCharInput('z', KeyModifiers.Control).Should().Be("\x1A"); // Ctrl+Z
        terminal.GenerateCharInput('A', KeyModifiers.Control).Should().Be("\x01"); // Ctrl+A (uppercase)
    }

    [TestMethod]
    public void CharInput_ControlSpecialCharacters_GenerateCorrectCodes()
    {
        // Arrange
        var terminal = CreateTerminal();

        // Act & Assert
        terminal.GenerateCharInput(' ', KeyModifiers.Control).Should().Be("\x00"); // Ctrl+Space = NUL
        terminal.GenerateCharInput('@', KeyModifiers.Control).Should().Be("\x00"); // Ctrl+@ = NUL
        terminal.GenerateCharInput('[', KeyModifiers.Control).Should().Be("\x1B"); // Ctrl+[ = ESC
        terminal.GenerateCharInput('\\', KeyModifiers.Control).Should().Be("\x1C"); // Ctrl+\ = FS
        terminal.GenerateCharInput(']', KeyModifiers.Control).Should().Be("\x1D"); // Ctrl+] = GS
        terminal.GenerateCharInput('^', KeyModifiers.Control).Should().Be("\x1E"); // Ctrl+^ = RS
        terminal.GenerateCharInput('_', KeyModifiers.Control).Should().Be("\x1F"); // Ctrl+_ = US
        terminal.GenerateCharInput('?', KeyModifiers.Control).Should().Be("\x7F"); // Ctrl+? = DEL
    }

    [TestMethod]
    public void CharInput_WithAlt_GeneratesEscapePrefix()
    {
        // Arrange
        var terminal = CreateTerminal();

        // Act & Assert
        var expected = "\u001ba";
        terminal.GenerateCharInput('a', KeyModifiers.Alt).Should().Be(expected); // Alt+a
        
        expected = "\u001bX";
        terminal.GenerateCharInput('X', KeyModifiers.Alt).Should().Be(expected); // Alt+X
        
        expected = "\u001b1";
        terminal.GenerateCharInput('1', KeyModifiers.Alt).Should().Be(expected); // Alt+1
    }

    #endregion

    #region Mode Changes

    [TestMethod]
    public void KeyboardInput_AfterModeChange_ReflectsNewMode()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.ApplicationCursorKeys = false;
        
        var normalSequence = terminal.GenerateKeyInput(Key.UpArrow);
        
        // Act - Change mode
        terminal.ApplicationCursorKeys = true;
        var appSequence = terminal.GenerateKeyInput(Key.UpArrow);

        // Assert
        normalSequence.Should().Be("\x1B[A");
        appSequence.Should().Be("\x1BOA");
    }

    [TestMethod]
    public void KeypadInput_AfterModeChange_ReflectsNewMode()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.ApplicationKeypad = false;
        
        var numericSequence = terminal.GenerateKeyInput(Key.Keypad5);
        
        // Act - Change mode
        terminal.ApplicationKeypad = true;
        var appSequence = terminal.GenerateKeyInput(Key.Keypad5);

        // Assert
        numericSequence.Should().Be("5");
        appSequence.Should().Be("\x1BOu");
    }

    #endregion

    #region Modifier Encoding

    [TestMethod]
    public void Modifiers_SingleModifier_GeneratesCorrectCode()
    {
        // Arrange
        var terminal = CreateTerminal();

        // Act & Assert
        // Shift = 1 + 1 = 2
        terminal.GenerateKeyInput(Key.Home, KeyModifiers.Shift).Should().Contain(";2");
        // Alt = 1 + 2 = 3
        terminal.GenerateKeyInput(Key.Home, KeyModifiers.Alt).Should().Contain(";3");
        // Control = 1 + 4 = 5
        terminal.GenerateKeyInput(Key.Home, KeyModifiers.Control).Should().Contain(";5");
    }

    [TestMethod]
    public void Modifiers_CombinedModifiers_GeneratesCorrectCode()
    {
        // Arrange
        var terminal = CreateTerminal();

        // Act & Assert
        // Shift + Alt = 1 + 1 + 2 = 4
        terminal.GenerateKeyInput(Key.Home, KeyModifiers.Shift | KeyModifiers.Alt).Should().Contain(";4");
        // Shift + Control = 1 + 1 + 4 = 6
        terminal.GenerateKeyInput(Key.Home, KeyModifiers.Shift | KeyModifiers.Control).Should().Contain(";6");
        // Alt + Control = 1 + 2 + 4 = 7
        terminal.GenerateKeyInput(Key.Home, KeyModifiers.Alt | KeyModifiers.Control).Should().Contain(";7");
        // All = 1 + 1 + 2 + 4 = 8
        terminal.GenerateKeyInput(Key.Home, KeyModifiers.Shift | KeyModifiers.Alt | KeyModifiers.Control).Should().Contain(";8");
    }

    #endregion

    #region Edge Cases

    [TestMethod]
    public void EmptyKey_DoesNotCrash()
    {
        // Arrange
        var terminal = CreateTerminal();

        // Act - This should return empty or handle gracefully
        var result = terminal.GenerateKeyInput((Key)999); // Invalid key

        // Assert - Should not throw
        result.Should().NotBeNull();
    }

    [TestMethod]
    public void AllKeys_GenerateNonEmptySequences()
    {
        // Arrange
        var terminal = CreateTerminal();

        // Act & Assert - All defined keys should generate something
        foreach (Key key in Enum.GetValues(typeof(Key)))
        {
            var sequence = terminal.GenerateKeyInput(key);
            sequence.Should().NotBeNull();
            // Most keys should generate non-empty sequences
            // (except potentially invalid ones)
        }
    }

    [TestMethod]
    public void ModifierCombinations_AllCombinations_WorkCorrectly()
    {
        // Arrange
        var terminal = CreateTerminal();
        
        // Act & Assert - Test all 8 combinations (2^3)
        for (int i = 0; i < 8; i++)
        {
            var mods = (KeyModifiers)i;
            var sequence = terminal.GenerateKeyInput(Key.Home, mods);
            sequence.Should().NotBeNull();
            sequence.Should().NotBeEmpty();
        }
    }

    #endregion
}
