namespace Clockwork.Core.Models;

/// <summary>
/// Constants for Nintendo DS memory addresses and structure.
/// </summary>
public static class DsConstants
{
    /// <summary>
    /// ARM9 binary load address in DS memory.
    /// </summary>
    public const uint ARM9_LOAD_ADDRESS = 0x02000000;

    /// <summary>
    /// SynthOverlay base address in DS memory.
    /// All addresses >= this value belong to the SynthOverlay (file 0065).
    /// </summary>
    public const uint SYNTH_OVERLAY_BASE_ADDRESS = 0x023C8000;
}
