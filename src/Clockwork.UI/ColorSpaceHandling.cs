namespace Veldrid
{
    /// <summary>
    /// Identifies how the ImGuiRenderer should treat vertex colors.
    /// </summary>
    public enum ColorSpaceHandling
    {
        /// <summary>
        /// The renderer will not convert sRGB vertex colors into linear space before blending them.
        /// </summary>
        Legacy = 0,

        /// <summary>
        /// The renderer will convert sRGB vertex colors into linear space before blending them with colors from user Textures.
        /// </summary>
        Linear = 1
    }
}
