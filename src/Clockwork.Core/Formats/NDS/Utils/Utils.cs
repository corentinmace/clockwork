using System;
using System.IO;
using System.Text;

namespace Clockwork.Core.Formats.NDS.Utils
{
    /// <summary>
    /// Utility functions for reading Nintendo DS binary data (little-endian).
    /// </summary>
    public static class Utils
    {
        /// <summary>
        /// Read a 16-bit signed integer from a byte array at the specified offset.
        /// </summary>
        public static int Read2BytesAsInt16(byte[] data, int offset)
        {
            if (data == null || offset + 1 >= data.Length)
                return 0;

            // Little-endian read
            return (short)(data[offset] | (data[offset + 1] << 8));
        }

        /// <summary>
        /// Read a 32-bit signed integer from a byte array at the specified offset.
        /// </summary>
        public static int Read4BytesAsInt32(byte[] data, int offset)
        {
            if (data == null || offset + 3 >= data.Length)
                return 0;

            // Little-endian read
            return data[offset] |
                   (data[offset + 1] << 8) |
                   (data[offset + 2] << 16) |
                   (data[offset + 3] << 24);
        }

        /// <summary>
        /// Read a 16-bit unsigned integer from a byte array at the specified offset.
        /// </summary>
        public static ushort Read2BytesAsUInt16(byte[] data, int offset)
        {
            if (data == null || offset + 1 >= data.Length)
                return 0;

            // Little-endian read
            return (ushort)(data[offset] | (data[offset + 1] << 8));
        }

        /// <summary>
        /// Read a 16-bit unsigned integer from a byte array at the specified offset (alias).
        /// </summary>
        public static ushort Read2BytesAsushort(byte[] data, int offset)
        {
            return Read2BytesAsUInt16(data, offset);
        }

        /// <summary>
        /// Read a 32-bit unsigned integer from a byte array at the specified offset.
        /// </summary>
        public static uint Read4BytesAsUInt32(byte[] data, int offset)
        {
            if (data == null || offset + 3 >= data.Length)
                return 0;

            // Little-endian read
            return (uint)(data[offset] |
                         (data[offset + 1] << 8) |
                         (data[offset + 2] << 16) |
                         (data[offset + 3] << 24));
        }

        /// <summary>
        /// Read a null-terminated string from a stream at the current position.
        /// </summary>
        public static string ReadNSBMDString(Stream stream, int maxLength = 256)
        {
            var bytes = new System.Collections.Generic.List<byte>();
            int b;
            int count = 0;

            while ((b = stream.ReadByte()) != -1 && b != 0 && count < maxLength)
            {
                bytes.Add((byte)b);
                count++;
            }

            return Encoding.UTF8.GetString(bytes.ToArray());
        }

        /// <summary>
        /// Convert a 15-bit BGR color value to a Color.
        /// </summary>
        public static NSBMD.Color BGR15ToColor(ushort bgr15)
        {
            // BGR555 format: -BBBBBGGGGGRRRRR
            byte r = (byte)((bgr15 & 0x1F) << 3);        // 5 bits red
            byte g = (byte)(((bgr15 >> 5) & 0x1F) << 3);  // 5 bits green
            byte b = (byte)(((bgr15 >> 10) & 0x1F) << 3); // 5 bits blue

            // Expand 5-bit to 8-bit by copying top bits to bottom
            r |= (byte)(r >> 5);
            g |= (byte)(g >> 5);
            b |= (byte)(b >> 5);

            return new NSBMD.Color(r, g, b, 255);
        }

        /// <summary>
        /// Convert BGR555 format from two bytes to a Color.
        /// </summary>
        public static NSBMD.Color BGR555ToColor(byte byte1, byte byte2)
        {
            ushort bgr15 = (ushort)(byte1 | (byte2 << 8));
            return BGR15ToColor(bgr15);
        }
    }

    /// <summary>
    /// Extension methods for Stream class.
    /// </summary>
    public static class StreamExtensions
    {
        /// <summary>
        /// Skip a specified number of bytes in the stream.
        /// </summary>
        public static void Skip(this Stream stream, long count)
        {
            if (count <= 0) return;
            stream.Seek(count, SeekOrigin.Current);
        }
    }
}
