using ClickableTransparentOverlay;
using Clockwork.Core;
using Clockwork.Core.Logging;
using Clockwork.Core.Services;
using Clockwork.Core.Settings;
using Clockwork.UI.Themes;
using Clockwork.UI.Views;
using ImGuiNET;

namespace Clockwork.UI;

/// <summary>
/// Main application overlay using ClickableTransparentOverlay and ImGui with multi-viewport support.
/// </summary>
public class MainOverlay : Overlay
{
    private ApplicationContext _appContext;

    // Views
    private readonly AboutView _aboutView;
    private readonly RomLoaderView _romLoaderView;
    private readonly HeaderEditorView _headerEditorView;
    private readonly MapEditorView _mapEditorView;
    private readonly TextEditorWindow _textEditorWindow;
    private readonly ScriptEditorWindow _scriptEditorWindow;
    private readonly LogViewerWindow _logViewerWindow;
    private readonly SettingsWindow _settingsWindow;
    private readonly ThemeEditorView _themeEditorView;
    private readonly MatrixEditorView _matrixEditorView;

    // Sidebar state and metrics
    private bool _isSidebarCollapsed = false;
    private bool _showMetricsWindow = false;

    // ROM Save state
    private bool _isShowingSaveRomDialog = false;
    private string _saveRomLog = "";

    private double _deltaTime = 1.0 / 60.0;

    public MainOverlay(ApplicationContext appContext, int width, int height)
        : base("Clockwork - Pokémon ROM Editor", width, height)
    {
        _appContext = appContext;

        // Initialize views
        _aboutView = new AboutView(_appContext);
        _romLoaderView = new RomLoaderView(_appContext);
        _headerEditorView = new HeaderEditorView(_appContext);
        _mapEditorView = new MapEditorView(_appContext);
        _textEditorWindow = new TextEditorWindow(_appContext);
        _scriptEditorWindow = new ScriptEditorWindow(_appContext);
        _logViewerWindow = new LogViewerWindow(_appContext);
        _settingsWindow = new SettingsWindow(_appContext);
        _themeEditorView = new ThemeEditorView();
        _matrixEditorView = new MatrixEditorView();

        // Connect theme editor to settings window
        _settingsWindow.SetThemeEditorView(_themeEditorView);

        AppLogger.Info("MainOverlay created");
    }

    protected override async Task PostInitialized()
    {
        await base.PostInitialized();

        // Configure ImGui after overlay and ImGui context are fully initialized
        ConfigureImGui();
    }

    private void ConfigureImGui()
    {
        var io = ImGui.GetIO();

        // Enable docking
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;

        // Enable multi-viewport / platform windows
        io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;

        // Enable keyboard navigation
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;

        // Configure style
        ConfigureImGuiStyle();

        AppLogger.Info("ImGui configured with docking and multi-viewport enabled");
    }

    private void ConfigureImGuiStyle()
    {
        var style = ImGui.GetStyle();

        // Colors - Modern Dark Theme
        var colors = style.Colors;
        colors[(int)ImGuiCol.Text] = new System.Numerics.Vector4(1.00f, 1.00f, 1.00f, 1.00f);
        colors[(int)ImGuiCol.TextDisabled] = new System.Numerics.Vector4(0.50f, 0.50f, 0.50f, 1.00f);
        colors[(int)ImGuiCol.WindowBg] = new System.Numerics.Vector4(0.10f, 0.10f, 0.10f, 1.00f);
        colors[(int)ImGuiCol.ChildBg] = new System.Numerics.Vector4(0.00f, 0.00f, 0.00f, 0.00f);
        colors[(int)ImGuiCol.PopupBg] = new System.Numerics.Vector4(0.19f, 0.19f, 0.19f, 0.92f);
        colors[(int)ImGuiCol.Border] = new System.Numerics.Vector4(0.19f, 0.19f, 0.19f, 0.29f);
        colors[(int)ImGuiCol.BorderShadow] = new System.Numerics.Vector4(0.00f, 0.00f, 0.00f, 0.24f);
        colors[(int)ImGuiCol.FrameBg] = new System.Numerics.Vector4(0.05f, 0.05f, 0.05f, 0.54f);
        colors[(int)ImGuiCol.FrameBgHovered] = new System.Numerics.Vector4(0.19f, 0.19f, 0.19f, 0.54f);
        colors[(int)ImGuiCol.FrameBgActive] = new System.Numerics.Vector4(0.20f, 0.22f, 0.23f, 1.00f);
        colors[(int)ImGuiCol.TitleBg] = new System.Numerics.Vector4(0.00f, 0.00f, 0.00f, 1.00f);
        colors[(int)ImGuiCol.TitleBgActive] = new System.Numerics.Vector4(0.06f, 0.06f, 0.06f, 1.00f);
        colors[(int)ImGuiCol.TitleBgCollapsed] = new System.Numerics.Vector4(0.00f, 0.00f, 0.00f, 1.00f);
        colors[(int)ImGuiCol.MenuBarBg] = new System.Numerics.Vector4(0.14f, 0.14f, 0.14f, 1.00f);
        colors[(int)ImGuiCol.ScrollbarBg] = new System.Numerics.Vector4(0.05f, 0.05f, 0.05f, 0.54f);
        colors[(int)ImGuiCol.ScrollbarGrab] = new System.Numerics.Vector4(0.34f, 0.34f, 0.34f, 0.54f);
        colors[(int)ImGuiCol.ScrollbarGrabHovered] = new System.Numerics.Vector4(0.40f, 0.40f, 0.40f, 0.54f);
        colors[(int)ImGuiCol.ScrollbarGrabActive] = new System.Numerics.Vector4(0.56f, 0.56f, 0.56f, 0.54f);
        colors[(int)ImGuiCol.CheckMark] = new System.Numerics.Vector4(0.33f, 0.67f, 0.86f, 1.00f);
        colors[(int)ImGuiCol.SliderGrab] = new System.Numerics.Vector4(0.34f, 0.34f, 0.34f, 0.54f);
        colors[(int)ImGuiCol.SliderGrabActive] = new System.Numerics.Vector4(0.56f, 0.56f, 0.56f, 0.54f);
        colors[(int)ImGuiCol.Button] = new System.Numerics.Vector4(0.05f, 0.05f, 0.05f, 0.54f);
        colors[(int)ImGuiCol.ButtonHovered] = new System.Numerics.Vector4(0.19f, 0.19f, 0.19f, 0.54f);
        colors[(int)ImGuiCol.ButtonActive] = new System.Numerics.Vector4(0.20f, 0.22f, 0.23f, 1.00f);
        colors[(int)ImGuiCol.Header] = new System.Numerics.Vector4(0.00f, 0.00f, 0.00f, 0.52f);
        colors[(int)ImGuiCol.HeaderHovered] = new System.Numerics.Vector4(0.00f, 0.00f, 0.00f, 0.36f);
        colors[(int)ImGuiCol.HeaderActive] = new System.Numerics.Vector4(0.20f, 0.22f, 0.23f, 0.33f);
        colors[(int)ImGuiCol.Separator] = new System.Numerics.Vector4(0.28f, 0.28f, 0.28f, 0.29f);
        colors[(int)ImGuiCol.SeparatorHovered] = new System.Numerics.Vector4(0.44f, 0.44f, 0.44f, 0.29f);
        colors[(int)ImGuiCol.SeparatorActive] = new System.Numerics.Vector4(0.40f, 0.44f, 0.47f, 1.00f);
        colors[(int)ImGuiCol.ResizeGrip] = new System.Numerics.Vector4(0.28f, 0.28f, 0.28f, 0.29f);
        colors[(int)ImGuiCol.ResizeGripHovered] = new System.Numerics.Vector4(0.44f, 0.44f, 0.44f, 0.29f);
        colors[(int)ImGuiCol.ResizeGripActive] = new System.Numerics.Vector4(0.40f, 0.44f, 0.47f, 1.00f);
        colors[(int)ImGuiCol.Tab] = new System.Numerics.Vector4(0.00f, 0.00f, 0.00f, 0.52f);
        colors[(int)ImGuiCol.TabHovered] = new System.Numerics.Vector4(0.14f, 0.14f, 0.14f, 1.00f);
        colors[(int)ImGuiCol.TabSelected] = new System.Numerics.Vector4(0.20f, 0.20f, 0.20f, 0.36f);
        colors[(int)ImGuiCol.TabDimmed] = new System.Numerics.Vector4(0.00f, 0.00f, 0.00f, 0.52f);
        colors[(int)ImGuiCol.TabDimmedSelected] = new System.Numerics.Vector4(0.14f, 0.14f, 0.14f, 1.00f);
        colors[(int)ImGuiCol.DockingPreview] = new System.Numerics.Vector4(0.33f, 0.67f, 0.86f, 1.00f);
        colors[(int)ImGuiCol.DockingEmptyBg] = new System.Numerics.Vector4(0.10f, 0.10f, 0.10f, 1.00f);
        colors[(int)ImGuiCol.PlotLines] = new System.Numerics.Vector4(1.00f, 0.00f, 0.00f, 1.00f);
        colors[(int)ImGuiCol.PlotLinesHovered] = new System.Numerics.Vector4(1.00f, 0.00f, 0.00f, 1.00f);
        colors[(int)ImGuiCol.PlotHistogram] = new System.Numerics.Vector4(1.00f, 0.00f, 0.00f, 1.00f);
        colors[(int)ImGuiCol.PlotHistogramHovered] = new System.Numerics.Vector4(1.00f, 0.00f, 0.00f, 1.00f);
        colors[(int)ImGuiCol.TableHeaderBg] = new System.Numerics.Vector4(0.00f, 0.00f, 0.00f, 0.52f);
        colors[(int)ImGuiCol.TableBorderStrong] = new System.Numerics.Vector4(0.00f, 0.00f, 0.00f, 0.52f);
        colors[(int)ImGuiCol.TableBorderLight] = new System.Numerics.Vector4(0.28f, 0.28f, 0.28f, 0.29f);
        colors[(int)ImGuiCol.TableRowBg] = new System.Numerics.Vector4(0.00f, 0.00f, 0.00f, 0.00f);
        colors[(int)ImGuiCol.TableRowBgAlt] = new System.Numerics.Vector4(1.00f, 1.00f, 1.00f, 0.06f);
        colors[(int)ImGuiCol.TextSelectedBg] = new System.Numerics.Vector4(0.20f, 0.22f, 0.23f, 1.00f);
        colors[(int)ImGuiCol.DragDropTarget] = new System.Numerics.Vector4(0.33f, 0.67f, 0.86f, 1.00f);
        colors[(int)ImGuiCol.NavCursor] = new System.Numerics.Vector4(1.00f, 0.00f, 0.00f, 1.00f);
        colors[(int)ImGuiCol.NavWindowingHighlight] = new System.Numerics.Vector4(1.00f, 0.00f, 0.00f, 0.70f);
        colors[(int)ImGuiCol.NavWindowingDimBg] = new System.Numerics.Vector4(1.00f, 0.00f, 0.00f, 0.20f);
        colors[(int)ImGuiCol.ModalWindowDimBg] = new System.Numerics.Vector4(0.10f, 0.10f, 0.10f, 0.70f);

        // Style parameters
        style.WindowPadding = new System.Numerics.Vector2(8.00f, 8.00f);
        style.FramePadding = new System.Numerics.Vector2(5.00f, 2.00f);
        style.CellPadding = new System.Numerics.Vector2(6.00f, 6.00f);
        style.ItemSpacing = new System.Numerics.Vector2(6.00f, 6.00f);
        style.ItemInnerSpacing = new System.Numerics.Vector2(6.00f, 6.00f);
        style.TouchExtraPadding = new System.Numerics.Vector2(0.00f, 0.00f);
        style.IndentSpacing = 25;
        style.ScrollbarSize = 15;
        style.GrabMinSize = 10;
        style.WindowBorderSize = 1;
        style.ChildBorderSize = 1;
        style.PopupBorderSize = 1;
        style.FrameBorderSize = 1;
        style.TabBorderSize = 1;
        style.WindowRounding = 7;
        style.ChildRounding = 4;
        style.FrameRounding = 3;
        style.PopupRounding = 4;
        style.ScrollbarRounding = 9;
        style.GrabRounding = 3;
        style.LogSliderDeadzone = 4;
        style.TabRounding = 4;

        AppLogger.Debug("ImGui style configured (dark theme)");
    }

    protected override void Render()
    {
        // Update application context
        _appContext.Update(_deltaTime);

        // Draw main UI
        DrawUI();

        // Draw views
        _aboutView.Draw();
        _romLoaderView.Draw();
        _headerEditorView.Draw();
        _mapEditorView.Draw();
        _textEditorWindow.Draw();
        _scriptEditorWindow.Draw();
        _logViewerWindow.Draw();
        _settingsWindow.Draw();
        _themeEditorView.Draw();
        _matrixEditorView.Draw();

        // ImGui metrics/debug window
        if (_showMetricsWindow)
        {
            ImGui.ShowMetricsWindow(ref _showMetricsWindow);
        }

        // Handle save ROM dialog
        if (_isShowingSaveRomDialog)
        {
            DrawSaveRomDialog();
        }
    }

    private void HandleKeyboardShortcuts()
    {
        var io = ImGui.GetIO();

        // Ctrl+O: Open ROM
        if (io.KeyCtrl && ImGui.IsKeyPressed(ImGuiKey.O))
        {
            _romLoaderView.IsVisible = true;
        }

        // Ctrl+S: Save ROM
        if (io.KeyCtrl && ImGui.IsKeyPressed(ImGuiKey.S))
        {
            SaveRomDialog();
        }
    }

    private void DrawUI()
    {
        // Handle keyboard shortcuts
        HandleKeyboardShortcuts();

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
        DrawMenuBar();

        // Sidebar
        DrawSidebar(sidebarWidth, menuBarHeight, viewport.WorkSize.Y);

        ImGui.End();
    }

    private void DrawMenuBar()
    {
        if (ImGui.BeginMenuBar())
        {
            if (ImGui.BeginMenu("File"))
            {
                if (ImGui.MenuItem("Quit", "Alt+F4"))
                {
                    this.Close();
                }
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("ROM"))
            {
                if (ImGui.MenuItem("Open ROM...", "Ctrl+O"))
                {
                    _romLoaderView.IsVisible = true;
                }

                if (ImGui.MenuItem("Save ROM...", "Ctrl+S"))
                {
                    SaveRomDialog();
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

                if (ImGui.MenuItem("Matrix Editor"))
                {
                    _matrixEditorView.IsVisible = true;
                }

                if (ImGui.MenuItem("Text Editor"))
                {
                    _textEditorWindow.IsVisible = true;
                }

                if (ImGui.MenuItem("Script Editor"))
                {
                    _scriptEditorWindow.IsVisible = true;
                }

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Tools"))
            {
                if (ImGui.MenuItem("Settings"))
                {
                    _settingsWindow.IsVisible = true;
                }

                if (ImGui.MenuItem("Log Viewer"))
                {
                    _logViewerWindow.IsVisible = true;
                }

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Help"))
            {
                if (ImGui.MenuItem("ImGui Metrics"))
                {
                    _showMetricsWindow = !_showMetricsWindow;
                }

                if (ImGui.MenuItem("About"))
                {
                    _aboutView.IsVisible = true;
                }

                ImGui.EndMenu();
            }

            ImGui.EndMenuBar();
        }
    }

    private void DrawSidebar(float width, float offsetY, float height)
    {
        var viewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(new System.Numerics.Vector2(viewport.WorkPos.X, viewport.WorkPos.Y + offsetY));
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(width, height - offsetY));

        ImGuiWindowFlags sidebarFlags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse;

        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0.0f);
        ImGui.Begin("Sidebar", sidebarFlags);
        ImGui.PopStyleVar();

        if (_isSidebarCollapsed)
        {
            // Collapsed sidebar: show only icons
            if (ImGui.Button(">>", new System.Numerics.Vector2(width - 16, 30)))
            {
                _isSidebarCollapsed = false;
            }
        }
        else
        {
            // Full sidebar
            if (ImGui.Button("<<", new System.Numerics.Vector2(width - 16, 30)))
            {
                _isSidebarCollapsed = true;
            }

            ImGui.Separator();
            ImGui.Spacing();

            // Navigation buttons
            if (ImGui.Button("ROM Loader", new System.Numerics.Vector2(width - 16, 0)))
            {
                _romLoaderView.IsVisible = true;
            }

            if (ImGui.Button("Header Editor", new System.Numerics.Vector2(width - 16, 0)))
            {
                _headerEditorView.IsVisible = true;
            }

            if (ImGui.Button("Map Editor", new System.Numerics.Vector2(width - 16, 0)))
            {
                _mapEditorView.IsVisible = true;
            }

            if (ImGui.Button("Matrix Editor", new System.Numerics.Vector2(width - 16, 0)))
            {
                _matrixEditorView.IsVisible = true;
            }

            if (ImGui.Button("Text Editor", new System.Numerics.Vector2(width - 16, 0)))
            {
                _textEditorWindow.IsVisible = true;
            }

            if (ImGui.Button("Script Editor", new System.Numerics.Vector2(width - 16, 0)))
            {
                _scriptEditorWindow.IsVisible = true;
            }

            ImGui.Separator();

            if (ImGui.Button("Settings", new System.Numerics.Vector2(width - 16, 0)))
            {
                _settingsWindow.IsVisible = true;
            }

            if (ImGui.Button("Log Viewer", new System.Numerics.Vector2(width - 16, 0)))
            {
                _logViewerWindow.IsVisible = true;
            }

            if (ImGui.Button("About", new System.Numerics.Vector2(width - 16, 0)))
            {
                _aboutView.IsVisible = true;
            }
        }

        ImGui.End();
    }

    private void SaveRomDialog()
    {
        _isShowingSaveRomDialog = true;
        _saveRomLog = "";
    }

    private void DrawSaveRomDialog()
    {
        ImGui.OpenPopup("Save ROM");

        var viewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(viewport.GetCenter(), ImGuiCond.Appearing, new System.Numerics.Vector2(0.5f, 0.5f));
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(500, 300));

        if (ImGui.BeginPopupModal("Save ROM", ref _isShowingSaveRomDialog, ImGuiWindowFlags.NoResize))
        {
            ImGui.Text("Save ROM functionality is not yet implemented.");
            ImGui.Separator();

            ImGui.TextWrapped(_saveRomLog);

            if (ImGui.Button("Close", new System.Numerics.Vector2(120, 0)))
            {
                _isShowingSaveRomDialog = false;
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }
    }

    public new void Run()
    {
        AppLogger.Info("Starting MainOverlay");
        base.Run();
    }
}
