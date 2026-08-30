using XTerm.Events;
using XTerm.Options;

namespace XTerm.Tests;

[TestClass]

public class KittyNotificationTests
{
    private const string Esc = "\x1b";
    private const string Bel = "\x07";
    private const string St = "\x1b\\";

    /// <summary>
    /// Notifications are opt-in, so every test that expects one asks for them.
    /// </summary>
    private static Terminal CreateTerminal() =>
        new(new TerminalOptions { KittyNotificationsEnabled = true });

    [TestMethod]
    public void Osc99_RaisesStructuredNotification_WhenComplete()
    {
        var terminal = CreateTerminal();
        TerminalEvents.NotificationEventArgs? notification = null;
        terminal.NotificationReceived += (_, e) => notification = e;

        terminal.Write($"{Esc}]99;i=build-42:p=title:d=0:e=1:u=2:n=YnVpbGQ=;QnVpbGQ={St}");
        terminal.Write($"{Esc}]99;i=build-42:p=body:d=1:e=1;IGZpbmlzaGVk{St}");

        notification.Should().NotBeNull();
        (notification!.Identifier).Should().Be("build-42");
        notification.Title.Should().Be("Build");
        notification.Body.Should().Be(" finished");
        notification.Text.Should().Be(" finished");
        notification.Urgency.Should().Be(2);
        notification.Icon.Should().Be("build");
    }

    [TestMethod]
    public void Osc99_AppendsMultipartPayload()
    {
        var terminal = CreateTerminal();
        TerminalEvents.NotificationEventArgs? notification = null;
        terminal.NotificationReceived += (_, e) => notification = e;

        terminal.Write($"{Esc}]99;i=build:p=body:d=0:e=1;QnVpbGQ={Bel}");
        terminal.Write($"{Esc}]99;i=build:p=body:d=1:e=1;IGZpbmlzaGVk{Bel}");

        notification.Should().NotBeNull();
        // Body-only chunks: the assembled body is PROMOTED to the title, per the spec's
        // "if a notification has no title, the body will be used as title."
        (notification!.Title).Should().Be("Build finished");
        notification.Body.Should().BeNull();
    }

    [TestMethod]
    public void Osc99_DefaultsToPlainTextTitle()
    {
        var terminal = CreateTerminal();
        TerminalEvents.NotificationEventArgs? notification = null;
        terminal.NotificationReceived += (_, e) => notification = e;

        terminal.Write($"{Esc}]99;;Hello world{St}");

        notification.Should().NotBeNull();
        (notification!.Title).Should().Be("Hello world");
        notification.Body.Should().BeNull();
        notification.Text.Should().Be("Hello world");
    }

    [TestMethod]
    public void Osc99_CompleteNotificationBypassesPendingIdentifierLimit()
    {
        var terminal = CreateTerminal();
        TerminalEvents.NotificationEventArgs? notification = null;
        terminal.NotificationReceived += (_, e) => notification = e;

        for (var i = 0; i < 16; i++)
            terminal.Write($"{Esc}]99;i=pending-{i}:d=0;partial{St}");

        terminal.Write($"{Esc}]99;;Hello world{St}");

        notification.Should().NotBeNull();
        (notification!.Title).Should().Be("Hello world");
    }

    [TestMethod]
    public void Osc99_AnswersCapabilityQuery()
    {
        var terminal = CreateTerminal();
        string? response = null;
        terminal.DataReceived += (_, e) => response = e.Data;

        terminal.Write($"{Esc}]99;i=query:p=?;{St}");

        response.Should().Be($"{Esc}]99;i=query:p=?;a=notify:o=always:u=0,1,2:p=title,body{St}");
    }

    [TestMethod]
    public void Osc99_AnswersCapabilityQuery_WithoutPayloadSeparator()
    {
        // The form every real detector sends (blessed, and ucs-detect through it): metadata only,
        // no ';' before the terminator. Requiring the separator made support undetectable.
        var terminal = CreateTerminal();
        string? response = null;
        terminal.DataReceived += (_, e) => response = e.Data;

        terminal.Write($"{Esc}]99;i=blessed:p=?{St}");

        response.Should().Be($"{Esc}]99;i=blessed:p=?;a=notify:o=always:u=0,1,2:p=title,body{St}");
    }

    [TestMethod]
    public void Osc99_MetadataOnly_WithoutQuery_ShowsNothing()
    {
        var terminal = CreateTerminal();
        var notifications = new List<TerminalEvents.NotificationEventArgs>();
        terminal.NotificationReceived += (_, e) => notifications.Add(e);

        terminal.Write($"{Esc}]99;i=x:d=1{St}");

        notifications.Should().BeEmpty();
    }

    [TestMethod]
    public void Osc99_Query_StaysSilent_WhenDisabled()
    {
        // Deliberate: refusing the query while the gate is off keeps well-behaved applications
        // from notifying into the void.
        var terminal = new Terminal(new TerminalOptions { KittyNotificationsEnabled = false });
        string? response = null;
        terminal.DataReceived += (_, e) => response = e.Data;

        terminal.Write($"{Esc}]99;i=blessed:p=?{St}");

        response.Should().BeNull();
    }

    [TestMethod]
    public void Osc99_DoesNotRaise_WhenDisabled()
    {
        var terminal = new Terminal(new TerminalOptions { KittyNotificationsEnabled = false });
        var notifications = new List<TerminalEvents.NotificationEventArgs>();
        terminal.NotificationReceived += (_, e) => notifications.Add(e);

        terminal.Write($"{Esc}]99;p=body;SGVsbG8={Bel}");

        notifications.Should().BeEmpty();
    }

    [TestMethod]
    public void Osc99_WorksByDefault()
    {
        // Display-only, so on by default like kitty, Ghostty, foot, WezTerm and iTerm2 — and the
        // default decides discoverability, because detectors read a refused p=? as "unsupported".
        var terminal = new Terminal(new TerminalOptions());
        var notifications = new List<TerminalEvents.NotificationEventArgs>();
        terminal.NotificationReceived += (_, e) => notifications.Add(e);

        terminal.Write($"{Esc}]99;;Hello world{St}");

        notifications.Should().ContainSingle();
        notifications[0].Title.Should().Be("Hello world");
    }

    [TestMethod]
    public void Osc99_BodyOnlyNotification_PromotesBodyToTitle()
    {
        // The spec: "If a notification has no title, the body will be used as title."
        var terminal = CreateTerminal();
        TerminalEvents.NotificationEventArgs? notification = null;
        terminal.NotificationReceived += (_, e) => notification = e;

        terminal.Write($"{Esc}]99;p=body;Only a body{St}");

        notification.Should().NotBeNull();
        (notification!.Title).Should().Be("Only a body");
        notification.Body.Should().BeNull();
        notification.Text.Should().Be("Only a body");
    }

    [TestMethod]
    public void Osc99_OutOfRangeUrgency_ReadsAsUnspecified()
    {
        // u is exactly 0, 1 or 2; u=999 must not escape into the public event.
        var terminal = CreateTerminal();
        TerminalEvents.NotificationEventArgs? notification = null;
        terminal.NotificationReceived += (_, e) => notification = e;

        terminal.Write($"{Esc}]99;u=999;Hello{St}");

        notification.Should().NotBeNull();
        (notification!.Urgency).Should().BeNull();
    }
}
