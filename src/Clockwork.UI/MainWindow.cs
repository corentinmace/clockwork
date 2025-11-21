using Clockwork.Core;
using Clockwork.Core.Logging;
using Clockwork.Core.Services;
using Clockwork.Core.Settings;
using Clockwork.UI.Graphics;
using Clockwork.UI.Icons;
using Clockwork.UI.Themes;
using Clockwork.UI.Views;
using ImGuiNET;
using Veldrid;
using Veldrid.Sdl2;

namespace Clockwork.UI;

/// <summary>
/// Main application window using Veldrid and ImGui.
/// </summary>
public class MainWindow
{
    private readonly Sdl2Window _window;
    private readonly GraphicsDevice _graphicsDevice;
    private ImGuiRenderer? _imguiRenderer;
    private TextureManager? _textureManager;
    private ApplicationContext _appContext;
    private CommandList? _commandList;

    // Views
    private readonly AboutView _aboutView;
    private readonly RomLoaderView _romLoaderView;
    private readonly HeaderEditorView _headerEditorView;
    private readonly MapEditorView _mapEditorView;
    private readonly TextEditorWindow _textEditorWindow;
    private readonly LevelScriptEditorView _levelScriptEditorView;
    private readonly LogViewerWindow _logViewerWindow;
    private readonly SettingsWindow _settingsWindow;
    private readonly ThemeEditorView _themeEditorView;
    private readonly MatrixEditorView _matrixEditorView;
    private readonly WildEditorView _wildEditorView;
    private readonly NsbtxEditorView _nsbtxEditorView;

    // Tools
    private readonly AddressHelperWindow _addressHelperWindow;
    private readonly ScrcmdTableHelperWindow _scrcmdTableHelperWindow;
    private readonly ScriptCommandDatabaseView _scriptCommandDatabaseView;

    // Sidebar state and metrics
    private bool _isSidebarCollapsed = false;
    private bool _showMetricsWindow = false;

    // Status bar
    private const float STATUS_BAR_HEIGHT = 30.0f;

    // ROM Save state
    private bool _isShowingSaveRomDialog = false;
    private string _saveRomLog = "";
    private bool _isSavingRom = false;

    public MainWindow(ApplicationContext appContext, Sdl2Window window, GraphicsDevice graphicsDevice)
    {
        _appContext = appContext;
        _window = window;
        _graphicsDevice = graphicsDevice;

        // Initialize views
        _aboutView = new AboutView(_appContext);
        _romLoaderView = new RomLoaderView(_appContext);
        _headerEditorView = new HeaderEditorView(_appContext);
        _mapEditorView = new MapEditorView(_appContext);
        _textEditorWindow = new TextEditorWindow(_appContext);
        _levelScriptEditorView = new LevelScriptEditorView(_appContext);
        _logViewerWindow = new LogViewerWindow(_appContext);
        _settingsWindow = new SettingsWindow(_appContext);
        _themeEditorView = new ThemeEditorView();
        _matrixEditorView = new MatrixEditorView();
        _wildEditorView = new WildEditorView(_appContext);
        _nsbtxEditorView = new NsbtxEditorView(_appContext);
        _nsbtxEditorView.Initialize();

        // Initialize tools
        _addressHelperWindow = new AddressHelperWindow(_appContext);
        _scrcmdTableHelperWindow = new ScrcmdTableHelperWindow(_appContext);
        _scriptCommandDatabaseView = new ScriptCommandDatabaseView();
        _scriptCommandDatabaseView.Initialize(_appContext);

        // Connect theme editor to settings window
        _settingsWindow.SetThemeEditorView(_themeEditorView);

        // Connect editors to header editor for navigation
        _headerEditorView.SetEditorReferences(_textEditorWindow, _levelScriptEditorView, _matrixEditorView, _wildEditorView);

        // Connect tools (ScrcmdTableHelper -> AddressHelper integration)
        _scrcmdTableHelperWindow.SetAddressHelperWindow(_addressHelperWindow);

        // Set up event handlers
        _window.Resized += OnResize;
        _window.Closed += OnClosing;

        // Initialize
        OnLoad();
    }

    public void Run()
    {
        AppLogger.Debug("Starting main render loop");

        // Main render loop
        while (_window.Exists)
        {
            var snapshot = _window.PumpEvents();
            if (!_window.Exists)
                break;

            double deltaTime = 1.0 / 60.0; // Approximate deltatime (can be improved with actual timing)

            // Update (pass snapshot to avoid calling PumpEvents twice)
            OnUpdate(deltaTime, snapshot);

            // Render
            OnRender(deltaTime);
        }
    }

    private void OnLoad()
    {
        AppLogger.Info("MainWindow OnLoad: Initializing Veldrid and ImGui");

        // Create command list for submitting rendering commands
        _commandList = _graphicsDevice.ResourceFactory.CreateCommandList();
        AppLogger.Debug("CommandList created");

        // Initialize ImGui renderer
        _imguiRenderer = new ImGuiRenderer(
            _graphicsDevice,
            _graphicsDevice.MainSwapchain.Framebuffer.OutputDescription,
            (int)_window.Width,
            (int)_window.Height);
        AppLogger.Debug("ImGuiRenderer created");

        // Initialize texture manager
        _textureManager = new TextureManager(_graphicsDevice, _imguiRenderer);
        AppLogger.Debug("TextureManager created");

        // Set texture manager for header editor tooltips
        _headerEditorView.SetTextureManager(_textureManager);
        AppLogger.Debug("TextureManager set for HeaderEditorView");

        // Configure ImGui
        var io = ImGui.GetIO();
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;

        // Initialize theme manager
        ThemeManager.Initialize();
        AppLogger.Debug("ThemeManager initialized");

        // Initialize theme editor view
        _themeEditorView.Initialize(_appContext);

        // Initialize matrix editor view
        _matrixEditorView.Initialize(_appContext);

        // Initialize wild editor view
        _wildEditorView.Initialize(_appContext);

        // Initialize level script editor view
        _levelScriptEditorView.Initialize(_appContext);

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

    private void OnResize()
    {
        _graphicsDevice.MainSwapchain.Resize((uint)_window.Width, (uint)_window.Height);
        _imguiRenderer?.WindowResized((int)_window.Width, (int)_window.Height);
    }

    private void OnUpdate(double deltaTime, InputSnapshot snapshot)
    {
        // Update ImGui input (for main window)
        _imguiRenderer?.Update((float)deltaTime, snapshot);

        // Update animated textures (GIFs)
        _textureManager?.Update(deltaTime);

        // Update application context
        _appContext.Update(deltaTime);

        // Close if requested
        if (!_appContext.IsRunning)
        {
            _window.Close();
        }
    }

    private void OnRender(double deltaTime)
    {
        if (_commandList == null || _imguiRenderer == null)
            return;

        // Begin command recording
        _commandList.Begin();

        // Clear screen
        _commandList.SetFramebuffer(_graphicsDevice.MainSwapchain.Framebuffer);
        _commandList.ClearColorTarget(0, new RgbaFloat(0.1f, 0.1f, 0.1f, 1.0f));

        // Draw ImGui UI
        DrawUI();

        // Render ImGui
        _imguiRenderer.Render(_graphicsDevice, _commandList);

        // End command recording and submit
        _commandList.End();
        _graphicsDevice.SubmitCommands(_commandList);

        // Swap buffers
        _graphicsDevice.SwapBuffers(_graphicsDevice.MainSwapchain);
    }

    private void OnClosing()
    {
        AppLogger.Info("MainWindow OnClosing: Cleaning up resources");

        // Save window state to settings
        SettingsManager.Settings.WindowWidth = (int)_window.Width;
        SettingsManager.Settings.WindowHeight = (int)_window.Height;
        SettingsManager.Settings.WindowMaximized = _window.WindowState == WindowState.Maximized;
        SettingsManager.Settings.SidebarCollapsed = _isSidebarCollapsed;
        SettingsManager.Save();
        AppLogger.Debug($"Window state saved: {_window.Width}x{_window.Height}, Maximized: {_window.WindowState == WindowState.Maximized}, Sidebar: {_isSidebarCollapsed}");

        _textureManager?.Dispose();
        AppLogger.Debug("TextureManager disposed");

        _imguiRenderer?.Dispose();
        AppLogger.Debug("ImGuiRenderer disposed");

        _commandList?.Dispose();
        AppLogger.Debug("CommandList disposed");

        _appContext.Shutdown();
        AppLogger.Info("MainWindow closed");
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

        // DockSpace size (offset to leave space for sidebar and status bar)
        var dockspaceSize = new System.Numerics.Vector2(viewport.WorkSize.X - sidebarWidth, viewport.WorkSize.Y - STATUS_BAR_HEIGHT);

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
            ImGui.DockSpace(dockspaceId, new System.Numerics.Vector2(dockspaceSize.X, dockspaceSize.Y - menuBarHeight - STATUS_BAR_HEIGHT), ImGuiDockNodeFlags.None);
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
                if (ImGui.MenuItem("Matrix Editor"))
                {
                    _matrixEditorView.IsVisible = true;
                }
                if (ImGui.MenuItem("Text Editor"))
                {
                    _textEditorWindow.IsVisible = true;
                }
                if (ImGui.MenuItem("NSBTX Editor"))
                {
                    _nsbtxEditorView.IsVisible = true;
                }
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Tools"))
            {
                if (ImGui.MenuItem("Address Helper"))
                {
                    _addressHelperWindow.IsVisible = true;
                }
                if (ImGui.MenuItem("Script Command Table"))
                {
                    _scrcmdTableHelperWindow.IsVisible = true;
                }
                if (ImGui.MenuItem("Script Command Database"))
                {
                    _scriptCommandDatabaseView.IsVisible = true;
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

        // Status bar
        DrawStatusBar();

        // Draw all views
        _aboutView.Draw();
        _romLoaderView.Draw();
        _headerEditorView.Draw();
        _mapEditorView.Draw();
        _matrixEditorView.Draw();
        _wildEditorView.Draw();
        _textEditorWindow.Draw();
        _levelScriptEditorView.Draw();
        _nsbtxEditorView.Draw();
        _logViewerWindow.Draw();
        _settingsWindow.Draw();
        _themeEditorView.Draw();

        // Draw tools
        _addressHelperWindow.Draw();
        _scrcmdTableHelperWindow.Draw();
        _scriptCommandDatabaseView.Draw();

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

        // Position sidebar on the left, below menu, above status bar
        ImGui.SetNextWindowPos(new System.Numerics.Vector2(viewport.WorkPos.X, viewport.WorkPos.Y + menuBarHeight));
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(sidebarWidth, viewport.WorkSize.Y - menuBarHeight - STATUS_BAR_HEIGHT));

        // Fixed window that cannot be moved or resized
        ImGuiWindowFlags sidebarFlags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking;

        ImGui.Begin("Navigation", sidebarFlags);

        // Toggle collapse button
        if (ImGui.Button(_isSidebarCollapsed ? FontAwesomeIcons.ArrowRight : FontAwesomeIcons.ArrowLeft, new System.Numerics.Vector2(-1, 40)))
        {
            _isSidebarCollapsed = !_isSidebarCollapsed;
        }

        if (_isSidebarCollapsed)
        {
            // Collapsed mode: Show only icons as buttons
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            _headerEditorView.IsVisible = DrawSidebarIconButton(FontAwesomeIcons.BookOpenReader, "Header Editor", _headerEditorView.IsVisible);
            _mapEditorView.IsVisible = DrawSidebarIconButton(FontAwesomeIcons.Map, "Map Editor", _mapEditorView.IsVisible);
            _matrixEditorView.IsVisible = DrawSidebarIconButton(FontAwesomeIcons.Grid, "Matrix Editor", _matrixEditorView.IsVisible);
            _wildEditorView.IsVisible = DrawSidebarIconButton(FontAwesomeIcons.Paw, "Wild Editor", _wildEditorView.IsVisible);
            _textEditorWindow.IsVisible = DrawSidebarIconButton(FontAwesomeIcons.Font, "Text Editor", _textEditorWindow.IsVisible);
            _levelScriptEditorView.IsVisible = DrawSidebarIconButton(FontAwesomeIcons.Database, "Level Script Editor", _levelScriptEditorView.IsVisible);
            _nsbtxEditorView.IsVisible = DrawSidebarIconButton(FontAwesomeIcons.Image, "NSBTX Editor", _nsbtxEditorView.IsVisible);

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            _addressHelperWindow.IsVisible = DrawSidebarIconButton(FontAwesomeIcons.Calculator, "Address Helper", _addressHelperWindow.IsVisible);
            _scrcmdTableHelperWindow.IsVisible = DrawSidebarIconButton(FontAwesomeIcons.Terminal, "Script Cmd Helper", _scrcmdTableHelperWindow.IsVisible);
        }
        else
        {
            // Expanded mode: Show icons with labels
            ImGui.Spacing();
            ImGui.TextColored(new System.Numerics.Vector4(0.4f, 0.7f, 1.0f, 1.0f), "NAVIGATION");
            ImGui.Separator();
            ImGui.Spacing();

            // Editors section
            if (ImGui.CollapsingHeader("Editors", ImGuiTreeNodeFlags.DefaultOpen))
            {
                _headerEditorView.IsVisible = DrawSidebarItem(FontAwesomeIcons.BookOpenReader, "Header Editor", _headerEditorView.IsVisible);
                _mapEditorView.IsVisible = DrawSidebarItem(FontAwesomeIcons.Map, "Map Editor", _mapEditorView.IsVisible);
                _matrixEditorView.IsVisible = DrawSidebarItem(FontAwesomeIcons.Grid, "Matrix Editor", _matrixEditorView.IsVisible);
                _wildEditorView.IsVisible = DrawSidebarItem(FontAwesomeIcons.Paw, "Wild Editor", _wildEditorView.IsVisible);
                _textEditorWindow.IsVisible = DrawSidebarItem(FontAwesomeIcons.Font, "Text Editor", _textEditorWindow.IsVisible);
                _levelScriptEditorView.IsVisible = DrawSidebarItem(FontAwesomeIcons.Database, "Level Script Editor", _levelScriptEditorView.IsVisible);
                _nsbtxEditorView.IsVisible = DrawSidebarItem(FontAwesomeIcons.Image, "NSBTX Editor", _nsbtxEditorView.IsVisible);
            }

            ImGui.Spacing();

            // Tools section
            if (ImGui.CollapsingHeader("Tools"))
            {
                _addressHelperWindow.IsVisible = DrawSidebarItem(FontAwesomeIcons.Calculator, "Address Helper", _addressHelperWindow.IsVisible);
                _scrcmdTableHelperWindow.IsVisible = DrawSidebarItem(FontAwesomeIcons.Terminal, "Script Cmd Helper", _scrcmdTableHelperWindow.IsVisible);
            }
        }

        ImGui.End();
    }

    /// <summary>
    /// Draw a sidebar item with icon and label (expanded mode)
    /// </summary>
    private bool DrawSidebarItem(string icon, string label, bool isVisible)
    {
        string displayText = $"{icon}  {label}";
        if (ImGui.Selectable(displayText, isVisible))
        {
            return !isVisible;
        }
        return isVisible;
    }

    /// <summary>
    /// Draw a sidebar icon button with tooltip (collapsed mode)
    /// </summary>
    private bool DrawSidebarIconButton(string icon, string tooltip, bool isVisible)
    {
        var buttonColor = isVisible
            ? new System.Numerics.Vector4(0.3f, 0.6f, 0.9f, 1.0f)
            : new System.Numerics.Vector4(0.2f, 0.2f, 0.2f, 0.5f);

        ImGui.PushStyleColor(ImGuiCol.Button, buttonColor);

        bool clicked = ImGui.Button(icon, new System.Numerics.Vector2(-1, 40));

        ImGui.PopStyleColor();

        // Show tooltip on hover
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.Text(tooltip);
            ImGui.EndTooltip();
        }

        if (clicked)
        {
            return !isVisible;
        }
        return isVisible;
    }

    /// <summary>
    /// Draw the status bar at the bottom of the window
    /// </summary>
    private void DrawStatusBar()
    {
        var viewport = ImGui.GetMainViewport();
        float menuBarHeight = ImGui.GetFrameHeight();

        // Position status bar at the bottom of the window
        float statusBarY = viewport.WorkPos.Y + viewport.WorkSize.Y - STATUS_BAR_HEIGHT;
        ImGui.SetNextWindowPos(new System.Numerics.Vector2(viewport.WorkPos.X, statusBarY));
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(viewport.WorkSize.X, STATUS_BAR_HEIGHT));

        // Fixed window that cannot be moved or resized
        ImGuiWindowFlags statusBarFlags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove |
                                          ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking | ImGuiWindowFlags.NoScrollbar;

        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(8.0f, 4.0f));
        ImGui.Begin("StatusBar", statusBarFlags);
        ImGui.PopStyleVar();

        // Get ROM service
        var romService = _appContext.GetService<RomService>();
        bool isRomLoaded = romService?.CurrentRom?.IsLoaded == true;

        // ROM status
        if (isRomLoaded)
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(0.4f, 0.8f, 0.4f, 1.0f)); // Green
            ImGui.Text($"{FontAwesomeIcons.CheckCircle} ROM Loaded");
            ImGui.PopStyleColor();

            // Show ROM info on hover
            if (ImGui.IsItemHovered())
            {
                var romInfo = romService?.CurrentRom;
                ImGui.BeginTooltip();
                ImGui.Text($"Game: {romInfo?.GameCode}");
                ImGui.Text($"Version: {romInfo?.Version}");
                ImGui.Text($"Path: {romInfo?.RomPath}");
                ImGui.EndTooltip();
            }
        }
        else
        {
            ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(0.8f, 0.4f, 0.4f, 1.0f)); // Red
            ImGui.Text($"{FontAwesomeIcons.TimesCircle} No ROM Loaded");
            ImGui.PopStyleColor();
        }

        // Error message (if any)
        var lastError = AppLogger.GetLastError();
        if (lastError != null)
        {
            ImGui.SameLine();
            ImGui.Spacing();
            ImGui.SameLine();
            ImGui.Text("|");
            ImGui.SameLine();

            ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(1.0f, 0.3f, 0.3f, 1.0f)); // Bright red
            string errorIcon = lastError.Level == LogLevel.Fatal ? FontAwesomeIcons.ExclamationTriangle : FontAwesomeIcons.TimesCircle;
            ImGui.Text($"{errorIcon} {lastError.Message}");
            ImGui.PopStyleColor();

            // Show full error details on hover
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text($"Time: {lastError.Timestamp:HH:mm:ss}");
                ImGui.Text($"Level: {lastError.Level}");
                ImGui.Separator();
                ImGui.TextWrapped(lastError.Message);
                ImGui.Spacing();
                ImGui.TextDisabled("Click to open Log Viewer");
                ImGui.EndTooltip();
            }

            // Click to open log viewer
            if (ImGui.IsItemClicked())
            {
                _logViewerWindow.IsVisible = true;
            }
        }

        // Log viewer button (right side)
        ImGui.SameLine(viewport.WorkSize.X - 120);
        if (ImGui.Button($"{FontAwesomeIcons.FileLines} Log Viewer"))
        {
            _logViewerWindow.IsVisible = !_logViewerWindow.IsVisible;
        }

        ImGui.End();
    }

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
            var romPackingService = _appContext.GetService<RomPackingService>();
            var textArchiveService = _appContext.GetService<TextArchiveService>();

            // Step 0: Rebuild text archives from expanded/ folder
            _saveRomLog += "=== Step 0: Repacking Expanded Files ===\n";
            bool textArchiveSuccess = textArchiveService?.BuildRequiredBins() ?? false;

            if (!textArchiveSuccess)
            {
                AppLogger.Error("Text archive rebuilding failed");
                _saveRomLog += "\n=== Text archive rebuilding failed! Save aborted. ===\n";
                _isSavingRom = false;
                return;
            }

            _saveRomLog += "Text archives rebuilt successfully.\n";
            _saveRomLog += "Note: Scripts are compiled individually when saved in editor.\n";

            // Step 1: Pack all NARC archives
            _saveRomLog += "\n=== Step 1: Packing NARC Archives ===\n";
            bool narcPackingSuccess = romPackingService?.PackAllNarcs(
                (msg) => { _saveRomLog += msg + "\n"; }
            ) ?? false;

            if (!narcPackingSuccess)
            {
                AppLogger.Error("NARC packing failed");
                _saveRomLog += "\n=== NARC packing failed! ===\n";
                _isSavingRom = false;
                return;
            }

            _saveRomLog += "\n=== Step 2: Creating ROM File ===\n";

            // Step 2: Pack ROM with ndstool
            bool success = ndsToolService?.PackRom(
                romService.CurrentRom.RomPath,
                savePath,
                (msg) => { _saveRomLog += msg + "\n"; }
            ) ?? false;

            _isSavingRom = false;

            if (success)
            {
                AppLogger.Info("ROM saved successfully via UI");
                _saveRomLog += "\n=== ROM saved successfully! ===\n";
            }
            else
            {
                AppLogger.Error("ROM creation failed via UI");
                _saveRomLog += "\n=== ROM creation failed! ===\n";
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
}
