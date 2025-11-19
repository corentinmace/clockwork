using Clockwork.Core.Models;

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
        _currentRom = null;
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
            // Check that the folder exists
            if (!Directory.Exists(folderPath))
            {
                return false;
            }

            // Read header.bin to get the game code
            string headerPath = Path.Combine(folderPath, "header.bin");
            if (!File.Exists(headerPath))
            {
                return false;
            }

            // Read the game code (4 bytes at offset 0x0C in the header)
            string gameCode = ReadGameCodeFromHeader(headerPath);
            if (string.IsNullOrEmpty(gameCode))
            {
                return false;
            }

            // Determine the game version
            if (!_gameCodeMapping.TryGetValue(gameCode, out GameVersion version))
            {
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

            // Initialize game directories
            InitializeGameDirectories(folderPath);

            return true;
        }
        catch
        {
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
        catch
        {
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
    }

    /// <summary>
    /// Unloads the current ROM.
    /// </summary>
    public void UnloadRom()
    {
        _currentRom = null;
    }
}
