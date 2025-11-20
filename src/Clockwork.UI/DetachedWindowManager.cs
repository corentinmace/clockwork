using Clockwork.Core.Logging;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Clockwork.UI;

/// <summary>
/// Manages detached windows as independent OS windows with their own SDL2 window and ImGui context.
/// Each detached window is a completely separate window that can be moved to other monitors.
/// </summary>
public static class DetachedWindowManager
{
    private static readonly Dictionary<string, DetachedWindowState> _windows = new();

    private class DetachedWindowState
    {
        public Vector2 DefaultSize { get; set; }
        public IndependentWindow? Window { get; set; }
        public string Title { get; set; } = "";
        public Action? DrawContent { get; set; }
    }

    /// <summary>
    /// Register a window that can be detached
    /// </summary>
    public static void RegisterWindow(string id, Vector2 defaultSize)
    {
        if (!_windows.ContainsKey(id))
        {
            _windows[id] = new DetachedWindowState
            {
                DefaultSize = defaultSize,
                Window = null
            };
        }
    }

    /// <summary>
    /// Toggle a detached window (create if not exists, close if exists)
    /// </summary>
    public static void Toggle(string id, string title, Action drawContent)
    {
        if (!_windows.ContainsKey(id))
        {
            AppLogger.Warn($"DetachedWindowManager: Attempted to toggle unregistered window '{id}'");
            return;
        }

        var state = _windows[id];

        if (state.Window != null && state.Window.IsOpen)
        {
            // Window is open, close it
            AppLogger.Debug($"DetachedWindowManager: Closing window '{id}'");
            state.Window.Dispose();
            state.Window = null;
        }
        else
        {
            // Window is closed or doesn't exist, create it
            AppLogger.Debug($"DetachedWindowManager: Opening window '{id}'");

            // Get main window position to offset the detached window
            var mainViewport = ImGui.GetMainViewport();
            Vector2 position = new Vector2(
                mainViewport.Pos.X + mainViewport.Size.X + 20,
                mainViewport.Pos.Y + 50
            );

            state.Title = title;
            state.DrawContent = drawContent;
            state.Window = new IndependentWindow(title, state.DefaultSize, position, drawContent);

            AppLogger.Info($"DetachedWindowManager: Created independent window '{id}'");
        }
    }

    /// <summary>
    /// Check if a window is currently detached (open)
    /// </summary>
    public static bool IsOpen(string id)
    {
        if (!_windows.ContainsKey(id))
            return false;

        var state = _windows[id];
        return state.Window != null && state.Window.IsOpen;
    }

    /// <summary>
    /// Update all detached windows. Call this in the main render loop.
    /// </summary>
    public static void UpdateAll(double deltaTime)
    {
        // Store current ImGui context to restore it later
        IntPtr mainContext = ImGui.GetCurrentContext();

        // Update all open windows
        foreach (var kvp in _windows.ToList()) // ToList to avoid modification during iteration
        {
            var state = kvp.Value;

            if (state.Window != null)
            {
                if (state.Window.IsOpen)
                {
                    state.Window.Update(deltaTime);
                }
                else
                {
                    // Window was closed, dispose it
                    AppLogger.Debug($"DetachedWindowManager: Window '{kvp.Key}' was closed, disposing");
                    state.Window.Dispose();
                    state.Window = null;
                }
            }
        }

        // Restore main ImGui context
        ImGui.SetCurrentContext(mainContext);
    }

    /// <summary>
    /// Close and dispose all detached windows
    /// </summary>
    public static void CloseAll()
    {
        AppLogger.Debug("DetachedWindowManager: Closing all windows");

        foreach (var state in _windows.Values)
        {
            if (state.Window != null)
            {
                state.Window.Dispose();
                state.Window = null;
            }
        }
    }
}
