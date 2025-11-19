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
    /// Converts the script file to binary format
    /// </summary>
    public byte[] ToBytes()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Simple format: Write header with counts, then all container data
        // Header structure:
        // - ushort: Script count
        // - ushort: Function count
        // - ushort: Action count
        // - ushort: Reserved (padding)

        writer.Write((ushort)Scripts.Count);
        writer.Write((ushort)Functions.Count);
        writer.Write((ushort)Actions.Count);
        writer.Write((ushort)0); // Reserved

        // Calculate and write offset tables
        List<uint> scriptOffsets = new();
        List<uint> functionOffsets = new();
        List<uint> actionOffsets = new();

        uint currentOffset = (uint)(8 + (Scripts.Count + Functions.Count + Actions.Count) * 4);

        // Calculate script offsets
        foreach (var script in Scripts)
        {
            scriptOffsets.Add(currentOffset);
            currentOffset += (uint)script.GetTotalSize();
        }

        // Calculate function offsets
        foreach (var function in Functions)
        {
            functionOffsets.Add(currentOffset);
            currentOffset += (uint)function.GetTotalSize();
        }

        // Calculate action offsets
        foreach (var action in Actions)
        {
            actionOffsets.Add(currentOffset);
            currentOffset += (uint)action.GetTotalSize();
        }

        // Write offset tables
        foreach (var offset in scriptOffsets)
            writer.Write(offset);
        foreach (var offset in functionOffsets)
            writer.Write(offset);
        foreach (var offset in actionOffsets)
            writer.Write(offset);

        // Write container data
        foreach (var script in Scripts)
        {
            var data = script.ToBytes();
            writer.Write(data);
        }

        foreach (var function in Functions)
        {
            var data = function.ToBytes();
            writer.Write(data);
        }

        foreach (var action in Actions)
        {
            var data = action.ToBytes();
            writer.Write(data);
        }

        return ms.ToArray();
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
