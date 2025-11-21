using Clockwork.Core.Logging;
using System.Reflection;
using System.Text.Json;

namespace Clockwork.Core.Services;

/// <summary>
/// Service for managing script command configuration files in %appdata%/Clockwork/Scrcmd/
/// This allows users to modify command definitions without recompiling.
/// </summary>
public class ScriptCommandConfigService : IApplicationService
{
    private static readonly string AppDataFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Clockwork",
        "Scrcmd"
    );

    private static readonly string CommandsFilePath = Path.Combine(AppDataFolder, "ScriptCommands.json");

    public ScriptCommandConfigService()
    {
    }

    public void Initialize()
    {
        EnsureConfigFileExists();
        AppLogger.Info($"ScriptCommandConfigService initialized. Config path: {AppDataFolder}");
    }

    public void Update(double deltaTime)
    {
        // No per-frame updates needed
    }

    public void Dispose()
    {
        AppLogger.Debug("ScriptCommandConfigService disposed");
    }

    /// <summary>
    /// Ensures the configuration file exists in %appdata%, copying from resources if needed
    /// </summary>
    private void EnsureConfigFileExists()
    {
        try
        {
            // Create directory if it doesn't exist
            if (!Directory.Exists(AppDataFolder))
            {
                Directory.CreateDirectory(AppDataFolder);
                AppLogger.Info($"Created Scrcmd config directory: {AppDataFolder}");
            }

            // If file doesn't exist, copy from embedded resources
            if (!File.Exists(CommandsFilePath))
            {
                CopyDefaultConfigFromResources();
            }
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to ensure config file exists: {ex.Message}");
        }
    }

    /// <summary>
    /// Copies the default ScriptCommands.json from embedded resources to %appdata%
    /// </summary>
    private void CopyDefaultConfigFromResources()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Clockwork.Core.Resources.ScriptCommands.json";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                AppLogger.Error("Failed to load default ScriptCommands.json from resources");
                return;
            }

            using var fileStream = File.Create(CommandsFilePath);
            stream.CopyTo(fileStream);

            AppLogger.Info($"Copied default ScriptCommands.json to {CommandsFilePath}");
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to copy default config from resources: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads script commands from the configuration file
    /// </summary>
    /// <returns>Dictionary of command ID to command info, or null if loading fails</returns>
    public Dictionary<ushort, Formats.NDS.Scripts.ScriptCommandInfo>? LoadCommands()
    {
        try
        {
            if (!File.Exists(CommandsFilePath))
            {
                AppLogger.Warn($"ScriptCommands.json not found at {CommandsFilePath}");
                return null;
            }

            var jsonContent = File.ReadAllText(CommandsFilePath);
            return ParseCommandsJson(jsonContent);
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to load script commands from {CommandsFilePath}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Loads constants (movements, comparisons, etc.) from the configuration file
    /// </summary>
    public (Dictionary<string, uint> movements,
            Dictionary<string, uint> comparisonOperators,
            Dictionary<string, uint> specialOverworlds,
            Dictionary<string, uint> overworldDirections) LoadConstants()
    {
        var movements = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
        var comparisonOperators = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
        var specialOverworlds = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
        var overworldDirections = new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);

        try
        {
            if (!File.Exists(CommandsFilePath))
            {
                return (movements, comparisonOperators, specialOverworlds, overworldDirections);
            }

            var jsonContent = File.ReadAllText(CommandsFilePath);
            var jsonDoc = JsonDocument.Parse(jsonContent);
            var root = jsonDoc.RootElement;

            // Load movements
            if (root.TryGetProperty("movements", out var movementsElement))
            {
                foreach (var prop in movementsElement.EnumerateObject())
                {
                    if (uint.TryParse(prop.Value.ToString(), out uint value))
                    {
                        movements[prop.Name] = value;
                    }
                }
            }

            // Load comparison operators
            if (root.TryGetProperty("comparisonOperators", out var compOperatorsElement))
            {
                foreach (var prop in compOperatorsElement.EnumerateObject())
                {
                    if (uint.TryParse(prop.Value.ToString(), out uint value))
                    {
                        comparisonOperators[prop.Name] = value;
                    }
                }
            }

            // Load special overworlds
            if (root.TryGetProperty("specialOverworlds", out var specialOwElement))
            {
                foreach (var prop in specialOwElement.EnumerateObject())
                {
                    if (uint.TryParse(prop.Value.ToString(), out uint value))
                    {
                        specialOverworlds[prop.Name] = value;
                    }
                }
            }

            // Load overworld directions
            if (root.TryGetProperty("overworldDirections", out var owDirectionsElement))
            {
                foreach (var prop in owDirectionsElement.EnumerateObject())
                {
                    if (uint.TryParse(prop.Value.ToString(), out uint value))
                    {
                        overworldDirections[prop.Name] = value;
                    }
                }
            }

            AppLogger.Info($"Loaded constants: {movements.Count} movements, {comparisonOperators.Count} operators, {specialOverworlds.Count} overworlds, {overworldDirections.Count} directions");
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to load constants from {CommandsFilePath}: {ex.Message}");
        }

        return (movements, comparisonOperators, specialOverworlds, overworldDirections);
    }

    /// <summary>
    /// Parses JSON content into command dictionary
    /// </summary>
    private Dictionary<ushort, Formats.NDS.Scripts.ScriptCommandInfo> ParseCommandsJson(string jsonContent)
    {
        var commands = new Dictionary<ushort, Formats.NDS.Scripts.ScriptCommandInfo>();

        try
        {
            var jsonDoc = JsonDocument.Parse(jsonContent);
            var root = jsonDoc.RootElement;

            // Try the real scrcmd format first (from LiTRE)
            if (root.TryGetProperty("scrcmd", out var scrcmdElement))
            {
                foreach (var cmdProp in scrcmdElement.EnumerateObject())
                {
                    try
                    {
                        // Parse hex ID from key (e.g., "0x0002" -> 2)
                        string hexKey = cmdProp.Name;
                        if (!hexKey.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                        {
                            AppLogger.Warn($"Skipping invalid scrcmd key: {hexKey}");
                            continue;
                        }

                        ushort id = Convert.ToUInt16(hexKey.Substring(2), 16);
                        var cmdElement = cmdProp.Value;

                        var name = cmdElement.TryGetProperty("name", out var nameProp)
                            ? nameProp.GetString() ?? "Unknown"
                            : "Unknown";

                        var description = cmdElement.TryGetProperty("description", out var descProp)
                            ? descProp.GetString() ?? ""
                            : "";

                        var parameters = new List<Formats.NDS.Scripts.ScriptParameterType>();
                        var parameterNames = new List<string>();

                        // Get parameter sizes (in bytes) from "parameters" field
                        if (cmdElement.TryGetProperty("parameters", out var paramSizesElement))
                        {
                            var parameterTypes = new List<string>();

                            // Get parameter types from "parameter_types" field
                            if (cmdElement.TryGetProperty("parameter_types", out var paramTypesElement))
                            {
                                foreach (var typeElement in paramTypesElement.EnumerateArray())
                                {
                                    parameterTypes.Add(typeElement.GetString() ?? "Integer");
                                }
                            }

                            // Get parameter value descriptions from "parameter_values" field
                            if (cmdElement.TryGetProperty("parameter_values", out var paramValuesElement))
                            {
                                foreach (var valueElement in paramValuesElement.EnumerateArray())
                                {
                                    parameterNames.Add(valueElement.GetString() ?? "");
                                }
                            }

                            int paramIndex = 0;
                            foreach (var sizeElement in paramSizesElement.EnumerateArray())
                            {
                                int size = sizeElement.GetInt32();
                                string typeStr = paramIndex < parameterTypes.Count
                                    ? parameterTypes[paramIndex]
                                    : "Integer";

                                // Get parameter name for heuristic detection
                                string paramName = paramIndex < parameterNames.Count
                                    ? parameterNames[paramIndex]
                                    : "";

                                // Map size + type + name to our ScriptParameterType
                                var paramType = MapParameterType(size, typeStr, paramName, name);
                                parameters.Add(paramType);
                                paramIndex++;
                            }
                        }

                        var info = new Formats.NDS.Scripts.ScriptCommandInfo
                        {
                            ID = id,
                            Name = name,
                            Parameters = parameters,
                            ParameterNames = parameterNames,
                            Description = description
                        };

                        commands[id] = info;
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Error($"Failed to parse scrcmd entry '{cmdProp.Name}': {ex.Message}");
                    }
                }

                AppLogger.Info($"Loaded {commands.Count} script commands from scrcmd format");
            }
            // Fallback to simple format for backwards compatibility
            else if (root.TryGetProperty("platinum", out var platinumElement) &&
                platinumElement.TryGetProperty("commands", out var commandsElement))
            {
                foreach (var cmdElement in commandsElement.EnumerateArray())
                {
                    try
                    {
                        var id = (ushort)cmdElement.GetProperty("id").GetInt32();
                        var name = cmdElement.GetProperty("name").GetString() ?? "Unknown";
                        var description = cmdElement.TryGetProperty("description", out var descProp)
                            ? descProp.GetString() ?? ""
                            : "";

                        var parameters = new List<Formats.NDS.Scripts.ScriptParameterType>();
                        if (cmdElement.TryGetProperty("parameters", out var paramsElement))
                        {
                            foreach (var paramElement in paramsElement.EnumerateArray())
                            {
                                var paramType = paramElement.GetString();
                                if (Enum.TryParse<Formats.NDS.Scripts.ScriptParameterType>(paramType, out var type))
                                {
                                    parameters.Add(type);
                                }
                                else
                                {
                                    AppLogger.Warn($"Unknown parameter type '{paramType}' for command {name} (ID: {id})");
                                }
                            }
                        }

                        var info = new Formats.NDS.Scripts.ScriptCommandInfo
                        {
                            ID = id,
                            Name = name,
                            Parameters = parameters,
                            Description = description
                        };

                        commands[id] = info;
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Error($"Failed to parse command in JSON: {ex.Message}");
                    }
                }

                AppLogger.Info($"Loaded {commands.Count} script commands from platinum format");
            }
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to parse ScriptCommands.json: {ex.Message}");
        }

        return commands;
    }

    /// <summary>
    /// Maps parameter size, type string, parameter name, and command name to ScriptParameterType
    /// Uses heuristics to detect offsets when not explicitly marked
    /// </summary>
    private Formats.NDS.Scripts.ScriptParameterType MapParameterType(int sizeInBytes, string typeStr, string paramName, string commandName)
    {
        // Handle Variable type - in LiTRE, "Variable" means a variable ID (like 0x800C)
        // which is a 2-byte word, NOT a variable-length sequence
        if (typeStr.Equals("Variable", StringComparison.OrdinalIgnoreCase))
        {
            return Formats.NDS.Scripts.ScriptParameterType.Word;
        }

        // Handle Offset type (usually 4 bytes)
        if (typeStr.Equals("Offset", StringComparison.OrdinalIgnoreCase))
        {
            return Formats.NDS.Scripts.ScriptParameterType.Offset;
        }

        // Heuristic: Detect offsets by parameter name or command name
        // 4-byte parameters that are likely offsets
        if (sizeInBytes == 4)
        {
            string paramLower = paramName.ToLower();
            string cmdLower = commandName.ToLower();

            // Common offset parameter names
            if (paramLower.Contains("offset") ||
                paramLower.Contains("jump") ||
                paramLower.Contains("call") ||
                paramLower.Contains("function") ||
                paramLower.Contains("script") ||
                paramLower.Contains("action") ||
                paramLower.Contains("pointer") ||
                paramLower.Contains("address"))
            {
                return Formats.NDS.Scripts.ScriptParameterType.Offset;
            }

            // Common jump/call commands
            if (cmdLower.StartsWith("jump") ||
                cmdLower.StartsWith("call") ||
                cmdLower.Contains("goto") ||
                cmdLower == "if" ||
                cmdLower == "while")
            {
                return Formats.NDS.Scripts.ScriptParameterType.Offset;
            }
        }

        // Map by size for Integer types
        return sizeInBytes switch
        {
            1 => Formats.NDS.Scripts.ScriptParameterType.Byte,
            2 => Formats.NDS.Scripts.ScriptParameterType.Word,
            4 => Formats.NDS.Scripts.ScriptParameterType.DWord,
            _ => Formats.NDS.Scripts.ScriptParameterType.Word // Default to Word
        };
    }

    /// <summary>
    /// Reloads commands from the configuration file
    /// </summary>
    public void ReloadCommands()
    {
        AppLogger.Info("Reloading script commands from config file...");
        Formats.NDS.Scripts.ScriptDatabase.ReloadFromConfig();
    }

    /// <summary>
    /// Gets the path to the configuration file
    /// </summary>
    public string GetConfigPath()
    {
        return CommandsFilePath;
    }

    /// <summary>
    /// Gets the directory containing configuration files
    /// </summary>
    public string GetConfigDirectory()
    {
        return AppDataFolder;
    }
}
