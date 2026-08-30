using XTerm.Options;

namespace XTerm.Tests.Graphics;

/// <summary>
/// XTSMGRAPHICS -- "CSI ? Pi ; Pa ; Pv S" -- and the bug it used to trigger.
///
/// <para>It shares its final character with SCROLL UP, and the CSI identifier used to have its
/// private marker stripped before the command was looked up, so the query was routed to the scroll
/// handler. Every Sixel-capable program sends one while working out what the terminal can do, which
/// made "the screen jumps when I run img2sixel" the visible symptom of a capability query going
/// unanswered. "?S" is now its own entry in the command table.</para>
/// </summary>
[TestClass]
public class GraphicsAttributesTests
{
    private const string Esc = "\u001b";

    private static Terminal Fresh() => new(new TerminalOptions
    {
        Cols = 40,
        Rows = 6,
        CellWidthPixels = 10,
        CellHeightPixels = 20
    });

    private static (Terminal Terminal, List<string> Replies) Listening()
    {
        var terminal = Fresh();
        var replies = new List<string>();
        terminal.DataReceived += (_, e) => replies.Add(e.Data);
        return (terminal, replies);
    }

    /// <summary>The regression: a capability query must not move the screen.</summary>
    [TestMethod]
    [DataRow("1;1;0", "colour register count")]
    [DataRow("2;1;0", "Sixel geometry")]
    [DataRow("1;4;0", "maximum colour registers")]
    public void A_graphics_query_does_not_scroll_the_screen(string parameters, string what)
    {
        var (terminal, _) = Listening();
        terminal.Write("top line\r\nsecond line");

        terminal.Write($"{Esc}[?{parameters}S");

        (terminal.GetLine(terminal.Buffer.YBase) == "top line").Should().BeTrue($"querying {what} scrolled the screen instead of answering");
    }

    [TestMethod]
    public void The_colour_register_count_is_reported()
    {
        var (terminal, replies) = Listening();

        terminal.Write($"{Esc}[?1;1;0S");

        replies.Should().ContainSingle().Which.Should().Be($"{Esc}[?1;0;256S");
    }

    [TestMethod]
    public void The_sixel_geometry_is_reported()
    {
        var (terminal, replies) = Listening();

        terminal.Write($"{Esc}[?2;1;0S");

        // 40 columns of 10 pixels, and whatever height the pixel budget allows across that width.
        var reply = replies.Should().ContainSingle().Which;
        reply.Should().StartWith($"{Esc}[?2;0;400;");
        reply.Should().EndWith("S");
    }

    /// <summary>
    /// The reported geometry has to be a size we would actually accept, or a program that sizes an
    /// image to fit gets one we then throw away.
    /// </summary>
    [TestMethod]
    public void The_reported_geometry_fits_within_the_pixel_budget()
    {
        var (terminal, replies) = Listening();

        terminal.Write($"{Esc}[?2;1;0S");

        var parts = replies[0].TrimEnd('S').Split(';');
        var width = int.Parse(parts[^2]);
        var height = int.Parse(parts[^1]);

        (((long)width * height <= terminal.Options.MaxSixelPixels)).Should().BeTrue($"reported {width}x{height}, which is larger than the {terminal.Options.MaxSixelPixels} pixel budget");
    }

    [TestMethod]
    public void An_unknown_item_is_refused()
    {
        var (terminal, replies) = Listening();

        terminal.Write($"{Esc}[?9;1;0S");

        replies.Should().ContainSingle().Which.Should().Be($"{Esc}[?9;1S");
    }

    /// <summary>
    /// The limits are fixed, so accepting a request to change them and quietly not doing it would
    /// be worse than refusing.
    /// </summary>
    [TestMethod]
    public void A_request_to_change_a_limit_is_refused()
    {
        var (terminal, replies) = Listening();

        terminal.Write($"{Esc}[?1;3;64S"); // action 3 is "set"

        replies.Should().ContainSingle().Which.Should().Be($"{Esc}[?1;2S");
    }

    /// <summary>Without the private marker it is still SCROLL UP, and still has to scroll.</summary>
    [TestMethod]
    public void Scroll_up_still_works()
    {
        var terminal = Fresh();
        terminal.Write("top line\r\nsecond line");

        terminal.Write($"{Esc}[1S");

        terminal.GetLine(terminal.Buffer.YBase).Should().Be("second line");
    }
}
