namespace Clockwork.Core.Models;

/// <summary>
/// Represents a map file with 32x32 tile grid.
/// Based on LiTRE MapFile.cs
/// </summary>
public class MapFile
{
    public const byte MAP_SIZE = 32; // Always 32x32 tiles

    // Map ID
    public int MapID { get; set; }

    // 32x32 grids
    public byte[,] Collisions { get; set; } = new byte[MAP_SIZE, MAP_SIZE];
    public byte[,] Types { get; set; } = new byte[MAP_SIZE, MAP_SIZE];

    // Buildings on this map
    public List<Building> Buildings { get; set; } = new();

    // Raw binary data (for features not yet implemented)
    public byte[]? MapModelData { get; set; }
    public byte[]? BdhcData { get; set; } // Terrain height data
    public byte[]? BgsData { get; set; }  // Background sound (HGSS only)

    /// <summary>
    /// Reads a map file from binary data.
    /// </summary>
    public static MapFile? ReadFromFile(string path, int mapID = 0)
    {
        try
        {
            if (!File.Exists(path))
                return null;

            byte[] data = File.ReadAllBytes(path);
            return ReadFromBytes(data, mapID);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Reads a map file from binary data.
    /// </summary>
    public static MapFile? ReadFromBytes(byte[] data, int mapID = 0)
    {
        try
        {
            var map = new MapFile { MapID = mapID };

            using var ms = new MemoryStream(data);
            using var reader = new BinaryReader(ms);

            // Read header (2 bytes)
            byte permissionsFlag = reader.ReadByte();
            byte buildingsFlag = reader.ReadByte();

            // Read permissions/collisions if present
            if (permissionsFlag == 1)
            {
                for (int y = 0; y < MAP_SIZE; y++)
                {
                    for (int x = 0; x < MAP_SIZE; x++)
                    {
                        map.Collisions[x, y] = reader.ReadByte();
                    }
                }

                for (int y = 0; y < MAP_SIZE; y++)
                {
                    for (int x = 0; x < MAP_SIZE; x++)
                    {
                        map.Types[x, y] = reader.ReadByte();
                    }
                }
            }

            // Read buildings if present
            if (buildingsFlag == 1)
            {
                uint buildingCount = reader.ReadUInt32();
                for (int i = 0; i < buildingCount; i++)
                {
                    byte[] buildingData = reader.ReadBytes(48);
                    map.Buildings.Add(Building.ReadFromBytes(buildingData));
                }
            }

            // Read remaining data (map model, bdhc, bgs)
            // For now, just store as raw bytes
            long remaining = ms.Length - ms.Position;
            if (remaining > 0)
            {
                map.MapModelData = reader.ReadBytes((int)remaining);
            }

            return map;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Writes the map file to binary data.
    /// </summary>
    public byte[] ToBytes()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Write header flags
        writer.Write((byte)1); // Permissions flag
        writer.Write((byte)1); // Buildings flag

        // Write collisions grid
        for (int y = 0; y < MAP_SIZE; y++)
        {
            for (int x = 0; x < MAP_SIZE; x++)
            {
                writer.Write(Collisions[x, y]);
            }
        }

        // Write types grid
        for (int y = 0; y < MAP_SIZE; y++)
        {
            for (int x = 0; x < MAP_SIZE; x++)
            {
                writer.Write(Types[x, y]);
            }
        }

        // Write buildings
        writer.Write((uint)Buildings.Count);
        foreach (var building in Buildings)
        {
            writer.Write(building.ToBytes());
        }

        // Write remaining data if present
        if (MapModelData != null)
        {
            writer.Write(MapModelData);
        }

        return ms.ToArray();
    }

    /// <summary>
    /// Writes the map file to disk.
    /// </summary>
    public bool WriteToFile(string path)
    {
        try
        {
            byte[] data = ToBytes();
            File.WriteAllBytes(path, data);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Check if a tile is walkable (collision value 0x00).
    /// </summary>
    public bool IsWalkable(int x, int y)
    {
        if (x < 0 || x >= MAP_SIZE || y < 0 || y >= MAP_SIZE)
            return false;

        return (Collisions[x, y] & 0x80) == 0;
    }

    /// <summary>
    /// Set tile walkability.
    /// </summary>
    public void SetWalkable(int x, int y, bool walkable)
    {
        if (x < 0 || x >= MAP_SIZE || y < 0 || y >= MAP_SIZE)
            return;

        if (walkable)
            Collisions[x, y] = 0x00;
        else
            Collisions[x, y] = 0x80;
    }
}
