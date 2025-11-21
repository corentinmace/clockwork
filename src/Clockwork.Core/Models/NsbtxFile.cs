using System.Text;
using Clockwork.Core.Logging;

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

        // Read main header (BTX0)
        file.Magic = Encoding.ASCII.GetString(reader.ReadBytes(4));
        if (file.Magic != "BTX0")
        {
            throw new InvalidDataException($"Invalid NSBTX magic: expected 'BTX0', got '{file.Magic}'");
        }

        ushort bom = reader.ReadUInt16(); // BOM (byte order mark) 0xFEFF - u16, NOT u32!
        file.Version = reader.ReadUInt16();
        file.FileSize = reader.ReadUInt32();
        ushort headerSize = reader.ReadUInt16();
        file.BlockCount = reader.ReadUInt16();

        AppLogger.Debug($"[NSBTX] BOM: 0x{bom:X4}");

        AppLogger.Debug($"[NSBTX] BTX0 header: version={file.Version}, fileSize={file.FileSize}, headerSize={headerSize}, blockCount={file.BlockCount}");

        // Read block offsets
        if (file.BlockCount > 0 && ms.Position + 4 <= ms.Length)
        {
            uint tex0Offset = reader.ReadUInt32();
            AppLogger.Debug($"[NSBTX] TEX0 offset from header: {tex0Offset}");

            // Parse TEX0 block to extract texture and palette names
            try
            {
                ParseTex0Block(data, (int)tex0Offset, file);
            }
            catch (Exception ex)
            {
                AppLogger.Error($"[NSBTX] Failed to parse TEX0 block: {ex.Message}");
            }
        }
        else
        {
            AppLogger.Warn($"[NSBTX] No blocks in file or cannot read block offset");
        }

        return file;
    }

    /// <summary>
    /// Parses the TEX0 block to extract texture and palette names
    /// </summary>
    private static void ParseTex0Block(byte[] data, int tex0Offset, NsbtxFile file)
    {
        AppLogger.Debug($"[NSBTX] ParseTex0Block: offset={tex0Offset}, dataLength={data.Length}");

        if (tex0Offset + 16 > data.Length)
        {
            AppLogger.Warn($"[NSBTX] TEX0 offset out of bounds");
            return;
        }

        using var ms = new MemoryStream(data, tex0Offset, data.Length - tex0Offset);
        using var reader = new BinaryReader(ms);

        // Read TEX0 header
        string stamp = Encoding.ASCII.GetString(reader.ReadBytes(4));
        AppLogger.Debug($"[NSBTX] TEX0 stamp: '{stamp}'");

        if (stamp != "TEX0")
        {
            AppLogger.Warn($"[NSBTX] Invalid TEX0 stamp: '{stamp}'");
            return;
        }

        uint sectionSize = reader.ReadUInt32();
        reader.ReadUInt32(); // unknown

        reader.ReadUInt16(); // texture_data_size_shr_3
        ushort texturesOff = reader.ReadUInt16(); // offset to TextureList (relative to TEX0)

        reader.ReadUInt16(); // compressed_texture_data_size_shr_3
        reader.ReadUInt16(); // compressed_textures_off

        reader.ReadUInt32(); // compressed_texture_info_off
        reader.ReadUInt32(); // unknown

        uint textureDataSize = reader.ReadUInt32(); // texture data size
        reader.ReadUInt32(); // texture_info_off

        reader.ReadUInt32(); // unknown
        uint paletteDataSize = reader.ReadUInt32(); // palette data size shr 3

        reader.ReadUInt32(); // palette_info_off
        uint palettesOff = reader.ReadUInt32(); // offset to PaletteList (relative to TEX0)

        AppLogger.Debug($"[NSBTX] TEX0 header: sectionSize={sectionSize}, texturesOff={texturesOff}, palettesOff={palettesOff}");

        // Parse texture names
        if (texturesOff > 0 && texturesOff < data.Length - tex0Offset)
        {
            AppLogger.Debug($"[NSBTX] Parsing textures at offset {tex0Offset + texturesOff}");
            file.TextureNames = ParseNameList(data, tex0Offset + texturesOff);
            AppLogger.Info($"[NSBTX] Found {file.TextureNames.Count} textures");
        }
        else
        {
            AppLogger.Warn($"[NSBTX] Invalid textures offset: {texturesOff}");
        }

        // Parse palette names
        if (palettesOff > 0 && palettesOff < data.Length - tex0Offset)
        {
            AppLogger.Debug($"[NSBTX] Parsing palettes at offset {tex0Offset + (int)palettesOff}");
            file.PaletteNames = ParseNameList(data, tex0Offset + (int)palettesOff);
            AppLogger.Info($"[NSBTX] Found {file.PaletteNames.Count} palettes");
        }
        else
        {
            AppLogger.Warn($"[NSBTX] Invalid palettes offset: {palettesOff}");
        }
    }

    /// <summary>
    /// Parses a NameList structure to extract names
    /// NameList structure:
    /// - 1 byte: dummy
    /// - 1 byte: count
    /// - 2 bytes: size
    /// - ... (header data)
    /// - data array
    /// - names array (16 bytes each, null-padded ASCII)
    /// </summary>
    private static List<string> ParseNameList(byte[] data, int offset)
    {
        var names = new List<string>();

        AppLogger.Debug($"[NSBTX] ParseNameList: offset={offset}, dataLength={data.Length}");

        if (offset + 4 > data.Length)
        {
            AppLogger.Warn($"[NSBTX] NameList offset out of bounds");
            return names;
        }

        using var ms = new MemoryStream(data, offset, data.Length - offset);
        using var reader = new BinaryReader(ms);

        byte dummy = reader.ReadByte();
        byte count = reader.ReadByte();
        ushort size = reader.ReadUInt16();

        AppLogger.Debug($"[NSBTX] NameList header: dummy={dummy}, count={count}, size={size}");

        if (count == 0)
        {
            AppLogger.Debug($"[NSBTX] NameList count is 0");
            return names;
        }

        // The names are located at a specific offset in the NameList
        // Skip to the names section
        // Structure: header (variable) + data section + names section

        // Read unknown header
        ushort unknownHeaderSize = reader.ReadUInt16();
        ushort elementSize = reader.ReadUInt16();
        ushort dataSectionSize = reader.ReadUInt16();

        AppLogger.Debug($"[NSBTX] NameList sizes: unknownHeaderSize={unknownHeaderSize}, elementSize={elementSize}, dataSectionSize={dataSectionSize}");

        // Skip the unknown header section
        if (unknownHeaderSize > 6)
        {
            reader.ReadBytes(unknownHeaderSize - 6);
        }

        // Skip the data section
        reader.ReadBytes(dataSectionSize);

        AppLogger.Debug($"[NSBTX] At position {ms.Position}, reading {count} names");

        // Now we're at the names section
        // Read 'count' names, each 16 bytes
        for (int i = 0; i < count; i++)
        {
            if (ms.Position + 16 > ms.Length)
            {
                AppLogger.Warn($"[NSBTX] Cannot read name {i}, out of bounds (pos={ms.Position}, length={ms.Length})");
                break;
            }

            byte[] nameBytes = reader.ReadBytes(16);

            // Find the null terminator
            int nullIndex = Array.IndexOf(nameBytes, (byte)0);
            int length = nullIndex >= 0 ? nullIndex : 16;

            if (length > 0)
            {
                string name = Encoding.ASCII.GetString(nameBytes, 0, length).Trim();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    AppLogger.Debug($"[NSBTX] Name {i}: '{name}'");
                    names.Add(name);
                }
            }
        }

        AppLogger.Debug($"[NSBTX] ParseNameList completed: {names.Count} names found");

        return names;
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
}
