namespace Clockwork.Core.Models;

/// <summary>
/// Represents a building on a map (48 bytes structure).
/// Based on LiTRE Building.cs
/// </summary>
public class Building
{
    // Model and position (16 bytes)
    public uint ModelID { get; set; }
    public short XPosition { get; set; }
    public short YPosition { get; set; }
    public short ZPosition { get; set; }

    // Fractional position (6 bytes) - 1/65536th precision
    public ushort XFraction { get; set; }
    public ushort YFraction { get; set; }
    public ushort ZFraction { get; set; }

    // Rotation (6 bytes) - special ushort format
    public ushort XRotation { get; set; }
    public ushort YRotation { get; set; }
    public ushort ZRotation { get; set; }

    // Scale (12 bytes) - usually 16 for normal scale
    public uint Width { get; set; }
    public uint Height { get; set; }
    public uint Length { get; set; }

    // Unknown fields (8 bytes)
    public uint Unknown1 { get; set; }
    public uint Unknown2 { get; set; }

    /// <summary>
    /// Calculate full X position (integer + fraction)
    /// </summary>
    public float FullX => XPosition + XFraction / 65536f;

    /// <summary>
    /// Calculate full Y position (integer + fraction)
    /// </summary>
    public float FullY => YPosition + YFraction / 65536f;

    /// <summary>
    /// Calculate full Z position (integer + fraction)
    /// </summary>
    public float FullZ => ZPosition + ZFraction / 65536f;

    /// <summary>
    /// Reads a building from binary data (48 bytes).
    /// </summary>
    public static Building ReadFromBytes(byte[] data, int offset = 0)
    {
        using var ms = new MemoryStream(data, offset, 48);
        using var reader = new BinaryReader(ms);

        return new Building
        {
            ModelID = reader.ReadUInt32(),
            XPosition = reader.ReadInt16(),
            YPosition = reader.ReadInt16(),
            ZPosition = reader.ReadInt16(),
            XFraction = reader.ReadUInt16(),
            YFraction = reader.ReadUInt16(),
            ZFraction = reader.ReadUInt16(),
            XRotation = reader.ReadUInt16(),
            YRotation = reader.ReadUInt16(),
            ZRotation = reader.ReadUInt16(),
            Width = reader.ReadUInt32(),
            Height = reader.ReadUInt32(),
            Length = reader.ReadUInt32(),
            Unknown1 = reader.ReadUInt32(),
            Unknown2 = reader.ReadUInt32()
        };
    }

    /// <summary>
    /// Writes the building to binary data (48 bytes).
    /// </summary>
    public byte[] ToBytes()
    {
        using var ms = new MemoryStream(48);
        using var writer = new BinaryWriter(ms);

        writer.Write(ModelID);
        writer.Write(XPosition);
        writer.Write(YPosition);
        writer.Write(ZPosition);
        writer.Write(XFraction);
        writer.Write(YFraction);
        writer.Write(ZFraction);
        writer.Write(XRotation);
        writer.Write(YRotation);
        writer.Write(ZRotation);
        writer.Write(Width);
        writer.Write(Height);
        writer.Write(Length);
        writer.Write(Unknown1);
        writer.Write(Unknown2);

        return ms.ToArray();
    }

    /// <summary>
    /// Convert degrees to ushort rotation format.
    /// </summary>
    public static ushort DegreesToRotation(float degrees)
    {
        return (ushort)(degrees * 65536 / 360);
    }

    /// <summary>
    /// Convert ushort rotation format to degrees.
    /// </summary>
    public static float RotationToDegrees(ushort rotation)
    {
        return rotation * 360f / 65536;
    }
}
