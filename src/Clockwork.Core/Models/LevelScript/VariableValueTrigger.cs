using System.IO;

namespace Clockwork.Core.Models.LevelScript;

/// <summary>
/// Trigger that activates when a game variable has a specific value
/// Type 0x0002: Variable Value Trigger
/// </summary>
public class VariableValueTrigger : ILevelScriptTrigger
{
    public ushort TriggerType => 0x0002;

    public ushort VariableNumber { get; set; }
    public ushort VariableValue { get; set; }
    public ushort ScriptFileID { get; set; }
    public ushort Unknown { get; set; }

    public static VariableValueTrigger ReadFromStream(BinaryReader reader)
    {
        return new VariableValueTrigger
        {
            Unknown = reader.ReadUInt16(),
            VariableNumber = reader.ReadUInt16(),
            VariableValue = reader.ReadUInt16(),
            ScriptFileID = reader.ReadUInt16()
        };
    }

    public void WriteToStream(BinaryWriter writer)
    {
        writer.Write(TriggerType);
        writer.Write(Unknown);
        writer.Write(VariableNumber);
        writer.Write(VariableValue);
        writer.Write(ScriptFileID);
    }

    public string GetDisplayString()
    {
        return $"Var {VariableNumber:X4} == {VariableValue:X4} → Script: {ScriptFileID}";
    }
}
