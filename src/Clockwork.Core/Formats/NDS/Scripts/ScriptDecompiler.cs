using System.Text;

namespace Clockwork.Core.Formats.NDS.Scripts;

/// <summary>
/// Converts binary script data to human-readable text format
/// </summary>
public static class ScriptDecompiler
{
    /// <summary>
    /// Decompiles a script container to text
    /// </summary>
    public static string DecompileContainer(ScriptContainer container)
    {
        var sb = new StringBuilder();

        // Header comment
        sb.AppendLine($"// {container.Type} {container.ID}");
        if (!string.IsNullOrEmpty(container.Name))
        {
            sb.AppendLine($"// Name: {container.Name}");
        }
        sb.AppendLine($"// Offset: 0x{container.Offset:X8}");
        sb.AppendLine();

        // Decompile each command
        foreach (var command in container.Commands)
        {
            string line = DecompileCommand(command);
            sb.AppendLine(line);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Decompiles a single command to text
    /// </summary>
    public static string DecompileCommand(ScriptCommand command)
    {
        var info = ScriptDatabase.GetCommandInfo(command.CommandID);
        string commandName = info?.Name ?? $"CMD_0x{command.CommandID:X4}";

        if (command.Parameters.Count == 0)
        {
            return $"    {commandName}()";
        }

        // Format parameters
        var paramStrings = new List<string>();
        for (int i = 0; i < command.Parameters.Count; i++)
        {
            var param = command.Parameters[i];
            string paramStr = FormatParameter(param, info?.Parameters.ElementAtOrDefault(i));
            paramStrings.Add(paramStr);
        }

        return $"    {commandName}({string.Join(", ", paramStrings)})";
    }

    /// <summary>
    /// Formats a parameter value based on its type
    /// </summary>
    private static string FormatParameter(byte[] data, ScriptParameterType? type)
    {
        if (data.Length == 0)
            return "0";

        // Format based on type hint if available
        if (type.HasValue)
        {
            return type.Value switch
            {
                ScriptParameterType.Byte => data[0].ToString(),
                ScriptParameterType.Word => BitConverter.ToUInt16(data, 0).ToString(),
                ScriptParameterType.DWord => BitConverter.ToUInt32(data, 0).ToString(),
                ScriptParameterType.Offset => $"@{BitConverter.ToUInt32(data, 0):X8}",
                _ => FormatRawBytes(data)
            };
        }

        // Auto-detect format based on length
        return data.Length switch
        {
            1 => data[0].ToString(),
            2 => BitConverter.ToUInt16(data, 0).ToString(),
            4 => BitConverter.ToUInt32(data, 0).ToString(),
            _ => FormatRawBytes(data)
        };
    }

    /// <summary>
    /// Formats raw bytes as hex string
    /// </summary>
    private static string FormatRawBytes(byte[] data)
    {
        return "0x" + BitConverter.ToString(data).Replace("-", "");
    }

    /// <summary>
    /// Decompiles an entire script file to text
    /// </summary>
    public static string DecompileFile(ScriptFile file)
    {
        var sb = new StringBuilder();

        // File header
        sb.AppendLine($"// ======================================");
        sb.AppendLine($"// Script File {file.FileID}");
        sb.AppendLine($"// ======================================");
        sb.AppendLine();

        // Scripts section
        if (file.Scripts.Count > 0)
        {
            sb.AppendLine("// ========== SCRIPTS ==========");
            sb.AppendLine();

            foreach (var script in file.Scripts)
            {
                sb.Append(DecompileContainer(script));
                sb.AppendLine();
            }
        }

        // Functions section
        if (file.Functions.Count > 0)
        {
            sb.AppendLine("// ========== FUNCTIONS ==========");
            sb.AppendLine();

            foreach (var function in file.Functions)
            {
                sb.Append(DecompileContainer(function));
                sb.AppendLine();
            }
        }

        // Actions section
        if (file.Actions.Count > 0)
        {
            sb.AppendLine("// ========== ACTIONS ==========");
            sb.AppendLine();

            foreach (var action in file.Actions)
            {
                sb.Append(DecompileContainer(action));
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }
}
