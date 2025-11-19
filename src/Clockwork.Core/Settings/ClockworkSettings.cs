namespace Clockwork.Core.Settings;

/// <summary>
/// Application settings for Clockwork.
/// This class holds all user preferences and configuration options.
/// </summary>
public class ClockworkSettings
{
    // === File Paths ===

    /// <summary>
    /// Path to the last opened ROM folder
    /// </summary>
    public string LastRomPath { get; set; } = string.Empty;

    /// <summary>
    /// Path to ndstool.exe (if manually specified)
    /// </summary>
    public string NdsToolPath { get; set; } = string.Empty;

    /// <summary>
    /// Default export directory for ROM packing
    /// </summary>
    public string ExportPath { get; set; } = string.Empty;

    /// <summary>
    /// Path to the last loaded ColorTable file (.ctb) for matrix editor
    /// </summary>
    public string LastColorTablePath { get; set; } = string.Empty;

    // === Editor Preferences ===

    /// <summary>
    /// Prefer hexadecimal display in text editor
    /// </summary>
    public bool TextEditorPreferHex { get; set; } = false;

    /// <summary>
    /// Script editor format preference (0 = plain text, 1 = formatted)
    /// </summary>
    public int ScriptEditorFormatPreference { get; set; } = 0;

    /// <summary>
    /// Show grid in map editor
    /// </summary>
    public bool MapEditorShowGrid { get; set; } = true;

    /// <summary>
    /// Grid size for map editor
    /// </summary>
    public int MapEditorGridSize { get; set; } = 16;

    // === Rendering Options ===

    /// <summary>
    /// Render buildings in map view
    /// </summary>
    public bool RenderBuildings { get; set; } = true;

    /// <summary>
    /// Render collision overlay in map view
    /// </summary>
    public bool RenderCollision { get; set; } = true;

    /// <summary>
    /// Render terrain overlay in map view
    /// </summary>
    public bool RenderTerrain { get; set; } = true;

    /// <summary>
    /// Collision overlay opacity (0.0 to 1.0)
    /// </summary>
    public float CollisionOpacity { get; set; } = 0.5f;

    /// <summary>
    /// Terrain overlay opacity (0.0 to 1.0)
    /// </summary>
    public float TerrainOpacity { get; set; } = 0.5f;

    // === Application Behavior ===

    /// <summary>
    /// Current theme name
    /// </summary>
    public string CurrentThemeName { get; set; } = "Dark";

    /// <summary>
    /// Automatically check for application updates on startup
    /// </summary>
    public bool AutomaticallyCheckForUpdates { get; set; } = true;

    /// <summary>
    /// Open the last ROM on startup
    /// </summary>
    public bool OpenLastRomOnStartup { get; set; } = false;

    /// <summary>
    /// Ask for confirmation before closing with unsaved changes
    /// </summary>
    public bool ConfirmBeforeClosing { get; set; } = true;

    /// <summary>
    /// Auto-save interval in minutes (0 = disabled)
    /// </summary>
    public int AutoSaveIntervalMinutes { get; set; } = 5;

    // === Window Layout ===

    /// <summary>
    /// Window width on last close
    /// </summary>
    public int WindowWidth { get; set; } = 1280;

    /// <summary>
    /// Window height on last close
    /// </summary>
    public int WindowHeight { get; set; } = 720;

    /// <summary>
    /// Window maximized state
    /// </summary>
    public bool WindowMaximized { get; set; } = false;

    /// <summary>
    /// Sidebar collapsed state
    /// </summary>
    public bool SidebarCollapsed { get; set; } = false;

    // === Logging ===

    /// <summary>
    /// Minimum log level to display (Debug, Info, Warning, Error, Fatal)
    /// </summary>
    public string MinimumLogLevel { get; set; } = "Debug";

    /// <summary>
    /// Enable logging to file
    /// </summary>
    public bool EnableFileLogging { get; set; } = true;

    /// <summary>
    /// Maximum number of log lines to keep in memory
    /// </summary>
    public int MaxLogLines { get; set; } = 1000;
}
