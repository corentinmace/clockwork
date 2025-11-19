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

            // Initialize arrays with [height, width] indexing (row-major order)
            matrix.Headers = new ushort[matrix.Height, matrix.Width];
            matrix.Altitudes = new byte[matrix.Height, matrix.Width];
            matrix.Maps = new ushort[matrix.Height, matrix.Width];

            // IMPORTANT: Read order matches LiTRE: Headers → Altitudes → Maps
            // Arrays use [row, col] indexing where row=height and col=width

            // Read headers grid if present (FIRST)
            if (matrix.HasHeaders)
            {
                for (int row = 0; row < matrix.Height; row++)
                {
                    for (int col = 0; col < matrix.Width; col++)
                    {
                        matrix.Headers[row, col] = reader.ReadUInt16();
                    }
                }
            }
            else
            {
                // Fill with empty cells
                for (int row = 0; row < matrix.Height; row++)
                {
                    for (int col = 0; col < matrix.Width; col++)
                    {
                        matrix.Headers[row, col] = EMPTY_CELL;
                    }
                }
            }

            // Read altitudes grid if present (SECOND)
            if (matrix.HasAltitudes)
            {
                for (int row = 0; row < matrix.Height; row++)
                {
                    for (int col = 0; col < matrix.Width; col++)
                    {
                        matrix.Altitudes[row, col] = reader.ReadByte();
                    }
                }
            }

            // Read maps grid (THIRD - always present)
            for (int row = 0; row < matrix.Height; row++)
            {
                for (int col = 0; col < matrix.Width; col++)
                {
                    matrix.Maps[row, col] = reader.ReadUInt16();
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

        // IMPORTANT: Write order matches LiTRE: Headers → Altitudes → Maps

        // Write headers grid if present (FIRST)
        if (HasHeaders)
        {
            for (int row = 0; row < Height; row++)
            {
                for (int col = 0; col < Width; col++)
                {
                    writer.Write(Headers[row, col]);
                }
            }
        }

        // Write altitudes grid if present (SECOND)
        if (HasAltitudes)
        {
            for (int row = 0; row < Height; row++)
            {
                for (int col = 0; col < Width; col++)
                {
                    writer.Write(Altitudes[row, col]);
                }
            }
        }

        // Write maps grid (THIRD - always present)
        for (int row = 0; row < Height; row++)
        {
            for (int col = 0; col < Width; col++)
            {
                writer.Write(Maps[row, col]);
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
    /// <param name="x">Column index (0 to Width-1)</param>
    /// <param name="y">Row index (0 to Height-1)</param>
    public bool IsCellEmpty(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return true;

        // Arrays use [row, col] indexing
        return Maps[y, x] == EMPTY_CELL;
    }

    /// <summary>
    /// Get map ID at position.
    /// </summary>
    /// <param name="x">Column index (0 to Width-1)</param>
    /// <param name="y">Row index (0 to Height-1)</param>
    public ushort GetMapID(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return EMPTY_CELL;

        // Arrays use [row, col] indexing
        return Maps[y, x];
    }

    /// <summary>
    /// Get header ID at position.
    /// </summary>
    /// <param name="x">Column index (0 to Width-1)</param>
    /// <param name="y">Row index (0 to Height-1)</param>
    public ushort GetHeaderID(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return EMPTY_CELL;

        // Arrays use [row, col] indexing
        return Headers[y, x];
    }
}
