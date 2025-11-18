using System;

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
    }
}
