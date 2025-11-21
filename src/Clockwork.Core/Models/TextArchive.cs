using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Clockwork.Core.Models;

/// <summary>
/// Represents a Pokemon DS text archive with encrypted messages.
/// Based on the format used in Diamond/Pearl/Platinum/HeartGold/SoulSilver.
/// </summary>
public class TextArchive
{
    public List<string> Messages { get; set; } = new();
    public ushort EncryptionKey { get; set; }

    private static Dictionary<ushort, char>? _charDictionary;

    /// <summary>
    /// Read a text archive from binary data.
    /// </summary>
    public static TextArchive ReadFromBytes(byte[] data)
    {
        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);

        var archive = new TextArchive();

        // Read header
        ushort stringCount = reader.ReadUInt16();
        ushort initialKey = reader.ReadUInt16();
        archive.EncryptionKey = initialKey;

        // Calculate base key
        ushort baseKey = (ushort)((initialKey * 0x2FD) & 0xFFFF);

        // Read offset and size table (encrypted)
        var offsets = new uint[stringCount];
        var sizes = new uint[stringCount];

        for (int i = 0; i < stringCount; i++)
        {
            // Calculate key for this entry
            ushort key1 = (ushort)((baseKey * (i + 1)) & 0xFFFF);
            uint key2 = (uint)(key1 | (key1 << 16));

            // Read and decrypt offset and size
            uint encOffset = reader.ReadUInt32();
            uint encSize = reader.ReadUInt32();

            offsets[i] = encOffset ^ key2;
            sizes[i] = encSize ^ key2;
        }

        // Read and decode messages
        for (int i = 0; i < stringCount; i++)
        {
            ms.Position = offsets[i];
            byte[] messageData = reader.ReadBytes((int)sizes[i]);

            string message = DecodeMessage(messageData, i, initialKey);
            archive.Messages.Add(message);
        }

        return archive;
    }

    /// <summary>
    /// Decode a single message from encrypted bytes.
    /// </summary>
    private static string DecodeMessage(byte[] data, int messageIndex, ushort initialKey)
    {
        // Calculate message key
        ushort key = (ushort)(((0x91BD3 * (messageIndex + 1)) & 0xFFFF));

        var sb = new StringBuilder();
        bool isCompressed = false;
        int bitIndex = 0;
        ushort compressedValue = 0;

        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);

        while (ms.Position < ms.Length)
        {
            ushort charValue;

            if (isCompressed)
            {
                // Extract 9-bit values from compressed stream
                if (bitIndex == 0)
                {
                    compressedValue = DecryptChar(reader.ReadUInt16(), ref key);
                    charValue = (ushort)(compressedValue & 0x1FF);
                    bitIndex = 9;
                }
                else if (bitIndex == 9)
                {
                    charValue = (ushort)(compressedValue >> 9);
                    bitIndex = 0;
                }
                else
                {
                    charValue = 0xFFFF;
                }
            }
            else
            {
                // Read uncompressed 16-bit value
                charValue = DecryptChar(reader.ReadUInt16(), ref key);
            }

            // Handle control codes
            if (charValue == 0xFFFF)
            {
                // Terminator
                break;
            }
            else if (charValue == 0xFFFE)
            {
                // Verbose code - next value is literal hex
                ushort verboseValue = DecryptChar(reader.ReadUInt16(), ref key);
                sb.Append($"\\x{verboseValue:X4}");
            }
            else if (charValue == 0xF100)
            {
                // Compression flag
                isCompressed = !isCompressed;
                bitIndex = 0;
            }
            else if (charValue == 0xE000)
            {
                // Newline
                sb.Append('\n');
            }
            else if (charValue == 0x25BC)
            {
                // Carriage return
                sb.Append('\r');
            }
            else if (charValue == 0x25BD)
            {
                // Form feed
                sb.Append('\f');
            }
            else if (charValue >= 0xF000 && charValue <= 0xF0FF)
            {
                // Special formatting code
                sb.Append($"[{charValue:X4}]");
            }
            else
            {
                // Regular character
                char c = GetCharacter(charValue);
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Decrypt a single character value.
    /// </summary>
    private static ushort DecryptChar(ushort encrypted, ref ushort key)
    {
        ushort decrypted = (ushort)(encrypted ^ key);
        key = (ushort)((key + 0x493D) & 0xFFFF);
        return decrypted;
    }

    /// <summary>
    /// Get character from character code using the lookup table.
    /// </summary>
    private static char GetCharacter(ushort code)
    {
        if (_charDictionary == null)
        {
            _charDictionary = BuildCharDictionary();
        }

        if (_charDictionary.TryGetValue(code, out char c))
        {
            return c;
        }

        // Unknown character
        return '?';
    }

    /// <summary>
    /// Build the character lookup dictionary for Pokemon DS games.
    /// Based on the character table from Gen IV games.
    /// </summary>
    private static Dictionary<ushort, char> BuildCharDictionary()
    {
        var dict = new Dictionary<ushort, char>();

        // ASCII-like characters
        for (ushort i = 0x20; i <= 0x7E; i++)
        {
            dict[i] = (char)i;
        }

        // Special Pokemon characters
        dict[0x0001] = 'À';
        dict[0x0002] = 'Á';
        dict[0x0003] = 'Â';
        dict[0x0004] = 'Ç';
        dict[0x0005] = 'È';
        dict[0x0006] = 'É';
        dict[0x0007] = 'Ê';
        dict[0x0008] = 'Ë';
        dict[0x0009] = 'Ì';
        dict[0x000A] = 'Î';
        dict[0x000B] = 'Ï';
        dict[0x000C] = 'Ò';
        dict[0x000D] = 'Ó';
        dict[0x000E] = 'Ô';
        dict[0x000F] = 'Œ';
        dict[0x0010] = 'Ù';
        dict[0x0011] = 'Ú';
        dict[0x0012] = 'Û';
        dict[0x0013] = 'Ñ';
        dict[0x0014] = 'ß';
        dict[0x0015] = 'à';
        dict[0x0016] = 'á';
        dict[0x0019] = 'ç';
        dict[0x001A] = 'è';
        dict[0x001B] = 'é';
        dict[0x001C] = 'ê';
        dict[0x001D] = 'ë';
        dict[0x001E] = 'ì';
        dict[0x0020] = 'î';
        dict[0x0021] = 'ï';
        dict[0x0022] = 'ò';
        dict[0x0023] = 'ó';
        dict[0x0024] = 'ô';
        dict[0x0025] = 'œ';
        dict[0x0026] = 'ù';
        dict[0x0027] = 'ú';
        dict[0x0028] = 'û';
        dict[0x0029] = 'ñ';

        // Japanese characters (simplified mapping)
        dict[0x0121] = 'ア';
        dict[0x0122] = 'イ';
        dict[0x0123] = 'ウ';
        dict[0x0124] = 'エ';
        dict[0x0125] = 'オ';

        return dict;
    }
}
