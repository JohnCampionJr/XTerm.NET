using XTerm.Common;

namespace XTerm.Tests.Common;

/// <summary>
/// The CSI identifier the parser hands to the input handler carries whatever it collected before
/// the final character -- the private marker among it. The lookup used to strip a leading '?' or
/// '>' and match on the rest, which made every private sequence an alias for whichever non-private
/// command shared its final character. These tests pin the identifier match down as exact.
/// </summary>
[TestClass]
public class CsiCommandExtensionsTests
{
    [TestMethod]
    [DataRow("S", CsiCommand.ScrollUp)]
    [DataRow("J", CsiCommand.EraseInDisplay)]
    [DataRow("K", CsiCommand.EraseInLine)]
    [DataRow("h", CsiCommand.SetMode)]
    [DataRow("l", CsiCommand.ResetMode)]
    [DataRow("c", CsiCommand.DeviceAttributes)]
    [DataRow("n", CsiCommand.DeviceStatusReport)]
    [DataRow("m", CsiCommand.SelectGraphicRendition)]
    [DataRow("r", CsiCommand.SetScrollRegion)]
    [DataRow("s", CsiCommand.SaveCursorAnsi)]
    [DataRow("t", CsiCommand.WindowManipulation)]
    [DataRow("u", CsiCommand.RestoreCursorAnsi)]
    [DataRow("$p", CsiCommand.RequestMode)]
    [DataRow(" q", CsiCommand.SelectCursorStyle)]
    public void ToCsiCommand_MapsNonPrivateIdentifiers(string identifier, CsiCommand command)
    {
        identifier.ToCsiCommand().Should().Be(command);
    }

    [TestMethod]
    [DataRow("?S", CsiCommand.GraphicsAttributes)] // XTSMGRAPHICS, not SCROLL UP
    [DataRow("?J", CsiCommand.EraseInDisplay)]     // DECSED
    [DataRow("?K", CsiCommand.EraseInLine)]        // DECSEL
    [DataRow("?h", CsiCommand.SetMode)]            // DECSET
    [DataRow("?l", CsiCommand.ResetMode)]          // DECRST
    [DataRow("?n", CsiCommand.DeviceStatusReport)] // DEC DSR
    [DataRow(">c", CsiCommand.DeviceAttributes)]   // DA2
    [DataRow("?$p", CsiCommand.RequestMode)]       // DECRQM, private
    [DataRow("=u", CsiCommand.KittyKeyboardSet)]   // Kitty keyboard, set flags
    [DataRow("?u", CsiCommand.KittyKeyboardQuery)] // Kitty keyboard, query flags
    [DataRow(">u", CsiCommand.KittyKeyboardPush)]  // Kitty keyboard, push flags
    [DataRow("<u", CsiCommand.KittyKeyboardPop)]   // Kitty keyboard, pop flags
    [DataRow(">q", CsiCommand.SelectCursorStyle)]  // XTVERSION, split from DECSCUSR by its marker
    public void ToCsiCommand_MapsExplicitPrivateIdentifiers(string identifier, CsiCommand command)
    {
        identifier.ToCsiCommand().Should().Be(command);
    }

    /// <summary>
    /// Every one of these used to be dispatched as its non-private namesake. Each comment is the
    /// command the old strip-then-match lookup ran instead.
    /// </summary>
    [TestMethod]
    [DataRow("?s")]  // XTSAVE -> saved the cursor
    [DataRow("?r")]  // XTRESTORE -> reset the scroll region and homed the cursor
    [DataRow(">m")]  // XTMODKEYS -> applied its arguments as SGR
    [DataRow(">n")]  // XTMODKEYS disable -> answered a device status report
    [DataRow(">t")]  // XTSMTITLE -> performed a window operation
    [DataRow("?t")]
    [DataRow("?c")]  // not a sequence at all -> answered as a secondary DA
    [DataRow("?m")]
    public void ToCsiCommand_UnmappedPrivateIdentifiers_ReturnUnknown(string identifier)
    {
        identifier.ToCsiCommand().Should().Be(CsiCommand.Unknown);
    }

    /// <summary>
    /// The same aliasing on the intermediate-byte axis. DECSCUSR is "CSI Ps SP q" and is mapped as
    /// " q"; the bare final character is DECLL, a sequence this terminal does not implement, and
    /// mapping it to DECSCUSR as well turned "clear the LEDs" into "blink the cursor".
    /// </summary>
    [TestMethod]
    public void ToCsiCommand_BareQ_IsDecllAndReturnsUnknown()
    {
        ("q".ToCsiCommand()).Should().Be(CsiCommand.Unknown);
    }

    /// <summary>
    /// '&lt;' and '=' were never stripped, so they are recognised only where the map lists them --
    /// the Kitty keyboard pop and set forms, and nothing else.
    /// </summary>
    [TestMethod]
    [DataRow("=c")]
    [DataRow("<c")]
    [DataRow("=S")]
    [DataRow("<m")]
    public void ToCsiCommand_OtherPrivateMarkers_ReturnUnknown(string identifier)
    {
        identifier.ToCsiCommand().Should().Be(CsiCommand.Unknown);
    }
}
