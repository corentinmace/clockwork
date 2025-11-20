using System.IO;

namespace Clockwork.Core.Models.LevelScript;

/// <summary>
/// Trigger that activates when a game variable has a specific value
/// Format: Header (1 byte marker + 4 bytes offset + 1 padding) + entries (2 bytes varID + 2 bytes value + 2 bytes scriptID)
/// </summary>
public class VariableValueTrigger : ILevelScriptTrigger
{
    public const byte VARIABLEVALUE_MARKER = 0x05;

    public ushort VariableID { get; set; }
    public ushort ExpectedValue { get; set; }
    public ushort ScriptToTrigger { get; set; }

    ushort ILevelScriptTrigger.TriggerType => VARIABLEVALUE_MARKER;

    public void WriteToStream(BinaryWriter writer)
    {
        // Note: The header is written by LevelScriptFile, not individual triggers
        writer.Write(VariableID);
        writer.Write(ExpectedValue);
        writer.Write(ScriptToTrigger);
    }

    public string GetDisplayString()
    {
        return $"Var {VariableID:X4} == {ExpectedValue:X4} → Script: {ScriptToTrigger}";
    }
}
