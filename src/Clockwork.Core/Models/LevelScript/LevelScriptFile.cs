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

        // Check if first value is 0 (valid level script)
        ms.Position = 0;
        uint firstValue = reader.ReadUInt32();
        if (firstValue == 0)
        {
            // Valid level script with no triggers
            return file;
        }

        ms.Position = 0; // Rewind

        bool hasConditionalStructure = false;
        int conditionalStructureOffset = -1;

        // Phase 1: Read all triggers
        while (ms.Position < ms.Length)
        {
            if (ms.Position + 1 > ms.Length)
                break;

            byte triggerType = reader.ReadByte();

            // Check if this is a valid trigger type (1, 2, 3, 4)
            if (triggerType != VariableValueTrigger.VARIABLEVALUE &&
                !MapLoadTrigger.IsValidTriggerType(triggerType))
            {
                // Not a valid trigger type, end of trigger section
                ms.Position -= 1;
                break;
            }

            // Update conditional structure offset if we have one
            if (hasConditionalStructure)
                conditionalStructureOffset -= sizeof(byte);

            // If trigger type is VARIABLEVALUE (1), read offset and continue
            if (triggerType == VariableValueTrigger.VARIABLEVALUE)
            {
                if (ms.Position + 4 > ms.Length)
                    break;

                hasConditionalStructure = true;
                conditionalStructureOffset = (int)reader.ReadUInt32();
                continue; // Don't create a trigger here, just note we have variable triggers
            }

            // Otherwise, it's a MapLoadTrigger (2, 3, 4)
            if (ms.Position + 4 > ms.Length)
                break;

            uint scriptToTrigger = reader.ReadUInt32();

            file.MapLoadTriggers.Add(new MapLoadTrigger
            {
                TriggerType = triggerType,
                ScriptToTrigger = scriptToTrigger
            });

            // Update conditional structure offset if we have one
            if (hasConditionalStructure)
                conditionalStructureOffset -= sizeof(uint);
        }

        // Check for minimum valid file size
        const int SMALLEST_TRIGGER_SIZE = 5;
        if (ms.Position == 1)
        {
            if (ms.Position + 2 <= ms.Length)
            {
                ushort check = reader.ReadUInt16();
                if (check == 0 && data.Length < SMALLEST_TRIGGER_SIZE)
                {
                    // Empty level script
                    return file;
                }
            }
        }

        // Validate file structure
        if (ms.Position < SMALLEST_TRIGGER_SIZE && file.MapLoadTriggers.Count == 0)
        {
            // Invalid file
            return file;
        }

        // Phase 2: Read VariableValueTriggers if they exist
        if (hasConditionalStructure)
        {
            // Validate conditional structure offset
            if (conditionalStructureOffset != 1)
            {
                // File is corrupted
                return file;
            }

            // Read variable value triggers
            while (ms.Position + 6 <= ms.Length)
            {
                ushort variableID = reader.ReadUInt16();

                // Variables below 1 are invalid, end of triggers
                if (variableID <= 0)
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

        return file;
    }

    /// <summary>
    /// Convert level script file to bytes
    /// </summary>
    public byte[] ToBytes()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Write all MapLoadTriggers first
        foreach (var trigger in MapLoadTriggers)
        {
            writer.Write(trigger.TriggerType);
            writer.Write(trigger.ScriptToTrigger);
        }

        // Write VariableValueTriggers if any
        if (VariableValueTriggers.Count > 0)
        {
            // Write VARIABLEVALUE marker
            writer.Write(VariableValueTrigger.VARIABLEVALUE);

            // Write offset (always 1 since no MapLoad triggers follow)
            writer.Write((uint)1);

            // Write padding byte
            writer.Write((byte)0);

            // Write all variable value triggers
            foreach (var trigger in VariableValueTriggers)
            {
                writer.Write(trigger.VariableID);
                writer.Write(trigger.ExpectedValue);
                writer.Write(trigger.ScriptToTrigger);
            }
        }

        // Write terminator
        writer.Write((ushort)0);

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
