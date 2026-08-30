using XTerm;
using XTerm.Events;
using XTerm.Options;

namespace XTerm.Tests;

/// <summary>
/// Bracketed paste MIME (private mode 5522): the paste notification triple, the single-use
/// token, the precedence rule over mode 2004, and the fallback ladder — held to the spec at
/// rockorager.dev/misc/bracketed-paste-mime.
/// </summary>
[TestClass]
public class PasteNotificationTests
{
    private const string Esc = "\u001b";
    private const string St = "\u001b\\";

    private static Terminal NewTerminal() =>
        new(new TerminalOptions { Cols = 20, Rows = 5 });

    private static TerminalPaste RichPaste(bool fromPrimary = false) =>
        new(new[] { "text/html", "text/plain" },
            mime => mime switch
            {
                "text/html" => System.Text.Encoding.UTF8.GetBytes("<b>hi</b>"),
                "text/plain" => System.Text.Encoding.UTF8.GetBytes("hi"),
                _ => null
            },
            fromPrimary);

    private static string PwOf(string okPacket)
    {
        var start = okPacket.IndexOf(":pw=") + 4;
        var end = okPacket.IndexOf('\u001b', start);
        return okPacket[start..end];
    }

    // ---- the mode itself ------------------------------------------------------------------

    [TestMethod]
    public void The_mode_sets_resets_and_answers_DECRQM()
    {
        var t = NewTerminal();
        var responses = new List<string>();
        t.DataReceived += (_, e) => responses.Add(e.Data);

        t.Write($"{Esc}[?5522$p");
        (responses[^1]).Should().Be($"{Esc}[?5522;2$y");   // recognised, not set

        t.Write($"{Esc}[?5522h{Esc}[?5522$p");
        (responses[^1]).Should().Be($"{Esc}[?5522;1$y");

        t.Write($"{Esc}[?5522l{Esc}[?5522$p");
        (responses[^1]).Should().Be($"{Esc}[?5522;2$y");
    }

    [TestMethod]
    public void RIS_clears_the_mode()
    {
        var t = NewTerminal();
        t.Write($"{Esc}[?5522h{Esc}c");
        t.PasteNotificationMode.Should().BeFalse();
    }

    // ---- the precedence ladder ------------------------------------------------------------

    [TestMethod]
    public void With_neither_mode_paste_sends_the_raw_text()
    {
        var t = NewTerminal();
        var sent = new List<string>();
        t.DataReceived += (_, e) => sent.Add(e.Data);

        t.Paste("hello");
        sent.Should().Equal(new[] { "hello" });
    }

    [TestMethod]
    public void With_2004_alone_paste_is_bracketed()
    {
        var t = NewTerminal();
        var sent = new List<string>();
        t.DataReceived += (_, e) => sent.Add(e.Data);

        t.Write($"{Esc}[?2004h");
        t.Paste("hello");
        sent.Should().Equal(new[] { $"{Esc}[200~hello{Esc}[201~" });
    }

    /// <summary>
    /// The rule the library exists to enforce: with 5522 set the paste is ANNOUNCED, and the
    /// terminal must never send both sequence types for one paste — even with 2004 also set.
    /// </summary>
    [TestMethod]
    public void With_5522_set_paste_is_announced_and_never_bracketed()
    {
        var t = NewTerminal();
        var sent = new List<string>();
        t.DataReceived += (_, e) => sent.Add(e.Data);

        t.Write($"{Esc}[?2004h{Esc}[?5522h");
        t.Paste(RichPaste());

        sent.Count.Should().Be(3);
        sent.Should().NotContain(p => p.Contains("200~") || p.Contains("201~"));

        var pw = PwOf(sent[0]);
        pw.Should().NotBeEmpty();
        sent[0].Should().Be($"{Esc}]5522;type=read:status=OK:pw={pw}{St}");
        var mimes = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("text/html text/plain"));
        sent[1].Should().Be($"{Esc}]5522;type=read:status=DATA:mime=Lg==:pw={pw};{mimes}{St}");
        sent[2].Should().Be($"{Esc}]5522;type=read:status=DONE:pw={pw}{St}");
    }

    [TestMethod]
    public void A_primary_selection_paste_carries_loc_primary()
    {
        var t = NewTerminal();
        var sent = new List<string>();
        t.DataReceived += (_, e) => sent.Add(e.Data);

        t.Write($"{Esc}[?5522h");
        t.Paste(RichPaste(fromPrimary: true));
        sent[0].Should().StartWith($"{Esc}]5522;type=read:status=OK:loc=primary:pw=");
    }

    // ---- redemption -----------------------------------------------------------------------

    private static (Terminal t, List<string> sent, string pw) Announced(bool fromPrimary = false)
    {
        var t = NewTerminal();
        var sent = new List<string>();
        t.DataReceived += (_, e) => sent.Add(e.Data);
        t.Write($"{Esc}[?5522h");
        t.Paste(RichPaste(fromPrimary));
        var pw = PwOf(sent[0]);
        sent.Clear();
        return (t, sent, pw);
    }

    private static string Read(string pw, string mimes, string loc = "")
    {
        var payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(mimes));
        return $"{Esc}]5522;type=read{loc}:pw={pw}:name=UGFzdGUgZXZlbnQ=;{payload}{St}";
    }

    [TestMethod]
    public void A_token_read_serves_the_announced_content_without_the_host_seam()
    {
        // ClipboardReadEnabled stays FALSE: the notification was the authorization, and the
        // host clipboard event must not fire for a token read.
        var (t, sent, pw) = Announced();
        var hostAsked = false;
        t.ClipboardReadRequested += (_, _) => hostAsked = true;

        t.Write(Read(pw, "text/html"));

        hostAsked.Should().BeFalse();
        sent[0].Should().Be($"{Esc}]5522;type=read:status=OK{St}");
        var mime = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("text/html"));
        var data = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("<b>hi</b>"));
        sent[1].Should().Be($"{Esc}]5522;type=read:status=DATA:mime={mime};{data}{St}");
        sent[2].Should().Be($"{Esc}]5522;type=read:status=DONE{St}");
        sent.Count.Should().Be(3);
    }

    [TestMethod]
    public void The_token_is_single_use()
    {
        var (t, sent, pw) = Announced();
        t.Write(Read(pw, "text/plain"));
        sent.Count.Should().Be(3);
        sent.Clear();

        // Second redemption: token consumed; reads are disabled, so the fallback is EPERM.
        t.Write(Read(pw, "text/plain"));
        sent.Single().Should().Be($"{Esc}]5522;type=read:status=EPERM{St}");
    }

    [TestMethod]
    public void A_wrong_token_falls_back_to_standard_security()
    {
        var (t, sent, _) = Announced();
        t.Write(Read(Convert.ToBase64String(new byte[16]), "text/plain"));
        sent.Single().Should().Be($"{Esc}]5522;type=read:status=EPERM{St}");
    }

    [TestMethod]
    public void A_token_without_a_name_is_treated_as_passwordless_and_survives()
    {
        // The spec: a pw with no name is treated as though no password was given. Nothing is
        // consumed, the request falls to the standard gated path (silent EPERM here, since
        // reads are off), and a corrected retry WITH the name is served.
        var (t, sent, pw) = Announced();
        var payload = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("text/plain"));
        t.Write($"{Esc}]5522;type=read:pw={pw};{payload}{St}");
        sent.Single().Should().Be($"{Esc}]5522;type=read:status=EPERM{St}");
        sent.Clear();

        t.Write(Read(pw, "text/plain"));
        (sent[^1]).Should().Contain("status=DONE");
    }

    [TestMethod]
    public void The_token_is_scoped_to_the_location_that_produced_the_paste()
    {
        // Announced from primary; redeemed against the default clipboard — refused.
        var (t, sent, pw) = Announced(fromPrimary: true);
        t.Write(Read(pw, "text/plain"));
        sent.Single().Should().Be($"{Esc}]5522;type=read:status=EPERM{St}");
    }

    [TestMethod]
    public void Redemption_with_the_matching_location_succeeds()
    {
        var (t, sent, pw) = Announced(fromPrimary: true);
        t.Write(Read(pw, "text/plain", loc: ":loc=primary"));
        sent.Count.Should().Be(3);
        (sent[^1]).Should().Contain("status=DONE");
    }

    [TestMethod]
    public void Unavailable_requested_types_are_skipped_not_errors()
    {
        var (t, sent, pw) = Announced();
        t.Write(Read(pw, "image/png text/plain"));

        // OK, one DATA (text/plain), DONE — the png is skipped per the spec.
        sent.Count.Should().Be(3);
        var mime = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("text/plain"));
        sent[1].Should().Contain($"mime={mime}");
    }

    [TestMethod]
    public void A_new_paste_invalidates_the_previous_token()
    {
        var (t, sent, oldPw) = Announced();
        t.Paste(RichPaste());
        var newPw = PwOf(sent[0]);
        sent.Clear();

        t.Write(Read(oldPw, "text/plain"));
        sent.Single().Should().Be($"{Esc}]5522;type=read:status=EPERM{St}");
        sent.Clear();

        t.Write(Read(newPw, "text/plain"));
        (sent[^1]).Should().Contain("status=DONE");
    }

    [TestMethod]
    public void Resetting_the_mode_invalidates_the_token()
    {
        var (t, sent, pw) = Announced();
        t.Write($"{Esc}[?5522l");
        t.Write(Read(pw, "text/plain"));
        sent.Single().Should().Be($"{Esc}]5522;type=read:status=EPERM{St}");
    }

    [TestMethod]
    public void An_embedders_own_bracketing_keeps_working()
    {
        // Additive contract: BracketedPasteMode stays a public flag an embedder may read to
        // wrap its own pastes; nothing here changes who owns the brackets unless the host
        // adopts Terminal.Paste.
        var t = NewTerminal();
        t.Write($"{Esc}[?2004h");
        t.BracketedPasteMode.Should().BeTrue();
        t.Write($"{Esc}[?2004l");
        t.BracketedPasteMode.Should().BeFalse();
    }

    [TestMethod]
    public void The_wire_password_is_base64_of_UTF8_text_and_survives_a_decode_reencode()
    {
        // The spec defines pw as base64-encoded UTF-8: a conforming client decodes it, holds
        // text, and re-encodes to redeem. Both the literal echo and the round-tripped form must
        // redeem the same token.
        var (t, sent, pw) = Announced();
        var logical = System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(pw));
        logical.All(char.IsAscii).Should().BeTrue("the logical password must be ASCII text");
        var reencoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(logical));

        t.Write(Read(reencoded, "text/plain"));
        (sent[^1]).Should().Contain("status=DONE");
    }

    [TestMethod]
    public void A_valid_token_with_a_malformed_payload_is_consumed()
    {
        // Presentation consumes, before payload validation: a malformed redemption cannot be
        // corrected and replayed against a token that should be spent.
        var (t, sent, pw) = Announced();
        t.Write($"{Esc}]5522;type=read:pw={pw}:name=UGFzdGUgZXZlbnQ=;!!!notbase64{St}");
        sent.Single().Should().Be($"{Esc}]5522;type=read:status=EINVAL{St}");
        sent.Clear();

        t.Write(Read(pw, "text/plain"));
        sent.Single().Should().Be($"{Esc}]5522;type=read:status=EPERM{St}");
    }

    [TestMethod]
    public void Requesting_only_unavailable_types_answers_ENOSYS_not_an_empty_success()
    {
        var (t, sent, pw) = Announced();
        t.Write(Read(pw, "image/png application/pdf"));
        sent.Single().Should().Be($"{Esc}]5522;type=read:status=ENOSYS{St}");
    }

    [TestMethod]
    public void A_supplied_empty_value_still_sends_its_DATA_chunk()
    {
        // Empty is an ANSWER: distinguishable from a type that was never available.
        var t = NewTerminal();
        var sent = new List<string>();
        t.DataReceived += (_, e) => sent.Add(e.Data);
        t.Write($"{Esc}[?5522h");
        t.Paste(new TerminalPaste(new[] { "text/plain" }, _ => Array.Empty<byte>()));
        var pw = PwOf(sent[0]);
        sent.Clear();

        t.Write(Read(pw, "text/plain"));
        var mime = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("text/plain"));
        sent.Should().Equal(new[]
        {
            $"{Esc}]5522;type=read:status=OK{St}",
            $"{Esc}]5522;type=read:status=DATA:mime={mime};{St}",
            $"{Esc}]5522;type=read:status=DONE{St}",
        });
    }

    [TestMethod]
    public void A_token_read_skips_the_host_seam_even_with_reads_enabled()
    {
        // The claim that matters: the paste notification IS the authorization, so a valid token
        // serves the announced content directly even when the host seam is fully armed.
        var t = new Terminal(new TerminalOptions { Cols = 20, Rows = 5, ClipboardReadEnabled = true });
        var sent = new List<string>();
        t.DataReceived += (_, e) => sent.Add(e.Data);
        var hostAsked = false;
        t.ClipboardReadRequested += (_, e) => { hostAsked = true; e.Text = "host secret"; };

        t.Write($"{Esc}[?5522h");
        t.Paste(RichPaste());
        var pw = PwOf(sent[0]);
        sent.Clear();

        t.Write(Read(pw, "text/plain"));
        hostAsked.Should().BeFalse();
        var data = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("hi"));
        sent.Should().Contain(r => r.Contains(data));
    }

    [TestMethod]
    public void A_dot_request_through_the_token_lists_the_available_types()
    {
        var (t, sent, pw) = Announced();
        t.Write(Read(pw, "."));
        var list = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("text/html text/plain"));
        sent[1].Should().Be($"{Esc}]5522;type=read:status=DATA:mime=Lg==;{list}{St}");
    }

    [TestMethod]
    public void Large_content_is_chunked_at_4096_bytes_before_encoding()
    {
        var big = new string('x', 10_000);
        var t = NewTerminal();
        var sent = new List<string>();
        t.DataReceived += (_, e) => sent.Add(e.Data);
        t.Write($"{Esc}[?5522h");
        t.Paste(new TerminalPaste(new[] { "text/plain" },
            _ => System.Text.Encoding.UTF8.GetBytes(big)));
        var pw = PwOf(sent[0]);
        sent.Clear();

        t.Write(Read(pw, "text/plain"));
        // OK + three DATA packets (4096+4096+1808) + DONE.
        sent.Count.Should().Be(5);
        var payloads = sent.Skip(1).Take(3)
            .Select(r => r[(r.LastIndexOf(';') + 1)..].Replace(St, ""))
            .Select(b => System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(b)))
            .ToArray();
        string.Concat(payloads).Should().Be(big);
    }

    [TestMethod]
    public void An_expired_token_is_refused_and_a_fresh_one_is_not()
    {
        var (t, sent, pw) = Announced();
        var start = DateTime.UtcNow;
        t.PasteClock = () => start + TimeSpan.FromSeconds(61);
        t.Write(Read(pw, "text/plain"));
        sent.Single().Should().Be($"{Esc}]5522;type=read:status=EPERM{St}");
        sent.Clear();

        // A fresh announcement under the shifted clock redeems fine: the boundary is relative.
        t.Paste(RichPaste());
        var pw2 = PwOf(sent[0]);
        sent.Clear();
        t.PasteClock = () => start + TimeSpan.FromSeconds(130);   // issued at +61: now 69s old
        t.Write(Read(pw2, "text/plain"));
        sent.Single().Should().Be($"{Esc}]5522;type=read:status=EPERM{St}");
        sent.Clear();

        t.Paste(RichPaste());
        var pw3 = PwOf(sent[0]);
        sent.Clear();
        t.Write(Read(pw3, "text/plain"));
        (sent[^1]).Should().Contain("status=DONE");
    }

    [TestMethod]
    public void RIS_invalidates_an_outstanding_token_not_just_the_mode()
    {
        var (t, sent, pw) = Announced();
        t.Write($"{Esc}c{Esc}[?5522h");
        sent.Clear();
        t.Write(Read(pw, "text/plain"));
        sent.Single().Should().Be($"{Esc}]5522;type=read:status=EPERM{St}");
    }
}
