// Matrix utilities for NSBMD

namespace Clockwork.Core.Formats.NDS.NSBMD
{
    public static class Matrix4x4Util
    {
        public static float[] LoadIdentity()
        {
            return new float[]
            {
                1, 0, 0, 0,
                0, 1, 0, 0,
                0, 0, 1, 0,
                0, 0, 0, 1
            };
        }
    }
}
