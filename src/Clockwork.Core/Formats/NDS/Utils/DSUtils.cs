using System;

namespace Clockwork.Core.Formats.NDS.Utils
{
    public static class DSUtils
    {
        public static byte[] ReadFromByteArray(byte[] source, int readFrom = 0, int? length = null)
        {
            int actualLength = length ?? (source.Length - readFrom);
            byte[] result = new byte[actualLength];
            Buffer.BlockCopy(source, readFrom, result, 0, actualLength);
            return result;
        }
    }
}
