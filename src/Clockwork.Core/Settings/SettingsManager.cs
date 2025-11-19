using System.Text.Json;
using Clockwork.Core.Logging;

namespace Clockwork.Core.Settings;

/// <summary>
/// Manages application settings persistence.
/// Handles loading and saving user preferences to JSON file.
/// </summary>
public static class SettingsManager
{
    private static ClockworkSettings _settings = new();
    private static string _settingsFilePath = string.Empty;
    private static readonly object _fileLock = new();

    /// <summary>
    /// Gets the current application settings.
    /// </summary>
    public static ClockworkSettings Settings => _settings;

    /// <summary>
    /// Initializes the settings manager and loads settings from disk.
    /// </summary>
    public static void Initialize()
    {
        try
        {
            // Use AppData/Clockwork for settings
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string clockworkPath = Path.Combine(appDataPath, "Clockwork");

            Directory.CreateDirectory(clockworkPath);

            _settingsFilePath = Path.Combine(clockworkPath, "userSettings.json");

            AppLogger.Info("SettingsManager initialized");
            AppLogger.Debug($"Settings file path: {_settingsFilePath}");

            Load();
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to initialize SettingsManager: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads settings from the JSON file.
    /// If the file doesn't exist, creates a new settings instance with defaults.
    /// </summary>
    public static void Load()
    {
        try
        {
            lock (_fileLock)
            {
                if (File.Exists(_settingsFilePath))
                {
                    AppLogger.Debug("Loading settings from file...");

                    string json = File.ReadAllText(_settingsFilePath);
                    var loadedSettings = JsonSerializer.Deserialize<ClockworkSettings>(json);

                    if (loadedSettings != null)
                    {
                        _settings = loadedSettings;
                        AppLogger.Info("Settings loaded successfully");
                    }
                    else
                    {
                        AppLogger.Warn("Failed to deserialize settings, using defaults");
                        _settings = new ClockworkSettings();
                        Save(); // Save defaults
                    }
                }
                else
                {
                    AppLogger.Info("Settings file not found, creating with defaults");
                    _settings = new ClockworkSettings();
                    Save(); // Create the file with defaults
                }
            }
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to load settings: {ex.Message}");
            _settings = new ClockworkSettings(); // Fallback to defaults
        }
    }

    /// <summary>
    /// Saves current settings to the JSON file.
    /// </summary>
    public static void Save()
    {
        try
        {
            lock (_fileLock)
            {
                AppLogger.Debug("Saving settings to file...");

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true, // Pretty-print JSON
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                string json = JsonSerializer.Serialize(_settings, options);
                File.WriteAllText(_settingsFilePath, json);

                AppLogger.Info("Settings saved successfully");
            }
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to save settings: {ex.Message}");
        }
    }

    /// <summary>
    /// Resets all settings to their default values.
    /// </summary>
    public static void ResetToDefaults()
    {
        AppLogger.Info("Resetting settings to defaults");
        _settings = new ClockworkSettings();
        Save();
    }

    /// <summary>
    /// Gets the settings file path.
    /// </summary>
    public static string SettingsFilePath => _settingsFilePath;
}
