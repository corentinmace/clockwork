using ImGuiNET;
using System;
using System.Numerics;
using Veldrid;
using Veldrid.Sdl2;

namespace Clockwork.UI;

/// <summary>
/// Represents a secondary viewport window for ImGui multi-viewport support.
/// Each viewport has its own SDL2 window, GraphicsDevice, and ImGuiRenderer.
/// </summary>
public unsafe class ViewportWindow : IDisposable
{
    public Sdl2Window Window { get; private set; }
    public GraphicsDevice GraphicsDevice { get; private set; }
    public ImGuiRenderer ImGuiRenderer { get; private set; }
    public CommandList CommandList { get; private set; }
    public uint ViewportID { get; private set; }

    private bool _disposed = false;
    private System.Runtime.InteropServices.GCHandle _gcHandle;

    public ViewportWindow(ImGuiViewportPtr viewport, GraphicsDevice mainGraphicsDevice)
    {
        ViewportID = viewport.ID;

        // Create SDL2 window with OpenGL support (same as main window)
        Window = new Sdl2Window(
            "Viewport",
            (int)viewport.Pos.X,
            (int)viewport.Pos.Y,
            (int)Math.Max(viewport.Size.X, 320),  // Minimum width
            (int)Math.Max(viewport.Size.Y, 240),  // Minimum height
            SDL_WindowFlags.OpenGL | SDL_WindowFlags.Shown | SDL_WindowFlags.Resizable,
            false);

        // Create graphics device for this window with OpenGL (same backend as main window)
        var options = new GraphicsDeviceOptions(
            debug: false,
            swapchainDepthFormat: null,
            syncToVerticalBlank: true,
            resourceBindingModel: ResourceBindingModel.Improved,
            preferDepthRangeZeroToOne: true,
            preferStandardClipSpaceYDirection: true);

        // Create OpenGL platform info from SDL2 window
        var platformInfo = new OpenGLPlatformInfo(
            openGLContextHandle: Window.OpenGLContextHandle,
            getProcAddress: Window.GetProcAddress,
            makeCurrent: Window.MakeCurrent,
            getCurrentContext: Window.GetCurrentContext,
            clearCurrentContext: Window.ClearCurrentContext,
            deleteContext: Window.DeleteContext,
            swapBuffers: Window.SwapBuffers,
            setSyncToVerticalBlank: Window.SetVSync);

        GraphicsDevice = GraphicsDevice.CreateOpenGL(
            options,
            platformInfo,
            (uint)Window.Width,
            (uint)Window.Height);

        // Create command list
        CommandList = GraphicsDevice.ResourceFactory.CreateCommandList();

        // Create ImGui renderer for this window
        ImGuiRenderer = new ImGuiRenderer(
            GraphicsDevice,
            GraphicsDevice.MainSwapchain.Framebuffer.OutputDescription,
            (int)Window.Width,
            (int)Window.Height);

        // Store GC handle to prevent garbage collection
        _gcHandle = System.Runtime.InteropServices.GCHandle.Alloc(this);
        unsafe
        {
            viewport.NativePtr->PlatformUserData = (IntPtr)_gcHandle;
        }

        // Set up window resize handler
        Window.Resized += OnWindowResized;
    }

    private void OnWindowResized()
    {
        GraphicsDevice.ResizeMainWindow((uint)Window.Width, (uint)Window.Height);
        ImGuiRenderer.WindowResized((int)Window.Width, (int)Window.Height);
    }

    public void Render(float deltaSeconds)
    {
        if (_disposed)
            return;

        // Process window events
        var snapshot = Window.PumpEvents();

        // Update ImGui input
        ImGuiRenderer.Update(deltaSeconds, snapshot);

        // Begin command recording
        CommandList.Begin();

        // Clear screen
        CommandList.SetFramebuffer(GraphicsDevice.MainSwapchain.Framebuffer);
        CommandList.ClearColorTarget(0, new RgbaFloat(0.1f, 0.1f, 0.1f, 1.0f));

        // Render ImGui for this viewport
        ImGuiRenderer.Render(GraphicsDevice, CommandList);

        // End command recording and submit
        CommandList.End();
        GraphicsDevice.SubmitCommands(CommandList);

        // Swap buffers
        GraphicsDevice.SwapBuffers(GraphicsDevice.MainSwapchain);
    }

    public void SetPosition(Vector2 pos)
    {
        Window.X = (int)pos.X;
        Window.Y = (int)pos.Y;
    }

    public void SetSize(Vector2 size)
    {
        Window.Width = (uint)size.X;
        Window.Height = (uint)size.Y;
        GraphicsDevice.ResizeMainWindow((uint)size.X, (uint)size.Y);
        ImGuiRenderer.WindowResized((int)size.X, (int)size.Y);
    }

    public void SetVisible(bool visible)
    {
        if (visible)
            Window.Visible = true;
        else
            Window.Visible = false;
    }

    public void SetFocus()
    {
        // SDL2 doesn't have a direct "SetFocus" but we can raise the window
        // This is platform-specific and might not work on all systems
    }

    public void SetTitle(string title)
    {
        Window.Title = title;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        // Release GCHandle
        if (_gcHandle.IsAllocated)
        {
            _gcHandle.Free();
        }

        Window.Resized -= OnWindowResized;

        CommandList?.Dispose();
        ImGuiRenderer?.Dispose();
        GraphicsDevice?.Dispose();
        Window?.Close();
    }
}
