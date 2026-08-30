using System.Diagnostics;
using XTerm.Options;
using XTerm.Search;

namespace XTerm.Tests;

/// <summary>
/// Searching the scrollback without turning the scrollback into text.
/// </summary>
[TestClass]
public class BufferSearchTests
{
    private const string Esc = "\u001b";

    private static Terminal Fresh(int cols = 20, int rows = 6, int scrollback = 200)
        => new(new TerminalOptions { Cols = cols, Rows = rows, Scrollback = scrollback });

    [TestMethod]
    public void It_finds_a_word_and_says_where()
    {
        var t = Fresh();
        t.Write("hello world\r\n");

        using var search = new BufferSearch(t);
        search.Find("world").Should().Be(1);

        var hit = search.HitsOnRow(t.Buffer.YBase)[0];
        hit.Column.Should().Be(6);
        hit.Cols.Should().Be(5);
    }

    [TestMethod]
    public void Case_is_ignored_unless_asked_for()
    {
        var t = Fresh();
        t.Write("Error and error\r\n");

        using var search = new BufferSearch(t);
        search.Find("ERROR").Should().Be(2);
        search.Find("Error", new SearchOptions { CaseSensitive = true }).Should().Be(1);
    }

    [TestMethod]
    public void Whole_word_refuses_a_match_inside_a_longer_one()
    {
        var t = Fresh(cols: 40);
        t.Write("cat concatenate cat\r\n");

        using var search = new BufferSearch(t);
        search.Find("cat").Should().Be(3);
        search.Find("cat", new SearchOptions { WholeWord = true }).Should().Be(2);
    }

    /// <summary>
    /// The case physical-line search misses, and the reason the walk crosses rows at all.
    /// </summary>
    [TestMethod]
    public void A_match_across_a_wrap_is_found_and_comes_back_as_two_runs()
    {
        var t = Fresh(cols: 10);
        t.Write("aaaaaaaaGREENbbb");   // wraps after 10, so GREEN straddles the boundary

        using var search = new BufferSearch(t);
        search.Find("green").Should().Be(2);

        var first = search.HitsOnRow(t.Buffer.YBase);
        var second = search.HitsOnRow(t.Buffer.YBase + 1);
        first.Length.Should().Be(1);
        second.Length.Should().Be(1);
        second[0].MatchId.Should().Be(first[0].MatchId);
        (first[0].Cols + second[0].Cols).Should().Be(5);
    }

    [TestMethod]
    public void Rows_with_nothing_on_them_come_back_empty()
    {
        var t = Fresh();
        t.Write("match\r\nnothing here\r\n");

        using var search = new BufferSearch(t);
        search.Find("match");

        search.HitsOnRow(t.Buffer.YBase + 1).ToArray().Should().BeEmpty();
    }

    [TestMethod]
    public void Stepping_walks_the_matches_and_wraps_round()
    {
        var t = Fresh();
        t.Write("a\r\na\r\na\r\n");

        using var search = new BufferSearch(t);
        search.Find("a").Should().Be(3);

        var rows = new List<int>();
        for (var i = 0; i < 4; i++)
        {
            search.TryMoveNext(out var hit).Should().BeTrue();
            rows.Add(hit.BufferRow);
        }

        rows[3].Should().Be(rows[0]);   // wrapped back to the first
    }

    /// <summary>A wrapped match is one stop, not two.</summary>
    [TestMethod]
    public void Stepping_counts_a_wrapped_match_once()
    {
        var t = Fresh(cols: 10);
        t.Write("aaaaaaaaGREENbbb");

        using var search = new BufferSearch(t);
        search.Find("green");

        search.TryMoveNext(out var first).Should().BeTrue();
        search.TryMoveNext(out var second).Should().BeTrue();
        second.MatchId.Should().Be(first.MatchId);
    }

    /// <summary>
    /// The results move with the buffer, or they point somewhere else while output scrolls past —
    /// and nothing about a wrong row looks wrong.
    /// </summary>
    [TestMethod]
    public void Results_follow_the_buffer_when_the_ring_drops_lines()
    {
        // Something above the needle, so the ring trims from the top without reaching it.
        var t = Fresh(rows: 3, scrollback: 10);
        for (var i = 0; i < 5; i++)
            t.Write($"before {i}\r\n");
        t.Write("needle\r\n");

        using var search = new BufferSearch(t);
        search.Find("needle").Should().Be(1);
        var before = FindRow(search, t);

        // Past capacity, so the oldest lines go and every row index below them shifts up.
        for (var i = 0; i < 10; i++)
            t.Write($"after {i}\r\n");

        var after = FindRow(search, t);
        search.Count.Should().Be(1);
        (after < before).Should().BeTrue($"the row should have moved up with the line: was {before}, now {after}");
        RowText(t, after).Should().Be("needle");
    }

    [TestMethod]
    public void A_result_scrolled_out_of_the_scrollback_is_dropped()
    {
        var t = Fresh(rows: 3, scrollback: 3);
        t.Write("needle\r\n");

        using var search = new BufferSearch(t);
        search.Find("needle").Should().Be(1);

        for (var i = 0; i < 40; i++)
            t.Write($"filler {i}\r\n");

        search.Count.Should().Be(0);
    }

    /// <summary>
    /// The cap says when it bit. A count that quietly stops being true reads as a bug in the search.
    /// </summary>
    [TestMethod]
    public void An_enormous_result_is_capped_and_admits_it()
    {
        var t = Fresh(cols: 80, rows: 10, scrollback: 5000);
        for (var i = 0; i < 3000; i++)
            t.Write(new string('a', 79) + "\r\n");

        using var search = new BufferSearch(t);
        var count = search.Find("a");

        search.Truncated.Should().BeTrue("far more matches than the cap");
        count.Should().Be(BufferSearch.MaxHits);
    }

    [TestMethod]
    public void An_empty_needle_finds_nothing()
    {
        var t = Fresh();
        t.Write("anything\r\n");

        using var search = new BufferSearch(t);
        search.Find("").Should().Be(0);
        search.TryMoveNext(out _).Should().BeFalse();
    }

    /// <summary>A needle outside the BMP is one codepoint in a cell, not two chars.</summary>
    [TestMethod]
    public void An_emoji_needle_matches_the_cell_that_holds_it()
    {
        var t = Fresh();
        t.Write("go \U0001F600 now\r\n");

        using var search = new BufferSearch(t);
        search.Find("\U0001F600").Should().Be(1);
        search.Find("go \U0001F600 now").Should().Be(1);
    }

    /// <summary>
    /// The placeholder behind a wide glyph is not a character, so a whole-word match must not sit
    /// flush against the CJK letter that owns it.
    /// </summary>
    [TestMethod]
    public void Whole_word_sees_through_a_wide_glyphs_placeholder()
    {
        var t = Fresh(cols: 30);
        t.Write("\u6F22word \u6F22 word\r\n");   // CJK+word joined, then separated

        using var search = new BufferSearch(t);
        var hits = search.Find("word", new SearchOptions { WholeWord = true });

        hits.Should().Be(1);   // only the separated one
    }

    /// <summary>What the whole design is for: a search allocates nothing worth counting.</summary>
    [TestMethod]
    public void Searching_does_not_allocate_per_line()
    {
        var t = Fresh(cols: 240, rows: 50, scrollback: 4000);
        for (var i = 0; i < 4000; i++)
            t.Write("compiling module target cache resolved warning linking\r\n");

        using var search = new BufferSearch(t);
        search.Find("zzz");   // warm, and matching nothing so no hits are stored

        // THIS thread's allocations, not the process's. xUnit runs test classes in parallel, so
        // the process-wide counter picks up whatever the suite happens to be doing on other threads
        // -- measured at 66 MB once, none of it from here.
        //
        // The per-thread counter is not exact either, which is why this takes the SMALLEST of
        // several windows rather than trusting one. The runtime credits a thread with a whole 8 KB
        // allocation quantum when it hands one out and subtracts the unused tail only while that
        // context is live, so a GC on ANY thread discards the context and the unused tail becomes
        // bytes we appear to have allocated. This search is the worst case for that: it allocates
        // its pattern up front and then walks 200,000 cells without allocating again, leaving a
        // full quantum exposed for the whole walk. CI read 8,200 bytes for a window that truly cost
        // 112. The real cost is deterministic and the error only ever adds, so the minimum is the
        // truth -- and the string-per-line version this guards against would blow the budget in
        // every window, not one in fifty.
        var allocated = long.MaxValue;
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var before = GC.GetAllocatedBytesForCurrentThread();
            search.Find("zzz");
            allocated = Math.Min(allocated, GC.GetAllocatedBytesForCurrentThread() - before);
        }

        (allocated < 4096).Should().BeTrue($"a search over 4,000 lines allocated {allocated:N0} bytes; the string-per-line version cost megabytes");
    }

    private static string RowText(Terminal t, int row)
    {
        var line = t.Buffer.Lines[row]!;
        return string.Concat(Enumerable.Range(0, line.Length).Select(c => line[c].Content)).TrimEnd();
    }

    private static int FindRow(BufferSearch search, Terminal t)
    {
        for (var i = 0; i < t.Buffer.Lines.Length; i++)
        {
            if (search.HitsOnRow(i).Length > 0)
                return i;
        }
        return -1;
    }
}
