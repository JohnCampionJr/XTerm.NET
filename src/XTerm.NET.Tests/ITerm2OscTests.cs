using XTerm.Graphics;
using XTerm.Options;
using XTerm.Tests.Graphics;
using XTerm.Events;

namespace XTerm.Tests;

[TestClass]

public class ITerm2OscTests
{
    private static Terminal CreateTerminal() => new(new TerminalOptions
    {
        Cols = 30,
        Rows = 12,
        CellWidthPixels = 2,
        CellHeightPixels = 3
    });

    private static string Osc(string data) => $"\u001b]1337;{data}\u001b\\";

    [TestMethod]
    public void File_InlinePng_IsDecodedAndPlaced()
    {
        var terminal = CreateTerminal();
        terminal.Write(Osc($"File=inline=1:{Png}"));

        var image = ImageAssertions.ImageAt(terminal, 0, 0);
        image.Should().NotBeNull();
        (image!.PixelWidth).Should().Be(5);
        image.PixelHeight.Should().Be(3);
    }

    [TestMethod]
    public void ShellIntegrationValues_AreRecorded()
    {
        var terminal = CreateTerminal();

        terminal.Write(Osc("SetUserVar=prompt=dmFsdWU="));
        terminal.Write(Osc("CurrentDir=/home/me"));
        terminal.Write(Osc("ShellIntegrationVersion=12"));
        terminal.Write(Osc("RemoteHost=me@example.test"));

        (terminal.UserVariables["prompt"]).Should().Be("value");
        terminal.CurrentDirectory.Should().Be("/home/me");
        terminal.ShellIntegrationVersion.Should().Be("12");
        terminal.RemoteHost.Should().Be("me@example.test");
    }

    [TestMethod]
    public void CurrentDir_PreservesLiteralPercentSequences()
    {
        var terminal = CreateTerminal();

        terminal.Write(Osc("CurrentDir=/data/AB%20CD"));

        terminal.CurrentDirectory.Should().Be("/data/AB%20CD");
    }

    [TestMethod]
    public void File_ExceedingRegistryBudget_IsIgnored()
    {
        var terminal = new Terminal(new TerminalOptions
        {
            Cols = 30,
            Rows = 12,
            MaxImageRegistryBytes = 1
        });
        terminal.Write(Osc($"File=inline=1:{Png}"));

        ImageAssertions.ImageAt(terminal, 0, 0).Should().BeNull();
    }

    [TestMethod]
    public void File_IsIgnoredWhenITerm2ImagesAreDisabled()
    {
        var terminal = CreateTerminal();
        terminal.Options.ITerm2ImagesEnabled = false;

        terminal.Write(Osc($"File=inline=1:{Png}"));

        ImageAssertions.ImageAt(terminal, 0, 0).Should().BeNull();
    }

    [TestMethod]
    public void DroppedFile_IsReportedAsUnrecognized()
    {
        var terminal = CreateTerminal();
        TerminalEvents.OscReceivedEventArgs? received = null;
        terminal.OscReceived += (_, e) => received = e;

        terminal.Write(Osc("File=inline=1:not-a-png"));

        (received!.Recognized).Should().BeFalse();
    }

    [TestMethod]
    public void UserVariables_AreBounded()
    {
        var terminal = CreateTerminal();
        terminal.Options.MaxUserVariables = 1;
        terminal.Options.MaxUserVariableBytes = 4;

        terminal.Write(Osc("SetUserVar=one=YWJjZA=="));
        terminal.Write(Osc("SetUserVar=two=YWJjZA=="));
        terminal.Write(Osc("SetUserVar=one=dG9vIGxvbmc="));

        (terminal.UserVariables["one"]).Should().Be("abcd");
        terminal.UserVariables.Should().ContainSingle();
    }

    [TestMethod]
    public void WindowExtensions_RespectPermissions()
    {
        var terminal = CreateTerminal();
        var raised = false;
        var attention = false;
        terminal.WindowRaised += (_, _) => raised = true;
        terminal.AttentionRequested += (_, _) => attention = true;

        terminal.Write(Osc("StealFocus="));
        terminal.Write(Osc("RequestAttention="));
        raised.Should().BeFalse();
        attention.Should().BeFalse();

        terminal.Options.WindowOptions.RaiseWin = true;
        terminal.Options.WindowOptions.RequestAttention = true;
        terminal.Write(Osc("StealFocus"));
        terminal.Write(Osc("RequestAttention=no"));

        raised.Should().BeTrue();
        attention.Should().BeTrue();
    }

    [TestMethod]
    public void ReportCellSize_RespondsWhenAllowed()
    {
        var terminal = CreateTerminal();
        terminal.Options.WindowOptions.GetCellSizePixels = true;
        string? response = null;
        terminal.DataReceived += (_, e) => response = e.Data;

        terminal.Write(Osc("ReportCellSize"));

        response.Should().Be("\u001b]1337;ReportCellSize=3.0;2.0;1.0\u001b\\");
    }

    [TestMethod]
    public void RequestAttention_PreservesItsAction()
    {
        var terminal = CreateTerminal();
        terminal.Options.WindowOptions.RequestAttention = true;
        string? action = null;
        terminal.AttentionRequested += (_, e) => action = e.Action;

        terminal.Write(Osc("RequestAttention=no"));

        action.Should().Be("no");
    }

    [TestMethod]
    public void File_ImageIsErasedByText()
    {
        var terminal = CreateTerminal();
        terminal.Write(Osc($"File=inline=1:{Png}"));

        terminal.Write("\u001b[1;1HX");

        ImageAssertions.ImageAt(terminal, 0, 0).Should().BeNull();
    }

    [TestMethod]
    public void UnknownKey_IsIgnored()
    {
        var terminal = CreateTerminal();

        terminal.Write(Osc("Unrecognised=value"));

        terminal.UserVariables.Should().BeEmpty();
        ImageAssertions.ImageAt(terminal, 0, 0).Should().BeNull();
    }

    private const string Png = "iVBORw0KGgoAAAANSUhEUgAAAAUAAAADCAYAAABbNsX4AAAAAXNSR0IArs4c6QAAAARnQU1BAACx" +
                               "jwv8YQUAAAAJcEhZcwAADsMAAA7DAcdvqGQAAABESURBVBhXY+ASkeOSN8+WM4naYeRZa+sWt6Ar" +
                               "iqH48PW8jmcxTXN1V07bFKC36nhx9T6GO9NOXPq40/eZ2J1ZvxigAAD9BhlVn28K4gAAAABJRU5E" +
                               "rkJggg==";

    [TestMethod]
    public void File_HugePayloadIsRejectedBeforeDecoding()
    {
        // A valid-base64 blob larger than the registry budget must be dropped from its LENGTH,
        // before FromBase64String materialises the decoded copy — otherwise the reject itself
        // costs the allocation the cap exists to prevent.
        var terminal = new Terminal(new TerminalOptions { MaxImageRegistryBytes = 1024 });
        var recognized = new List<bool>();
        terminal.OscReceived += (_, e) => { if (e.Code == 1337) recognized.Add(e.Recognized); };

        var huge = Convert.ToBase64String(new byte[8192]);   // 8x the budget, valid base64
        terminal.Write($"\u001b]1337;File=inline=1:{huge}\u001b\\");

        recognized.Should().Equal(new[] { false });
    }

    [TestMethod]
    public void ReportCellSize_SpeaksPointsAndScale()
    {
        // iTerm2's fields are points with a pixels-per-point scale: 20px cells on a 2x display
        // are 10.0 points, and the scale rides along as the third field.
        var terminal = new Terminal(new TerminalOptions
        {
            CellWidthPixels = 18,
            CellHeightPixels = 36,
            DisplayScale = 2.0,
            WindowOptions = { GetCellSizePixels = true },
        });
        string? response = null;
        terminal.DataReceived += (_, e) => response = e.Data;

        terminal.Write("\u001b]1337;ReportCellSize\u001b\\");

        response.Should().Be("\u001b]1337;ReportCellSize=18.0;9.0;2.0\u001b\\");
    }

    [TestMethod]
    public void Capabilities_ReportsTheImplementedSet()
    {
        var terminal = CreateTerminal();
        string? response = null;
        terminal.DataReceived += (_, e) => response = e.Data;

        terminal.Write(Osc("Capabilities"));

        response.Should().Be("\u001b]1337;Capabilities=T24CwLrMUBFGsGoSyHNoSx\u001b\\");
    }
}
