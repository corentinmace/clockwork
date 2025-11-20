using System;
using System.IO;

namespace Clockwork.Core.Models;

/// <summary>
/// Represents a wild Pokemon encounter file for Diamond/Pearl/Platinum.
/// File size: 0x18A (394) bytes.
/// Contains encounter rates and Pokemon data for various encounter methods.
/// </summary>
public class EncounterFile
{
    // Walking encounters (grass/cave)
    public byte WalkingRate { get; set; }
    public byte[] WalkingLevels { get; set; } = new byte[12];
    public uint[] WalkingPokemon { get; set; } = new uint[12];

    // Special time/method encounters
    public ushort[] SwarmPokemon { get; set; } = new ushort[2];
    public uint[] DayPokemon { get; set; } = new uint[2];
    public uint[] NightPokemon { get; set; } = new uint[2];
    public uint[] RadarPokemon { get; set; } = new uint[4];

    // Regional forms (Shellos/Gastrodon)
    public uint[] RegionalForms { get; set; } = new uint[5];
    public uint UnknownTable { get; set; }

    // Dual slot encounters (GBA game inserted)
    public uint[] RubyPokemon { get; set; } = new uint[2];
    public uint[] SapphirePokemon { get; set; } = new uint[2];
    public uint[] EmeraldPokemon { get; set; } = new uint[2];
    public uint[] FireRedPokemon { get; set; } = new uint[2];
    public uint[] LeafGreenPokemon { get; set; } = new uint[2];

    // Water encounters
    public byte SurfRate { get; set; }
    public byte[] SurfMaxLevels { get; set; } = new byte[5];
    public byte[] SurfMinLevels { get; set; } = new byte[5];
    public ushort[] SurfPokemon { get; set; } = new ushort[5];

    // Fishing encounters
    public byte OldRodRate { get; set; }
    public byte[] OldRodMaxLevels { get; set; } = new byte[5];
    public byte[] OldRodMinLevels { get; set; } = new byte[5];
    public ushort[] OldRodPokemon { get; set; } = new ushort[5];

    public byte GoodRodRate { get; set; }
    public byte[] GoodRodMaxLevels { get; set; } = new byte[5];
    public byte[] GoodRodMinLevels { get; set; } = new byte[5];
    public ushort[] GoodRodPokemon { get; set; } = new ushort[5];

    public byte SuperRodRate { get; set; }
    public byte[] SuperRodMaxLevels { get; set; } = new byte[5];
    public byte[] SuperRodMinLevels { get; set; } = new byte[5];
    public ushort[] SuperRodPokemon { get; set; } = new ushort[5];

    /// <summary>
    /// Read encounter file from bytes.
    /// </summary>
    public static EncounterFile ReadFromBytes(byte[] data)
    {
        var encounter = new EncounterFile();

        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);

        try
        {
            // Walking encounters
            encounter.WalkingRate = (byte)reader.ReadUInt32();

            for (int i = 0; i < 12; i++)
            {
                encounter.WalkingLevels[i] = (byte)reader.ReadUInt32();
                encounter.WalkingPokemon[i] = reader.ReadUInt32();
            }

            // Special encounters
            for (int i = 0; i < 2; i++)
            {
                encounter.SwarmPokemon[i] = (ushort)reader.ReadUInt32();
            }

            for (int i = 0; i < 2; i++)
            {
                encounter.DayPokemon[i] = reader.ReadUInt32();
            }

            for (int i = 0; i < 2; i++)
            {
                encounter.NightPokemon[i] = reader.ReadUInt32();
            }

            for (int i = 0; i < 4; i++)
            {
                encounter.RadarPokemon[i] = reader.ReadUInt32();
            }

            for (int i = 0; i < 5; i++)
            {
                encounter.RegionalForms[i] = reader.ReadUInt32();
            }

            encounter.UnknownTable = reader.ReadUInt32();

            // Dual slot encounters
            for (int i = 0; i < 2; i++)
            {
                encounter.RubyPokemon[i] = reader.ReadUInt32();
            }

            for (int i = 0; i < 2; i++)
            {
                encounter.SapphirePokemon[i] = reader.ReadUInt32();
            }

            for (int i = 0; i < 2; i++)
            {
                encounter.EmeraldPokemon[i] = reader.ReadUInt32();
            }

            for (int i = 0; i < 2; i++)
            {
                encounter.FireRedPokemon[i] = reader.ReadUInt32();
            }

            for (int i = 0; i < 2; i++)
            {
                encounter.LeafGreenPokemon[i] = reader.ReadUInt32();
            }

            // Water encounters (Surf)
            // Position should be at 0xCC
            encounter.SurfRate = (byte)reader.ReadUInt32();

            for (int i = 0; i < 5; i++)
            {
                encounter.SurfMaxLevels[i] = reader.ReadByte();
                encounter.SurfMinLevels[i] = reader.ReadByte();
                reader.BaseStream.Position += 0x2; // Skip 2 bytes padding
                encounter.SurfPokemon[i] = (ushort)reader.ReadUInt32();
            }

            // Old Rod encounters
            // Position should be at 0x124
            encounter.OldRodRate = (byte)reader.ReadUInt32();

            for (int i = 0; i < 5; i++)
            {
                encounter.OldRodMaxLevels[i] = reader.ReadByte();
                encounter.OldRodMinLevels[i] = reader.ReadByte();
                reader.BaseStream.Position += 0x2; // Skip 2 bytes padding
                encounter.OldRodPokemon[i] = (ushort)reader.ReadUInt32();
            }

            // Good Rod encounters
            encounter.GoodRodRate = (byte)reader.ReadUInt32();

            for (int i = 0; i < 5; i++)
            {
                encounter.GoodRodMaxLevels[i] = reader.ReadByte();
                encounter.GoodRodMinLevels[i] = reader.ReadByte();
                reader.BaseStream.Position += 0x2; // Skip 2 bytes padding
                encounter.GoodRodPokemon[i] = (ushort)reader.ReadUInt32();
            }

            // Super Rod encounters
            encounter.SuperRodRate = (byte)reader.ReadUInt32();

            for (int i = 0; i < 5; i++)
            {
                encounter.SuperRodMaxLevels[i] = reader.ReadByte();
                encounter.SuperRodMinLevels[i] = reader.ReadByte();
                reader.BaseStream.Position += 0x2; // Skip 2 bytes padding
                encounter.SuperRodPokemon[i] = (ushort)reader.ReadUInt32();
            }
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"Failed to read encounter file: {ex.Message}", ex);
        }

        return encounter;
    }

    /// <summary>
    /// Convert encounter file to bytes for saving.
    /// </summary>
    public byte[] ToBytes()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        try
        {
            // Walking encounters
            writer.Write((uint)WalkingRate);

            for (int i = 0; i < 12; i++)
            {
                writer.Write((uint)WalkingLevels[i]);
                writer.Write(WalkingPokemon[i]);
            }

            // Special encounters
            for (int i = 0; i < 2; i++)
            {
                writer.Write((uint)SwarmPokemon[i]);
            }

            for (int i = 0; i < 2; i++)
            {
                writer.Write(DayPokemon[i]);
            }

            for (int i = 0; i < 2; i++)
            {
                writer.Write(NightPokemon[i]);
            }

            for (int i = 0; i < 4; i++)
            {
                writer.Write(RadarPokemon[i]);
            }

            for (int i = 0; i < 5; i++)
            {
                writer.Write(RegionalForms[i]);
            }

            writer.Write(UnknownTable);

            // Dual slot encounters
            for (int i = 0; i < 2; i++)
            {
                writer.Write(RubyPokemon[i]);
            }

            for (int i = 0; i < 2; i++)
            {
                writer.Write(SapphirePokemon[i]);
            }

            for (int i = 0; i < 2; i++)
            {
                writer.Write(EmeraldPokemon[i]);
            }

            for (int i = 0; i < 2; i++)
            {
                writer.Write(FireRedPokemon[i]);
            }

            for (int i = 0; i < 2; i++)
            {
                writer.Write(LeafGreenPokemon[i]);
            }

            // Water encounters (Surf)
            writer.Write((uint)SurfRate);

            for (int i = 0; i < 5; i++)
            {
                writer.Write(SurfMaxLevels[i]);
                writer.Write(SurfMinLevels[i]);
                writer.BaseStream.Position += 0x2; // Skip 2 bytes padding (write zeros)
                writer.Write((uint)SurfPokemon[i]);
            }

            // Old Rod encounters
            writer.Write((uint)OldRodRate);

            for (int i = 0; i < 5; i++)
            {
                writer.Write(OldRodMaxLevels[i]);
                writer.Write(OldRodMinLevels[i]);
                writer.BaseStream.Position += 0x2; // Skip 2 bytes padding
                writer.Write((uint)OldRodPokemon[i]);
            }

            // Good Rod encounters
            writer.Write((uint)GoodRodRate);

            for (int i = 0; i < 5; i++)
            {
                writer.Write(GoodRodMaxLevels[i]);
                writer.Write(GoodRodMinLevels[i]);
                writer.BaseStream.Position += 0x2; // Skip 2 bytes padding
                writer.Write((uint)GoodRodPokemon[i]);
            }

            // Super Rod encounters
            writer.Write((uint)SuperRodRate);

            for (int i = 0; i < 5; i++)
            {
                writer.Write(SuperRodMaxLevels[i]);
                writer.Write(SuperRodMinLevels[i]);
                writer.BaseStream.Position += 0x2; // Skip 2 bytes padding
                writer.Write((uint)SuperRodPokemon[i]);
            }

            return ms.ToArray();
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"Failed to write encounter file: {ex.Message}", ex);
        }
    }
}
