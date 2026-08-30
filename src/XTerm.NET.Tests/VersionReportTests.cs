using XTerm.Common;
using XTerm.Options;

namespace XTerm.Tests;

/// <summary>
/// XTVERSION -- "CSI &gt; Ps q" -- and the bug it used to trigger.
///
/// <para>It shares its final character with DECSCUSR, and the CSI identifier has its private marker
/// stripped before the command is looked up, so the query was routed to the cursor style handler.
/// Ps 0 landed in DECSCUSR's "blinking block" case, which is why asking a terminal what it was
/// changed the shape of the cursor and left it there.</para>
/// </summary>
[TestClass]
public class VersionReportTests
{
    private const string Esc = "\u001b";

    private static (Terminal Terminal, List<string> Replies) Listening()
    {
        var terminal = new Terminal(new TerminalOptions
        {
            Cols = 40,
            Rows = 6,
            CursorStyle = CursorStyle.Underline,
            CursorBlink = false
        });

        var replies = new List<string>();
        terminal.DataReceived += (_, e) => replies.Add(e.Data);
        return (terminal, replies);
    }

    /// <summary>The regression: asking for the version must not restyle the cursor.</summary>
    [TestMethod]
    public void A_version_query_leaves_the_cursor_alone()
    {
        var (terminal, _) = Listening();

        terminal.Write($"{Esc}[>0q");

        terminal.Options.CursorStyle.Should().Be(CursorStyle.Underline);
        terminal.Options.CursorBlink.Should().BeFalse();
    }

    /// <summary>And it must not do so by way of the event a host listens on either.</summary>
    [TestMethod]
    public void A_version_query_raises_no_cursor_style_change()
    {
        var (terminal, _) = Listening();
        var changes = 0;
        terminal.CursorStyleChanged += (_, _) => changes++;

        terminal.Write($"{Esc}[>0q");

        changes.Should().Be(0);
    }

    [TestMethod]
    public void The_version_is_reported()
    {
        var (terminal, replies) = Listening();

        terminal.Write($"{Esc}[>0q");

        var version = typeof(Terminal).Assembly.GetName().Version!;
        var expected = $"{version.Major}.{version.Minor}.{Math.Max(0, version.Build)}";

        replies.Should().ContainSingle().Which.Should().Be($"{Esc}P>|XTerm.NET({expected}){Esc}\\");
    }

    /// <summary>
    /// The reply is a DCS string, and a program reading it looks for that frame: "DCS &gt; |" to
    /// open and a string terminator to close. Getting either wrong leaves it waiting for a
    /// terminator that never comes, or splicing the reply into whatever it reads next.
    /// </summary>
    [TestMethod]
    public void The_reply_is_a_dcs_string()
    {
        var (terminal, replies) = Listening();

        terminal.Write($"{Esc}[>0q");

        var reply = replies.Should().ContainSingle().Which;
        reply.Should().StartWith($"{Esc}P>|");
        reply.Should().EndWith($"{Esc}\\");
    }

    /// <summary>An omitted parameter is Ps 0, which is the request.</summary>
    [TestMethod]
    public void An_omitted_parameter_is_the_version_request()
    {
        var (terminal, replies) = Listening();

        terminal.Write($"{Esc}[>q");

        replies.Should().ContainSingle().Which.Should().StartWith($"{Esc}P>|XTerm.NET(");
    }

    /// <summary>
    /// Ps 0 is the only request defined. A program that asked something else would read the version
    /// back as the answer to its own question, so nothing is sent.
    /// </summary>
    [TestMethod]
    [DataRow(1)]
    [DataRow(2)]
    public void An_unknown_request_is_not_answered(int ps)
    {
        var (terminal, replies) = Listening();

        terminal.Write($"{Esc}[>{ps}q");

        replies.Should().BeEmpty();
        terminal.Options.CursorStyle.Should().Be(CursorStyle.Underline);
    }

    /// <summary>Without the private marker it is still DECSCUSR, and still has to restyle.</summary>
    [TestMethod]
    [DataRow("5 q", CursorStyle.Bar, true)]
    [DataRow("2 q", CursorStyle.Block, false)]
    [DataRow("4 q", CursorStyle.Underline, false)]
    public void Decscusr_still_works(string sequence, CursorStyle style, bool blink)
    {
        var (terminal, replies) = Listening();

        terminal.Write($"{Esc}[{sequence}");

        terminal.Options.CursorStyle.Should().Be(style);
        terminal.Options.CursorBlink.Should().Be(blink);
        replies.Should().BeEmpty();
    }

    /// <summary>
    /// "CSI ? Ps q" is neither sequence. Reading it as XTVERSION would be a second wrong reading of
    /// the same final character, and reading it as DECSCUSR is the one this branch exists to stop.
    /// </summary>
    [TestMethod]
    public void A_question_marked_q_is_neither()
    {
        var (terminal, replies) = Listening();

        terminal.Write($"{Esc}[?2q");

        replies.Should().BeEmpty();
        terminal.Options.CursorStyle.Should().Be(CursorStyle.Underline);
        terminal.Options.CursorBlink.Should().BeFalse();
    }

    /// <summary>
    /// The marker is what tells the two apart, so it is read rather than merely detected --
    /// <c>IsPrivateMode</c> is true for both '?' and '&gt;' and cannot make the distinction.
    /// </summary>
    [TestMethod]
    [DataRow(">q", '>')]
    [DataRow("?h", '?')]
    [DataRow(" q", '\0')]
    [DataRow("m", '\0')]
    [DataRow("", '\0')]
    public void The_private_marker_is_readable(string identifier, char marker)
    {
        identifier.PrivateMarker().Should().Be(marker);
    }
}
