using XTerm.Options;

namespace XTerm.Tests;

/// <summary>
/// Emoji tag sequences: a base flag followed by plane-14 TAG characters and CANCEL TAG — the
/// subdivision flags of England, Scotland and Wales. The tags are format characters spelling out
/// a region code; they extend the flag's cluster and occupy no columns. ucs-detect measured
/// 🏴gbsct at eight columns because each tag advanced the cursor by its table width, and which
/// width that was depended on the Wcwidth version the HOST happened to resolve.
/// </summary>
[TestClass]
public class EmojiTagSequenceTests
{
    private const string BlackFlag = "🏴";
    private const string CancelTag = "󠁿";

    private static string Subdivision(string code)
    {
        var tags = string.Concat(code.Select(c => char.ConvertFromUtf32(0xE0000 + c)));
        return BlackFlag + tags + CancelTag;
    }

    [TestMethod]
    [DataRow("gbeng")]
    [DataRow("gbsct")]
    [DataRow("gbwls")]
    public void A_subdivision_flag_is_one_cluster_two_columns(string code)
    {
        var terminal = new Terminal(new TerminalOptions { Cols = 20, Rows = 3 });
        terminal.Write(Subdivision(code));

        terminal.Buffer.X.Should().Be(2);
        var line = terminal.Buffer.Lines[0]!;
        line[0].Width.Should().Be(2);
        line[0].Content.Should().Be(Subdivision(code));
    }

    [TestMethod]
    public void What_follows_the_flag_lands_beside_it()
    {
        var terminal = new Terminal(new TerminalOptions { Cols = 20, Rows = 3 });
        terminal.Write(Subdivision("gbsct") + "X");

        (terminal.Buffer.Lines[0]![2].Content).Should().Be("X");
        terminal.Buffer.X.Should().Be(3);
    }

    [TestMethod]
    public void A_lone_tag_character_is_zero_width()
    {
        // Like a lone ZWJ: a format character with nothing to decorate occupies nothing.
        var terminal = new Terminal(new TerminalOptions { Cols = 20, Rows = 3 });
        terminal.Write("󠁧");

        terminal.Buffer.X.Should().Be(0);
    }
}
