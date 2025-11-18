// Stub for NSBTX Loader - minimal implementation
// Full implementation can be added later if needed

using System.IO;
using System.Collections.Generic;
using Clockwork.Core.Formats.NDS.NSBMD;

namespace Clockwork.Core.Formats.NDS.NSBTX
{
    public static class NSBTXLoader
    {
        public static NSBMDMaterial[] ReadTex0(Stream stream, long offset, out int texnum, out int palnum,
            out List<NSBMDTexture> textures, out List<NSBMDPalette> palettes)
        {
            // Minimal stub implementation
            texnum = 0;
            palnum = 0;
            textures = new List<NSBMDTexture>();
            palettes = new List<NSBMDPalette>();
            return new NSBMDMaterial[0];
        }
    }
}
