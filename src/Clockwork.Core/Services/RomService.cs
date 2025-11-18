using Clockwork.Core.Models;

namespace Clockwork.Core.Services;

/// <summary>
/// Service de gestion de la ROM Pokémon.
/// </summary>
public class RomService : IApplicationService
{
    private RomInfo? _currentRom;
    private readonly Dictionary<string, GameVersion> _gameCodeMapping = new()
    {
        // Diamond
        { "ADAE", GameVersion.Diamond }, // US
        { "ADAJ", GameVersion.Diamond }, // JP
        { "ADAP", GameVersion.Diamond }, // EU
        { "ADAS", GameVersion.Diamond }, // ES
        { "ADAI", GameVersion.Diamond }, // IT
        { "ADAF", GameVersion.Diamond }, // FR
        { "ADAD", GameVersion.Diamond }, // DE
        { "ADAK", GameVersion.Diamond }, // KR

        // Pearl
        { "APAE", GameVersion.Pearl }, // US
        { "APAJ", GameVersion.Pearl }, // JP
        { "APAP", GameVersion.Pearl }, // EU
        { "APAS", GameVersion.Pearl }, // ES
        { "APAI", GameVersion.Pearl }, // IT
        { "APAF", GameVersion.Pearl }, // FR
        { "APAD", GameVersion.Pearl }, // DE
        { "APAK", GameVersion.Pearl }, // KR

        // Platinum
        { "CPUE", GameVersion.Platinum }, // US
        { "CPUJ", GameVersion.Platinum }, // JP
        { "CPUP", GameVersion.Platinum }, // EU
        { "CPUS", GameVersion.Platinum }, // ES
        { "CPUI", GameVersion.Platinum }, // IT
        { "CPUF", GameVersion.Platinum }, // FR
        { "CPUD", GameVersion.Platinum }, // DE
        { "CPUK", GameVersion.Platinum }, // KR

        // HeartGold
        { "IPKE", GameVersion.HeartGold }, // US
        { "IPKJ", GameVersion.HeartGold }, // JP
        { "IPKP", GameVersion.HeartGold }, // EU
        { "IPKS", GameVersion.HeartGold }, // ES
        { "IPKI", GameVersion.HeartGold }, // IT
        { "IPKF", GameVersion.HeartGold }, // FR
        { "IPKD", GameVersion.HeartGold }, // DE
        { "IPKK", GameVersion.HeartGold }, // KR

        // SoulSilver
        { "IPGE", GameVersion.SoulSilver }, // US
        { "IPGJ", GameVersion.SoulSilver }, // JP
        { "IPGP", GameVersion.SoulSilver }, // EU
        { "IPGS", GameVersion.SoulSilver }, // ES
        { "IPGI", GameVersion.SoulSilver }, // IT
        { "IPGF", GameVersion.SoulSilver }, // FR
        { "IPGD", GameVersion.SoulSilver }, // DE
        { "IPGK", GameVersion.SoulSilver }, // KR
    };

    public RomInfo? CurrentRom => _currentRom;

    public void Initialize()
    {
        _currentRom = null;
    }

    public void Update(double deltaTime)
    {
        // Rien à faire
    }

    public void Shutdown()
    {
        _currentRom = null;
    }

    /// <summary>
    /// Charge une ROM depuis un dossier extrait.
    /// </summary>
    /// <param name="folderPath">Chemin vers le dossier contenant la ROM extraite.</param>
    /// <returns>True si le chargement a réussi.</returns>
    public bool LoadRomFromFolder(string folderPath)
    {
        try
        {
            // Vérifier que le dossier existe
            if (!Directory.Exists(folderPath))
            {
                return false;
            }

            // Lire le header.bin pour obtenir le game code
            string headerPath = Path.Combine(folderPath, "header.bin");
            if (!File.Exists(headerPath))
            {
                return false;
            }

            // Lire le game code (4 bytes à l'offset 0x0C dans le header)
            string gameCode = ReadGameCodeFromHeader(headerPath);
            if (string.IsNullOrEmpty(gameCode))
            {
                return false;
            }

            // Déterminer la version du jeu
            if (!_gameCodeMapping.TryGetValue(gameCode, out GameVersion version))
            {
                return false;
            }

            // Créer RomInfo
            _currentRom = new RomInfo
            {
                GameCode = gameCode,
                Version = version,
                Language = GetLanguageFromGameCode(gameCode),
                Family = GetGameFamily(version),
                GameName = GetGameName(version),
                RomPath = folderPath
            };

            // Initialiser les chemins des dossiers
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
    /// Lit le game code depuis le fichier header.bin.
    /// </summary>
    private string ReadGameCodeFromHeader(string headerPath)
    {
        try
        {
            using var fs = new FileStream(headerPath, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(fs);

            // Le game code est à l'offset 0x0C et fait 4 bytes
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
    /// Détermine la langue depuis le game code.
    /// </summary>
    private GameLanguage GetLanguageFromGameCode(string gameCode)
    {
        if (gameCode.Length != 4) return GameLanguage.Unknown;

        return gameCode[3] switch
        {
            'E' => GameLanguage.English,
            'J' => GameLanguage.Japanese,
            'F' => GameLanguage.French,
            'I' => GameLanguage.Italian,
            'D' => GameLanguage.German,
            'S' => GameLanguage.Spanish,
            'K' => GameLanguage.Korean,
            'P' => GameLanguage.English, // EU = English par défaut
            _ => GameLanguage.Unknown
        };
    }

    /// <summary>
    /// Détermine la famille du jeu.
    /// </summary>
    private GameFamily GetGameFamily(GameVersion version)
    {
        return version switch
        {
            GameVersion.Diamond or GameVersion.Pearl => GameFamily.DiamondPearl,
            GameVersion.Platinum => GameFamily.Platinum,
            GameVersion.HeartGold or GameVersion.SoulSilver => GameFamily.HeartGoldSoulSilver,
            _ => GameFamily.Unknown
        };
    }

    /// <summary>
    /// Obtient le nom du jeu.
    /// </summary>
    private string GetGameName(GameVersion version)
    {
        return version switch
        {
            GameVersion.Diamond => "Pokémon Diamond",
            GameVersion.Pearl => "Pokémon Pearl",
            GameVersion.Platinum => "Pokémon Platinum",
            GameVersion.HeartGold => "Pokémon HeartGold",
            GameVersion.SoulSilver => "Pokémon SoulSilver",
            _ => "Unknown"
        };
    }

    /// <summary>
    /// Initialise les chemins des dossiers du jeu.
    /// </summary>
    private void InitializeGameDirectories(string romPath)
    {
        if (_currentRom == null) return;

        // Ces chemins sont basés sur la structure typique d'une ROM DS extraite
        // Le dossier "a" contient les fichiers du jeu organisés en sous-dossiers
        string dataPath = Path.Combine(romPath, "data");

        _currentRom.GameDirectories = new Dictionary<string, string>
        {
            ["root"] = romPath,
            ["data"] = dataPath,
            // TODO: Ajouter les chemins spécifiques selon la version du jeu
            // Ces chemins varient selon Diamond/Pearl/Platinum/HGSS
        };
    }

    /// <summary>
    /// Décharge la ROM actuelle.
    /// </summary>
    public void UnloadRom()
    {
        _currentRom = null;
    }
}
