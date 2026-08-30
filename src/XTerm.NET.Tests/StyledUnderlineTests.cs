using System.Runtime.CompilerServices;
using XTerm;
using XTerm.Buffer;
using XTerm.Common;
using XTerm.Options;

namespace XTerm.Tests;

/// <summary>
/// Styled underlines (SGR 4:1–4:5, 21) and underline colour (SGR 58/59).
/// </summary>
/// <remarks>
/// The squiggly underline an LSP puts under an error. The style enum and sub-parameter parsing
/// already existed — the sub-parameters were being read and then dropped, so a program asking for a
/// curly underline got a straight one.
/// </remarks>
[TestClass]
public class StyledUnderlineTests
{
    private const string Esc = "\u001b";

    private static Terminal Fresh() => new(new TerminalOptions { Cols = 20, Rows = 3 });

    private static BufferCell FirstCell(Terminal terminal)
        => terminal.Buffer.Lines[terminal.Buffer.YBase]![0];

    [TestMethod]
    [DataRow("4", UnderlineStyle.Single)]
    [DataRow("4:0", UnderlineStyle.None)]
    [DataRow("4:1", UnderlineStyle.Single)]
    [DataRow("4:2", UnderlineStyle.Double)]
    [DataRow("4:3", UnderlineStyle.Curly)]
    [DataRow("4:4", UnderlineStyle.Dotted)]
    [DataRow("4:5", UnderlineStyle.Dashed)]
    [DataRow("21", UnderlineStyle.Double)]
    public void Sgr_selects_the_underline_style(string sgr, UnderlineStyle expected)
    {
        var terminal = Fresh();
        terminal.Write($"{Esc}[{sgr}mx");

        FirstCell(terminal).Attributes.GetUnderlineStyle().Should().Be(expected);
    }

    /// <summary>
    /// A style nobody has defined is still an underline. Drawing a plain one is closer to what the
    /// program asked for than drawing nothing at all.
    /// </summary>
    [TestMethod]
    public void An_unknown_style_still_underlines()
    {
        var terminal = Fresh();
        terminal.Write($"{Esc}[4:9mx");

        FirstCell(terminal).Attributes.GetUnderlineStyle().Should().Be(UnderlineStyle.Single);
    }

    /// <summary>
    /// The style is the single source of truth, so a cell underlined by any of these reports it.
    /// Keeping a separate flag beside the style is how a cell ends up underlined by one and not
    /// the other.
    /// </summary>
    [TestMethod]
    public void IsUnderline_follows_the_style()
    {
        var terminal = Fresh();

        terminal.Write($"{Esc}[4:3mx");
        FirstCell(terminal).Attributes.IsUnderline().Should().BeTrue();

        terminal.Write($"{Esc}[24m{Esc}[1;1Hy");
        FirstCell(terminal).Attributes.IsUnderline().Should().BeFalse();
        FirstCell(terminal).Attributes.GetUnderlineStyle().Should().Be(UnderlineStyle.None);
    }

    // ---- colour ---------------------------------------------------------------------------------

    [TestMethod]
    public void Sgr58_sets_a_truecolor_underline_as_subparameters()
    {
        var terminal = Fresh();
        terminal.Write($"{Esc}[4:3;58:2::255:0:0mx");

        var attr = FirstCell(terminal).Attributes;
        attr.TryGetUnderlineColor(out var color, out var mode).Should().BeTrue();
        color.Should().Be((255 << 16) | (0 << 8) | 0);
        mode.Should().Be(1);
        attr.GetUnderlineStyle().Should().Be(UnderlineStyle.Curly);
    }

    /// <summary>
    /// Both spellings are in use, and a terminal that takes only one looks broken to half its
    /// callers.
    /// </summary>
    [TestMethod]
    public void Sgr58_also_accepts_separate_parameters()
    {
        var terminal = Fresh();
        terminal.Write($"{Esc}[58;2;0;128;255mx");

        FirstCell(terminal).Attributes.TryGetUnderlineColor(out var color, out var mode).Should().BeTrue();
        color.Should().Be((0 << 16) | (128 << 8) | 255);
        mode.Should().Be(1);
    }

    [TestMethod]
    public void Sgr58_accepts_an_indexed_colour()
    {
        var terminal = Fresh();
        terminal.Write($"{Esc}[58:5:196mx");

        FirstCell(terminal).Attributes.TryGetUnderlineColor(out var color, out var mode).Should().BeTrue();
        color.Should().Be(196);
        mode.Should().Be(0);
    }

    [TestMethod]
    public void Sgr59_puts_the_underline_back_to_the_foreground()
    {
        var terminal = Fresh();
        terminal.Write($"{Esc}[58:2::255:0:0m{Esc}[59mx");

        FirstCell(terminal).Attributes.TryGetUnderlineColor(out _, out _).Should().BeFalse();
    }

    [TestMethod]
    public void A_reset_clears_the_style_and_the_colour()
    {
        var terminal = Fresh();
        terminal.Write($"{Esc}[4:3;58:2::255:0:0m{Esc}[0mx");

        var attr = FirstCell(terminal).Attributes;
        attr.GetUnderlineStyle().Should().Be(UnderlineStyle.None);
        attr.TryGetUnderlineColor(out _, out _).Should().BeFalse();
    }

    /// <summary>
    /// The same colour used twice is one entry, which is what keeps twenty bits of id enough.
    /// </summary>
    [TestMethod]
    public void The_same_colour_interns_once()
    {
        var terminal = Fresh();

        terminal.Write($"{Esc}[58:2::10:20:30mx");
        var first = FirstCell(terminal).Attributes.GetUnderlineColorId();

        terminal.Write($"{Esc}[0m{Esc}[58:2::10:20:30m{Esc}[1;1Hy");
        var second = FirstCell(terminal).Attributes.GetUnderlineColorId();

        second.Should().Be(first);
        first.Should().NotBe(0);
    }

    // ---- an abandoned sequence must not poison the next one --------------------------------------
    //
    // Raised in review. The sub-parameter accumulator is parser-lifetime state, and nothing cleared
    // it when a sequence was abandoned rather than dispatched -- so every digit of the NEXT sequence
    // up to its first separator was swallowed into the stale sub-parameter and its first parameter
    // read as 0. Worse than a dropped sequence, because 0 means something for most of them.

    private static AttributeData AttrAt(Terminal terminal)
        => terminal.Buffer.Lines[terminal.Buffer.YBase]![0].Attributes;

    /// <summary>What a clean SGR 31 gives, to compare a poisoned one against.</summary>
    private static int RedForeground()
    {
        var terminal = Fresh();
        terminal.Write($"{Esc}[31mx");
        return AttrAt(terminal).GetFgColor();
    }

    [TestMethod]
    [DataRow("\u001b[4:3")]            // ESC begins the next sequence and abandons this one
    [DataRow("\u001b[4:3\u0018")]      // CAN
    [DataRow("\u001b[4:3\u001a")]      // SUB
    [DataRow("\u001b[4:3\u001bc")]     // RIS
    public void An_abandoned_sequence_does_not_swallow_the_next_one(string abandoned)
    {
        var terminal = Fresh();
        terminal.Write(abandoned);
        terminal.Write($"{Esc}[31mx");

        AttrAt(terminal).GetFgColor().Should().Be(RedForeground());
    }

    /// <summary>
    /// Not only SGR: a lost first parameter homes the cursor instead of moving it.
    /// </summary>
    [TestMethod]
    public void An_abandoned_sequence_does_not_swallow_a_cursor_move()
    {
        var terminal = Fresh();
        terminal.Write($"{Esc}[4:3");
        terminal.Write($"{Esc}[2;5H");
        terminal.Write("z");

        (terminal.Buffer.Lines[terminal.Buffer.YBase + 1]![4].Content).Should().Be("z");
    }

    // ---- the reason this is stored as an id ------------------------------------------------------

    /// <summary>
    /// The whole feature had to fit in bits the cell already owned.
    /// </summary>
    /// <remarks>
    /// A full RGB underline colour plus its mode is more bits than were left, and growing
    /// AttributeData grows every cell in the buffer — the thing measured as costing most on fills.
    /// So the cell carries an interned id, and this asserts the cost of the feature is zero.
    /// </remarks>
    [TestMethod]
    public void The_cell_did_not_grow()
    {
        (Unsafe.SizeOf<AttributeData>()).Should().Be(12);
        (RuntimeHelpers.IsReferenceOrContainsReferences<AttributeData>()).Should().BeFalse();
    }

    /// <summary>
    /// Style and colour live in the same int as the boolean attributes and must not disturb them.
    /// </summary>
    [TestMethod]
    public void The_style_and_colour_do_not_disturb_the_other_attributes()
    {
        var terminal = Fresh();
        terminal.Write($"{Esc}[1;3;4:3;58:2::255:0:0;9mx");

        var attr = FirstCell(terminal).Attributes;
        attr.IsBold().Should().BeTrue();
        attr.IsItalic().Should().BeTrue();
        attr.IsStrikethrough().Should().BeTrue();
        attr.GetUnderlineStyle().Should().Be(UnderlineStyle.Curly);
        attr.TryGetUnderlineColor(out _, out _).Should().BeTrue();
    }
}
