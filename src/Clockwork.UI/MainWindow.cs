using Clockwork.Core;
using Clockwork.UI.Views;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace Clockwork.UI;

/// <summary>
/// Main application window using OpenTK and ImGui.
/// </summary>
public class MainWindow : GameWindow
{
    private ImGuiController? _imguiController;
    private ApplicationContext _appContext;

    // Views
    private readonly AboutView _aboutView;
    private readonly RomLoaderView _romLoaderView;
    private readonly HeaderEditorView _headerEditorView;
    private readonly MapEditorView _mapEditorView;
    private readonly TextEditorWindow _textEditorWindow;

    public MainWindow(ApplicationContext appContext, GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
        : base(gameWindowSettings, nativeWindowSettings)
    {
        _appContext = appContext;

        // Initialize views
        _aboutView = new AboutView(_appContext);
        _romLoaderView = new RomLoaderView(_appContext);
        _headerEditorView = new HeaderEditorView(_appContext);
        _mapEditorView = new MapEditorView(_appContext);
        _textEditorWindow = new TextEditorWindow(_appContext);
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        Title = "Clockwork - Pokémon ROM Editor";

        _imguiController = new ImGuiController(ClientSize.X, ClientSize.Y);

        // Configure ImGui style
        ConfigureImGuiStyle();

        Console.WriteLine("Application started successfully!");
    }

    private void ConfigureImGuiStyle()
    {
        var style = ImGui.GetStyle();

        // Rounding
        style.WindowRounding = 6.0f;
        style.FrameRounding = 3.0f;
        style.GrabRounding = 3.0f;
        style.TabRounding = 3.0f;

        // Spacing
        style.WindowPadding = new System.Numerics.Vector2(10, 10);
        style.FramePadding = new System.Numerics.Vector2(8, 4);
        style.ItemSpacing = new System.Numerics.Vector2(8, 4);

        // Colors (modern dark theme)
        var colors = style.Colors;
        colors[(int)ImGuiCol.WindowBg] = new System.Numerics.Vector4(0.11f, 0.11f, 0.11f, 0.94f);
        colors[(int)ImGuiCol.ChildBg] = new System.Numerics.Vector4(0.15f, 0.15f, 0.15f, 1.00f);
        colors[(int)ImGuiCol.PopupBg] = new System.Numerics.Vector4(0.11f, 0.11f, 0.11f, 0.94f);
        colors[(int)ImGuiCol.Border] = new System.Numerics.Vector4(0.25f, 0.25f, 0.27f, 0.50f);
        colors[(int)ImGuiCol.FrameBg] = new System.Numerics.Vector4(0.20f, 0.21f, 0.22f, 1.00f);
        colors[(int)ImGuiCol.FrameBgHovered] = new System.Numerics.Vector4(0.30f, 0.31f, 0.32f, 1.00f);
        colors[(int)ImGuiCol.FrameBgActive] = new System.Numerics.Vector4(0.25f, 0.26f, 0.27f, 1.00f);
        colors[(int)ImGuiCol.TitleBg] = new System.Numerics.Vector4(0.08f, 0.08f, 0.09f, 1.00f);
        colors[(int)ImGuiCol.TitleBgActive] = new System.Numerics.Vector4(0.15f, 0.15f, 0.16f, 1.00f);
        colors[(int)ImGuiCol.MenuBarBg] = new System.Numerics.Vector4(0.15f, 0.15f, 0.16f, 1.00f);
        colors[(int)ImGuiCol.CheckMark] = new System.Numerics.Vector4(0.40f, 0.70f, 1.00f, 1.00f);
        colors[(int)ImGuiCol.SliderGrab] = new System.Numerics.Vector4(0.40f, 0.70f, 1.00f, 1.00f);
        colors[(int)ImGuiCol.SliderGrabActive] = new System.Numerics.Vector4(0.50f, 0.80f, 1.00f, 1.00f);
        colors[(int)ImGuiCol.Button] = new System.Numerics.Vector4(0.25f, 0.26f, 0.27f, 1.00f);
        colors[(int)ImGuiCol.ButtonHovered] = new System.Numerics.Vector4(0.35f, 0.36f, 0.37f, 1.00f);
        colors[(int)ImGuiCol.ButtonActive] = new System.Numerics.Vector4(0.40f, 0.70f, 1.00f, 1.00f);
        colors[(int)ImGuiCol.Header] = new System.Numerics.Vector4(0.25f, 0.26f, 0.27f, 1.00f);
        colors[(int)ImGuiCol.HeaderHovered] = new System.Numerics.Vector4(0.35f, 0.36f, 0.37f, 1.00f);
        colors[(int)ImGuiCol.HeaderActive] = new System.Numerics.Vector4(0.40f, 0.70f, 1.00f, 1.00f);
        colors[(int)ImGuiCol.Tab] = new System.Numerics.Vector4(0.15f, 0.15f, 0.16f, 1.00f);
        colors[(int)ImGuiCol.TabHovered] = new System.Numerics.Vector4(0.40f, 0.70f, 1.00f, 1.00f);
        colors[(int)ImGuiCol.TabActive] = new System.Numerics.Vector4(0.30f, 0.50f, 0.80f, 1.00f);
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);

        GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
        _imguiController?.WindowResized(ClientSize.X, ClientSize.Y);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        _imguiController?.Update(this, args.Time);

        // Update application context
        _appContext.Update(args.Time);

        // Close if requested
        if (!_appContext.IsRunning)
        {
            Close();
        }
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        // Clear screen
        GL.ClearColor(new Color4(0.1f, 0.1f, 0.1f, 1.0f));
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // Draw ImGui UI
        DrawUI();

        // Render ImGui
        _imguiController?.Render();

        SwapBuffers();
    }

    private void DrawUI()
    {
        // Calculate sidebar width
        float sidebarWidth = _isSidebarCollapsed ? 50 : 250;

        // Create DockSpace that starts after the sidebar
        var viewport = ImGui.GetMainViewport();
        float menuBarHeight = ImGui.GetFrameHeight();

        // DockSpace size (offset to leave space for sidebar)
        var dockspaceSize = new System.Numerics.Vector2(viewport.WorkSize.X - sidebarWidth, viewport.WorkSize.Y);

        ImGui.SetNextWindowPos(viewport.WorkPos);
        ImGui.SetNextWindowSize(viewport.WorkSize);
        ImGui.SetNextWindowViewport(viewport.ID);

        ImGuiWindowFlags windowFlags = ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoDocking;
        windowFlags |= ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove;
        windowFlags |= ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus;

        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0.0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0.0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(0.0f, 0.0f));

        ImGui.Begin("DockSpaceWindow", windowFlags);
        ImGui.PopStyleVar(3);

        // DockSpace (offset to leave space for sidebar)
        ImGuiIOPtr io = ImGui.GetIO();
        if ((io.ConfigFlags & ImGuiConfigFlags.DockingEnable) != 0)
        {
            // Position cursor for DockSpace
            ImGui.SetCursorPos(new System.Numerics.Vector2(sidebarWidth, menuBarHeight));

            uint dockspaceId = ImGui.GetID("MainDockSpace");
            ImGui.DockSpace(dockspaceId, new System.Numerics.Vector2(dockspaceSize.X, dockspaceSize.Y - menuBarHeight), ImGuiDockNodeFlags.None);
        }

        // Main menu
        if (ImGui.BeginMenuBar())
        {
            if (ImGui.BeginMenu("File"))
            {
                if (ImGui.MenuItem("Quit", "Alt+F4"))
                {
                    _appContext.IsRunning = false;
                }
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("ROM"))
            {
                if (ImGui.MenuItem("Open ROM...", "Ctrl+O"))
                {
                    _romLoaderView.IsVisible = true;
                }
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Editors"))
            {
                if (ImGui.MenuItem("Header Editor"))
                {
                    _headerEditorView.IsVisible = true;
                }
                if (ImGui.MenuItem("Map Editor"))
                {
                    _mapEditorView.IsVisible = true;
                }
                if (ImGui.MenuItem("Text Editor"))
                {
                    _textEditorWindow.IsVisible = true;
                }
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Help"))
            {
                if (ImGui.MenuItem("About"))
                {
                    _aboutView.IsVisible = true;
                }
                if (ImGui.MenuItem("ImGui Metrics"))
                {
                    _showMetricsWindow = !_showMetricsWindow;
                }
                ImGui.EndMenu();
            }

            ImGui.EndMenuBar();
        }

        ImGui.End();

        // Sidebar menu
        DrawSidebar();

        // Draw all views
        _aboutView.Draw();
        _romLoaderView.Draw();
        _headerEditorView.Draw();
        _mapEditorView.Draw();
        _textEditorWindow.Draw();

        if (_showMetricsWindow)
        {
            ImGui.ShowMetricsWindow(ref _showMetricsWindow);
        }
    }

    private void DrawSidebar()
    {
        // Calculate sidebar position and size
        float sidebarWidth = _isSidebarCollapsed ? 50 : 250;
        var viewport = ImGui.GetMainViewport();
        float menuBarHeight = ImGui.GetFrameHeight();

        // Position sidebar on the left, below menu
        ImGui.SetNextWindowPos(new System.Numerics.Vector2(viewport.WorkPos.X, viewport.WorkPos.Y + menuBarHeight));
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(sidebarWidth, viewport.WorkSize.Y - menuBarHeight));

        // Fixed window that cannot be moved or resized
        ImGuiWindowFlags sidebarFlags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking;

        ImGui.Begin("Navigation", sidebarFlags);

        // Toggle collapse button
        if (ImGui.Button(_isSidebarCollapsed ? "»" : "«", new System.Numerics.Vector2(-1, 30)))
        {
            _isSidebarCollapsed = !_isSidebarCollapsed;
        }

        if (!_isSidebarCollapsed)
        {
            ImGui.Spacing();
            ImGui.TextColored(new System.Numerics.Vector4(0.4f, 0.7f, 1.0f, 1.0f), "NAVIGATION");
            ImGui.Separator();
            ImGui.Spacing();

            // Editors section
            if (ImGui.CollapsingHeader("Editors", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGui.Selectable("  📝 Header Editor", _headerEditorView.IsVisible))
                {
                    _headerEditorView.IsVisible = !_headerEditorView.IsVisible;
                }
                if (ImGui.Selectable("  🗺️  Map Editor", _mapEditorView.IsVisible))
                {
                    _mapEditorView.IsVisible = !_mapEditorView.IsVisible;
                }
                if (ImGui.Selectable("  📄 Text Editor", _textEditorWindow.IsVisible))
                {
                    _textEditorWindow.IsVisible = !_textEditorWindow.IsVisible;
                }
            }
        }

        ImGui.End();
    }

    // Sidebar state and metrics
    private bool _isSidebarCollapsed = false;
    private bool _showMetricsWindow = false;

    protected override void OnUnload()
    {
        base.OnUnload();

        _imguiController?.Dispose();
        _appContext.Shutdown();
    }
}
