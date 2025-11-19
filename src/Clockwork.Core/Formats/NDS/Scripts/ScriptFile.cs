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

        // TODO: Implement binary writing
        // This will need to write headers and all containers
        // Format specifics depend on the game version

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
