using System.IO;

namespace Clockwork.Core.Models.LevelScript;

/// <summary>
/// Trigger that activates when a specific map/screen loads
/// Format: 1 byte triggerType + 4 bytes scriptID
/// </summary>
public class MapLoadTrigger : ILevelScriptTrigger
{
    // Trigger type constants (from LiTRE)
    public const byte MAPCHANGE = 2;
    public const byte SCREENRESET = 3;
    public const byte LOADGAME = 4;

    // Valid trigger types for map/screen load
    private static readonly byte[] ValidTriggerTypes = { MAPCHANGE, SCREENRESET, LOADGAME };

    public byte TriggerType { get; set; }
    public uint ScriptToTrigger { get; set; }

    ushort ILevelScriptTrigger.TriggerType => TriggerType;

    public static bool IsValidTriggerType(byte type)
    {
        return System.Array.IndexOf(ValidTriggerTypes, type) >= 0;
    }

    public void WriteToStream(BinaryWriter writer)
    {
        writer.Write(TriggerType);
        writer.Write(ScriptToTrigger);
    }

    public string GetDisplayString()
    {
        string triggerName = TriggerType switch
        {
            MAPCHANGE => "Map Change",
            SCREENRESET => "Screen Reset",
            LOADGAME => "Load Game",
            _ => $"Type {TriggerType}"
        };

        return $"{triggerName} → Script: {ScriptToTrigger}";
    }
}
