using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Clockwork.UI;

/// <summary>
/// Manages detached windows that are moved to separate OS viewports.
/// When toggled, windows are moved outside the main window as independent OS windows.
/// </summary>
public static class DetachedWindowManager
{
    private static readonly Dictionary<string, DetachedWindowState> _windows = new();

    private class DetachedWindowState
    {
        public bool IsOpen { get; set; }
        public Vector2 DefaultSize { get; set; }
        public bool HasBeenPositioned { get; set; }
    }

    public static void RegisterWindow(string id, Vector2 defaultSize)
    {
        if (!_windows.ContainsKey(id))
        {
            _windows[id] = new DetachedWindowState
            {
                IsOpen = false,
                DefaultSize = defaultSize,
                HasBeenPositioned = false
            };
        }
    }

    public static void Toggle(string id)
    {
        if (_windows.ContainsKey(id))
        {
            var state = _windows[id];
            state.IsOpen = !state.IsOpen;

            // Reset positioning flag when toggling
            if (state.IsOpen)
            {
                state.HasBeenPositioned = false;
            }
        }
    }

    public static bool IsOpen(string id)
    {
        return _windows.ContainsKey(id) && _windows[id].IsOpen;
    }

    public static void DrawWindow(string id, string title, Action drawContent)
    {
        if (!_windows.ContainsKey(id) || !_windows[id].IsOpen)
            return;

        var state = _windows[id];

        // Set window size on first use
        ImGui.SetNextWindowSize(state.DefaultSize, ImGuiCond.FirstUseEver);

        // Position the window outside the main viewport on first open
        // This encourages ImGui to create a new OS viewport for it
        if (!state.HasBeenPositioned)
        {
            var mainViewport = ImGui.GetMainViewport();
            // Position it to the right of the main window
            Vector2 detachedPos = new Vector2(
                mainViewport.Pos.X + mainViewport.Size.X + 20,
                mainViewport.Pos.Y + 50
            );
            ImGui.SetNextWindowPos(detachedPos, ImGuiCond.Always);
            state.HasBeenPositioned = true;
        }

        // Create a non-dockable window that will be in its own viewport
        // NoDocking flag prevents it from being docked back into the main window
        bool isOpen = state.IsOpen;
        ImGuiWindowFlags flags = ImGuiWindowFlags.NoDocking;

        if (ImGui.Begin(title, ref isOpen, flags))
        {
            drawContent?.Invoke();
            ImGui.End();
        }

        state.IsOpen = isOpen;
    }

    public static void CloseAll()
    {
        foreach (var window in _windows.Values)
        {
            window.IsOpen = false;
            window.HasBeenPositioned = false;
        }
    }
}
