using System.Text;

namespace Clockwork.Core.Models;

/// <summary>
/// Represents area configuration data that links game areas to NSBTX tilesets
/// Format for DP/Pt: 8 bytes total
/// </summary>
public class AreaData
{
    // Building tileset ID (2 bytes)
    public ushort BuildingsTileset { get; set; }

    // Spring tileset - can be 1 or 2 bytes depending on format
    // If UsesTwoByteSpringFormat is true, this is the full 2-byte value
    // Otherwise, only the lower byte is used
    public ushort SpringTileset { get; set; }

    // Seasonal map tileset IDs (only used when UsesTwoByteSpringFormat = false)
    public byte MapTilesetSummer { get; set; }
    public byte MapTilesetFall { get; set; }
    public byte MapTilesetWinter { get; set; }

    // Light type (2 bytes)
    public ushort LightType { get; set; }

    // Format flag: if true, SpringTileset is 2 bytes and other seasons are ignored
    // This is indicated by Winter == 255 in the file
    public bool UsesTwoByteSpringFormat { get; set; }

    // Convenience properties for compatibility
    public byte MapTilesetSpring
    {
        get => (byte)(SpringTileset & 0xFF);
        set => SpringTileset = UsesTwoByteSpringFormat ? SpringTileset : value;
    }

    /// <summary>
    /// Reads area data from bytes (DP/Pt format: 8 bytes total)
    /// </summary>
    public static AreaData ReadFromBytes(byte[] data)
    {
        var area = new AreaData();

        // Validate size (must be exactly 8 bytes)
        if (data.Length != 8)
        {
            throw new InvalidDataException($"Area data file has invalid size: {data.Length} bytes (expected 8)");
        }

        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);

        // Read in order:
        // 1. Building tileset ID (2 bytes)
        area.BuildingsTileset = reader.ReadUInt16();

        // 2-5. Read 4 bytes that can be interpreted two ways:
        byte springByte = reader.ReadByte();
        byte summerByte = reader.ReadByte();
        byte fallByte = reader.ReadByte();
        byte winterByte = reader.ReadByte();

        // Check format: if winter == 255, spring is 2 bytes
        if (winterByte == 255)
        {
            // 2-byte spring format: combine spring + summer into ushort
            area.SpringTileset = (ushort)(springByte | (summerByte << 8));
            area.MapTilesetSummer = 0; // Not used
            area.MapTilesetFall = 0;   // Not used
            area.MapTilesetWinter = 255;
            area.UsesTwoByteSpringFormat = true;
        }
        else
        {
            // Normal format: 4 separate seasonal tilesets
            area.SpringTileset = springByte;
            area.MapTilesetSummer = summerByte;
            area.MapTilesetFall = fallByte;
            area.MapTilesetWinter = winterByte;
            area.UsesTwoByteSpringFormat = false;
        }

        // 6. Light type (2 bytes)
        area.LightType = reader.ReadUInt16();

        return area;
    }

    /// <summary>
    /// Converts area data to bytes for saving (DP/Pt format: 8 bytes total)
    /// </summary>
    public byte[] ToBytes()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Write in order:
        // 1. Building tileset ID (2 bytes)
        writer.Write(BuildingsTileset);

        // 2-5. Write 4 bytes depending on format
        if (UsesTwoByteSpringFormat)
        {
            // 2-byte spring format: write spring as ushort, then padding
            writer.Write((byte)(SpringTileset & 0xFF));        // Low byte
            writer.Write((byte)((SpringTileset >> 8) & 0xFF)); // High byte
            writer.Write((byte)0);   // Fall (unused)
            writer.Write((byte)255); // Winter = 255 (format marker)
        }
        else
        {
            // Normal format: write 4 separate seasonal tilesets
            writer.Write((byte)SpringTileset); // Only low byte used
            writer.Write(MapTilesetSummer);
            writer.Write(MapTilesetFall);
            writer.Write(MapTilesetWinter);
        }

        // 6. Light type (2 bytes)
        writer.Write(LightType);

        return ms.ToArray();
    }
}
