using Clockwork.Core.Logging;
using ImGuiNET;
using System;
using System.Numerics;
using Veldrid;
using Veldrid.OpenGL;
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
        AppLogger.Debug($"ViewportWindow: Creating window for viewport {ViewportID}");

        // Create SDL2 window with OpenGL support (same as main window)
        Window = new Sdl2Window(
            "Viewport",
            (int)viewport.Pos.X,
            (int)viewport.Pos.Y,
            (int)Math.Max(viewport.Size.X, 320),  // Minimum width
            (int)Math.Max(viewport.Size.Y, 240),  // Minimum height
            SDL_WindowFlags.OpenGL | SDL_WindowFlags.Shown | SDL_WindowFlags.Resizable,
            false);
        AppLogger.Debug($"ViewportWindow: SDL2 window created for viewport {ViewportID}");

        // Get native SDL window handle
        IntPtr nativeWindow = Window.SdlWindowHandle;
        AppLogger.Debug($"ViewportWindow: Native SDL window handle: {nativeWindow}");

        // Create OpenGL context for this window
        IntPtr glContext = SDL2Native.SDL_GL_CreateContext(nativeWindow);
        if (glContext == IntPtr.Zero)
        {
            AppLogger.Error($"ViewportWindow: Failed to create OpenGL context for viewport {ViewportID}");
            throw new Exception("Failed to create OpenGL context for viewport window");
        }
        AppLogger.Debug($"ViewportWindow: OpenGL context created: {glContext}");

        // Create graphics device for this window with OpenGL (same backend as main window)
        var options = new GraphicsDeviceOptions(
            debug: false,
            swapchainDepthFormat: null,
            syncToVerticalBlank: true,
            resourceBindingModel: ResourceBindingModel.Improved,
            preferDepthRangeZeroToOne: true,
            preferStandardClipSpaceYDirection: true);

        // Create OpenGL platform info using SDL2Native P/Invoke functions
        var platformInfo = new OpenGLPlatformInfo(
            openGLContextHandle: glContext,
            getProcAddress: (name) => SDL2Native.SDL_GL_GetProcAddress(name),
            makeCurrent: (ctx) => SDL2Native.SDL_GL_MakeCurrent(nativeWindow, ctx),
            getCurrentContext: () => SDL2Native.SDL_GL_GetCurrentContext(),
            clearCurrentContext: () => SDL2Native.SDL_GL_MakeCurrent(IntPtr.Zero, IntPtr.Zero),
            deleteContext: (ctx) => SDL2Native.SDL_GL_DeleteContext(ctx),
            swapBuffers: () => SDL2Native.SDL_GL_SwapWindow(nativeWindow),
            setSyncToVerticalBlank: (enabled) => SDL2Native.SDL_GL_SetSwapInterval(enabled ? 1 : 0));

        GraphicsDevice = GraphicsDevice.CreateOpenGL(
            options,
            platformInfo,
            (uint)Window.Width,
            (uint)Window.Height);
        AppLogger.Debug($"ViewportWindow: GraphicsDevice created for viewport {ViewportID}");

        // Create command list
        CommandList = GraphicsDevice.ResourceFactory.CreateCommandList();
        AppLogger.Debug($"ViewportWindow: CommandList created for viewport {ViewportID}");

        // Create ImGui renderer for this window
        ImGuiRenderer = new ImGuiRenderer(
            GraphicsDevice,
            GraphicsDevice.MainSwapchain.Framebuffer.OutputDescription,
            (int)Window.Width,
            (int)Window.Height);
        AppLogger.Debug($"ViewportWindow: ImGuiRenderer created for viewport {ViewportID}");

        // Store GC handle to prevent garbage collection
        _gcHandle = System.Runtime.InteropServices.GCHandle.Alloc(this);
        unsafe
        {
            viewport.NativePtr->PlatformUserData = (IntPtr)_gcHandle;
        }

        // Set up window resize handler
        Window.Resized += OnWindowResized;

        AppLogger.Info($"ViewportWindow: Viewport window {ViewportID} fully initialized");
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

        AppLogger.Debug($"ViewportWindow: Disposing viewport window {ViewportID}");
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

        AppLogger.Info($"ViewportWindow: Viewport window {ViewportID} disposed");
    }
}
