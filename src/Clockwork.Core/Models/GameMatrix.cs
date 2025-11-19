namespace Clockwork.Core.Models;

/// <summary>
/// Represents a spatial grid of maps (variable size).
/// Based on LiTRE GameMatrix.cs
/// </summary>
public class GameMatrix
{
    public const ushort EMPTY_CELL = 65535; // Marker for empty cells

    // Matrix ID
    public int MatrixID { get; set; }

    // Grid dimensions
    public byte Width { get; set; }
    public byte Height { get; set; }

    // Grid data (width x height)
    public ushort[,] Maps { get; set; } = new ushort[0, 0];
    public ushort[,] Headers { get; set; } = new ushort[0, 0];
    public byte[,] Altitudes { get; set; } = new byte[0, 0];

    // Flags
    public bool HasHeaders { get; set; }
    public bool HasAltitudes { get; set; }

    /// <summary>
    /// Reads a matrix from a file.
    /// </summary>
    public static GameMatrix? ReadFromFile(string path, int matrixID = 0)
    {
        try
        {
            if (!File.Exists(path))
                return null;

            byte[] data = File.ReadAllBytes(path);
            return ReadFromBytes(data, matrixID);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Reads a matrix from binary data.
    /// </summary>
    public static GameMatrix? ReadFromBytes(byte[] data, int matrixID = 0)
    {
        try
        {
            var matrix = new GameMatrix { MatrixID = matrixID };

            using var ms = new MemoryStream(data);
            using var reader = new BinaryReader(ms);

            // Read dimensions
            matrix.Width = reader.ReadByte();
            matrix.Height = reader.ReadByte();

            // Read flags
            matrix.HasHeaders = reader.ReadByte() == 1;
            matrix.HasAltitudes = reader.ReadByte() == 1;

            // Initialize arrays
            matrix.Maps = new ushort[matrix.Width, matrix.Height];
            matrix.Headers = new ushort[matrix.Width, matrix.Height];
            matrix.Altitudes = new byte[matrix.Width, matrix.Height];

            // Read maps grid
            for (int y = 0; y < matrix.Height; y++)
            {
                for (int x = 0; x < matrix.Width; x++)
                {
                    matrix.Maps[x, y] = reader.ReadUInt16();
                }
            }

            // Read headers grid if present
            if (matrix.HasHeaders)
            {
                for (int y = 0; y < matrix.Height; y++)
                {
                    for (int x = 0; x < matrix.Width; x++)
                    {
                        matrix.Headers[x, y] = reader.ReadUInt16();
                    }
                }
            }
            else
            {
                // Fill with empty cells
                for (int y = 0; y < matrix.Height; y++)
                {
                    for (int x = 0; x < matrix.Width; x++)
                    {
                        matrix.Headers[x, y] = EMPTY_CELL;
                    }
                }
            }

            // Read altitudes grid if present
            if (matrix.HasAltitudes)
            {
                for (int y = 0; y < matrix.Height; y++)
                {
                    for (int x = 0; x < matrix.Width; x++)
                    {
                        matrix.Altitudes[x, y] = reader.ReadByte();
                    }
                }
            }

            return matrix;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Writes the matrix to binary data.
    /// </summary>
    public byte[] ToBytes()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Write dimensions
        writer.Write(Width);
        writer.Write(Height);

        // Write flags
        writer.Write((byte)(HasHeaders ? 1 : 0));
        writer.Write((byte)(HasAltitudes ? 1 : 0));

        // Write maps grid
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                writer.Write(Maps[x, y]);
            }
        }

        // Write headers grid if present
        if (HasHeaders)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    writer.Write(Headers[x, y]);
                }
            }
        }

        // Write altitudes grid if present
        if (HasAltitudes)
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    writer.Write(Altitudes[x, y]);
                }
            }
        }

        return ms.ToArray();
    }

    /// <summary>
    /// Writes the matrix to a file.
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
    /// Check if a cell is empty.
    /// </summary>
    public bool IsCellEmpty(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return true;

        return Maps[x, y] == EMPTY_CELL;
    }

    /// <summary>
    /// Get map ID at position.
    /// </summary>
    public ushort GetMapID(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return EMPTY_CELL;

        return Maps[x, y];
    }

    /// <summary>
    /// Get header ID at position.
    /// </summary>
    public ushort GetHeaderID(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return EMPTY_CELL;

        return Headers[x, y];
    }
}
