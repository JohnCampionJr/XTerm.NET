using XTerm.Input;
using XTerm.Options;

namespace XTerm.Tests;

/// <summary>
/// The terminal side of the Kitty keyboard protocol: the four CSI u sequences, the per-screen
/// flag stacks, and the query an application actually probes with.
/// </summary>
[TestClass]
public class KittyKeyboardStateTests
{
    private static Terminal NewTerminal(bool enabled = true)
        => new(new TerminalOptions { Cols = 20, Rows = 5, KittyKeyboardEnabled = enabled });

    private static KittyKeyboardFlags Flags(Terminal t) => t.KittyKeyboardState.Flags;

    // ----- CSI = flags ; mode u ------------------------------------------------------------

    [TestMethod]
    public void Set_assigns_the_flags()
    {
        var t = NewTerminal();
        t.Write("\u001b[=5u");
        Flags(t).Should().Be(KittyKeyboardFlags.DisambiguateEscapeCodes | KittyKeyboardFlags.ReportAlternateKeys);
    }

    [TestMethod]
    public void Set_mode_two_only_sets_the_given_bits()
    {
        var t = NewTerminal();
        t.Write("\u001b[=1u");
        t.Write("\u001b[=2;2u");
        Flags(t).Should().Be(KittyKeyboardFlags.DisambiguateEscapeCodes | KittyKeyboardFlags.ReportEventTypes);
    }

    [TestMethod]
    public void Set_mode_three_only_clears_the_given_bits()
    {
        var t = NewTerminal();
        t.Write("\u001b[=3u");
        t.Write("\u001b[=1;3u");
        Flags(t).Should().Be(KittyKeyboardFlags.ReportEventTypes);
    }

    [TestMethod]
    public void Set_with_no_parameters_clears_the_flags()
    {
        var t = NewTerminal();
        t.Write("\u001b[=31u");
        t.Write("\u001b[=u");
        Flags(t).Should().Be(KittyKeyboardFlags.None);
    }

    // ----- CSI ? u -------------------------------------------------------------------------

    [TestMethod]
    public void Query_answers_with_the_active_flags()
    {
        var t = NewTerminal();
        var responses = new List<string>();
        t.DataReceived += (_, e) => responses.Add(e.Data);

        t.Write("\u001b[?u");
        t.Write("\u001b[=5u");
        t.Write("\u001b[?u");

        responses.Should().Equal(new[] { "\u001b[?0u", "\u001b[?5u" });
    }

    [TestMethod]
    public void Query_does_not_move_the_cursor()
    {
        // The regression that motivated the exact-match lookup: the identifier "?u" used to be
        // stripped to "u" and executed RESTORE CURSOR, so Neovim's startup probe for Kitty
        // support teleported the cursor to wherever CSI s last saved it.
        var t = NewTerminal();
        t.Write("\u001b[2;2H\u001b[s");   // save at (1,1)...
        t.Write("\u001b[4;6H");            // ...move away
        t.Write("\u001b[?u");

        t.Buffer.X.Should().Be(5);
        t.Buffer.Y.Should().Be(3);
    }

    [TestMethod]
    public void Bare_CSI_u_still_restores_the_cursor()
    {
        var t = NewTerminal();
        t.Write("\u001b[2;2H\u001b[s");
        t.Write("\u001b[4;6H");
        t.Write("\u001b[u");

        t.Buffer.X.Should().Be(1);
        t.Buffer.Y.Should().Be(1);
    }

    // ----- CSI > flags u / CSI < count u ---------------------------------------------------

    [TestMethod]
    public void Push_saves_the_current_flags_and_pop_restores_them()
    {
        var t = NewTerminal();
        t.Write("\u001b[=1u");
        t.Write("\u001b[>2u");
        t.Write("\u001b[>15u");
        Flags(t).Should().Be((KittyKeyboardFlags)15);

        t.Write("\u001b[<u");
        Flags(t).Should().Be(KittyKeyboardFlags.ReportEventTypes);

        // The base state set with CSI = IS on the stack — kitty's
        // screen_set_key_encoding_flags creates a stack entry when it is empty, and the active
        // flags are just the top of the stack. So the second pop restores what CSI = 1 u
        // established; it does not zero it. (The spec's "a pop that empties the stack resets all
        // flags" describes popping PAST that base entry, which the over-pop test below covers.)
        t.Write("\u001b[<u");
        Flags(t).Should().Be(KittyKeyboardFlags.DisambiguateEscapeCodes);

        t.Write("\u001b[<u");
        Flags(t).Should().Be(KittyKeyboardFlags.None);
    }

    [TestMethod]
    public void A_matched_push_pop_pair_preserves_the_base_flags()
    {
        // The ordinary lifecycle, and the case the stack exists to make safe: the shell sets its
        // base flags, a well-behaved application pushes on entry and pops on exit. The shell must
        // get ITS flags back — not legacy encoding.
        var t = NewTerminal();
        t.Write("\u001b[=1u");   // the shell's flags
        t.Write("\u001b[>8u");   // an application pushes
        t.Write("\u001b[<u");    // and pops on exit
        Flags(t).Should().Be(KittyKeyboardFlags.DisambiguateEscapeCodes);
    }

    [TestMethod]
    public void Pop_takes_a_count()
    {
        var t = NewTerminal();
        t.Write("\u001b[=1u");
        t.Write("\u001b[>2u");
        t.Write("\u001b[>4u");
        t.Write("\u001b[>8u");
        t.Write("\u001b[<2u");
        Flags(t).Should().Be((KittyKeyboardFlags)2);
    }

    [TestMethod]
    public void Popping_past_the_bottom_leaves_the_flags_at_zero()
    {
        var t = NewTerminal();
        t.Write("\u001b[=1u");
        t.Write("\u001b[<5u");
        Flags(t).Should().Be(KittyKeyboardFlags.None);
    }

    [TestMethod]
    public void The_stack_is_bounded_by_evicting_the_oldest_entry()
    {
        // An application looping on push must not grow memory forever: spec says a full stack
        // evicts its OLDEST entry. Twenty pushes therefore leave sixteen entries, not twenty —
        // fifteen pops still find values, and the sixteenth empties the stack (which resets the
        // flags to zero, per the pop rule above).
        var t = NewTerminal();
        t.Write("\u001b[=1u");
        for (var i = 0; i < 20; i++)
            t.Write("\u001b[>8u");

        for (var i = 0; i < 15; i++)
            t.Write("\u001b[<u");
        Flags(t).Should().Be((KittyKeyboardFlags)8);

        // The base entry CSI = 1 u created was itself evicted by the churn, so draining the
        // stack really is protocol-off here.
        t.Write("\u001b[<u");
        Flags(t).Should().Be(KittyKeyboardFlags.None);
    }

    // ----- Flag masking --------------------------------------------------------------------

    [TestMethod]
    public void Out_of_range_flags_round_trip_through_the_query_masked()
    {
        // kitty masks to the five defined bits on both set and push (val & 0x7f, less its
        // stack-occupied marker). CSI = 255 u must answer the query with 31 — echoing back 255
        // hands the application a value it never sent and cannot interpret as flags.
        var t = NewTerminal();
        var responses = new List<string>();
        t.DataReceived += (_, e) => responses.Add(e.Data);

        t.Write("\u001b[=255u");
        t.Write("\u001b[?u");

        responses.Should().Equal(new[] { "\u001b[?31u" });
    }

    [TestMethod]
    public void A_push_of_only_undefined_bits_does_not_activate_the_protocol()
    {
        // Without masking, CSI > 32 u leaves KittyKeyboardActive true with no bit the encoder
        // understands — the exact state where an unhandled chord would otherwise send nothing.
        var t = NewTerminal();
        t.Write("\u001b[>32u");
        Flags(t).Should().Be(KittyKeyboardFlags.None);
        t.KittyKeyboardActive.Should().BeFalse();
    }

    [TestMethod]
    public void An_explicit_mode_of_zero_does_nothing()
    {
        // An OMITTED mode defaults to 1, but kitty's switch takes no branch for an explicit 0 —
        // it is an unknown mode, not an alias for assign.
        var t = NewTerminal();
        t.Write("\u001b[=5u");
        t.Write("\u001b[=1;0u");
        Flags(t).Should().Be(KittyKeyboardFlags.DisambiguateEscapeCodes | KittyKeyboardFlags.ReportAlternateKeys);
    }

    // ----- Per-screen flags ----------------------------------------------------------------

    [TestMethod]
    public void The_alternate_screen_has_its_own_flags()
    {
        var t = NewTerminal();
        t.Write("\u001b[=1u");           // the shell's flags, on the main screen
        t.Write("\u001b[?1049h");        // a full-screen app starts...
        Flags(t).Should().Be(KittyKeyboardFlags.None);

        t.Write("\u001b[=8u");           // ...and sets its own
        t.Write("\u001b[?1049l");        // and exits
        Flags(t).Should().Be(KittyKeyboardFlags.DisambiguateEscapeCodes);

        // Its flags are still waiting if it comes back.
        t.Write("\u001b[?1049h");
        Flags(t).Should().Be(KittyKeyboardFlags.ReportAllKeysAsEscapeCodes);
    }

    [TestMethod]
    public void An_application_dying_without_popping_cannot_poison_the_shell()
    {
        // The scenario the per-screen rule exists for: vim pushes flags on the alternate screen
        // and crashes. Leaving the alternate screen must hand the shell ITS flags back.
        var t = NewTerminal();
        t.Write("\u001b[?1049h");
        t.Write("\u001b[>31u");          // vim pushes everything on
        t.Write("\u001b[?1049l");        // crash: no pop
        Flags(t).Should().Be(KittyKeyboardFlags.None);
    }

    [TestMethod]
    public void Each_screen_pops_from_its_own_stack()
    {
        var t = NewTerminal();
        t.Write("\u001b[=1u");
        t.Write("\u001b[>2u");           // main stack: [1, 2]
        t.Write("\u001b[>4u");           // main stack: [1, 2, 4], flags 4
        t.Write("\u001b[?1049h");
        t.Write("\u001b[<u");            // alt stack is empty: flags stay 0
        Flags(t).Should().Be(KittyKeyboardFlags.None);

        t.Write("\u001b[?1049l");
        Flags(t).Should().Be((KittyKeyboardFlags)4);
        t.Write("\u001b[<u");            // the alt-screen pop consumed NOTHING of main's stack
        Flags(t).Should().Be(KittyKeyboardFlags.ReportEventTypes);
    }

    // ----- Reset ---------------------------------------------------------------------------

    [TestMethod]
    public void RIS_clears_the_flags_and_both_stacks()
    {
        // RIS is exactly how someone recovers from an application that set flags and died.
        var t = NewTerminal();
        t.Write("\u001b[=31u");
        t.Write("\u001b[>31u");
        t.Write("\u001bc");
        Flags(t).Should().Be(KittyKeyboardFlags.None);

        t.Write("\u001b[<u");            // the stack is gone too, not just the flags
        Flags(t).Should().Be(KittyKeyboardFlags.None);
    }

    // ----- The option gate -----------------------------------------------------------------

    [TestMethod]
    public void When_disabled_the_sequences_are_consumed_in_silence()
    {
        var t = NewTerminal(enabled: false);
        var responses = new List<string>();
        t.DataReceived += (_, e) => responses.Add(e.Data);

        t.Write("\u001b[=31u");
        t.Write("\u001b[>31u");
        t.Write("\u001b[?u");            // no answer is how a terminal says "legacy encoding"

        Flags(t).Should().Be(KittyKeyboardFlags.None);
        responses.Should().BeEmpty();
        t.KittyKeyboardActive.Should().BeFalse();
    }

    [TestMethod]
    public void When_disabled_the_query_still_does_not_move_the_cursor()
    {
        var t = NewTerminal(enabled: false);
        t.Write("\u001b[2;2H\u001b[s");
        t.Write("\u001b[4;6H");
        t.Write("\u001b[?u");

        t.Buffer.X.Should().Be(5);
        t.Buffer.Y.Should().Be(3);
    }

    // ----- The terminal-level API the host uses --------------------------------------------

    [TestMethod]
    public void KittyKeyboardActive_follows_the_flags()
    {
        var t = NewTerminal();
        t.KittyKeyboardActive.Should().BeFalse();
        t.Write("\u001b[=1u");
        t.KittyKeyboardActive.Should().BeTrue();
        t.Write("\u001b[=u");
        t.KittyKeyboardActive.Should().BeFalse();
    }

    [TestMethod]
    public void GenerateKittyKeyInput_encodes_under_the_active_flags()
    {
        var t = NewTerminal();
        t.Write("\u001b[=1u");
        var ev = new KeyEvent { Key = "Escape" };
        t.GenerateKittyKeyInput(ev).Should().Be("\u001b[27u");
    }

    [TestMethod]
    public void GenerateKittyKeyInput_honours_MacOptionIsMeta()
    {
        var t = new Terminal(new TerminalOptions { Cols = 20, Rows = 5, MacOptionIsMeta = true });
        t.Write("\u001b[=1u");
        var ev = new KeyEvent { Key = "ƒ", Code = "KeyF", AltKey = true };
        t.GenerateKittyKeyInput(ev).Should().Be("\u001b[102;3u");
    }

    [TestMethod]
    [DataRow("Escape", "", "\u001b")]
    [DataRow("F13", "", "\u001b[25~")]
    [DataRow("F20", "", "\u001b[34~")]
    [DataRow("Enter", "NumpadEnter", "\r")]
    public void GenerateKittyKeyInput_uses_legacy_functional_bytes_when_flags_do_not_request_CSI_u(
        string key, string code, string expected)
    {
        var t = NewTerminal();
        t.Write("\u001b[=4u");

        t.GenerateKittyKeyInput(new KeyEvent { Key = key, Code = code }).Should().Be(expected);
    }

    [TestMethod]
    public void NumpadEnter_legacy_fallback_honours_application_keypad_mode()
    {
        var t = NewTerminal();
        t.Write("\u001b[=16u");
        t.Write("\u001b=");

        t.GenerateKittyKeyInput(new KeyEvent { Key = "Enter", Code = "NumpadEnter" }).Should().Be("\u001bOM");
    }

    [TestMethod]
    public void Legacy_fallback_does_not_run_without_active_Kitty_flags()
        => NewTerminal().GenerateKittyKeyInput(new KeyEvent { Key = "Escape" }).Should().BeNull();

    [TestMethod]
    public void Legacy_fallback_does_not_turn_a_release_into_another_press()
    {
        var t = NewTerminal();
        t.Write("\u001b[=4u");

        t.GenerateKittyKeyInput(
            new KeyEvent { Key = "Escape" }, KittyKeyboardEventType.Release).Should().BeNull();
    }
}
