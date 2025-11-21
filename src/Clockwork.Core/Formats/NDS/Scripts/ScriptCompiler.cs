using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Clockwork.Core.Formats.NDS.Scripts;

/// <summary>
/// Compiles script text to binary format
/// </summary>
public static class ScriptCompiler
{
    // Regex to match command lines: "    CommandName param1 param2 param3"
    // Commands are words starting with a letter, followed by optional space-separated parameters
    private static readonly Regex CommandRegex = new Regex(
        @"^\s*([a-zA-Z_]\w*)(?:\s+(.+?))?\s*$",
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

            var match = CommandRegex.Match(trimmed);
            if (!match.Success)
                continue;

            string commandName = match.Groups[1].Value;
            string paramsStr = match.Groups[2].Success ? match.Groups[2].Value : "";

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
    /// Splits parameter string by spaces (e.g., "1500" or "0x800C 0" or "EQUAL Function#1")
    /// </summary>
    private static List<string> SplitParameters(string paramsStr)
    {
        if (string.IsNullOrWhiteSpace(paramsStr))
            return new List<string>();

        // Split by whitespace (space, tab, etc.)
        return paramsStr.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrEmpty(p))
            .ToList();
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
                        byte byteValue = ParseNumericOrConstant<byte>(value, commandName, paramIndex, "Byte", 0, 255);
                        return new[] { byteValue };
                    }

                case ScriptParameterType.Word:
                    {
                        ushort wordValue = ParseNumericOrConstant<ushort>(value, commandName, paramIndex, "Word", 0, 65535);
                        return BitConverter.GetBytes(wordValue);
                    }

                case ScriptParameterType.DWord:
                    {
                        uint dwordValue = ParseNumericOrConstant<uint>(value, commandName, paramIndex, "DWord", 0, uint.MaxValue);
                        return BitConverter.GetBytes(dwordValue);
                    }

                case ScriptParameterType.Offset:
                    {
                        // Handle special references like "Function#1", "Script#2", etc.
                        if (TryParseReference(value, out uint refValue))
                        {
                            return BitConverter.GetBytes(refValue);
                        }

                        // Remove @ prefix if present
                        if (value.StartsWith("@"))
                            value = value.Substring(1);

                        uint offsetValue = ParseNumericOrConstant<uint>(value, commandName, paramIndex, "Offset", 0, uint.MaxValue);
                        return BitConverter.GetBytes(offsetValue);
                    }

                case ScriptParameterType.Variable:
                    {
                        // For variable parameters, try to parse as single value first
                        // Then fall back to multiple values
                        var parts = value.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        var bytes = new List<byte>();

                        foreach (var part in parts)
                        {
                            try
                            {
                                bytes.Add(ParseNumeric<byte>(part));
                            }
                            catch
                            {
                                throw new ScriptCompilationException(
                                    $"Invalid variable parameter #{paramIndex + 1} for command '{commandName}': " +
                                    $"Cannot parse '{part}' as byte value. Expected format: '0x01 0x02' or '1 2'");
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
    /// Tries to parse references like "Function#1", "Script#2", etc.
    /// Encodes the reference type using special markers in the high bits
    /// </summary>
    private static bool TryParseReference(string value, out uint result)
    {
        result = 0;

        // Match patterns like "Function#1", "Script#2", "Action#3"
        var match = System.Text.RegularExpressions.Regex.Match(value, @"^(Function|Script|Action)#(\d+)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (match.Success)
        {
            string type = match.Groups[1].Value;
            uint id = uint.Parse(match.Groups[2].Value);

            // Encode type in high bits using special markers
            // These will be resolved to actual offsets during FixRelativeOffsets
            // Format: [8 bits type marker][24 bits ID]
            const uint FUNCTION_MARKER = 0x80000000;  // Bit 31 set
            const uint SCRIPT_MARKER = 0x40000000;    // Bit 30 set
            const uint ACTION_MARKER = 0x20000000;    // Bit 29 set

            uint marker = type.ToLower() switch
            {
                "function" => FUNCTION_MARKER,
                "script" => SCRIPT_MARKER,
                "action" => ACTION_MARKER,
                _ => 0
            };

            // Combine marker with ID (ID should fit in 24 bits)
            if (id > 0x00FFFFFF)
            {
                throw new ScriptCompilationException($"Reference ID {id} is too large (max: 16777215)");
            }

            result = marker | id;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Parses numeric value or named constant with validation
    /// </summary>
    private static T ParseNumericOrConstant<T>(string value, string commandName, int paramIndex, string typeName, long min, long max) where T : struct
    {
        // First try to parse as numeric
        if (TryParseNumeric<T>(value, out T numericResult))
        {
            // Validate range
            long numValue = Convert.ToInt64(numericResult);
            if (numValue >= min && numValue <= max)
            {
                return numericResult;
            }

            throw new ScriptCompilationException(
                $"Parameter #{paramIndex + 1} for command '{commandName}' is out of range. " +
                $"Value '{value}' ({numValue}) must be between {min} and {max} for type {typeName}");
        }

        // Try to parse as constant
        if (TryParseConstant(value, out long constValue))
        {
            if (constValue >= min && constValue <= max)
            {
                return (T)Convert.ChangeType(constValue, typeof(T));
            }

            throw new ScriptCompilationException(
                $"Constant '{value}' (value: {constValue}) is out of range for type {typeName} (range: {min}-{max})");
        }

        throw new ScriptCompilationException(
            $"Invalid {typeName} parameter #{paramIndex + 1} for command '{commandName}': " +
            $"Cannot parse '{value}' as a number or known constant. Expected format: decimal (e.g., '10') or hex (e.g., '0x0A')");
    }

    /// <summary>
    /// Tries to parse a value as numeric (decimal or hex)
    /// </summary>
    private static bool TryParseNumeric<T>(string value, out T result) where T : struct
    {
        result = default;
        value = value.Trim();

        try
        {
            result = ParseNumeric<T>(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Tries to parse a named constant (like "EQUAL", "TRUE", etc.)
    /// </summary>
    private static bool TryParseConstant(string value, out long result)
    {
        result = 0;

        // Define common constants used in scripts
        var constants = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase)
        {
            // Comparison operators
            { "EQUAL", 0 },
            { "NOTEQUAL", 1 },
            { "LESS", 2 },
            { "LESSOREQUAL", 3 },
            { "GREATER", 4 },
            { "GREATEROREQUAL", 5 },

            // Boolean values
            { "FALSE", 0 },
            { "TRUE", 1 },

            // Common values
            { "NULL", 0 },
            { "NONE", 0 }
        };

        if (constants.TryGetValue(value, out result))
        {
            return true;
        }

        return false;
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
    /// Parses multiple containers from text (separated by headers like "Script 1:", "Function 2:", "Action 3:")
    /// </summary>
    public static List<ScriptContainer> ParseContainersFromText(string text, ContainerType type)
    {
        var containers = new List<ScriptContainer>();

        if (string.IsNullOrWhiteSpace(text))
            return containers;

        // Regex to match container headers: "Script 1:", "Function 2:", "Action 3:"
        var headerRegex = new Regex(@"^(Script|Function|Action)\s+(\d+)\s*:\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase);
        var matches = headerRegex.Matches(text);

        if (matches.Count == 0)
        {
            // No headers found - treat entire text as a single container with ID 0
            var container = CompileContainer(text, type, 0);
            if (container.Commands.Count > 0)
            {
                containers.Add(container);
            }
            return containers;
        }

        // Parse each container block
        for (int i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            string containerTypeStr = match.Groups[1].Value;
            uint id = uint.Parse(match.Groups[2].Value);

            // Verify the header type matches the expected container type
            ContainerType detectedType = containerTypeStr.ToLower() switch
            {
                "script" => ContainerType.Script,
                "function" => ContainerType.Function,
                "action" => ContainerType.Action,
                _ => type // Fallback to expected type
            };

            if (detectedType != type)
            {
                // Skip containers that don't match the expected type
                // (e.g., if parsing Scripts tab, skip any Function/Action headers that might be there)
                continue;
            }

            // Extract content between this header and the next one (or end of file)
            int startIndex = match.Index + match.Length;
            int endIndex = (i < matches.Count - 1) ? matches[i + 1].Index : text.Length;
            string containerText = text.Substring(startIndex, endIndex - startIndex).Trim();

            // Remove trailing "End" if present
            if (containerText.EndsWith("End", StringComparison.OrdinalIgnoreCase))
            {
                containerText = containerText.Substring(0, containerText.Length - 3).Trim();
            }

            try
            {
                var container = CompileContainer(containerText, type, id);
                if (container.Commands.Count > 0)
                {
                    containers.Add(container);
                }
            }
            catch (ScriptCompilationException ex)
            {
                // Add container ID to error message
                throw new ScriptCompilationException($"{type} {id}: {ex.Message}", ex);
            }
        }

        return containers;
    }
}
