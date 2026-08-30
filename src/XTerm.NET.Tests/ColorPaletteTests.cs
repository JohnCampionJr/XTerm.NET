using System.Threading;
using System.Threading.Tasks;
using XTerm;
using XTerm.Common;
using XTerm.Options;

namespace XTerm.Tests;

/// <summary>
/// Covers OSC 4 (indexed palette), OSC 10/11/12 (foreground, background, cursor), and the
/// OSC 104/110/111/112 resets.
/// </summary>
[TestClass]
public class ColorPaletteTests
{
    private Terminal CreateTerminal(ThemeOptions? theme = null)
    {
        var options = new TerminalOptions { Cols = 80, Rows = 24 };
        if (theme is not null)
        {
            options.Theme = theme;
        }

        return new Terminal(options);
    }

    private static List<string> CaptureReplies(Terminal terminal)
    {
        var replies = new List<string>();
        terminal.DataReceived += (_, e) => replies.Add(e.Data);
        return replies;
    }

    // ---- defaults ----------------------------------------------------------------------------

    [TestMethod]
    public void Defaults_MatchXtermAnsiColors()
    {
        var terminal = CreateTerminal();

        terminal.Colors[0].Should().Be(0x000000);
        terminal.Colors[1].Should().Be(0xCD0000);
        terminal.Colors[15].Should().Be(0xFFFFFF);
    }

    [TestMethod]
    public void Defaults_ComputeThe216ColorCube()
    {
        var terminal = CreateTerminal();

        terminal.Colors[16].Should().Be(0x000000);   // cube origin
        terminal.Colors[231].Should().Be(0xFFFFFF);  // cube far corner
        terminal.Colors[25].Should().Be(0x005FAF);   // r=0 g=1 b=3 -> 0,95,175
    }

    [TestMethod]
    public void Defaults_ComputeTheGrayscaleRamp()
    {
        var terminal = CreateTerminal();

        terminal.Colors[232].Should().Be(0x080808);
        terminal.Colors[255].Should().Be(0xEEEEEE);
    }

    [TestMethod]
    public void Defaults_PreserveThePreviousQueryAnswers_WhenNoThemeIsSet()
    {
        // This change must not move colours for an embedder that sets no theme; it only stops the
        // answers being constants.
        var terminal = CreateTerminal();

        terminal.Colors.Foreground.Should().Be(0xFFFFFF);
        terminal.Colors.Background.Should().Be(0x000000);
        terminal.Colors.Cursor.Should().Be(0xFFFFFF);
    }

    // ---- the light background case ------------------------------------------------------------

    [TestMethod]
    public void Theme_SetsTheBackground_AndTheQueryReportsIt()
    {
        // The reason this PR exists. A program asks what the background is before choosing its own
        // colours; a constant reply of black made every one of them render for a dark terminal.
        var terminal = CreateTerminal(new ThemeOptions { Background = "#ffffff", Foreground = "#000000" });
        var replies = CaptureReplies(terminal);

        terminal.Write("\x1B]11;?\x07");

        replies.Should().ContainSingle().Which.Should().Be("\u001b]11;rgb:ffff/ffff/ffff\u0007");
    }

    [TestMethod]
    public void IsLightBackground_FollowsTheTheme()
    {
        (CreateTerminal(new ThemeOptions { Background = "#ffffff" }).Colors.IsLightBackground).Should().BeTrue();
        (CreateTerminal(new ThemeOptions { Background = "#000000" }).Colors.IsLightBackground).Should().BeFalse();

        // Luma-weighted rather than averaged: pure blue is dark despite a high channel value.
        (CreateTerminal(new ThemeOptions { Background = "#0000ff" }).Colors.IsLightBackground).Should().BeFalse();
    }

    [TestMethod]
    public void ApplyTheme_ReseedsAtRuntime_ForAnOsThemeFlip()
    {
        var terminal = CreateTerminal(new ThemeOptions { Background = "#000000" });
        terminal.Colors.IsLightBackground.Should().BeFalse();

        terminal.Colors.ApplyTheme(new ThemeOptions { Background = "#ffffff", Foreground = "#000000" });

        terminal.Colors.IsLightBackground.Should().BeTrue();
        terminal.Colors.Foreground.Should().Be(0x000000);
    }

    [TestMethod]
    public void Reset_RestoresTheEmbedderTheme_NotAFactoryDarkPalette()
    {
        // The failure this guards: a program sets colours, then resets, and a light terminal is
        // left black because "reset" meant xterm's defaults rather than the configured theme.
        var terminal = CreateTerminal(new ThemeOptions { Background = "#ffffff", Black = "#eeeeee" });

        terminal.Write("\x1B]11;rgb:00/00/00\x07");
        terminal.Write("\x1B]4;0;#123456\x07");
        terminal.Colors.Background.Should().Be(0x000000);

        terminal.Write("\x1B]111\x07");
        terminal.Write("\x1B]104\x07");

        terminal.Colors.Background.Should().Be(0xFFFFFF);
        terminal.Colors[0].Should().Be(0xEEEEEE);
    }

    [TestMethod]
    public void Theme_OverridesAnsiSlots()
    {
        var terminal = CreateTerminal(new ThemeOptions { Red = "#ff8800", BrightWhite = "rgb:11/22/33" });

        terminal.Colors[1].Should().Be(0xFF8800);
        terminal.Colors[15].Should().Be(0x112233);
    }

    // ---- OSC 4 -------------------------------------------------------------------------------

    [TestMethod]
    public void Osc4_SetsAnIndexedColor()
    {
        var terminal = CreateTerminal();

        terminal.Write("\x1B]4;1;rgb:ff/00/00\x07");

        terminal.Colors[1].Should().Be(0xFF0000);
    }

    [TestMethod]
    public void Osc4_SetsMultiplePairsInOneSequence()
    {
        // Theme scripts send all sixteen at once rather than as sixteen sequences.
        var terminal = CreateTerminal();

        terminal.Write("\x1B]4;1;#ff0000;2;#00ff00;3;#0000ff\x07");

        terminal.Colors[1].Should().Be(0xFF0000);
        terminal.Colors[2].Should().Be(0x00FF00);
        terminal.Colors[3].Should().Be(0x0000FF);
    }

    [TestMethod]
    public void Osc4_QueriesAnIndexedColor()
    {
        var terminal = CreateTerminal();
        var replies = CaptureReplies(terminal);

        terminal.Write("\x1B]4;1;?\x07");

        replies.Should().ContainSingle().Which.Should().Be("\u001b]4;1;rgb:cdcd/0000/0000\u0007");
    }

    [TestMethod]
    public void Osc4_QueryReflectsAPriorSet()
    {
        var terminal = CreateTerminal();
        terminal.Write("\x1B]4;5;#010203\x07");
        var replies = CaptureReplies(terminal);

        terminal.Write("\x1B]4;5;?\x07");

        replies.Should().ContainSingle().Which.Should().Be("\u001b]4;5;rgb:0101/0202/0303\u0007");
    }

    [TestMethod]
    public void Osc4_IgnoresOutOfRangeAndMalformedEntries()
    {
        var terminal = CreateTerminal();

        terminal.Write("\x1B]4;999;#ffffff\x07");
        terminal.Write("\x1B]4;1;notacolor\x07");

        terminal.Colors[1].Should().Be(0xCD0000);
    }

    [TestMethod]
    public void Osc104_ResetsASingleIndex()
    {
        var terminal = CreateTerminal();
        terminal.Write("\x1B]4;1;#ffffff\x07");
        terminal.Write("\x1B]4;2;#ffffff\x07");

        terminal.Write("\x1B]104;1\x07");

        terminal.Colors[1].Should().Be(0xCD0000);
        terminal.Colors[2].Should().Be(0xFFFFFF);
    }

    // ---- OSC 10/11/12 ------------------------------------------------------------------------

    [TestMethod]
    public void Osc10_SetsForeground()
    {
        var terminal = CreateTerminal();

        terminal.Write("\x1B]10;#abcdef\x07");

        terminal.Colors.Foreground.Should().Be(0xABCDEF);
    }

    [TestMethod]
    public void Osc10_WithMultipleSpecs_AdvancesThroughResources()
    {
        // xterm defines OSC 10 ; fg ; bg as setting both; handling only the first drops the
        // background silently.
        var terminal = CreateTerminal();

        terminal.Write("\x1B]10;#111111;#222222;#333333\x07");

        terminal.Colors.Foreground.Should().Be(0x111111);
        terminal.Colors.Background.Should().Be(0x222222);
        terminal.Colors.Cursor.Should().Be(0x333333);
    }

    [TestMethod]
    public void Osc12_SetsCursor()
    {
        var terminal = CreateTerminal();

        terminal.Write("\x1B]12;red\x07");

        terminal.Colors.Cursor.Should().Be(0xFF0000);
    }

    [TestMethod]
    [DataRow("110", 0xFFFFFF)]
    [DataRow("111", 0x000000)]
    [DataRow("112", 0xFFFFFF)]
    public void Osc110To112_ResetTheirOwnResource(string code, int expected)
    {
        var terminal = CreateTerminal();
        terminal.Write("\x1B]10;#123456\x07");
        terminal.Write("\x1B]11;#123456\x07");
        terminal.Write("\x1B]12;#123456\x07");

        terminal.Write($"\x1B]{code}\x07");

        var actual = code switch
        {
            "110" => terminal.Colors.Foreground,
            "111" => terminal.Colors.Background,
            _ => terminal.Colors.Cursor,
        };
        actual.Should().Be(expected);
    }

    [TestMethod]
    public void ColorChanged_FiresForSetsAndNotForNoOps()
    {
        var terminal = CreateTerminal();
        var changes = new List<ColorChangedEventArgs>();
        terminal.Colors.ColorChanged += (_, e) => changes.Add(e);

        terminal.Write("\x1B]4;1;#ff0000\x07");
        terminal.Write("\x1B]4;1;#ff0000\x07");   // same value, no repaint needed

        var change = changes.Should().ContainSingle().Which;
        change.Target.Should().Be(ColorTarget.Indexed);
        change.Index.Should().Be(1);
        change.Rgb.Should().Be(0xFF0000);
    }

    // ---- no-op suppression and index handling ------------------------------------------------

    [TestMethod]
    public void ResetAllColors_IsSilent_WhenNothingHasChanged()
    {
        // Every other setter here suppresses a no-op. A bare OSC 104 on an untouched palette used to
        // fire anyway, telling a renderer to repaint for a change that did not happen.
        var terminal = CreateTerminal();
        var changes = new List<ColorChangedEventArgs>();
        terminal.Colors.ColorChanged += (_, e) => changes.Add(e);

        terminal.Write("\u001b]104\u0007");

        changes.Should().BeEmpty();
    }

    [TestMethod]
    public void ResetAllColors_StillFires_WhenSomethingHadChanged()
    {
        var terminal = CreateTerminal();
        terminal.Write("\u001b]4;1;#ff0000\u0007");

        var changes = new List<ColorChangedEventArgs>();
        terminal.Colors.ColorChanged += (_, e) => changes.Add(e);

        terminal.Write("\u001b]104\u0007");

        var change = changes.Should().ContainSingle().Which;
        change.Target.Should().Be(ColorTarget.Indexed);
        terminal.Colors[1].Should().Be(0xCD0000);
    }

    [TestMethod]
    public void ApplyTheme_IsSilent_WhenTheThemeIsUnchanged()
    {
        var terminal = CreateTerminal(new ThemeOptions { Background = "#ffffff" });
        var changes = new List<ColorChangedEventArgs>();
        terminal.Colors.ColorChanged += (_, e) => changes.Add(e);

        terminal.Colors.ApplyTheme(new ThemeOptions { Background = "#ffffff" });

        changes.Should().BeEmpty();
    }

    [TestMethod]
    public void ApplyTheme_FiresOnceForARealChange()
    {
        var terminal = CreateTerminal(new ThemeOptions { Background = "#000000" });
        var changes = new List<ColorChangedEventArgs>();
        terminal.Colors.ColorChanged += (_, e) => changes.Add(e);

        terminal.Colors.ApplyTheme(new ThemeOptions { Background = "#ffffff" });

        changes.Should().ContainSingle().Which.Target.Should().Be(ColorTarget.All);
    }

    [TestMethod]
    [DataRow(-1)]
    [DataRow(256)]
    [DataRow(999)]
    public void PaletteIndex_OutOfRange_Throws(int index)
    {
        // Not clamped. Clamping made SetColor(999, ...) quietly rewrite entry 255 and the indexer
        // answer for entry 0 when asked about -1: a plausible wrong answer where there should have
        // been none.
        var terminal = CreateTerminal();

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => terminal.Colors[index]);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => terminal.Colors.SetColor(index, 0x123456));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => terminal.Colors.ResetColor(index));
    }

    [TestMethod]
    public void PaletteIndex_OutOfRangeOverOsc_IsStillIgnoredRatherThanThrown()
    {
        // The parser must not surface a malformed sequence as an exception; InputHandler range-checks
        // before it reaches the palette, and that has to keep working now the palette throws.
        var terminal = CreateTerminal();

        var ex = Record.Exception(() => terminal.Write("\u001b]4;999;#ffffff\u0007"));

        ex.Should().BeNull();
        terminal.Colors[255].Should().Be(0xEEEEEE);
    }

    // ---- concurrency -------------------------------------------------------------------------

    [TestMethod]
    public async Task ApplyTheme_IsNeverObservedHalfApplied()
    {
        // The bug this pins: ApplyTheme used to Array.Copy into the live array, and Array.Copy is not
        // atomic, so a renderer scanning the palette could paint a frame that was half one theme and
        // half the other. A reference swap has no middle.
        var dark = new ThemeOptions
        {
            Black = "#000000", Red = "#110000", Green = "#001100", Yellow = "#111100",
            Blue = "#000011", Magenta = "#110011", Cyan = "#001111", White = "#111111",
        };
        var light = new ThemeOptions
        {
            Black = "#ffffff", Red = "#ff0000", Green = "#00ff00", Yellow = "#ffff00",
            Blue = "#0000ff", Magenta = "#ff00ff", Cyan = "#00ffff", White = "#ffffff",
        };

        var terminal = CreateTerminal(dark);
        int[] darkAnsi = ReadAnsi(terminal);
        terminal.Colors.ApplyTheme(light);
        int[] lightAnsi = ReadAnsi(terminal);
        lightAnsi.Should().NotEqual(darkAnsi);

        using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var mixed = 0;

        var reader = Task.Run(() =>
        {
            while (!stop.IsCancellationRequested)
            {
                int[] seen = ReadAnsi(terminal);
                if (!seen.SequenceEqual(darkAnsi) && !seen.SequenceEqual(lightAnsi))
                {
                    Interlocked.Increment(ref mixed);
                }
            }
        });

        var writer = Task.Run(() =>
        {
            var toggle = false;
            while (!stop.IsCancellationRequested)
            {
                terminal.Colors.ApplyTheme(toggle ? dark : light);
                toggle = !toggle;
            }
        });

        await Task.WhenAll(reader, writer);

        Volatile.Read(ref mixed).Should().Be(0);
    }

    /// <summary>
    /// Reads the first eight ANSI colours through ONE snapshot.
    /// </summary>
    /// <remarks>
    /// Eight calls to the indexer would each take their own snapshot and could straddle a theme
    /// change, which is the whole reason Take exists.
    /// </remarks>
    private static int[] ReadAnsi(Terminal terminal)
    {
        ColorSnapshot snapshot = terminal.Colors.Take();
        var values = new int[8];
        for (var i = 0; i < values.Length; i++)
        {
            values[i] = snapshot[i];
        }

        return values;
    }

    [TestMethod]
    public void Take_ReturnsAViewThatDoesNotMoveAfterwards()
    {
        // The property that lets a renderer trust a snapshot for a whole frame. Writes copy on
        // write for exactly this; mutating the live array in place would have been safe for the
        // VALUE and still broken this.
        var terminal = CreateTerminal();
        ColorSnapshot before = terminal.Colors.Take();
        int original = before[1];

        terminal.Write("\u001b]4;1;#123456\u0007");

        before[1].Should().Be(original);
        terminal.Colors[1].Should().Be(0x123456);
        (terminal.Colors.Take()[1]).Should().Be(0x123456);
    }

    // ---- colour spec parsing -----------------------------------------------------------------

    [TestMethod]
    [DataRow("rgb:ff/00/00", 0xFF0000)]
    [DataRow("rgb:f/0/0", 0xFF0000)]              // 1 digit: f is FULL intensity, not 0x0f
    [DataRow("rgb:ffff/0000/0000", 0xFF0000)]     // 4 digits, as emitted by queries
    [DataRow("#ff0000", 0xFF0000)]
    [DataRow("#f00", 0xFF0000)]
    [DataRow("#ffff00000000", 0xFF0000)]
    [DataRow("red", 0xFF0000)]
    [DataRow("RED", 0xFF0000)]
    public void ColorSpec_ParsesTheFormsProgramsActuallySend(string spec, int expected)
    {
        ColorSpec.TryParse(spec, out var rgb).Should().BeTrue();
        rgb.Should().Be(expected);
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("   ")]
    [DataRow(null)]
    [DataRow("rgb:ff/00")]
    [DataRow("rgb:gg/00/00")]
    [DataRow("#ff00")]
    [DataRow("chartreuse")]
    public void ColorSpec_RejectsWhatItCannotRead(string? spec)
    {
        ColorSpec.TryParse(spec, out _).Should().BeFalse();
    }

    [TestMethod]
    public void ColorSpec_FormatWidensChannelsByRepetition()
    {
        // 0xff must become 0xffff, not 0xff00: full intensity has to survive the widening.
        ColorSpec.Format(0xFF0080).Should().Be("rgb:ffff/0000/8080");
    }

    [TestMethod]
    public void ColorSpec_RoundTripsThroughFormatAndParse()
    {
        ColorSpec.TryParse(ColorSpec.Format(0x123456), out var rgb).Should().BeTrue();
        rgb.Should().Be(0x123456);
    }
}
