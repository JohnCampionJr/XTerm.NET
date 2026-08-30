using System.Text;
using XTerm.Parser;

namespace XTerm.Tests.Parser;

/// <summary>
/// DCS was entered and then thrown away: every byte between the introducer and the terminator was
/// discarded without being collected, and the Dcs event was marked obsolete because nothing ever
/// raised it. That made Sixel unreachable -- not unimplemented, but impossible to implement, since
/// the payload never reached anything that could decode it.
///
/// <para>These cover the state machine in front of the payload, which is the same grammar CSI uses,
/// and the payload delivery itself, which is streamed rather than handed over whole because a
/// full-screen image runs to hundreds of kilobytes.</para>
/// </summary>
[TestClass]
public class DcsSequenceTests
{
    private const string Esc = "\u001b";
    private const string St = Esc + "\\";

    /// <summary>Everything a DCS sequence produced, in order, as one transcript.</summary>
    private sealed class Recorder
    {
        public string? Identifier;
        public XTerm.Parser.Params? Parameters;
        public readonly StringBuilder Payload = new();
        public int Hooks;
        public int Unhooks;
        public bool? TerminatedCleanly;
        public string? WholePayload;

        public static Recorder Of(string input, bool subscribeWholePayload = false)
        {
            var parser = new EscapeSequenceParser();
            var recorder = new Recorder();

            parser.DcsHook += (_, e) =>
            {
                recorder.Hooks++;
                recorder.Identifier = e.Identifier;
                recorder.Parameters = e.Parameters;
            };
            parser.DcsPut += (_, e) => recorder.Payload.Append(e.Data.Span);
            parser.DcsUnhook += (_, e) =>
            {
                recorder.Unhooks++;
                recorder.TerminatedCleanly = e.TerminatedCleanly;
            };
            if (subscribeWholePayload)
                parser.Dcs += (_, e) => recorder.WholePayload = e.Data;

            parser.Parse(input);
            return recorder;
        }
    }

    [TestMethod]
    public void A_dcs_sequence_reports_its_final_character_as_the_identifier()
    {
        var recorded = Recorder.Of(Esc + "Pq" + St);

        recorded.Hooks.Should().Be(1);
        recorded.Identifier.Should().Be("q");
    }

    [TestMethod]
    public void Intermediates_precede_the_final_character_in_the_identifier()
    {
        // DECRQSS is "ESC P $ q", and reads as "$q" for the same reason a private CSI reads as "?h".
        var recorded = Recorder.Of(Esc + "P$qm" + St);

        recorded.Identifier.Should().Be("$q");
        recorded.Payload.ToString().Should().Be("m");
    }

    [TestMethod]
    public void XtGetTcap_is_told_apart_from_Sixel_by_its_intermediate()
    {
        // "ESC P + q" and "ESC P q" are one character apart and mean entirely different things: a
        // capability query and an image. The identifier is what keeps them apart, so it has to carry
        // the intermediate rather than just the final character.
        var query = Recorder.Of(Esc + "P+q544e" + St);
        var sixel = Recorder.Of(Esc + "Pq#0;2;100;0;0~" + St);

        query.Identifier.Should().Be("+q");
        query.Payload.ToString().Should().Be("544e");
        sixel.Identifier.Should().Be("q");
    }

    [TestMethod]
    public void Parameters_before_the_final_character_are_parsed()
    {
        var recorded = Recorder.Of(Esc + "P0;1;8q" + St);

        recorded.Parameters.Should().NotBeNull();
        (recorded.Parameters!.GetParam(0, -1)).Should().Be(0);
        recorded.Parameters.GetParam(1, -1).Should().Be(1);
        recorded.Parameters.GetParam(2, -1).Should().Be(8);
    }

    [TestMethod]
    public void An_omitted_parameter_defaults_to_zero()
    {
        var recorded = Recorder.Of(Esc + "P;1q" + St);

        (recorded.Parameters!.GetParam(0, -1)).Should().Be(0);
        recorded.Parameters.GetParam(1, -1).Should().Be(1);
    }

    [TestMethod]
    public void The_payload_is_delivered_between_hook_and_unhook()
    {
        var recorded = Recorder.Of(Esc + "Pqpayload here" + St);

        recorded.Hooks.Should().Be(1);
        recorded.Unhooks.Should().Be(1);
        recorded.Payload.ToString().Should().Be("payload here");
    }

    /// <summary>
    /// A payload larger than one internal chunk still arrives whole and in order. The parser
    /// batches puts rather than raising one per character, and that seam is where a long image
    /// would lose or reorder bytes if the flush were wrong.
    /// </summary>
    [TestMethod]
    public void A_payload_longer_than_the_chunk_buffer_arrives_intact()
    {
        var payload = string.Concat(Enumerable.Range(0, 5000).Select(i => (char)('a' + i % 26)));

        var recorded = Recorder.Of(Esc + "Pq" + payload + St);

        recorded.Payload.ToString().Should().Be(payload);
    }

    /// <summary>The payload does not care where the write boundaries fall.</summary>
    [TestMethod]
    public void A_payload_split_across_writes_arrives_intact()
    {
        var parser = new EscapeSequenceParser();
        var payload = new StringBuilder();
        parser.DcsPut += (_, e) => payload.Append(e.Data.Span);

        parser.Parse(Esc + "P0;1");
        parser.Parse("q#0;2;10");
        parser.Parse("0;0;0@@" + Esc);
        parser.Parse("\\");

        payload.ToString().Should().Be("#0;2;100;0;0@@");
    }

    [TestMethod]
    [DataRow("\u001b\\", "two-byte ST")]
    [DataRow("\u009c", "single-byte ST")]
    public void A_string_terminator_ends_the_sequence_cleanly(string terminator, string what)
    {
        var recorded = Recorder.Of(Esc + "Pqdata" + terminator);

        recorded.Unhooks.Should().Be(1);
        ((recorded.TerminatedCleanly == true)).Should().BeTrue($"{what} should end the sequence cleanly");
    }

    /// <summary>
    /// An abandoned sequence has to be distinguishable from a finished one. Half a picture is not
    /// worth drawing, and the only way a decoder can tell is if the parser says so.
    /// </summary>
    [TestMethod]
    [DataRow("\u0018", "CAN")]
    [DataRow("\u001a", "SUB")]
    [DataRow("\u001b[", "another escape sequence starting on top of it")]
    public void An_abandoned_sequence_is_reported_as_unclean(string interruption, string what)
    {
        var recorded = Recorder.Of(Esc + "Pqdata" + interruption);

        recorded.Unhooks.Should().Be(1);
        ((recorded.TerminatedCleanly == false)).Should().BeTrue($"{what} abandons the sequence; reporting it as clean would let a truncated image be shown");
    }

    [TestMethod]
    public void The_whole_payload_event_fires_for_short_sequences()
    {
        var recorded = Recorder.Of(Esc + "P$qm" + St, subscribeWholePayload: true);

        recorded.WholePayload.Should().Be("m");
    }

    /// <summary>
    /// The convenience event stops accumulating past its cap. A Sixel image is unbounded, and
    /// buffering one so it can be handed over as a single string is how a terminal ends up holding
    /// a copy of every picture ever drawn.
    /// </summary>
    [TestMethod]
    public void The_whole_payload_event_gives_up_on_oversized_sequences()
    {
        var huge = new string('~', EscapeSequenceParser.MaxAccumulatedDcsLength + 1);

        var recorded = Recorder.Of(Esc + "Pq" + huge + St, subscribeWholePayload: true);

        recorded.WholePayload.Should().BeNull();
        recorded.Payload.ToString().Should().Be(huge);
    }

    /// <summary>Nothing is accumulated at all when nobody subscribed to the whole-payload event.</summary>
    [TestMethod]
    public void A_sequence_with_no_whole_payload_listener_still_streams()
    {
        var recorded = Recorder.Of(Esc + "Pqdata" + St);

        recorded.WholePayload.Should().BeNull();
        recorded.Payload.ToString().Should().Be("data");
    }

    [TestMethod]
    public void A_reset_mid_payload_closes_the_sequence()
    {
        var parser = new EscapeSequenceParser();
        bool? cleanly = null;
        int unhooks = 0;
        parser.DcsUnhook += (_, e) => { unhooks++; cleanly = e.TerminatedCleanly; };

        parser.Parse(Esc + "Pqhalf an image");
        parser.Reset();

        unhooks.Should().Be(1);
        ((cleanly == false)).Should().BeTrue("a reset abandons whatever was arriving; a decoder left open would wait for a payload that never comes");
    }

    [TestMethod]
    public void Two_sequences_in_a_row_are_kept_apart()
    {
        var parser = new EscapeSequenceParser();
        var identifiers = new List<string>();
        var payloads = new List<string>();
        var current = new StringBuilder();

        parser.DcsHook += (_, e) => { identifiers.Add(e.Identifier); current.Clear(); };
        parser.DcsPut += (_, e) => current.Append(e.Data.Span);
        parser.DcsUnhook += (_, _) => payloads.Add(current.ToString());

        parser.Parse(Esc + "Pqfirst" + St + Esc + "P$qsecond" + St);

        identifiers.Should().Equal(new[] { "q", "$q" });
        payloads.Should().Equal(new[] { "first", "second" });
    }

    /// <summary>
    /// A DCS must not leave its intermediates behind for the next sequence to pick up.
    /// </summary>
    /// <remarks>
    /// The collect buffer is cleared when a CSI or DCS begins, and on the way out of a CSI -- but
    /// nothing clears it on the way out of a DCS, and an ESC sequence does not clear it on the way
    /// in. So a DECRQSS, whose intermediate is "$", left that "$" sitting in the buffer, and the
    /// next "ESC ( B" reported its intermediates as "$(" instead of "(" -- designating a character
    /// set the program never asked for, one sequence after the one that caused it.
    /// </remarks>
    [TestMethod]
    public void A_dcs_does_not_leave_its_intermediates_for_the_next_sequence()
    {
        var parser = new EscapeSequenceParser();
        string? finalChar = null;
        string? collected = null;
        parser.Esc += (_, e) => { finalChar = e.FinalChar; collected = e.Collected; };

        // DECRQSS collects "$", then an ordinary charset designation follows.
        parser.Parse(Esc + "P$qm" + St + Esc + "(B");

        finalChar.Should().Be("B");
        ((collected == "(")).Should().BeTrue($"the DCS left its intermediates behind: ESC ( B reported '{collected}' instead of '('");
    }

    /// <summary>The same for a CSI following a DCS, which shares the collect buffer.</summary>
    [TestMethod]
    public void A_csi_after_a_dcs_sees_only_its_own_intermediates()
    {
        var parser = new EscapeSequenceParser();
        string? identifier = null;
        parser.Csi += (_, e) => identifier = e.Identifier;

        parser.Parse(Esc + "P$qm" + St + Esc + "[?25h");

        identifier.Should().Be("?h");
    }

    /// <summary>The regression this whole area exists to guard: the parser has to come back.</summary>
    [TestMethod]
    public void Text_after_a_dcs_sequence_still_prints()
    {
        var parser = new EscapeSequenceParser();
        var printed = new StringBuilder();
        parser.Print += (_, e) => printed.Append(e.Data);

        parser.Parse(Esc + "Pq#0;2;100;0;0@" + St + "OK");

        printed.ToString().Should().Be("OK");
    }
}
