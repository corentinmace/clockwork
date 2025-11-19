namespace Clockwork.Core.Models;

/// <summary>
/// Represents a Pokémon Platinum map header (24 bytes).
/// Based on LiTRE structure.
/// </summary>
public class MapHeader
{
    // Header ID (not stored in file, set when loading)
    public int HeaderID { get; set; }
    public string InternalName { get; set; } = string.Empty;

    // Binary structure for Platinum (24 bytes)
    public byte AreaDataID { get; set; }           // 0x00, 1 byte
    public byte Unknown1 { get; set; }             // 0x01, 1 byte
    public ushort MatrixID { get; set; }           // 0x02, 2 bytes
    public ushort ScriptFileID { get; set; }       // 0x04, 2 bytes
    public ushort LevelScriptID { get; set; }      // 0x06, 2 bytes
    public ushort TextArchiveID { get; set; }      // 0x08, 2 bytes
    public ushort MusicDayID { get; set; }         // 0x0A, 2 bytes
    public ushort MusicNightID { get; set; }       // 0x0C, 2 bytes
    public byte WildPokemon { get; set; }          // 0x0E, 1 byte (NOTE: byte, not ushort!)
    public byte TimeID { get; set; }               // 0x0F, 1 byte
    public ushort EventFileID { get; set; }        // 0x10, 2 bytes
    public byte LocationName { get; set; }         // 0x12, 1 byte
    public byte AreaIcon { get; set; }             // 0x13, 1 byte
    public byte WeatherID { get; set; }            // 0x14, 1 byte
    public byte CameraAngleID { get; set; }        // 0x15, 1 byte
    public ushort MapSettings { get; set; }        // 0x16, 2 bytes

    // Map settings bitfield breakdown (for Platinum)
    // Bits 0-6: Location Specifier (7 bits)
    // Bits 7-11: Battle Background (5 bits)
    // Bits 12-15: Flags (4 bits)

    public byte LocationSpecifier
    {
        get => (byte)(MapSettings & 0x7F);
        set => MapSettings = (ushort)((MapSettings & ~0x7F) | (value & 0x7F));
    }

    public byte BattleBackground
    {
        get => (byte)((MapSettings >> 7) & 0x1F);
        set => MapSettings = (ushort)((MapSettings & ~(0x1F << 7)) | ((value & 0x1F) << 7));
    }

    public byte Flags
    {
        get => (byte)((MapSettings >> 12) & 0x0F);
        set => MapSettings = (ushort)((MapSettings & ~(0x0F << 12)) | ((value & 0x0F) << 12));
    }

    public bool AllowFly
    {
        get => (Flags & 0x01) != 0;
        set
        {
            if (value) Flags |= 0x01;
            else Flags &= unchecked((byte)~0x01);
        }
    }

    public bool AllowEscapeRope
    {
        get => (Flags & 0x02) != 0;
        set
        {
            if (value) Flags |= 0x02;
            else Flags &= unchecked((byte)~0x02);
        }
    }

    public bool AllowRunningShoes
    {
        get => (Flags & 0x04) != 0;
        set
        {
            if (value) Flags |= 0x04;
            else Flags &= unchecked((byte)~0x04);
        }
    }

    public bool AllowBike
    {
        get => (Flags & 0x08) != 0;
        set
        {
            if (value) Flags |= 0x08;
            else Flags &= unchecked((byte)~0x08);
        }
    }

    /// <summary>
    /// Reads a map header from a binary file.
    /// </summary>
    public static MapHeader? ReadFromFile(string path, int headerID = 0)
    {
        try
        {
            if (!File.Exists(path)) return null;

            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(fs);

            if (fs.Length < 24) return null; // Header must be at least 24 bytes

            return new MapHeader
            {
                HeaderID = headerID,
                AreaDataID = reader.ReadByte(),
                Unknown1 = reader.ReadByte(),
                MatrixID = reader.ReadUInt16(),
                ScriptFileID = reader.ReadUInt16(),
                LevelScriptID = reader.ReadUInt16(),
                TextArchiveID = reader.ReadUInt16(),
                MusicDayID = reader.ReadUInt16(),
                MusicNightID = reader.ReadUInt16(),
                WildPokemon = reader.ReadByte(),
                TimeID = reader.ReadByte(),
                EventFileID = reader.ReadUInt16(),
                LocationName = reader.ReadByte(),
                AreaIcon = reader.ReadByte(),
                WeatherID = reader.ReadByte(),
                CameraAngleID = reader.ReadByte(),
                MapSettings = reader.ReadUInt16()
            };
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Writes this map header to a binary file.
    /// </summary>
    public bool WriteToFile(string path)
    {
        try
        {
            using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
            using var writer = new BinaryWriter(fs);

            writer.Write(AreaDataID);
            writer.Write(Unknown1);
            writer.Write(MatrixID);
            writer.Write(ScriptFileID);
            writer.Write(LevelScriptID);
            writer.Write(TextArchiveID);
            writer.Write(MusicDayID);
            writer.Write(MusicNightID);
            writer.Write(WildPokemon);
            writer.Write(TimeID);
            writer.Write(EventFileID);
            writer.Write(LocationName);
            writer.Write(AreaIcon);
            writer.Write(WeatherID);
            writer.Write(CameraAngleID);
            writer.Write(MapSettings);

            return true;
        }
        catch
        {
            return false;
        }
    }
}
