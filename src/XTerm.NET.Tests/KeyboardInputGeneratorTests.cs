using XTerm;
using XTerm.Input;
using XTerm.Options;

namespace XTerm.Tests;

[TestClass]

public class KeyboardInputGeneratorTests
{
    [TestMethod]
    public void ArrowWithoutModifiers_UsesStandardCsi()
    {
        var terminal = new Terminal(new TerminalOptions());

        var sequence = terminal.GenerateKeyInput(Key.LeftArrow, KeyModifiers.None);

        sequence.Should().Be("\u001b[D");
    }

    [TestMethod]
    public void ArrowWithAltControl_EncodesModifierCode()
    {
        var terminal = new Terminal(new TerminalOptions());

        var sequence = terminal.GenerateKeyInput(Key.UpArrow, KeyModifiers.Alt | KeyModifiers.Control);

        sequence.Should().Be("\u001b[1;7A");
    }

    [TestMethod]
    public void CharWithAltAndControl_PrefixesEscAndControlCode()
    {
        var terminal = new Terminal(new TerminalOptions());

        var sequence = terminal.GenerateCharInput('a', KeyModifiers.Control | KeyModifiers.Alt);

        sequence.Should().Be("\u001b\u0001");
    }
}
