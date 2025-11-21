using System.Text;

namespace Clockwork.Core.Models;

/// <summary>
/// Represents area configuration data that links game areas to NSBTX tilesets
/// Format for DP/Pt only
/// </summary>
public class AreaData
{
    // Tileset IDs
    public ushort BuildingsTileset { get; set; }
    public ushort MapBaseTileset { get; set; }

    // Seasonal map tileset variants
    public byte MapTilesetSpring { get; set; }
    public byte MapTilesetSummer { get; set; }
    public byte MapTilesetFall { get; set; }
    public byte MapTilesetWinter { get; set; }

    // Light type
    public ushort LightType { get; set; }

    // Indicates if this uses 2-byte tileset format (when winter == 255)
    public bool UsesTwoByteFormat { get; set; }

    /// <summary>
    /// Reads area data from bytes (DP/Pt format)
    /// </summary>
    public static AreaData ReadFromBytes(byte[] data)
    {
        var area = new AreaData();

        // Validate minimum size (at least 8 bytes: 2 buildings + 2 map + 4 seasonal/padding)
        if (data.Length < 8)
        {
            throw new InvalidDataException($"Area data file is too short: {data.Length} bytes (minimum 8 required)");
        }

        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);

        // DP/Pt format: 8 or 10 bytes depending on winter tileset value
        area.BuildingsTileset = reader.ReadUInt16();

        // Read potential 2-byte base tileset OR 1-byte + seasonal variants
        ushort tempMapBaseTileset = reader.ReadUInt16();
        byte tempMapTilesetSpring = reader.ReadByte();
        byte tempMapTilesetSummer = reader.ReadByte();
        byte tempMapTilesetFall = reader.ReadByte();
        byte tempMapTilesetWinter = reader.ReadByte();

        // Check if this is 2-byte format (winter == 255 means base tileset is 2 bytes)
        if (tempMapTilesetWinter == 255)
        {
            area.MapBaseTileset = tempMapBaseTileset;
            area.UsesTwoByteFormat = true;
        }
        else
        {
            // 1-byte format with seasonal variants
            area.MapBaseTileset = (byte)tempMapBaseTileset; // Only lower byte is valid
            area.MapTilesetSpring = tempMapTilesetSpring;
            area.MapTilesetSummer = tempMapTilesetSummer;
            area.MapTilesetFall = tempMapTilesetFall;
            area.MapTilesetWinter = tempMapTilesetWinter;
            area.UsesTwoByteFormat = false;
        }

        // Read light type if available (some files may be 8 bytes only)
        if (data.Length >= 10)
        {
            area.LightType = reader.ReadUInt16();
        }
        else
        {
            // Default light type for short files
            area.LightType = 0;
            Logging.AppLogger.Warn($"[AreaData] File is only {data.Length} bytes, missing light type field");
        }

        return area;
    }

    /// <summary>
    /// Converts area data to bytes for saving (DP/Pt format)
    /// </summary>
    public byte[] ToBytes()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // DP/Pt format
        writer.Write(BuildingsTileset);

        if (UsesTwoByteFormat)
        {
            // 2-byte format (winter == 255)
            writer.Write(MapBaseTileset);
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((byte)255); // Marker for 2-byte format
        }
        else
        {
            // 1-byte format with seasonal variants
            writer.Write((byte)MapBaseTileset);
            writer.Write((byte)0); // Upper byte of base tileset
            writer.Write(MapTilesetSpring);
            writer.Write(MapTilesetSummer);
            writer.Write(MapTilesetFall);
            writer.Write(MapTilesetWinter);
        }

        writer.Write(LightType);

        return ms.ToArray();
    }
}
