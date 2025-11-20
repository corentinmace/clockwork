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
    private readonly Dictionary<uint, ViewportWindow> _viewportWindows = new();
    private bool _disposed = false;

    // Define delegate types matching ImGui's expected signatures
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void CreateWindowDelegate(ImGuiViewport* viewport);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void DestroyWindowDelegate(ImGuiViewport* viewport);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void ShowWindowDelegate(ImGuiViewport* viewport);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void SetWindowPosDelegate(ImGuiViewport* viewport, Vector2 pos);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate Vector2 GetWindowPosDelegate(ImGuiViewport* viewport);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void SetWindowSizeDelegate(ImGuiViewport* viewport, Vector2 size);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate Vector2 GetWindowSizeDelegate(ImGuiViewport* viewport);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void SetWindowFocusDelegate(ImGuiViewport* viewport);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate byte GetWindowFocusDelegate(ImGuiViewport* viewport);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate byte GetWindowMinimizedDelegate(ImGuiViewport* viewport);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void SetWindowTitleDelegate(ImGuiViewport* viewport, IntPtr title);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void RenderWindowDelegate(ImGuiViewport* viewport, IntPtr renderArg);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void SwapBuffersDelegate(ImGuiViewport* viewport, IntPtr renderArg);

    // Store delegates to prevent garbage collection
    private CreateWindowDelegate _createWindowDelegate;
    private DestroyWindowDelegate _destroyWindowDelegate;
    private ShowWindowDelegate _showWindowDelegate;
    private SetWindowPosDelegate _setWindowPosDelegate;
    private GetWindowPosDelegate _getWindowPosDelegate;
    private SetWindowSizeDelegate _setWindowSizeDelegate;
    private GetWindowSizeDelegate _getWindowSizeDelegate;
    private SetWindowFocusDelegate _setWindowFocusDelegate;
    private GetWindowFocusDelegate _getWindowFocusDelegate;
    private GetWindowMinimizedDelegate _getWindowMinimizedDelegate;
    private SetWindowTitleDelegate _setWindowTitleDelegate;
    private RenderWindowDelegate _renderWindowDelegate;
    private SwapBuffersDelegate _swapBuffersDelegate;

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

        // Assign callbacks to ImGui platform IO
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

    private void CreateWindow(ImGuiViewport* viewport)
    {
        try
        {
            var viewportPtr = new ImGuiViewportPtr(viewport);
            var window = new ViewportWindow(viewportPtr, _mainGraphicsDevice);
            _viewportWindows[viewportPtr.ID] = window;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ViewportManager] Error creating viewport window: {ex.Message}");
        }
    }

    private void DestroyWindow(ImGuiViewport* viewport)
    {
        var viewportPtr = new ImGuiViewportPtr(viewport);
        if (_viewportWindows.TryGetValue(viewportPtr.ID, out var window))
        {
            window.Dispose();
            _viewportWindows.Remove(viewportPtr.ID);
        }
    }

    private void ShowWindow(ImGuiViewport* viewport)
    {
        var viewportPtr = new ImGuiViewportPtr(viewport);
        if (_viewportWindows.TryGetValue(viewportPtr.ID, out var window))
        {
            window.SetVisible(true);
        }
    }

    private void SetWindowPos(ImGuiViewport* viewport, Vector2 pos)
    {
        var viewportPtr = new ImGuiViewportPtr(viewport);
        if (_viewportWindows.TryGetValue(viewportPtr.ID, out var window))
        {
            window.SetPosition(pos);
        }
    }

    private Vector2 GetWindowPos(ImGuiViewport* viewport)
    {
        var viewportPtr = new ImGuiViewportPtr(viewport);
        if (_viewportWindows.TryGetValue(viewportPtr.ID, out var window))
        {
            return new Vector2((float)window.Window.X, (float)window.Window.Y);
        }
        return Vector2.Zero;
    }

    private void SetWindowSize(ImGuiViewport* viewport, Vector2 size)
    {
        var viewportPtr = new ImGuiViewportPtr(viewport);
        if (_viewportWindows.TryGetValue(viewportPtr.ID, out var window))
        {
            window.SetSize(size);
        }
    }

    private Vector2 GetWindowSize(ImGuiViewport* viewport)
    {
        var viewportPtr = new ImGuiViewportPtr(viewport);
        if (_viewportWindows.TryGetValue(viewportPtr.ID, out var window))
        {
            return new Vector2((float)window.Window.Width, (float)window.Window.Height);
        }
        return Vector2.Zero;
    }

    private void SetWindowFocus(ImGuiViewport* viewport)
    {
        var viewportPtr = new ImGuiViewportPtr(viewport);
        if (_viewportWindows.TryGetValue(viewportPtr.ID, out var window))
        {
            window.SetFocus();
        }
    }

    private byte GetWindowFocus(ImGuiViewport* viewport)
    {
        var viewportPtr = new ImGuiViewportPtr(viewport);
        if (_viewportWindows.TryGetValue(viewportPtr.ID, out var window))
        {
            return (byte)(window.Window.Focused ? 1 : 0);
        }
        return 0;
    }

    private byte GetWindowMinimized(ImGuiViewport* viewport)
    {
        var viewportPtr = new ImGuiViewportPtr(viewport);
        if (_viewportWindows.TryGetValue(viewportPtr.ID, out var window))
        {
            return (byte)(window.Window.WindowState == WindowState.Minimized ? 1 : 0);
        }
        return 0;
    }

    private void SetWindowTitle(ImGuiViewport* viewport, IntPtr title)
    {
        var viewportPtr = new ImGuiViewportPtr(viewport);
        if (_viewportWindows.TryGetValue(viewportPtr.ID, out var window))
        {
            string titleStr = Marshal.PtrToStringUTF8(title) ?? "Viewport";
            window.SetTitle(titleStr);
        }
    }

    private void RenderWindow(ImGuiViewport* viewport, IntPtr renderArg)
    {
        // Rendering is handled in UpdateAndRender
    }

    private void SwapBuffers(ImGuiViewport* viewport, IntPtr renderArg)
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
