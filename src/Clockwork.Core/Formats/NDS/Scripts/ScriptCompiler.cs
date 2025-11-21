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

    // Regex to detect lines that look like commands but have invalid syntax
    private static readonly Regex PotentialCommandRegex = new Regex(
        @"^\s*([a-zA-Z_]\w*)",
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
        int lineNumber = 0;

        foreach (var line in lines)
        {
            lineNumber++;
            var trimmed = line.Trim();

            // Skip empty lines and comments
            if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("//"))
                continue;

            var match = CommandRegex.Match(line);
            if (!match.Success)
            {
                // Check if this looks like a command but has invalid syntax
                var potentialMatch = PotentialCommandRegex.Match(trimmed);
                if (potentialMatch.Success)
                {
                    string potentialCommand = potentialMatch.Groups[1].Value;
                    throw new ScriptCompilationException(
                        $"Line {lineNumber}: Invalid command syntax '{trimmed}'. " +
                        $"Did you mean '{potentialCommand}()'? Commands must use the format: CommandName(param1, param2, ...)");
                }
                continue;
            }

            string commandName = match.Groups[1].Value;
            string paramsStr = match.Groups[2].Value;

            // Look up command in database
            var commandInfo = ScriptDatabase.GetCommandInfo(commandName);
            if (commandInfo == null)
            {
                throw new ScriptCompilationException(
                    $"Line {lineNumber}: Unknown command '{commandName}'. This command is not defined in the script command database. " +
                    $"Check the command name or update ScriptCommands.json in %appdata%/Clockwork/Scrcmd/");
            }

            // Parse parameters
            List<byte[]> paramValues;
            try
            {
                paramValues = ParseParameters(paramsStr, commandInfo);
            }
            catch (ScriptCompilationException ex)
            {
                // Add line number to error message if not already present
                if (!ex.Message.Contains("Line "))
                {
                    throw new ScriptCompilationException($"Line {lineNumber}: {ex.Message}", ex);
                }
                throw;
            }

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
        {
            if (commandInfo.Parameters.Count > 0)
            {
                throw new ScriptCompilationException(
                    $"Command '{commandInfo.Name}' expects {commandInfo.Parameters.Count} parameter(s), but got 0. " +
                    $"Expected: {commandInfo.GetSignature()}");
            }
            return result;
        }

        var paramStrings = SplitParameters(paramsStr);

        if (paramStrings.Count != commandInfo.Parameters.Count)
        {
            throw new ScriptCompilationException(
                $"Command '{commandInfo.Name}' expects {commandInfo.Parameters.Count} parameter(s), but got {paramStrings.Count}. " +
                $"Expected: {commandInfo.GetSignature()}");
        }

        for (int i = 0; i < paramStrings.Count; i++)
        {
            var paramStr = paramStrings[i].Trim();
            var paramType = commandInfo.Parameters[i];

            try
            {
                byte[] paramBytes = EncodeParameter(paramStr, paramType, commandInfo.Name, i);
                result.Add(paramBytes);
            }
            catch (ScriptCompilationException)
            {
                throw; // Re-throw our custom exception
            }
            catch (Exception ex)
            {
                throw new ScriptCompilationException(
                    $"Invalid parameter #{i + 1} for command '{commandInfo.Name}': " +
                    $"Cannot parse '{paramStr}' as {paramType}. {ex.Message}");
            }
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
    /// Encodes a parameter value to bytes with validation
    /// </summary>
    private static byte[] EncodeParameter(string value, ScriptParameterType type, string commandName, int paramIndex)
    {
        value = value.Trim();

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ScriptCompilationException(
                $"Parameter #{paramIndex + 1} for command '{commandName}' cannot be empty");
        }

        try
        {
            switch (type)
            {
                case ScriptParameterType.Byte:
                    {
                        byte byteValue = ParseNumericWithValidation<byte>(value, commandName, paramIndex, "Byte", 0, 255);
                        return new[] { byteValue };
                    }

                case ScriptParameterType.Word:
                    {
                        ushort wordValue = ParseNumericWithValidation<ushort>(value, commandName, paramIndex, "Word", 0, 65535);
                        return BitConverter.GetBytes(wordValue);
                    }

                case ScriptParameterType.DWord:
                    {
                        uint dwordValue = ParseNumericWithValidation<uint>(value, commandName, paramIndex, "DWord", 0, uint.MaxValue);
                        return BitConverter.GetBytes(dwordValue);
                    }

                case ScriptParameterType.Offset:
                    {
                        // Remove @ prefix if present
                        if (value.StartsWith("@"))
                            value = value.Substring(1);

                        uint offsetValue = ParseNumericWithValidation<uint>(value, commandName, paramIndex, "Offset", 0, uint.MaxValue);
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
                            try
                            {
                                bytes.Add(ParseNumeric<byte>(hex));
                            }
                            catch
                            {
                                throw new ScriptCompilationException(
                                    $"Invalid variable parameter #{paramIndex + 1} for command '{commandName}': " +
                                    $"Cannot parse '{hex}' as byte value. Expected format: '0x01 0x02' or '1 2'");
                            }
                        }

                        return bytes.ToArray();
                    }

                default:
                    throw new ScriptCompilationException(
                        $"Unknown parameter type '{type}' for parameter #{paramIndex + 1} in command '{commandName}'");
            }
        }
        catch (ScriptCompilationException)
        {
            throw; // Re-throw our custom exceptions
        }
        catch (Exception ex)
        {
            throw new ScriptCompilationException(
                $"Failed to encode parameter #{paramIndex + 1} for command '{commandName}': {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Parses numeric value with validation
    /// </summary>
    private static T ParseNumericWithValidation<T>(string value, string commandName, int paramIndex, string typeName, long min, long max) where T : struct
    {
        try
        {
            T result = ParseNumeric<T>(value);

            // Validate range
            long numValue = Convert.ToInt64(result);
            if (numValue < min || numValue > max)
            {
                throw new ScriptCompilationException(
                    $"Parameter #{paramIndex + 1} for command '{commandName}' is out of range. " +
                    $"Value '{value}' ({numValue}) must be between {min} and {max} for type {typeName}");
            }

            return result;
        }
        catch (ScriptCompilationException)
        {
            throw; // Re-throw our custom exceptions
        }
        catch (FormatException)
        {
            throw new ScriptCompilationException(
                $"Invalid {typeName} parameter #{paramIndex + 1} for command '{commandName}': " +
                $"Cannot parse '{value}' as a number. Expected format: decimal (e.g., '10') or hex (e.g., '0x0A')");
        }
        catch (OverflowException)
        {
            throw new ScriptCompilationException(
                $"Parameter #{paramIndex + 1} for command '{commandName}' is too large for type {typeName}. " +
                $"Value '{value}' must be between {min} and {max}");
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
