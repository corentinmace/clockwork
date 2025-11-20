using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using Veldrid;

namespace Clockwork.UI;

/// <summary>
/// Manages ImGui viewports for multi-window support with Veldrid backend.
/// Configures platform callbacks and handles viewport lifecycle.
/// </summary>
public unsafe class ViewportManager : IDisposable
{
    private readonly GraphicsDevice _mainGraphicsDevice;
    private readonly Dictionary<IntPtr, ViewportWindow> _viewportWindows = new();
    private bool _disposed = false;

    // Store delegates to prevent garbage collection
    private ImGuiPlatformIO.CreateWindow_Delegate _createWindowDelegate;
    private ImGuiPlatformIO.DestroyWindow_Delegate _destroyWindowDelegate;
    private ImGuiPlatformIO.ShowWindow_Delegate _showWindowDelegate;
    private ImGuiPlatformIO.SetWindowPos_Delegate _setWindowPosDelegate;
    private ImGuiPlatformIO.GetWindowPos_Delegate _getWindowPosDelegate;
    private ImGuiPlatformIO.SetWindowSize_Delegate _setWindowSizeDelegate;
    private ImGuiPlatformIO.GetWindowSize_Delegate _getWindowSizeDelegate;
    private ImGuiPlatformIO.SetWindowFocus_Delegate _setWindowFocusDelegate;
    private ImGuiPlatformIO.GetWindowFocus_Delegate _getWindowFocusDelegate;
    private ImGuiPlatformIO.GetWindowMinimized_Delegate _getWindowMinimizedDelegate;
    private ImGuiPlatformIO.SetWindowTitle_Delegate _setWindowTitleDelegate;
    private ImGuiPlatformIO.RenderWindow_Delegate _renderWindowDelegate;
    private ImGuiPlatformIO.SwapBuffers_Delegate _swapBuffersDelegate;

    public ViewportManager(GraphicsDevice mainGraphicsDevice)
    {
        _mainGraphicsDevice = mainGraphicsDevice;
        InitializePlatformCallbacks();
    }

    private void InitializePlatformCallbacks()
    {
        var platformIO = ImGui.GetPlatformIO();

        // Create delegates and store them to prevent GC
        _createWindowDelegate = CreateWindow;
        _destroyWindowDelegate = DestroyWindow;
        _showWindowDelegate = ShowWindow;
        _setWindowPosDelegate = SetWindowPos;
        _getWindowPosDelegate = GetWindowPos;
        _setWindowSizeDelegate = SetWindowSize;
        _getWindowSizeDelegate = GetWindowSize;
        _setWindowFocusDelegate = SetWindowFocus;
        _getWindowFocusDelegate = GetWindowFocus;
        _getWindowMinimizedDelegate = GetWindowMinimized;
        _setWindowTitleDelegate = SetWindowTitle;
        _renderWindowDelegate = RenderWindow;
        _swapBuffersDelegate = SwapBuffers;

        // Assign callbacks
        platformIO.Platform_CreateWindow = Marshal.GetFunctionPointerForDelegate(_createWindowDelegate);
        platformIO.Platform_DestroyWindow = Marshal.GetFunctionPointerForDelegate(_destroyWindowDelegate);
        platformIO.Platform_ShowWindow = Marshal.GetFunctionPointerForDelegate(_showWindowDelegate);
        platformIO.Platform_SetWindowPos = Marshal.GetFunctionPointerForDelegate(_setWindowPosDelegate);
        platformIO.Platform_GetWindowPos = Marshal.GetFunctionPointerForDelegate(_getWindowPosDelegate);
        platformIO.Platform_SetWindowSize = Marshal.GetFunctionPointerForDelegate(_setWindowSizeDelegate);
        platformIO.Platform_GetWindowSize = Marshal.GetFunctionPointerForDelegate(_getWindowSizeDelegate);
        platformIO.Platform_SetWindowFocus = Marshal.GetFunctionPointerForDelegate(_setWindowFocusDelegate);
        platformIO.Platform_GetWindowFocus = Marshal.GetFunctionPointerForDelegate(_getWindowFocusDelegate);
        platformIO.Platform_GetWindowMinimized = Marshal.GetFunctionPointerForDelegate(_getWindowMinimizedDelegate);
        platformIO.Platform_SetWindowTitle = Marshal.GetFunctionPointerForDelegate(_setWindowTitleDelegate);
        platformIO.Platform_RenderWindow = Marshal.GetFunctionPointerForDelegate(_renderWindowDelegate);
        platformIO.Platform_SwapBuffers = Marshal.GetFunctionPointerForDelegate(_swapBuffersDelegate);
    }

    private void CreateWindow(ImGuiViewportPtr viewport)
    {
        try
        {
            var window = new ViewportWindow(viewport, _mainGraphicsDevice);
            _viewportWindows[viewport.ID] = window;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ViewportManager] Error creating viewport window: {ex.Message}");
        }
    }

    private void DestroyWindow(ImGuiViewportPtr viewport)
    {
        if (_viewportWindows.TryGetValue(viewport.ID, out var window))
        {
            window.Dispose();
            _viewportWindows.Remove(viewport.ID);
        }
    }

    private void ShowWindow(ImGuiViewportPtr viewport)
    {
        if (_viewportWindows.TryGetValue(viewport.ID, out var window))
        {
            window.SetVisible(true);
        }
    }

    private void SetWindowPos(ImGuiViewportPtr viewport, Vector2 pos)
    {
        if (_viewportWindows.TryGetValue(viewport.ID, out var window))
        {
            window.SetPosition(pos);
        }
    }

    private Vector2 GetWindowPos(ImGuiViewportPtr viewport)
    {
        if (_viewportWindows.TryGetValue(viewport.ID, out var window))
        {
            return new Vector2(window.Window.X, window.Window.Y);
        }
        return Vector2.Zero;
    }

    private void SetWindowSize(ImGuiViewportPtr viewport, Vector2 size)
    {
        if (_viewportWindows.TryGetValue(viewport.ID, out var window))
        {
            window.SetSize(size);
        }
    }

    private Vector2 GetWindowSize(ImGuiViewportPtr viewport)
    {
        if (_viewportWindows.TryGetValue(viewport.ID, out var window))
        {
            return new Vector2(window.Window.Width, window.Window.Height);
        }
        return Vector2.Zero;
    }

    private void SetWindowFocus(ImGuiViewportPtr viewport)
    {
        if (_viewportWindows.TryGetValue(viewport.ID, out var window))
        {
            window.SetFocus();
        }
    }

    private byte GetWindowFocus(ImGuiViewportPtr viewport)
    {
        if (_viewportWindows.TryGetValue(viewport.ID, out var window))
        {
            return (byte)(window.Window.Focused ? 1 : 0);
        }
        return 0;
    }

    private byte GetWindowMinimized(ImGuiViewportPtr viewport)
    {
        if (_viewportWindows.TryGetValue(viewport.ID, out var window))
        {
            return (byte)(window.Window.WindowState == Veldrid.Sdl2.WindowState.Minimized ? 1 : 0);
        }
        return 0;
    }

    private void SetWindowTitle(ImGuiViewportPtr viewport, IntPtr title)
    {
        if (_viewportWindows.TryGetValue(viewport.ID, out var window))
        {
            string titleStr = Marshal.PtrToStringUTF8(title) ?? "Viewport";
            window.SetTitle(titleStr);
        }
    }

    private void RenderWindow(ImGuiViewportPtr viewport, IntPtr renderArg)
    {
        // Rendering is handled in UpdateAndRender
    }

    private void SwapBuffers(ImGuiViewportPtr viewport, IntPtr renderArg)
    {
        // Swapping is handled in UpdateAndRender
    }

    /// <summary>
    /// Update and render all viewport windows.
    /// Call this in the main render loop.
    /// </summary>
    public void UpdateAndRender(float deltaSeconds)
    {
        if (_disposed)
            return;

        foreach (var window in _viewportWindows.Values)
        {
            window.Render(deltaSeconds);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        // Dispose all viewport windows
        foreach (var window in _viewportWindows.Values)
        {
            window.Dispose();
        }
        _viewportWindows.Clear();
    }
}
