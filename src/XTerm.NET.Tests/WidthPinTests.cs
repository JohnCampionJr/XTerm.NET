using XTerm.Options;

namespace XTerm.Tests;

/// <summary>
/// Width pins where the Wcwidth package disagrees with observable terminal behavior — each pin
/// documents a class where package versions disagree with each other or with python wcwidth,
/// the referee ucs-detect measures every terminal against. Whatever version dependency
/// unification resolves at runtime, these answers must hold.
/// </summary>
[TestClass]
public class WidthPinTests
{
    private static int MeasuredWidth(string text)
    {
        var terminal = new Terminal(new TerminalOptions { Cols = 20, Rows = 2 });
        terminal.Write(text);
        return terminal.Buffer.X;
    }

    [TestMethod]
    [DataRow(0x0600)]   // ARABIC NUMBER SIGN
    [DataRow(0x0605)]   // ARABIC NUMBER MARK ABOVE
    [DataRow(0x06DD)]   // ARABIC END OF AYAH
    [DataRow(0x070F)]   // SYRIAC ABBREVIATION MARK
    [DataRow(0x0890)]   // ARABIC POUND MARK ABOVE
    [DataRow(0x08E2)]   // ARABIC DISPUTED END OF AYAH
    [DataRow(0x110BD)]  // KAITHI NUMBER SIGN, astral
    [DataRow(0x110CD)]  // KAITHI NUMBER SIGN ABOVE, astral
    public void A_prepended_concatenation_mark_occupies_a_column(int codePoint)
    {
        // Visible format characters: Wcwidth 3.0.0 said 1, 4.0.1 says 0, the referee says 1.
        // Width 0 standalone means the next character prints over the top of it.
        MeasuredWidth(char.ConvertFromUtf32(codePoint)).Should().Be(1);
    }

    [TestMethod]
    public void Ethiopic_HA_occupies_a_column()
    {
        // Wcwidth 4.0.1's zero table runs one past the trailing jamo and swallows U+1200.
        MeasuredWidth("\u1200").Should().Be(1);
        MeasuredWidth("\u1201").Should().Be(1);
    }
}
