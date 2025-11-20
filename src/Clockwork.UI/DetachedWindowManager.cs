using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Clockwork.UI;

/// <summary>
/// Manages detached windows that can be pulled out of the main window as separate OS windows
/// </summary>
public static class DetachedWindowManager
{
    private static readonly Dictionary<string, DetachedWindowState> _windows = new();

    private class DetachedWindowState
    {
        public bool IsOpen { get; set; }
        public Vector2 DefaultSize { get; set; }
        public Action? DrawContent { get; set; }
    }

    /// <summary>
    /// Register a detachable window
    /// </summary>
    /// <param name="id">Unique identifier for the window</param>
    /// <param name="defaultSize">Default size when first opened</param>
    public static void RegisterWindow(string id, Vector2 defaultSize)
    {
        if (!_windows.ContainsKey(id))
        {
            _windows[id] = new DetachedWindowState
            {
                IsOpen = false,
                DefaultSize = defaultSize
            };
        }
    }

    /// <summary>
    /// Toggle a detached window
    /// </summary>
    /// <param name="id">Unique identifier for the window</param>
    public static void Toggle(string id)
    {
        if (_windows.ContainsKey(id))
        {
            _windows[id].IsOpen = !_windows[id].IsOpen;
        }
    }

    /// <summary>
    /// Check if a window is currently open
    /// </summary>
    public static bool IsOpen(string id)
    {
        return _windows.ContainsKey(id) && _windows[id].IsOpen;
    }

    /// <summary>
    /// Draw a detached window with custom content
    /// </summary>
    /// <param name="id">Unique identifier for the window</param>
    /// <param name="title">Window title</param>
    /// <param name="drawContent">Action to draw the window content</param>
    public static void DrawWindow(string id, string title, Action drawContent)
    {
        if (!_windows.ContainsKey(id) || !_windows[id].IsOpen)
            return;

        var state = _windows[id];

        // Set window size on first use
        ImGui.SetNextWindowSize(state.DefaultSize, ImGuiCond.FirstUseEver);

        // Position the window at center of viewport on first use
        var viewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(viewport.GetCenter(), ImGuiCond.FirstUseEver, new Vector2(0.5f, 0.5f));

        // Create a window that can be dragged outside the main window with viewports
        bool isOpen = state.IsOpen;
        ImGuiWindowFlags flags = ImGuiWindowFlags.None; // Allow docking and viewport detachment

        if (ImGui.Begin(title, ref isOpen, flags))
        {
            drawContent?.Invoke();
            ImGui.End();
        }

        state.IsOpen = isOpen;
    }

    /// <summary>
    /// Close all detached windows
    /// </summary>
    public static void CloseAll()
    {
        foreach (var window in _windows.Values)
        {
            window.IsOpen = false;
        }
    }
}
