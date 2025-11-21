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

        if (tex0Offset + 64 > data.Length)
        {
            AppLogger.Warn($"[NSBTX] TEX0 offset out of bounds (need at least 64 bytes)");
            return;
        }

        using var ms = new MemoryStream(data, tex0Offset, data.Length - tex0Offset);
        using var reader = new BinaryReader(ms);

        // Read TEX0 header
        string stamp = Encoding.ASCII.GetString(reader.ReadBytes(4));
        AppLogger.Debug($"[NSBTX] TEX0 stamp: '{stamp}' at offset {tex0Offset}");

        if (stamp != "TEX0")
        {
            AppLogger.Warn($"[NSBTX] Invalid TEX0 stamp: '{stamp}'");
            return;
        }

        uint sectionSize = reader.ReadUInt32();
        AppLogger.Debug($"[NSBTX] @0x08: sectionSize = {sectionSize}");

        uint unknown1 = reader.ReadUInt32();
        AppLogger.Debug($"[NSBTX] @0x0C: unknown1 = 0x{unknown1:X8}");

        ushort textureDataSizeShr3 = reader.ReadUInt16();
        AppLogger.Debug($"[NSBTX] @0x10: textureDataSizeShr3 = {textureDataSizeShr3} (real size: {textureDataSizeShr3 << 3})");

        ushort texturesOff = reader.ReadUInt16();
        AppLogger.Debug($"[NSBTX] @0x12: texturesOff = {texturesOff} (absolute: {tex0Offset + texturesOff})");

        ushort compressedTextureSizeShr3 = reader.ReadUInt16();
        AppLogger.Debug($"[NSBTX] @0x14: compressedTextureSizeShr3 = {compressedTextureSizeShr3}");

        ushort compressedTexturesOff = reader.ReadUInt16();
        AppLogger.Debug($"[NSBTX] @0x16: compressedTexturesOff = {compressedTexturesOff}");

        uint compressedTextureInfoOff = reader.ReadUInt32();
        AppLogger.Debug($"[NSBTX] @0x18: compressedTextureInfoOff = {compressedTextureInfoOff}");

        uint unknown2 = reader.ReadUInt32();
        AppLogger.Debug($"[NSBTX] @0x1C: unknown2 = 0x{unknown2:X8}");

        uint textureDataOff = reader.ReadUInt32();
        AppLogger.Debug($"[NSBTX] @0x20: textureDataOff = {textureDataOff}");

        uint textureInfoOff = reader.ReadUInt32();
        AppLogger.Debug($"[NSBTX] @0x24: textureInfoOff = {textureInfoOff}");

        uint unknown3 = reader.ReadUInt32();
        AppLogger.Debug($"[NSBTX] @0x28: unknown3 = 0x{unknown3:X8}");

        uint paletteDataSizeShr3 = reader.ReadUInt32();
        AppLogger.Debug($"[NSBTX] @0x2C: paletteDataSizeShr3 = {paletteDataSizeShr3} (real size: {paletteDataSizeShr3 << 3})");

        uint paletteDataOff = reader.ReadUInt32();
        AppLogger.Debug($"[NSBTX] @0x30: paletteDataOff = {paletteDataOff}");

        uint paletteInfoOff = reader.ReadUInt32();
        AppLogger.Debug($"[NSBTX] @0x34: paletteInfoOff = {paletteInfoOff}");

        // Current position should be at 0x38
        AppLogger.Debug($"[NSBTX] Current position after header: 0x{ms.Position:X} (expected 0x38)");

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

        // Parse palette names - try paletteInfoOff instead of after paletteDataOff
        if (paletteInfoOff > 0 && paletteInfoOff < data.Length - tex0Offset)
        {
            AppLogger.Debug($"[NSBTX] Parsing palettes at paletteInfoOff: {tex0Offset + (int)paletteInfoOff}");
            file.PaletteNames = ParseNameList(data, tex0Offset + (int)paletteInfoOff);
            AppLogger.Info($"[NSBTX] Found {file.PaletteNames.Count} palettes");
        }
        else
        {
            AppLogger.Warn($"[NSBTX] Invalid palette info offset: {paletteInfoOff}");
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
