using System.Collections.Generic;

namespace Clockwork.Core.Data;

/// <summary>
/// Game constants for Pokémon NDS games (Diamond/Pearl/Platinum/HeartGold/SoulSilver)
/// </summary>
public static class GameConstants
{
    /// <summary>
    /// Weather type names for Platinum (37 entries)
    /// </summary>
    public static readonly Dictionary<byte, string> PtWeatherNames = new()
    {
        { 0, "Normal" },
        { 1, "Normal, somewhat dark" },
        { 2, "Rain" },
        { 3, "Heavy rain" },
        { 4, "Thunderstorm" },
        { 5, "Snowfall, slow" },
        { 6, "Diamond dust" },
        { 7, "Blizzard" },
        { 8, "Normal [08]" },
        { 9, "Volcanic ash fall, slow" },
        { 10, "Sand storm" },
        { 11, "Hail" },
        { 12, "Rocks ascending (?)" },
        { 13, "Normal [13]" },
        { 14, "Fog" },
        { 15, "Deep fog" },
        { 16, "Dark, Flash usable" },
        { 17, "Lightning, no rain" },
        { 18, "Light fog" },
        { 19, "Heavy fog" },
        { 20, "Normal [20]" },
        { 21, "Diamond dust [21]" },
        { 22, "Volcanic ash fall, steady" },
        { 23, "Eterna forest weather" },
        { 24, "Player spotlight [24]" },
        { 25, "Player spotlight [25]" },
        { 26, "Dark fog" },
        { 27, "Somewhat green" },
        { 28, "Somewhat red" },
        { 29, "Somewhat blue" },
        { 30, "Dim light" },
        { 31, "Normal [31]" },
        { 32, "Rain [32]" },
        { 33, "Normal [33]" },
        { 34, "Diamond dust [34]" },
        { 35, "Diamond dust [35]" },
        { 36, "Snowfall, slow [36]" }
    };

    /// <summary>
    /// Camera angle names for Diamond/Pearl/Platinum (16 entries)
    /// </summary>
    public static readonly Dictionary<byte, string> DPPtCameraAngles = new()
    {
        { 0, "3D Normal" },
        { 1, "3D Top Higher" },
        { 2, "3D Front Low - Wide FOV" },
        { 3, "3D Front" },
        { 4, "2D Ortho" },
        { 5, "3D Normal - Wide FOV" },
        { 6, "3D Bird View" },
        { 7, "3D Normal [07]" },
        { 8, "3D Bird View Far" },
        { 9, "3D Front - Wide FOV" },
        { 10, "3D Top - Narrow" },
        { 11, "3D Normal [11]" },
        { 12, "3D Top" },
        { 13, "Front 3D" },
        { 14, "3D Top - Wide FOV" },
        { 15, "3D Front Low" }
    };

    /// <summary>
    /// Gets weather name by ID for Platinum (default), returns "Unknown (ID)" if not found
    /// </summary>
    public static string GetWeatherName(byte weatherId)
    {
        return PtWeatherNames.TryGetValue(weatherId, out var name)
            ? name
            : $"Unknown ({weatherId})";
    }

    /// <summary>
    /// Gets camera angle name by ID for DPPt (default), returns "Unknown (ID)" if not found
    /// </summary>
    public static string GetCameraAngleName(byte cameraId)
    {
        return DPPtCameraAngles.TryGetValue(cameraId, out var name)
            ? name
            : $"Unknown ({cameraId})";
    }

    /// <summary>
    /// Text archive numbers for location names by game
    /// </summary>
    public static class LocationNamesTextArchive
    {
        public const int Platinum = 433;
    }

    /// <summary>
    /// ARM9 binary load address in DS memory.
    /// </summary>
    public const uint ARM9_LOAD_ADDRESS = 0x02000000;

    /// <summary>
    /// SynthOverlay base address in DS memory.
    /// All addresses >= this value belong to the SynthOverlay (file 0065).
    /// </summary>
    public const uint SYNTH_OVERLAY_BASE_ADDRESS = 0x023C8000;
}
