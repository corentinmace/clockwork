using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Clockwork.Core.Formats.NDS.Scripts;

/// <summary>
/// Compiles script text to binary format
/// </summary>
public static class ScriptCompiler
{
    // Regex to match command lines: "    CommandName(param1, param2, ...)"
    private static readonly Regex CommandRegex = new Regex(
        @"^\s*(\w+)\s*\((.*?)\)\s*$",
        RegexOptions.Compiled
    );

    /// <summary>
    /// Compiles script text to a ScriptContainer
    /// </summary>
    public static ScriptContainer CompileContainer(string text, ContainerType type, uint id)
    {
        var container = new ScriptContainer
        {
            ID = id,
            Type = type,
            Commands = new List<ScriptCommand>()
        };

        var lines = text.Split('\n');

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            // Skip empty lines and comments
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("//"))
                continue;

            var match = CommandRegex.Match(line);
            if (!match.Success)
                continue;

            string commandName = match.Groups[1].Value;
            string paramsStr = match.Groups[2].Value;

            // Look up command in database
            var commandInfo = ScriptDatabase.GetCommandInfo(commandName);
            if (commandInfo == null)
            {
                throw new Exception($"Unknown command: {commandName}");
            }

            // Parse parameters
            var paramValues = ParseParameters(paramsStr, commandInfo);

            // Create command
            var command = new ScriptCommand
            {
                CommandID = commandInfo.ID,
                Parameters = paramValues
            };

            container.Commands.Add(command);
        }

        return container;
    }

    /// <summary>
    /// Parses parameter string into byte arrays
    /// </summary>
    private static List<byte[]> ParseParameters(string paramsStr, ScriptCommandInfo commandInfo)
    {
        var result = new List<byte[]>();

        if (string.IsNullOrWhiteSpace(paramsStr))
            return result;

        var paramStrings = SplitParameters(paramsStr);

        if (paramStrings.Count != commandInfo.Parameters.Count)
        {
            throw new Exception($"Command {commandInfo.Name} expects {commandInfo.Parameters.Count} parameters, got {paramStrings.Count}");
        }

        for (int i = 0; i < paramStrings.Count; i++)
        {
            var paramStr = paramStrings[i].Trim();
            var paramType = commandInfo.Parameters[i];

            byte[] paramBytes = EncodeParameter(paramStr, paramType);
            result.Add(paramBytes);
        }

        return result;
    }

    /// <summary>
    /// Splits parameter string by commas, handling nested parentheses
    /// </summary>
    private static List<string> SplitParameters(string paramsStr)
    {
        var result = new List<string>();
        var current = new StringBuilder();
        int depth = 0;

        foreach (char c in paramsStr)
        {
            if (c == '(' || c == '[' || c == '{')
            {
                depth++;
                current.Append(c);
            }
            else if (c == ')' || c == ']' || c == '}')
            {
                depth--;
                current.Append(c);
            }
            else if (c == ',' && depth == 0)
            {
                result.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0)
        {
            result.Add(current.ToString());
        }

        return result;
    }

    /// <summary>
    /// Encodes a parameter value to bytes
    /// </summary>
    private static byte[] EncodeParameter(string value, ScriptParameterType type)
    {
        value = value.Trim();

        switch (type)
        {
            case ScriptParameterType.Byte:
                {
                    byte byteValue = ParseNumeric<byte>(value);
                    return new[] { byteValue };
                }

            case ScriptParameterType.Word:
                {
                    ushort wordValue = ParseNumeric<ushort>(value);
                    return BitConverter.GetBytes(wordValue);
                }

            case ScriptParameterType.DWord:
                {
                    uint dwordValue = ParseNumeric<uint>(value);
                    return BitConverter.GetBytes(dwordValue);
                }

            case ScriptParameterType.Offset:
                {
                    // Remove @ prefix if present
                    if (value.StartsWith("@"))
                        value = value.Substring(1);

                    uint offsetValue = ParseNumeric<uint>(value);
                    return BitConverter.GetBytes(offsetValue);
                }

            case ScriptParameterType.Variable:
                {
                    // For variable parameters, try to parse as hex bytes
                    // Format: "0x01 0x02 0x03" or "01 02 03"
                    var hexValues = value.Split(new[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    var bytes = new List<byte>();

                    foreach (var hex in hexValues)
                    {
                        bytes.Add(ParseNumeric<byte>(hex));
                    }

                    return bytes.ToArray();
                }

            default:
                throw new Exception($"Unknown parameter type: {type}");
        }
    }

    /// <summary>
    /// Parses numeric value (supports decimal and hex)
    /// </summary>
    private static T ParseNumeric<T>(string value) where T : struct
    {
        value = value.Trim();

        // Handle hex (0x prefix)
        if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            value = value.Substring(2);

            if (typeof(T) == typeof(byte))
                return (T)(object)byte.Parse(value, NumberStyles.HexNumber);
            else if (typeof(T) == typeof(ushort))
                return (T)(object)ushort.Parse(value, NumberStyles.HexNumber);
            else if (typeof(T) == typeof(uint))
                return (T)(object)uint.Parse(value, NumberStyles.HexNumber);
        }

        // Handle decimal
        if (typeof(T) == typeof(byte))
            return (T)(object)byte.Parse(value);
        else if (typeof(T) == typeof(ushort))
            return (T)(object)ushort.Parse(value);
        else if (typeof(T) == typeof(uint))
            return (T)(object)uint.Parse(value);

        throw new Exception($"Unsupported numeric type: {typeof(T)}");
    }

    /// <summary>
    /// Compiles all three tabs into a ScriptFile
    /// </summary>
    public static ScriptFile CompileScriptFile(int fileID, string scriptText, string functionText, string actionText)
    {
        var scriptFile = new ScriptFile
        {
            FileID = fileID,
            IsLevelScript = false,
            Scripts = new List<ScriptContainer>(),
            Functions = new List<ScriptContainer>(),
            Actions = new List<ScriptContainer>()
        };

        // Parse Scripts tab
        var scriptContainers = ParseContainersFromText(scriptText, ContainerType.Script);
        scriptFile.Scripts.AddRange(scriptContainers);

        // Parse Functions tab
        var functionContainers = ParseContainersFromText(functionText, ContainerType.Function);
        scriptFile.Functions.AddRange(functionContainers);

        // Parse Actions tab
        var actionContainers = ParseContainersFromText(actionText, ContainerType.Action);
        scriptFile.Actions.AddRange(actionContainers);

        return scriptFile;
    }

    /// <summary>
    /// Parses multiple containers from text (separated by comments)
    /// </summary>
    private static List<ScriptContainer> ParseContainersFromText(string text, ContainerType type)
    {
        var containers = new List<ScriptContainer>();

        if (string.IsNullOrWhiteSpace(text))
            return containers;

        // For now, treat the entire text as a single container
        // TODO: Support multiple containers separated by comment headers
        var container = CompileContainer(text, type, 0);

        if (container.Commands.Count > 0)
        {
            containers.Add(container);
        }

        return containers;
    }
}
