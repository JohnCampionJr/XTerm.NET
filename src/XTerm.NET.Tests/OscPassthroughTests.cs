using XTerm;
using XTerm.Events;
using XTerm.Options;

namespace XTerm.Tests;

/// <summary>
/// Covers <see cref="Terminal.OscReceived"/>, the escape hatch for OSC codes this terminal does not
/// implement.
/// </summary>
[TestClass]
public class OscPassthroughTests
{
    private Terminal CreateTerminal(int cols = 80, int rows = 24)
    {
        var options = new TerminalOptions { Cols = cols, Rows = rows };
        return new Terminal(options);
    }

    private static List<TerminalEvents.OscReceivedEventArgs> Capture(Terminal terminal)
    {
        var seen = new List<TerminalEvents.OscReceivedEventArgs>();
        terminal.OscReceived += (_, e) => seen.Add(e);
        return seen;
    }

    [TestMethod]
    public void OscReceived_FiresForUnknownSequence()
    {
        // The reason this event exists: a code with no case here reaches Debug.WriteLine and is
        // otherwise unrecoverable. OSC 1337 is iTerm2's proprietary space; its unknown keys remain
        // available to a listener even though the useful keys have built-in handling.
        //
        // This used to use OSC 133, which was unimplemented when the event was added and is not any
        // more. That is the Recognized contract working rather than a test going stale: a listener
        // filling the gap stops doing so on its own once a code lands in HandleOsc. The pair below
        // pins both halves of it.
        var terminal = CreateTerminal();
        var seen = Capture(terminal);

        terminal.Write("\x1B]1337;SetMark\x07");

        var osc = seen.Should().ContainSingle().Which;
        osc.Code.Should().Be(1337);
        osc.Identifier.Should().Be("1337");
        osc.Data.Should().Be("SetMark");
        osc.Raw.Should().Be("1337;SetMark");
        osc.Recognized.Should().BeFalse("the terminal has no handler for 1337, which is the point");
    }

    [TestMethod]
    public void OscReceived_ReportsShellIntegrationAsRecognized_NowThatItIsImplemented()
    {
        // The other half: OSC 133 is handled now, so a listener that only wants what this terminal
        // ignores must be told to leave it alone.
        var terminal = CreateTerminal();
        var seen = Capture(terminal);

        terminal.Write("\x1B]133;A\x07");

        var osc = seen.Should().ContainSingle().Which;
        osc.Code.Should().Be(133);
        osc.Recognized.Should().BeTrue("133 reaches a handler now, and Recognized has to say so");
    }

    [TestMethod]
    public void OscReceived_FiresForKnownSequence_AndReportsItRecognized()
    {
        var terminal = CreateTerminal();
        var seen = Capture(terminal);

        terminal.Write("\x1B]0;A Title\x07");

        var osc = seen.Should().ContainSingle().Which;
        osc.Code.Should().Be(0);
        osc.Data.Should().Be("A Title");
        osc.Recognized.Should().BeTrue();
    }

    [TestMethod]
    public void OscReceived_DoesNotDisturbBuiltInHandling()
    {
        // Purely additive: subscribing must not change what the terminal already did.
        var terminal = CreateTerminal();
        Capture(terminal);

        terminal.Write("\x1B]0;Still Set\x07");

        terminal.Title.Should().Be("Still Set");
    }

    [TestMethod]
    public void OscReceived_FiresAfterBuiltInHandling()
    {
        // Ordering is contractual: a listener reads terminal state as settled, not mid-flight.
        var terminal = CreateTerminal();
        string? titleWhenObserved = null;
        terminal.OscReceived += (_, _) => titleWhenObserved = terminal.Title;

        terminal.Write("\x1B]0;Observed\x07");

        titleWhenObserved.Should().Be("Observed");
    }

    [TestMethod]
    public void OscReceived_ReportsNegativeCode_ForNonNumericIdentifier()
    {
        var terminal = CreateTerminal();
        var seen = Capture(terminal);

        terminal.Write("\x1B]notanumber;payload\x07");

        var osc = seen.Should().ContainSingle().Which;
        osc.Code.Should().Be(-1);
        osc.Identifier.Should().Be("notanumber");
        osc.Data.Should().Be("payload");
        osc.Recognized.Should().BeFalse();
    }

    [TestMethod]
    public void OscReceived_ReportsEmptyData_WhenSequenceHasNoParameters()
    {
        var terminal = CreateTerminal();
        var seen = Capture(terminal);

        terminal.Write("\x1B]133\x07");

        var osc = seen.Should().ContainSingle().Which;
        osc.Code.Should().Be(133);
        osc.Data.Should().Be(string.Empty);
        osc.Raw.Should().Be("133");
    }

    [TestMethod]
    public void OscReceived_KeepsDataIntact_WhenPayloadContainsSemicolons()
    {
        // Only the FIRST ';' separates identifier from data. OSC 9;4 and OSC 133;D;<exit> both carry
        // their own sub-parameters, and a handler cannot reconstruct them if this splits too eagerly.
        var terminal = CreateTerminal();
        var seen = Capture(terminal);

        terminal.Write("\x1B]9;4;1;50\x07");

        var osc = seen.Should().ContainSingle().Which;
        osc.Code.Should().Be(9);
        osc.Data.Should().Be("4;1;50");
        osc.Raw.Should().Be("9;4;1;50");
    }

    [TestMethod]
    public void OscReceived_FiresOncePerSequence()
    {
        var terminal = CreateTerminal();
        var seen = Capture(terminal);

        terminal.Write("\x1B]133;A\x07\x1B]133;B\x07\x1B]133;C\x07");

        seen.Count.Should().Be(3);
        seen.Select(o => o.Data).Should().Equal(new[] { "A", "B", "C" });
    }

    [TestMethod]
    public void OscReceived_AcceptsStringTerminator_AsWellAsBel()
    {
        // Shell-integration snippets in the wild use both terminators; OSC 133 examples ship with ST.
        var terminal = CreateTerminal();
        var seen = Capture(terminal);

        terminal.Write("\x1B]133;D;0\x1B\\");

        var osc = seen.Should().ContainSingle().Which;
        osc.Code.Should().Be(133);
        osc.Data.Should().Be("D;0");
    }
}
