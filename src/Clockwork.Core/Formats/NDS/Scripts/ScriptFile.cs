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
        // TODO: Implement offset fixup
        // This requires:
        // 1. Re-parse commands from data to find their positions
        // 2. For each command with Offset parameters, calculate relative offset
        // 3. Replace the offset bytes in data

        // For now, this is left as a placeholder
        // Offsets written during compilation will be used as-is
        // This means Function#1, Script#2 references will have raw ID values instead of proper offsets
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
