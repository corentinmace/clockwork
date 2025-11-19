namespace Clockwork.Core.Formats.NDS.Scripts;

/// <summary>
/// Type of script container
/// </summary>
public enum ContainerType
{
    Script,
    Function,
    Action
}

/// <summary>
/// Container for a sequence of script commands (script, function, or action)
/// </summary>
public class ScriptContainer
{
    /// <summary>
    /// Container ID/number
    /// </summary>
    public uint ID { get; set; }

    /// <summary>
    /// Type of container (Script, Function, or Action)
    /// </summary>
    public ContainerType Type { get; set; }

    /// <summary>
    /// List of commands in this container
    /// </summary>
    public List<ScriptCommand> Commands { get; set; } = new();

    /// <summary>
    /// Original offset in the file
    /// </summary>
    public uint Offset { get; set; }

    /// <summary>
    /// Optional name/label for this container
    /// </summary>
    public string? Name { get; set; }

    public ScriptContainer()
    {
    }

    public ScriptContainer(uint id, ContainerType type)
    {
        ID = id;
        Type = type;
    }

    /// <summary>
    /// Gets the total size of this container in bytes
    /// </summary>
    public int GetTotalSize()
    {
        int size = 0;
        foreach (var command in Commands)
        {
            size += command.GetTotalSize();
        }
        return size;
    }

    /// <summary>
    /// Converts container to binary representation
    /// </summary>
    public byte[] ToBytes()
    {
        using var ms = new MemoryStream();

        foreach (var command in Commands)
        {
            var commandBytes = command.ToBytes();
            ms.Write(commandBytes, 0, commandBytes.Length);
        }

        return ms.ToArray();
    }

    public override string ToString()
    {
        string typeName = Type switch
        {
            ContainerType.Script => "Script",
            ContainerType.Function => "Function",
            ContainerType.Action => "Action",
            _ => "Unknown"
        };

        return string.IsNullOrEmpty(Name)
            ? $"{typeName} {ID}"
            : $"{typeName} {ID}: {Name}";
    }
}
