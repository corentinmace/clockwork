using System.Collections.Generic;

namespace Clockwork.Core.Data;

/// <summary>
/// Game constants for Pokémon NDS games (Diamond/Pearl/Platinum/HeartGold/SoulSilver)
/// </summary>
public static class GameConstants
{
    /// <summary>
    /// Weather type names for Diamond/Pearl
    /// </summary>
    public static readonly Dictionary<byte, string> WeatherNames = new()
    {
        { 0, "None" },
        { 1, "Sunny / Clearing" },
        { 2, "Rain" },
        { 3, "Snow" },
        { 4, "Sandstorm" },
        { 5, "Fog" },
        { 6, "Heavy Fog" },
        { 7, "Heavy Rain" },
        { 8, "Thunderstorm" },
        { 9, "Blizzard" },
        { 10, "Diamond Dust" },
        { 11, "Dark Flash" },
        { 12, "Dark" },
        { 13, "Underwater" }
    };

    /// <summary>
    /// Camera angle names for Diamond/Pearl/Platinum
    /// </summary>
    public static readonly Dictionary<byte, string> CameraAngles = new()
    {
        { 0, "Default (45°)" },
        { 1, "Overhead (90°)" },
        { 2, "Free Camera" },
        { 3, "Low Angle" },
        { 4, "Side View" },
        { 5, "Battle Camera" },
        { 6, "Custom 1" },
        { 7, "Custom 2" }
    };

    /// <summary>
    /// Gets weather name by ID, returns "Unknown (ID)" if not found
    /// </summary>
    public static string GetWeatherName(byte weatherId)
    {
        return WeatherNames.TryGetValue(weatherId, out var name)
            ? name
            : $"Unknown ({weatherId})";
    }

    /// <summary>
    /// Gets camera angle name by ID, returns "Unknown (ID)" if not found
    /// </summary>
    public static string GetCameraAngleName(byte cameraId)
    {
        return CameraAngles.TryGetValue(cameraId, out var name)
            ? name
            : $"Unknown ({cameraId})";
    }

    /// <summary>
    /// Text archive numbers for location names by game
    /// </summary>
    public static class LocationNamesTextArchive
    {
        public const int DiamondPearl = 382;
        public const int Platinum = 433;
        public const int HeartGoldSoulSilver = 279; // 272 for Japanese
    }
}
