using Clockwork.Core.Logging;

namespace Clockwork.Core.Formats.NDS.Scripts;

/// <summary>
/// Represents a complete NDS script file containing scripts, functions, and actions
/// </summary>
public class ScriptFile
{
    /// <summary>
    /// File ID
    /// </summary>
    public int FileID { get; set; }

    /// <summary>
    /// Whether this is a level script
    /// </summary>
    public bool IsLevelScript { get; set; }

    /// <summary>
    /// All scripts in this file
    /// </summary>
    public List<ScriptContainer> Scripts { get; set; } = new();

    /// <summary>
    /// All functions in this file
    /// </summary>
    public List<ScriptContainer> Functions { get; set; } = new();

    /// <summary>
    /// All actions in this file
    /// </summary>
    public List<ScriptContainer> Actions { get; set; } = new();

    public ScriptFile()
    {
    }

    public ScriptFile(int fileID)
    {
        FileID = fileID;
    }

    /// <summary>
    /// Checks if this file has no content
    /// </summary>
    public bool IsEmpty =>
        Scripts.Count == 0 && Functions.Count == 0 && Actions.Count == 0;

    /// <summary>
    /// Reads a script file from binary data
    /// </summary>
    public static ScriptFile FromBinary(byte[] data, int fileID = 0)
    {
        var file = new ScriptFile(fileID);

        using var ms = new MemoryStream(data);
        using var reader = new BinaryReader(ms);

        // TODO: Implement binary reading
        // This will need to parse the header and read all containers
        // Format specifics depend on the game version

        return file;
    }

    /// <summary>
    /// Reads a script file from a file path
    /// </summary>
    public static ScriptFile FromFile(string path, int fileID = 0)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Script file not found: {path}");
        }

        var data = File.ReadAllBytes(path);
        return FromBinary(data, fileID);
    }

    /// <summary>
    /// Converts the script file to binary format (LiTRE-compatible format)
    /// Format:
    /// - Script offsets (4 bytes each)
    /// - Magic 0xFD13 (2 bytes) - marks end of header
    /// - Script data
    /// - Function data
    /// - Action data (with halfword alignment)
    /// </summary>
    public byte[] ToBytes()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Phase 1: Reserve space for script offset table
        long headerStartPos = writer.BaseStream.Position;
        for (int i = 0; i < Scripts.Count; i++)
        {
            writer.Write((uint)0); // Placeholder offsets
        }

        // Write magic number 0xFD13 to mark end of header
        writer.Write((ushort)0xFD13);

        // Track actual offsets for each container
        Dictionary<(ContainerType type, uint id), long> containerOffsets = new();

        // Phase 2: Write Scripts
        foreach (var script in Scripts)
        {
            long offset = writer.BaseStream.Position;
            containerOffsets[(ContainerType.Script, script.ID)] = offset;

            // Write script commands
            foreach (var command in script.Commands)
            {
                writer.Write(command.CommandID);
                foreach (var param in command.Parameters)
                {
                    writer.Write(param);
                }
            }
        }

        // Phase 3: Write Functions
        foreach (var function in Functions)
        {
            long offset = writer.BaseStream.Position;
            containerOffsets[(ContainerType.Function, function.ID)] = offset;

            // Write function commands
            foreach (var command in function.Commands)
            {
                writer.Write(command.CommandID);
                foreach (var param in command.Parameters)
                {
                    writer.Write(param);
                }
            }
        }

        // Phase 4: Write Actions (with halfword alignment)
        // Ensure position is aligned to 2-byte boundary
        if (writer.BaseStream.Position % 2 != 0)
        {
            writer.Write((byte)0); // Padding byte
        }

        foreach (var action in Actions)
        {
            long offset = writer.BaseStream.Position;
            containerOffsets[(ContainerType.Action, action.ID)] = offset;

            // Write action commands
            foreach (var command in action.Commands)
            {
                writer.Write(command.CommandID);
                foreach (var param in command.Parameters)
                {
                    writer.Write(param);
                }
            }
        }

        // Phase 5: Back-patch script offsets in header
        long endPos = writer.BaseStream.Position;
        writer.BaseStream.Position = headerStartPos;

        for (int i = 0; i < Scripts.Count; i++)
        {
            var script = Scripts[i];
            if (containerOffsets.TryGetValue((ContainerType.Script, script.ID), out long offset))
            {
                writer.Write((uint)offset);
            }
            else
            {
                writer.Write((uint)0); // No offset found
            }
        }

        // Restore position to end
        writer.BaseStream.Position = endPos;

        // Phase 6: Convert offset parameters to relative offsets
        // Get the complete data
        byte[] data = ms.ToArray();

        // Fix up relative offsets in commands
        FixRelativeOffsets(data, containerOffsets);

        return data;
    }

    /// <summary>
    /// Fixes relative offset parameters in script commands
    /// Offset parameters must be relative to the current position + 4
    /// </summary>
    private void FixRelativeOffsets(byte[] data, Dictionary<(ContainerType type, uint id), long> containerOffsets)
    {
        // Markers used to encode reference types (must match ScriptCompiler)
        const uint FUNCTION_MARKER = 0x80000000;
        const uint SCRIPT_MARKER = 0x40000000;
        const uint ACTION_MARKER = 0x20000000;
        const uint MARKER_MASK = 0xE0000000;  // Mask for all marker bits
        const uint ID_MASK = 0x00FFFFFF;      // Mask for ID bits

        // Start after the header (script offsets + 0xFD13)
        int headerSize = Scripts.Count * 4 + 2;
        int position = headerSize;

        // Process all containers in order
        var allContainers = new List<(ContainerType type, uint id, ScriptContainer container)>();
        foreach (var script in Scripts)
            allContainers.Add((ContainerType.Script, script.ID, script));
        foreach (var function in Functions)
            allContainers.Add((ContainerType.Function, function.ID, function));
        foreach (var action in Actions)
            allContainers.Add((ContainerType.Action, action.ID, action));

        // Process each container
        foreach (var (containerType, containerId, container) in allContainers)
        {
            // Skip padding byte if present (for actions)
            if (position % 2 != 0 && containerType == ContainerType.Action)
            {
                position++;
            }

            // Process each command in the container
            foreach (var command in container.Commands)
            {
                int commandStartPos = position;

                // Skip command ID (2 bytes)
                position += 2;

                // Get command info to know parameter types
                var commandInfo = ScriptDatabase.GetCommandInfo(command.CommandID);
                if (commandInfo == null)
                {
                    // Unknown command, skip all parameters
                    foreach (var param in command.Parameters)
                    {
                        position += param.Length;
                    }
                    continue;
                }

                // Process each parameter
                for (int paramIndex = 0; paramIndex < command.Parameters.Count; paramIndex++)
                {
                    var paramBytes = command.Parameters[paramIndex];
                    var paramType = commandInfo.Parameters[paramIndex];

                    // Check if this is an Offset parameter
                    if (paramType == ScriptParameterType.Offset && paramBytes.Length == 4)
                    {
                        // Read the offset value (little-endian)
                        uint offsetValue = BitConverter.ToUInt32(paramBytes, 0);

                        // Check if this is a reference marker
                        uint markerBits = offsetValue & MARKER_MASK;
                        if (markerBits != 0)
                        {
                            // Extract reference type and ID
                            uint refId = offsetValue & ID_MASK;
                            ContainerType refType = markerBits switch
                            {
                                FUNCTION_MARKER => ContainerType.Function,
                                SCRIPT_MARKER => ContainerType.Script,
                                ACTION_MARKER => ContainerType.Action,
                                _ => ContainerType.Script
                            };

                            // Look up the target offset
                            if (containerOffsets.TryGetValue((refType, refId), out long targetOffset))
                            {
                                // Calculate relative offset: target - (current position + 4)
                                // The +4 accounts for the size of the offset field itself
                                int relativeOffset = (int)(targetOffset - position - 4);

                                // Write the relative offset back to the data array
                                byte[] relativeBytes = BitConverter.GetBytes(relativeOffset);
                                Array.Copy(relativeBytes, 0, data, position, 4);

                                // Also update the parameter bytes in the command object
                                Array.Copy(relativeBytes, 0, paramBytes, 0, 4);
                            }
                            else
                            {
                                AppLogger.Warn($"Cannot resolve reference to {refType} #{refId} at position {position}");
                            }
                        }
                    }

                    position += paramBytes.Length;
                }
            }
        }
    }

    /// <summary>
    /// Saves the script file to disk
    /// </summary>
    public void SaveToFile(string path)
    {
        var data = ToBytes();
        File.WriteAllBytes(path, data);
    }

    /// <summary>
    /// Gets a summary of the file contents
    /// </summary>
    public string GetSummary()
    {
        return $"Script File {FileID}: {Scripts.Count} scripts, {Functions.Count} functions, {Actions.Count} actions";
    }
}
