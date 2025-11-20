using System;
using System.Collections.Generic;
using System.IO;

namespace Clockwork.Core.Models.LevelScript;

/// <summary>
/// Represents a level script file containing triggers
/// </summary>
public class LevelScriptFile
{
    public int ScriptID { get; set; }
    public List<ILevelScriptTrigger> Triggers { get; set; } = new();

    /// <summary>
    /// Read level script file from bytes
    /// </summary>
    public static LevelScriptFile ReadFromBytes(byte[] data, int scriptId)
    {
        var file = new LevelScriptFile
        {
            ScriptID = scriptId
        };

        if (data.Length < 2)
            return file;

        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);

        // Read triggers until end of file or terminator (0xFD13)
        while (ms.Position < ms.Length - 1)
        {
            ushort triggerType = reader.ReadUInt16();

            if (triggerType == 0xFD13) // Terminator
                break;

            ms.Position -= 2; // Rewind to read full trigger

            ILevelScriptTrigger? trigger = triggerType switch
            {
                0x0001 => MapLoadTrigger.ReadFromStream(reader),
                0x0002 => VariableValueTrigger.ReadFromStream(reader),
                _ => null
            };

            if (trigger != null)
                file.Triggers.Add(trigger);
            else
                break; // Unknown trigger type, stop parsing
        }

        return file;
    }

    /// <summary>
    /// Convert level script file to bytes
    /// </summary>
    public byte[] ToBytes()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Write all triggers
        foreach (var trigger in Triggers)
        {
            trigger.WriteToStream(writer);
        }

        // Write terminator
        writer.Write((ushort)0xFD13);

        return ms.ToArray();
    }
}
