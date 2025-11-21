namespace Clockwork.Core.Formats.NDS.Scripts;

/// <summary>
/// Parameter type for script commands
/// </summary>
public enum ScriptParameterType
{
    /// <summary>
    /// 1 byte (byte)
    /// </summary>
    Byte,

    /// <summary>
    /// 2 bytes (ushort)
    /// </summary>
    Word,

    /// <summary>
    /// 4 bytes (uint)
    /// </summary>
    DWord,

    /// <summary>
    /// Variable length parameter
    /// </summary>
    Variable,

    /// <summary>
    /// Offset/pointer to another location
    /// </summary>
    Offset
}

/// <summary>
/// Information about a script command (name and parameters)
/// </summary>
public class ScriptCommandInfo
{
    /// <summary>
    /// Command ID
    /// </summary>
    public ushort ID { get; set; }

    /// <summary>
    /// Command name
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Parameter types for this command
    /// </summary>
    public List<ScriptParameterType> Parameters { get; set; } = new();

    /// <summary>
    /// Parameter names (optional, for display)
    /// </summary>
    public List<string> ParameterNames { get; set; } = new();

    /// <summary>
    /// Description of what this command does
    /// </summary>
    public string Description { get; set; } = string.Empty;

    public ScriptCommandInfo()
    {
    }

    public ScriptCommandInfo(ushort id, string name, params ScriptParameterType[] parameters)
    {
        ID = id;
        Name = name;
        Parameters = parameters.ToList();
    }

    /// <summary>
    /// Gets a human-readable signature for this command
    /// Format: "CommandName param1:type param2:type ..."
    /// </summary>
    public string GetSignature()
    {
        if (Parameters.Count == 0)
            return Name;

        var paramList = new List<string>();
        for (int i = 0; i < Parameters.Count; i++)
        {
            string paramName = i < ParameterNames.Count && !string.IsNullOrEmpty(ParameterNames[i])
                ? ParameterNames[i]
                : $"param{i + 1}";
            string paramType = Parameters[i].ToString().ToLower();
            paramList.Add($"{paramName}:{paramType}");
        }

        return $"{Name} {string.Join(" ", paramList)}";
    }
}
