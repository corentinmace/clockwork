using System;
using System.Collections.Generic;
using System.IO;

namespace Clockwork.Core.Models.LevelScript;

/// <summary>
/// Represents a level script file containing triggers
/// Format based on LiTRE implementation
/// </summary>
public class LevelScriptFile
{
    public int ScriptID { get; set; }
    public List<MapLoadTrigger> MapLoadTriggers { get; set; } = new();
    public List<VariableValueTrigger> VariableValueTriggers { get; set; } = new();

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

        // Check for 0xFD13 magic - if present, this is NOT a level script
        ushort firstWord = reader.ReadUInt16();
        if (firstWord == 0xFD13)
        {
            // This is a regular script file, not a level script
            return file;
        }
        ms.Position = 0; // Rewind

        // Phase 1: Read MapScreenLoadTrigger entries
        // Format: 1 byte triggerType + 4 bytes scriptID
        while (ms.Position < ms.Length)
        {
            if (ms.Position + 1 >= ms.Length)
                break;

            byte triggerType = reader.ReadByte();

            // Check if this is a valid trigger type
            if (!MapLoadTrigger.IsValidTriggerType(triggerType))
            {
                // Not a map load trigger, rewind and move to next phase
                ms.Position -= 1;
                break;
            }

            if (ms.Position + 4 > ms.Length)
                break;

            uint scriptToTrigger = reader.ReadUInt32();

            file.MapLoadTriggers.Add(new MapLoadTrigger
            {
                TriggerType = triggerType,
                ScriptToTrigger = scriptToTrigger
            });
        }

        // Phase 2: Read VariableValueTrigger entries if present
        if (ms.Position < ms.Length)
        {
            // Read the variable value header
            if (ms.Position + 6 <= ms.Length)
            {
                byte varMarker = reader.ReadByte();

                if (varMarker == VariableValueTrigger.VARIABLEVALUE_MARKER)
                {
                    uint conditionalOffset = reader.ReadUInt32();
                    byte padding = reader.ReadByte(); // padding

                    // Read variable value conditions
                    while (ms.Position + 6 <= ms.Length)
                    {
                        ushort variableID = reader.ReadUInt16();

                        // Terminator check
                        if (variableID == 0)
                            break;

                        ushort expectedValue = reader.ReadUInt16();
                        ushort scriptID = reader.ReadUInt16();

                        file.VariableValueTriggers.Add(new VariableValueTrigger
                        {
                            VariableID = variableID,
                            ExpectedValue = expectedValue,
                            ScriptToTrigger = scriptID
                        });
                    }
                }
            }
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

        // Write MapLoadTriggers
        foreach (var trigger in MapLoadTriggers)
        {
            writer.Write(trigger.TriggerType);
            writer.Write(trigger.ScriptToTrigger);
        }

        // Write VariableValueTriggers if any
        if (VariableValueTriggers.Count > 0)
        {
            // Write header
            writer.Write(VariableValueTrigger.VARIABLEVALUE_MARKER);

            // Calculate offset (header is 6 bytes: 1 marker + 4 offset + 1 padding)
            uint offset = (uint)(6 + VariableValueTriggers.Count * 6);
            writer.Write(offset);
            writer.Write((byte)0); // padding

            // Write conditions
            foreach (var trigger in VariableValueTriggers)
            {
                writer.Write(trigger.VariableID);
                writer.Write(trigger.ExpectedValue);
                writer.Write(trigger.ScriptToTrigger);
            }

            // Write terminator
            writer.Write((ushort)0);
        }

        return ms.ToArray();
    }

    /// <summary>
    /// Get all triggers as a unified list for display
    /// </summary>
    public List<ILevelScriptTrigger> GetAllTriggers()
    {
        var all = new List<ILevelScriptTrigger>();
        all.AddRange(MapLoadTriggers);
        all.AddRange(VariableValueTriggers);
        return all;
    }
}
