using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Maths;

namespace Clockwork.UI;

/// <summary>
/// Represents a secondary viewport window for ImGui multi-viewport support
/// </summary>
internal class ImGuiViewportWindow : IDisposable
{
    private readonly IWindow _window;
    private GL? _gl;
    private bool _disposed;

    public IWindow Window => _window;
    public GL? GL => _gl;

    public ImGuiViewportWindow(Vector2D<int> size, string title)
    {
        var options = WindowOptions.Default;
        options.Size = size;
        options.Title = title;
        options.VSync = false; // Secondary windows don't need VSync
        options.ShouldSwapAutomatically = true;
        options.IsVisible = false; // Start hidden, will be shown by ImGui
        options.API = new GraphicsAPI(ContextAPI.OpenGL, new APIVersion(3, 3));

        _window = Window.Create(options);

        // Set up window events
        _window.Load += OnLoad;
    }

    private void OnLoad()
    {
        _gl = _window.CreateOpenGL();
    }

    public void Show()
    {
        if (!_window.IsInitialized)
            _window.Initialize();

        _window.IsVisible = true;
    }

    public void Hide()
    {
        _window.IsVisible = false;
    }

    public void SetPosition(Vector2D<int> position)
    {
        _window.Position = position;
    }

    public Vector2D<int> GetPosition()
    {
        return _window.Position;
    }

    public void SetSize(Vector2D<int> size)
    {
        _window.Size = size;
    }

    public Vector2D<int> GetSize()
    {
        return _window.Size;
    }

    public void SetTitle(string title)
    {
        _window.Title = title;
    }

    public void Focus()
    {
        // Silk.NET doesn't have a direct Focus() method
        // But we can try to bring to front
        _window.IsVisible = true;
    }

    public bool IsFocused()
    {
        // Approximate - check if window is active
        return _window.IsVisible && !_window.WindowState.HasFlag(WindowState.Minimized);
    }

    public bool IsMinimized()
    {
        return _window.WindowState.HasFlag(WindowState.Minimized);
    }

    public void DoEvents()
    {
        _window.DoEvents();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _gl?.Dispose();
            _window?.Dispose();
            _disposed = true;
        }
    }
}
