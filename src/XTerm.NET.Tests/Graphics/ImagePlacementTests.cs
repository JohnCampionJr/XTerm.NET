using XTerm.Graphics;

namespace XTerm.Tests.Graphics;

/// <summary>
/// A placement is what a cell references: which part of a picture it shows, at what size. Kitty
/// transmits an image once and may then show it several times, cropped and scaled differently, so
/// the view of the pixels has to live apart from the pixels.
///
/// <para>The load-bearing detail is that the two scalings are different arithmetic rather than one
/// formula with a special case. Natural keeps a picture at its own size and lets the edge tiles fall
/// short; Stretched divides the source across the cell box it was told to fill. Folding them
/// together would quietly resample every Sixel image, which is what the first test here guards.</para>
/// </summary>
[TestClass]
public class ImagePlacementTests
{
    private const int CellWidth = 14;
    private const int CellHeight = 15;

    private static TerminalImage Image(int width, int height)
        => new(new byte[width * height * TerminalImage.BytesPerPixel], width, height, CellWidth, CellHeight);

    // ---- the regression guard ---------------------------------------------------------------

    /// <summary>
    /// A natural placement must lay its tiles exactly where the image itself would. Sixel goes
    /// through a placement now, and a difference of a pixel or two per tile would be a resampled
    /// picture with nothing to report it.
    /// </summary>
    /// <remarks>
    /// The dimensions are the ones a real Sixel produced in testing: 1160x870 over a 14x15 cell,
    /// which needs 83 columns. 83 times 14 is 1162, so the source does NOT divide evenly into the
    /// cells it occupies — precisely the case where a proportional division would disagree.
    /// </remarks>
    [TestMethod]
    public void A_natural_placement_lays_tiles_exactly_where_the_image_does()
    {
        var image = Image(1160, 870);
        var placement = ImagePlacement.Natural(image);

        placement.Cols.Should().Be(image.Cols);
        placement.Rows.Should().Be(image.Rows);

        for (int row = 0; row < image.Rows; row++)
        {
            for (int col = 0; col < image.Cols; col++)
            {
                var onImage = image.TryGetTileSource(col, row, out var ix, out var iy, out var iw, out var ih);
                var onPlacement = placement.TryGetTileSource(col, row, out var px, out var py, out var pw, out var ph);

                onPlacement.Should().Be(onImage);
                ((ix, iy, iw, ih) == (px, py, pw, ph)).Should().BeTrue($"tile ({col},{row}) moved: image gave ({ix},{iy},{iw},{ih}), placement gave ({px},{py},{pw},{ph})");
            }
        }
    }

    /// <summary>
    /// And the two modes really do disagree, which is why both exist. Documented here so nobody
    /// simplifies one into the other.
    /// </summary>
    [TestMethod]
    public void The_two_scalings_disagree_on_an_image_that_does_not_divide_evenly()
    {
        var image = Image(1160, 870);
        var natural = ImagePlacement.Natural(image);
        var stretched = new ImagePlacement(image, 0, 0, 0, 1160, 870,
                                           natural.Cols, natural.Rows, ImageScaling.Stretched);

        natural.TryGetTileSource(0, 0, out _, out _, out var naturalWidth, out _);
        stretched.TryGetTileSource(0, 0, out _, out _, out var stretchedWidth, out _);

        naturalWidth.Should().Be(14);
        stretchedWidth.Should().Be(13);
    }

    // ---- natural ------------------------------------------------------------------------------

    [TestMethod]
    public void A_natural_edge_tile_reports_only_the_pixels_it_covers()
    {
        // Seven pixels wide over 14-pixel cells: one column holding half a cell.
        var placement = ImagePlacement.Natural(Image(7, 15));

        placement.Cols.Should().Be(1);
        placement.TryGetTileSource(0, 0, out var x, out var y, out var w, out var h).Should().BeTrue();
        (x, y, w, h).Should().Be((0, 0, 7, 15));

        placement.GetTileCoverage(w, h, out var cellsWide, out var cellsHigh);
        cellsWide.Should().Be(0.5);
        cellsHigh.Should().Be(1.0);
    }

    // ---- stretched ----------------------------------------------------------------------------

    /// <summary>What `c=` and `r=` mean: fill the box asked for, whatever the source size.</summary>
    [TestMethod]
    public void A_stretched_placement_fills_the_cell_box_it_was_given()
    {
        // 40x40 into 4 columns by 2 rows, which is what chafa asks for.
        var image = Image(40, 40);
        var placement = new ImagePlacement(image, 0, 0, 0, 40, 40, 4, 2, ImageScaling.Stretched);

        placement.TryGetTileSource(0, 0, out var x, out var y, out var w, out var h).Should().BeTrue();
        (x, y, w, h).Should().Be((0, 0, 10, 20));

        placement.TryGetTileSource(3, 1, out x, out y, out w, out h).Should().BeTrue();
        (x, y, w, h).Should().Be((30, 20, 10, 20));
    }

    /// <summary>Every tile fills its cell, so the destination is never scaled down.</summary>
    [TestMethod]
    public void A_stretched_tile_always_covers_a_whole_cell()
    {
        var placement = new ImagePlacement(Image(41, 41), 0, 0, 0, 41, 41, 4, 2, ImageScaling.Stretched);

        placement.TryGetTileSource(3, 1, out _, out _, out var w, out var h);
        placement.GetTileCoverage(w, h, out var cellsWide, out var cellsHigh);

        cellsWide.Should().Be(1.0);
        cellsHigh.Should().Be(1.0);
    }

    /// <summary>
    /// Tiles must abut exactly. Rounding each tile's own width independently would leave a seam of
    /// dropped pixels, or overlap and draw a column twice.
    /// </summary>
    [TestMethod]
    public void Stretched_tiles_meet_without_a_seam_or_an_overlap()
    {
        // 41 does not divide by 4, so the rounding has somewhere to go wrong.
        var placement = new ImagePlacement(Image(41, 41), 0, 0, 0, 41, 41, 4, 3, ImageScaling.Stretched);

        int nextX = 0;
        for (int col = 0; col < placement.Cols; col++)
        {
            placement.TryGetTileSource(col, 0, out var x, out _, out var w, out _);
            ((x == nextX)).Should().BeTrue($"column {col} starts at {x}, but the one before it ended at {nextX}");
            nextX = x + w;
        }
        nextX.Should().Be(41);

        int nextY = 0;
        for (int row = 0; row < placement.Rows; row++)
        {
            placement.TryGetTileSource(0, row, out _, out var y, out _, out var h);
            ((y == nextY)).Should().BeTrue($"row {row} starts at {y}, but the one before it ended at {nextY}");
            nextY = y + h;
        }
        nextY.Should().Be(41);
    }

    // ---- cropping -----------------------------------------------------------------------------

    [TestMethod]
    public void A_crop_offsets_every_tile()
    {
        var placement = new ImagePlacement(Image(100, 100), 0, 20, 30, 40, 60, 2, 3, ImageScaling.Stretched);

        placement.TryGetTileSource(0, 0, out var x, out var y, out var w, out var h).Should().BeTrue();
        (x, y, w, h).Should().Be((20, 30, 20, 20));

        placement.TryGetTileSource(1, 2, out x, out y, out w, out h).Should().BeTrue();
        (x, y, w, h).Should().Be((40, 70, 20, 20));
    }

    /// <summary>
    /// The crop arrives from another process. A rectangle that runs off the edge is clamped to what
    /// exists rather than refused — a picture slightly smaller than asked for beats no picture.
    /// </summary>
    [TestMethod]
    public void A_crop_running_off_the_edge_is_clamped()
    {
        var placement = new ImagePlacement(Image(100, 100), 0, 80, 80, 500, 500, 2, 2, ImageScaling.Stretched);

        placement.SourceWidth.Should().Be(20);
        placement.SourceHeight.Should().Be(20);

        placement.TryGetTileSource(1, 1, out var x, out var y, out var w, out var h).Should().BeTrue();
        (x, y, w, h).Should().Be((90, 90, 10, 10));
    }

    // ---- bounds -------------------------------------------------------------------------------

    [TestMethod]
    public void A_tile_outside_the_placement_is_refused()
    {
        var placement = new ImagePlacement(Image(40, 40), 0, 0, 0, 40, 40, 4, 2, ImageScaling.Stretched);

        placement.TryGetTileSource(4, 0, out _, out _, out _, out _).Should().BeFalse();
        placement.TryGetTileSource(0, 2, out _, out _, out _, out _).Should().BeFalse();
        placement.TryGetTileSource(-1, 0, out _, out _, out _, out _).Should().BeFalse();
    }

    [TestMethod]
    public void A_placement_covering_no_cells_is_refused()
    {
        var image = Image(40, 40);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => new ImagePlacement(image, 0, 0, 0, 40, 40, 0, 2, ImageScaling.Stretched));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => new ImagePlacement(image, 0, 0, 0, 40, 0, 4, 2, ImageScaling.Stretched));
    }

    /// <summary>
    /// Two placements of one picture share its pixels, so a host keys its texture on the image and
    /// gets one upload for both.
    /// </summary>
    [TestMethod]
    public void Two_placements_of_one_image_share_its_pixels()
    {
        var image = Image(40, 40);
        var first = ImagePlacement.Natural(image);
        var second = new ImagePlacement(image, 7, 0, 0, 20, 20, 2, 1, ImageScaling.Stretched);

        first.Image.Should().BeSameAs(image);
        second.Image.Should().BeSameAs(image);
        second.Should().NotBeSameAs(first);
        second.Id.Should().Be(7);
    }

    // ---- pixel offsets within the first cell ------------------------------------------------------

    /// <summary>
    /// A picture 28 pixels wide over a 14 pixel cell, shifted 4 pixels right. It still owns two
    /// cells -- the protocol is explicit that an offset "is not added to the number of
    /// rows/columns" -- so the last 4 pixels fall off the end and are clipped.
    /// </summary>
    private static ImagePlacement Shifted(int offsetX, int offsetY)
        => new(Image(28, 30), id: 0,
               sourceX: 0, sourceY: 0, sourceWidth: 28, sourceHeight: 30,
               cols: 2, rows: 2, ImageScaling.Natural, zIndex: 0,
               offsetX: offsetX, offsetY: offsetY);

    [TestMethod]
    public void An_offset_does_not_add_columns_or_rows()
    {
        var placement = Shifted(4, 5);

        placement.Cols.Should().Be(2);
        placement.Rows.Should().Be(2);
    }

    /// <summary>
    /// The leading tile starts partway into its cell and shows correspondingly fewer pixels. This is
    /// the case the older size-only call cannot express, which is why the layout call exists.
    /// </summary>
    [TestMethod]
    public void The_first_tile_starts_at_the_offset_and_is_shorter_for_it()
    {
        var placement = Shifted(4, 5);

        placement.TryGetTileLayout(0, 0, out var x, out var y, out var w, out var h,
                                               out var offX, out var offY, out var wide, out var high).Should().BeTrue();

        x.Should().Be(0);
        y.Should().Be(0);
        w.Should().Be(10);                       // 14 - 4
        h.Should().Be(10);                       // 15 - 5
        offX.Should().BeApproximately(4 / 14.0, Math.Pow(10, -6));
        offY.Should().BeApproximately(5 / 15.0, Math.Pow(10, -6));
        wide.Should().BeApproximately(10 / 14.0, Math.Pow(10, -6));
        high.Should().BeApproximately(10 / 15.0, Math.Pow(10, -6));
    }

    /// <summary>
    /// The tile after it fills its cell from the left, continuing from where the first stopped --
    /// no gap and no repeated pixels at the join.
    /// </summary>
    [TestMethod]
    public void Later_tiles_continue_from_the_first_without_a_seam()
    {
        var placement = Shifted(4, 0);

        placement.TryGetTileLayout(0, 0, out _, out _, out var firstWidth, out _,
                                               out _, out _, out _, out _).Should().BeTrue();
        placement.TryGetTileLayout(1, 0, out var x, out _, out var w, out _,
                                               out var offX, out _, out var wide, out _).Should().BeTrue();

        x.Should().Be(firstWidth);               // starts exactly where the first ended
        offX.Should().Be(0);                     // and at the left edge of its own cell
        w.Should().Be(14);
        wide.Should().BeApproximately(1.0, Math.Pow(10, -6));
    }

    /// <summary>What runs past the last cell of the box is clipped, not wrapped or shrunk to fit.</summary>
    [TestMethod]
    public void Pixels_pushed_past_the_last_cell_are_clipped()
    {
        var placement = Shifted(4, 0);

        var total = 0;
        for (int col = 0; col < placement.Cols; col++)
        {
            placement.TryGetTileLayout(col, 0, out _, out _, out var w, out _,
                                                   out _, out _, out _, out _).Should().BeTrue();
            total += w;
        }

        // Two cells hold 28 pixels; four of them went to the offset, so four of the picture are lost.
        total.Should().Be(24);
    }

    /// <summary>
    /// The protocol requires an offset smaller than the cell. A larger one is clamped rather than
    /// refused -- it arrives from another process, and shifting the picture entirely out of its own
    /// first cell is not a meaningful request to honour.
    /// </summary>
    [TestMethod]
    public void An_offset_of_a_whole_cell_or_more_is_clamped()
    {
        (Shifted(CellWidth, 0).OffsetX).Should().Be(CellWidth - 1);
        (Shifted(0, CellHeight * 3).OffsetY).Should().Be(CellHeight - 1);
        (Shifted(-5, 0).OffsetX).Should().Be(0);
    }

    /// <summary>
    /// With no offset the layout call must agree with the older source-only one, tile for tile, in
    /// both scalings. They are the same arithmetic and must not drift apart.
    /// </summary>
    [TestMethod]
    public void Without_an_offset_the_layout_matches_the_source_call()
    {
        var natural = ImagePlacement.Natural(Image(1160, 870));
        var stretched = new ImagePlacement(Image(97, 61), 0, 0, 0, 97, 61, 7, 3, ImageScaling.Stretched);

        foreach (var placement in new[] { natural, stretched })
        {
            for (int row = 0; row < placement.Rows; row++)
            {
                for (int col = 0; col < placement.Cols; col++)
                {
                    var bySource = placement.TryGetTileSource(col, row, out var sx, out var sy, out var sw, out var sh);
                    var byLayout = placement.TryGetTileLayout(col, row, out var lx, out var ly, out var lw, out var lh,
                                                              out var offX, out var offY, out _, out _);

                    byLayout.Should().Be(bySource);
                    ((sx, sy, sw, sh) == (lx, ly, lw, lh)).Should().BeTrue($"tile ({col},{row}) disagrees: source ({sx},{sy},{sw},{sh}) vs layout ({lx},{ly},{lw},{lh})");
                    offX.Should().Be(0);
                    offY.Should().Be(0);
                }
            }
        }
    }
}