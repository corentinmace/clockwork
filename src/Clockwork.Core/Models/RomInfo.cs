namespace Clockwork.Core.Models;

/// <summary>
/// Contient les informations sur la ROM chargée.
/// </summary>
public class RomInfo
{
    /// <summary>
    /// Code du jeu (ex: "CPUE" pour Pokémon Platine US).
    /// </summary>
    public string GameCode { get; set; } = string.Empty;

    /// <summary>
    /// Nom du jeu.
    /// </summary>
    public string GameName { get; set; } = string.Empty;

    /// <summary>
    /// Version du jeu.
    /// </summary>
    public GameVersion Version { get; set; } = GameVersion.Unknown;

    /// <summary>
    /// Langue du jeu.
    /// </summary>
    public GameLanguage Language { get; set; } = GameLanguage.Unknown;

    /// <summary>
    /// Famille du jeu.
    /// </summary>
    public GameFamily Family { get; set; } = GameFamily.Unknown;

    /// <summary>
    /// Chemin du dossier ROM.
    /// </summary>
    public string RomPath { get; set; } = string.Empty;

    /// <summary>
    /// Indique si une ROM est chargée.
    /// </summary>
    public bool IsLoaded => !string.IsNullOrEmpty(GameCode) && Version != GameVersion.Unknown;

    /// <summary>
    /// Dictionnaire des chemins vers les différents dossiers du jeu.
    /// </summary>
    public Dictionary<string, string> GameDirectories { get; set; } = new();
}
