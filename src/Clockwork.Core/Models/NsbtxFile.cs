using System.Text;
using Clockwork.Core.Logging;

namespace Clockwork.Core.Models;

/// <summary>
/// Texture information extracted from NSBTX
/// </summary>
public class TextureInfo
{
    public int TextureOffset { get; set; }
    public ushort Parameters { get; set; }
    public byte Width { get; set; }
    public byte Height { get; set; }
    public int Format { get; set; }
    public bool Color0Transparent { get; set; }
    public int ActualWidth => 8 << (Width & 0x07);
    public int ActualHeight => 8 << (Height & 0x07);
}

/// <summary>
/// Palette information extracted from NSBTX
/// </summary>
public class PaletteInfo
{
    public int PaletteOffset { get; set; }
    public ushort Color0 { get; set; }
}

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
    /// List of texture information
    /// </summary>
    public List<TextureInfo> Textures { get; set; } = new();

    /// <summary>
    /// List of palette information
    /// </summary>
    public List<PaletteInfo> Palettes { get; set; } = new();

    /// <summary>
    /// Raw file data (for re-export without modification)
    /// </summary>
    public byte[] RawData { get; set; } = Array.Empty<byte>();

    /// <summary>
    /// File path this NSBTX was loaded from
    /// </summary>
    public string FilePath { get; set; } = string.Empty;

    /// <summary>
    /// Offset to TEX0 block in file
    /// </summary>
    public int Tex0Offset { get; set; }

    /// <summary>
    /// Offset to texture data within TEX0 block
    /// </summary>
    public uint TextureDataOffset { get; set; }

    /// <summary>
    /// Offset to palette data within TEX0 block
    /// </summary>
    public uint PaletteDataOffset { get; set; }

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

        // Store offsets for later use
        file.Tex0Offset = tex0Offset;
        file.TextureDataOffset = texDataOffset;

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

        // Store palette data offset
        file.PaletteDataOffset = palDataOffset;

        // Parse texture names using dictionary offset
        if (texDictOffset > 0 && texDictOffset < data.Length - tex0Offset)
        {
            AppLogger.Debug($"[NSBTX] Parsing textures at offset {tex0Offset + texDictOffset}");
            (file.TextureNames, file.Textures) = ParseTextureList(data, tex0Offset + texDictOffset);
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
            (file.PaletteNames, file.Palettes) = ParsePaletteList(data, tex0Offset + palDictOffset);
            AppLogger.Info($"[NSBTX] Found {file.PaletteNames.Count} palettes");
        }
        else
        {
            AppLogger.Warn($"[NSBTX] Invalid palette dictionary offset: {palDictOffset}");
        }
    }

    /// <summary>
    /// Parses texture list with names and info (texture entries are 8 bytes each)
    /// </summary>
    private static (List<string>, List<TextureInfo>) ParseTextureList(byte[] data, int offset)
    {
        var names = new List<string>();
        var textures = new List<TextureInfo>();

        AppLogger.Debug($"[NSBTX] ParseTextureList: offset={offset}");

        if (offset + 4 > data.Length)
        {
            AppLogger.Warn($"[NSBTX] Texture list offset out of bounds");
            return (names, textures);
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
            return (names, textures);
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

        // Read texture info entries (8 bytes each: offset, params, width, unk1, height, unk2)
        for (int i = 0; i < numObjs; i++)
        {
            var texInfo = new TextureInfo
            {
                TextureOffset = reader.ReadUInt16() << 3,
                Parameters = reader.ReadUInt16(),
                Width = reader.ReadByte()
            };
            reader.ReadByte(); // Unknown1
            texInfo.Height = reader.ReadByte();
            reader.ReadByte(); // Unknown2

            // Extract format from parameters (bits 26-28)
            texInfo.Format = (texInfo.Parameters >> 10) & 0x07;
            texInfo.Color0Transparent = (texInfo.Parameters & 0x20) != 0;

            textures.Add(texInfo);
        }

        AppLogger.Debug($"[NSBTX] Parsed {textures.Count} texture info entries, now at position {ms.Position}");

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

        AppLogger.Info($"[NSBTX] Parsed {names.Count} texture names and {textures.Count} texture info entries");
        return (names, textures);
    }

    /// <summary>
    /// Parses palette list with names and info (palette entries are 4 bytes each)
    /// </summary>
    private static (List<string>, List<PaletteInfo>) ParsePaletteList(byte[] data, int offset)
    {
        var names = new List<string>();
        var palettes = new List<PaletteInfo>();

        AppLogger.Debug($"[NSBTX] ParsePaletteList: offset={offset}");

        if (offset + 4 > data.Length)
        {
            AppLogger.Warn($"[NSBTX] Palette list offset out of bounds");
            return (names, palettes);
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
            return (names, palettes);
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

        // Read palette info entries (4 bytes each: offset + color0)
        for (int i = 0; i < numObjs; i++)
        {
            var palInfo = new PaletteInfo
            {
                PaletteOffset = reader.ReadUInt16() << 3,
                Color0 = reader.ReadUInt16()
            };
            palettes.Add(palInfo);
        }

        AppLogger.Debug($"[NSBTX] Parsed {palettes.Count} palette info entries, now at position {ms.Position}");

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

        AppLogger.Info($"[NSBTX] Parsed {names.Count} palette names and {palettes.Count} palette info entries");
        return (names, palettes);
    }

    /// <summary>
    /// Gets the raw texture data for a specific texture index
    /// </summary>
    public byte[]? GetTextureData(int textureIndex)
    {
        if (textureIndex < 0 || textureIndex >= Textures.Count)
            return null;

        var texInfo = Textures[textureIndex];
        int absoluteOffset = Tex0Offset + (int)TextureDataOffset + texInfo.TextureOffset;

        // Calculate texture data size based on format and dimensions
        int width = texInfo.ActualWidth;
        int height = texInfo.ActualHeight;
        int dataSize = texInfo.Format switch
        {
            1 => width * height, // A3I5 - 1 byte per pixel
            2 => (width * height) / 4, // 4-color - 2 bits per pixel
            3 => (width * height) / 2, // 16-color - 4 bits per pixel
            4 => width * height, // 256-color - 1 byte per pixel
            5 => (width * height) / 2, // 4x4 compressed - 4 bits per texel
            6 => width * height, // A5I3 - 1 byte per pixel
            7 => width * height * 2, // Direct color - 2 bytes per pixel
            _ => 0
        };

        if (dataSize == 0 || absoluteOffset + dataSize > RawData.Length)
            return null;

        byte[] textureData = new byte[dataSize];
        Array.Copy(RawData, absoluteOffset, textureData, 0, dataSize);
        return textureData;
    }

    /// <summary>
    /// Gets the palette data for a specific palette index (256 colors, RGB555 format)
    /// </summary>
    public ushort[]? GetPaletteData(int paletteIndex)
    {
        if (paletteIndex < 0 || paletteIndex >= Palettes.Count)
            return null;

        var palInfo = Palettes[paletteIndex];
        int absoluteOffset = Tex0Offset + (int)PaletteDataOffset + palInfo.PaletteOffset;

        // Palette is 256 colors * 2 bytes each (RGB555 format)
        int paletteSize = 256 * 2;

        if (absoluteOffset + paletteSize > RawData.Length)
            return null;

        ushort[] palette = new ushort[256];
        for (int i = 0; i < 256; i++)
        {
            int offset = absoluteOffset + i * 2;
            palette[i] = (ushort)(RawData[offset] | (RawData[offset + 1] << 8));
        }

        return palette;
    }

    /// <summary>
    /// Converts tiled texture data (8x8 tiles) to linear format
    /// Nintendo DS textures are stored as 8x8 pixel tiles
    /// </summary>
    private byte[] UntileTiledData(byte[] tiledData, int width, int height, int bytesPerPixel)
    {
        byte[] linear = new byte[tiledData.Length];
        int tileWidth = width / 8;
        int tileHeight = height / 8;

        for (int ty = 0; ty < tileHeight; ty++)
        {
            for (int tx = 0; tx < tileWidth; tx++)
            {
                int tileIndex = ty * tileWidth + tx;
                int tileOffset = tileIndex * 64 * bytesPerPixel; // 8x8 = 64 pixels per tile

                for (int py = 0; py < 8; py++)
                {
                    for (int px = 0; px < 8; px++)
                    {
                        int srcIdx = tileOffset + (py * 8 + px) * bytesPerPixel;
                        int dstX = tx * 8 + px;
                        int dstY = ty * 8 + py;
                        int dstIdx = (dstY * width + dstX) * bytesPerPixel;

                        if (srcIdx + bytesPerPixel <= tiledData.Length && dstIdx + bytesPerPixel <= linear.Length)
                        {
                            for (int b = 0; b < bytesPerPixel; b++)
                            {
                                linear[dstIdx + b] = tiledData[srcIdx + b];
                            }
                        }
                    }
                }
            }
        }

        return linear;
    }

    /// <summary>
    /// Decodes a texture to RGBA8888 format (ready for OpenGL)
    /// Returns null if texture cannot be decoded
    /// </summary>
    /// <param name="textureIndex">Index of the texture to decode</param>
    /// <param name="paletteIndex">Index of the palette to use (for paletted formats)</param>
    /// <param name="useTiledFormat">If true, treat texture as 8x8 tiled. If false, treat as linear.</param>
    public byte[]? DecodeTextureToRGBA(int textureIndex, int paletteIndex = 0, bool useTiledFormat = false)
    {
        if (textureIndex < 0 || textureIndex >= Textures.Count)
            return null;

        var texInfo = Textures[textureIndex];
        var textureData = GetTextureData(textureIndex);
        if (textureData == null)
            return null;

        int width = texInfo.ActualWidth;
        int height = texInfo.ActualHeight;

        // Apply tiling if requested
        switch (texInfo.Format)
        {
            case 1: // A3I5 (Translucent) - 1 byte per pixel
                if (useTiledFormat)
                    textureData = UntileTiledData(textureData, width, height, 1);
                return DecodeA3I5(textureData, width, height);

            case 2: // 4-Color Palette - 2 bits per pixel (4 pixels per byte)
                if (useTiledFormat)
                    return Decode4ColorPaletteTiled(textureData, width, height, paletteIndex);
                else
                    return Decode4ColorPaletteLinear(textureData, width, height, paletteIndex);

            case 3: // 16-Color Palette - 4 bits per pixel (2 pixels per byte)
                if (useTiledFormat)
                    return Decode16ColorPaletteTiled(textureData, width, height, paletteIndex);
                else
                    return Decode16ColorPaletteLinear(textureData, width, height, paletteIndex);

            case 4: // 256-Color Palette - 1 byte per pixel
                if (useTiledFormat)
                    textureData = UntileTiledData(textureData, width, height, 1);
                return Decode256ColorPalette(textureData, width, height, paletteIndex);

            case 6: // A5I3 (Translucent) - 1 byte per pixel
                if (useTiledFormat)
                    textureData = UntileTiledData(textureData, width, height, 1);
                return DecodeA5I3(textureData, width, height);

            case 7: // Direct Color (RGB555) - 2 bytes per pixel
                if (useTiledFormat)
                    textureData = UntileTiledData(textureData, width, height, 2);
                return DecodeDirectColor(textureData, width, height);

            default:
                AppLogger.Warn($"[NSBTX] Unsupported texture format: {texInfo.Format}");
                return null;
        }
    }

    /// <summary>
    /// Decodes a 256-color palette texture to RGBA8888
    /// </summary>
    private byte[]? Decode256ColorPalette(byte[] textureData, int width, int height, int paletteIndex)
    {
        var palette = GetPaletteData(paletteIndex);
        if (palette == null)
            return null;

        byte[] rgba = new byte[width * height * 4];

        for (int i = 0; i < textureData.Length; i++)
        {
            byte paletteIdx = textureData[i];
            ushort color555 = palette[paletteIdx];

            // Convert RGB555 to RGB888
            int r = ((color555 >> 0) & 0x1F) * 255 / 31;
            int g = ((color555 >> 5) & 0x1F) * 255 / 31;
            int b = ((color555 >> 10) & 0x1F) * 255 / 31;

            int rgbaIdx = i * 4;
            rgba[rgbaIdx + 0] = (byte)r;
            rgba[rgbaIdx + 1] = (byte)g;
            rgba[rgbaIdx + 2] = (byte)b;
            rgba[rgbaIdx + 3] = 255; // Full alpha
        }

        return rgba;
    }

    /// <summary>
    /// Decodes a 16-color palette texture to RGBA8888 (linear format)
    /// </summary>
    private byte[]? Decode16ColorPaletteLinear(byte[] textureData, int width, int height, int paletteIndex)
    {
        var palette = GetPaletteData(paletteIndex);
        if (palette == null)
            return null;

        byte[] rgba = new byte[width * height * 4];
        int pixelIdx = 0;

        // 2 pixels per byte (4 bits each), stored linearly
        for (int i = 0; i < textureData.Length && pixelIdx < width * height; i++)
        {
            byte packed = textureData[i];

            // First pixel (lower 4 bits)
            byte paletteIdx1 = (byte)(packed & 0x0F);
            ushort color1 = palette[paletteIdx1];
            rgba[pixelIdx * 4 + 0] = (byte)(((color1 >> 0) & 0x1F) * 255 / 31);
            rgba[pixelIdx * 4 + 1] = (byte)(((color1 >> 5) & 0x1F) * 255 / 31);
            rgba[pixelIdx * 4 + 2] = (byte)(((color1 >> 10) & 0x1F) * 255 / 31);
            rgba[pixelIdx * 4 + 3] = 255;
            pixelIdx++;

            if (pixelIdx >= width * height)
                break;

            // Second pixel (upper 4 bits)
            byte paletteIdx2 = (byte)((packed >> 4) & 0x0F);
            ushort color2 = palette[paletteIdx2];
            rgba[pixelIdx * 4 + 0] = (byte)(((color2 >> 0) & 0x1F) * 255 / 31);
            rgba[pixelIdx * 4 + 1] = (byte)(((color2 >> 5) & 0x1F) * 255 / 31);
            rgba[pixelIdx * 4 + 2] = (byte)(((color2 >> 10) & 0x1F) * 255 / 31);
            rgba[pixelIdx * 4 + 3] = 255;
            pixelIdx++;
        }

        return rgba;
    }

    /// <summary>
    /// Decodes a 16-color palette texture to RGBA8888 (tiled format)
    /// </summary>
    private byte[]? Decode16ColorPaletteTiled(byte[] textureData, int width, int height, int paletteIndex)
    {
        var palette = GetPaletteData(paletteIndex);
        if (palette == null)
            return null;

        byte[] rgba = new byte[width * height * 4];

        // Untile the 4-bit data (2 pixels per byte, so 0.5 bytes per pixel)
        int tileWidth = width / 8;
        int tileHeight = height / 8;

        for (int ty = 0; ty < tileHeight; ty++)
        {
            for (int tx = 0; tx < tileWidth; tx++)
            {
                int tileIndex = ty * tileWidth + tx;
                int tileOffset = tileIndex * 32; // 8x8 = 64 pixels, 4 bits each = 32 bytes per tile

                for (int py = 0; py < 8; py++)
                {
                    for (int px = 0; px < 8; px++)
                    {
                        int pixelInTile = py * 8 + px;
                        int byteInTile = pixelInTile / 2;
                        int srcIdx = tileOffset + byteInTile;

                        if (srcIdx < textureData.Length)
                        {
                            byte packed = textureData[srcIdx];
                            byte palIdx = (px % 2 == 0) ? (byte)(packed & 0x0F) : (byte)((packed >> 4) & 0x0F);

                            ushort color = palette[palIdx];
                            int dstX = tx * 8 + px;
                            int dstY = ty * 8 + py;
                            int dstIdx = (dstY * width + dstX) * 4;

                            rgba[dstIdx + 0] = (byte)(((color >> 0) & 0x1F) * 255 / 31);
                            rgba[dstIdx + 1] = (byte)(((color >> 5) & 0x1F) * 255 / 31);
                            rgba[dstIdx + 2] = (byte)(((color >> 10) & 0x1F) * 255 / 31);
                            rgba[dstIdx + 3] = 255;
                        }
                    }
                }
            }
        }

        return rgba;
    }

    /// <summary>
    /// Decodes a 4-color palette texture to RGBA8888 (linear format)
    /// </summary>
    private byte[]? Decode4ColorPaletteLinear(byte[] textureData, int width, int height, int paletteIndex)
    {
        var palette = GetPaletteData(paletteIndex);
        if (palette == null)
            return null;

        byte[] rgba = new byte[width * height * 4];
        int pixelIdx = 0;

        // 4 pixels per byte (2 bits each), stored linearly
        for (int i = 0; i < textureData.Length && pixelIdx < width * height; i++)
        {
            byte packed = textureData[i];

            for (int bit = 0; bit < 8; bit += 2)
            {
                byte paletteIdx = (byte)((packed >> bit) & 0x03);
                ushort color = palette[paletteIdx];

                rgba[pixelIdx * 4 + 0] = (byte)(((color >> 0) & 0x1F) * 255 / 31);
                rgba[pixelIdx * 4 + 1] = (byte)(((color >> 5) & 0x1F) * 255 / 31);
                rgba[pixelIdx * 4 + 2] = (byte)(((color >> 10) & 0x1F) * 255 / 31);
                rgba[pixelIdx * 4 + 3] = 255;
                pixelIdx++;

                if (pixelIdx >= width * height)
                    break;
            }
        }

        return rgba;
    }

    /// <summary>
    /// Decodes a 4-color palette texture to RGBA8888 (tiled format)
    /// </summary>
    private byte[]? Decode4ColorPaletteTiled(byte[] textureData, int width, int height, int paletteIndex)
    {
        var palette = GetPaletteData(paletteIndex);
        if (palette == null)
            return null;

        byte[] rgba = new byte[width * height * 4];

        // Untile the 2-bit data (4 pixels per byte, so 0.25 bytes per pixel)
        int tileWidth = width / 8;
        int tileHeight = height / 8;

        for (int ty = 0; ty < tileHeight; ty++)
        {
            for (int tx = 0; tx < tileWidth; tx++)
            {
                int tileIndex = ty * tileWidth + tx;
                int tileOffset = tileIndex * 16; // 8x8 = 64 pixels, 2 bits each = 16 bytes per tile

                for (int py = 0; py < 8; py++)
                {
                    for (int px = 0; px < 8; px++)
                    {
                        int pixelInTile = py * 8 + px;
                        int byteInTile = pixelInTile / 4;
                        int bitInByte = (pixelInTile % 4) * 2;
                        int srcIdx = tileOffset + byteInTile;

                        if (srcIdx < textureData.Length)
                        {
                            byte packed = textureData[srcIdx];
                            byte palIdx = (byte)((packed >> bitInByte) & 0x03);

                            ushort color = palette[palIdx];
                            int dstX = tx * 8 + px;
                            int dstY = ty * 8 + py;
                            int dstIdx = (dstY * width + dstX) * 4;

                            rgba[dstIdx + 0] = (byte)(((color >> 0) & 0x1F) * 255 / 31);
                            rgba[dstIdx + 1] = (byte)(((color >> 5) & 0x1F) * 255 / 31);
                            rgba[dstIdx + 2] = (byte)(((color >> 10) & 0x1F) * 255 / 31);
                            rgba[dstIdx + 3] = 255;
                        }
                    }
                }
            }
        }

        return rgba;
    }

    /// <summary>
    /// Decodes a direct color (RGB555) texture to RGBA8888
    /// </summary>
    private byte[]? DecodeDirectColor(byte[] textureData, int width, int height)
    {
        byte[] rgba = new byte[width * height * 4];
        int pixelIdx = 0;

        // 2 bytes per pixel (RGB555)
        for (int i = 0; i < textureData.Length; i += 2)
        {
            ushort color555 = (ushort)(textureData[i] | (textureData[i + 1] << 8));

            rgba[pixelIdx * 4 + 0] = (byte)(((color555 >> 0) & 0x1F) * 255 / 31);
            rgba[pixelIdx * 4 + 1] = (byte)(((color555 >> 5) & 0x1F) * 255 / 31);
            rgba[pixelIdx * 4 + 2] = (byte)(((color555 >> 10) & 0x1F) * 255 / 31);
            rgba[pixelIdx * 4 + 3] = 255;
            pixelIdx++;
        }

        return rgba;
    }

    /// <summary>
    /// Decodes an A3I5 (3-bit alpha, 5-bit intensity) texture to RGBA8888
    /// Format: 1 byte per pixel, lower 5 bits = intensity, upper 3 bits = alpha
    /// </summary>
    private byte[] DecodeA3I5(byte[] textureData, int width, int height)
    {
        byte[] rgba = new byte[width * height * 4];

        for (int i = 0; i < textureData.Length && i < width * height; i++)
        {
            byte pixel = textureData[i];

            // Extract intensity (lower 5 bits) and alpha (upper 3 bits)
            int intensity = pixel & 0x1F;       // 0-31
            int alpha = (pixel >> 5) & 0x07;    // 0-7

            // Scale to 0-255 range
            byte intensityScaled = (byte)((intensity * 255) / 31);
            byte alphaScaled = (byte)((alpha * 255) / 7);

            // Use intensity for RGB (grayscale)
            int rgbaIdx = i * 4;
            rgba[rgbaIdx + 0] = intensityScaled; // R
            rgba[rgbaIdx + 1] = intensityScaled; // G
            rgba[rgbaIdx + 2] = intensityScaled; // B
            rgba[rgbaIdx + 3] = alphaScaled;     // A
        }

        return rgba;
    }

    /// <summary>
    /// Decodes an A5I3 (5-bit alpha, 3-bit intensity) texture to RGBA8888
    /// Format: 1 byte per pixel, lower 3 bits = intensity, upper 5 bits = alpha
    /// </summary>
    private byte[] DecodeA5I3(byte[] textureData, int width, int height)
    {
        byte[] rgba = new byte[width * height * 4];

        for (int i = 0; i < textureData.Length && i < width * height; i++)
        {
            byte pixel = textureData[i];

            // Extract intensity (lower 3 bits) and alpha (upper 5 bits)
            int intensity = pixel & 0x07;       // 0-7
            int alpha = (pixel >> 3) & 0x1F;    // 0-31

            // Scale to 0-255 range
            byte intensityScaled = (byte)((intensity * 255) / 7);
            byte alphaScaled = (byte)((alpha * 255) / 31);

            // Use intensity for RGB (grayscale)
            int rgbaIdx = i * 4;
            rgba[rgbaIdx + 0] = intensityScaled; // R
            rgba[rgbaIdx + 1] = intensityScaled; // G
            rgba[rgbaIdx + 2] = intensityScaled; // B
            rgba[rgbaIdx + 3] = alphaScaled;     // A
        }

        return rgba;
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
            Textures = new List<TextureInfo>(Textures),
            Palettes = new List<PaletteInfo>(Palettes),
            RawData = (byte[])RawData.Clone(),
            FilePath = FilePath,
            Tex0Offset = Tex0Offset,
            TextureDataOffset = TextureDataOffset,
            PaletteDataOffset = PaletteDataOffset
        };
    }
}
