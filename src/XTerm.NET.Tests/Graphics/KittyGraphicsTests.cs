using System.Linq;
using XTerm.Buffer;
using XTerm.Graphics;
using XTerm.Options;

namespace XTerm.Tests.Graphics;

/// <summary>
/// The Kitty graphics protocol, driven the way a program drives it: escape sequences in, pictures
/// and replies out.
///
/// <para>Cell metrics are pinned at 2x3 pixels so a hand-sized payload still covers several cells
/// and the tile arithmetic is checkable by eye.</para>
/// </summary>
[TestClass]
public class KittyGraphicsTests
{
    private const string Esc = "\u001b";
    private const string St = Esc + "\\";

    private const int CellPixelWidth = 2;
    private const int CellPixelHeight = 3;

    private static Terminal Fresh(Action<TerminalOptions>? configure = null)
    {
        var options = new TerminalOptions
        {
            Cols = 30,
            Rows = 12,
            CellWidthPixels = CellPixelWidth,
            CellHeightPixels = CellPixelHeight
        };
        configure?.Invoke(options);
        return new Terminal(options);
    }

    /// <summary>Wraps control data and payload as one Kitty escape sequence.</summary>
    private static string Apc(string control, string payload = "")
        => payload.Length == 0 ? $"{Esc}_G{control}{St}" : $"{Esc}_G{control};{payload}{St}";

    /// <summary>RGBA bytes for a solid picture, base64 as the protocol carries them.</summary>
    private static string SolidRgba(int width, int height, byte r, byte g, byte b, byte a = 255)
    {
        var bytes = new byte[width * height * 4];
        for (int i = 0; i < width * height; i++)
        {
            bytes[i * 4] = r;
            bytes[i * 4 + 1] = g;
            bytes[i * 4 + 2] = b;
            bytes[i * 4 + 3] = a;
        }
        return Convert.ToBase64String(bytes);
    }

    private static BufferCell Cell(Terminal terminal, int col, int screenRow)
        => terminal.Buffer.Lines[terminal.Buffer.YBase + screenRow]![col];

    private static (byte R, byte G, byte B, byte A) Pixel(TerminalImage image, int x, int y)
    {
        var span = image.Pixels.Span;
        var at = (y * image.PixelWidth + x) * TerminalImage.BytesPerPixel;
        return (span[at + 2], span[at + 1], span[at], span[at + 3]);
    }

    private static List<string> Replies(Terminal terminal)
    {
        var replies = new List<string>();
        terminal.DataReceived += (_, e) => replies.Add(e.Data);
        return replies;
    }

    // ---- transmit and display -------------------------------------------------------------------

    [TestMethod]
    public void An_rgba_image_is_decoded_and_placed_at_the_cursor()
    {
        var terminal = Fresh();

        // 4x6 pixels over 2x3 cells: two columns by two rows.
        terminal.Write(Apc("a=T,f=32,s=4,v=6,q=2", SolidRgba(4, 6, 200, 100, 50)));

        var placement = ImageAssertions.PlacementAt(terminal, 0, 0);
        placement.Should().NotBeNull();

        var image = ImageAssertions.ImageAt(terminal, 0, 0)!;
        image.PixelWidth.Should().Be(4);
        image.PixelHeight.Should().Be(6);
        (placement!.Value.Cols).Should().Be(2);
        ImageAssertions.RowsOf(terminal, placement.Value.Serial).Should().Be(2);
        Pixel(image, 0, 0).Should().Be((200, 100, 50, (byte)255));

        // Every covered position reads the pixels it should: two pixels across per cell, three down.
        for (int row = 0; row < 2; row++)
        {
            for (int col = 0; col < 2; col++)
            {
                (ImageAssertions.PlacementAt(terminal, col, row)!.Value.Serial).Should().Be(placement.Value.Serial);
                ImageAssertions.SourceAt(terminal, col, row).Should().Be((col * 2, row * 3));
            }
        }
    }

    [TestMethod]
    public void An_rgb_image_is_taken_as_opaque()
    {
        var terminal = Fresh();

        var rgb = Convert.ToBase64String(new byte[] { 10, 20, 30, 40, 50, 60 }); // two pixels
        terminal.Write(Apc("a=T,f=24,s=2,v=1,q=2", rgb));

        var image = ImageAssertions.ImageAt(terminal, 0, 0);
        image.Should().NotBeNull();
        Pixel(image!, 0, 0).Should().Be(((byte)10, (byte)20, (byte)30, (byte)255));
        Pixel(image!, 1, 0).Should().Be(((byte)40, (byte)50, (byte)60, (byte)255));
    }

    [TestMethod]
    public void Transparency_is_kept()
    {
        var terminal = Fresh();
        terminal.Write(Apc("a=T,f=32,s=2,v=3,q=2", SolidRgba(2, 3, 9, 8, 7, a: 64)));

        (Pixel(ImageAssertions.ImageAt(terminal, 0, 0)!, 0, 0).A).Should().Be((byte)64);
    }

    // ---- what chafa actually sends ---------------------------------------------------------------

    /// <summary>
    /// The exact shape <c>chafa -f kitty</c> emits: control data alone in the first sequence with no
    /// semicolon at all, payload in the middle ones, and an empty <c>m=0</c> to finish.
    /// </summary>
    [TestMethod]
    public void A_chunked_transmission_in_chafas_shape_is_assembled()
    {
        var terminal = Fresh();
        var payload = SolidRgba(4, 6, 11, 22, 33);

        var half = payload.Length / 2;
        terminal.Write(Apc("a=T,f=32,s=4,v=6,c=2,r=2,m=1,q=2"));      // control only, no payload
        terminal.Write(Apc("m=1", payload[..half]));
        terminal.Write(Apc("m=1", payload[half..]));
        terminal.Write(Apc("m=0"));                                    // empty terminator

        var image = ImageAssertions.ImageAt(terminal, 0, 0);
        image.Should().NotBeNull();
        Pixel(image!, 0, 0).Should().Be((11, 22, 33, (byte)255));
    }

    /// <summary>
    /// Split at a point that is not a multiple of four, which is where decoding each chunk as it
    /// arrives would corrupt everything after the join.
    /// </summary>
    [TestMethod]
    public void A_chunk_boundary_off_a_base64_quantum_still_assembles()
    {
        var terminal = Fresh();
        var payload = SolidRgba(4, 6, 5, 6, 7);

        var awkward = 5; // deliberately not a multiple of 4
        terminal.Write(Apc("a=T,f=32,s=4,v=6,m=1,q=2", payload[..awkward]));
        terminal.Write(Apc("m=0", payload[awkward..]));

        var image = ImageAssertions.ImageAt(terminal, 0, 0);
        image.Should().NotBeNull();
        Pixel(image!, 0, 0).Should().Be(((byte)5, (byte)6, (byte)7, (byte)255));
    }

    // ---- c and r ---------------------------------------------------------------------------------

    /// <summary>
    /// c and r name a box to fill, and chafa always sends them. A 4x6 picture asked into 4 columns
    /// by 1 row must occupy that, not the two-by-two its own size would give.
    /// </summary>
    [TestMethod]
    public void A_cell_box_stretches_the_picture_to_fit()
    {
        var terminal = Fresh();
        terminal.Write(Apc("a=T,f=32,s=4,v=6,c=4,r=1,q=2", SolidRgba(4, 6, 1, 2, 3)));

        var placement = ImageAssertions.PlacementAt(terminal, 0, 0);
        placement.Should().NotBeNull();
        (placement!.Value.Cols).Should().Be(4);
        ImageAssertions.RowsOf(terminal, placement.Value.Serial).Should().Be(1);

        ImageAssertions.IsImageAt(terminal, 0, 1).Should().BeFalse();   // one row only
        ImageAssertions.IsImageAt(terminal, 3, 0).Should().BeTrue();
    }

    /// <summary>Without them the picture keeps its own size, and the edge tiles are clipped.</summary>
    [TestMethod]
    public void Without_a_cell_box_the_picture_keeps_its_natural_size()
    {
        var terminal = Fresh();
        terminal.Write(Apc("a=T,f=32,s=4,v=6,q=2", SolidRgba(4, 6, 1, 2, 3)));

        var placement = ImageAssertions.PlacementAt(terminal, 0, 0)!.Value;
        placement.Cols.Should().Be(2);
        ImageAssertions.RowsOf(terminal, placement.Serial).Should().Be(2);
    }

    [TestMethod]
    public void A_crop_shows_only_the_part_asked_for()
    {
        var terminal = Fresh();
        terminal.Write(Apc("a=T,f=32,s=8,v=12,x=2,y=3,w=4,h=6,q=2", SolidRgba(8, 12, 1, 2, 3)));

        var placement = ImageAssertions.PlacementAt(terminal, 0, 0)!.Value;
        placement.SrcX.Should().Be(2);
        placement.SrcY.Should().Be(3);
        placement.SrcWidth.Should().Be(4);
        // The run is one row of the crop, so its height is that row's slice rather than the whole.
        (ImageAssertions.PlacementsOn(terminal, 0)
                                       .Where(p => p.Serial == placement.Serial)
                                       .Sum(p => p.SrcHeight)
                        + ImageAssertions.PlacementsOn(terminal, 1)
                                       .Where(p => p.Serial == placement.Serial)
                                       .Sum(p => p.SrcHeight)).Should().Be(6);
    }

    // ---- transmit once, place many ---------------------------------------------------------------

    [TestMethod]
    public void An_image_can_be_transmitted_then_placed_by_id()
    {
        var terminal = Fresh();

        terminal.Write(Apc("a=t,i=7,f=32,s=4,v=6,q=2", SolidRgba(4, 6, 90, 80, 70)));
        ImageAssertions.IsImageAt(terminal, 0, 0).Should().BeFalse();   // a=t shows nothing

        terminal.Write(Apc("a=p,i=7,q=2"));

        var image = ImageAssertions.ImageAt(terminal, 0, 0);
        image.Should().NotBeNull();
        Pixel(image!, 0, 0).Should().Be(((byte)90, (byte)80, (byte)70, (byte)255));
    }

    /// <summary>
    /// Two appearances of one picture share its pixels but are distinct placements, which is what
    /// keeps a renderer from running one strip across the join between them.
    /// </summary>
    [TestMethod]
    public void Two_placements_of_one_image_share_pixels_but_stay_distinct()
    {
        var terminal = Fresh();

        terminal.Write(Apc("a=t,i=3,f=32,s=4,v=6,q=2", SolidRgba(4, 6, 1, 2, 3)));
        terminal.Write($"{Esc}[1;1H");
        terminal.Write(Apc("a=p,i=3,C=1,q=2"));
        terminal.Write($"{Esc}[1;3H");
        terminal.Write(Apc("a=p,i=3,C=1,q=2"));

        var first = ImageAssertions.PlacementAt(terminal, 0, 0);
        var second = ImageAssertions.PlacementAt(terminal, 2, 0);

        first.Should().NotBeNull();
        second.Should().NotBeNull();
        (second!.Value.ImageId).Should().Be(first!.Value.ImageId);
        second.Value.Serial.Should().NotBe(first.Value.Serial);
    }

    [TestMethod]
    public void Placing_an_unknown_id_reports_that_it_is_missing()
    {
        var terminal = Fresh();
        var replies = Replies(terminal);

        terminal.Write(Apc("a=p,i=99"));

        replies.Should().Contain(r => r.Contains("ENOENT"));
        ImageAssertions.IsImageAt(terminal, 0, 0).Should().BeFalse();
    }

    // ---- the cursor -------------------------------------------------------------------------------

    [TestMethod]
    public void The_cursor_lands_below_the_picture_by_default()
    {
        var terminal = Fresh();
        terminal.Write(Apc("a=T,f=32,s=4,v=6,q=2", SolidRgba(4, 6, 1, 2, 3)));

        terminal.Buffer.X.Should().Be(0);
        terminal.Buffer.Y.Should().Be(2);
    }

    [TestMethod]
    public void C_equals_one_leaves_the_cursor_alone()
    {
        var terminal = Fresh();
        terminal.Write($"{Esc}[3;5H");
        terminal.Write(Apc("a=T,f=32,s=4,v=6,C=1,q=2", SolidRgba(4, 6, 1, 2, 3)));

        terminal.Buffer.X.Should().Be(4);
        terminal.Buffer.Y.Should().Be(2);
        ImageAssertions.IsImageAt(terminal, 4, 2).Should().BeTrue();
    }

    // ---- replies -----------------------------------------------------------------------------------

    /// <summary>
    /// A query is how a program finds out the terminal speaks this protocol. It must answer, and it
    /// must not draw anything -- programs probe with a real image and expect their output untouched.
    /// </summary>
    [TestMethod]
    public void A_query_replies_and_places_nothing()
    {
        var terminal = Fresh();
        var replies = Replies(terminal);

        terminal.Write(Apc("i=31,s=1,v=1,a=q,t=d,f=24", "AAAA"));

        replies.Should().ContainSingle().Which.Should().Be($"{Esc}_Gi=31;OK{St}");
        ImageAssertions.IsImageAt(terminal, 0, 0).Should().BeFalse();
        terminal.Buffer.X.Should().Be(0);
        terminal.Buffer.Y.Should().Be(0);
    }

    [TestMethod]
    [DataRow(1, false, "q=1 suppresses success")]
    [DataRow(2, false, "q=2 suppresses everything")]
    [DataRow(0, true, "q=0 says so")]
    public void Quiet_is_honoured(int quiet, bool expectReply, string what)
    {
        var terminal = Fresh();
        var replies = Replies(terminal);

        terminal.Write(Apc($"i=31,s=1,v=1,a=q,t=d,f=24,q={quiet}", "AAAA"));

        ((replies.Count > 0 == expectReply)).Should().BeTrue(what);
    }

    [TestMethod]
    public void A_failure_is_still_reported_under_q_equals_one()
    {
        var terminal = Fresh();
        var replies = Replies(terminal);

        terminal.Write(Apc("a=p,i=99,q=1"));

        replies.Should().Contain(r => r.Contains("ENOENT"));
    }

    /// <summary>
    /// Reading a file the client names would have the terminal open a path on its say-so, and this
    /// library runs inside hosts that may hold more privilege than the program they run.
    /// </summary>
    [TestMethod]
    [DataRow('f', "a file")]
    [DataRow('t', "a temporary file")]
    [DataRow('s', "shared memory")]
    public void Transmission_from_outside_the_escape_sequence_is_refused(char medium, string what)
    {
        var terminal = Fresh();
        var replies = Replies(terminal);

        terminal.Write(Apc($"a=T,i=5,f=32,s=4,v=6,t={medium}", "L3RtcC94"));

        replies.Any(r => r.Contains("ENOTSUP")).Should().BeTrue($"{what} should be refused");
        ImageAssertions.IsImageAt(terminal, 0, 0).Should().BeFalse();
    }

    /// <summary>
    /// Animating an image that was never transmitted is ENOENT rather than ENOTSUP: the action is
    /// supported, the picture is what is missing. See <c>KittyAnimationTests</c> for the rest.
    /// </summary>
    [TestMethod]
    public void Animating_an_unknown_image_says_so()
    {
        var terminal = Fresh();
        var replies = Replies(terminal);

        terminal.Write(Apc("a=a,i=5"));

        replies.Should().Contain(r => r.Contains("ENOENT"));
    }

    /// <summary>An action letter from no revision of the protocol is still refused outright.</summary>
    [TestMethod]
    public void An_unknown_action_is_refused_rather_than_ignored()
    {
        var terminal = Fresh();
        var replies = Replies(terminal);

        terminal.Write(Apc("a=w,i=5"));

        replies.Should().Contain(r => r.Contains("ENOTSUP"));
    }

    // ---- deletion ------------------------------------------------------------------------------------

    [TestMethod]
    public void Delete_all_clears_the_screen_of_pictures()
    {
        var terminal = Fresh();
        terminal.Write(Apc("a=T,f=32,s=4,v=6,q=2", SolidRgba(4, 6, 1, 2, 3)));
        ImageAssertions.IsImageAt(terminal, 0, 0).Should().BeTrue();

        terminal.Write(Apc("a=d,d=a,q=2"));

        ImageAssertions.IsImageAt(terminal, 0, 0).Should().BeFalse();
    }

    [TestMethod]
    public void Delete_by_id_removes_only_that_picture()
    {
        var terminal = Fresh();

        terminal.Write(Apc("a=T,i=1,f=32,s=4,v=6,C=1,q=2", SolidRgba(4, 6, 1, 1, 1)));
        terminal.Write($"{Esc}[5;1H");
        terminal.Write(Apc("a=T,i=2,f=32,s=4,v=6,C=1,q=2", SolidRgba(4, 6, 2, 2, 2)));

        terminal.Write(Apc("a=d,d=i,i=1,q=2"));

        ImageAssertions.IsImageAt(terminal, 0, 0).Should().BeFalse();
        ImageAssertions.IsImageAt(terminal, 0, 4).Should().BeTrue();
    }

    /// <summary>
    /// A lower-case target frees the placement but keeps the pixels, so the picture can be shown
    /// again without retransmitting it.
    /// </summary>
    [TestMethod]
    public void Delete_keeps_the_image_unless_the_target_is_upper_case()
    {
        var terminal = Fresh();
        terminal.Write(Apc("a=t,i=4,f=32,s=4,v=6,q=2", SolidRgba(4, 6, 7, 7, 7)));

        terminal.Write(Apc("a=d,d=i,i=4,q=2"));
        terminal.Write(Apc("a=p,i=4,q=2"));
        ImageAssertions.IsImageAt(terminal, 0, 0).Should().BeTrue();

        terminal.Write(Apc("a=d,d=I,i=4,q=2"));
        var replies = Replies(terminal);
        terminal.Write(Apc("a=p,i=4"));
        replies.Should().Contain(r => r.Contains("ENOENT"));
    }

    // ---- malformed input ------------------------------------------------------------------------------

    /// <summary>
    /// The payload is untrusted output from another process. None of these may throw, and the
    /// parser has to come back afterwards.
    /// </summary>
    [TestMethod]
    [DataRow("a=T,f=32,s=4,v=6", "!!!not base64!!!", "bad base64")]
    [DataRow("a=T,f=32,s=999,v=999", "AAAA", "payload smaller than declared")]
    [DataRow("a=T,f=32", "AAAA", "no dimensions for a raw format")]
    [DataRow("a=T,f=100", "AAAA", "not a png")]
    [DataRow("a=T,f=77,s=1,v=1", "AAAA", "unknown format")]
    [DataRow("", "AAAA", "no control data at all")]
    [DataRow("a=T,f=32,s=1,v=1,zzz=9", "AAAAAA==", "an unknown key")]
    public void Malformed_commands_are_survived(string control, string payload, string what)
    {
        var terminal = Fresh();

        var exception = Record.Exception(() => terminal.Write(Apc(control, payload)));
        (exception is null).Should().BeTrue($"{what} threw: {exception}");

        terminal.Write("OK");
        terminal.GetLine(terminal.Buffer.YBase + terminal.Buffer.Y).Should().Contain("OK");
    }

    [TestMethod]
    public void An_image_larger_than_the_budget_is_refused()
    {
        var terminal = Fresh(o => o.MaxSixelPixels = 4);
        var replies = Replies(terminal);

        terminal.Write(Apc("a=T,i=2,f=32,s=100,v=100", SolidRgba(100, 100, 1, 2, 3)));

        replies.Should().Contain(r => r.Contains("EFBIG"));
        ImageAssertions.IsImageAt(terminal, 0, 0).Should().BeFalse();
    }

    [TestMethod]
    public void Kitty_can_be_switched_off()
    {
        var terminal = Fresh(o => o.KittyGraphicsEnabled = false);
        var replies = Replies(terminal);

        terminal.Write(Apc("a=T,f=32,s=4,v=6,q=0", SolidRgba(4, 6, 1, 2, 3)));

        ImageAssertions.IsImageAt(terminal, 0, 0).Should().BeFalse();
        replies.Should().BeEmpty();
    }

    /// <summary>An abandoned transmission must not be appended to whatever comes next.</summary>
    [TestMethod]
    public void An_interrupted_transmission_is_dropped()
    {
        var terminal = Fresh();

        terminal.Write($"{Esc}_Ga=T,f=32,s=4,v=6,m=1,q=2;AAAA");   // no terminator
        terminal.Write($"{Esc}[1;1H");                              // something else entirely

        terminal.Write(Apc("a=T,f=32,s=4,v=6,q=2", SolidRgba(4, 6, 60, 60, 60)));

        var image = ImageAssertions.ImageAt(terminal, 0, 0);
        image.Should().NotBeNull();
        Pixel(image!, 0, 0).Should().Be(((byte)60, (byte)60, (byte)60, (byte)255));
    }

    // ---- lifetime, the same as any other cell content --------------------------------------------------

    [TestMethod]
    public void A_kitty_picture_is_an_overlay_rather_than_content()
    {
        var terminal = Fresh();
        terminal.Write(Apc("a=T,f=32,s=4,v=6,q=2", SolidRgba(4, 6, 1, 2, 3)));

        // Printing does not remove it. A Kitty placement is an overlay the z-index orders against
        // the text, not content the way a Sixel is, so the character lands and the picture stays --
        // hidden while it is in front, and there again the moment it is deleted.
        terminal.Write($"{Esc}[1;1HX");
        (Cell(terminal, 0, 0).Content).Should().Be("X");
        ImageAssertions.IsImageAt(terminal, 0, 0).Should().BeTrue();
        ImageAssertions.IsImageAt(terminal, 1, 0).Should().BeTrue();

        // Erasing does remove it, whichever protocol placed it: a cleared cell is blank, and a
        // picture showing through one would be a leak.
        terminal.Write($"{Esc}[2J");
        ImageAssertions.IsImageAt(terminal, 0, 0).Should().BeFalse();
        ImageAssertions.IsImageAt(terminal, 1, 0).Should().BeFalse();
    }
}
