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

            if (root.TryGetProperty("platinum", out var platinumElement) &&
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
}
