using System.Text.Json;
using System.Reflection;
using Clockwork.Core.Services;

namespace Clockwork.Core.Formats.NDS.Scripts;

/// <summary>
/// Database of script commands for different Pokémon NDS games
/// Loads from %appdata%/Clockwork/Scrcmd/ScriptCommands.json
/// </summary>
public static class ScriptDatabase
{
    private static Dictionary<ushort, ScriptCommandInfo>? _platinumCommands;
    private static Dictionary<string, uint>? _movements;
    private static Dictionary<string, uint>? _comparisonOperators;
    private static Dictionary<string, uint>? _specialOverworlds;
    private static Dictionary<string, uint>? _overworldDirections;
    private static bool _isInitialized = false;
    private static ScriptCommandConfigService? _configService;

    /// <summary>
    /// Sets the configuration service used to load commands
    /// </summary>
    public static void SetConfigService(ScriptCommandConfigService configService)
    {
        _configService = configService;
        _isInitialized = false; // Force reload on next access
    }

    /// <summary>
    /// Gets script command database for Platinum (default)
    /// </summary>
    public static Dictionary<ushort, ScriptCommandInfo> PlatinumCommands
    {
        get
        {
            if (!_isInitialized)
            {
                LoadCommands();
            }
            return _platinumCommands!;
        }
    }

    /// <summary>
    /// Gets movement constants (e.g., "WalkUp" -> 0)
    /// </summary>
    public static Dictionary<string, uint> Movements
    {
        get
        {
            if (!_isInitialized)
            {
                LoadCommands();
            }
            return _movements ?? new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Gets comparison operator constants (e.g., "EQUAL" -> 0)
    /// </summary>
    public static Dictionary<string, uint> ComparisonOperators
    {
        get
        {
            if (!_isInitialized)
            {
                LoadCommands();
            }
            return _comparisonOperators ?? new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Gets special overworld constants
    /// </summary>
    public static Dictionary<string, uint> SpecialOverworlds
    {
        get
        {
            if (!_isInitialized)
            {
                LoadCommands();
            }
            return _specialOverworlds ?? new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Gets overworld direction constants
    /// </summary>
    public static Dictionary<string, uint> OverworldDirections
    {
        get
        {
            if (!_isInitialized)
            {
                LoadCommands();
            }
            return _overworldDirections ?? new Dictionary<string, uint>(StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>
    /// Gets command info by ID, returns null if not found
    /// </summary>
    public static ScriptCommandInfo? GetCommandInfo(ushort commandID)
    {
        if (PlatinumCommands.TryGetValue(commandID, out var info))
        {
            return info;
        }
        return null;
    }

    /// <summary>
    /// Gets command name by ID, returns hex ID if not found
    /// </summary>
    public static string GetCommandName(ushort commandID)
    {
        var info = GetCommandInfo(commandID);
        return info?.Name ?? $"CMD_0x{commandID:X4}";
    }

    /// <summary>
    /// Gets command info by name, returns null if not found
    /// </summary>
    public static ScriptCommandInfo? GetCommandInfo(string commandName)
    {
        var commands = PlatinumCommands.Values;
        return commands.FirstOrDefault(c => c.Name.Equals(commandName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Reloads commands from configuration file
    /// </summary>
    public static void ReloadFromConfig()
    {
        _isInitialized = false;
        LoadCommands();
    }

    private static void LoadCommands()
    {
        // Try loading from config service first
        if (_configService != null)
        {
            var commands = _configService.LoadCommands();
            if (commands != null && commands.Count > 0)
            {
                _platinumCommands = commands;

                // Also load constants
                var (movements, compOps, specialOw, owDirs) = _configService.LoadConstants();
                _movements = movements;
                _comparisonOperators = compOps;
                _specialOverworlds = specialOw;
                _overworldDirections = owDirs;

                _isInitialized = true;
                Console.WriteLine($"Loaded {_platinumCommands.Count} script commands from config service");
                return;
            }
        }

        // Fallback to embedded resources if config service fails
        LoadFromEmbeddedResources();
    }

    private static void LoadFromEmbeddedResources()
    {
        try
        {
            // Load JSON from embedded resource
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Clockwork.Core.Resources.ScriptCommands.json";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                Console.WriteLine($"Failed to load script commands JSON: resource not found");
                _platinumCommands = new Dictionary<ushort, ScriptCommandInfo>();
                _isInitialized = true;
                return;
            }

            using var reader = new StreamReader(stream);
            var jsonContent = reader.ReadToEnd();

            var jsonDoc = JsonDocument.Parse(jsonContent);
            var root = jsonDoc.RootElement;

            _platinumCommands = new Dictionary<ushort, ScriptCommandInfo>();

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

                        var parameters = new List<ScriptParameterType>();
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

                                // Map size + type to our ScriptParameterType
                                var paramType = MapParameterTypeFromEmbedded(size, typeStr);
                                parameters.Add(paramType);
                                paramIndex++;
                            }
                        }

                        var info = new ScriptCommandInfo
                        {
                            ID = id,
                            Name = name,
                            Parameters = parameters,
                            ParameterNames = parameterNames,
                            Description = description
                        };

                        _platinumCommands[id] = info;
                    }
                    catch
                    {
                        // Skip invalid entries
                    }
                }
            }
            // Fallback to simple format for backwards compatibility
            else if (root.TryGetProperty("platinum", out var platinumElement) &&
                platinumElement.TryGetProperty("commands", out var commandsElement))
            {
                foreach (var cmdElement in commandsElement.EnumerateArray())
                {
                    var id = (ushort)cmdElement.GetProperty("id").GetInt32();
                    var name = cmdElement.GetProperty("name").GetString() ?? "Unknown";
                    var description = cmdElement.TryGetProperty("description", out var descProp)
                        ? descProp.GetString() ?? ""
                        : "";

                    var parameters = new List<ScriptParameterType>();
                    if (cmdElement.TryGetProperty("parameters", out var paramsElement))
                    {
                        foreach (var paramElement in paramsElement.EnumerateArray())
                        {
                            var paramType = paramElement.GetString();
                            if (Enum.TryParse<ScriptParameterType>(paramType, out var type))
                            {
                                parameters.Add(type);
                            }
                        }
                    }

                    var info = new ScriptCommandInfo
                    {
                        ID = id,
                        Name = name,
                        Parameters = parameters,
                        Description = description
                    };

                    _platinumCommands[id] = info;
                }
            }

            _isInitialized = true;
            Console.WriteLine($"Loaded {_platinumCommands.Count} script commands from embedded resources (fallback)");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading script commands from embedded resources: {ex.Message}");
            _platinumCommands = new Dictionary<ushort, ScriptCommandInfo>();
            _isInitialized = true;
        }
    }

    /// <summary>
    /// Maps parameter size and type string to ScriptParameterType (for embedded resources)
    /// </summary>
    private static ScriptParameterType MapParameterTypeFromEmbedded(int sizeInBytes, string typeStr)
    {
        // Handle Variable type
        if (typeStr.Equals("Variable", StringComparison.OrdinalIgnoreCase))
        {
            return ScriptParameterType.Variable;
        }

        // Handle Offset type (usually 4 bytes)
        if (typeStr.Equals("Offset", StringComparison.OrdinalIgnoreCase))
        {
            return ScriptParameterType.Offset;
        }

        // Map by size for Integer types
        return sizeInBytes switch
        {
            1 => ScriptParameterType.Byte,
            2 => ScriptParameterType.Word,
            4 => ScriptParameterType.DWord,
            _ => ScriptParameterType.Word // Default to Word
        };
    }
}
