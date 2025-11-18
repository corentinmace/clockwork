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

        public static Color White => new Color(255, 255, 255, 255);
        public static Color Black => new Color(0, 0, 0, 255);
        public static Color Transparent => new Color(0, 0, 0, 0);

        public static Color FromArgb(int argb)
        {
            return new Color(
                (byte)((argb >> 16) & 0xFF),
                (byte)((argb >> 8) & 0xFF),
                (byte)(argb & 0xFF),
                (byte)((argb >> 24) & 0xFF)
            );
        }

        public static Color FromArgb(int a, int r, int g, int b)
        {
            return new Color((byte)r, (byte)g, (byte)b, (byte)a);
        }

        public int ToArgb()
        {
            return (A << 24) | (R << 16) | (G << 8) | B;
        }
    }
}
