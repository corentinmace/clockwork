// Stub classes for NSBCA, NSBTA, and NSBTP animation formats
// These are minimal implementations to allow NSBMDGlRenderer to compile
// Full implementation would require parsing the binary animation data

using System;
using System.Collections.Generic;

namespace Clockwork.Core.Formats.NDS.NSBMD
{
    #region NSBCA (Joint/Bone Animation)

    /// <summary>
    /// NSBCA file - Joint/Bone animation for NSBMD models
    /// </summary>
    public class NSBCA_File
    {
        public NSBCAHeader Header { get; set; } = new NSBCAHeader();
        public J_AC[] JAC { get; set; } = Array.Empty<J_AC>();
    }

    public class NSBCAHeader
    {
        public string ID { get; set; } = "";
        public int file_size { get; set; } = 0;
    }

    /// <summary>
    /// Joint Animation Container
    /// </summary>
    public class J_AC
    {
        public string ID { get; set; } = "";
        public short NrFrames { get; set; }
        public short NrObjects { get; set; }
        public int Unknown1 { get; set; }
        public byte[] JointData { get; set; } = Array.Empty<byte>();
        public byte[] RotationData { get; set; } = Array.Empty<byte>();
        public objInfo[] ObjInfo { get; set; } = Array.Empty<objInfo>();
    }

    /// <summary>
    /// Per-object animation info
    /// </summary>
    public class objInfo
    {
        public ushort Flag { get; set; }
        public byte Unknown1 { get; set; }
        public byte ID { get; set; }

        // Translation
        public List<int>[] translate { get; set; } = new List<int>[3] { new(), new(), new() };
        public List<int>[] translate_keyframes { get; set; } = new List<int>[3] { new(), new(), new() };
        public short tStart { get; set; }
        public short tEnd { get; set; }

        // Rotation/Pivot
        public List<int> rotate { get; set; } = new();
        public List<int>[] rotate_keyframes { get; set; } = new List<int>[2] { new(), new() };
        public short rStart { get; set; }
        public short rEnd { get; set; }

        // Scale - [axis][component]
        public List<int>[][] scale { get; set; } = new List<int>[3][]
        {
            new List<int>[2] { new(), new() },
            new List<int>[2] { new(), new() },
            new List<int>[2] { new(), new() }
        };
        public List<int>[][] scale_keyframes { get; set; } = new List<int>[3][]
        {
            new List<int>[2] { new(), new() },
            new List<int>[2] { new(), new() },
            new List<int>[2] { new(), new() }
        };
        public short sStart { get; set; }
        public short sEnd { get; set; }
    }

    #endregion

    #region NSBTA (Material/Texture Animation)

    /// <summary>
    /// NSBTA file - Material/Texture animation
    /// </summary>
    public class NSBTA_File
    {
        public NSBTAHeader Header { get; set; } = new NSBTAHeader();
        public NSBTA_MAT MAT { get; set; } = new NSBTA_MAT();
    }

    public class NSBTAHeader
    {
        public string ID { get; set; } = "";
        public int file_size { get; set; } = 0;
    }

    public class NSBTA_MAT
    {
        public string[] names { get; set; } = Array.Empty<string>();
    }

    #endregion

    #region NSBTP (Texture Pattern Animation)

    /// <summary>
    /// NSBTP file - Texture pattern animation (texture/palette swaps)
    /// </summary>
    public class NSBTP_File
    {
        public NSBTPHeader Header { get; set; } = new NSBTPHeader();
        public NSBTP_MPT MPT { get; set; } = new NSBTP_MPT();
        public animData[] AnimData { get; set; } = Array.Empty<animData>();
    }

    public class NSBTPHeader
    {
        public string ID { get; set; } = "";
        public int file_size { get; set; } = 0;
    }

    public class NSBTP_MPT
    {
        public int NoFrames { get; set; }
        public int NoTex { get; set; }
        public int NoPal { get; set; }
        public string[] names { get; set; } = Array.Empty<string>();
        public NSBTP_InfoBlock infoBlock { get; set; } = new NSBTP_InfoBlock();
    }

    public class NSBTP_InfoBlock
    {
        public NSBTP_InfoData[] Data { get; set; } = Array.Empty<NSBTP_InfoData>();
    }

    public class NSBTP_InfoData
    {
        public int Unknown1 { get; set; }
        public int KeyFrameCount { get; set; }
    }

    public class animData
    {
        public keyFrame[] KeyFrames { get; set; } = Array.Empty<keyFrame>();
    }

    public class keyFrame
    {
        public short Start { get; set; }
        public byte texId { get; set; }
        public byte palId { get; set; }
        public string texName { get; set; } = "";
        public string palName { get; set; } = "";
    }

    #endregion
}
