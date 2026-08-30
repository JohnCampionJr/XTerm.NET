using XTerm.Graphics;
using XTerm.Options;

namespace XTerm.Tests.Graphics;

/// <summary>
/// Decoding a DECSIXEL payload into pixels. Driven through <see cref="Terminal.Write"/> rather
/// than against the decoder directly, because the seam that matters is the whole path: parser
/// hook, streamed payload, decode, and an image landing on a cell.
///
/// <para>The payloads here are small enough to work out by hand. A Sixel data character carries
/// six stacked pixels as the low six bits of <c>c - 0x3F</c>, so '@' is the top pixel alone and
/// '~' is all six.</para>
/// </summary>
[TestClass]
public class SixelDecoderTests
{
    private const string Esc = "\u001b";
    private const string St = Esc + "\\";

    /// <summary>Background select 1: pixels left unset stay transparent.</summary>
    private const int Transparent = 1;

    /// <summary>Background select 0: pixels left unset take the terminal background.</summary>
    private const int OpaqueBackground = 0;

    private static Terminal Fresh(Action<TerminalOptions>? configure = null)
    {
        var options = new TerminalOptions { Cols = 40, Rows = 10 };
        configure?.Invoke(options);
        return new Terminal(options);
    }

    private static void WriteSixel(Terminal terminal, string body, int backgroundSelect = Transparent)
        => terminal.Write($"{Esc}P0;{backgroundSelect};0q{body}{St}");

    private static TerminalImage? TryDecode(string body, int backgroundSelect = Transparent,
                                           Action<TerminalOptions>? configure = null)
    {
        var terminal = Fresh(configure);
        WriteSixel(terminal, body, backgroundSelect);
        return (terminal.Buffer.Lines[0]!.TryGetImageAt(0, out var __i1) ? __i1 : null);
    }

    private static TerminalImage Decode(string body, int backgroundSelect = Transparent,
                                        Action<TerminalOptions>? configure = null)
    {
        var image = TryDecode(body, backgroundSelect, configure);
        (image is not null).Should().BeTrue("no image reached the buffer");
        return image!;
    }

    private static (byte R, byte G, byte B, byte A) Pixel(TerminalImage image, int x, int y)
    {
        var span = image.Pixels.Span;
        var offset = (y * image.PixelWidth + x) * TerminalImage.BytesPerPixel;
        return (span[offset + 2], span[offset + 1], span[offset], span[offset + 3]);
    }

    private static readonly (byte R, byte G, byte B, byte A) Red = (255, 0, 0, 255);
    private static readonly (byte R, byte G, byte B, byte A) Green = (0, 255, 0, 255);
    private static readonly (byte R, byte G, byte B, byte A) Blue = (0, 0, 255, 255);
    private static readonly (byte R, byte G, byte B, byte A) Clear = (0, 0, 0, 0);

    [TestMethod]
    public void A_single_sixel_sets_the_top_pixel_of_its_band()
    {
        var image = Decode("#0;2;100;0;0@");

        image.PixelWidth.Should().Be(1);
        image.PixelHeight.Should().Be(6);
        Pixel(image, 0, 0).Should().Be(Red);
        Pixel(image, 0, 1).Should().Be(Clear);
    }

    [TestMethod]
    public void A_full_sixel_sets_all_six_pixels_of_its_band()
    {
        var image = Decode("#0;2;0;100;0~");

        image.PixelHeight.Should().Be(6);
        for (int y = 0; y < 6; y++)
            Pixel(image, 0, y).Should().Be(Green);
    }

    [TestMethod]
    public void A_question_mark_advances_without_drawing()
    {
        var image = Decode("#0;2;100;0;0??@");

        image.PixelWidth.Should().Be(3);
        Pixel(image, 0, 0).Should().Be(Clear);
        Pixel(image, 1, 0).Should().Be(Clear);
        Pixel(image, 2, 0).Should().Be(Red);
    }

    [TestMethod]
    public void A_repeat_introducer_repeats_the_following_sixel()
    {
        var image = Decode("#0;2;0;0;100!4~");

        image.PixelWidth.Should().Be(4);
        image.PixelHeight.Should().Be(6);
        Pixel(image, 0, 0).Should().Be(Blue);
        Pixel(image, 3, 5).Should().Be(Blue);
    }

    [TestMethod]
    public void A_graphics_newline_starts_the_next_band_six_rows_down()
    {
        var image = Decode("#0;2;100;0;0@-#0;2;0;0;100@");

        image.PixelHeight.Should().Be(12);
        Pixel(image, 0, 0).Should().Be(Red);
        Pixel(image, 0, 6).Should().Be(Blue);
    }

    [TestMethod]
    public void A_graphics_carriage_return_returns_to_the_left_of_the_same_band()
    {
        // Three red columns, back to the start, then one blue sixel over the first of them.
        var image = Decode("#0;2;100;0;0!3~$#1;2;0;0;100@");

        image.PixelWidth.Should().Be(3);
        Pixel(image, 0, 0).Should().Be(Blue);
        Pixel(image, 0, 1).Should().Be(Red);
        Pixel(image, 1, 0).Should().Be(Red);
    }

    [TestMethod]
    public void Raster_attributes_declare_the_image_size()
    {
        // Six rows are drawn; the raster attribute says the image is two rows tall.
        var image = Decode("\"1;1;3;2#0;2;100;0;0!3~");

        image.PixelWidth.Should().Be(3);
        image.PixelHeight.Should().Be(2);
        Pixel(image, 2, 1).Should().Be(Red);
    }

    [TestMethod]
    public void An_image_without_raster_attributes_is_sized_by_what_it_drew()
    {
        var image = Decode("#0;2;100;0;0!7~");

        image.PixelWidth.Should().Be(7);
        image.PixelHeight.Should().Be(6);
    }

    /// <summary>
    /// Sixel's hue ring is rotated 120 degrees from the usual one -- hue 0 is blue, not red. A
    /// conversion that looks correct but skips the rotation produces plausible, wrong colours.
    /// </summary>
    [TestMethod]
    [DataRow(0, (byte)0, (byte)0, (byte)255)]     // hue 0   -> blue
    [DataRow(120, (byte)255, (byte)0, (byte)0)]   // hue 120 -> red
    [DataRow(240, (byte)0, (byte)255, (byte)0)]   // hue 240 -> green
    public void Hls_colours_are_converted_on_sixels_hue_ring(int hue, byte r, byte g, byte b)
    {
        var image = Decode($"#0;1;{hue};50;100@");

        Pixel(image, 0, 0).Should().Be((r, g, b, (byte)255));
    }

    [TestMethod]
    public void An_hls_colour_with_no_saturation_is_grey()
    {
        var image = Decode("#0;1;120;50;0@");

        var pixel = Pixel(image, 0, 0);
        pixel.G.Should().Be(pixel.R);
        pixel.B.Should().Be(pixel.G);
        pixel.R.Should().BeInRange(126, 129);
    }

    [TestMethod]
    public void Selecting_a_register_without_defining_it_uses_the_vt340_default()
    {
        // Register 2 is the VT340's red, 80/13/13 percent.
        var image = Decode("#2~");

        var pixel = Pixel(image, 0, 0);
        pixel.R.Should().Be((byte)204);
        pixel.G.Should().Be((byte)33);
        pixel.B.Should().Be((byte)33);
    }

    [TestMethod]
    public void Unset_pixels_are_transparent_under_background_select_one()
    {
        var image = Decode("#0;2;100;0;0@", Transparent);

        (Pixel(image, 0, 5).A).Should().Be((byte)0);
    }

    [TestMethod]
    public void Unset_pixels_take_the_terminal_background_otherwise()
    {
        var image = Decode("#0;2;100;0;0@", OpaqueBackground);

        var background = Pixel(image, 0, 5);
        background.A.Should().Be((byte)255);
        Pixel(image, 0, 0).Should().Be(Red);
    }

    /// <summary>
    /// A payload declares no size until it has been drawn, so without a ceiling a process can make
    /// the terminal allocate until it dies.
    /// </summary>
    [TestMethod]
    public void An_image_larger_than_the_budget_is_discarded()
    {
        var image = TryDecode("\"1;1;4000;4000#0;2;100;0;0~",
            configure: o => o.MaxSixelPixels = 1000);

        image.Should().BeNull();
    }

    [TestMethod]
    public void An_image_that_grows_past_the_budget_while_drawing_is_discarded()
    {
        // No raster attribute, so the size only becomes apparent as it is drawn.
        var image = TryDecode("#0;2;100;0;0!5000~", configure: o => o.MaxSixelPixels = 600);

        image.Should().BeNull();
    }

    [TestMethod]
    public void An_abandoned_payload_produces_no_image()
    {
        var terminal = Fresh();

        // CAN mid-payload: the sequence is dropped rather than terminated.
        terminal.Write($"{Esc}P0;1;0q#0;2;100;0;0!20~\u0018");

        (terminal.Buffer.Lines[0]!.TryGetImageAt(0, out var __i2) ? __i2 : null).Should().BeNull();
    }

    [TestMethod]
    public void An_empty_payload_produces_no_image()
    {
        var terminal = Fresh();
        terminal.Write($"{Esc}P0;1;0q{St}");

        (terminal.Buffer.Lines[0]!.TryGetImageAt(0, out var __i3) ? __i3 : null).Should().BeNull();
    }

    /// <summary>
    /// The payload is untrusted output from someone else's process. Nonsense in it must not reach
    /// the caller as an exception.
    /// </summary>
    [TestMethod]
    [DataRow("#", "a bare colour introducer")]
    [DataRow("#;;;;;", "empty colour parameters")]
    [DataRow("#999999999;2;999;999;999~", "absurd register and channel values")]
    [DataRow("!", "a bare repeat introducer")]
    [DataRow("!999999999~", "an absurd repeat count")]
    [DataRow("\"", "a bare raster introducer")]
    [DataRow("\"0;0;0;0~", "zero raster dimensions")]
    [DataRow("$$$---", "controls with no data")]
    [DataRow("#0;7;1;2;3~", "an unknown colour system")]
    [DataRow("\n\r\t   ~", "whitespace between the data")]
    public void Malformed_payloads_are_survived(string body, string what)
    {
        var terminal = Fresh();

        var exception = Record.Exception(() => WriteSixel(terminal, body));

        (exception is null).Should().BeTrue($"{what} threw: {exception}");

        // And the parser still comes back.
        terminal.Write("OK");
        terminal.GetLine(terminal.Buffer.Y).Should().Contain("OK");
    }

    [TestMethod]
    public void A_payload_split_across_writes_decodes_the_same()
    {
        var whole = Fresh();
        WriteSixel(whole, "#0;2;100;0;0!4~-#1;2;0;0;100!4~");

        var split = Fresh();
        split.Write($"{Esc}P0;1;0q#0;2;10");
        split.Write("0;0;0!4~-#1;2;0");
        split.Write($";0;100!4~{St}");

        var a = whole.Buffer.Lines[0]!.TryGetImageAt(0, out var wholeImage) ? wholeImage : null;
        var b = split.Buffer.Lines[0]!.TryGetImageAt(0, out var splitImage) ? splitImage : null;

        a.Should().NotBeNull();
        b.Should().NotBeNull();
        (b!.PixelWidth).Should().Be(a!.PixelWidth);
        b.PixelHeight.Should().Be(a.PixelHeight);
        a.Pixels.Span.SequenceEqual(b.Pixels.Span).Should().BeTrue("the same payload decoded differently depending on where the write boundaries fell");
    }

    [TestMethod]
    public void Sixel_can_be_switched_off_entirely()
    {
        var terminal = Fresh(o => o.SixelEnabled = false);
        WriteSixel(terminal, "#0;2;100;0;0~");

        (terminal.Buffer.Lines[0]!.TryGetImageAt(0, out var __i4) ? __i4 : null).Should().BeNull();
    }

    [TestMethod]
    public void The_tile_grid_follows_the_configured_cell_size()
    {
        // A 4x12 image over 2x3 cells covers two columns and four rows.
        var image = Decode("#0;2;100;0;0!4~-!4~", configure: o =>
        {
            o.CellWidthPixels = 2;
            o.CellHeightPixels = 3;
        });

        image.PixelWidth.Should().Be(4);
        image.PixelHeight.Should().Be(12);
        image.Cols.Should().Be(2);
        image.Rows.Should().Be(4);
    }

    [TestMethod]
    public void An_edge_tile_reports_only_the_pixels_it_actually_covers()
    {
        // 7 pixels wide over 2-pixel cells: four columns, the last holding a single pixel.
        var image = Decode("#0;2;100;0;0!7~", configure: o =>
        {
            o.CellWidthPixels = 2;
            o.CellHeightPixels = 3;
        });

        image.Cols.Should().Be(4);

        image.TryGetTileSource(0, 0, out var x, out var y, out var w, out var h).Should().BeTrue();
        (x, y, w, h).Should().Be((0, 0, 2, 3));

        image.TryGetTileSource(3, 0, out x, out y, out w, out h).Should().BeTrue();
        (x, y, w, h).Should().Be((6, 0, 1, 3));

        image.TryGetTileSource(4, 0, out _, out _, out _, out _).Should().BeFalse();
    }
}
