using XTerm.Options;
using XTerm.Common;

namespace XTerm.Tests.Options;

[TestClass]

public class TerminalOptionsTests
{
    [TestMethod]
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var options = new TerminalOptions();

        // Assert
        options.Cols.Should().Be(80);
        options.Rows.Should().Be(24);
        options.Scrollback.Should().Be(1000);
        options.TabStopWidth.Should().Be(8);
        options.BellSound.Should().BeFalse();
        options.BellVolume.Should().Be(0.5);
        options.BellStyle.Should().Be(BellStyle.None);
        options.CursorBlinkRate.Should().Be(530);
        options.CursorStyle.Should().Be(CursorStyle.Block);
        options.CursorBlink.Should().BeFalse();
        options.FontFamily.Should().Be("monospace");
        options.FontSize.Should().Be(15);
        options.FontWeight.Should().Be("normal");
        options.FontWeightBold.Should().Be("bold");
        options.LetterSpacing.Should().Be(0);
        options.LineHeight.Should().Be(1.0);
        options.Wraparound.Should().BeTrue();
        options.ConvertEol.Should().BeFalse();
        options.TermName.Should().Be("xterm");
        options.FastScrollModifier.Should().BeFalse();
        options.ScrollSensitivity.Should().Be(1);
        options.AllowTransparency.Should().BeFalse();
        options.MacOptionIsMeta.Should().BeFalse();
        options.RightClickSelectsWord.Should().BeTrue();
        options.RendererType.Should().Be(RendererType.Canvas);
        options.ClipboardWriteEnabled.Should().BeTrue();
        options.ClipboardReadEnabled.Should().BeFalse();
        options.WindowOptions.Should().NotBeNull();
        options.Theme.Should().NotBeNull();
        options.MinimumContrastRatio.Should().Be(1);
        options.DrawBoldTextInBrightColors.Should().BeTrue();
        options.KittyNotificationsEnabled.Should().BeTrue();   // display-only: on by default, like every terminal that implements OSC 99
        options.CustomKeyEventHandler.Should().BeNull();
        options.ClipboardWriteEnabled.Should().BeTrue();
        options.ClipboardReadEnabled.Should().BeFalse();
        options.MaxClipboardBytes.Should().Be(64 * 1024 * 1024);
    }

    [TestMethod]
    public void Cols_CanBeSet()
    {
        // Arrange
        var options = new TerminalOptions();

        // Act
        options.Cols = 120;

        // Assert
        options.Cols.Should().Be(120);
    }

    [TestMethod]
    public void Rows_CanBeSet()
    {
        // Arrange
        var options = new TerminalOptions();

        // Act
        options.Rows = 40;

        // Assert
        options.Rows.Should().Be(40);
    }

    [TestMethod]
    public void Scrollback_CanBeSet()
    {
        // Arrange
        var options = new TerminalOptions();

        // Act
        options.Scrollback = 5000;

        // Assert
        options.Scrollback.Should().Be(5000);
    }

    [TestMethod]
    public void BellSound_CanBeToggled()
    {
        // Arrange
        var options = new TerminalOptions();

        // Act
        options.BellSound = true;

        // Assert
        options.BellSound.Should().BeTrue();
    }

    [TestMethod]
    public void CursorStyle_CanBeChanged()
    {
        // Arrange
        var options = new TerminalOptions();

        // Act
        options.CursorStyle = CursorStyle.Bar;

        // Assert
        options.CursorStyle.Should().Be(CursorStyle.Bar);
    }

    [TestMethod]
    public void CursorBlink_CanBeToggled()
    {
        // Arrange
        var options = new TerminalOptions();

        // Act
        options.CursorBlink = true;

        // Assert
        options.CursorBlink.Should().BeTrue();
    }

    [TestMethod]
    public void FontFamily_CanBeSet()
    {
        // Arrange
        var options = new TerminalOptions();

        // Act
        options.FontFamily = "Courier New";

        // Assert
        options.FontFamily.Should().Be("Courier New");
    }

    [TestMethod]
    public void FontSize_CanBeSet()
    {
        // Arrange
        var options = new TerminalOptions();

        // Act
        options.FontSize = 20;

        // Assert
        options.FontSize.Should().Be(20);
    }

    [TestMethod]
    public void Wraparound_CanBeToggled()
    {
        // Arrange
        var options = new TerminalOptions();

        // Act
        options.Wraparound = false;

        // Assert
        options.Wraparound.Should().BeFalse();
    }

    [TestMethod]
    public void Clone_CreatesIndependentCopy()
    {
        // Arrange
        var options = new TerminalOptions
        {
            Cols = 100,
            Rows = 30,
            Scrollback = 2000,
            BellSound = true,
            CursorBlink = true,
            FontFamily = "Test Font",
            ClipboardWriteEnabled = false,
            ClipboardReadEnabled = true
        };

        // Act
        var clone = options.Clone();

        // Assert
        clone.Cols.Should().Be(options.Cols);
        clone.Rows.Should().Be(options.Rows);
        clone.Scrollback.Should().Be(options.Scrollback);
        clone.BellSound.Should().Be(options.BellSound);
        clone.CursorBlink.Should().Be(options.CursorBlink);
        clone.FontFamily.Should().Be(options.FontFamily);
        clone.ClipboardWriteEnabled.Should().Be(options.ClipboardWriteEnabled);
        clone.ClipboardReadEnabled.Should().Be(options.ClipboardReadEnabled);

        // Verify independence
        clone.Cols = 120;
        options.Cols.Should().Be(100);
        clone.Cols.Should().Be(120);
    }

    [TestMethod]
    public void CustomKeyEventHandler_CanBeSet()
    {
        // Arrange
        var options = new TerminalOptions();
        Func<KeyEvent, bool> handler = (e) => true;

        // Act
        options.CustomKeyEventHandler = handler;

        // Assert
        options.CustomKeyEventHandler.Should().NotBeNull();
        options.CustomKeyEventHandler.Should().Be(handler);
    }

    [TestMethod]
    [DataRow(BellStyle.None)]
    [DataRow(BellStyle.Sound)]
    [DataRow(BellStyle.Visual)]
    [DataRow(BellStyle.Both)]
    public void BellStyle_CanBeSet(BellStyle style)
    {
        // Arrange
        var options = new TerminalOptions();

        // Act
        options.BellStyle = style;

        // Assert
        options.BellStyle.Should().Be(style);
    }

    [TestMethod]
    [DataRow(RendererType.Canvas)]
    [DataRow(RendererType.Dom)]
    [DataRow(RendererType.WebGL)]
    public void RendererType_CanBeSet(RendererType type)
    {
        // Arrange
        var options = new TerminalOptions();

        // Act
        options.RendererType = type;

        // Assert
        options.RendererType.Should().Be(type);
    }

    [TestMethod]
    public void AllProperties_CanBeModified()
    {
        // Arrange
        var options = new TerminalOptions();

        // Act
        options.Cols = 100;
        options.Rows = 30;
        options.Scrollback = 2000;
        options.TabStopWidth = 4;
        options.BellSound = true;
        options.BellVolume = 0.8;
        options.BellStyle = BellStyle.Both;
        options.CursorBlinkRate = 600;
        options.CursorStyle = CursorStyle.Underline;
        options.CursorBlink = true;
        options.FontFamily = "Arial";
        options.FontSize = 18;
        options.FontWeight = "600";
        options.FontWeightBold = "800";
        options.LetterSpacing = 1.5;
        options.LineHeight = 1.2;
        options.Wraparound = false;
        options.ConvertEol = true;
        options.TermName = "xterm-256color";
        options.FastScrollModifier = true;
        options.ScrollSensitivity = 3;
        options.AllowTransparency = true;
        options.MacOptionIsMeta = true;
        options.RightClickSelectsWord = false;
        options.RendererType = RendererType.WebGL;
        options.MinimumContrastRatio = 4.5;
        options.DrawBoldTextInBrightColors = false;
        options.KittyNotificationsEnabled = true;

        // Assert
        options.Cols.Should().Be(100);
        options.Rows.Should().Be(30);
        options.Scrollback.Should().Be(2000);
        options.TabStopWidth.Should().Be(4);
        options.BellSound.Should().BeTrue();
        options.BellVolume.Should().Be(0.8);
        options.BellStyle.Should().Be(BellStyle.Both);
        options.CursorBlinkRate.Should().Be(600);
        options.CursorStyle.Should().Be(CursorStyle.Underline);
        options.CursorBlink.Should().BeTrue();
        options.FontFamily.Should().Be("Arial");
        options.FontSize.Should().Be(18);
        options.FontWeight.Should().Be("600");
        options.FontWeightBold.Should().Be("800");
        options.LetterSpacing.Should().Be(1.5);
        options.LineHeight.Should().Be(1.2);
        options.Wraparound.Should().BeFalse();
        options.ConvertEol.Should().BeTrue();
        options.TermName.Should().Be("xterm-256color");
        options.FastScrollModifier.Should().BeTrue();
        options.ScrollSensitivity.Should().Be(3);
        options.AllowTransparency.Should().BeTrue();
        options.MacOptionIsMeta.Should().BeTrue();
        options.RightClickSelectsWord.Should().BeFalse();
        options.RendererType.Should().Be(RendererType.WebGL);
        options.MinimumContrastRatio.Should().Be(4.5);
        options.DrawBoldTextInBrightColors.Should().BeFalse();
        options.KittyNotificationsEnabled.Should().BeTrue();
    }

    [TestMethod]
    public void Clone_CopiesEverySettableProperty()
    {
        var options = new TerminalOptions();
        SetDistinctValues(options);

        var clone = options.Clone();

        AssertPropertiesEqual(options, clone);
        clone.WindowOptions.Should().NotBeSameAs(options.WindowOptions);
        clone.Theme.Should().NotBeSameAs(options.Theme);
        AssertPropertiesEqual(options.WindowOptions, clone.WindowOptions);
        AssertPropertiesEqual(options.Theme, clone.Theme);
    }

    private static void SetDistinctValues(object target)
    {
        foreach (var property in target.GetType().GetProperties().Where(p => p.CanRead && p.CanWrite))
        {
            var current = property.GetValue(target);
            object? value = property.PropertyType switch
            {
                var type when type == typeof(bool) => !(bool)current!,
                var type when type == typeof(int) => (int)current! + 1,
                var type when type == typeof(long) => (long)current! + 1,
                var type when type == typeof(double) => (double)current! + 0.5,
                var type when type == typeof(string) => (current as string ?? string.Empty) + "-clone",
                var type when type.IsEnum => Enum.GetValues(type).Cast<object>()
                    .First(value => !Equals(value, current)),
                var type when type == typeof(Func<KeyEvent, bool>) => (Func<KeyEvent, bool>)(_ => true),

                // The nested option objects are varied by the recursion below, not here: replacing
                // the reference would defeat the NotSame checks the caller makes on them.
                var type when type == typeof(WindowOptions) || type == typeof(ThemeOptions) => current,

                // Anything else stops the test rather than passing it. A type this cannot vary gets
                // set to the value it already had, so the clone would be compared default against
                // default and agree whether or not the copy constructor ever touched it -- the
                // guard reporting success at exactly the moment it stopped guarding. The property
                // most likely to be forgotten is the one added next, which is also when a new type
                // is most likely to appear, so the two failures would arrive together and cancel.
                _ => throw new NotSupportedException(
                    $"{target.GetType().Name}.{property.Name} is a {property.PropertyType.Name}; "
                    + "teach SetDistinctValues how to vary it, or this guard silently stops "
                    + "checking that property.")
            };

            property.SetValue(target, value);
        }

        if (target is TerminalOptions options)
        {
            SetDistinctValues(options.WindowOptions);
            SetDistinctValues(options.Theme);
        }
    }

    private static void AssertPropertiesEqual(object expected, object actual)
    {
        foreach (var property in expected.GetType().GetProperties().Where(p => p.CanRead && p.CanWrite))
        {
            if (property.PropertyType is { } type
                && (type == typeof(WindowOptions) || type == typeof(ThemeOptions)))
            {
                continue;
            }

            property.GetValue(actual).Should().Be(property.GetValue(expected));
        }
    }
}

[TestClass]

public class WindowOptionsTests
{
    [TestMethod]
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var options = new WindowOptions();

        // Assert
        options.GetWinPosition.Should().BeFalse();
        options.GetWinSizePixels.Should().BeFalse();
        options.GetWinSizeChars.Should().BeFalse();
        options.GetScreenSizePixels.Should().BeFalse();
        options.GetCellSizePixels.Should().BeFalse();
        options.GetIconTitle.Should().BeFalse();
        options.GetWinTitle.Should().BeFalse();
        options.GetWinState.Should().BeFalse();
        options.SetWinPosition.Should().BeFalse();
        options.SetWinSizePixels.Should().BeFalse();
        options.SetWinSizeChars.Should().BeFalse();
        options.RaiseWin.Should().BeFalse();
        options.LowerWin.Should().BeFalse();
        options.RefreshWin.Should().BeFalse();
        options.RestoreWin.Should().BeFalse();
        options.MaximizeWin.Should().BeFalse();
        options.MinimizeWin.Should().BeFalse();
        options.FullscreenWin.Should().BeFalse();
    }

    [TestMethod]
    public void Clone_CreatesIndependentCopy()
    {
        // Arrange
        var options = new WindowOptions
        {
            GetWinPosition = true,
            SetWinPosition = true,
            MaximizeWin = true
        };

        // Act
        var clone = options.Clone();

        // Assert
        clone.GetWinPosition.Should().Be(options.GetWinPosition);
        clone.SetWinPosition.Should().Be(options.SetWinPosition);
        clone.MaximizeWin.Should().Be(options.MaximizeWin);

        // Verify independence
        clone.GetWinPosition = false;
        options.GetWinPosition.Should().BeTrue();
        clone.GetWinPosition.Should().BeFalse();
    }

    [TestMethod]
    public void AllProperties_CanBeToggled()
    {
        // Arrange
        var options = new WindowOptions();

        // Act
        options.GetWinPosition = true;
        options.GetWinSizePixels = true;
        options.GetWinSizeChars = true;
        options.GetScreenSizePixels = true;
        options.GetCellSizePixels = true;
        options.GetIconTitle = true;
        options.GetWinTitle = true;
        options.GetWinState = true;
        options.SetWinPosition = true;
        options.SetWinSizePixels = true;
        options.SetWinSizeChars = true;
        options.RaiseWin = true;
        options.LowerWin = true;
        options.RefreshWin = true;
        options.RestoreWin = true;
        options.MaximizeWin = true;
        options.MinimizeWin = true;
        options.FullscreenWin = true;

        // Assert
        options.GetWinPosition.Should().BeTrue();
        options.GetWinSizePixels.Should().BeTrue();
        options.GetWinSizeChars.Should().BeTrue();
        options.GetScreenSizePixels.Should().BeTrue();
        options.GetCellSizePixels.Should().BeTrue();
        options.GetIconTitle.Should().BeTrue();
        options.GetWinTitle.Should().BeTrue();
        options.GetWinState.Should().BeTrue();
        options.SetWinPosition.Should().BeTrue();
        options.SetWinSizePixels.Should().BeTrue();
        options.SetWinSizeChars.Should().BeTrue();
        options.RaiseWin.Should().BeTrue();
        options.LowerWin.Should().BeTrue();
        options.RefreshWin.Should().BeTrue();
        options.RestoreWin.Should().BeTrue();
        options.MaximizeWin.Should().BeTrue();
        options.MinimizeWin.Should().BeTrue();
        options.FullscreenWin.Should().BeTrue();
    }
}

[TestClass]

public class ThemeOptionsTests
{
    [TestMethod]
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var theme = new ThemeOptions();

        // Assert
        theme.Foreground.Should().BeNull();
        theme.Background.Should().BeNull();
        theme.Cursor.Should().BeNull();
        theme.CursorAccent.Should().BeNull();
        theme.Selection.Should().BeNull();
        theme.SelectionInactive.Should().BeNull();
        theme.Black.Should().BeNull();
        theme.Red.Should().BeNull();
        theme.Green.Should().BeNull();
        theme.Yellow.Should().BeNull();
        theme.Blue.Should().BeNull();
        theme.Magenta.Should().BeNull();
        theme.Cyan.Should().BeNull();
        theme.White.Should().BeNull();
        theme.BrightBlack.Should().BeNull();
        theme.BrightRed.Should().BeNull();
        theme.BrightGreen.Should().BeNull();
        theme.BrightYellow.Should().BeNull();
        theme.BrightBlue.Should().BeNull();
        theme.BrightMagenta.Should().BeNull();
        theme.BrightCyan.Should().BeNull();
        theme.BrightWhite.Should().BeNull();
    }

    [TestMethod]
    public void Colors_CanBeSet()
    {
        // Arrange
        var theme = new ThemeOptions();

        // Act
        theme.Foreground = "#FFFFFF";
        theme.Background = "#000000";
        theme.Red = "#FF0000";
        theme.Green = "#00FF00";
        theme.Blue = "#0000FF";

        // Assert
        theme.Foreground.Should().Be("#FFFFFF");
        theme.Background.Should().Be("#000000");
        theme.Red.Should().Be("#FF0000");
        theme.Green.Should().Be("#00FF00");
        theme.Blue.Should().Be("#0000FF");
    }

    [TestMethod]
    public void Clone_CreatesIndependentCopy()
    {
        // Arrange
        var theme = new ThemeOptions
        {
            Foreground = "#FFFFFF",
            Background = "#000000",
            Red = "#FF0000",
            BrightRed = "#FF6666"
        };

        // Act
        var clone = theme.Clone();

        // Assert
        clone.Foreground.Should().Be(theme.Foreground);
        clone.Background.Should().Be(theme.Background);
        clone.Red.Should().Be(theme.Red);
        clone.BrightRed.Should().Be(theme.BrightRed);

        // Verify independence
        clone.Foreground = "#AAAAAA";
        theme.Foreground.Should().Be("#FFFFFF");
        clone.Foreground.Should().Be("#AAAAAA");
    }

    [TestMethod]
    public void AllColors_CanBeSet()
    {
        // Arrange
        var theme = new ThemeOptions();

        // Act
        theme.Foreground = "#F";
        theme.Background = "#B";
        theme.Cursor = "#C";
        theme.CursorAccent = "#CA";
        theme.Selection = "#S";
        theme.SelectionInactive = "#SI";
        theme.Black = "#0";
        theme.Red = "#1";
        theme.Green = "#2";
        theme.Yellow = "#3";
        theme.Blue = "#4";
        theme.Magenta = "#5";
        theme.Cyan = "#6";
        theme.White = "#7";
        theme.BrightBlack = "#8";
        theme.BrightRed = "#9";
        theme.BrightGreen = "#A";
        theme.BrightYellow = "#BB";
        theme.BrightBlue = "#CC";
        theme.BrightMagenta = "#DD";
        theme.BrightCyan = "#EE";
        theme.BrightWhite = "#FF";

        // Assert
        theme.Foreground.Should().Be("#F");
        theme.Background.Should().Be("#B");
        theme.Cursor.Should().Be("#C");
        theme.CursorAccent.Should().Be("#CA");
        theme.Selection.Should().Be("#S");
        theme.SelectionInactive.Should().Be("#SI");
        theme.Black.Should().Be("#0");
        theme.Red.Should().Be("#1");
        theme.Green.Should().Be("#2");
        theme.Yellow.Should().Be("#3");
        theme.Blue.Should().Be("#4");
        theme.Magenta.Should().Be("#5");
        theme.Cyan.Should().Be("#6");
        theme.White.Should().Be("#7");
        theme.BrightBlack.Should().Be("#8");
        theme.BrightRed.Should().Be("#9");
        theme.BrightGreen.Should().Be("#A");
        theme.BrightYellow.Should().Be("#BB");
        theme.BrightBlue.Should().Be("#CC");
        theme.BrightMagenta.Should().Be("#DD");
        theme.BrightCyan.Should().Be("#EE");
        theme.BrightWhite.Should().Be("#FF");
    }
}

[TestClass]

public class KeyEventTests
{
    [TestMethod]
    public void Constructor_SetsDefaultValues()
    {
        // Arrange & Act
        var keyEvent = new KeyEvent();

        // Assert
        keyEvent.Key.Should().Be(string.Empty);
        keyEvent.CtrlKey.Should().BeFalse();
        keyEvent.AltKey.Should().BeFalse();
        keyEvent.ShiftKey.Should().BeFalse();
        keyEvent.MetaKey.Should().BeFalse();
        keyEvent.KeyCode.Should().Be(0);
    }

    [TestMethod]
    public void Properties_CanBeSet()
    {
        // Arrange
        var keyEvent = new KeyEvent();

        // Act
        keyEvent.Key = "Enter";
        keyEvent.CtrlKey = true;
        keyEvent.AltKey = true;
        keyEvent.ShiftKey = true;
        keyEvent.MetaKey = true;
        keyEvent.KeyCode = 13;

        // Assert
        keyEvent.Key.Should().Be("Enter");
        keyEvent.CtrlKey.Should().BeTrue();
        keyEvent.AltKey.Should().BeTrue();
        keyEvent.ShiftKey.Should().BeTrue();
        keyEvent.MetaKey.Should().BeTrue();
        keyEvent.KeyCode.Should().Be(13);
    }
}

/// <summary>
/// Options that a host changes while the terminal is running, rather than at construction.
/// A settable property that quietly does nothing is worse than one that is not there.
/// </summary>
[TestClass]
public class LiveOptionsTests
{
    private static Terminal WithHistory(int rows, int scrollback, int linesWritten)
    {
        var terminal = new Terminal(new TerminalOptions { Cols = 20, Rows = rows, Scrollback = scrollback });
        for (var i = 0; i < linesWritten; i++)
            terminal.WriteLine($"line{i}");
        return terminal;
    }
    private static bool Holds(Terminal t, string text)
    {
        for (var y = 0; y < t.Buffer.Lines.Length; y++)
            if (t.Buffer.Lines[y]?.TranslateToString(true).Trim() == text)
                return true;
        return false;
    }
    [TestMethod]
    public void Lowering_the_scrollback_after_construction_shrinks_the_history()
    {
        // Scrollback was read once, when the buffer was built, and never again -- so a host
        // reclaiming memory set a property that reported the new value and changed nothing.
        var terminal = WithHistory(rows: 4, scrollback: 50, linesWritten: 40);
        terminal.Buffer.Lines.MaxLength.Should().Be(54);
        terminal.Options.Scrollback = 5;
        terminal.Buffer.Lines.MaxLength.Should().Be(9);
    }
    [TestMethod]
    public void Shrinking_the_scrollback_drops_the_oldest_lines_and_keeps_the_screen()
    {
        // CircularList.Resize keeps the FRONT of the list, which for a scrollback is backwards:
        // it would discard the screen the user is looking at and keep the history nobody asked to
        // keep. The oldest go.
        var terminal = WithHistory(rows: 4, scrollback: 50, linesWritten: 40);
        Holds(terminal, "line0").Should().BeTrue("the oldest line should still be here before shrinking");
        terminal.Options.Scrollback = 5;
        Holds(terminal, "line0").Should().BeFalse("the oldest line should have been dropped");
        Holds(terminal, "line39").Should().BeTrue("the newest line must survive -- it is on screen");
    }
    [TestMethod]
    public void Shrinking_the_scrollback_leaves_the_viewport_on_the_live_bottom()
    {
        // The viewport is recomputed against what is left rather than shifted by the trim amount,
        // or it ends up a fixed distance from rows that no longer exist and everything written
        // afterwards lands outside the visible area.
        var terminal = WithHistory(rows: 4, scrollback: 50, linesWritten: 40);
        terminal.Options.Scrollback = 5;
        terminal.Buffer.YDisp.Should().Be(terminal.Buffer.YBase);
        terminal.WriteLine("after");
        Holds(terminal, "after").Should().BeTrue();
    }
    [TestMethod]
    public void Raising_the_scrollback_after_construction_grows_the_history()
    {
        var terminal = WithHistory(rows: 4, scrollback: 5, linesWritten: 20);
        terminal.Buffer.Lines.MaxLength.Should().Be(9);
        terminal.Options.Scrollback = 100;
        terminal.Buffer.Lines.MaxLength.Should().Be(104);
        Holds(terminal, "line19").Should().BeTrue("growing must not disturb what is already held");
    }
    [TestMethod]
    public void The_alternate_screen_keeps_no_history_whatever_the_scrollback_says()
    {
        // The alternate buffer is constructed with none by definition, and a later write to the
        // option must not give it any -- a full-screen program's scrollback is the shell's.
        var terminal = WithHistory(rows: 4, scrollback: 50, linesWritten: 20);
        terminal.Write($"{((char)0x1B)}[?1049h");
        var altCapacity = terminal.Buffer.Lines.MaxLength;
        terminal.Options.Scrollback = 500;
        terminal.Buffer.Lines.MaxLength.Should().Be(altCapacity);
    }
    [TestMethod]
    public void Setting_the_scrollback_to_what_it_already_is_changes_nothing()
    {
        var terminal = WithHistory(rows: 4, scrollback: 50, linesWritten: 40);
        var before = terminal.Buffer.Lines.MaxLength;
        terminal.Options.Scrollback = 50;
        terminal.Buffer.Lines.MaxLength.Should().Be(before);
        Holds(terminal, "line0").Should().BeTrue("a no-op write must not trim anything");
    }
    [TestMethod]
    public void Assigning_a_theme_after_construction_reseeds_the_palette()
    {
        // ColorPalette.ApplyTheme documents itself as the runtime path for an embedder following
        // the OS light/dark setting -- but the option that names the theme was read once, to build
        // the palette, and never again. An embedder assigning a new theme watched nothing happen.
        var terminal = new Terminal(new TerminalOptions
        {
            Cols = 20,
            Rows = 4,
            Theme = new ThemeOptions { Background = "#000000", Foreground = "#ffffff" },
        });

        terminal.Options.Theme = new ThemeOptions { Background = "#ffffff", Foreground = "#000000" };

        terminal.Colors.Background.Should().Be(0xFFFFFF);
        terminal.Colors.Foreground.Should().Be(0x000000);
    }

    [TestMethod]
    public void A_new_theme_reseeds_colours_an_application_had_changed()
    {
        // Half in the old theme and half in the new one is not a theme, so OSC 10/11 changes are
        // re-seeded away rather than preserved across a theme switch.
        var terminal = new Terminal(new TerminalOptions
        {
            Cols = 20,
            Rows = 4,
            Theme = new ThemeOptions { Background = "#000000" },
        });
        terminal.Write($"{((char)0x1B)}]11;#123456{((char)0x1B)}\\");   // OSC 11: application sets it

        terminal.Options.Theme = new ThemeOptions { Background = "#ffffff" };

        terminal.Colors.Background.Should().Be(0xFFFFFF);
    }

    [TestMethod]
    public void Changing_the_tab_stop_width_lays_the_stops_out_again()
    {
        // ResetTabStops read this, but only ran at construction, on a resize and on RIS -- so the
        // change looked ignored, and then took effect later when something unrelated resized the
        // window.
        var terminal = new Terminal(new TerminalOptions { Cols = 40, Rows = 4, TabStopWidth = 8 });
        terminal.Write("\t");
        terminal.Buffer.X.Should().Be(8);

        terminal.Options.TabStopWidth = 4;

        terminal.Write($"{((char)0x1B)}[1;1H\t");
        terminal.Buffer.X.Should().Be(4);
    }

    [TestMethod]
    public void A_resize_keeps_the_options_size_in_step_with_the_terminal()
    {
        // Options.Cols went on reporting the number the terminal was BUILT with while Terminal.Cols
        // reported the number it is. Two public properties of the same name, disagreeing.
        var terminal = new Terminal(new TerminalOptions { Cols = 80, Rows = 24 });

        terminal.Resize(120, 40);

        terminal.Options.Cols.Should().Be(120);
        terminal.Options.Rows.Should().Be(40);
        terminal.Options.Cols.Should().Be(terminal.Cols);
        terminal.Options.Rows.Should().Be(terminal.Rows);
    }

    [TestMethod]
    public void The_options_object_a_caller_kept_does_not_reach_the_terminal()
    {
        // The snapshot contract from #101, restated here because the live hook is installed on the
        // terminal's own copy: making Scrollback live must not quietly re-alias the two.
        var mine = new TerminalOptions { Cols = 20, Rows = 4, Scrollback = 50 };
        var terminal = new Terminal(mine);
        mine.Scrollback = 5;
        terminal.Buffer.Lines.MaxLength.Should().Be(54);
    }
}
