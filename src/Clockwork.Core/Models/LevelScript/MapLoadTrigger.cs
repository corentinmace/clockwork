using System.IO;

namespace Clockwork.Core.Models.LevelScript;

/// <summary>
/// Trigger that activates when a specific map/screen loads
/// Format: 1 byte triggerType + 4 bytes scriptID
/// </summary>
public class MapLoadTrigger : ILevelScriptTrigger
{
    // Valid trigger types for map/screen load
    private static readonly byte[] ValidTriggerTypes = { 1, 2, 3, 4, 5, 6 };

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
            1 => "Map Load",
            2 => "On Foot",
            3 => "Surf",
            4 => "Bike",
            5 => "Fly",
            6 => "Special",
            _ => $"Type {TriggerType}"
        };

        return $"{triggerName} → Script: {ScriptToTrigger}";
    }
}
