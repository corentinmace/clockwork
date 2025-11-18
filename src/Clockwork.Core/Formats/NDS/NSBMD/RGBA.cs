// RGBA color structure for NSBMD.
// Code adapted from kiwi.ds' NSBMD Model Viewer.

namespace Clockwork.Core.Formats.NDS.NSBMD
{
    public struct RGBA
    {
        public byte R, G, B, A;

        public RGBA(byte r, byte g, byte b, byte a)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }
    }
}
