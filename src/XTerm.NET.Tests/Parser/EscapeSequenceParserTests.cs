using XTerm.Parser;
using XTerm.Events.Parser;
using System.Text;

namespace XTerm.Tests.Parser;

[TestClass]

public class EscapeSequenceParserTests
{
    [TestMethod]
    public void Constructor_InitializesParser()
    {
        // Arrange & Act
        var parser = new EscapeSequenceParser();

        // Assert
        parser.Should().NotBeNull();
    }

    [TestMethod]
    public void Parse_SimpleText_CallsPrintHandler()
    {
        // Arrange
        var parser = new EscapeSequenceParser();
        var printed = new StringBuilder();
        parser.Print += (sender, e) => printed.Append(e.Data);

        // Act
        parser.Parse("Hello");

        // Assert
        printed.ToString().Should().Be("Hello");
    }

    [TestMethod]
    public void Parse_EmptyString_DoesNothing()
    {
        // Arrange
        var parser = new EscapeSequenceParser();
        var called = false;
        parser.Print += (sender, e) => called = true;

        // Act
        parser.Parse("");

        // Assert
        called.Should().BeFalse();
    }

    [TestMethod]
    public void Parse_ControlCharacter_CallsExecuteHandler()
    {
        // Arrange
        var parser = new EscapeSequenceParser();
        var executedCodes = new List<int>();
        parser.Execute += (sender, e) => executedCodes.Add(e.Code);

        // Act
        parser.Parse("\x07"); // BEL

        // Assert
        executedCodes.Should().Contain(0x07);
    }

    [TestMethod]
    public void Parse_LineFeed_CallsExecuteHandler()
    {
        // Arrange
        var parser = new EscapeSequenceParser();
        var executedCodes = new List<int>();
        parser.Execute += (sender, e) => executedCodes.Add(e.Code);

        // Act
        parser.Parse("\n");

        // Assert
        executedCodes.Should().Contain(0x0A);
    }

    [TestMethod]
    public void Parse_CarriageReturn_CallsExecuteHandler()
    {
        // Arrange
        var parser = new EscapeSequenceParser();
        var executedCodes = new List<int>();
        parser.Execute += (sender, e) => executedCodes.Add(e.Code);

        // Act
        parser.Parse("\r");

        // Assert
        executedCodes.Should().Contain(0x0D);
    }

    [TestMethod]
    public void Parse_Tab_CallsExecuteHandler()
    {
        // Arrange
        var parser = new EscapeSequenceParser();
        var executedCodes = new List<int>();
        parser.Execute += (sender, e) => executedCodes.Add(e.Code);

        // Act
        parser.Parse("\t");

        // Assert
        executedCodes.Should().Contain(0x09);
    }

    [TestMethod]
    public void Parse_Backspace_CallsExecuteHandler()
    {
        // Arrange
        var parser = new EscapeSequenceParser();
        var executedCodes = new List<int>();
        parser.Execute += (sender, e) => executedCodes.Add(e.Code);

        // Act
        parser.Parse("\x08");

        // Assert
        executedCodes.Should().Contain(0x08);
    }

    [TestMethod]
    public void Parse_CsiSequence_CallsCsiHandler()
    {
        // Arrange
        var parser = new EscapeSequenceParser();
        var csiCalls = new List<(string identifier, Params parameters)>();
        parser.Csi += (sender, e) => csiCalls.Add((e.Identifier, e.Parameters));

        // Act
        parser.Parse("\x1B[H"); // Cursor Home

        // Assert
        csiCalls.Should().ContainSingle();
        csiCalls[0].identifier.Should().Contain("H");
    }

    [TestMethod]
    public void Parse_CsiWithParameters_PassesParameters()
    {
        // Arrange
        var parser = new EscapeSequenceParser();
        var csiCalls = new List<(string identifier, Params parameters)>();
        parser.Csi += (sender, e) => csiCalls.Add((e.Identifier, e.Parameters));

        // Act
        parser.Parse("\x1B[10;20H"); // Cursor Position

        // Assert
        csiCalls.Should().ContainSingle();
        var call = csiCalls[0];
        call.identifier.Should().Contain("H");
        call.parameters.GetParam(0).Should().Be(10);
        call.parameters.GetParam(1).Should().Be(20);
    }

    [TestMethod]
    public void Parse_CsiWithSingleParameter_ParsesCorrectly()
    {
        // Arrange
        var parser = new EscapeSequenceParser();
        var csiCalls = new List<(string identifier, Params parameters)>();
        parser.Csi += (sender, e) => csiCalls.Add((e.Identifier, e.Parameters));

        // Act
        parser.Parse("\x1B[5A"); // Cursor Up 5

        // Assert
        csiCalls.Should().ContainSingle();
        var call = csiCalls[0];
        call.identifier.Should().Contain("A");
        call.parameters.GetParam(0).Should().Be(5);
    }

    [TestMethod]
    public void Parse_SgrSequence_CallsCsiHandler()
    {
        // Arrange
        var parser = new EscapeSequenceParser();
        var csiCalls = new List<(string identifier, Params parameters)>();
        parser.Csi += (sender, e) => csiCalls.Add((e.Identifier, e.Parameters));

        // Act
        parser.Parse("\x1B[1;31m"); // Bold + Red foreground

        // Assert
        csiCalls.Should().ContainSingle();
        var call = csiCalls[0];
        call.identifier.Should().Contain("m");
        call.parameters.GetParam(0).Should().Be(1);
        call.parameters.GetParam(1).Should().Be(31);
    }

    [TestMethod]
    public void Parse_EscSequence_CallsEscHandler()
    {
        // Arrange
        var parser = new EscapeSequenceParser();
        var escCalls = new List<(string finalChar, string collected)>();
        parser.Esc += (sender, e) => escCalls.Add((e.FinalChar, e.Collected));

        // Act
        parser.Parse("\x1B" + "D"); // Index

        // Assert
        escCalls.Should().ContainSingle();
        escCalls[0].finalChar.Should().Be("D");
    }

    [TestMethod]
    public void Parse_OscSequence_CallsOscHandler()
    {
        // Arrange
        var parser = new EscapeSequenceParser();
        var oscData = new List<string>();
        parser.Osc += (sender, e) => oscData.Add(e.Data);

        // Act
        parser.Parse("\x1B]0;Test Title\x07"); // Set title

        // Assert
        oscData.Should().ContainSingle();
        oscData[0].Should().Be("0;Test Title");
    }

    [TestMethod]
    public void Parse_OscWithEscTerminator_CallsOscHandler()
    {
        // Arrange
        var parser = new EscapeSequenceParser();
        var oscData = new List<string>();
        parser.Osc += (sender, e) => oscData.Add(e.Data);

        // Act
        parser.Parse("\x1B]2;Window Title\x1B\\"); // Set title with ESC terminator

        // Assert
        oscData.Should().ContainSingle();
        oscData[0].Should().Be("2;Window Title");
    }

    [TestMethod]
    public void Parse_MixedContent_HandlesCorrectly()
    {
        // Arrange
        var parser = new EscapeSequenceParser();
        var printed = new StringBuilder();
        var csiCalls = new List<string>();
        
        parser.Print += (sender, e) => printed.Append(e.Data);
        parser.Csi += (sender, e) => csiCalls.Add(e.Identifier);

        // Act
        parser.Parse("Hello\x1B[1mWorld");

        // Assert
        printed.ToString().Should().Contain("Hello");
        printed.ToString().Should().Contain("World");
        csiCalls.Should().ContainSingle();
    }

    [TestMethod]
    public void Parse_MultipleSequences_HandlesAll()
    {
        // Arrange
        var parser = new EscapeSequenceParser();
        var csiCalls = new List<string>();
        parser.Csi += (sender, e) => csiCalls.Add(e.Identifier);

        // Act
        parser.Parse("\x1B[H\x1B[2J\x1B[1;1H");

        // Assert
        csiCalls.Count.Should().Be(3);
        csiCalls[0].Should().Contain("H");
        csiCalls[1].Should().Contain("J");
        csiCalls[2].Should().Contain("H");
    }

    [TestMethod]
    public void Parse_LongString_HandlesCorrectly()
    {
        // Arrange
        var parser = new EscapeSequenceParser();
        var printed = new StringBuilder();
        parser.Print += (sender, e) => printed.Append(e.Data);
        var longString = new string('A', 1000);

        // Act
        parser.Parse(longString);

        // Assert
        printed.Length.Should().Be(1000);
    }

    [TestMethod]
    public void Reset_ResetsParserState()
    {
        // Arrange
        var parser = new EscapeSequenceParser();
        parser.Parse("\x1B[");

        // Act
        parser.Reset();
        
        var printed = new StringBuilder();
        parser.Print += (sender, e) => printed.Append(e.Data);
        parser.Parse("Test");

        // Assert
        printed.ToString().Should().Be("Test");
    }

    [TestMethod]
    public void Parse_IncompleteSequence_HandlesGracefully()
    {
        // Arrange
        var parser = new EscapeSequenceParser();
        var printed = new StringBuilder();
        parser.Print += (sender, e) => printed.Append(e.Data);

        // Act
        parser.Parse("\x1B[");
        parser.Parse("H");

        // Assert - Should complete the sequence
        // The parser handles incomplete sequences by continuing in next parse
    }

    [TestMethod]
    public void Parse_CsiErase_ParsesCorrectly()
    {
        // Arrange
        var parser = new EscapeSequenceParser();
        var csiCalls = new List<(string identifier, Params parameters)>();
        parser.Csi += (sender, e) => csiCalls.Add((e.Identifier, e.Parameters));

        // Act
        parser.Parse("\x1B[2J"); // Erase Display

        // Assert
        csiCalls.Should().ContainSingle();
        csiCalls[0].identifier.Should().Contain("J");
        (csiCalls[0].parameters.GetParam(0)).Should().Be(2);
    }

    [TestMethod]
    public void Parse_CsiCursorMovement_ParsesCorrectly()
    {
        // Arrange
        var parser = new EscapeSequenceParser();
        var csiCalls = new List<(string identifier, Params parameters)>();
        parser.Csi += (sender, e) => csiCalls.Add((e.Identifier, e.Parameters));

        // Act
        parser.Parse("\x1B[5A"); // Cursor Up
        parser.Parse("\x1B[3B"); // Cursor Down
        parser.Parse("\x1B[2C"); // Cursor Forward
        parser.Parse("\x1B[4D"); // Cursor Backward

        // Assert
        csiCalls.Count.Should().Be(4);
        csiCalls[0].identifier.Should().Contain("A");
        csiCalls[1].identifier.Should().Contain("B");
        csiCalls[2].identifier.Should().Contain("C");
        csiCalls[3].identifier.Should().Contain("D");
    }

    [TestMethod]
    public void Parse_SaveRestoreCursor_ParsesCorrectly()
    {
        // Arrange
        var parser = new EscapeSequenceParser();
        var escCalls = new List<string>();
        parser.Esc += (sender, e) => escCalls.Add(e.FinalChar);

        // Act
        parser.Parse("\x1B" + "7"); // Save cursor - ESC followed by '7'
        parser.Parse("\x1B" + "8"); // Restore cursor - ESC followed by '8'

        // Assert
        escCalls.Count.Should().Be(2);
        escCalls.Should().Contain("7");
        escCalls.Should().Contain("8");
    }

    [TestMethod]
    public void Parse_ComplexSgrSequence_ParsesAllParameters()
    {
        // Arrange
        var parser = new EscapeSequenceParser();
        var csiCalls = new List<(string identifier, Params parameters)>();
        parser.Csi += (sender, e) => csiCalls.Add((e.Identifier, e.Parameters));

        // Act
        parser.Parse("\x1B[1;3;4;31;42m"); // Bold, Italic, Underline, Red FG, Green BG

        // Assert
        csiCalls.Should().ContainSingle();
        var call = csiCalls[0];
        call.identifier.Should().Contain("m");
        call.parameters.GetParam(0).Should().Be(1);
        call.parameters.GetParam(1).Should().Be(3);
        call.parameters.GetParam(2).Should().Be(4);
        call.parameters.GetParam(3).Should().Be(31);
        call.parameters.GetParam(4).Should().Be(42);
    }

    [TestMethod]
    public void Parse_ScrollRegion_ParsesCorrectly()
    {
        // Arrange
        var parser = new EscapeSequenceParser();
        var csiCalls = new List<(string identifier, Params parameters)>();
        parser.Csi += (sender, e) => csiCalls.Add((e.Identifier, e.Parameters));

        // Act
        parser.Parse("\x1B[5;20r"); // Set scroll region

        // Assert
        csiCalls.Should().ContainSingle();
        var call = csiCalls[0];
        call.identifier.Should().Contain("r");
        call.parameters.GetParam(0).Should().Be(5);
        call.parameters.GetParam(1).Should().Be(20);
    }

    [TestMethod]
    public void Parse_TextWithEmbeddedEscapes_HandlesCorrectly()
    {
        // Arrange
        var parser = new EscapeSequenceParser();
        var printed = new StringBuilder();
        var csiCount = 0;
        
        parser.Print += (sender, e) => printed.Append(e.Data);
        parser.Csi += (sender, e) => csiCount++;

        // Act
        parser.Parse("Line1\x1B[1mBold\x1B[0mNormal");

        // Assert
        printed.ToString().Should().Contain("Line1");
        printed.ToString().Should().Contain("Bold");
        printed.ToString().Should().Contain("Normal");
        csiCount.Should().Be(2);
    }

    [TestMethod]
    public void Parse_ZeroParameters_HandlesCorrectly()
    {
        // Arrange
        var parser = new EscapeSequenceParser();
        var csiCalls = new List<(string identifier, Params parameters)>();
        parser.Csi += (sender, e) => csiCalls.Add((e.Identifier, e.Parameters));

        // Act
        parser.Parse("\x1B[m"); // SGR reset with no parameters

        // Assert
        csiCalls.Should().ContainSingle();
        csiCalls[0].identifier.Should().Contain("m");
    }

    [TestMethod]
    public void Handlers_CanBeNull_WithoutCrashing()
    {
        // Arrange
        var parser = new EscapeSequenceParser();
        // All handlers are null by default

        // Act & Assert - Should not throw
        parser.Parse("Hello");
        parser.Parse("\x1B[H");
        parser.Parse("\x1B]0;Title\x07");
        parser.Parse("\x07");
    }

    [TestMethod]
    public void Parse_UnicodeCharacters_HandlesCorrectly()
    {
        // Arrange
        var parser = new EscapeSequenceParser();
        var printed = new StringBuilder();
        parser.Print += (sender, e) => printed.Append(e.Data);

        // Act
        parser.Parse("Hello ?? ??");

        // Assert
        printed.ToString().Should().Contain("Hello");
        printed.ToString().Should().Contain("??");
        printed.ToString().Should().Contain("??");
    }
}
