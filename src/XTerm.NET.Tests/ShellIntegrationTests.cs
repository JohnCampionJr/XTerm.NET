using XTerm;
using XTerm.Common;
using XTerm.Events;
using XTerm.Options;

namespace XTerm.Tests;

/// <summary>
/// Covers OSC 133 (FinalTerm/FTCS shell integration marks) and OSC 9 (the ConEmu extensions:
/// working directory, progress, notification).
/// </summary>
[TestClass]
public class ShellIntegrationTests
{
    private Terminal CreateTerminal(int cols = 80, int rows = 24)
    {
        var options = new TerminalOptions { Cols = cols, Rows = rows };
        return new Terminal(options);
    }

    // ---- OSC 133 -----------------------------------------------------------------------------

    [TestMethod]
    public void ShellIntegrationState_IsNullBeforeAnyMark()
    {
        // Null is the third state and the reason this property is nullable: a shell with no
        // integration configured is indistinguishable from one sitting at a prompt, and defaulting
        // to PromptStart would assert the shell is idle on no evidence at all.
        var terminal = CreateTerminal();

        terminal.ShellIntegrationState.Should().BeNull();
        terminal.LastCommandExitCode.Should().BeNull();
    }

    [TestMethod]
    [DataRow("A", ShellIntegrationMark.PromptStart)]
    [DataRow("B", ShellIntegrationMark.CommandStart)]
    [DataRow("C", ShellIntegrationMark.CommandExecuted)]
    [DataRow("D", ShellIntegrationMark.CommandFinished)]
    public void Osc133_RecordsEachMark(string letter, ShellIntegrationMark expected)
    {
        var terminal = CreateTerminal();
        TerminalEvents.ShellIntegrationEventArgs? received = null;
        terminal.ShellIntegrationMarkReceived += (_, e) => received = e;

        terminal.Write($"\x1B]133;{letter}\x07");

        terminal.ShellIntegrationState.Should().Be(expected);
        received.Should().NotBeNull();
        (received!.Mark).Should().Be(expected);
    }

    [TestMethod]
    public void Osc133_TracksAFullPromptCommandCycle()
    {
        // The sequence a caller actually depends on: at B the shell is waiting for input, at C
        // something else owns the terminal, at D it is the shell's again.
        var terminal = CreateTerminal();
        var marks = new List<ShellIntegrationMark>();
        terminal.ShellIntegrationMarkReceived += (_, e) => marks.Add(e.Mark);

        terminal.Write("\x1B]133;A\x07");
        terminal.Write("\x1B]133;B\x07");
        terminal.ShellIntegrationState.Should().Be(ShellIntegrationMark.CommandStart);

        terminal.Write("\x1B]133;C\x07");
        terminal.ShellIntegrationState.Should().Be(ShellIntegrationMark.CommandExecuted);

        terminal.Write("\x1B]133;D;0\x07");
        terminal.ShellIntegrationState.Should().Be(ShellIntegrationMark.CommandFinished);

        marks.Should().Equal(new[]
            {
                ShellIntegrationMark.PromptStart,
                ShellIntegrationMark.CommandStart,
                ShellIntegrationMark.CommandExecuted,
                ShellIntegrationMark.CommandFinished,
            });
    }

    [TestMethod]
    public void Osc133_CapturesExitCode()
    {
        var terminal = CreateTerminal();
        TerminalEvents.ShellIntegrationEventArgs? received = null;
        terminal.ShellIntegrationMarkReceived += (_, e) => received = e;

        terminal.Write("\x1B]133;D;127\x07");

        terminal.LastCommandExitCode.Should().Be(127);
        (received!.ExitCode).Should().Be(127);
    }

    [TestMethod]
    public void Osc133_CapturesNegativeExitCode()
    {
        // Microsoft's own pwsh snippet returns -1 for a PowerShell-native error.
        var terminal = CreateTerminal();

        terminal.Write("\x1B]133;D;-1\x07");

        terminal.LastCommandExitCode.Should().Be(-1);
    }

    [TestMethod]
    public void Osc133_LeavesExitCodeNull_WhenTheShellOmitsIt()
    {
        // cmd.exe cannot read the previous command's status from its prompt, so a bare D is normal
        // rather than malformed. Null must not collapse into 0, or every cmd.exe command would look
        // like it succeeded.
        var terminal = CreateTerminal();
        TerminalEvents.ShellIntegrationEventArgs? received = null;
        terminal.ShellIntegrationMarkReceived += (_, e) => received = e;

        terminal.Write("\x1B]133;D\x07");

        terminal.ShellIntegrationState.Should().Be(ShellIntegrationMark.CommandFinished);
        terminal.LastCommandExitCode.Should().BeNull();
        (received!.ExitCode).Should().BeNull();
    }

    [TestMethod]
    public void Osc133_ClearsAStaleExitCode_WhenTheNextCommandReportsNone()
    {
        var terminal = CreateTerminal();

        terminal.Write("\x1B]133;D;3\x07");
        terminal.LastCommandExitCode.Should().Be(3);

        terminal.Write("\x1B]133;D\x07");
        terminal.LastCommandExitCode.Should().BeNull();
    }

    [TestMethod]
    public void Osc133_ReportsNoExitCode_OnMarksThatCannotCarryOne()
    {
        var terminal = CreateTerminal();
        TerminalEvents.ShellIntegrationEventArgs? received = null;
        terminal.ShellIntegrationMarkReceived += (_, e) => received = e;

        terminal.Write("\x1B]133;A\x07");

        (received!.ExitCode).Should().BeNull();
    }

    [TestMethod]
    public void Osc133_AcceptsStringTerminator()
    {
        // The bash and cmd snippets in Microsoft's docs terminate with ST, not BEL.
        var terminal = CreateTerminal();

        terminal.Write("\x1B]133;D;0\x1B\\");

        terminal.ShellIntegrationState.Should().Be(ShellIntegrationMark.CommandFinished);
        terminal.LastCommandExitCode.Should().Be(0);
    }

    [TestMethod]
    public void Osc133_IgnoresUnknownMark_WithoutDisturbingState()
    {
        var terminal = CreateTerminal();
        terminal.Write("\x1B]133;A\x07");

        terminal.Write("\x1B]133;Z\x07");

        terminal.ShellIntegrationState.Should().Be(ShellIntegrationMark.PromptStart);
    }

    [TestMethod]
    public void Osc133_IgnoresEmptyPayload()
    {
        var terminal = CreateTerminal();

        terminal.Write("\x1B]133\x07");

        terminal.ShellIntegrationState.Should().BeNull();
    }

    // ---- OSC 9 ; 9 : working directory -------------------------------------------------------

    [TestMethod]
    public void Osc9Cwd_SetsCurrentDirectory()
    {
        // Microsoft's documented Windows prompts emit 9;9 rather than OSC 7, so a terminal reading
        // only 7 loses the working directory on Windows entirely.
        var terminal = CreateTerminal();
        string? reported = null;
        terminal.DirectoryChanged += (_, e) => reported = e.Directory;

        terminal.Write("\x1B]9;9;C:\\Users\\me\x07");

        terminal.CurrentDirectory.Should().Be("C:\\Users\\me");
        reported.Should().Be("C:\\Users\\me");
    }

    [TestMethod]
    public void Osc9Cwd_StripsSurroundingQuotes()
    {
        // The pwsh snippet in Microsoft's docs emits the path already quoted.
        var terminal = CreateTerminal();

        terminal.Write("\x1B]9;9;\"C:\\Program Files\"\x07");

        terminal.CurrentDirectory.Should().Be("C:\\Program Files");
    }

    [TestMethod]
    public void Osc9Cwd_IgnoresEmptyPath()
    {
        var terminal = CreateTerminal();
        terminal.Write("\x1B]9;9;C:\\keep\x07");

        terminal.Write("\x1B]9;9;\x07");

        terminal.CurrentDirectory.Should().Be("C:\\keep");
    }

    // ---- OSC 9 ; 4 : progress ----------------------------------------------------------------

    [TestMethod]
    [DataRow(0, ProgressState.None)]
    [DataRow(1, ProgressState.Normal)]
    [DataRow(2, ProgressState.Error)]
    [DataRow(3, ProgressState.Indeterminate)]
    [DataRow(4, ProgressState.Warning)]
    public void Osc9Progress_RecordsEachState(int raw, ProgressState expected)
    {
        var terminal = CreateTerminal();
        TerminalEvents.ProgressEventArgs? received = null;
        terminal.ProgressChanged += (_, e) => received = e;

        terminal.Write($"\x1B]9;4;{raw};50\x07");

        terminal.ProgressState.Should().Be(expected);
        received.Should().NotBeNull();
        (received!.State).Should().Be(expected);
    }

    [TestMethod]
    public void Osc9Progress_RecordsValue()
    {
        var terminal = CreateTerminal();

        terminal.Write("\x1B]9;4;1;42\x07");

        terminal.ProgressState.Should().Be(ProgressState.Normal);
        terminal.ProgressValue.Should().Be(42);
    }

    [TestMethod]
    public void Osc9Progress_ClampsOutOfRangeValue()
    {
        var terminal = CreateTerminal();

        terminal.Write("\x1B]9;4;1;250\x07");

        terminal.ProgressValue.Should().Be(100);
    }

    [TestMethod]
    public void Osc9Progress_ZeroesValue_ForStatesThatHaveNone()
    {
        // Indeterminate carries no percentage; leaving a stale one would render a bar at the old
        // position while claiming the extent is unknown.
        var terminal = CreateTerminal();
        terminal.Write("\x1B]9;4;1;80\x07");

        terminal.Write("\x1B]9;4;3\x07");

        terminal.ProgressState.Should().Be(ProgressState.Indeterminate);
        terminal.ProgressValue.Should().Be(0);
    }

    [TestMethod]
    public void Osc9Progress_ClearsOnStateNone()
    {
        var terminal = CreateTerminal();
        terminal.Write("\x1B]9;4;1;80\x07");

        terminal.Write("\x1B]9;4;0\x07");

        terminal.ProgressState.Should().Be(ProgressState.None);
        terminal.ProgressValue.Should().Be(0);
    }

    [TestMethod]
    public void Osc9Progress_IgnoresUnknownState()
    {
        var terminal = CreateTerminal();
        terminal.Write("\x1B]9;4;1;80\x07");

        terminal.Write("\x1B]9;4;9;10\x07");

        terminal.ProgressState.Should().Be(ProgressState.Normal);
        terminal.ProgressValue.Should().Be(80);
    }

    // ---- OSC 9 : notification ----------------------------------------------------------------

    [TestMethod]
    public void Osc9Notification_RaisesWithText()
    {
        var terminal = CreateTerminal();
        string? text = null;
        terminal.NotificationReceived += (_, e) => text = e.Text;

        terminal.Write("\x1B]9;Build finished\x07");

        text.Should().Be("Build finished");
    }

    [TestMethod]
    [DataRow("9")]
    [DataRow("4")]
    public void Osc9_IgnoresAClaimedSubCommandWithNoPayload(string subCommand)
    {
        // "OSC 9;9" carries a sub-command and nothing else. Falling through to the notification case
        // would raise a toast whose entire body is "9".
        var terminal = CreateTerminal();
        var notifications = new List<string>();
        terminal.NotificationReceived += (_, e) => notifications.Add(e.Text);

        terminal.Write($"\u001b]9;{subCommand}\u0007");

        notifications.Should().BeEmpty();
    }

    [TestMethod]
    public void Osc9Notification_StillFiresForTextThatContainsSemicolons()
    {
        // The fallback must keep the whole body. Only a CLAIMED sub-command is special.
        var terminal = CreateTerminal();
        string? text = null;
        terminal.NotificationReceived += (_, e) => text = e.Text;

        terminal.Write("\u001b]9;Build finished; 3 warnings\u0007");

        text.Should().Be("Build finished; 3 warnings");
    }

    [TestMethod]
    public void Osc9Notification_DoesNotFireForProgressOrCwd()
    {
        // The sub-parameters are not notifications. Without this distinction OSC 9;4 would raise a
        // toast reading "4;1;50" on every progress tick.
        var terminal = CreateTerminal();
        var notifications = new List<string>();
        terminal.NotificationReceived += (_, e) => notifications.Add(e.Text);

        terminal.Write("\x1B]9;4;1;50\x07");
        terminal.Write("\x1B]9;9;/home/me\x07");

        notifications.Should().BeEmpty();
    }
}
