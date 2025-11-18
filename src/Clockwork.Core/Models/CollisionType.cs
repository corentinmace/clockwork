namespace Clockwork.Core.Models;

/// <summary>
/// Collision types for map tiles.
/// Based on LiTRE MapEditor.cs PrepareCollisionPainterGraphics()
/// </summary>
public enum CollisionType : byte
{
    /// <summary>Walkable (default) - White</summary>
    Walkable = 0x00,

    /// <summary>Snow surface - Lavender</summary>
    Snow = 0x01,

    /// <summary>Leaves surface - ForestGreen</summary>
    Leaves = 0x02,

    /// <summary>Grass surface - LimeGreen</summary>
    Grass = 0x04,

    /// <summary>Stairs and Ice - PowderBlue</summary>
    StairsAndIce = 0x06,

    /// <summary>Metal surface - Silver</summary>
    Metal = 0x07,

    /// <summary>Stone surface - DimGray</summary>
    Stone = 0x0A,

    /// <summary>Wood surface - SaddleBrown</summary>
    Wood = 0x0D,

    /// <summary>Blocked/Hazard - Red</summary>
    Blocked = 0x80
}

/// <summary>
/// Helper methods for collision types.
/// </summary>
public static class CollisionTypeHelper
{
    /// <summary>
    /// Get the display color for a collision type (RGBA).
    /// </summary>
    public static (byte R, byte G, byte B, byte A) GetColor(byte collisionValue)
    {
        // Default transparency (used by all except Walkable)
        const byte defaultAlpha = 128;

        return collisionValue switch
        {
            0x00 => (255, 255, 255, 32),      // Walkable - White with low alpha
            0x01 => (230, 230, 250, defaultAlpha), // Snow - Lavender
            0x02 => (34, 139, 34, defaultAlpha),   // Leaves - ForestGreen
            0x04 => (50, 205, 50, defaultAlpha),   // Grass - LimeGreen
            0x06 => (176, 224, 230, defaultAlpha), // Stairs/Ice - PowderBlue
            0x07 => (192, 192, 192, defaultAlpha), // Metal - Silver
            0x0A => (105, 105, 105, defaultAlpha), // Stone - DimGray
            0x0D => (139, 69, 19, defaultAlpha),   // Wood - SaddleBrown
            0x80 => (255, 0, 0, defaultAlpha),     // Blocked - Red
            _ => (128, 128, 128, defaultAlpha)     // Unknown - Gray
        };
    }

    /// <summary>
    /// Get the display name for a collision type.
    /// </summary>
    public static string GetName(byte collisionValue)
    {
        return collisionValue switch
        {
            0x00 => "Walkable",
            0x01 => "Snow",
            0x02 => "Leaves",
            0x04 => "Grass",
            0x06 => "Stairs/Ice",
            0x07 => "Metal",
            0x0A => "Stone",
            0x0D => "Wood",
            0x80 => "Blocked",
            _ => $"Unknown (0x{collisionValue:X2})"
        };
    }

    /// <summary>
    /// Get all known collision types.
    /// </summary>
    public static byte[] GetAllTypes()
    {
        return new byte[] { 0x00, 0x01, 0x02, 0x04, 0x06, 0x07, 0x0A, 0x0D, 0x80 };
    }
}
