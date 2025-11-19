using System.Text.Json;
using Clockwork.Core.Logging;
using Clockwork.Core.Themes;
using ImGuiNET;

namespace Clockwork.UI.Themes;

/// <summary>
/// Manages themes for the application (loading, saving, applying).
/// </summary>
public static class ThemeManager
{
    private static readonly string _themesDirectory;
    private static readonly Dictionary<string, Theme> _themes = new();
    private static Theme? _currentTheme;
    private static readonly object _fileLock = new object();

    static ThemeManager()
    {
        // Themes directory: AppData/Clockwork/Themes/
        string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        _themesDirectory = Path.Combine(appDataPath, "Clockwork", "Themes");
    }

    /// <summary>
    /// Gets all available themes (predefined + custom).
    /// </summary>
    public static IReadOnlyDictionary<string, Theme> AvailableThemes => _themes;

    /// <summary>
    /// Gets the currently active theme.
    /// </summary>
    public static Theme? CurrentTheme => _currentTheme;

    /// <summary>
    /// Initializes the theme manager and loads all themes.
    /// </summary>
    public static void Initialize()
    {
        AppLogger.Info("Initializing ThemeManager...");

        // Load predefined themes
        foreach (var theme in PredefinedThemes.GetAllPredefined())
        {
            _themes[theme.Name] = theme;
            AppLogger.Debug($"Loaded predefined theme: {theme.Name}");
        }

        // Load custom themes from disk
        LoadCustomThemes();

        AppLogger.Info($"ThemeManager initialized with {_themes.Count} themes");
    }

    /// <summary>
    /// Loads all custom themes from the themes directory.
    /// </summary>
    private static void LoadCustomThemes()
    {
        try
        {
            lock (_fileLock)
            {
                if (!Directory.Exists(_themesDirectory))
                {
                    Directory.CreateDirectory(_themesDirectory);
                    AppLogger.Debug($"Created themes directory: {_themesDirectory}");
                    return;
                }

                var themeFiles = Directory.GetFiles(_themesDirectory, "*.json");
                AppLogger.Debug($"Found {themeFiles.Length} custom theme files");

                foreach (var filePath in themeFiles)
                {
                    try
                    {
                        string json = File.ReadAllText(filePath);
                        var options = new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        };

                        var theme = JsonSerializer.Deserialize<Theme>(json, options);

                        if (theme != null)
                        {
                            // Ensure custom themes are not read-only
                            theme.IsReadOnly = false;

                            _themes[theme.Name] = theme;
                            AppLogger.Debug($"Loaded custom theme: {theme.Name} from {Path.GetFileName(filePath)}");
                        }
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Error($"Failed to load theme from {Path.GetFileName(filePath)}: {ex.Message}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to load custom themes: {ex.Message}");
        }
    }

    /// <summary>
    /// Saves a custom theme to disk.
    /// </summary>
    /// <param name="theme">The theme to save.</param>
    public static void SaveTheme(Theme theme)
    {
        if (theme.IsReadOnly)
        {
            AppLogger.Warn($"Cannot save read-only theme: {theme.Name}");
            return;
        }

        try
        {
            lock (_fileLock)
            {
                if (!Directory.Exists(_themesDirectory))
                {
                    Directory.CreateDirectory(_themesDirectory);
                }

                // Sanitize filename (remove invalid characters)
                string safeFileName = string.Join("_", theme.Name.Split(Path.GetInvalidFileNameChars()));
                string filePath = Path.Combine(_themesDirectory, $"{safeFileName}.json");

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                string json = JsonSerializer.Serialize(theme, options);
                File.WriteAllText(filePath, json);

                // Update in-memory collection
                _themes[theme.Name] = theme;

                AppLogger.Info($"Saved custom theme: {theme.Name} to {filePath}");
            }
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to save theme {theme.Name}: {ex.Message}");
            throw;
        }
    }

    /// <summary>
    /// Deletes a custom theme.
    /// </summary>
    /// <param name="themeName">Name of the theme to delete.</param>
    public static bool DeleteTheme(string themeName)
    {
        if (!_themes.TryGetValue(themeName, out var theme))
        {
            AppLogger.Warn($"Theme not found: {themeName}");
            return false;
        }

        if (theme.IsReadOnly)
        {
            AppLogger.Warn($"Cannot delete read-only theme: {themeName}");
            return false;
        }

        try
        {
            lock (_fileLock)
            {
                // Remove from disk
                string safeFileName = string.Join("_", themeName.Split(Path.GetInvalidFileNameChars()));
                string filePath = Path.Combine(_themesDirectory, $"{safeFileName}.json");

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                // Remove from memory
                _themes.Remove(themeName);

                AppLogger.Info($"Deleted custom theme: {themeName}");
                return true;
            }
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to delete theme {themeName}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Applies a theme to ImGui.
    /// </summary>
    /// <param name="themeName">Name of the theme to apply.</param>
    public static bool ApplyTheme(string themeName)
    {
        if (!_themes.TryGetValue(themeName, out var theme))
        {
            AppLogger.Warn($"Theme not found: {themeName}, falling back to Dark theme");
            theme = PredefinedThemes.Dark();
        }

        return ApplyTheme(theme);
    }

    /// <summary>
    /// Applies a theme to ImGui.
    /// </summary>
    /// <param name="theme">The theme to apply.</param>
    public static bool ApplyTheme(Theme theme)
    {
        try
        {
            var style = ImGui.GetStyle();

            // Apply style properties
            style.WindowRounding = theme.WindowRounding;
            style.FrameRounding = theme.FrameRounding;
            style.GrabRounding = theme.GrabRounding;
            style.TabRounding = theme.TabRounding;
            style.WindowPadding = theme.WindowPadding;
            style.FramePadding = theme.FramePadding;
            style.ItemSpacing = theme.ItemSpacing;

            // Apply all colors
            var colors = style.Colors;
            colors[(int)ImGuiCol.WindowBg] = theme.WindowBg;
            colors[(int)ImGuiCol.ChildBg] = theme.ChildBg;
            colors[(int)ImGuiCol.PopupBg] = theme.PopupBg;
            colors[(int)ImGuiCol.Border] = theme.Border;
            colors[(int)ImGuiCol.BorderShadow] = theme.BorderShadow;
            colors[(int)ImGuiCol.FrameBg] = theme.FrameBg;
            colors[(int)ImGuiCol.FrameBgHovered] = theme.FrameBgHovered;
            colors[(int)ImGuiCol.FrameBgActive] = theme.FrameBgActive;
            colors[(int)ImGuiCol.TitleBg] = theme.TitleBg;
            colors[(int)ImGuiCol.TitleBgActive] = theme.TitleBgActive;
            colors[(int)ImGuiCol.TitleBgCollapsed] = theme.TitleBgCollapsed;
            colors[(int)ImGuiCol.MenuBarBg] = theme.MenuBarBg;
            colors[(int)ImGuiCol.ScrollbarBg] = theme.ScrollbarBg;
            colors[(int)ImGuiCol.ScrollbarGrab] = theme.ScrollbarGrab;
            colors[(int)ImGuiCol.ScrollbarGrabHovered] = theme.ScrollbarGrabHovered;
            colors[(int)ImGuiCol.ScrollbarGrabActive] = theme.ScrollbarGrabActive;
            colors[(int)ImGuiCol.CheckMark] = theme.CheckMark;
            colors[(int)ImGuiCol.SliderGrab] = theme.SliderGrab;
            colors[(int)ImGuiCol.SliderGrabActive] = theme.SliderGrabActive;
            colors[(int)ImGuiCol.Button] = theme.Button;
            colors[(int)ImGuiCol.ButtonHovered] = theme.ButtonHovered;
            colors[(int)ImGuiCol.ButtonActive] = theme.ButtonActive;
            colors[(int)ImGuiCol.Header] = theme.Header;
            colors[(int)ImGuiCol.HeaderHovered] = theme.HeaderHovered;
            colors[(int)ImGuiCol.HeaderActive] = theme.HeaderActive;
            colors[(int)ImGuiCol.Separator] = theme.Separator;
            colors[(int)ImGuiCol.SeparatorHovered] = theme.SeparatorHovered;
            colors[(int)ImGuiCol.SeparatorActive] = theme.SeparatorActive;
            colors[(int)ImGuiCol.ResizeGrip] = theme.ResizeGrip;
            colors[(int)ImGuiCol.ResizeGripHovered] = theme.ResizeGripHovered;
            colors[(int)ImGuiCol.ResizeGripActive] = theme.ResizeGripActive;
            colors[(int)ImGuiCol.Tab] = theme.Tab;
            colors[(int)ImGuiCol.TabHovered] = theme.TabHovered;
            colors[(int)ImGuiCol.TabSelected] = theme.TabActive;
            colors[(int)ImGuiCol.TabDimmed] = theme.TabUnfocused;
            colors[(int)ImGuiCol.TabDimmedSelected] = theme.TabUnfocusedActive;
            colors[(int)ImGuiCol.DockingPreview] = theme.DockingPreview;
            colors[(int)ImGuiCol.DockingEmptyBg] = theme.DockingEmptyBg;
            colors[(int)ImGuiCol.PlotLines] = theme.PlotLines;
            colors[(int)ImGuiCol.PlotLinesHovered] = theme.PlotLinesHovered;
            colors[(int)ImGuiCol.PlotHistogram] = theme.PlotHistogram;
            colors[(int)ImGuiCol.PlotHistogramHovered] = theme.PlotHistogramHovered;
            colors[(int)ImGuiCol.TableHeaderBg] = theme.TableHeaderBg;
            colors[(int)ImGuiCol.TableBorderStrong] = theme.TableBorderStrong;
            colors[(int)ImGuiCol.TableBorderLight] = theme.TableBorderLight;
            colors[(int)ImGuiCol.TableRowBg] = theme.TableRowBg;
            colors[(int)ImGuiCol.TableRowBgAlt] = theme.TableRowBgAlt;
            colors[(int)ImGuiCol.Text] = theme.Text;
            colors[(int)ImGuiCol.TextDisabled] = theme.TextDisabled;
            colors[(int)ImGuiCol.TextSelectedBg] = theme.TextSelectedBg;
            colors[(int)ImGuiCol.DragDropTarget] = theme.DragDropTarget;
            colors[(int)ImGuiCol.NavCursor] = theme.NavHighlight;
            colors[(int)ImGuiCol.NavWindowingHighlight] = theme.NavWindowingHighlight;
            colors[(int)ImGuiCol.NavWindowingDimBg] = theme.NavWindowingDimBg;
            colors[(int)ImGuiCol.ModalWindowDimBg] = theme.ModalWindowDimBg;

            _currentTheme = theme;
            AppLogger.Info($"Applied theme: {theme.Name}");

            return true;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to apply theme {theme.Name}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets a theme by name.
    /// </summary>
    public static Theme? GetTheme(string name)
    {
        return _themes.TryGetValue(name, out var theme) ? theme : null;
    }
}
