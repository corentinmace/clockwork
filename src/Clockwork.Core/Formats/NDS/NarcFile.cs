using System.IO;
using Clockwork.Core.Logging;

namespace Clockwork.Core.Formats.NDS;

/// <summary>
/// Represents a NARC (Nitro Archive) file used in Nintendo DS games.
/// Handles packing and unpacking of NARC archives.
/// Based on LiTRE implementation.
/// </summary>
public class NarcFile : IDisposable
{
    // NARC format constants
    private const uint NARC_FILE_MAGIC_NUM = 0x4352414E; // "NARC"
    private const uint FILE_ALLOCATION_TABLE_HEADER_LENGTH = 12;
    private const uint FILE_ALLOCATION_TABLE_ELEMENT_LENGTH = 8;
    private const uint FILE_IMAGE_HEADER_SIZE = 8;

    public string Name { get; set; } = string.Empty;
    public MemoryStream[] Elements { get; set; } = Array.Empty<MemoryStream>();

    /// <summary>
    /// Creates a NARC archive from a folder containing files.
    /// </summary>
    /// <param name="dirPath">Path to the folder containing the files to pack</param>
    /// <returns>A NarcFile object with all files loaded</returns>
    public static NarcFile FromFolder(string dirPath)
    {
        var narc = new NarcFile
        {
            Name = Path.GetFileNameWithoutExtension(dirPath)
        };

        string[] fileNames = Directory.GetFiles(dirPath, "*.*", SearchOption.AllDirectories);
        Array.Sort(fileNames, StringComparer.OrdinalIgnoreCase);

        uint numberOfElements = (uint)fileNames.Length;
        narc.Elements = new MemoryStream[numberOfElements];

        // Load files in parallel for performance
        Parallel.For(0, numberOfElements, i =>
        {
            using var fs = File.OpenRead(fileNames[i]);
            var ms = new MemoryStream();
            byte[] buffer = new byte[fs.Length];
            fs.Read(buffer, 0, (int)fs.Length);
            ms.Write(buffer, 0, (int)fs.Length);
            narc.Elements[i] = ms;
        });

        AppLogger.Debug($"Loaded NARC \"{narc.Name}\" with {numberOfElements} elements from folder: {dirPath}");

        return narc;
    }

    /// <summary>
    /// Saves the NARC archive to a file.
    /// </summary>
    /// <param name="filePath">Path where the NARC file will be saved</param>
    public void Save(string filePath)
    {
        uint fileSizeOffset, fileImageSizeOffset, curOffset;

        using var bw = new BinaryWriter(File.Create(filePath));

        // Write NARC Section (Header)
        bw.Write(NARC_FILE_MAGIC_NUM);      // Magic number "NARC"
        bw.Write(0x0100FFFE);                // Version + byte order
        fileSizeOffset = (uint)bw.BaseStream.Position;
        bw.Write((uint)0x0);                 // File size (will be written later)
        bw.Write((ushort)16);                // Full size of header section
        bw.Write((ushort)3);                 // Number of sections in the header

        // Write FATB Section (File Allocation Table)
        bw.Write(0x46415442);                // "BTAF" magic number
        bw.Write((uint)(FILE_ALLOCATION_TABLE_HEADER_LENGTH +
                 Elements.Length * FILE_ALLOCATION_TABLE_ELEMENT_LENGTH));
        bw.Write((uint)Elements.Length);     // Number of elements

        curOffset = 0;
        for (int i = 0; i < Elements.Length; i++)
        {
            // Force offsets to be a multiple of 4
            while (curOffset % 4 != 0)
            {
                curOffset++;
            }

            bw.Write(curOffset);                          // Start offset
            curOffset += (uint)Elements[i].Length;
            bw.Write(curOffset);                          // End offset
        }

        // Write FNTB Section (File Name Table)
        // Note: Names are not preserved in this implementation
        bw.Write(0x464E5442);                // "BTNF" magic number
        bw.Write(0x10);                      // FNTB Size
        bw.Write(0x4);                       // Offset of the first name directory
        bw.Write(0x10000);                   // Filler data (1 directory at position 0)

        // Write FIMG Section (File Image Data)
        bw.Write(0x46494D47);                // "GMIF" magic number
        fileImageSizeOffset = (uint)bw.BaseStream.Position;
        bw.Write((uint)0x0);                 // File image size (will be written later)

        curOffset = 0;
        byte[] buffer;
        for (int i = 0; i < Elements.Length; i++)
        {
            // Force offsets to be a multiple of 4 with padding
            while (curOffset % 4 != 0)
            {
                bw.Write((byte)0xFF);
                curOffset++;
            }

            // Write file data
            buffer = new byte[Elements[i].Length];
            Elements[i].Seek(0, SeekOrigin.Begin);
            Elements[i].Read(buffer, 0, (int)Elements[i].Length);
            bw.Write(buffer, 0, (int)Elements[i].Length);
            curOffset += (uint)Elements[i].Length;
        }

        // Pad to 4-byte boundary
        while (curOffset % 4 != 0)
        {
            bw.Write((byte)0xFF);
            curOffset++;
        }

        // Write sizes (backpatching)
        int fileSize = (int)bw.BaseStream.Position;
        bw.Seek((int)fileSizeOffset, SeekOrigin.Begin);
        bw.Write((uint)fileSize);                                    // Total file size

        bw.Seek((int)fileImageSizeOffset, SeekOrigin.Begin);
        bw.Write((uint)(curOffset + FILE_IMAGE_HEADER_SIZE));        // FIMG section size

        AppLogger.Debug($"Saved NARC \"{Name}\" with {Elements.Length} elements and filesize {fileSize} bytes to file: {filePath}");
    }

    /// <summary>
    /// Disposes all memory streams
    /// </summary>
    public void Dispose()
    {
        if (Elements != null)
        {
            foreach (var element in Elements)
            {
                element?.Dispose();
            }
        }
    }
}
