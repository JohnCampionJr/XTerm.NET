using XTerm;
using XTerm.Common;
using XTerm.Options;

namespace XTerm.Tests;

/// <summary>
/// OSC 22 — Kitty mouse pointer shapes.
/// </summary>
/// <remarks>
/// The emulator's job is the name and the stack; drawing a pointer is the host's. So these tests
/// look at what the terminal reports, what it tells a listener, and what it answers when asked.
/// </remarks>
[TestClass]
public class PointerShapeTests
{
    private const string Esc = "\u001b";
    private const string St = "\u001b\\";

    /// <summary>
    /// A terminal with the feature switched on, which is what a host that wires
    /// <see cref="Terminal.PointerShapeChanged"/> does. The off-by-default behaviour has its own
    /// tests below.
    /// </summary>
    private static Terminal Fresh() =>
        new(new TerminalOptions { Cols = 20, Rows = 5, PointerShapesEnabled = true });

    [TestMethod]
    public void Sets_a_shape()
    {
        var terminal = Fresh();
        terminal.PointerShape.Should().BeNull();

        terminal.Write($"{Esc}]22;pointer{St}");

        terminal.PointerShape.Should().Be("pointer");
    }

    /// <summary>
    /// The '=' form is the same set operation, spelled explicitly.
    /// </summary>
    [TestMethod]
    public void Sets_a_shape_with_the_explicit_operator()
    {
        var terminal = Fresh();

        terminal.Write($"{Esc}]22;=text{St}");

        terminal.PointerShape.Should().Be("text");
    }

    /// <summary>
    /// A bare OSC 22 is how an application says "use your own pointer again" without having to know
    /// what the terminal's own pointer is.
    /// </summary>
    [TestMethod]
    public void An_empty_shape_clears()
    {
        var terminal = Fresh();
        terminal.Write($"{Esc}]22;wait{St}");

        terminal.Write($"{Esc}]22;{St}");

        terminal.PointerShape.Should().BeNull();
    }

    /// <summary>
    /// Names outside the table mean nothing to a host, so they are refused rather than passed on.
    /// </summary>
    [TestMethod]
    public void An_unknown_name_leaves_the_shape_alone()
    {
        var terminal = Fresh();
        terminal.Write($"{Esc}]22;pointer{St}");

        terminal.Write($"{Esc}]22;no-such-shape{St}");

        terminal.PointerShape.Should().Be("pointer");
    }

    [TestMethod]
    public void Pushes_and_pops()
    {
        var terminal = Fresh();

        terminal.Write($"{Esc}]22;text{St}");
        terminal.Write($"{Esc}]22;>wait{St}");
        terminal.PointerShape.Should().Be("wait");

        terminal.Write($"{Esc}]22;<{St}");
        terminal.PointerShape.Should().Be("text");
    }

    /// <summary>
    /// A push of several names leaves the last one current, and popping walks back through them.
    /// </summary>
    [TestMethod]
    public void Pushes_a_list_with_the_last_name_current()
    {
        var terminal = Fresh();

        terminal.Write($"{Esc}]22;>pointer,wait{St}");
        terminal.PointerShape.Should().Be("wait");

        terminal.Write($"{Esc}]22;<{St}");
        terminal.PointerShape.Should().Be("pointer");
    }

    /// <summary>
    /// Only the last name of a push is ever meant to be seen, so a host is told once and swaps the
    /// real pointer once, rather than flickering through the names on the way there.
    /// </summary>
    [TestMethod]
    public void Pushing_a_list_raises_once()
    {
        var terminal = Fresh();
        var shapes = new List<string?>();
        terminal.PointerShapeChanged += (_, e) => shapes.Add(e.Shape);

        terminal.Write($"{Esc}]22;>pointer,text,wait{St}");

        shapes.Should().Equal(new[] { "wait" });
    }

    /// <summary>
    /// The comma-separated list is a push-only form; a set takes one name, so a list is simply not a
    /// shape this terminal knows.
    /// </summary>
    [TestMethod]
    public void Setting_takes_a_single_name_not_a_list()
    {
        var terminal = Fresh();
        terminal.Write($"{Esc}]22;pointer{St}");

        terminal.Write($"{Esc}]22;text,wait{St}");

        terminal.PointerShape.Should().Be("pointer");
    }

    /// <summary>
    /// The list after a pop is defined to be ignored, and unwinding past the bottom is a no-op — an
    /// application walking back out does not have to have counted its pushes.
    /// </summary>
    [TestMethod]
    public void Popping_an_empty_stack_does_nothing()
    {
        var terminal = Fresh();

        terminal.Write($"{Esc}]22;<{St}");
        terminal.Write($"{Esc}]22;<wait{St}");

        terminal.PointerShape.Should().BeNull();
    }

    /// <summary>
    /// A set replaces the top of the stack rather than growing it, so what was pushed underneath is
    /// still what a pop returns to.
    /// </summary>
    [TestMethod]
    public void Setting_replaces_the_top_of_the_stack()
    {
        var terminal = Fresh();

        terminal.Write($"{Esc}]22;text{St}");
        terminal.Write($"{Esc}]22;>wait{St}");
        terminal.Write($"{Esc}]22;progress{St}");
        terminal.PointerShape.Should().Be("progress");

        terminal.Write($"{Esc}]22;<{St}");
        terminal.PointerShape.Should().Be("text");
    }

    /// <summary>
    /// A program that pushes and never pops must not grow the stack without bound. The oldest entry
    /// goes, since nobody is going to pop back that far, and the newest shape still takes effect.
    /// </summary>
    [TestMethod]
    public void The_stack_is_bounded()
    {
        var terminal = Fresh();

        for (var i = 0; i < PointerShapeStack.MaxDepth + 4; i++)
            terminal.Write($"{Esc}]22;>wait{St}");
        terminal.Write($"{Esc}]22;>pointer{St}");

        terminal.PointerShape.Should().Be("pointer");

        // One pop per surviving entry empties it, however many pushes arrived.
        for (var i = 0; i < PointerShapeStack.MaxDepth; i++)
            terminal.Write($"{Esc}]22;<{St}");

        terminal.PointerShape.Should().BeNull();
    }

    /// <summary>
    /// A host has to swap the real pointer when this changes, and cannot poll for it.
    /// </summary>
    [TestMethod]
    public void Raises_an_event_on_each_change()
    {
        var terminal = Fresh();
        var shapes = new List<string?>();
        terminal.PointerShapeChanged += (_, e) => shapes.Add(e.Shape);

        terminal.Write($"{Esc}]22;pointer{St}");
        terminal.Write($"{Esc}]22;>wait{St}");
        terminal.Write($"{Esc}]22;<{St}");
        terminal.Write($"{Esc}]22;{St}");

        shapes.Should().Equal(new[] { "pointer", "wait", "pointer", null });
    }

    /// <summary>
    /// Programs re-send the same shape as the mouse moves over the same region; a host should only
    /// hear about real changes.
    /// </summary>
    [TestMethod]
    public void Repeating_the_same_shape_raises_nothing()
    {
        var terminal = Fresh();
        var changes = 0;
        terminal.PointerShapeChanged += (_, _) => changes++;

        terminal.Write($"{Esc}]22;text{St}");
        terminal.Write($"{Esc}]22;text{St}");
        terminal.Write($"{Esc}]22;text{St}");

        changes.Should().Be(1);
    }

    /// <summary>
    /// Each screen keeps its own stack, so a full-screen program suspending back to the shell does
    /// not leave its pointer over the shell — and finds its own again when it returns.
    /// </summary>
    [TestMethod]
    public void Each_screen_keeps_its_own_stack()
    {
        var terminal = Fresh();
        terminal.Write($"{Esc}]22;text{St}");

        terminal.Write($"{Esc}[?1049h");
        terminal.PointerShape.Should().BeNull();

        terminal.Write($"{Esc}]22;wait{St}");
        terminal.PointerShape.Should().Be("wait");

        terminal.Write($"{Esc}[?1049l");
        terminal.PointerShape.Should().Be("text");

        terminal.Write($"{Esc}[?1049h");
        terminal.PointerShape.Should().Be("wait");
    }

    /// <summary>
    /// Switching screens changes the current shape without any application asking, so the host has
    /// to be told the same way it is told about a set.
    /// </summary>
    [TestMethod]
    public void Switching_screens_raises_the_change()
    {
        var terminal = Fresh();
        terminal.Write($"{Esc}]22;text{St}");

        var shapes = new List<string?>();
        terminal.PointerShapeChanged += (_, e) => shapes.Add(e.Shape);

        terminal.Write($"{Esc}[?1049h");
        terminal.Write($"{Esc}[?1049l");

        shapes.Should().Equal(new string?[] { null, "text" });
    }

    /// <summary>
    /// RIS is the way out of a `wait` pointer left behind by a program that died holding one, and it
    /// has to clear the screen the program was not on as well.
    /// </summary>
    [TestMethod]
    public void Reset_empties_both_stacks()
    {
        var terminal = Fresh();
        terminal.Write($"{Esc}]22;text{St}");
        terminal.Write($"{Esc}[?1049h");
        terminal.Write($"{Esc}]22;wait{St}");

        var cleared = 0;
        terminal.PointerShapeChanged += (_, e) => { if (e.IsCleared) cleared++; };

        terminal.Write($"{Esc}c");

        terminal.PointerShape.Should().BeNull();
        cleared.Should().Be(1);

        terminal.Write($"{Esc}[?1049h");
        terminal.PointerShape.Should().BeNull();
    }

    /// <summary>
    /// The query is how a program finds out the feature exists at all before relying on it.
    /// </summary>
    [TestMethod]
    public void Answers_a_support_query()
    {
        var terminal = Fresh();
        var replies = new List<string>();
        terminal.DataReceived += (_, e) => replies.Add(e.Data);

        terminal.Write($"{Esc}]22;?pointer,crosshair,no-such-name,wait{St}");

        replies.Should().Equal(new[] { $"{Esc}]22;1,1,0,1{St}" });
    }

    [TestMethod]
    public void Answers_the_current_shape_query()
    {
        var terminal = Fresh();
        var replies = new List<string>();
        terminal.DataReceived += (_, e) => replies.Add(e.Data);

        terminal.Write($"{Esc}]22;?__current__{St}");
        terminal.Write($"{Esc}]22;grabbing{St}");
        terminal.Write($"{Esc}]22;?__current__{St}");

        // "0" while nothing is set: the stack is empty, not holding some default.
        replies.Should().Equal(new[] { $"{Esc}]22;0{St}", $"{Esc}]22;grabbing{St}" });
    }

    [TestMethod]
    public void Answers_the_special_shape_names()
    {
        var terminal = Fresh();
        var replies = new List<string>();
        terminal.DataReceived += (_, e) => replies.Add(e.Data);

        terminal.Write($"{Esc}]22;?__default__,__grabbed__{St}");

        replies.Should().Equal(new[] { $"{Esc}]22;default,grabbing{St}" });
    }

    /// <summary>
    /// A query must never make the terminal write the application's own bytes back to it — an
    /// unsupported name is answered with a 0, not echoed.
    /// </summary>
    [TestMethod]
    public void A_query_never_echoes_the_name_back()
    {
        var terminal = Fresh();
        var replies = new List<string>();
        terminal.DataReceived += (_, e) => replies.Add(e.Data);

        terminal.Write($"{Esc}]22;?]22;wait{St}");

        replies.Should().Equal(new[] { $"{Esc}]22;0{St}" });
    }

    /// <summary>
    /// Every name in the published table has to be accepted, or a program has no way to tell which
    /// half of the protocol it got.
    /// </summary>
    [TestMethod]
    public void Accepts_every_published_shape_name()
    {
        foreach (var name in PointerShapes.All)
        {
            var terminal = Fresh();
            terminal.Write($"{Esc}]22;{name}{St}");
            terminal.PointerShape.Should().Be(name);
        }
    }

    /// <summary>
    /// Off unless a host asks for it. Only a host can change a real pointer, so a stock terminal
    /// nobody has wired up must not claim it can.
    /// </summary>
    [TestMethod]
    public void Disabled_by_default()
    {
        (new TerminalOptions().PointerShapesEnabled).Should().BeFalse();
    }

    /// <summary>
    /// A host that cannot change the pointer leaves the feature off, and the terminal then keeps no
    /// state and tells nobody anything.
    /// </summary>
    [TestMethod]
    public void Disabled_ignores_the_sequence()
    {
        var terminal = new Terminal(new TerminalOptions { Cols = 20, Rows = 5 });
        var changes = 0;
        terminal.PointerShapeChanged += (_, _) => changes++;

        terminal.Write($"{Esc}]22;pointer{St}");
        terminal.Write($"{Esc}]22;>wait{St}");

        terminal.PointerShape.Should().BeNull();
        changes.Should().Be(0);
    }

    /// <summary>
    /// The query has to go silent too. Answering "supported" while the pointer never changes is
    /// worse than not answering: the application cannot tell those apart from its end.
    /// </summary>
    [TestMethod]
    public void Disabled_answers_no_query()
    {
        var terminal = new Terminal(new TerminalOptions { Cols = 20, Rows = 5 });
        var replies = new List<string>();
        terminal.DataReceived += (_, e) => replies.Add(e.Data);

        terminal.Write($"{Esc}]22;?pointer,wait{St}");
        terminal.Write($"{Esc}]22;?__current__{St}");

        replies.Should().BeEmpty();
    }
}
