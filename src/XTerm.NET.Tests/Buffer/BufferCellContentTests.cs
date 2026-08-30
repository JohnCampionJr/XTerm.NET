using XTerm.Buffer;

namespace XTerm.Tests.Buffer;

/// <summary>
/// BufferCell.Content is derived rather than stored -- from CodePoint for the single-codepoint case
/// and otherwise from an interned cluster id. Setting it must therefore still round-trip, for
/// anything a hosted program can actually produce.
/// </summary>
[TestClass]
public class BufferCellContentTests
{
    [TestMethod]
    [DataRow("")]
    [DataRow("a")]
    [DataRow("\u00e9")]
    [DataRow("\u4e16")]
    [DataRow("\U0001F600")]
    [DataRow("e\u0301")]
    [DataRow("\U0001F468\u200D\U0001F469")]
    public void Content_round_trips(string text)
    {
        var cell = new BufferCell { Content = text };

        cell.Content.Should().Be(text);
    }

    /// <summary>
    /// A LONE surrogate is not a scalar value, so ConvertToUtf32 throws on one -- and this used to
    /// be a plain string field that stored whatever it was given. The text comes from a hosted
    /// program, which may emit any UTF-16 at all, so a cell must take it without throwing.
    /// </summary>
    /// <remarks>
    /// Built here rather than passed as InlineData on purpose. xUnit serialises theory data, and a
    /// lone surrogate does not survive the round trip -- it arrives as U+FFFD, so the test would
    /// pass while never once handling the input it names.
    /// </remarks>
    [TestMethod]
    public void An_unpaired_surrogate_is_stored_rather_than_thrown_on()
    {
        var cases = new[]
        {
            new string(new[] { '\uD83D' }),            // a high surrogate with nothing after it
            new string(new[] { '\uDE00' }),            // a low surrogate with nothing before it
            new string(new[] { '\uD83D', 'x' }),       // a high surrogate followed by something else
            new string(new[] { 'x', '\uDE00' }),
            new string(new[] { '\uDE00', '\uD83D' }), // a pair, the wrong way round
        };

        foreach (var text in cases)
        {
            var cell = new BufferCell();

            var thrown = Record.Exception(() => cell.Content = text);

            (thrown is null).Should().BeTrue($"threw on {Describe(text)}: {thrown?.GetType().Name}");
            ((text == cell.Content)).Should().BeTrue($"{Describe(text)} came back as {Describe(cell.Content)}");
        }
    }

    private static string Describe(string text)
        => "[" + string.Join(" ", text.Select(c => ((int)c).ToString("X4"))) + "]";

    /// <summary>
    /// And it records U+FFFD as the codepoint, which is what it renders as -- so width and the
    /// combining-character tests, which read CodePoint, see something meaningful rather than half a
    /// character.
    /// </summary>
    [TestMethod]
    public void An_unpaired_surrogate_reads_as_the_replacement_character()
    {
        var cell = new BufferCell { Content = "\uD83D" };

        cell.CodePoint.Should().Be(0xFFFD);
    }
}
