using Clockwork.Core.Models;
using Clockwork.Core.Logging;
using Clockwork.Core.Settings;

namespace Clockwork.Core.Services;

/// <summary>
/// Service for managing Pokémon ROM.
/// </summary>
public class RomService : IApplicationService
{
    private RomInfo? _currentRom;
    private readonly Dictionary<string, GameVersion> _gameCodeMapping = new()
    {
        // Platinum Italian
        { "PLIT", GameVersion.Platinum }
    };

    public RomInfo? CurrentRom => _currentRom;

    public void Initialize()
    {
        AppLogger.Info("RomService initialized");
        _currentRom = null;

        // Auto-load last ROM if configured
        if (SettingsManager.Settings.OpenLastRomOnStartup &&
            !string.IsNullOrEmpty(SettingsManager.Settings.LastRomPath))
        {
            AppLogger.Info("Auto-loading last ROM from settings...");
            LoadRomFromFolder(SettingsManager.Settings.LastRomPath);
        }
    }

    public void Update(double deltaTime)
    {
        // Nothing to do
    }

    public void Shutdown()
    {
        _currentRom = null;
    }

    public void Dispose()
    {
        _currentRom = null;
    }

    /// <summary>
    /// Loads a ROM from an extracted folder.
    /// </summary>
    /// <param name="folderPath">Path to the folder containing the extracted ROM.</param>
    /// <returns>True if loading succeeded.</returns>
    public bool LoadRomFromFolder(string folderPath)
    {
        try
        {
            AppLogger.Info($"Attempting to load ROM from folder: {folderPath}");

            // Check that the folder exists
            if (!Directory.Exists(folderPath))
            {
                AppLogger.Error($"ROM folder does not exist: {folderPath}");
                return false;
            }

            // Read header.bin to get the game code
            string headerPath = Path.Combine(folderPath, "header.bin");
            if (!File.Exists(headerPath))
            {
                AppLogger.Error($"header.bin not found at: {headerPath}");
                return false;
            }

            // Read the game code (4 bytes at offset 0x0C in the header)
            string gameCode = ReadGameCodeFromHeader(headerPath);
            if (string.IsNullOrEmpty(gameCode))
            {
                AppLogger.Error("Failed to read game code from header.bin");
                return false;
            }

            AppLogger.Debug($"Game code detected: {gameCode}");

            // Determine the game version
            if (!_gameCodeMapping.TryGetValue(gameCode, out GameVersion version))
            {
                AppLogger.Error($"Unsupported game code: {gameCode}");
                return false;
            }

            // Create RomInfo
            _currentRom = new RomInfo
            {
                GameCode = gameCode,
                Version = version,
                Language = GetLanguageFromGameCode(gameCode),
                Family = GetGameFamily(version),
                GameName = GetGameName(version),
                RomPath = folderPath
            };

            AppLogger.Info($"ROM identified: {_currentRom.GameName} ({gameCode})");

            // Initialize game directories
            InitializeGameDirectories(folderPath);

            // Save last ROM path to settings and persist immediately
            SettingsManager.Settings.LastRomPath = folderPath;
            SettingsManager.Save(); // Save immediately to avoid losing this on crash
            AppLogger.Debug($"Saved last ROM path to settings: {folderPath}");

            AppLogger.Info("ROM loaded successfully");
            return true;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Exception while loading ROM: {ex.Message}");
            _currentRom = null;
            return false;
        }
    }

    /// <summary>
    /// Reads the game code from the header.bin file.
    /// </summary>
    private string ReadGameCodeFromHeader(string headerPath)
    {
        try
        {
            using var fs = new FileStream(headerPath, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(fs);

            // The game code is at offset 0x0C and is 4 bytes
            fs.Seek(0x0C, SeekOrigin.Begin);
            byte[] codeBytes = reader.ReadBytes(4);

            return System.Text.Encoding.ASCII.GetString(codeBytes);
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to read game code from header: {ex.Message}");
            return string.Empty;
        }
    }

    /// <summary>
    /// Determines the language from the game code.
    /// </summary>
    private GameLanguage GetLanguageFromGameCode(string gameCode)
    {
        if (gameCode.Length != 4) return GameLanguage.Unknown;

        return gameCode[3] switch
        {
            'T' => GameLanguage.English,
            _ => GameLanguage.Unknown
        };
    }

    /// <summary>
    /// Determines the game family.
    /// </summary>
    private GameFamily GetGameFamily(GameVersion version)
    {
        return version switch
        {
            GameVersion.Platinum => GameFamily.Platinum,
            _ => GameFamily.Unknown
        };
    }

    /// <summary>
    /// Gets the game name.
    /// </summary>
    private string GetGameName(GameVersion version)
    {
        return version switch
        {
            GameVersion.Platinum => "Pokémon Platinum",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Initializes the game directory paths.
    /// Based on LiTRE structure: files are loaded from unpacked/ folder.
    /// </summary>
    private void InitializeGameDirectories(string romPath)
    {
        if (_currentRom == null) return;

        AppLogger.Debug("Initializing game directory structure");

        // LiTRE structure: unpacked/ contains extracted NARC files
        string dataPath = Path.Combine(romPath, "data");
        string unpackedPath = Path.Combine(romPath, "unpacked");
        string expandedPath = Path.Combine(romPath, "expanded");

        _currentRom.GameDirectories = new Dictionary<string, string>
        {
            ["root"] = romPath,
            ["data"] = dataPath,
            ["unpacked"] = unpackedPath,
            ["expanded"] = expandedPath,

            // Unpacked directories (following LiTRE structure) - binary files
            ["dynamicHeaders"] = Path.Combine(unpackedPath, "dynamicHeaders"),
            ["scripts"] = Path.Combine(unpackedPath, "scripts"),
            ["eventFiles"] = Path.Combine(unpackedPath, "eventFiles"),
            ["matrices"] = Path.Combine(unpackedPath, "matrices"),
            ["maps"] = Path.Combine(unpackedPath, "maps"),
            ["textArchives"] = Path.Combine(unpackedPath, "textArchives"),

            // Expanded directories (following LiTRE structure) - text files
            ["expandedTextArchives"] = Path.Combine(expandedPath, "textArchives"),

            // Data directories (not unpacked)
            ["fielddata"] = Path.Combine(dataPath, "fielddata"),
            ["maptable"] = Path.Combine(dataPath, "fielddata", "maptable"),
        };

        AppLogger.Debug($"Game directories initialized: {_currentRom.GameDirectories.Count} paths registered");
    }

    /// <summary>
    /// Unloads the current ROM.
    /// </summary>
    public void UnloadRom()
    {
        if (_currentRom != null)
        {
            AppLogger.Info($"Unloading ROM: {_currentRom.GameName}");
        }
        _currentRom = null;
    }
}
