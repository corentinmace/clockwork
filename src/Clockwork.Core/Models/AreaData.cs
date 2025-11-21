using System.Text;

namespace Clockwork.Core.Models;

/// <summary>
/// Represents area configuration data that links game areas to NSBTX tilesets
/// Different format for HGSS vs DP/Pt
/// </summary>
public class AreaData
{
    // Tileset IDs
    public ushort BuildingsTileset { get; set; }
    public ushort MapBaseTileset { get; set; }

    // Seasonal map tileset variants (DP/Pt only)
    public byte MapTilesetSpring { get; set; }
    public byte MapTilesetSummer { get; set; }
    public byte MapTilesetFall { get; set; }
    public byte MapTilesetWinter { get; set; }

    // HGSS specific fields
    public ushort DynamicTextureType { get; set; }
    public byte AreaType { get; set; } // Indoor/outdoor

    // Common fields
    public ushort LightType { get; set; }

    // Indicates if this uses 2-byte tileset format (when winter == 255)
    public bool UsesTwoByteFormat { get; set; }

    /// <summary>
    /// Reads area data from bytes
    /// Format differs between HGSS and DP/Pt
    /// </summary>
    public static AreaData ReadFromBytes(byte[] data, GameVersion gameVersion)
    {
        var area = new AreaData();

        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);

        if (gameVersion == GameVersion.HeartGold || gameVersion == GameVersion.SoulSilver)
        {
            // HGSS format: 10 bytes total
            area.BuildingsTileset = reader.ReadUInt16();
            area.MapBaseTileset = reader.ReadUInt16();
            area.DynamicTextureType = reader.ReadUInt16();
            area.AreaType = reader.ReadByte();
            reader.ReadByte(); // Padding
            area.LightType = reader.ReadUInt16();
            area.UsesTwoByteFormat = true;
        }
        else
        {
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

            area.LightType = reader.ReadUInt16();
        }

        return area;
    }

    /// <summary>
    /// Converts area data to bytes for saving
    /// </summary>
    public byte[] ToBytes(GameVersion gameVersion)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        if (gameVersion == GameVersion.HeartGold || gameVersion == GameVersion.SoulSilver)
        {
            // HGSS format
            writer.Write(BuildingsTileset);
            writer.Write(MapBaseTileset);
            writer.Write(DynamicTextureType);
            writer.Write(AreaType);
            writer.Write((byte)0); // Padding
            writer.Write(LightType);
        }
        else
        {
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
        }

        return ms.ToArray();
    }
}
