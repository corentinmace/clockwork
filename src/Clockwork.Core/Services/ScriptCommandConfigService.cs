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
    /// Parses JSON content into command dictionary
    /// </summary>
    private Dictionary<ushort, Formats.NDS.Scripts.ScriptCommandInfo> ParseCommandsJson(string jsonContent)
    {
        var commands = new Dictionary<ushort, Formats.NDS.Scripts.ScriptCommandInfo>();

        try
        {
            var jsonDoc = JsonDocument.Parse(jsonContent);
            var root = jsonDoc.RootElement;

            if (root.TryGetProperty("platinum", out var platinumElement) &&
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
            }

            AppLogger.Info($"Loaded {commands.Count} script commands from config file");
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to parse ScriptCommands.json: {ex.Message}");
        }

        return commands;
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
