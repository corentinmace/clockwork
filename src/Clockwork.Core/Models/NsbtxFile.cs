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
        AppLogger.Debug($"[NSBTX] Section size: {sectionSize}");

        // Texture Information block
        uint texUnk = reader.ReadUInt32();
        ushort texDataSize = (ushort)(reader.ReadUInt16() << 3);
        ushort texDictOffset = reader.ReadUInt16();
        ushort texUnk2 = reader.ReadUInt16();
        reader.BaseStream.Position += 2; // Skip padding
        uint texDataOffset = reader.ReadUInt32();

        AppLogger.Debug($"[NSBTX] Texture info: dictOffset={texDictOffset}, dataSize={texDataSize}, dataOffset={texDataOffset}");

        // Compressed Texture Information block
        uint compTexUnk = reader.ReadUInt32();
        ushort compTexDataSize = (ushort)(reader.ReadUInt16() << 3);
        ushort compTexDictOffset = reader.ReadUInt16();
        ushort compTexUnk2 = reader.ReadUInt16();
        reader.BaseStream.Position += 2; // Skip padding
        uint compTexDataOffset = reader.ReadUInt32();
        uint compTexPalIndex = reader.ReadUInt32();

        // Palette Information block
        uint palUnk = reader.ReadUInt32();
        ushort palDataSize = (ushort)(reader.ReadUInt16() << 3);
        ushort palUnk2 = reader.ReadUInt16();
        ushort palDictOffset = reader.ReadUInt16();
        reader.BaseStream.Position += 2; // Skip padding
        uint palDataOffset = reader.ReadUInt32();

        AppLogger.Debug($"[NSBTX] Palette info: dictOffset={palDictOffset}, dataSize={palDataSize}, dataOffset={palDataOffset}");
        AppLogger.Debug($"[NSBTX] Current position after header: 0x{ms.Position:X}");

        // Parse texture names using dictionary offset
        if (texDictOffset > 0 && texDictOffset < data.Length - tex0Offset)
        {
            AppLogger.Debug($"[NSBTX] Parsing textures at offset {tex0Offset + texDictOffset}");
            file.TextureNames = ParseTextureNameList(data, tex0Offset + texDictOffset);
            AppLogger.Info($"[NSBTX] Found {file.TextureNames.Count} textures");
        }
        else
        {
            AppLogger.Warn($"[NSBTX] Invalid texture dictionary offset: {texDictOffset}");
        }

        // Parse palette names using dictionary offset
        if (palDictOffset > 0 && palDictOffset < data.Length - tex0Offset)
        {
            AppLogger.Debug($"[NSBTX] Parsing palettes at offset {tex0Offset + palDictOffset}");
            file.PaletteNames = ParsePaletteNameList(data, tex0Offset + palDictOffset);
            AppLogger.Info($"[NSBTX] Found {file.PaletteNames.Count} palettes");
        }
        else
        {
            AppLogger.Warn($"[NSBTX] Invalid palette dictionary offset: {palDictOffset}");
        }
    }

    /// <summary>
    /// Parses texture name list (texture entries are 8 bytes each)
    /// </summary>
    private static List<string> ParseTextureNameList(byte[] data, int offset)
    {
        var names = new List<string>();

        AppLogger.Debug($"[NSBTX] ParseTextureNameList: offset={offset}");

        if (offset + 4 > data.Length)
        {
            AppLogger.Warn($"[NSBTX] Texture name list offset out of bounds");
            return names;
        }

        using var ms = new MemoryStream(data, offset, data.Length - offset);
        using var reader = new BinaryReader(ms);

        // Read main header
        byte dummy = reader.ReadByte();
        byte numObjs = reader.ReadByte();
        ushort sectionSize = reader.ReadUInt16();

        AppLogger.Debug($"[NSBTX] Texture list: count={numObjs}, sectionSize={sectionSize}");

        if (numObjs == 0)
        {
            return names;
        }

        // Read unknown block header
        ushort headerSize = reader.ReadUInt16();
        ushort blockSectionSize = reader.ReadUInt16();
        uint constant = reader.ReadUInt32();

        AppLogger.Debug($"[NSBTX] Unknown block: headerSize={headerSize}, blockSectionSize={blockSectionSize}, constant=0x{constant:X8}");

        // Skip unknown block data (2 shorts per texture = 4 bytes each)
        int unknownDataSize = numObjs * 4;
        reader.ReadBytes(unknownDataSize);
        AppLogger.Debug($"[NSBTX] Skipped {unknownDataSize} bytes of unknown block data");

        // Read and skip info block header (header_size + data_size = 4 bytes)
        ushort infoHeaderSize = reader.ReadUInt16();
        ushort infoDataSize = reader.ReadUInt16();
        AppLogger.Debug($"[NSBTX] Info block: headerSize={infoHeaderSize}, dataSize={infoDataSize}");

        // Skip texture info entries (8 bytes each: offset, params, width, unk1, height, unk2)
        int textureInfoSize = numObjs * 8;
        reader.ReadBytes(textureInfoSize);

        AppLogger.Debug($"[NSBTX] Skipped {textureInfoSize} bytes of texture info, now at position {ms.Position}");

        // Read texture names (16 bytes each, ASCII)
        for (int i = 0; i < numObjs; i++)
        {
            if (ms.Position + 16 > ms.Length)
            {
                AppLogger.Warn($"[NSBTX] Cannot read texture name {i}, out of bounds");
                break;
            }

            long posBeforeRead = ms.Position;
            byte[] nameBytes = reader.ReadBytes(16);

            // Log raw bytes for first 5 and last 5 entries
            if (i < 5 || i >= numObjs - 5)
            {
                string hexBytes = BitConverter.ToString(nameBytes).Replace("-", " ");
                AppLogger.Debug($"[NSBTX] Texture {i} raw bytes at 0x{posBeforeRead:X}: {hexBytes}");
            }

            int nullIndex = Array.IndexOf(nameBytes, (byte)0);
            int length = nullIndex >= 0 ? nullIndex : 16;

            if (length > 0)
            {
                string name = Encoding.ASCII.GetString(nameBytes, 0, length);
                names.Add(name);
                if (i < 5 || i >= numObjs - 5)
                {
                    AppLogger.Debug($"[NSBTX] Texture {i}: '{name}' (length={length})");
                }
            }
        }

        AppLogger.Info($"[NSBTX] Parsed {names.Count} texture names");
        return names;
    }

    /// <summary>
    /// Parses palette name list (palette entries are 4 bytes each)
    /// </summary>
    private static List<string> ParsePaletteNameList(byte[] data, int offset)
    {
        var names = new List<string>();

        AppLogger.Debug($"[NSBTX] ParsePaletteNameList: offset={offset}");

        if (offset + 4 > data.Length)
        {
            AppLogger.Warn($"[NSBTX] Palette name list offset out of bounds");
            return names;
        }

        using var ms = new MemoryStream(data, offset, data.Length - offset);
        using var reader = new BinaryReader(ms);

        // Read main header
        byte dummy = reader.ReadByte();
        byte numObjs = reader.ReadByte();
        ushort sectionSize = reader.ReadUInt16();

        AppLogger.Debug($"[NSBTX] Palette list: count={numObjs}, sectionSize={sectionSize}");

        if (numObjs == 0)
        {
            return names;
        }

        // Read unknown block header
        ushort headerSize = reader.ReadUInt16();
        ushort blockSectionSize = reader.ReadUInt16();
        uint constant = reader.ReadUInt32();

        AppLogger.Debug($"[NSBTX] Unknown block: headerSize={headerSize}, blockSectionSize={blockSectionSize}, constant=0x{constant:X8}");

        // Skip unknown block data (2 shorts per palette = 4 bytes each)
        int unknownDataSize = numObjs * 4;
        reader.ReadBytes(unknownDataSize);
        AppLogger.Debug($"[NSBTX] Skipped {unknownDataSize} bytes of unknown block data");

        // Read and skip info block header (header_size + data_size = 4 bytes)
        ushort infoHeaderSize = reader.ReadUInt16();
        ushort infoDataSize = reader.ReadUInt16();
        AppLogger.Debug($"[NSBTX] Info block: headerSize={infoHeaderSize}, dataSize={infoDataSize}");

        // Skip palette info entries (4 bytes each: offset + color0)
        int paletteInfoSize = numObjs * 4;
        reader.ReadBytes(paletteInfoSize);

        AppLogger.Debug($"[NSBTX] Skipped {paletteInfoSize} bytes of palette info, now at position {ms.Position}");

        // Read palette names (16 bytes each, ASCII)
        for (int i = 0; i < numObjs; i++)
        {
            if (ms.Position + 16 > ms.Length)
            {
                AppLogger.Warn($"[NSBTX] Cannot read palette name {i}, out of bounds");
                break;
            }

            long posBeforeRead = ms.Position;
            byte[] nameBytes = reader.ReadBytes(16);

            // Log raw bytes for first 5 and last 5 entries
            if (i < 5 || i >= numObjs - 5)
            {
                string hexBytes = BitConverter.ToString(nameBytes).Replace("-", " ");
                AppLogger.Debug($"[NSBTX] Palette {i} raw bytes at 0x{posBeforeRead:X}: {hexBytes}");
            }

            int nullIndex = Array.IndexOf(nameBytes, (byte)0);
            int length = nullIndex >= 0 ? nullIndex : 16;

            if (length > 0)
            {
                string name = Encoding.ASCII.GetString(nameBytes, 0, length);
                names.Add(name);
                if (i < 5 || i >= numObjs - 5)
                {
                    AppLogger.Debug($"[NSBTX] Palette {i}: '{name}' (length={length})");
                }
            }
        }

        AppLogger.Info($"[NSBTX] Parsed {names.Count} palette names");
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
