namespace Clockwork.Core.Models;

/// <summary>
/// Terrain type helper for map tiles.
/// Based on LiTRE MapEditor.cs PrepareTypePainterGraphics()
/// Terrain types are stored as bytes with 40+ different values.
/// </summary>
public static class TerrainTypeHelper
{
    /// <summary>
    /// Get the display color for a terrain type (RGBA).
    /// </summary>
    public static (byte R, byte G, byte B, byte A) GetColor(byte terrainValue)
    {
        const byte defaultAlpha = 255;
        const byte semiAlpha = 128;

        return terrainValue switch
        {
            // Default walkable
            0x00 => (255, 255, 255, defaultAlpha),

            // Vegetation
            0x02 => (50, 205, 50, defaultAlpha),      // LimeGreen
            0x03 => (0, 128, 0, defaultAlpha),        // Green
            0x06 => (154, 205, 50, semiAlpha),        // YellowGreen
            0x07 => (0, 100, 0, semiAlpha),           // DarkGreen

            // Special surfaces
            0x08 => (222, 184, 135, semiAlpha),       // BurlyWood
            0x09 => (106, 90, 205, semiAlpha),        // SlateBlue
            0x0A => (255, 99, 71, semiAlpha),         // Tomato
            0x0C => (222, 184, 135, defaultAlpha),    // BurlyWood

            // Water
            0x10 => (135, 206, 235, defaultAlpha),    // SkyBlue
            0x13 => (70, 130, 180, defaultAlpha),     // SteelBlue
            0x15 => (65, 105, 225, defaultAlpha),     // RoyalBlue
            0x16 => (119, 136, 153, defaultAlpha),    // LightSlateGray

            // Special surface
            0x20 => (0, 255, 255, defaultAlpha),      // Cyan
            0x21 => (255, 218, 185, defaultAlpha),    // PeachPuff

            // Dangerous zones
            0x30 or 0x31 or 0x32 or 0x33 => (255, 0, 0, defaultAlpha), // Red

            // Building/interior zones
            0x38 or 0x39 or 0x3A or 0x3B => (128, 0, 0, defaultAlpha), // Maroon
            0x3C or 0x3D or 0x3E => (127, 101, 65, 33),                // Brown special
            0x40 or 0x41 or 0x42 or 0x43 => (255, 215, 0, defaultAlpha), // Gold

            // Other
            0x4B or 0x4C => (160, 82, 45, defaultAlpha), // Sienna

            // Special zones
            0x5E or 0x5F => (153, 50, 204, defaultAlpha), // DarkOrchid
            0x62 or 0x63 or 0x64 or 0x65 => (153, 50, 204, defaultAlpha),
            0x69 => (153, 50, 204, defaultAlpha),
            0x6C or 0x6D or 0x6E or 0x6F => (153, 50, 204, defaultAlpha),

            // Honeydew and variants
            0xA1 or 0xA2 or 0xA3 => (240, 255, 240, defaultAlpha), // Honeydew
            0xA4 => (205, 133, 63, defaultAlpha),      // Peru
            0xA6 => (46, 139, 87, defaultAlpha),       // SeaGreen

            _ => (200, 200, 200, defaultAlpha)         // Unknown - Light gray
        };
    }

    /// <summary>
    /// Get the display name for a terrain type.
    /// </summary>
    public static string GetName(byte terrainValue)
    {
        return terrainValue switch
        {
            0x00 => "Default",
            0x02 => "Vegetation",
            0x03 => "Vegetation (Dark)",
            0x06 => "Vegetation (Yellow)",
            0x07 => "Vegetation (Deep)",
            0x08 => "Sand (Semi)",
            0x09 => "Special Surface",
            0x0A => "Hot Surface",
            0x0C => "Sand",
            0x10 => "Water (Shallow)",
            0x13 => "Water",
            0x15 => "Water (Deep)",
            0x16 => "Water Edge",
            0x20 => "Special (Cyan)",
            0x21 => "Special (Peach)",
            0x30 or 0x31 or 0x32 or 0x33 => "Danger Zone",
            0x38 or 0x39 or 0x3A or 0x3B => "Building Interior",
            0x3C or 0x3D or 0x3E => "Building Special",
            0x40 or 0x41 or 0x42 or 0x43 => "Building (Gold)",
            0x4B or 0x4C => "Rocky",
            0x5E or 0x5F => "Special Zone",
            0x62 or 0x63 or 0x64 or 0x65 => "Special Zone",
            0x69 => "Special Zone",
            0x6C or 0x6D or 0x6E or 0x6F => "Special Zone",
            0xA1 or 0xA2 or 0xA3 => "Light Surface",
            0xA4 => "Desert",
            0xA6 => "Forest Floor",
            _ => $"Type 0x{terrainValue:X2}"
        };
    }

    /// <summary>
    /// Get commonly used terrain types for UI selection.
    /// </summary>
    public static byte[] GetCommonTypes()
    {
        return new byte[]
        {
            0x00, // Default
            0x02, 0x03, // Vegetation
            0x0C, // Sand
            0x10, 0x13, 0x15, // Water
            0x30, // Danger
            0x38, // Building
            0xA1  // Light
        };
    }
}
