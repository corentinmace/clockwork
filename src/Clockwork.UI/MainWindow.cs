using Clockwork.Core;
using Clockwork.Core.Logging;
using Clockwork.Core.Services;
using Clockwork.Core.Settings;
using Clockwork.UI.Themes;
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
    private readonly ScriptEditorWindow _scriptEditorWindow;
    private readonly LogViewerWindow _logViewerWindow;
    private readonly SettingsWindow _settingsWindow;
    private readonly ThemeEditorView _themeEditorView;

    public MainWindow(ApplicationContext appContext, GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
        : base(gameWindowSettings, nativeWindowSettings)
    {
        _appContext = appContext;

        // Initialize logger
        AppLogger.Initialize();
        AppLogger.Info("Clockwork application starting...");

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

        // Connect theme editor to settings window
        _settingsWindow.SetThemeEditorView(_themeEditorView);
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        AppLogger.Info("MainWindow OnLoad: Initializing OpenGL and ImGui");

        Title = "Clockwork - Pokémon ROM Editor";

        _imguiController = new ImGuiController(ClientSize.X, ClientSize.Y);
        AppLogger.Debug("ImGuiController created");

        // Initialize theme manager
        ThemeManager.Initialize();
        AppLogger.Debug("ThemeManager initialized");

        // Initialize theme editor view
        _themeEditorView.Initialize(_appContext);

        // Apply theme from settings
        string themeName = SettingsManager.Settings.CurrentThemeName;
        ThemeManager.ApplyTheme(themeName);
        AppLogger.Info($"Applied theme: {themeName}");

        // Restore sidebar state from settings
        _isSidebarCollapsed = SettingsManager.Settings.SidebarCollapsed;
        AppLogger.Debug($"Sidebar collapsed state restored: {_isSidebarCollapsed}");

        AppLogger.Info("MainWindow loaded successfully");
        Console.WriteLine("Application started successfully!");
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);

        GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
        _imguiController?.WindowResized(ClientSize.X, ClientSize.Y);
    }

    protected override void OnTextInput(TextInputEventArgs e)
    {
        base.OnTextInput(e);

        _imguiController?.PressChar((char)e.Unicode);
    }

    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);

        _imguiController?.MouseScroll(e.OffsetX, e.OffsetY);
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

            if (ImGui.BeginMenu("View"))
            {
                if (ImGui.MenuItem("Log Viewer"))
                {
                    _logViewerWindow.IsVisible = true;
                }
                if (ImGui.MenuItem("Theme Editor"))
                {
                    _themeEditorView.IsVisible = true;
                }
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Settings"))
            {
                if (ImGui.MenuItem("Preferences..."))
                {
                    _settingsWindow.IsVisible = true;
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
        _scriptEditorWindow.Draw();
        _logViewerWindow.Draw();
        _settingsWindow.Draw();
        _themeEditorView.Draw();

        // Draw dialogs
        DrawSaveRomDialog();

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
                if (ImGui.Selectable("  📜 Script Editor", _scriptEditorWindow.IsVisible))
                {
                    _scriptEditorWindow.IsVisible = !_scriptEditorWindow.IsVisible;
                }
            }
        }

        ImGui.End();
    }

    // Sidebar state and metrics
    private bool _isSidebarCollapsed = false;
    private bool _showMetricsWindow = false;

    // ROM Save state
    private bool _isShowingSaveRomDialog = false;
    private string _saveRomLog = "";
    private bool _isSavingRom = false;

    private void SaveRomDialog()
    {
        var romService = _appContext.GetService<RomService>();
        var dialogService = _appContext.GetService<DialogService>();

        if (romService?.CurrentRom?.IsLoaded != true)
        {
            AppLogger.Warn("Save ROM requested but no ROM is loaded");
            // TODO: Show error dialog
            Console.WriteLine("No ROM loaded");
            return;
        }

        AppLogger.Debug("Opening save file dialog");

        // Open save file dialog
        string? savePath = dialogService?.SaveFileDialog(
            "NDS ROM Files|*.nds|All Files|*.*",
            "Save ROM As",
            "output.nds"
        );

        if (string.IsNullOrEmpty(savePath))
        {
            AppLogger.Debug("Save ROM cancelled by user");
            return; // User cancelled
        }

        AppLogger.Info($"User requested ROM save to: {savePath}");

        _saveRomLog = "";
        _isSavingRom = true;
        _isShowingSaveRomDialog = true;

        // Save ROM in background
        Task.Run(() =>
        {
            var ndsToolService = _appContext.GetService<NdsToolService>();
            bool success = ndsToolService?.PackRom(
                romService.CurrentRom.RomPath,
                savePath,
                (msg) => { _saveRomLog += msg + "\n"; }
            ) ?? false;

            _isSavingRom = false;

            if (success)
            {
                AppLogger.Info("ROM packing completed successfully via UI");
                _saveRomLog += "\n=== ROM saved successfully! ===\n";
            }
            else
            {
                AppLogger.Error("ROM packing failed via UI");
                _saveRomLog += "\n=== ROM save failed! ===\n";
            }
        });
    }

    private void DrawSaveRomDialog()
    {
        if (!_isShowingSaveRomDialog)
            return;

        ImGui.SetNextWindowSize(new System.Numerics.Vector2(600, 400), ImGuiCond.FirstUseEver);

        bool isOpen = _isShowingSaveRomDialog;
        if (ImGui.Begin("Saving ROM", ref isOpen, ImGuiWindowFlags.NoCollapse))
        {
            ImGui.TextColored(new System.Numerics.Vector4(0.4f, 0.7f, 1.0f, 1.0f), "ROM Packing Progress");
            ImGui.Separator();
            ImGui.Spacing();

            if (_isSavingRom)
            {
                ImGui.Text("Packing ROM, please wait...");
                ImGui.Spacing();
            }

            // Display logs
            ImGui.BeginChild("SaveRomLogs", new System.Numerics.Vector2(0, -40), ImGuiChildFlags.Border);
            ImGui.TextWrapped(_saveRomLog);

            // Auto-scroll to bottom
            if (ImGui.GetScrollY() >= ImGui.GetScrollMaxY())
                ImGui.SetScrollHereY(1.0f);

            ImGui.EndChild();

            ImGui.Spacing();

            if (!_isSavingRom && ImGui.Button("Close", new System.Numerics.Vector2(-1, 30)))
            {
                _isShowingSaveRomDialog = false;
            }

            if (_isSavingRom)
            {
                ImGui.BeginDisabled();
                ImGui.Button("Close", new System.Numerics.Vector2(-1, 30));
                ImGui.EndDisabled();
            }
        }
        ImGui.End();

        _isShowingSaveRomDialog = isOpen;
    }

    protected override void OnUnload()
    {
        base.OnUnload();

        AppLogger.Info("MainWindow OnUnload: Cleaning up resources");

        // Save window state to settings
        SettingsManager.Settings.WindowWidth = ClientSize.X;
        SettingsManager.Settings.WindowHeight = ClientSize.Y;
        SettingsManager.Settings.WindowMaximized = WindowState == WindowState.Maximized;
        SettingsManager.Settings.SidebarCollapsed = _isSidebarCollapsed;
        AppLogger.Debug($"Window state saved: {ClientSize.X}x{ClientSize.Y}, Maximized: {WindowState == WindowState.Maximized}, Sidebar: {_isSidebarCollapsed}");

        _imguiController?.Dispose();
        AppLogger.Debug("ImGuiController disposed");

        _appContext.Shutdown();
        AppLogger.Info("MainWindow unloaded");
    }
}
