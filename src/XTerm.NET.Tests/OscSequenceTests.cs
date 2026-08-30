using XTerm;
using XTerm.Common;
using XTerm.Events;
using XTerm.Options;

namespace XTerm.Tests;

[TestClass]

public class OscSequenceTests
{
    private Terminal CreateTerminal(int cols = 80, int rows = 24)
    {
        var options = new TerminalOptions { Cols = cols, Rows = rows };
        return new Terminal(options);
    }

    [TestMethod]
    public void OscSetTitle_SetsTerminalTitle()
    {
        // Arrange
        var terminal = CreateTerminal();
        var titleChanged = false;
        string? newTitle = null;
        terminal.TitleChanged += (sender, e) =>
        {
            titleChanged = true;
            newTitle = e.Title;
        };

        // Act
        terminal.Write("\x1B]0;My Terminal Title\x07");

        // Assert
        terminal.Title.Should().Be("My Terminal Title");
        titleChanged.Should().BeTrue();
        newTitle.Should().Be("My Terminal Title");
    }

    [TestMethod]
    public void OscSetWindowTitle_SetsTerminalTitle()
    {
        // Arrange
        var terminal = CreateTerminal();

        // Act
        terminal.Write("\x1B]2;Window Title\x07");

        // Assert
        terminal.Title.Should().Be("Window Title");
    }

    [TestMethod]
    public void OscSetWindowTitle_WithEscTerminator_Works()
    {
        // Arrange
        var terminal = CreateTerminal();

        // Act
        terminal.Write("\x1B]2;Title with ESC terminator\x1B\\");

        // Assert
        terminal.Title.Should().Be("Title with ESC terminator");
    }

    [TestMethod]
    public void OscSetTitle_EmptyTitle_ClearsTitle()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.Write("\x1B]0;Initial Title\x07");

        // Act
        terminal.Write("\x1B]0;\x07");

        // Assert
        terminal.Title.Should().Be("");
    }

    [TestMethod]
    public void OscCurrentDirectory_SetsDirectory()
    {
        // Arrange
        var terminal = CreateTerminal();
        var directoryChanged = false;
        string? newDirectory = null;
        terminal.DirectoryChanged += (sender, e) =>
        {
            directoryChanged = true;
            newDirectory = e.Directory;
        };

        // Act
        terminal.Write("\x1B]7;file://localhost/home/user/projects\x07");

        // Assert
        terminal.CurrentDirectory.Should().Be("/home/user/projects");
        directoryChanged.Should().BeTrue();
        newDirectory.Should().Be("/home/user/projects");
    }

    [TestMethod]
    public void OscCurrentDirectory_WindowsPath_Works()
    {
        // Arrange
        var terminal = CreateTerminal();

        // Act
        terminal.Write("\x1B]7;file://localhost/C:/Users/Test\x07");

        // Assert
        terminal.CurrentDirectory.Should().Be("/C:/Users/Test");
    }

    [TestMethod]
    public void OscCurrentDirectory_UrlEncoded_Decodes()
    {
        // Arrange
        var terminal = CreateTerminal();

        // Act
        terminal.Write("\x1B]7;file://localhost/home/user/my%20folder\x07");

        // Assert
        terminal.CurrentDirectory.Should().Be("/home/user/my folder");
    }

    [TestMethod]
    public void OscHyperlink_StartLink_SetsHyperlink()
    {
        // Arrange
        var terminal = CreateTerminal();
        string? changedUrl = null;
        var isCleared = true;
        terminal.HyperlinkChanged += (sender, e) => changedUrl = e.Url;
        terminal.HyperlinkChanged += (sender, e) => isCleared = e.IsCleared;

        // Act
        terminal.Write("\x1B]8;;http://example.com\x07");

        // Assert
        terminal.CurrentHyperlink.Should().Be("http://example.com");
        changedUrl.Should().Be("http://example.com");
        isCleared.Should().BeFalse();
    }

    [TestMethod]
    public void OscHyperlink_EndLink_ClearsHyperlink()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.Write("\x1B]8;;http://example.com\x07");
        var eventCount = 0;
        string changedUrl = "not cleared";
        var isCleared = false;
        terminal.HyperlinkChanged += (sender, e) =>
        {
            eventCount++;
            changedUrl = e.Url;
            isCleared = e.IsCleared;
        };

        // Act
        terminal.Write("\x1B]8;;\x07");

        // Assert
        terminal.CurrentHyperlink.Should().BeNull();
        eventCount.Should().Be(1);
        changedUrl.Should().Be(string.Empty);
        isCleared.Should().BeTrue();
    }

    [TestMethod]
    public void OscHyperlink_WithId_SetsHyperlinkId()
    {
        // Arrange
        var terminal = CreateTerminal();

        // Act
        terminal.Write("\x1B]8;id=link123;http://example.com\x07");

        // Assert
        terminal.CurrentHyperlink.Should().Be("http://example.com");
        terminal.HyperlinkId.Should().Be("link123");
    }

    [TestMethod]
    public void OscHyperlink_CompleteSequence_Works()
    {
        // Arrange
        var terminal = CreateTerminal();

        // Act - Start link, print text, end link
        terminal.Write("\x1B]8;;https://github.com\x07");
        terminal.Write("GitHub");
        terminal.Write("\x1B]8;;\x07");

        // Assert
        terminal.CurrentHyperlink.Should().BeNull();
        var line = terminal.GetLine(0);
        line.Should().Contain("GitHub");
    }

    [TestMethod]
    public void OscColorQuery_Foreground_RespondsWithColor()
    {
        // Arrange
        var terminal = CreateTerminal();
        string? response = null;
        terminal.DataReceived += (sender, e) => response = e.Data;

        // Act
        terminal.Write("\x1B]10;?\x07");

        // Assert
        response.Should().NotBeNull();
        response.Should().Contain("rgb:");
        response.Should().Contain("]10;");
    }

    [TestMethod]
    public void OscColorQuery_Background_RespondsWithColor()
    {
        // Arrange
        var terminal = CreateTerminal();
        string? response = null;
        terminal.DataReceived += (sender, e) => response = e.Data;

        // Act
        terminal.Write("\x1B]11;?\x07");

        // Assert
        response.Should().NotBeNull();
        response.Should().Contain("rgb:");
        response.Should().Contain("]11;");
    }

    [TestMethod]
    public void OscColorQuery_Cursor_RespondsWithColor()
    {
        // Arrange
        var terminal = CreateTerminal();
        string? response = null;
        terminal.DataReceived += (sender, e) => response = e.Data;

        // Act
        terminal.Write("\x1B]12;?\x07");

        // Assert
        response.Should().NotBeNull();
        response.Should().Contain("rgb:");
        response.Should().Contain("]12;");
    }

    [TestMethod]
    public void OscClipboard_Query_ReturnsHostDataWhenEnabled()
    {
        // Arrange
        var terminal = new Terminal(new TerminalOptions
        {
            ClipboardReadEnabled = true
        });
        string? response = null;
        terminal.DataReceived += (sender, e) => response = e.Data;
        terminal.ClipboardReadRequested += (_, e) => e.Data = System.Text.Encoding.UTF8.GetBytes("Hello");

        // Act
        terminal.Write("\x1B]52;c;?\x07");

        // Assert
        response.Should().NotBeNull();
        response.Should().Be("\x1B]52;c;SGVsbG8=\x07");
    }

    [TestMethod]
    public void OscClipboard_Query_IsIgnoredByDefault()
    {
        // Reads are opt-in, and a disabled read answers NOTHING — the host is not even asked.
        var terminal = new Terminal(new TerminalOptions());
        string? response = null;
        var readRequested = false;
        terminal.DataReceived += (sender, e) => response = e.Data;
        terminal.ClipboardReadRequested += (_, _) => readRequested = true;

        // Act
        terminal.Write("\x1B]52;c;?\x07");

        // Assert
        response.Should().BeNull();
        readRequested.Should().BeFalse();
    }

    [TestMethod]
    public void OscClipboard_SetData_RaisesWriteRequest()
    {
        // Arrange
        var terminal = CreateTerminal();
        TerminalEvents.ClipboardWriteEventArgs? request = null;
        terminal.ClipboardWriteRequested += (_, e) => request = e;
        var base64Data = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("Hello, World!"));

        // Act
        terminal.Write($"\x1B]52;c;{base64Data}\x07");

        // Assert
        request.Should().NotBeNull();
        request.Target.Should().Be("c");
        request.Text.Should().Be("Hello, World!");
    }

    [TestMethod]
    public void OscClipboard_Query_WhenEnabledAndHandled_ReturnsClipboardText()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.Options.ClipboardReadEnabled = true;
        string? response = null;
        terminal.ClipboardReadRequested += (_, e) =>
        {
            e.Target.Should().Be("p");
            e.Text = "Hello, World!";
        };
        terminal.DataReceived += (_, e) => response = e.Data;

        // Act
        terminal.Write("\x1B]52;p;?\x07");

        // Assert
        response.Should().Be("\x1B]52;p;SGVsbG8sIFdvcmxkIQ==\x07");
    }

    [TestMethod]
    public void OscClipboard_Query_CanBeAnsweredAfterTheHandlerReturns()
    {
        // The async-host path: an Avalonia clipboard is awaited, so the handler returns first
        // and answers later. The response must be byte-identical to the synchronous one.
        var terminal = new Terminal(new TerminalOptions { ClipboardReadEnabled = true });
        string? response = null;
        terminal.DataReceived += (_, e) => response = e.Data;
        TerminalEvents.ClipboardReadEventArgs? pending = null;
        terminal.ClipboardReadRequested += (_, e) => { e.Defer(); pending = e; };

        terminal.Write("\u001b]52;c;?\u0007");
        response.Should().BeNull();                        // nothing answered yet...
        pending!.Respond("deferred");
        response.Should().Be("\u001b]52;c;ZGVmZXJyZWQ=\u0007");

        response = null;
        pending.Respond("again");                     // a second call is ignored
        response.Should().BeNull();
    }

    [TestMethod]
    public void OscClipboard_DeferredDecline_StaysSilent()
    {
        var terminal = new Terminal(new TerminalOptions { ClipboardReadEnabled = true });
        string? response = null;
        terminal.DataReceived += (_, e) => response = e.Data;
        TerminalEvents.ClipboardReadEventArgs? pending = null;
        terminal.ClipboardReadRequested += (_, e) => { e.Defer(); pending = e; };

        terminal.Write("\u001b]52;c;?\u0007");
        pending!.Respond((string?)null);
        response.Should().BeNull();
    }

    [TestMethod]
    public void OscClipboard_RespondInsideTheHandlerPlusHandled_EmitsOnce()
    {
        // Off-contract but easy to write: a handler that serves from a cache calls Respond
        // synchronously AND sets Handled. The single-response guarantee must hold — whoever
        // claims the response first wins, and the other path stays silent.
        var terminal = new Terminal(new TerminalOptions { ClipboardReadEnabled = true });
        var responses = new List<string>();
        terminal.DataReceived += (_, e) => responses.Add(e.Data);
        terminal.ClipboardReadRequested += (_, e) =>
        {
            e.Respond("cached");
            e.Text = "sync";
        };

        terminal.Write("\u001b]52;c;?\u0007");

        responses.Should().ContainSingle();
        responses[0].Should().Be("\u001b]52;c;Y2FjaGVk\u0007");
    }

    [TestMethod]
    public void OscClipboard_SyncAnswerDisarmsRespond()
    {
        // Handled synchronously answers as the handler returns; Respond afterwards must not
        // produce a SECOND response.
        var terminal = new Terminal(new TerminalOptions { ClipboardReadEnabled = true });
        var responses = new List<string>();
        terminal.DataReceived += (_, e) => responses.Add(e.Data);
        TerminalEvents.ClipboardReadEventArgs? pending = null;
        terminal.ClipboardReadRequested += (_, e) => { e.Text = "sync"; pending = e; };

        terminal.Write("\u001b]52;c;?\u0007");
        pending!.Respond("late");
        responses.Should().ContainSingle();
        responses[0].Should().Be("\u001b]52;c;c3luYw==\u0007");
    }

    [TestMethod]
    public void OscClipboard_Query_WhenEnabledAndDeclined_DoesNotRespond()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.Options.ClipboardReadEnabled = true;
        string? response = null;
        var raised = false;
        terminal.ClipboardReadRequested += (_, e) =>
        {
            raised = true;
            e.Target.Should().Be("s");
        };
        terminal.DataReceived += (_, e) => response = e.Data;

        // Act
        terminal.Write("\x1B]52;s;?\x07");

        // Assert
        raised.Should().BeTrue();
        response.Should().BeNull();
    }

    [TestMethod]
    public void OscClipboard_SetInvalidData_RaisesClearRequest()
    {
        // Arrange
        var terminal = CreateTerminal();
        TerminalEvents.ClipboardWriteEventArgs? request = null;
        terminal.ClipboardWriteRequested += (_, e) => request = e;

        // Act
        terminal.Write("\x1B]52;c;!\x07");

        // Assert
        request.Should().NotBeNull();
        request.Target.Should().Be("c");
        request.Text.Should().Be(string.Empty);
    }

    [TestMethod]
    public void OscClipboard_EmptyTarget_DefaultsToSelectionZero()
    {
        // Arrange
        var terminal = CreateTerminal();
        TerminalEvents.ClipboardWriteEventArgs? request = null;
        terminal.ClipboardWriteRequested += (_, e) => request = e;
        var base64Data = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("Hello, World!"));

        // Act
        terminal.Write($"\x1B]52;;{base64Data}\x07");

        // Assert
        request.Should().NotBeNull();
        request.Target.Should().Be("s0");
    }

    [TestMethod]
    public void OscClipboard_InvalidTarget_IsIgnored()
    {
        // Arrange
        var terminal = CreateTerminal();
        var raised = false;
        terminal.ClipboardWriteRequested += (_, _) => raised = true;
        var base64Data = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("Hello, World!"));

        // Act
        terminal.Write($"\x1B]52;x;{base64Data}\x07");

        // Assert
        raised.Should().BeFalse();
    }

    [TestMethod]
    public void OscClipboard_SetData_PropagatesHostExceptions()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.ClipboardWriteRequested += (_, _) => throw new InvalidOperationException();
        var base64Data = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("Hello, World!"));

        // Act & Assert
        Assert.ThrowsExactly<InvalidOperationException>(() => terminal.Write($"\x1B]52;c;{base64Data}\x07"));
    }

    [TestMethod]
    public void OscClipboard_SetData_WhenDisabled_IsIgnored()
    {
        // Arrange
        var terminal = CreateTerminal();
        terminal.Options.ClipboardWriteEnabled = false;
        var raised = false;
        terminal.ClipboardWriteRequested += (_, _) => raised = true;
        var base64Data = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("Hello, World!"));

        // Act
        terminal.Write($"\x1B]52;s;{base64Data}\x07");

        // Assert
        raised.Should().BeFalse();
    }

    [TestMethod]
    public void OscKittyClipboard_WriteChunks_RaisesClipboardWriteRequested()
    {
        // Arrange
        var terminal = CreateTerminal();
        TerminalEvents.ClipboardWriteEventArgs? request = null;
        string? response = null;
        terminal.ClipboardWriteRequested += (_, e) => request = e;
        terminal.DataReceived += (_, e) => response = e.Data;

        // Act
        terminal.Write("\x1B]5522;type=write\x1B\\");
        terminal.Write("\x1B]5522;type=wdata:mime=dGV4dC9wbGFpbg==;aGVsbG8=\x07");
        terminal.Write("\x1B]5522;type=wdata\x1B\\");

        // Assert
        request.Should().NotBeNull();
        (request!.Target).Should().Be("c");
        request.MimeType.Should().Be("text/plain");
        request.Text.Should().Be("hello");
        response.Should().Be("\x1B]5522;type=write:status=DONE\x1B\\");
    }

    [TestMethod]
    public void OscKittyClipboard_Write_PreservesMimeTypeAndBinaryData()
    {
        // Arrange
        var terminal = CreateTerminal();
        TerminalEvents.ClipboardWriteEventArgs? request = null;
        terminal.ClipboardWriteRequested += (_, e) => request = e;

        // Act
        terminal.Write("\x1B]5522;type=write\x1B\\");
        terminal.Write("\x1B]5522;type=wdata:mime=aW1hZ2UvcG5n;AP+A\x07");
        terminal.Write("\x1B]5522;type=wdata\x1B\\");

        // Assert
        request.Should().NotBeNull();
        (request!.MimeType).Should().Be("image/png");
        request.Data.Should().Equal([0x00, 0xFF, 0x80]);
    }

    [TestMethod]
    public void OscKittyClipboard_WriteStart_ReplacesAbandonedTransfer()
    {
        // Arrange
        var terminal = CreateTerminal();
        string? text = null;
        terminal.ClipboardWriteRequested += (_, e) => text = e.Text;

        // Act
        terminal.Write("\x1B]5522;type=write\x1B\\");
        terminal.Write("\x1B]5522;type=wdata:mime=dGV4dC9wbGFpbg==;b2xk\x1B\\");
        terminal.Write("\x1B]5522;type=write\x1B\\");
        terminal.Write("\x1B]5522;type=wdata:mime=dGV4dC9wbGFpbg==;bmV3\x1B\\");
        terminal.Write("\x1B]5522;type=wdata\x1B\\");

        // Assert
        text.Should().Be("new");
    }

    [TestMethod]
    public void OscKittyClipboard_Write_CommitsEveryMimeTypeInOneEvent()
    {
        // Platform clipboards replace their contents on each set, so the transfer arrives as ONE
        // event carrying every format — the host builds one data object and commits once.
        var terminal = new Terminal(new TerminalOptions());
        var events = new List<TerminalEvents.ClipboardWriteEventArgs>();
        terminal.ClipboardWriteRequested += (_, e) => events.Add(e);
        string? response = null;
        terminal.DataReceived += (_, e) => response = e.Data;

        var plainMime = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("text/plain"));
        var htmlMime = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("text/html"));
        terminal.Write("\u001b]5522;type=write:id=w1\u001b\\");
        terminal.Write($"\u001b]5522;type=wdata:mime={plainMime};{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("hello"))}\u001b\\");
        terminal.Write($"\u001b]5522;type=wdata:mime={htmlMime};{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("<b>hello</b>"))}\u001b\\");
        terminal.Write("\u001b]5522;type=wdata\u001b\\");

        var e = events.Should().ContainSingle().Which;
        e.Formats.Count.Should().Be(2);
        e.Formats[0].MimeType.Should().Be("text/plain");
        System.Text.Encoding.UTF8.GetString(e.Formats[0].Data).Should().Be("hello");
        e.Formats[1].MimeType.Should().Be("text/html");
        System.Text.Encoding.UTF8.GetString(e.Formats[1].Data).Should().Be("<b>hello</b>");
        e.Text.Should().Be("hello");                       // the text/* convenience
        response.Should().Be("\u001b]5522;type=write:status=DONE:id=w1\u001b\\");
    }

    [TestMethod]
    public void OscKittyClipboard_WriteAlias_RidesTheSameEventWithTargetData()
    {
        var terminal = new Terminal(new TerminalOptions());
        var events = new List<TerminalEvents.ClipboardWriteEventArgs>();
        terminal.ClipboardWriteRequested += (_, e) => events.Add(e);

        var plainMime = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("text/plain"));
        var aliasList = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("UTF8_STRING"));
        terminal.Write("\u001b]5522;type=write\u001b\\");
        terminal.Write($"\u001b]5522;type=wdata:mime={plainMime};{Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("hi"))}\u001b\\");
        terminal.Write($"\u001b]5522;type=walias:mime={plainMime};{aliasList}\u001b\\");
        terminal.Write("\u001b]5522;type=wdata\u001b\\");

        var e = events.Should().ContainSingle().Which;
        e.Formats.Count.Should().Be(2);
        e.Formats[0].MimeType.Should().Be("text/plain");
        e.Formats[1].MimeType.Should().Be("UTF8_STRING");
        e.Formats[1].Data.Should().Equal(e.Formats[0].Data);  // the alias shares the target's bytes
    }

    [TestMethod]
    public void OscKittyClipboard_WriteError_EchoesIdAndIgnoresStrayData()
    {
        // Arrange
        var terminal = CreateTerminal();
        var responses = new List<string>();
        terminal.DataReceived += (_, e) => responses.Add(e.Data);

        // Act
        terminal.Write("\x1B]5522;type=write:id=w1;\x1B\\");
        terminal.Write("\x1B]5522;type=wdata:mime=dGV4dC9wbGFpbg==;invalid!\x1B\\");
        terminal.Write("\x1B]5522;type=wdata:mime=dGV4dC9wbGFpbg==;dGV4dA==\x1B\\");

        // Assert
        responses.Should().Equal(["\x1B]5522;type=write:status=EINVAL:id=w1\x1B\\"]);
    }

    [TestMethod]
    public void OscKittyClipboard_WriteHonorsConfiguredSizeLimit()
    {
        // Arrange
        var terminal = new Terminal(new TerminalOptions { MaxClipboardBytes = 1 });
        string? response = null;
        terminal.DataReceived += (_, e) => response = e.Data;

        // Act
        terminal.Write("\x1B]5522;type=write:id=w1\x1B\\");
        terminal.Write("\x1B]5522;type=wdata:mime=dGV4dC9wbGFpbg==;dHc=\x1B\\");

        // Assert
        response.Should().Be("\x1B]5522;type=write:status=EIO:id=w1\x1B\\");
    }

    [TestMethod]
    public void OscKittyClipboard_AliasLimitReturnsEfbig()
    {
        // Arrange
        var terminal = new Terminal(new TerminalOptions { MaxClipboardBytes = 1 });
        string? response = null;
        terminal.DataReceived += (_, e) => response = e.Data;

        // Act
        terminal.Write("\x1B]5522;type=write:id=w1\x1B\\");
        terminal.Write("\x1B]5522;type=walias:mime=dGV4dC9wbGFpbg==;VVRGOF9TVFJJTkc=\x1B\\");

        // Assert
        response.Should().Be("\x1B]5522;type=write:status=EIO:id=w1\x1B\\");
    }

    [TestMethod]
    public void OscKittyClipboard_MimeEntriesCountAgainstTransferLimit()
    {
        // Arrange
        var terminal = new Terminal(new TerminalOptions { MaxClipboardBytes = 600 });
        string? response = null;
        terminal.DataReceived += (_, e) => response = e.Data;

        // Act
        terminal.Write("\x1B]5522;type=write:id=w1\x1B\\");
        terminal.Write("\x1B]5522;type=wdata:mime=dGV4dC9h;\x1B\\");
        terminal.Write("\x1B]5522;type=wdata:mime=dGV4dC9i;\x1B\\");
        terminal.Write("\x1B]5522;type=wdata:mime=dGV4dC9j;\x1B\\");

        // Assert
        response.Should().Be("\x1B]5522;type=write:status=EIO:id=w1\x1B\\");
    }

    [TestMethod]
    public void OscKittyClipboard_Read_RequiresOptInAndReturnsHostData()
    {
        // Arrange
        var terminal = new Terminal(new TerminalOptions
        {
            ClipboardReadEnabled = true
        });
        var responses = new List<string>();
        terminal.DataReceived += (_, e) => responses.Add(e.Data);
        string? target = null;
        terminal.ClipboardReadRequested += (_, e) =>
        {
            target = e.Target;
            e.Data = System.Text.Encoding.UTF8.GetBytes("hello");
        };

        // Act
        terminal.Write("\x1B]5522;type=read:loc=primary:id=r1;dGV4dC9wbGFpbg==\x1B\\");

        // Assert
        responses.Should().Equal([
                "\x1B]5522;type=read:status=OK:id=r1\x1B\\",
                "\x1B]5522;type=read:status=DATA:mime=dGV4dC9wbGFpbg==:id=r1;aGVsbG8=\x1B\\",
                "\x1B]5522;type=read:status=DONE:id=r1\x1B\\"
            ]);
        target.Should().Be("p");
    }

    [TestMethod]
    public void OscKittyClipboard_Read_UsesAnyRequestedMimeType()
    {
        // Arrange
        var terminal = new Terminal(new TerminalOptions { ClipboardReadEnabled = true });
        var responses = new List<string>();
        terminal.DataReceived += (_, e) => responses.Add(e.Data);
        terminal.ClipboardReadRequested += (_, e) =>
        {
            if (e.MimeType == "text/plain")
                e.Data = System.Text.Encoding.UTF8.GetBytes("text");
        };

        // Act
        terminal.Write("\x1B]5522;type=read;dGV4dC9odG1sIHRleHQvcGxhaW4=\x1B\\");

        // Assert
        responses.Should().Contain("\x1B]5522;type=read:status=DATA:mime=dGV4dC9wbGFpbg==;dGV4dA==\x1B\\");
    }

    [TestMethod]
    public void OscKittyClipboard_ReadTypeList_DecodesDotPayload()
    {
        // Arrange
        var terminal = new Terminal(new TerminalOptions { ClipboardReadEnabled = true });
        string? requestedMimeType = null;
        terminal.ClipboardReadRequested += (_, e) =>
        {
            requestedMimeType = e.MimeType;
            e.Data = System.Text.Encoding.UTF8.GetBytes("text/plain");
        };

        // Act
        terminal.Write("\x1B]5522;type=read;Lg==\x1B\\");

        // Assert
        requestedMimeType.Should().Be(".");
    }

    [TestMethod]
    public void OscKittyClipboard_ReadDisabled_DoesNotRequestOrRespond()
    {
        // Arrange
        var terminal = CreateTerminal();
        var requested = false;
        string? response = null;
        terminal.ClipboardReadRequested += (_, _) => requested = true;
        terminal.DataReceived += (_, e) => response = e.Data;

        // Act
        terminal.Write("\x1B]5522;type=read;dGV4dC9wbGFpbg==\x07");

        // Assert
        requested.Should().BeFalse();
        response.Should().Be("\x1B]5522;type=read:status=EPERM\x1B\\");
    }

    [TestMethod]
    public void OscKittyClipboard_Read_CanBeAnsweredAfterTheHandlerReturns()
    {
        // The async-host path: the reply cannot begin until every requested mime resolves, so a
        // mixed sync/deferred pair emits nothing until the deferred one lands — then the whole
        // OK/DATA/DONE flow goes out in order.
        var terminal = new Terminal(new TerminalOptions { ClipboardReadEnabled = true });
        var responses = new List<string>();
        terminal.DataReceived += (_, e) => responses.Add(e.Data);
        TerminalEvents.ClipboardReadEventArgs? pending = null;
        terminal.ClipboardReadRequested += (_, e) =>
        {
            if (e.MimeType == "text/plain")
                e.Text = "sync";
            else
            {
                e.Defer();
                pending = e;
            }
        };

        var mimes = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("text/plain text/html"));
        terminal.Write($"\u001b]5522;type=read:id=r1;{mimes}\u001b\\");
        responses.Should().BeEmpty();

        pending!.Respond("<b>late</b>");

        responses[0].Should().Be("\u001b]5522;type=read:status=OK:id=r1\u001b\\");
        responses[1].Should().Contain("status=DATA");
        responses[2].Should().Contain("status=DATA");
        (responses[^1]).Should().Be("\u001b]5522;type=read:status=DONE:id=r1\u001b\\");
        responses.Count.Should().Be(4);
    }

    [TestMethod]
    public void OscKittyClipboard_DeferredDeclineOfEveryMime_AnswersEperm()
    {
        // Unlike OSC 52, 5522 must ANSWER a decline: all-deferred, all-null resolves to EPERM.
        var terminal = new Terminal(new TerminalOptions { ClipboardReadEnabled = true });
        var responses = new List<string>();
        terminal.DataReceived += (_, e) => responses.Add(e.Data);
        var pending = new List<TerminalEvents.ClipboardReadEventArgs>();
        terminal.ClipboardReadRequested += (_, e) => { e.Defer(); pending.Add(e); };

        var mimes = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("text/plain"));
        terminal.Write($"\u001b]5522;type=read:id=r2;{mimes}\u001b\\");
        responses.Should().BeEmpty();

        pending.Single().Respond((byte[]?)null);

        responses.Single().Should().Be("\u001b]5522;type=read:status=EPERM:id=r2\u001b\\");
    }

    [TestMethod]
    public void OscColorPalette_Change_DoesNotThrow()
    {
        // Arrange
        var terminal = CreateTerminal();

        // Act & Assert - Should not throw
        terminal.Write("\x1B]4;1;rgb:ff/00/00\x07"); // Set color 1 to red
    }

    [TestMethod]
    public void OscColorReset_DoesNotThrow()
    {
        // Arrange
        var terminal = CreateTerminal();

        // Act & Assert - Should not throw
        terminal.Write("\x1B]104;1\x07"); // Reset color 1
        terminal.Write("\x1B]104\x07");   // Reset all colors
    }

    [TestMethod]
    public void OscMultipleSequences_AllProcessed()
    {
        // Arrange
        var terminal = CreateTerminal();
        var titleChangeCount = 0;
        var directoryChangeCount = 0;
        terminal.TitleChanged += (sender, e) => titleChangeCount++;
        terminal.DirectoryChanged += (sender, e) => directoryChangeCount++;

        // Act
        terminal.Write("\x1B]0;Title1\x07");
        terminal.Write("\x1B]7;file://localhost/path1\x07");
        terminal.Write("\x1B]0;Title2\x07");
        terminal.Write("\x1B]7;file://localhost/path2\x07");

        // Assert
        terminal.Title.Should().Be("Title2");
        terminal.CurrentDirectory.Should().Be("/path2");
        titleChangeCount.Should().Be(2);
        directoryChangeCount.Should().Be(2);
    }

    [TestMethod]
    public void OscWithText_InterleavedCorrectly()
    {
        // Arrange
        var terminal = CreateTerminal();

        // Act
        terminal.Write("Before ");
        terminal.Write("\x1B]0;Test Title\x07");
        terminal.Write("After");

        // Assert
        terminal.Title.Should().Be("Test Title");
        var line = terminal.GetLine(0);
        line.Should().Contain("Before After");
    }

    [TestMethod]
    public void OscInvalidSequence_DoesNotCrash()
    {
        // Arrange
        var terminal = CreateTerminal();

        // Act & Assert - Should not throw
        terminal.Write("\x1B]999;invalid\x07");
        terminal.Write("\x1B]\x07");
        terminal.Write("\x1B];\x07");
    }

    [TestMethod]
    public void OscHyperlink_MultipleParams_ParsesCorrectly()
    {
        // Arrange
        var terminal = CreateTerminal();

        // Act
        terminal.Write("\x1B]8;id=abc:key=value;http://test.com\x07");

        // Assert
        terminal.CurrentHyperlink.Should().Be("http://test.com");
        terminal.HyperlinkId.Should().Be("abc");
    }

    [TestMethod]
    public void OscDirectoryChange_MultipleEvents_FiresEachTime()
    {
        // Arrange
        var terminal = CreateTerminal();
        var paths = new List<string>();
        terminal.DirectoryChanged += (sender, e) => paths.Add(e.Directory);

        // Act
        terminal.Write("\x1B]7;file://localhost/home\x07");
        terminal.Write("\x1B]7;file://localhost/usr\x07");
        terminal.Write("\x1B]7;file://localhost/var\x07");

        // Assert
        paths.Count.Should().Be(3);
        paths[0].Should().Be("/home");
        paths[1].Should().Be("/usr");
        paths[2].Should().Be("/var");
    }

    [TestMethod]
    public void OscTitleChange_SpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var terminal = CreateTerminal();

        // Act
        terminal.Write("\x1B]0;Title with émojis 😀 and spëcial chars\x07");

        // Assert
        terminal.Title.Should().Be("Title with émojis 😀 and spëcial chars");
    }

    [TestMethod]
    public void OscHyperlink_ComplexUrl_PreservesUrl()
    {
        // Arrange
        var terminal = CreateTerminal();

        // Act
        terminal.Write("\x1B]8;;https://example.com/path?param=value&other=123#anchor\x07");

        // Assert
        terminal.CurrentHyperlink.Should().Be("https://example.com/path?param=value&other=123#anchor");
    }

    [TestMethod]
    public void OscEmptyCommand_DoesNotCrash()
    {
        // Arrange
        var terminal = CreateTerminal();

        // Act & Assert - Should not throw
        terminal.Write("\x1B]\x07");
    }

    [TestMethod]
    public void OscColorQueries_Sequential_AllRespond()
    {
        // Arrange
        var terminal = CreateTerminal();
        var responses = new List<string>();
        terminal.DataReceived += (sender, e) => responses.Add(e.Data);

        // Act
        terminal.Write("\x1B]10;?\x07");
        terminal.Write("\x1B]11;?\x07");
        terminal.Write("\x1B]12;?\x07");

        // Assert
        responses.Count.Should().Be(3);
        responses.Should().AllSatisfy(r => r.Should().Contain("rgb:"));
    }
}
