using Clockwork.Core.Logging;
using ImGuiNET;
using System;
using System.Numerics;
using Veldrid;
using Veldrid.Sdl2;

namespace Clockwork.UI;

/// <summary>
/// Represents a completely independent window with its own SDL2 window,
/// GraphicsDevice, ImGuiRenderer, and ImGui context.
/// Used for truly detached editor windows.
/// </summary>
public class IndependentWindow : IDisposable
{
    private readonly Sdl2Window _window;
    private readonly GraphicsDevice _graphicsDevice;
    private readonly ImGuiRenderer _imguiRenderer;
    private readonly CommandList _commandList;
    private readonly IntPtr _imguiContext;
    private readonly Action _drawContent;
    private readonly string _title;

    private bool _disposed = false;
    private bool _isClosing = false;

    public bool IsOpen => _window.Exists && !_isClosing;

    public IndependentWindow(string title, Vector2 size, Vector2 position, Action drawContent)
    {
        _title = title;
        _drawContent = drawContent;

        AppLogger.Debug($"IndependentWindow: Creating window '{title}' at ({position.X}, {position.Y}) with size ({size.X}, {size.Y})");

        // Create SDL2 window
        _window = new Sdl2Window(
            title,
            (int)position.X,
            (int)position.Y,
            (int)size.X,
            (int)size.Y,
            SDL_WindowFlags.OpenGL | SDL_WindowFlags.Shown | SDL_WindowFlags.Resizable,
            false);

        // Create graphics device
        var options = new GraphicsDeviceOptions(
            debug: false,
            swapchainDepthFormat: null,
            syncToVerticalBlank: true,
            resourceBindingModel: ResourceBindingModel.Improved,
            preferDepthRangeZeroToOne: true,
            preferStandardClipSpaceYDirection: true);

        _graphicsDevice = GraphicsDevice.CreateOpenGL(
            options,
            _window.Width,
            _window.Height);

        AppLogger.Debug($"IndependentWindow: GraphicsDevice created for '{title}'");

        // Create command list
        _commandList = _graphicsDevice.ResourceFactory.CreateCommandList();

        // Create a NEW ImGui context for this window (isolated from main window)
        _imguiContext = ImGui.CreateContext();
        ImGui.SetCurrentContext(_imguiContext);

        AppLogger.Debug($"IndependentWindow: Created new ImGui context {_imguiContext} for '{title}'");

        // Configure ImGui for this context
        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;
        // No viewports needed for independent windows

        // Create ImGui renderer for this window
        _imguiRenderer = new ImGuiRenderer(
            _graphicsDevice,
            _graphicsDevice.MainSwapchain.Framebuffer.OutputDescription,
            (int)_window.Width,
            (int)_window.Height);

        AppLogger.Debug($"IndependentWindow: ImGuiRenderer created for '{title}'");

        // Apply same theme as main window
        Themes.ThemeManager.ApplyCurrentTheme();

        // Set up event handlers
        _window.Resized += OnWindowResized;
        _window.Closed += OnWindowClosed;

        AppLogger.Info($"IndependentWindow: Window '{title}' fully initialized");
    }

    private void OnWindowResized()
    {
        _graphicsDevice.ResizeMainWindow((uint)_window.Width, (uint)_window.Height);
        _imguiRenderer.WindowResized((int)_window.Width, (int)_window.Height);
    }

    private void OnWindowClosed()
    {
        AppLogger.Debug($"IndependentWindow: Window '{_title}' closed by user");
        _isClosing = true;
    }

    /// <summary>
    /// Update and render this window. Call this in the main render loop.
    /// </summary>
    public void Update(double deltaTime)
    {
        if (_disposed || !_window.Exists)
            return;

        // Switch to this window's ImGui context
        ImGui.SetCurrentContext(_imguiContext);

        // Process window events
        var snapshot = _window.PumpEvents();
        if (!_window.Exists)
        {
            _isClosing = true;
            return;
        }

        // Update ImGui input for this window
        _imguiRenderer.Update((float)deltaTime, snapshot);

        // Begin command recording
        _commandList.Begin();

        // Clear screen
        _commandList.SetFramebuffer(_graphicsDevice.MainSwapchain.Framebuffer);
        _commandList.ClearColorTarget(0, new RgbaFloat(0.1f, 0.1f, 0.1f, 1.0f));

        // Draw window content
        DrawContent();

        // Render ImGui
        _imguiRenderer.Render(_graphicsDevice, _commandList);

        // End command recording and submit
        _commandList.End();
        _graphicsDevice.SubmitCommands(_commandList);

        // Swap buffers
        _graphicsDevice.SwapBuffers(_graphicsDevice.MainSwapchain);
    }

    private void DrawContent()
    {
        // Create a fullscreen window for the content
        ImGui.SetNextWindowPos(Vector2.Zero);
        ImGui.SetNextWindowSize(new Vector2(_window.Width, _window.Height));

        ImGuiWindowFlags flags = ImGuiWindowFlags.NoTitleBar |
                                 ImGuiWindowFlags.NoResize |
                                 ImGuiWindowFlags.NoMove |
                                 ImGuiWindowFlags.NoCollapse |
                                 ImGuiWindowFlags.NoBringToFrontOnFocus;

        if (ImGui.Begin($"##Content_{_title}", flags))
        {
            _drawContent?.Invoke();
            ImGui.End();
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        AppLogger.Debug($"IndependentWindow: Disposing window '{_title}'");
        _disposed = true;

        // Unsubscribe from events
        _window.Resized -= OnWindowResized;
        _window.Closed -= OnWindowClosed;

        // Switch to this context before destroying
        ImGui.SetCurrentContext(_imguiContext);

        // Dispose resources
        _commandList?.Dispose();
        _imguiRenderer?.Dispose();
        _graphicsDevice?.Dispose();

        // Destroy ImGui context
        ImGui.DestroyContext(_imguiContext);

        _window?.Close();

        AppLogger.Info($"IndependentWindow: Window '{_title}' disposed");
    }
}
