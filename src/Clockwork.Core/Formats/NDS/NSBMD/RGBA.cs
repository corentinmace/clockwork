// RGBA color structure for NSBMD.
// Code adapted from kiwi.ds' NSBMD Model Viewer.

using System;

namespace Clockwork.Core.Formats.NDS.NSBMD
{
    public struct RGBA
    {
        public byte A;
        public byte R;
        public byte G;
        public byte B;

        public RGBA(byte r, byte g, byte b, byte a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        /// <summary>
        /// Transparent color.
        /// </summary>
        public static RGBA Transparent = new RGBA { R = 0xFF, A = 0x0 };

        public static RGBA fromColor(Color c)
        {
            RGBA a = new RGBA();
            a.R = c.R;
            a.G = c.G;
            a.B = c.B;
            a.A = c.A;
            return a;
        }

        /// <summary>
        /// Index accessor.
        /// </summary>
        public byte this[int i]
        {
            get
            {
                switch (i)
                {
                    case 0:
                        return R;
                    case 1:
                        return G;
                    case 2:
                        return B;
                    case 3:
                        return A;
                    default:
                        throw new Exception();
                }
            }
            set
            {
                switch (i)
                {
                    case 0:
                        R = value;
                        break;
                    case 1:
                        G = value;
                        break;
                    case 2:
                        B = value;
                        break;
                    case 3:
                        A = value;
                        break;
                    default:
                        throw new Exception();
                }
            }
        }

        public Color ToColor()
        {
            return Color.FromArgb(A, R, G, B);
        }
    }
}
