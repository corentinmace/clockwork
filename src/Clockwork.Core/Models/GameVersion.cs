namespace Clockwork.Core.Models;

/// <summary>
/// Représente les différentes versions de jeux Pokémon DS supportées.
/// </summary>
public enum GameVersion
{
    Unknown,
    Diamond,
    Pearl,
    Platinum,
    HeartGold,
    SoulSilver
}

/// <summary>
/// Représente les langues supportées.
/// </summary>
public enum GameLanguage
{
    Unknown,
    Japanese,
    English,
    French,
    Italian,
    German,
    Spanish,
    Korean
}

/// <summary>
/// Représente la famille de jeux (génération).
/// </summary>
public enum GameFamily
{
    Unknown,
    DiamondPearl,
    Platinum,
    HeartGoldSoulSilver
}
