using Clockwork.Core.Logging;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Clockwork.UI;

/// <summary>
/// Manages detached windows as floating ImGui windows within the main window.
/// These windows can be moved freely, docked, or undocked within the main window space.
/// </summary>
public static class DetachedWindowManager
{
    private static readonly Dictionary<string, DetachedWindowState> _windows = new();

    private class DetachedWindowState
    {
        public Vector2 DefaultSize { get; set; }
        public bool IsOpen { get; set; }
        public string Title { get; set; } = "";
        public Action? DrawContent { get; set; }
        public Vector2 Position { get; set; }
        public bool PositionSet { get; set; }
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
                IsOpen = false
            };
        }
    }

    /// <summary>
    /// Toggle a detached window (open if closed, close if open)
    /// </summary>
    public static void Toggle(string id, string title, Action drawContent)
    {
        if (!_windows.ContainsKey(id))
        {
            AppLogger.Warn($"DetachedWindowManager: Attempted to toggle unregistered window '{id}'");
            return;
        }

        var state = _windows[id];

        if (state.IsOpen)
        {
            // Window is open, close it
            AppLogger.Debug($"DetachedWindowManager: Closing floating window '{id}'");
            state.IsOpen = false;
        }
        else
        {
            // Window is closed, open it
            AppLogger.Debug($"DetachedWindowManager: Opening floating window '{id}'");

            state.Title = title;
            state.DrawContent = drawContent;
            state.IsOpen = true;
            state.PositionSet = false; // Reset position flag to center on first open

            AppLogger.Info($"DetachedWindowManager: Opened floating window '{id}'");
        }
    }

    /// <summary>
    /// Check if a window is currently open
    /// </summary>
    public static bool IsOpen(string id)
    {
        if (!_windows.ContainsKey(id))
            return false;

        return _windows[id].IsOpen;
    }

    /// <summary>
    /// Draw all open detached windows. Call this in the main render loop.
    /// </summary>
    public static void DrawAll()
    {
        foreach (var kvp in _windows)
        {
            var state = kvp.Value;

            if (state.IsOpen)
            {
                DrawFloatingWindow(kvp.Key, state);
            }
        }
    }

    private static void DrawFloatingWindow(string id, DetachedWindowState state)
    {
        // Set initial position and size on first draw
        if (!state.PositionSet)
        {
            // Center the window on first open
            var viewport = ImGui.GetMainViewport();
            state.Position = new Vector2(
                viewport.Pos.X + (viewport.Size.X - state.DefaultSize.X) * 0.5f,
                viewport.Pos.Y + (viewport.Size.Y - state.DefaultSize.Y) * 0.5f
            );

            ImGui.SetNextWindowPos(state.Position, ImGuiCond.FirstUseEver);
            ImGui.SetNextWindowSize(state.DefaultSize, ImGuiCond.FirstUseEver);
            state.PositionSet = true;
        }

        // Window flags for floating behavior
        ImGuiWindowFlags flags = ImGuiWindowFlags.None;

        // Draw the window
        bool isOpen = state.IsOpen;
        if (ImGui.Begin(state.Title, ref isOpen, flags))
        {
            // Draw the content
            state.DrawContent?.Invoke();

            ImGui.End();
        }

        // Update open state if user closed the window
        if (!isOpen)
        {
            AppLogger.Debug($"DetachedWindowManager: User closed floating window '{id}'");
            state.IsOpen = false;
        }
    }

    /// <summary>
    /// Close all detached windows
    /// </summary>
    public static void CloseAll()
    {
        AppLogger.Debug("DetachedWindowManager: Closing all floating windows");

        foreach (var state in _windows.Values)
        {
            state.IsOpen = false;
        }
    }
}
