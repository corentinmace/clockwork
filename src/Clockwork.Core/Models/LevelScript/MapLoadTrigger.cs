using System.IO;

namespace Clockwork.Core.Models.LevelScript;

/// <summary>
/// Trigger that activates when a specific map/screen loads
/// Type 0x0001: Map/Screen Load Trigger
/// </summary>
public class MapLoadTrigger : ILevelScriptTrigger
{
    public ushort TriggerType => 0x0001;

    public ushort Unknown { get; set; }
    public ushort ScriptFileID { get; set; }

    public static MapLoadTrigger ReadFromStream(BinaryReader reader)
    {
        return new MapLoadTrigger
        {
            Unknown = reader.ReadUInt16(),
            ScriptFileID = reader.ReadUInt16()
        };
    }

    public void WriteToStream(BinaryWriter writer)
    {
        writer.Write(TriggerType);
        writer.Write(Unknown);
        writer.Write(ScriptFileID);
    }

    public string GetDisplayString()
    {
        return $"Map Load → Script: {ScriptFileID}";
    }
}
