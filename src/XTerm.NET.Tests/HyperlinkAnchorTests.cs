using XTerm.Buffer;
using XTerm.Options;

namespace XTerm.Tests;

/// <summary>
/// OSC 8 hyperlinks anchored to the columns they cover.
///
/// <para>The point of OSC 8 is a link whose DISPLAY TEXT is not the URL — "click here", a filename,
/// a commit subject. That is exactly what a regular expression over the visible text cannot find,
/// so the two are complementary rather than the same feature, and only this one can answer "what is
/// under the pointer".</para>
/// </summary>
[TestClass]
public class HyperlinkAnchorTests
{
    private const string Esc = "\u001b";
    private const string St = Esc + "\\";

    private static string Link(string url, string parameters = "") => $"{Esc}]8;{parameters};{url}{St}";
    private static string EndLink() => Link("");

    private static Terminal Fresh(int cols = 20, int rows = 5)
        => new(new TerminalOptions { Cols = cols, Rows = rows });

    private static BufferLine Row(Terminal t, int row = 0) => t.Buffer.Lines[t.Buffer.YBase + row]!;

    [TestMethod]
    public void A_link_covers_the_text_printed_inside_it()
    {
        var t = Fresh();
        t.Write(Link("https://example.com") + "click here" + EndLink());

        Row(t).TryGetLinkAt(3, out var link).Should().BeTrue();
        link.Url.Should().Be("https://example.com");
        link.Column.Should().Be(0);
        link.Cols.Should().Be(10);
    }

    /// <summary>The whole reason this cannot be a regex: no URL appears on screen at all.</summary>
    [TestMethod]
    public void The_display_text_need_not_look_like_a_url()
    {
        var t = Fresh();
        t.Write(Link("https://example.com/deep/path") + "click here" + EndLink());

        var text = string.Concat(Enumerable.Range(0, 10).Select(c => Row(t)[c].Content));
        text.Should().Be("click here");
        text.Should().NotContain("http");
        Row(t).TryGetLinkAt(0, out _).Should().BeTrue();
    }

    [TestMethod]
    public void Text_outside_the_link_is_not_covered()
    {
        var t = Fresh();
        t.Write("before " + Link("https://example.com") + "link" + EndLink() + " after");

        Row(t).TryGetLinkAt(0, out _).Should().BeFalse("text before the link");
        Row(t).TryGetLinkAt(7, out _).Should().BeTrue("the link itself");
        Row(t).TryGetLinkAt(11, out _).Should().BeFalse("text after the link");
    }

    /// <summary>One span, not one per character.</summary>
    [TestMethod]
    public void A_contiguous_link_is_a_single_span()
    {
        var t = Fresh();
        t.Write(Link("https://example.com") + "abcdefgh" + EndLink());

        (Row(t).Links).Should().ContainSingle();
        (Row(t).Links[0].Cols).Should().Be(8);
    }

    /// <summary>
    /// The id groups spans that are not contiguous, so a link that wrapped is one link.
    /// </summary>
    [TestMethod]
    public void The_id_parameter_is_kept()
    {
        var t = Fresh();
        t.Write(Link("https://example.com", "id=42") + "abc" + EndLink());

        Row(t).TryGetLinkAt(1, out var link).Should().BeTrue();
        link.Id.Should().Be("42");
    }

    /// <summary>Two different links side by side stay two spans.</summary>
    [TestMethod]
    public void Adjacent_links_to_different_urls_are_not_joined()
    {
        var t = Fresh();
        t.Write(Link("https://a.example") + "aa" + EndLink()
              + Link("https://b.example") + "bb" + EndLink());

        (Row(t).Links.Count).Should().Be(2);
        Row(t).TryGetLinkAt(0, out var first).Should().BeTrue();
        Row(t).TryGetLinkAt(2, out var second).Should().BeTrue();
        first.Url.Should().Be("https://a.example");
        second.Url.Should().Be("https://b.example");
    }

    [TestMethod]
    public void Typing_over_a_link_takes_those_columns_out_of_it()
    {
        var t = Fresh();
        t.Write(Link("https://example.com") + "abcdefgh" + EndLink());
        t.Write($"{Esc}[1;1HXX");

        Row(t).TryGetLinkAt(0, out _).Should().BeFalse("the overwritten columns are not the link");
        Row(t).TryGetLinkAt(2, out var rest).Should().BeTrue();
        rest.Column.Should().Be(2);
        rest.Cols.Should().Be(6);
    }

    /// <summary>Writing through the middle leaves the two halves.</summary>
    [TestMethod]
    public void Typing_through_the_middle_splits_it()
    {
        var t = Fresh();
        t.Write(Link("https://example.com") + "abcdefgh" + EndLink());
        t.Write($"{Esc}[1;4HXX");

        (Row(t).Links.Count).Should().Be(2);
        Row(t).TryGetLinkAt(0, out var left).Should().BeTrue();
        Row(t).TryGetLinkAt(5, out var right).Should().BeTrue();
        left.Cols.Should().Be(3);
        right.Cols.Should().Be(3);
        Row(t).TryGetLinkAt(3, out _).Should().BeFalse();
    }

    /// <summary>
    /// The batched writer bypasses Print, so it keeps the bookkeeping itself. Without that a link
    /// would cover the text or not depending on which writer happened to take it.
    /// </summary>
    [TestMethod]
    public void The_batched_and_per_character_paths_agree()
    {
        const string input = "before ";
        var batched = Fresh();
        batched.Write(input + Link("https://example.com") + "click here" + EndLink() + " after");

        var perCharacter = Fresh();
        perCharacter.UseRunPrinting = false;
        perCharacter.Write(input + Link("https://example.com") + "click here" + EndLink() + " after");

        Describe(batched).Should().Be(Describe(perCharacter));
        Describe(batched).Should().Be("https://example.com@7+10");
    }

    /// <summary>And the byte entry, which is a third writer again.</summary>
    [TestMethod]
    public void The_byte_entry_agrees_too()
    {
        var viaString = Fresh();
        viaString.Write(Link("https://example.com") + "click here" + EndLink());

        var viaBytes = Fresh();
        viaBytes.Write(System.Text.Encoding.UTF8.GetBytes(
            Link("https://example.com") + "click here" + EndLink()));

        Describe(viaBytes).Should().Be(Describe(viaString));
    }

    /// <summary>A recycled line is a new line: the ring hands back the object it is about to drop.</summary>
    [TestMethod]
    public void A_line_reused_by_the_ring_carries_no_links_over()
    {
        var t = new Terminal(new TerminalOptions { Cols = 20, Rows = 3, Scrollback = 2 });
        t.Write(Link("https://example.com") + "link" + EndLink() + "\r\n");

        for (var i = 0; i < 20; i++)
            t.Write($"line {i}\r\n");

        for (var i = 0; i < t.Buffer.Lines.Length; i++)
            (t.Buffer.Lines[i]?.HasLinks ?? false).Should().BeFalse($"row {i} kept a link from a line the ring had dropped");
    }

    /// <summary>Ordinary output carries no links, and pays nothing to say so.</summary>
    [TestMethod]
    public void Text_with_no_link_records_none()
    {
        var t = Fresh();
        t.Write("just some ordinary output");

        (Row(t).HasLinks).Should().BeFalse();
        (Row(t).Links).Should().BeEmpty();
    }

    [TestMethod]
    public void Reflow_moves_a_link_with_its_text_and_removes_the_old_span()
    {
        var t = Fresh(cols: 10, rows: 5);
        t.Write("0123456789" + Link("https://example.com") + "ABCD" + EndLink());
        t.Write($"{Esc}[5;1H"); // Keep the cursor out of the wrapped group so it may reflow.

        t.Resize(20, 5);

        (Row(t, 1).HasLinks).Should().BeFalse();
        Row(t).TryGetLinkAt(12, out var link).Should().BeTrue();
        link.Column.Should().Be(10);
        link.Cols.Should().Be(4);
    }

    [TestMethod]
    public void Widening_joins_the_pieces_of_one_wrapped_link()
    {
        var t = Fresh(cols: 10, rows: 5);
        t.Write(Link("https://example.com") + "0123456789ABCD" + EndLink());
        t.Write($"{Esc}[5;1H");

        t.Resize(20, 5);

        var link = (Row(t).Links).Should().ContainSingle().Which;
        link.Column.Should().Be(0);
        link.Cols.Should().Be(14);
    }

    [TestMethod]
    public void Reflow_splits_a_link_at_each_new_wrap_boundary()
    {
        var t = Fresh(cols: 12, rows: 6);
        t.Write(Link("https://example.com", "id=wrapped") + "abcdefghij" + EndLink());
        t.Write($"{Esc}[6;1H");

        t.Resize(4, 6);

        var spans = Enumerable.Range(0, t.Buffer.Lines.Length)
            .Select(row => t.Buffer.Lines[row])
            .Where(line => line?.HasLinks == true)
            .Select(line => line!.Links.Single())
            .ToArray();
        spans.Select(span => span.Cols).Should().Equal(new[] { 4, 4, 2 });
        spans.Should().AllSatisfy(span =>
        {
            span.Column.Should().Be(0);
            span.Id.Should().Be("wrapped");
        });
    }

    /// <summary>
    /// Erasing takes the link with the text. Unlike a mark: a mark records a position in the
    /// history, but a link is a property of its text, and an erased span left clickable is an
    /// invisible link.
    /// </summary>
    [TestMethod]
    public void Erasing_the_text_takes_the_link_with_it()
    {
        var t = Fresh();
        t.Write(Link("https://example.com") + "abcdefgh" + EndLink());
        t.Write($"{Esc}[1;3H{Esc}[K");   // erase from column 3 to end of line

        Row(t).TryGetLinkAt(0, out var kept).Should().BeTrue("the unerased head should keep its link");
        kept.Cols.Should().Be(2);
        Row(t).TryGetLinkAt(4, out _).Should().BeFalse("the erased span must not stay clickable");
    }

    /// <summary>
    /// A new link that names no id must not inherit the previous link's — that would join two
    /// unrelated links into one.
    /// </summary>
    [TestMethod]
    public void A_new_link_without_an_id_does_not_inherit_the_old_one()
    {
        var t = Fresh(cols: 30);
        t.Write(Link("https://a.example", "id=7") + "aa" + EndLink());
        t.Write(Link("https://b.example") + "bb" + EndLink());

        Row(t).TryGetLinkAt(2, out var second).Should().BeTrue();
        second.Id.Should().BeNull();
    }

    /// <summary>Splitting a link in the middle keeps Links in left-to-right order.</summary>
    [TestMethod]
    public void A_split_keeps_the_spans_in_order()
    {
        var t = Fresh(cols: 40);
        t.Write(Link("https://a.example") + "aaaaaa" + EndLink()
              + Link("https://b.example") + "bbbbbb" + EndLink());
        t.Write($"{Esc}[1;3HXX");   // split the first link through the middle

        var columns = Row(t).Links.Select(l => l.Column).ToList();
        columns.Should().Equal(columns.OrderBy(c => c));
    }

    /// <summary>The prompt walks clamp extreme rows instead of wrapping the arithmetic.</summary>
    [TestMethod]
    public void Prompt_navigation_survives_extreme_rows()
    {
        var t = Fresh();
        t.Write($"{Esc}]133;A\u0007$ ");

        t.TryFindPreviousPrompt(int.MaxValue, out _).Should().BeTrue();
        t.TryFindPreviousPrompt(int.MinValue, out _).Should().BeFalse();
        t.TryFindNextPrompt(int.MinValue, out _).Should().BeTrue();
        t.TryFindNextPrompt(int.MaxValue, out _).Should().BeFalse();
    }

    private static string Describe(Terminal t)
        => string.Join(" ", Row(t).Links.Select(l => $"{l.Url}@{l.Column}+{l.Cols}"));
}
