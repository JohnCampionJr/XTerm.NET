using XTerm.Buffer;
using XTerm.Common;
using XTerm.Options;

namespace XTerm.Tests;

/// <summary>
/// The Kitty text sizing protocol, OSC 66.
///
/// <para>Two halves, and they are worth keeping apart. The WIDTH half is the emulator's own: a run
/// really claims <c>s * w</c> columns, so the cursor, selection and search agree with the client
/// about how much room it took — which is the point of the <c>w</c> key, a client stating a
/// string's width instead of both sides guessing at Unicode. The SCALE half the emulator only
/// records, on the line, for a renderer to draw.</para>
/// </summary>
[TestClass]
public class TextSizingTests
{
    private const string Esc = "\u001b";
    private const string St = Esc + "\\";

    private static string Sized(string metadata, string text) => $"{Esc}]66;{metadata};{text}{St}";

    private static Terminal Fresh(int cols = 20, int rows = 5)
        => new(new TerminalOptions { Cols = cols, Rows = rows });

    private static BufferLine Row(Terminal t, int row = 0) => t.Buffer.Lines[t.Buffer.YBase + row]!;

    [TestMethod]
    public void Scale_makes_each_character_claim_that_many_columns()
    {
        var t = Fresh();
        t.Write(Sized("s=2", "abc"));

        // Three blocks of two columns: text in the first cell of each, a continuation after it.
        (Row(t)[0].Content).Should().Be("a");
        (Row(t)[0].Width).Should().Be(2);
        (Row(t)[1].Width).Should().Be(0);
        (Row(t)[2].Content).Should().Be("b");
        (Row(t)[4].Content).Should().Be("c");
        t.Buffer.X.Should().Be(6);
    }

    [TestMethod]
    public void Width_puts_the_whole_run_in_the_cells_it_asked_for()
    {
        var t = Fresh();
        t.Write(Sized("n=1:d=2:w=1", "ab"));

        (Row(t)[0].Content).Should().Be("ab");
        (Row(t)[0].Width).Should().Be(1);
        t.Buffer.X.Should().Be(1);
    }

    [TestMethod]
    public void Scale_and_width_together_give_a_block_of_scale_times_width()
    {
        var t = Fresh();
        t.Write(Sized("s=2:w=3", "hi"));

        (Row(t)[0].Content).Should().Be("hi");
        (Row(t)[0].Width).Should().Be(6);
        t.Buffer.X.Should().Be(6);
    }

    /// <summary>What the protocol's own capability probe measures.</summary>
    [TestMethod]
    public void The_cursor_advance_reports_support_the_way_the_probe_expects()
    {
        var t = Fresh();

        t.Write(Sized("w=2", " "));
        t.Buffer.X.Should().Be(2);

        t.Write(Sized("s=2", " "));
        t.Buffer.X.Should().Be(4);
    }

    [TestMethod]
    public void The_run_is_recorded_on_the_line_with_what_was_asked_for()
    {
        var t = Fresh();
        t.Write(Sized("s=3:n=1:d=2:v=1:h=2", "x"));

        Row(t).TryGetSizedRunAt(1, out var run).Should().BeTrue();
        run.Column.Should().Be(0);
        run.Cols.Should().Be(3);
        run.Rows.Should().Be(3);
        run.Sizing.Scale.Should().Be(3);
        run.Sizing.IsFractional.Should().BeTrue();
        run.Sizing.VerticalAlignment.Should().Be(TextSizeVerticalAlignment.Bottom);
        run.Sizing.HorizontalAlignment.Should().Be(TextSizeHorizontalAlignment.Center);
    }

    [TestMethod]
    public void Adjacent_runs_with_the_same_sizing_are_one_span()
    {
        var t = Fresh();
        t.Write(Sized("s=2", "ab"));

        (Row(t).SizedRuns).Should().ContainSingle();
        (Row(t).SizedRuns[0].Cols).Should().Be(4);
    }

    [TestMethod]
    public void Ordinary_text_is_not_a_sized_run()
    {
        var t = Fresh();
        t.Write("plain");

        (Row(t).HasSizedRuns).Should().BeFalse();
        (Row(t)[0].Width).Should().Be(1);
    }

    [TestMethod]
    public void A_block_that_does_not_fit_wraps_whole()
    {
        var t = Fresh(cols: 5);
        t.Write("ab" + Sized("s=2:w=2", "X"));

        // Four columns will not fit after "ab", so the block goes to the next line rather than
        // being split across the edge.
        (Row(t)[1].Content).Should().Be("b");
        (Row(t, 1)[0].Content).Should().Be("X");
        (Row(t, 1)[0].Width).Should().Be(4);
        t.Buffer.X.Should().Be(4);
        t.Buffer.Y.Should().Be(1);
    }

    [TestMethod]
    public void With_wrapping_off_the_block_is_moved_back_to_fit()
    {
        var t = Fresh(cols: 5);
        t.Options.Wraparound = false;
        t.Write("ab" + Sized("s=2:w=2", "X"));

        t.Buffer.Y.Should().Be(0);
        (Row(t)[1].Content).Should().Be("X");
        (Row(t)[1].Width).Should().Be(4);
        t.Buffer.X.Should().Be(5);
    }

    [TestMethod]
    public void A_block_wider_than_the_screen_is_discarded()
    {
        var t = Fresh(cols: 5);
        t.Write(Sized("s=2:w=7", "X"));

        (Row(t).HasSizedRuns).Should().BeFalse();
        t.Buffer.X.Should().Be(0);
    }

    [TestMethod]
    public void Writing_over_part_of_a_block_erases_all_of_it()
    {
        var t = Fresh();
        t.Write(Sized("s=2:w=2", "X"));       // columns 0..3
        t.Write($"{Esc}[1;3H" + "y");         // over column 2

        (Row(t).HasSizedRuns).Should().BeFalse();
        (Row(t)[2].Content).Should().Be("y");

        // The rest of the block is gone rather than left claiming columns it no longer owns.
        (Row(t)[0].Content).Should().Be(" ");
        (Row(t)[0].Width).Should().Be(1);
        (Row(t)[3].Content).Should().Be(" ");
    }

    [TestMethod]
    public void Writing_a_new_block_over_an_old_one_replaces_it()
    {
        var t = Fresh();
        t.Write(Sized("s=2:w=2", "X"));
        t.Write($"{Esc}[1;1H" + Sized("s=2", "y"));

        (Row(t).SizedRuns).Should().ContainSingle();
        (Row(t).SizedRuns[0].Cols).Should().Be(2);
        (Row(t)[0].Content).Should().Be("y");
        (Row(t)[2].Content).Should().Be(" ");
        (Row(t)[3].Content).Should().Be(" ");
    }

    [TestMethod]
    public void An_empty_payload_draws_nothing_and_moves_nothing()
    {
        var t = Fresh();
        t.Write(Sized("s=2", ""));

        t.Buffer.X.Should().Be(0);
        (Row(t).HasSizedRuns).Should().BeFalse();
    }

    [TestMethod]
    public void Semicolons_in_the_text_are_text()
    {
        var t = Fresh();
        t.Write(Sized("w=3", "a;b"));

        (Row(t)[0].Content).Should().Be("a;b");
        (Row(t)[0].Width).Should().Be(3);
    }

    [TestMethod]
    public void A_wide_character_keeps_its_own_width_inside_the_scale()
    {
        var t = Fresh();
        t.Write(Sized("s=2", "\u4F60"));   // a CJK ideograph, two columns before scaling

        (Row(t)[0].Width).Should().Be(4);
        t.Buffer.X.Should().Be(4);
    }

    [TestMethod]
    public void Erasing_the_line_erases_the_run()
    {
        var t = Fresh();
        t.Write(Sized("s=2:w=2", "X"));
        t.Write($"{Esc}[1;3H" + $"{Esc}[K");   // erase from column 3 rightwards

        (Row(t).HasSizedRuns).Should().BeFalse();
        (Row(t)[0].Content).Should().Be(" ");
        (Row(t)[0].Width).Should().Be(1);
    }

    [TestMethod]
    public void Erasing_characters_erases_a_block_they_touch()
    {
        var t = Fresh();
        t.Write(Sized("s=2:w=2", "X"));
        t.Write($"{Esc}[1;4H" + $"{Esc}[1X");   // ECH over the block's last column

        (Row(t).HasSizedRuns).Should().BeFalse();
        (Row(t)[0].Width).Should().Be(1);
    }

    [TestMethod]
    public void Shifting_cells_erases_a_block_they_belong_to()
    {
        var t = Fresh();
        t.Write(Sized("s=2:w=2", "X"));

        // ICH at the block's second column: the cells move, so the block is gone rather than left
        // described by columns that now hold something else.
        t.Write($"{Esc}[1;2H" + $"{Esc}[2@");

        (Row(t).HasSizedRuns).Should().BeFalse();
        for (var col = 0; col < 6; col++)
            ((Row(t)[col].Width <= 1)).Should().BeTrue($"column {col} still claims columns");
    }

    [TestMethod]
    public void Deleting_characters_erases_a_block_they_belong_to()
    {
        var t = Fresh();
        t.Write("ab" + Sized("s=2:w=2", "X"));
        t.Write($"{Esc}[1;1H" + $"{Esc}[1P");

        (Row(t).HasSizedRuns).Should().BeFalse();
        for (var col = 0; col < 6; col++)
            ((Row(t)[col].Width <= 1)).Should().BeTrue($"column {col} still claims columns");
    }

    /// <summary>
    /// The protocol's rule for text aimed at a row a taller block already occupies: the cursor moves
    /// past the block's cells and the text lands after them. Without it a client printing normally
    /// under a heading has its output drawn over by the heading's lower half.
    /// </summary>
    [TestMethod]
    public void Text_under_a_tall_block_is_pushed_past_it()
    {
        var t = Fresh();
        t.Write(Sized("s=2", "Big"));    // three 2-column blocks, two rows tall, columns 0..5
        t.Write("\r\nxyz");

        (Row(t, 1)[6].Content).Should().Be("x");
        (Row(t, 1)[7].Content).Should().Be("y");
        (Row(t, 1)[8].Content).Should().Be("z");
        (Row(t, 1)[0].Content).Should().Be(" ");
        t.Buffer.X.Should().Be(9);
    }

    [TestMethod]
    public void The_row_below_a_one_row_block_is_ordinary()
    {
        var t = Fresh();
        t.Write(Sized("w=2", "X"));      // two columns, but only one row tall
        t.Write("\r\nxyz");

        (Row(t, 1)[0].Content).Should().Be("x");
    }

    [TestMethod]
    public void The_rows_a_block_occupies_are_answerable()
    {
        var t = Fresh();
        t.Write(Sized("s=3:w=2", "X"));  // 6 columns, 3 rows

        var top = t.Buffer.YBase;
        t.Buffer.TryGetSizedRunCovering(top + 1, 5, out var run, out var anchor).Should().BeTrue();
        anchor.Should().Be(top);
        run.Rows.Should().Be(3);
        t.Buffer.TryGetSizedRunCovering(top + 2, 0, out _, out _).Should().BeTrue();

        // Its own row is not "covered from above", and the row past its height is not covered at all.
        t.Buffer.TryGetSizedRunCovering(top, 0, out _, out _).Should().BeFalse();
        t.Buffer.TryGetSizedRunCovering(top + 3, 0, out _, out _).Should().BeFalse();
        t.Buffer.TryGetSizedRunCovering(top + 1, 6, out _, out _).Should().BeFalse();
    }

    /// <summary>
    /// The payload of an OSC is not a preceding graphic character, so <c>CSI b</c> has nothing to
    /// repeat — replaying a scaled block as plain cells is neither what was printed nor what was
    /// asked for.
    /// </summary>
    [TestMethod]
    public void A_sized_block_is_not_repeated_by_rep()
    {
        var t = Fresh();
        t.Write(Sized("s=2", "X"));
        t.Write($"{Esc}[3b");

        t.Buffer.X.Should().Be(2);
        (Row(t)[2].Content).Should().Be(" ");
        (Row(t).SizedRuns).Should().ContainSingle();
    }

    [TestMethod]
    public void Insert_mode_shifts_the_rest_of_the_line_intact()
    {
        var t = Fresh();
        t.Write(Sized("s=2:w=2", "X"));   // columns 0..3
        t.Write("tail");                  // columns 4..7

        t.Write($"{Esc}[1;5H{Esc}[4h");   // cursor to column 5, IRM on
        t.Write(Sized("w=2", "Z"));

        Row(t).TranslateToString(trimRight: true).Should().Be("XZtail");
        (Row(t)[4].Width).Should().Be(2);
        (Row(t)[6].Content).Should().Be("t");

        // The block that was not shifted is untouched.
        Row(t).TryGetSizedRunAt(0, out var first).Should().BeTrue();
        first.Cols.Should().Be(4);
    }

    [TestMethod]
    public void Insert_mode_over_a_block_erases_it()
    {
        var t = Fresh();
        t.Write(Sized("s=2:w=2", "X"));
        t.Write($"{Esc}[1;2H{Esc}[4h" + "a");

        (Row(t).HasSizedRuns).Should().BeFalse();
        for (var col = 0; col < 8; col++)
            ((Row(t)[col].Width <= 1)).Should().BeTrue($"column {col} still claims columns");
    }

    /// <summary>
    /// Widening moves no cell of a group holding a block — reflow leaves such a group alone — so the
    /// block is still where its run says it is, and the run is still worth keeping.
    /// </summary>
    [TestMethod]
    public void A_block_survives_the_line_growing()
    {
        var t = Fresh(cols: 10, rows: 4);
        t.Write(Sized("s=2:w=2", "Z"));

        t.Resize(14, 4);

        Row(t).TryGetSizedRunAt(0, out var run).Should().BeTrue();
        run.Cols.Should().Be(4);
        Row(t).TranslateToString(trimRight: true).Should().Be("Z");
    }

    /// <summary>
    /// A block cut in half by a narrowing does not survive it: the columns holding the rest of the
    /// glyph are gone, so what is left becomes spaces rather than a cell claiming columns that no
    /// longer exist.
    /// </summary>
    [TestMethod]
    public void A_block_cut_by_a_narrowing_is_dropped()
    {
        var t = Fresh(cols: 10, rows: 4);
        t.Write($"{Esc}[1;7H" + Sized("s=2:w=2", "Z"));   // columns 6..9

        t.Resize(8, 4);

        (Row(t).HasSizedRuns).Should().BeFalse();
        (Row(t)[6].Content).Should().Be(" ");
        (Row(t)[6].Width).Should().Be(1);
    }

    /// <summary>
    /// Reflow redistributes cells between lines and run metadata cannot travel with them, so a
    /// wrapped group holding a block is left alone — as a double-width line already is. Without
    /// that, the compaction copies read cells the same pass had already blanked.
    /// </summary>
    [TestMethod]
    public void Reflow_does_not_garble_a_wrapped_group_holding_a_block()
    {
        var t = Fresh(cols: 10, rows: 4);
        t.Write("0123456789");            // fills the row, so the next write wraps
        t.Write("ab" + Sized("s=2:w=2", "Z") + "cd");

        t.Resize(14, 4);
        (Row(t, 0).TranslateToString() + Row(t, 1).TranslateToString()).Should().Contain("Z");
        (Row(t, 0).TranslateToString() + Row(t, 1).TranslateToString()).Should().Contain("cd");

        // Narrowing does not re-wrap that group either, so it loses what no longer fits -- the same
        // cost a double-width line already pays here. What it must not do is garble what remains.
        t.Resize(6, 4);
        var all = string.Concat(Enumerable.Range(0, t.Buffer.Lines.Length)
            .Select(i => t.Buffer.Lines[i]?.TranslateToString() ?? string.Empty));
        all.Should().Contain("Z");
        all.Should().Contain("012345");

        for (var i = 0; i < t.Buffer.Lines.Length; i++)
        {
            var line = t.Buffer.Lines[i];
            if (line is null)
                continue;

            for (var col = 0; col < line.Length; col++)
                ((col + line[col].Width <= line.Length)).Should().BeTrue($"line {i} column {col} runs off the end");
        }
    }

    [TestMethod]
    public void A_recycled_line_keeps_no_runs()
    {
        // No scrollback, so the line holding the run is the very object the ring hands back for
        // the new bottom row -- which is what makes clearing it on reuse necessary.
        var t = new Terminal(new TerminalOptions { Cols = 10, Rows = 2, Scrollback = 0 });
        t.Write(Sized("s=2", "X"));
        t.Write("\r\n\r\n\r\n");

        for (var i = 0; i < t.Buffer.Lines.Length; i++)
        {
            var line = t.Buffer.Lines[i];
            if (line is not null)
                line.HasSizedRuns.Should().BeFalse();
        }
    }

    [TestMethod]
    [DataRow("s=0")]          // scale starts at one
    [DataRow("s=8")]          // and stops at seven
    [DataRow("w=8")]
    [DataRow("n=16")]
    [DataRow("d=16")]
    [DataRow("n=3:d=2")]      // a denominator must exceed its numerator
    [DataRow("v=3")]
    [DataRow("h=3")]
    [DataRow("s")]            // not a pair
    [DataRow("s=x")]
    [DataRow("s=-1")]
    [DataRow("s=+2")]         // the grammar is digits, not int.TryParse's idea of a number
    [DataRow("s= 2")]
    public void Metadata_out_of_range_is_not_handled(string metadata)
    {
        TextSizing.TryParse(metadata, out _).Should().BeFalse();

        var t = Fresh();
        var recognized = true;
        t.OscReceived += (_, e) => recognized = e.Recognized;
        t.Write(Sized(metadata, "X"));

        recognized.Should().BeFalse();
        (Row(t).HasSizedRuns).Should().BeFalse();
    }

    /// <summary>
    /// The text is what the user was meant to read, so a metadata the terminal cannot honour costs
    /// the sizing rather than the heading.
    /// </summary>
    [TestMethod]
    public void Text_of_an_unhandled_sequence_is_still_printed()
    {
        var t = Fresh();
        t.Write(Sized("s=99", "Hi"));

        Row(t).TranslateToString(trimRight: true).Should().Be("Hi");
        t.Buffer.X.Should().Be(2);
        (Row(t).HasSizedRuns).Should().BeFalse();
    }

    /// <summary>
    /// This protocol has been extended before. A key from a later revision costs its own effect, not
    /// the run of text it was attached to.
    /// </summary>
    [TestMethod]
    public void An_unknown_key_is_ignored_and_the_rest_honoured()
    {
        TextSizing.TryParse("s=2:q=1", out var sizing).Should().BeTrue();
        sizing.Scale.Should().Be(2);

        var t = Fresh();
        var recognized = false;
        t.OscReceived += (_, e) => recognized = e.Recognized;
        t.Write(Sized("s=2:q=1", "X"));

        recognized.Should().BeTrue();
        (Row(t)[0].Content).Should().Be("X");
        (Row(t)[0].Width).Should().Be(2);
    }

    /// <summary>
    /// The protocol's 4096-byte payload limit belongs to the sequence, not to one of its modes.
    /// With w=0 every grapheme is interned in the process-wide cluster table, so an unbounded
    /// payload here is the more expensive of the two to let through.
    /// </summary>
    [TestMethod]
    public void A_payload_over_the_limit_is_cut_when_each_grapheme_is_its_own_block()
    {
        var t = Fresh(cols: 80, rows: 4);
        t.Write(Sized("s=1", new string('x', 5000)));

        var printed = 0;
        for (var row = 0; row < t.Buffer.Lines.Length; row++)
        {
            var line = t.Buffer.Lines[row];
            if (line is null)
                continue;

            for (var col = 0; col < line.Length; col++)
            {
                if (line[col].Content == "x")
                    printed++;
            }
        }

        printed.Should().Be(4096);
    }

    [TestMethod]
    public void A_key_longer_than_one_letter_is_ignored_too()
    {
        TextSizing.TryParse("scale=2:s=3", out var sizing).Should().BeTrue();
        sizing.Scale.Should().Be(3);
    }

    [TestMethod]
    public void Metadata_defaults_to_plain_text()
    {
        TextSizing.TryParse("", out var sizing).Should().BeTrue();
        sizing.Should().Be(TextSizing.Default);
        sizing.IsDefault.Should().BeTrue();
        sizing.IsFractional.Should().BeFalse();
        sizing.Scale.Should().Be(1);
        sizing.Width.Should().Be(0);
    }

    [TestMethod]
    public void A_fraction_of_one_is_no_fraction()
    {
        TextSizing.TryParse("n=2:d=2", out _).Should().BeFalse();
        TextSizing.TryParse("n=0:d=2", out var sizing).Should().BeTrue();
        sizing.IsFractional.Should().BeFalse();
    }

    [TestMethod]
    public void Unscaled_text_with_only_a_width_is_still_a_run()
    {
        // The width half of the protocol on its own: no scaling asked for, but the client is
        // telling the terminal how many cells its text takes.
        TextSizing.TryParse("w=2", out var sizing).Should().BeTrue();
        sizing.IsDefault.Should().BeFalse();
        sizing.Scale.Should().Be(1);
    }

    [TestMethod]
    public void A_link_covers_a_sized_run_written_inside_it()
    {
        var t = Fresh();
        t.Write($"{Esc}]8;;https://example.com{St}" + Sized("s=2", "X") + $"{Esc}]8;;{St}");

        Row(t).TryGetLinkAt(1, out var link).Should().BeTrue();
        link.Url.Should().Be("https://example.com");
        link.Cols.Should().Be(2);
    }
    /// <summary>
    /// The protocol erases over a REGION, so clearing the screen below a block's own row still takes
    /// the block: two of its three rows were inside what was cleared.
    /// </summary>
    [TestMethod]
    public void Erasing_below_takes_a_block_hanging_into_it()
    {
        var t = Fresh();
        t.Write(Sized("s=3", "H"));
        t.Write($"{Esc}[2;1H{Esc}[J");   // cursor to row 2, erase below

        (Row(t).HasSizedRuns).Should().BeFalse();
        (Row(t)[0].Content).Should().Be(" ");
        t.Buffer.TryGetSizedRunCovering(t.Buffer.YBase + 1, 0, out _, out _).Should().BeFalse();
    }

    /// <summary>
    /// Same rule, a line at a time and a few cells at a time.
    /// </summary>
    [TestMethod]
    [DataRow("[K")]      // erase to the right of a covered cell
    [DataRow("[2K")]     // erase the whole covered row
    [DataRow("[4X")]     // erase characters on the covered row
    public void Erasing_a_covered_row_takes_the_block(string erase)
    {
        var t = Fresh();
        t.Write(Sized("s=2", "H"));
        t.Write($"{Esc}[2;1H{Esc}{erase}");

        (Row(t).HasSizedRuns).Should().BeFalse();
        (Row(t)[0].Content).Should().Be(" ");
    }

    /// <summary>
    /// An erase that misses the block entirely leaves it alone -- the region rule is about
    /// intersection, not about the presence of a block anywhere above.
    /// </summary>
    [TestMethod]
    public void Erasing_beside_a_block_leaves_it_alone()
    {
        var t = Fresh();
        t.Write(Sized("s=2", "H"));         // columns 0..1
        t.Write($"{Esc}[2;5H{Esc}[K");      // erase from column 5 rightwards on the row below

        Row(t).TryGetSizedRunAt(0, out _).Should().BeTrue();
    }

    /// <summary>
    /// Splicing a line into the middle of a block would leave its lower rows stranded a row further
    /// down than the run says, so the block is erased instead.
    /// </summary>
    [TestMethod]
    public void Inserting_a_line_through_a_block_erases_it()
    {
        var t = Fresh();
        t.Write(Sized("s=2", "Hi"));
        t.Write($"{Esc}[2;1H{Esc}[L");

        (Row(t).HasSizedRuns).Should().BeFalse();
        (Row(t)[0].Content).Should().Be(" ");
    }

    /// <summary>
    /// And deleting one of the rows a block hangs over does the same.
    /// </summary>
    [TestMethod]
    public void Deleting_a_covered_line_erases_the_block()
    {
        var t = Fresh();
        t.Write(Sized("s=2", "Hi"));
        t.Write($"{Esc}[2;1H{Esc}[M");

        (Row(t).HasSizedRuns).Should().BeFalse();
    }

    /// <summary>
    /// The rule is about the cells the text will OVERWRITE, so a double-width character whose right
    /// half would land inside a block is moved past it too, not just one that starts there.
    /// </summary>
    [TestMethod]
    public void A_wide_character_may_not_overlap_a_block_from_the_left()
    {
        var t = Fresh();
        t.Write("x" + Sized("s=2", "A"));   // block at columns 1..2, two rows tall
        t.Write("\r\n\u4e2d");             // a CJK ideograph, two columns wide

        (Row(t, 1)[0].Content).Should().Be(" ");
        (Row(t, 1)[3].Content).Should().Be("\u4e2d");
    }

    /// <summary>
    /// Clearing the screen takes the last block with it, so the print path stops looking for rows
    /// hanging over -- a heading early in a session must not retire the fast path for the rest of it.
    /// </summary>
    [TestMethod]
    public void Clearing_the_screen_stops_the_search_for_blocks()
    {
        var t = Fresh();
        t.Write(Sized("s=2", "H"));
        t.Buffer.HasMultiRowSizedRuns.Should().BeTrue();

        t.Write($"{Esc}[2J");

        t.Buffer.HasMultiRowSizedRuns.Should().BeFalse();
    }

    /// <summary>
    /// A row full of blocks that cannot be merged into one run is still skipped completely -- the
    /// loop's bound is for hostile input, not for a legal screen.
    /// </summary>
    [TestMethod]
    public void A_row_of_many_blocks_is_skipped_completely()
    {
        var t = Fresh(cols: 80, rows: 4);
        for (var i = 0; i < 20; i++)
            t.Write(Sized("s=2", "a") + Sized("s=2:n=1:d=2", "b"));   // 40 unmergeable blocks, 80 columns

        t.Write("\r\nZ");

        // Nowhere on the covered row is free, so the text lands on the row after it.
        (Row(t, 2)[0].Content).Should().Be("Z");
        (Row(t, 1)[0].Content).Should().Be(" ");
    }
    /// <summary>
    /// A scroll of a PARTIAL region splices a line out of the middle of the buffer exactly as
    /// <c>DL</c> does, so a block straddling the region's top boundary dies the same way.
    /// </summary>
    [TestMethod]
    [DataRow("[S")]      // scroll the region up
    [DataRow("[T")]      // and down
    public void A_region_scroll_erases_a_block_it_would_tear(string scroll)
    {
        var t = Fresh(rows: 6);
        t.Write($"{Esc}[1;1H" + Sized("s=2", "H"));   // anchored on row 0, reaching row 1
        t.Write($"{Esc}[2;5r");                        // region rows 2..5, so the block straddles its top
        t.Write($"{Esc}{scroll}");

        (Row(t).HasSizedRuns).Should().BeFalse();
        (Row(t)[0].Content).Should().Be(" ");
    }

    /// <summary>
    /// And one that reaches out of the region's BOTTOM: its lower rows stay where they are while
    /// the row describing them moves.
    /// </summary>
    [TestMethod]
    public void A_region_scroll_erases_a_block_reaching_below_it()
    {
        var t = Fresh(rows: 6);
        t.Write($"{Esc}[2;5r{Esc}[5;1H" + Sized("s=2", "H"));   // anchored on the region's last row
        t.Write($"{Esc}[S");

        t.Buffer.HasMultiRowSizedRuns.Should().BeFalse();
    }

    /// <summary>
    /// A block wholly inside the region travels with its rows, which move together -- a scroll is
    /// not an erase.
    /// </summary>
    [TestMethod]
    public void A_region_scroll_carries_a_block_that_fits_inside_it()
    {
        var t = Fresh(rows: 6);
        t.Write($"{Esc}[2;5r{Esc}[3;1H" + Sized("s=2", "H"));   // rows 2..3, inside the region
        t.Write($"{Esc}[S");

        Row(t, 1).TryGetSizedRunAt(0, out var run).Should().BeTrue();
        run.Rows.Should().Be(2);
        (Row(t, 1)[0].Content).Should().Be("H");
    }
}
