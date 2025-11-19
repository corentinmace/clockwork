namespace Clockwork.Core.Formats.NDS.Scripts;

/// <summary>
/// Represents a single script command with its ID and parameters
/// </summary>
public class ScriptCommand
{
    /// <summary>
    /// Command ID (2 bytes)
    /// </summary>
    public ushort CommandID { get; set; }

    /// <summary>
    /// Command parameters (raw bytes)
    /// </summary>
    public List<byte[]> Parameters { get; set; } = new();

    /// <summary>
    /// Original offset in the file (for reference/debugging)
    /// </summary>
    public uint Offset { get; set; }

    public ScriptCommand()
    {
    }

    public ScriptCommand(ushort commandID, List<byte[]> parameters)
    {
        CommandID = commandID;
        Parameters = parameters;
    }

    /// <summary>
    /// Gets the total size of this command in bytes (ID + all parameters)
    /// </summary>
    public int GetTotalSize()
    {
        int size = 2; // Command ID
        foreach (var param in Parameters)
        {
            size += param.Length;
        }
        return size;
    }

    /// <summary>
    /// Converts command to binary representation
    /// </summary>
    public byte[] ToBytes()
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);

        // Write command ID
        writer.Write(CommandID);

        // Write all parameters
        foreach (var param in Parameters)
        {
            writer.Write(param);
        }

        return ms.ToArray();
    }
}
