using Clockwork.Core;
using Clockwork.Core.Logging;
using Clockwork.Core.Settings;
using Clockwork.Core.Themes;
using ImGuiNET;
using System.Numerics;

namespace Clockwork.UI.Views;

/// <summary>
/// Window for editing application settings
/// </summary>
public class SettingsWindow : IView
{
    private readonly ApplicationContext _appContext;
    public bool IsVisible { get; set; } = false;

    private ClockworkSettings _settings;
    private ThemeEditorView? _themeEditorView;

    public SettingsWindow(ApplicationContext appContext)
    {
        _appContext = appContext;
        _settings = SettingsManager.Settings;
    }

    public void SetThemeEditorView(ThemeEditorView themeEditorView)
    {
        _themeEditorView = themeEditorView;
    }

    public void Draw()
    {
        if (!IsVisible)
            return;

        bool isVisible = IsVisible;
        ImGui.SetNextWindowSize(new Vector2(700, 600), ImGuiCond.FirstUseEver);

        if (ImGui.Begin("Settings", ref isVisible, ImGuiWindowFlags.NoCollapse))
        {
            DrawSettingsContent();
        }
        ImGui.End();

        IsVisible = isVisible;
    }

    private void DrawSettingsContent()
    {
        if (ImGui.BeginTabBar("SettingsTabs"))
        {
            if (ImGui.BeginTabItem("General"))
            {
                DrawGeneralSettings();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Editor"))
            {
                DrawEditorSettings();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Rendering"))
            {
                DrawRenderingSettings();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Paths"))
            {
                DrawPathsSettings();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Advanced"))
            {
                DrawAdvancedSettings();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        ImGui.Separator();
        ImGui.Spacing();

        // Save/Reset buttons
        if (ImGui.Button("Save Settings", new Vector2(150, 30)))
        {
            SaveSettings();
        }

        ImGui.SameLine();

        if (ImGui.Button("Reset to Defaults", new Vector2(150, 30)))
        {
            if (ImGui.IsItemClicked())
            {
                ResetToDefaults();
            }
        }

        ImGui.SameLine();
        ImGui.TextDisabled("(?)");
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Reset all settings to their default values");
        }
    }

    private void DrawGeneralSettings()
    {
        // Theme selection
        ImGui.TextColored(new Vector4(0.4f, 0.7f, 1.0f, 1.0f), "Theme");
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.Text("Current theme:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(200);
        if (ImGui.BeginCombo("##ThemeCombo", _settings.CurrentThemeName))
        {
            foreach (var theme in ThemeManager.AvailableThemes.Values)
            {
                bool isSelected = _settings.CurrentThemeName == theme.Name;
                if (ImGui.Selectable($"{theme.Name}{(theme.IsReadOnly ? " (prédéfini)" : "")}", isSelected))
                {
                    _settings.CurrentThemeName = theme.Name;
                    ThemeManager.ApplyTheme(theme);
                    AppLogger.Info($"Theme changed to: {theme.Name}");
                }

                if (isSelected)
                {
                    ImGui.SetItemDefaultFocus();
                }
            }

            ImGui.EndCombo();
        }

        ImGui.SameLine();
        if (ImGui.Button("Éditeur de thèmes..."))
        {
            if (_themeEditorView != null)
            {
                _themeEditorView.IsVisible = true;
            }
        }

        ImGui.Spacing();
        ImGui.Spacing();

        ImGui.TextColored(new Vector4(0.4f, 0.7f, 1.0f, 1.0f), "Application Behavior");
        ImGui.Separator();
        ImGui.Spacing();

        bool openLastRom = _settings.OpenLastRomOnStartup;
        if (ImGui.Checkbox("Open last ROM on startup", ref openLastRom))
        {
            _settings.OpenLastRomOnStartup = openLastRom;
        }

        bool confirmBeforeClosing = _settings.ConfirmBeforeClosing;
        if (ImGui.Checkbox("Confirm before closing with unsaved changes", ref confirmBeforeClosing))
        {
            _settings.ConfirmBeforeClosing = confirmBeforeClosing;
        }

        bool autoCheckUpdates = _settings.AutomaticallyCheckForUpdates;
        if (ImGui.Checkbox("Automatically check for updates", ref autoCheckUpdates))
        {
            _settings.AutomaticallyCheckForUpdates = autoCheckUpdates;
        }

        ImGui.Spacing();
        ImGui.Text("Auto-save interval (minutes, 0 = disabled):");
        int autoSaveInterval = _settings.AutoSaveIntervalMinutes;
        if (ImGui.SliderInt("##autosave", ref autoSaveInterval, 0, 30))
        {
            _settings.AutoSaveIntervalMinutes = autoSaveInterval;
        }

        if (_settings.AutoSaveIntervalMinutes > 0)
        {
            ImGui.SameLine();
            ImGui.TextDisabled($"(every {_settings.AutoSaveIntervalMinutes} min)");
        }
        else
        {
            ImGui.SameLine();
            ImGui.TextDisabled("(disabled)");
        }
    }

    private void DrawEditorSettings()
    {
        ImGui.TextColored(new Vector4(0.4f, 0.7f, 1.0f, 1.0f), "Text Editor");
        ImGui.Separator();
        ImGui.Spacing();

        bool preferHex = _settings.TextEditorPreferHex;
        if (ImGui.Checkbox("Prefer hexadecimal display", ref preferHex))
        {
            _settings.TextEditorPreferHex = preferHex;
        }

        ImGui.Spacing();
        ImGui.TextColored(new Vector4(0.4f, 0.7f, 1.0f, 1.0f), "Script Editor");
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.Text("Format preference:");
        int scriptFormat = _settings.ScriptEditorFormatPreference;
        if (ImGui.RadioButton("Plain text", ref scriptFormat, 0))
        {
            _settings.ScriptEditorFormatPreference = scriptFormat;
        }
        if (ImGui.RadioButton("Formatted", ref scriptFormat, 1))
        {
            _settings.ScriptEditorFormatPreference = scriptFormat;
        }

        ImGui.Spacing();
        ImGui.TextColored(new Vector4(0.4f, 0.7f, 1.0f, 1.0f), "Map Editor");
        ImGui.Separator();
        ImGui.Spacing();

        bool showGrid = _settings.MapEditorShowGrid;
        if (ImGui.Checkbox("Show grid", ref showGrid))
        {
            _settings.MapEditorShowGrid = showGrid;
        }

        ImGui.Text("Grid size:");
        int gridSize = _settings.MapEditorGridSize;
        if (ImGui.SliderInt("##gridsize", ref gridSize, 8, 32))
        {
            _settings.MapEditorGridSize = gridSize;
        }
    }

    private void DrawRenderingSettings()
    {
        ImGui.TextColored(new Vector4(0.4f, 0.7f, 1.0f, 1.0f), "Map Rendering");
        ImGui.Separator();
        ImGui.Spacing();

        bool renderBuildings = _settings.RenderBuildings;
        if (ImGui.Checkbox("Render buildings", ref renderBuildings))
        {
            _settings.RenderBuildings = renderBuildings;
        }

        bool renderCollision = _settings.RenderCollision;
        if (ImGui.Checkbox("Render collision overlay", ref renderCollision))
        {
            _settings.RenderCollision = renderCollision;
        }

        bool renderTerrain = _settings.RenderTerrain;
        if (ImGui.Checkbox("Render terrain overlay", ref renderTerrain))
        {
            _settings.RenderTerrain = renderTerrain;
        }

        ImGui.Spacing();
        ImGui.Text("Collision opacity:");
        float collisionOpacity = _settings.CollisionOpacity;
        if (ImGui.SliderFloat("##collisionopacity", ref collisionOpacity, 0.0f, 1.0f))
        {
            _settings.CollisionOpacity = collisionOpacity;
        }

        ImGui.Text("Terrain opacity:");
        float terrainOpacity = _settings.TerrainOpacity;
        if (ImGui.SliderFloat("##terrainopacity", ref terrainOpacity, 0.0f, 1.0f))
        {
            _settings.TerrainOpacity = terrainOpacity;
        }
    }

    private void DrawPathsSettings()
    {
        ImGui.TextColored(new Vector4(0.4f, 0.7f, 1.0f, 1.0f), "File Paths");
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.Text("Last ROM path:");
        ImGui.TextDisabled(_settings.LastRomPath);
        ImGui.Spacing();

        ImGui.Text("Export path:");
        string exportPath = _settings.ExportPath;
        ImGui.InputText("##exportpath", ref exportPath, 500);
        _settings.ExportPath = exportPath;
        ImGui.Spacing();

        ImGui.Text("ndstool.exe path:");
        string ndsToolPath = _settings.NdsToolPath;
        ImGui.InputText("##ndstoolpath", ref ndsToolPath, 500);
        _settings.NdsToolPath = ndsToolPath;
        ImGui.SameLine();
        if (ImGui.Button("Browse"))
        {
            // TODO: Open file dialog
        }
    }

    private void DrawAdvancedSettings()
    {
        ImGui.TextColored(new Vector4(0.4f, 0.7f, 1.0f, 1.0f), "Logging");
        ImGui.Separator();
        ImGui.Spacing();

        bool enableFileLogging = _settings.EnableFileLogging;
        if (ImGui.Checkbox("Enable logging to file", ref enableFileLogging))
        {
            _settings.EnableFileLogging = enableFileLogging;
        }

        ImGui.Text("Minimum log level:");
        string[] logLevels = { "Debug", "Info", "Warning", "Error", "Fatal" };
        int currentIndex = Array.IndexOf(logLevels, _settings.MinimumLogLevel);
        if (currentIndex < 0) currentIndex = 0;

        if (ImGui.Combo("##loglevel", ref currentIndex, logLevels, logLevels.Length))
        {
            _settings.MinimumLogLevel = logLevels[currentIndex];
        }

        ImGui.Text("Max log lines in memory:");
        int maxLogLines = _settings.MaxLogLines;
        if (ImGui.SliderInt("##maxloglines", ref maxLogLines, 100, 10000))
        {
            _settings.MaxLogLines = maxLogLines;
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.TextColored(new Vector4(0.4f, 0.7f, 1.0f, 1.0f), "Information");
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.Text($"Settings file: {SettingsManager.SettingsFilePath}");
        ImGui.SameLine();
        if (ImGui.Button("Open Folder"))
        {
            OpenSettingsFolder();
        }
    }

    private void SaveSettings()
    {
        SettingsManager.Save();
        AppLogger.Info("Settings saved from Settings window");

        // Visual feedback
        ImGui.OpenPopup("SettingsSaved");
    }

    private void ResetToDefaults()
    {
        SettingsManager.ResetToDefaults();
        _settings = SettingsManager.Settings;
        AppLogger.Info("Settings reset to defaults");
    }

    private void OpenSettingsFolder()
    {
        try
        {
            string folder = Path.GetDirectoryName(SettingsManager.SettingsFilePath) ?? string.Empty;
            if (Directory.Exists(folder))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = folder,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to open settings folder: {ex.Message}");
        }
    }
}
