using XTerm.Buffer;
using XTerm.Common;
using XTerm.Options;

namespace XTerm.Tests;

/// <summary>
/// What each SGR colour form stores: the value, the raw mode int hosts have always read, and the
/// named <see cref="ColorMode"/>.
/// </summary>
/// <remarks>
/// The 16- and 256-colour palettes name the same entries below 16 but render differently -- bold
/// brightens only the first -- so the mode is the only thing that tells <c>31</c> from
/// <c>38;5;1</c>. Every form is driven through the parser rather than built by hand, so the test
/// is of what the parser actually stores.
/// </remarks>
public class SgrColorModeTests
{
    private static readonly string EscChar = ((char)0x1B).ToString();

    private static AttributeData AttributesAfter(string sgr)
    {
        var terminal = new Terminal(new TerminalOptions { Cols = 10, Rows = 2 });
        terminal.Write(Esc.Csi($"{sgr}m") + "X");
        return terminal.Buffer.Lines[terminal.Buffer.YBase]![0].Attributes;
    }

    [Theory]
    [InlineData("30", 0, 0, ColorMode.Palette16)]
    [InlineData("31", 1, 0, ColorMode.Palette16)]
    [InlineData("37", 7, 0, ColorMode.Palette16)]
    [InlineData("90", 8, 0, ColorMode.Palette16)]
    [InlineData("97", 15, 0, ColorMode.Palette16)]
    [InlineData("38;5;1", 1, 2, ColorMode.Palette256)]
    [InlineData("38;5;9", 9, 2, ColorMode.Palette256)]
    [InlineData("38;5;15", 15, 2, ColorMode.Palette256)]
    [InlineData("38;5;16", 16, 2, ColorMode.Palette256)]
    [InlineData("38;5;255", 255, 2, ColorMode.Palette256)]
    [InlineData("38:5:1", 1, 2, ColorMode.Palette256)]
    [InlineData("38:5:200", 200, 2, ColorMode.Palette256)]
    [InlineData("38;2;1;2;3", 0x010203, 1, ColorMode.RGB)]
    [InlineData("38:2::1:2:3", 0x010203, 1, ColorMode.RGB)]
    // 0x000100 is numerically 256, the default foreground's value, and is still RGB.
    [InlineData("38;2;0;1;0", 0x000100, 1, ColorMode.RGB)]
    [InlineData("31;39", 256, 0, ColorMode.Default)]
    [InlineData("38;5;1;39", 256, 0, ColorMode.Default)]
    [InlineData("0", 256, 0, ColorMode.Default)]
    public void Foreground_EachForm_StoresItsValueAndMode(string sgr, int color, int mode, ColorMode kind)
    {
        var attributes = AttributesAfter(sgr);

        Assert.Equal(color, attributes.GetFgColor());
        Assert.Equal(mode, attributes.GetFgColorMode());
        Assert.Equal(kind, attributes.GetFgColorKind());
    }

    [Theory]
    [InlineData("40", 0, 0, ColorMode.Palette16)]
    [InlineData("41", 1, 0, ColorMode.Palette16)]
    [InlineData("47", 7, 0, ColorMode.Palette16)]
    [InlineData("100", 8, 0, ColorMode.Palette16)]
    [InlineData("107", 15, 0, ColorMode.Palette16)]
    [InlineData("48;5;1", 1, 2, ColorMode.Palette256)]
    [InlineData("48;5;9", 9, 2, ColorMode.Palette256)]
    [InlineData("48;5;16", 16, 2, ColorMode.Palette256)]
    [InlineData("48;5;255", 255, 2, ColorMode.Palette256)]
    [InlineData("48:5:1", 1, 2, ColorMode.Palette256)]
    [InlineData("48;2;1;2;3", 0x010203, 1, ColorMode.RGB)]
    [InlineData("48:2::1:2:3", 0x010203, 1, ColorMode.RGB)]
    // 0x000101 is numerically 257, the default background's value, and is still RGB.
    [InlineData("48;2;0;1;1", 0x000101, 1, ColorMode.RGB)]
    [InlineData("41;49", 257, 0, ColorMode.Default)]
    [InlineData("48;5;1;49", 257, 0, ColorMode.Default)]
    [InlineData("0", 257, 0, ColorMode.Default)]
    public void Background_EachForm_StoresItsValueAndMode(string sgr, int color, int mode, ColorMode kind)
    {
        var attributes = AttributesAfter(sgr);

        Assert.Equal(color, attributes.GetBgColor());
        Assert.Equal(mode, attributes.GetBgColorMode());
        Assert.Equal(kind, attributes.GetBgColorKind());
    }

    [Fact]
    public void ColorMode_StoredValues_AreTheRawModeInts()
    {
        // Hosts that read GetFgColorMode() as an int keep working for these three; the cast is
        // exact in both directions.
        Assert.Equal(0, (int)ColorMode.Palette16);
        Assert.Equal(1, (int)ColorMode.RGB);
        Assert.Equal(2, (int)ColorMode.Palette256);
    }

    [Fact]
    public void Sixteen_And256Palette_SameIndex_AreDifferentAttributes()
    {
        // The equality a renderer coalesces runs on must split them: bold draws them differently.
        Assert.NotEqual(AttributesAfter("31"), AttributesAfter("38;5;1"));
        Assert.NotEqual(AttributesAfter("41"), AttributesAfter("48;5;1"));
        Assert.Equal(AttributesAfter("38;5;1"), AttributesAfter("38:5:1"));
    }

    [Theory]
    [InlineData("1;38;5;1;48;5;2")]
    [InlineData("1;31;42")]
    [InlineData("1;38;5;9;48;5;15")]
    [InlineData("1;91;48;2;0;1;1")]
    public void Decrqss_ReplaysToTheSameAttributes(string sgr)
    {
        var source = new Terminal(new TerminalOptions { Cols = 10, Rows = 2 });
        source.Write(Esc.Csi($"{sgr}m") + "X");
        var reply = RequestSgr(source);

        var replayed = new Terminal(new TerminalOptions { Cols = 10, Rows = 2 });
        replayed.Write(Esc.Csi($"{reply}m") + "X");

        Assert.Equal(
            source.Buffer.Lines[source.Buffer.YBase]![0].Attributes,
            replayed.Buffer.Lines[replayed.Buffer.YBase]![0].Attributes);
    }

    private static string RequestSgr(Terminal terminal)
    {
        var reply = new System.Text.StringBuilder();
        terminal.DataReceived += (_, e) => reply.Append(e.Data);
        terminal.Write($"{EscChar}P$qm{Esc.St}");

        var prefix = $"{EscChar}P1$r";
        var suffix = $"m{Esc.St}";
        var text = reply.ToString();
        Assert.StartsWith(prefix, text);
        Assert.EndsWith(suffix, text);
        return text[prefix.Length..^suffix.Length];
    }
}
