using XTerm.Options;

namespace XTerm.Tests;

/// <summary>
/// U+FFFC OBJECT REPLACEMENT CHARACTER stands in for an embedded object, and on its own it is an ordinary
/// one-column character. It shares a branch with ZWJ, which is where it keeps going wrong: ZWJ is genuinely
/// zero-width, U+FFFC is not.
///
/// <para>Measuring it as 0 does not draw it oddly — the cursor never moves, so the next character prints on
/// top of it and it disappears. That was fixed once in the width loop, and the single-character fast path
/// added later for print performance reintroduced it, because the fast path was reasoned against the loop as
/// it read before the fix. These tests pin both paths.</para>
///
/// <para>It is also the one codepoint of the 36,254 that ucs-detect expects to be narrow that this terminal
/// measured wrongly, so it is worth a test of its own.</para>
/// </summary>
[TestClass]
public class ObjectReplacementTests
{
    private const string Object = "￼";
    private const string Zwj = "‍";

    private static Terminal Write(string text)
    {
        var terminal = new Terminal(new TerminalOptions { Cols = 20, Rows = 3 });
        terminal.Write(text);
        return terminal;
    }

    /// <summary>One character, one column, cursor moved.</summary>
    [TestMethod]
    public void A_lone_object_replacement_is_one_column()
    {
        var terminal = Write(Object);
        var line = terminal.Buffer.Lines[0]!;

        line[0].Content.Should().Be(Object);
        line[0].Width.Should().Be(1);
        terminal.Buffer.X.Should().Be(1);
    }

    /// <summary>The consequence of measuring 0: whatever follows lands on top of it.</summary>
    [TestMethod]
    public void It_is_not_overwritten_by_the_next_character()
    {
        var terminal = Write(Object + "X");
        var line = terminal.Buffer.Lines[0]!;

        line[0].Content.Should().Be(Object);
        line[1].Content.Should().Be("X");
        terminal.Buffer.X.Should().Be(2);
    }

    /// <summary>Mid-line it holds its own column too, so the rest of the line keeps its alignment.</summary>
    [TestMethod]
    public void It_holds_a_column_in_the_middle_of_a_run()
    {
        var terminal = Write("AB" + Object + "CD");
        var line = terminal.Buffer.Lines[0]!;

        line[2].Content.Should().Be(Object);
        line[3].Content.Should().Be("C");
        line[4].Content.Should().Be("D");
        terminal.Buffer.X.Should().Be(5);
    }

    /// <summary>ZWJ shares the branch and is unaffected: it is zero-width whether it joins anything or not.</summary>
    [TestMethod]
    public void A_lone_zero_width_joiner_still_measures_zero()
    {
        var terminal = Write(Zwj);

        terminal.Buffer.X.Should().Be(0);
    }
}
