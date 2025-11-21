using System.Text;

namespace Clockwork.Core.Models;

/// <summary>
/// Represents an NSBTX (Nintendo DS Texture) file
/// NSBTX files contain texture and palette data for 3D models
/// </summary>
public class NsbtxFile
{
    /// <summary>
    /// File signature - should be "BTX0"
    /// </summary>
    public string Magic { get; set; } = string.Empty;

    /// <summary>
    /// File format version
    /// </summary>
    public ushort Version { get; set; }

    /// <summary>
    /// Total file size
    /// </summary>
    public uint FileSize { get; set; }

    /// <summary>
    /// Number of data blocks in the file
    /// </summary>
    public ushort BlockCount { get; set; }

    /// <summary>
    /// List of texture names found in the file
    /// </summary>
    public List<string> TextureNames { get; set; } = new();

    /// <summary>
    /// List of palette names found in the file
    /// </summary>
    public List<string> PaletteNames { get; set; } = new();

    /// <summary>
    /// Raw file data (for re-export without modification)
    /// </summary>
    public byte[] RawData { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// File path this NSBTX was loaded from
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Loads an NSBTX file from a byte array
    /// </summary>
    public static NsbtxFile FromBytes(byte[] data, string filePath = "")
    {
        var file = new NsbtxFile
        {
            RawData = data,
            FilePath = filePath
        };

        if (data.Length < 16)
        {
            throw new InvalidDataException("File too small to be a valid NSBTX");
        }

        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);

        // Read main header
        file.Magic = Encoding.ASCII.GetString(reader.ReadBytes(4));
        if (file.Magic != "BTX0")
        {
            throw new InvalidDataException($"Invalid NSBTX magic: expected 'BTX0', got '{file.Magic}'");
        }

        reader.ReadUInt32(); // BOM (byte order mark)
        file.Version = reader.ReadUInt16();
        file.FileSize = reader.ReadUInt32();
        reader.ReadUInt16(); // Header size
        file.BlockCount = reader.ReadUInt16();

        // Try to parse texture and palette names from TEX0 block
        // This is a simplified parser - a full implementation would need more details
        try
        {
            file.TextureNames = ExtractNames(data, "TEX");
            file.PaletteNames = ExtractNames(data, "PAL");
        }
        catch
        {
            // If parsing fails, just leave the lists empty
        }

        return file;
    }

    /// <summary>
    /// Saves the NSBTX file to disk
    /// </summary>
    public void SaveToFile(string path)
    {
        File.WriteAllBytes(path, RawData);
    }

    /// <summary>
    /// Creates a copy of this NSBTX file
    /// </summary>
    public NsbtxFile Clone()
    {
        return new NsbtxFile
        {
            Magic = Magic,
            Version = Version,
            FileSize = FileSize,
            BlockCount = BlockCount,
            TextureNames = new List<string>(TextureNames),
            PaletteNames = new List<string>(PaletteNames),
            RawData = (byte[])RawData.Clone(),
            FilePath = FilePath
        };
    }

    /// <summary>
    /// Extracts resource names from the NSBTX file
    /// This is a simplified implementation
    /// </summary>
    private static List<string> ExtractNames(byte[] data, string section)
    {
        var names = new List<string>();

        // Search for the section marker in the file
        string marker = section + "\0";
        byte[] markerBytes = Encoding.ASCII.GetBytes(marker);

        // Simple search through file data
        // A full implementation would properly parse the name dictionary structure
        for (int i = 0; i < data.Length - markerBytes.Length; i++)
        {
            bool found = true;
            for (int j = 0; j < markerBytes.Length; j++)
            {
                if (data[i + j] != markerBytes[j])
                {
                    found = false;
                    break;
                }
            }

            if (found)
            {
                // Try to extract null-terminated strings after the marker
                int offset = i + markerBytes.Length;
                while (offset < data.Length && names.Count < 256) // Max 256 names
                {
                    var name = ReadNullTerminatedString(data, offset, out int bytesRead);
                    if (bytesRead == 0 || string.IsNullOrWhiteSpace(name))
                        break;

                    // Only add if it looks like a valid name (alphanumeric + underscore)
                    if (name.Length > 0 && name.Length < 64 &&
                        name.All(c => char.IsLetterOrDigit(c) || c == '_' || c == '-'))
                    {
                        names.Add(name);
                    }

                    offset += bytesRead;
                }
                break;
            }
        }

        return names;
    }

    /// <summary>
    /// Reads a null-terminated string from a byte array
    /// </summary>
    private static string ReadNullTerminatedString(byte[] data, int offset, out int bytesRead)
    {
        bytesRead = 0;
        if (offset >= data.Length)
            return string.Empty;

        var bytes = new List<byte>();
        while (offset + bytesRead < data.Length && data[offset + bytesRead] != 0)
        {
            bytes.Add(data[offset + bytesRead]);
            bytesRead++;

            // Safety limit
            if (bytesRead > 255)
                break;
        }

        if (offset + bytesRead < data.Length && data[offset + bytesRead] == 0)
            bytesRead++; // Include the null terminator

        return Encoding.ASCII.GetString(bytes.ToArray());
    }
}
