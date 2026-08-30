using XTerm;
using XTerm.Options;
using XTerm.Parser;
using XTerm.Common;

namespace XTerm.Tests;

[TestClass]

public class WindowManipulationTests
{
    private Terminal CreateTerminal(WindowOptions? windowOptions = null)
    {
        var options = new TerminalOptions
        {
            Cols = 80,
            Rows = 24,
            WindowOptions = windowOptions ?? new WindowOptions()
        };
        return new Terminal(options);
    }

    [TestMethod]
    public void Terminal_InitializesWindowEvents()
    {
        // Arrange & Act
        var terminal = CreateTerminal();
        var moveEventFired = false;
        var resizeEventFired = false;
        var minimizeEventFired = false;

        // Assert - Verify events can be subscribed to without error
        terminal.WindowMoved += (sender, e) => moveEventFired = true;
        terminal.WindowResized += (sender, e) => resizeEventFired = true;
        terminal.WindowMinimized += (sender, e) => minimizeEventFired = true;
        terminal.WindowMaximized += (sender, e) => { };
        terminal.WindowRestored += (sender, e) => { };
        terminal.WindowRaised += (sender, e) => { };
        terminal.WindowLowered += (sender, e) => { };
        terminal.WindowRefreshed += (sender, e) => { };
        terminal.WindowFullscreened += (sender, e) => { };
        terminal.WindowInfoRequested += (sender, e) => { };

        // Verify terminal is properly initialized
        terminal.Should().NotBeNull();
        terminal.Options.Should().NotBeNull();
        terminal.Options.WindowOptions.Should().NotBeNull();
        
        // Events should not have fired yet
        moveEventFired.Should().BeFalse();
        resizeEventFired.Should().BeFalse();
        minimizeEventFired.Should().BeFalse();
    }

    [TestMethod]
    public void WindowManipulation_MoveWindow_FiresOnWindowMove()
    {
        // Arrange
        var windowOptions = new WindowOptions { SetWinPosition = true };
        var terminal = CreateTerminal(windowOptions);
        var moveEventFired = false;
        int capturedX = 0, capturedY = 0;

        terminal.WindowMoved += (sender, e) =>
        {
            moveEventFired = true;
            capturedX = e.X;
            capturedY = e.Y;
        };

        // Act
        terminal.Write("\x1b[3;100;200t"); // CSI 3 ; 100 ; 200 t

        // Assert
        moveEventFired.Should().BeTrue();
        capturedX.Should().Be(100);
        capturedY.Should().Be(200);
    }

    [TestMethod]
    public void WindowManipulation_MoveWindow_DoesNotFireWhenPermissionDenied()
    {
        // Arrange
        var windowOptions = new WindowOptions { SetWinPosition = false };
        var terminal = CreateTerminal(windowOptions);
        var moveEventFired = false;

        terminal.WindowMoved += (sender, e) => moveEventFired = true;

        // Act
        terminal.Write("\x1b[3;100;200t");

        // Assert
        moveEventFired.Should().BeFalse();
    }

    [TestMethod]
    public void WindowManipulation_ResizeWindow_FiresOnWindowResize()
    {
        // Arrange
        var windowOptions = new WindowOptions { SetWinSizePixels = true };
        var terminal = CreateTerminal(windowOptions);
        var resizeEventFired = false;
        int capturedWidth = 0, capturedHeight = 0;

        terminal.WindowResized += (sender, e) =>
        {
            resizeEventFired = true;
            capturedWidth = e.Width;
            capturedHeight = e.Height;
        };

        // Act
        terminal.Write("\x1b[4;600;800t"); // CSI 4 ; 600 ; 800 t

        // Assert
        resizeEventFired.Should().BeTrue();
        capturedWidth.Should().Be(800);
        capturedHeight.Should().Be(600);
    }

    [TestMethod]
    public void WindowManipulation_MinimizeWindow_FiresOnWindowMinimize()
    {
        // Arrange
        var windowOptions = new WindowOptions { MinimizeWin = true };
        var terminal = CreateTerminal(windowOptions);
        var eventFired = false;

        terminal.WindowMinimized += (sender, e) => eventFired = true;

        // Act
        terminal.Write("\x1b[2t"); // CSI 2 t

        // Assert
        eventFired.Should().BeTrue();
    }

    [TestMethod]
    public void WindowManipulation_MaximizeWindow_FiresOnWindowMaximize()
    {
        // Arrange
        var windowOptions = new WindowOptions { MaximizeWin = true };
        var terminal = CreateTerminal(windowOptions);
        var eventFired = false;

        terminal.WindowMaximized += (sender, e) => eventFired = true;

        // Act
        terminal.Write("\x1b[9;1t"); // CSI 9 ; 1 t

        // Assert
        eventFired.Should().BeTrue();
    }

    [TestMethod]
    public void WindowManipulation_RestoreWindow_FiresOnWindowRestore()
    {
        // Arrange
        var windowOptions = new WindowOptions { RestoreWin = true };
        var terminal = CreateTerminal(windowOptions);
        var eventFired = false;

        terminal.WindowRestored += (sender, e) => eventFired = true;

        // Act - Test both operation 1 (de-iconify) and operation 9;0 (restore from maximize)
        terminal.Write("\x1b[1t"); // CSI 1 t

        // Assert
        eventFired.Should().BeTrue();
    }

    [TestMethod]
    public void WindowManipulation_RestoreFromMaximize_FiresOnWindowRestore()
    {
        // Arrange
        var windowOptions = new WindowOptions { RestoreWin = true };
        var terminal = CreateTerminal(windowOptions);
        var eventFired = false;

        terminal.WindowRestored += (sender, e) => eventFired = true;

        // Act
        terminal.Write("\x1b[9;0t"); // CSI 9 ; 0 t

        // Assert
        eventFired.Should().BeTrue();
    }

    [TestMethod]
    public void WindowManipulation_RaiseWindow_FiresOnWindowRaise()
    {
        // Arrange
        var windowOptions = new WindowOptions { RaiseWin = true };
        var terminal = CreateTerminal(windowOptions);
        var eventFired = false;

        terminal.WindowRaised += (sender, e) => eventFired = true;

        // Act
        terminal.Write("\x1b[5t"); // CSI 5 t

        // Assert
        eventFired.Should().BeTrue();
    }

    [TestMethod]
    public void WindowManipulation_LowerWindow_FiresOnWindowLower()
    {
        // Arrange
        var windowOptions = new WindowOptions { LowerWin = true };
        var terminal = CreateTerminal(windowOptions);
        var eventFired = false;

        terminal.WindowLowered += (sender, e) => eventFired = true;

        // Act
        terminal.Write("\x1b[6t"); // CSI 6 t

        // Assert
        eventFired.Should().BeTrue();
    }

    [TestMethod]
    public void WindowManipulation_RefreshWindow_FiresOnWindowRefresh()
    {
        // Arrange
        var windowOptions = new WindowOptions { RefreshWin = true };
        var terminal = CreateTerminal(windowOptions);
        var eventFired = false;

        terminal.WindowRefreshed += (sender, e) => eventFired = true;

        // Act
        terminal.Write("\x1b[7t"); // CSI 7 t

        // Assert
        eventFired.Should().BeTrue();
    }

    [TestMethod]
    public void WindowManipulation_FullscreenToggle_FiresOnWindowFullscreen()
    {
        // Arrange
        var windowOptions = new WindowOptions { FullscreenWin = true };
        var terminal = CreateTerminal(windowOptions);
        var eventCount = 0;

        terminal.WindowFullscreened += (sender, e) => eventCount++;

        // Act
        terminal.Write("\x1b[10;0t"); // Exit fullscreen
        terminal.Write("\x1b[10;1t"); // Enter fullscreen
        terminal.Write("\x1b[10;2t"); // Toggle fullscreen

        // Assert
        eventCount.Should().Be(3);
    }

    [TestMethod]
    public void WindowManipulation_QueryWindowState_FiresOnWindowInfoRequest()
    {
        // Arrange
        var windowOptions = new WindowOptions { GetWinState = true };
        var terminal = CreateTerminal(windowOptions);
        var requestReceived = false;
        WindowInfoRequest capturedRequest = default;

        terminal.WindowInfoRequested += (sender, e) =>
        {
            requestReceived = true;
            capturedRequest = e.Request;
        };

        // Act
        terminal.Write("\x1b[11t"); // CSI 11 t

        // Assert
        requestReceived.Should().BeTrue();
        capturedRequest.Should().Be(WindowInfoRequest.State);
    }

    [TestMethod]
    public void WindowManipulation_QueryWindowPosition_FiresOnWindowInfoRequest()
    {
        // Arrange
        var windowOptions = new WindowOptions { GetWinPosition = true };
        var terminal = CreateTerminal(windowOptions);
        var requestReceived = false;
        WindowInfoRequest capturedRequest = default;

        terminal.WindowInfoRequested += (sender, e) =>
        {
            requestReceived = true;
            capturedRequest = e.Request;
        };

        // Act
        terminal.Write("\x1b[13t"); // CSI 13 t

        // Assert
        requestReceived.Should().BeTrue();
        capturedRequest.Should().Be(WindowInfoRequest.Position);
    }

    [TestMethod]
    public void WindowManipulation_QueryWindowSizePixels_FiresOnWindowInfoRequest()
    {
        // Arrange
        var windowOptions = new WindowOptions { GetWinSizePixels = true };
        var terminal = CreateTerminal(windowOptions);
        var requestReceived = false;
        WindowInfoRequest capturedRequest = default;

        terminal.WindowInfoRequested += (sender, e) =>
        {
            requestReceived = true;
            capturedRequest = e.Request;
        };

        // Act
        terminal.Write("\x1b[14t"); // CSI 14 t

        // Assert
        requestReceived.Should().BeTrue();
        capturedRequest.Should().Be(WindowInfoRequest.SizePixels);
    }

    [TestMethod]
    public void WindowManipulation_QueryScreenSizePixels_FiresOnWindowInfoRequest()
    {
        // Arrange
        var windowOptions = new WindowOptions { GetScreenSizePixels = true };
        var terminal = CreateTerminal(windowOptions);
        var requestReceived = false;

        terminal.WindowInfoRequested += (sender, e) =>
        {
            requestReceived = true;
        };

        // Act
        terminal.Write("\x1b[15t"); // CSI 15 t

        // Assert
        requestReceived.Should().BeTrue();
    }

    [TestMethod]
    public void WindowManipulation_QueryCellSizePixels_FiresOnWindowInfoRequest()
    {
        // Arrange
        var windowOptions = new WindowOptions { GetCellSizePixels = true };
        var terminal = CreateTerminal(windowOptions);
        var requestReceived = false;

        terminal.WindowInfoRequested += (sender, e) =>
        {
            requestReceived = true;
        };

        // Act
        terminal.Write("\x1b[16t"); // CSI 16 t

        // Assert
        requestReceived.Should().BeTrue();
    }

    [TestMethod]
    public void WindowManipulation_QueryTextAreaSize_RespondsWithSize()
    {
        // Arrange
        var windowOptions = new WindowOptions { GetWinSizeChars = true };
        var terminal = CreateTerminal(windowOptions);
        var responseReceived = false;
        string capturedResponse = string.Empty;

        terminal.DataReceived += (sender, e) =>
        {
            responseReceived = true;
            capturedResponse = e.Data;
        };

        // Act
        terminal.Write("\x1b[18t"); // CSI 18 t

        // Assert
        responseReceived.Should().BeTrue();
        capturedResponse.Should().Contain($"{terminal.Rows}");
        capturedResponse.Should().Contain($"{terminal.Cols}");
        capturedResponse.Should().Contain("\u001b[8;");
    }

    [TestMethod]
    public void WindowManipulation_QueryWindowTitle_SendsDirectResponse()
    {
        // Arrange - Window title query (21t) sends direct response using terminal's Title
        var windowOptions = new WindowOptions { GetWinTitle = true };
        var terminal = CreateTerminal(windowOptions);
        terminal.Title = "Test Title";
        
        var responseReceived = false;
        string capturedResponse = string.Empty;

        terminal.DataReceived += (sender, e) =>
        {
            responseReceived = true;
            capturedResponse = e.Data;
        };

        // Act
        terminal.Write("\x1b[21t"); // CSI 21 t

        // Assert
        responseReceived.Should().BeTrue();
        capturedResponse.Should().Contain("Test Title");
        capturedResponse.Should().Be("\u001b]lTest Title\u0007");
    }

    [TestMethod]
    public void WindowManipulation_QueryIconTitle_FiresOnWindowInfoRequest()
    {
        // Arrange
        var windowOptions = new WindowOptions { GetIconTitle = true };
        var terminal = CreateTerminal(windowOptions);
        var requestReceived = false;

        terminal.WindowInfoRequested += (sender, e) =>
        {
            requestReceived = true;
        };

        // Act
        terminal.Write("\x1b[20t"); // CSI 20 t

        // Assert
        requestReceived.Should().BeTrue();
    }

    [TestMethod]
    public void WindowManipulation_ResizeTextArea_ResizesTerminal()
    {
        // Arrange
        var windowOptions = new WindowOptions { SetWinSizeChars = true };
        var terminal = CreateTerminal(windowOptions);
        var initialCols = terminal.Cols;
        var initialRows = terminal.Rows;

        // Act
        terminal.Write("\x1b[8;30;100t"); // CSI 8 ; 30 ; 100 t (resize to 30 rows, 100 cols)

        // Assert
        terminal.Cols.Should().Be(100);
        terminal.Rows.Should().Be(30);
    }

    [TestMethod]
    public void WindowManipulation_ResizeTextArea_DoesNotResizeWhenPermissionDenied()
    {
        // Arrange
        var windowOptions = new WindowOptions { SetWinSizeChars = false };
        var terminal = CreateTerminal(windowOptions);
        var initialCols = terminal.Cols;
        var initialRows = terminal.Rows;

        // Act
        terminal.Write("\x1b[8;30;100t");

        // Assert
        terminal.Cols.Should().Be(initialCols);
        terminal.Rows.Should().Be(initialRows);
    }

    [TestMethod]
    public void WindowManipulation_MultipleOperations_AllFireCorrectly()
    {
        // Arrange
        var windowOptions = new WindowOptions
        {
            SetWinPosition = true,
            MinimizeWin = true,
            MaximizeWin = true,
            RaiseWin = true
        };
        var terminal = CreateTerminal(windowOptions);
        
        var moveCount = 0;
        var minimizeCount = 0;
        var maximizeCount = 0;
        var raiseCount = 0;

        terminal.WindowMoved += (sender, e) => moveCount++;
        terminal.WindowMinimized += (sender, e) => minimizeCount++;
        terminal.WindowMaximized += (sender, e) => maximizeCount++;
        terminal.WindowRaised += (sender, e) => raiseCount++;

        // Act
        terminal.Write("\x1b[3;10;20t");  // Move
        terminal.Write("\x1b[2t");        // Minimize
        terminal.Write("\x1b[9;1t");      // Maximize
        terminal.Write("\x1b[5t");        // Raise
        terminal.Write("\x1b[3;30;40t");  // Move again

        // Assert
        moveCount.Should().Be(2);
        minimizeCount.Should().Be(1);
        maximizeCount.Should().Be(1);
        raiseCount.Should().Be(1);
    }

    [TestMethod]
    public void WindowManipulation_InvalidOperation_DoesNotCrash()
    {
        // Arrange
        var terminal = CreateTerminal();
        var anyEventFired = false;

        terminal.WindowMoved += (sender, e) => anyEventFired = true;
        terminal.WindowResized += (sender, e) => anyEventFired = true;
        terminal.WindowMinimized += (sender, e) => anyEventFired = true;
        terminal.WindowMaximized += (sender, e) => anyEventFired = true;
        terminal.WindowRestored += (sender, e) => anyEventFired = true;
        terminal.WindowRaised += (sender, e) => anyEventFired = true;
        terminal.WindowLowered += (sender, e) => anyEventFired = true;
        terminal.WindowRefreshed += (sender, e) => anyEventFired = true;
        terminal.WindowFullscreened += (sender, e) => anyEventFired = true;

        // Act - Invalid operation code should be ignored without throwing
        var exception = Record.Exception(() => terminal.Write("\x1b[999t"));

        // Assert
        exception.Should().BeNull();
        anyEventFired.Should().BeFalse(); // No valid event should fire for invalid operation
    }

    [TestMethod]
    public void WindowManipulation_MissingParameters_DoesNotCrash()
    {
        // Arrange
        var windowOptions = new WindowOptions { SetWinPosition = true };
        var terminal = CreateTerminal(windowOptions);
        var eventFired = false;
        int capturedX = -1, capturedY = -1;

        terminal.WindowMoved += (sender, e) =>
        {
            eventFired = true;
            capturedX = e.X;
            capturedY = e.Y;
        };

        // Act - Missing parameters should be handled gracefully
        var exception = Record.Exception(() => terminal.Write("\x1b[3t"));

        // Assert
        exception.Should().BeNull();
        // If event fires, parameters should default to 0
        if (eventFired)
        {
            capturedX.Should().Be(0);
            capturedY.Should().Be(0);
        }
    }

    [TestMethod]
    public void Dispose_ClearsWindowEvents()
    {
        // Arrange
        var terminal = CreateTerminal();
        var eventCount = 0;
        var windowOptions = new WindowOptions { MinimizeWin = true };
        terminal.Options.WindowOptions.MinimizeWin = true;
        
        terminal.WindowMinimized += (sender, e) => eventCount++;

        // Act
        terminal.Dispose();
        terminal.Write("\x1b[2t"); // Try to trigger minimize

        // Assert
        eventCount.Should().Be(0); // Event should not fire after dispose
    }

    [TestMethod]
    public void WindowInfoRequest_AllEnumValues_AreDefined()
    {
        // Assert - Verify all expected enum values exist and have distinct values
        var allValues = Enum.GetValues<WindowInfoRequest>();
        
        allValues.Should().Contain(WindowInfoRequest.Position);
        allValues.Should().Contain(WindowInfoRequest.SizePixels);
        allValues.Should().Contain(WindowInfoRequest.SizeCharacters);
        allValues.Should().Contain(WindowInfoRequest.ScreenSizePixels);
        allValues.Should().Contain(WindowInfoRequest.CellSizePixels);
        allValues.Should().Contain(WindowInfoRequest.Title);
        allValues.Should().Contain(WindowInfoRequest.IconTitle);
        allValues.Should().Contain(WindowInfoRequest.State);

        // Verify all values are unique
        var uniqueValues = allValues.Distinct().ToList();
        uniqueValues.Count.Should().Be(allValues.Length);
    }

    // ===== New Request/Response Tests =====

    [TestMethod]
    public void WindowInfoRequest_StateQuery_SendsResponseWhenHandled()
    {
        // Arrange
        var windowOptions = new WindowOptions { GetWinState = true };
        var terminal = CreateTerminal(windowOptions);
        string? capturedResponse = null;

        terminal.WindowInfoRequested += (sender, e) =>
        {
            if (e.Request == WindowInfoRequest.State)
            {
                e.Handled = true;
                e.IsIconified = false; // Window is not minimized
            }
        };

        terminal.DataReceived += (sender, e) =>
        {
            capturedResponse = e.Data;
        };

        // Act
        terminal.Write("\x1b[11t"); // CSI 11 t - Query window state

        // Assert
        capturedResponse.Should().NotBeNull();
        capturedResponse.Should().Be("\u001b[1t"); // 1 = not iconified
    }

    [TestMethod]
    public void WindowInfoRequest_StateQuery_SendsIconifiedResponseWhenMinimized()
    {
        // Arrange
        var windowOptions = new WindowOptions { GetWinState = true };
        var terminal = CreateTerminal(windowOptions);
        string? capturedResponse = null;

        terminal.WindowInfoRequested += (sender, e) =>
        {
            if (e.Request == WindowInfoRequest.State)
            {
                e.Handled = true;
                e.IsIconified = true; // Window is minimized
            }
        };

        terminal.DataReceived += (sender, e) =>
        {
            capturedResponse = e.Data;
        };

        // Act
        terminal.Write("\x1b[11t");

        // Assert
        capturedResponse.Should().NotBeNull();
        capturedResponse.Should().Be("\u001b[2t"); // 2 = iconified
    }

    [TestMethod]
    public void WindowInfoRequest_StateQuery_NoResponseWhenNotHandled()
    {
        // Arrange
        var windowOptions = new WindowOptions { GetWinState = true };
        var terminal = CreateTerminal(windowOptions);
        string? capturedResponse = null;

        terminal.WindowInfoRequested += (sender, e) =>
        {
            // Don't set Handled = true
        };

        terminal.DataReceived += (sender, e) =>
        {
            capturedResponse = e.Data;
        };

        // Act
        terminal.Write("\x1b[11t");

        // Assert
        capturedResponse.Should().BeNull(); // No response when not handled
    }

    [TestMethod]
    public void WindowInfoRequest_PositionQuery_SendsPositionResponse()
    {
        // Arrange
        var windowOptions = new WindowOptions { GetWinPosition = true };
        var terminal = CreateTerminal(windowOptions);
        string? capturedResponse = null;

        terminal.WindowInfoRequested += (sender, e) =>
        {
            if (e.Request == WindowInfoRequest.Position)
            {
                e.Handled = true;
                e.X = 100;
                e.Y = 200;
            }
        };

        terminal.DataReceived += (sender, e) =>
        {
            capturedResponse = e.Data;
        };

        // Act
        terminal.Write("\x1b[13t"); // CSI 13 t - Query window position

        // Assert
        capturedResponse.Should().NotBeNull();
        capturedResponse.Should().Be("\u001b[3;100;200t");
    }

    [TestMethod]
    public void WindowInfoRequest_SizePixelsQuery_SendsSizeResponse()
    {
        // Arrange
        var windowOptions = new WindowOptions { GetWinSizePixels = true };
        var terminal = CreateTerminal(windowOptions);
        string? capturedResponse = null;

        terminal.WindowInfoRequested += (sender, e) =>
        {
            if (e.Request == WindowInfoRequest.SizePixels)
            {
                e.Handled = true;
                e.WidthPixels = 800;
                e.HeightPixels = 600;
            }
        };

        terminal.DataReceived += (sender, e) =>
        {
            capturedResponse = e.Data;
        };

        // Act
        terminal.Write("\x1b[14t"); // CSI 14 t - Query window size in pixels

        // Assert
        capturedResponse.Should().NotBeNull();
        capturedResponse.Should().Be("\u001b[4;600;800t"); // Format: CSI 4 ; height ; width t
    }

    [TestMethod]
    public void WindowInfoRequest_ScreenSizePixelsQuery_SendsScreenSizeResponse()
    {
        // Arrange
        var windowOptions = new WindowOptions { GetScreenSizePixels = true };
        var terminal = CreateTerminal(windowOptions);
        string? capturedResponse = null;

        terminal.WindowInfoRequested += (sender, e) =>
        {
            if (e.Request == WindowInfoRequest.ScreenSizePixels)
            {
                e.Handled = true;
                e.WidthPixels = 1920;
                e.HeightPixels = 1080;
            }
        };

        terminal.DataReceived += (sender, e) =>
        {
            capturedResponse = e.Data;
        };

        // Act
        terminal.Write("\x1b[15t"); // CSI 15 t - Query screen size in pixels

        // Assert
        capturedResponse.Should().NotBeNull();
        capturedResponse.Should().Be("\u001b[5;1080;1920t"); // Format: CSI 5 ; height ; width t
    }

    [TestMethod]
    public void WindowInfoRequest_CellSizePixelsQuery_SendsCellSizeResponse()
    {
        // Arrange
        var windowOptions = new WindowOptions { GetCellSizePixels = true };
        var terminal = CreateTerminal(windowOptions);
        string? capturedResponse = null;

        terminal.WindowInfoRequested += (sender, e) =>
        {
            if (e.Request == WindowInfoRequest.CellSizePixels)
            {
                e.Handled = true;
                e.CellWidth = 8;
                e.CellHeight = 16;
            }
        };

        terminal.DataReceived += (sender, e) =>
        {
            capturedResponse = e.Data;
        };

        // Act
        terminal.Write("\x1b[16t"); // CSI 16 t - Query cell size in pixels

        // Assert
        capturedResponse.Should().NotBeNull();
        capturedResponse.Should().Be("\u001b[6;16;8t"); // Format: CSI 6 ; height ; width t
    }

    [TestMethod]
    public void WindowInfoRequest_IconTitleQuery_SendsTitleResponse()
    {
        // Arrange
        var windowOptions = new WindowOptions { GetIconTitle = true };
        var terminal = CreateTerminal(windowOptions);
        string? capturedResponse = null;

        terminal.WindowInfoRequested += (sender, e) =>
        {
            if (e.Request == WindowInfoRequest.IconTitle)
            {
                e.Handled = true;
                e.Title = "My Icon Title";
            }
        };

        terminal.DataReceived += (sender, e) =>
        {
            capturedResponse = e.Data;
        };

        // Act
        terminal.Write("\x1b[20t"); // CSI 20 t - Query icon title

        // Assert
        capturedResponse.Should().NotBeNull();
        capturedResponse.Should().Be("\u001b]LMy Icon Title\u0007"); // Format: OSC L title BEL
    }

    [TestMethod]
    public void WindowInfoRequest_IconTitleQuery_NoResponseWhenTitleIsNull()
    {
        // Arrange
        var windowOptions = new WindowOptions { GetIconTitle = true };
        var terminal = CreateTerminal(windowOptions);
        string? capturedResponse = null;

        terminal.WindowInfoRequested += (sender, e) =>
        {
            if (e.Request == WindowInfoRequest.IconTitle)
            {
                e.Handled = true;
                e.Title = null; // Explicitly null
            }
        };

        terminal.DataReceived += (sender, e) =>
        {
            capturedResponse = e.Data;
        };

        // Act
        terminal.Write("\x1b[20t");

        // Assert
        capturedResponse.Should().BeNull(); // No response when title is null
    }

    [TestMethod]
    public void WindowInfoRequest_TextAreaSizeQuery_SendsDirectResponse()
    {
        // Arrange - Text area size (18t) responds directly without event handler
        var windowOptions = new WindowOptions { GetWinSizeChars = true };
        var options = new TerminalOptions
        {
            Cols = 120,
            Rows = 40,
            WindowOptions = windowOptions
        };
        var terminal = new Terminal(options);
        string? capturedResponse = null;

        terminal.DataReceived += (sender, e) =>
        {
            capturedResponse = e.Data;
        };

        // Act
        terminal.Write("\x1b[18t"); // CSI 18 t - Query text area size in characters

        // Assert
        capturedResponse.Should().NotBeNull();
        capturedResponse.Should().Be("\u001b[8;40;120t"); // Format: CSI 8 ; rows ; cols t
    }

    [TestMethod]
    public void WindowInfoRequest_ScreenSizeCharsQuery_SendsDirectResponse()
    {
        // Arrange - Screen size in chars (19t) responds directly
        var windowOptions = new WindowOptions { GetScreenSizePixels = true };
        var options = new TerminalOptions
        {
            Cols = 80,
            Rows = 24,
            WindowOptions = windowOptions
        };
        var terminal = new Terminal(options);
        string? capturedResponse = null;

        terminal.DataReceived += (sender, e) =>
        {
            capturedResponse = e.Data;
        };

        // Act
        terminal.Write("\x1b[19t"); // CSI 19 t - Query screen size in characters

        // Assert
        capturedResponse.Should().NotBeNull();
        capturedResponse.Should().Be("\u001b[9;24;80t"); // Format: CSI 9 ; rows ; cols t
    }

    [TestMethod]
    public void WindowInfoRequest_TitleQuery_SendsDirectResponseFromTerminalTitle()
    {
        // Arrange - Window title (21t) uses terminal's current title
        var windowOptions = new WindowOptions { GetWinTitle = true };
        var terminal = CreateTerminal(windowOptions);
        terminal.Title = "Terminal Window Title";
        string? capturedResponse = null;

        terminal.DataReceived += (sender, e) =>
        {
            capturedResponse = e.Data;
        };

        // Act
        terminal.Write("\x1b[21t"); // CSI 21 t - Query window title

        // Assert
        capturedResponse.Should().NotBeNull();
        capturedResponse.Should().Be("\u001b]lTerminal Window Title\u0007"); // Format: OSC l title BEL
    }

    [TestMethod]
    public void WindowInfoRequest_EventArgsPropertiesInitializeCorrectly()
    {
        // Arrange
        var windowOptions = new WindowOptions { GetWinPosition = true };
        var terminal = CreateTerminal(windowOptions);
        Events.TerminalEvents.WindowInfoRequestedEventArgs? capturedArgs = null;

        terminal.WindowInfoRequested += (sender, e) =>
        {
            capturedArgs = e;
        };

        // Act
        terminal.Write("\x1b[13t");

        // Assert
        capturedArgs.Should().NotBeNull();
        capturedArgs.Request.Should().Be(WindowInfoRequest.Position);
        capturedArgs.Handled.Should().BeFalse(); // Default is false
        capturedArgs.X.Should().Be(0); // Default is 0
        capturedArgs.Y.Should().Be(0);
        capturedArgs.WidthPixels.Should().Be(0);
        capturedArgs.HeightPixels.Should().Be(0);
        capturedArgs.CellWidth.Should().Be(0);
        capturedArgs.CellHeight.Should().Be(0);
        capturedArgs.Title.Should().BeNull();
        capturedArgs.IsIconified.Should().BeFalse();
    }

    [TestMethod]
    public void WindowInfoRequest_MultipleQueries_EachHandledIndependently()
    {
        // Arrange
        var windowOptions = new WindowOptions 
        { 
            GetWinState = true, 
            GetWinPosition = true,
            GetWinSizePixels = true
        };
        var terminal = CreateTerminal(windowOptions);
        var responses = new List<string>();

        terminal.WindowInfoRequested += (sender, e) =>
        {
            e.Handled = true;
            switch (e.Request)
            {
                case WindowInfoRequest.State:
                    e.IsIconified = false;
                    break;
                case WindowInfoRequest.Position:
                    e.X = 50;
                    e.Y = 75;
                    break;
                case WindowInfoRequest.SizePixels:
                    e.WidthPixels = 640;
                    e.HeightPixels = 480;
                    break;
            }
        };

        terminal.DataReceived += (sender, e) =>
        {
            responses.Add(e.Data);
        };

        // Act
        terminal.Write("\x1b[11t"); // State
        terminal.Write("\x1b[13t"); // Position
        terminal.Write("\x1b[14t"); // Size

        // Assert
        responses.Count.Should().Be(3);
        responses[0].Should().Be("\u001b[1t"); // Not iconified
        responses[1].Should().Be("\u001b[3;50;75t"); // Position
        responses[2].Should().Be("\u001b[4;480;640t"); // Size
    }

    [TestMethod]
    public void WindowManipulation_PermissionsRespected_ForAllOperations()
    {
        // Arrange
        var windowOptions = new WindowOptions(); // All permissions false by default
        var terminal = CreateTerminal(windowOptions);
        
        var eventCount = 0;
        terminal.WindowMoved += (sender, e) => eventCount++;
        terminal.WindowResized += (sender, e) => eventCount++;
        terminal.WindowMinimized += (sender, e) => eventCount++;
        terminal.WindowMaximized += (sender, e) => eventCount++;
        terminal.WindowRestored += (sender, e) => eventCount++;
        terminal.WindowRaised += (sender, e) => eventCount++;
        terminal.WindowLowered += (sender, e) => eventCount++;
        terminal.WindowRefreshed += (sender, e) => eventCount++;
        terminal.WindowFullscreened += (sender, e) => eventCount++;
        terminal.WindowInfoRequested += (sender, e) => eventCount++;

        // Act - Try all operations
        terminal.Write("\x1b[3;10;20t");  // Move
        terminal.Write("\x1b[4;600;800t"); // Resize
        terminal.Write("\x1b[2t");         // Minimize
        terminal.Write("\x1b[9;1t");       // Maximize
        terminal.Write("\x1b[1t");         // Restore
        terminal.Write("\x1b[5t");         // Raise
        terminal.Write("\x1b[6t");         // Lower
        terminal.Write("\x1b[7t");         // Refresh
        terminal.Write("\x1b[10;1t");      // Fullscreen
        terminal.Write("\x1b[11t");        // Query state

        // Assert - No events should fire because all permissions are false
        eventCount.Should().Be(0);
    }
}
