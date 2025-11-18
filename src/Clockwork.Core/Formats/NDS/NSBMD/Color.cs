// Simple Color structure for NSBMD (replaces System.Drawing.Color)

namespace Clockwork.Core.Formats.NDS.NSBMD
{
    public struct Color
    {
        public byte R, G, B, A;

        public Color(byte r, byte g, byte b, byte a = 255)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public static Color FromArgb(int argb)
        {
            return new Color(
                (byte)((argb >> 16) & 0xFF),
                (byte)((argb >> 8) & 0xFF),
                (byte)(argb & 0xFF),
                (byte)((argb >> 24) & 0xFF)
            );
        }

        public static Color FromArgb(byte a, byte r, byte g, byte b)
        {
            return new Color(r, g, b, a);
        }

        public int ToArgb()
        {
            return (A << 24) | (R << 16) | (G << 8) | B;
        }
    }
}
