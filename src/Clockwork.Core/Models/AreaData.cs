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

    // Seasonal map tileset IDs (4 bytes, 1 byte each)
    public byte MapTilesetSpring { get; set; }
    public byte MapTilesetSummer { get; set; }
    public byte MapTilesetFall { get; set; }
    public byte MapTilesetWinter { get; set; }

    // Light type (2 bytes)
    public ushort LightType { get; set; }

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

        // 2-5. Seasonal map tileset IDs (4 bytes, 1 each)
        area.MapTilesetSpring = reader.ReadByte();
        area.MapTilesetSummer = reader.ReadByte();
        area.MapTilesetFall = reader.ReadByte();
        area.MapTilesetWinter = reader.ReadByte();

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

        // 2-5. Seasonal map tileset IDs (4 bytes, 1 each)
        writer.Write(MapTilesetSpring);
        writer.Write(MapTilesetSummer);
        writer.Write(MapTilesetFall);
        writer.Write(MapTilesetWinter);

        // 6. Light type (2 bytes)
        writer.Write(LightType);

        return ms.ToArray();
    }
}
