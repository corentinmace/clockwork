using Clockwork.Core.Formats.NDS.Scripts;
using Clockwork.Core.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Clockwork.Core.Services;

/// <summary>
/// Service for managing NDS script files with auto-extraction and compilation.
/// Handles bidirectional sync between binary (unpacked/scripts) and text (script_export) formats.
/// </summary>
public class ScriptService : IApplicationService
{
    private readonly ApplicationContext _appContext;
    private RomService? _romService;

    // Cached script files
    private Dictionary<int, ScriptFile> _loadedScripts = new();

    public ScriptService(ApplicationContext appContext)
    {
        _appContext = appContext;
    }

    public void Initialize()
    {
        _romService = _appContext.GetService<RomService>();
        AppLogger.Debug("ScriptService initialized");
    }

    public void Update(double deltaTime)
    {
        // No per-frame updates needed
    }

    public void Dispose()
    {
        _loadedScripts.Clear();
        AppLogger.Debug("ScriptService disposed");
    }

    /// <summary>
    /// Load a script file by ID, with auto-extraction to script_export if needed
    /// </summary>
    public ScriptFile? LoadScript(int scriptID)
    {
        // Check cache first
        if (_loadedScripts.TryGetValue(scriptID, out var cached))
        {
            return cached;
        }

        if (_romService?.CurrentRom?.IsLoaded != true)
        {
            AppLogger.Warn("Cannot load script: ROM not loaded");
            return null;
        }

        try
        {
            string binPath = GetScriptBinaryPath(scriptID);
            string[] textPaths = GetScriptExportPaths(scriptID);

            if (!File.Exists(binPath))
            {
                AppLogger.Warn($"Script {scriptID} not found at {binPath}");
                return null;
            }

            // Ensure script_export directory exists
            string exportDir = Path.GetDirectoryName(textPaths[0])!;
            if (!Directory.Exists(exportDir))
            {
                Directory.CreateDirectory(exportDir);
            }

            ScriptFile? script = null;
            bool shouldExtract = false;

            // Check if any text file is missing or binary is newer
            foreach (var txtPath in textPaths)
            {
                if (!File.Exists(txtPath))
                {
                    shouldExtract = true;
                    AppLogger.Debug($"Script {scriptID}: {Path.GetFileName(txtPath)} not found, extracting from .bin");
                    break;
                }
                else if (File.GetLastWriteTimeUtc(binPath) > File.GetLastWriteTimeUtc(txtPath))
                {
                    shouldExtract = true;
                    AppLogger.Debug($"Script {scriptID}: .bin is newer, re-extracting");
                    break;
                }
            }

            if (shouldExtract)
            {
                // Load from binary and export to text files
                script = ScriptFile.FromFile(binPath, scriptID);
                ExportScriptToTextFiles(script, textPaths);
                AppLogger.Info($"Extracted script {scriptID} to script_export/");
            }
            else
            {
                // Load from text files
                script = ImportScriptFromTextFiles(scriptID, textPaths);
                AppLogger.Debug($"Loaded script {scriptID} from script_export/");
            }

            // Cache it
            if (script != null)
            {
                _loadedScripts[scriptID] = script;
            }

            return script;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to load script {scriptID}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Save a script file, updating both text and binary formats
    /// </summary>
    public bool SaveScript(int scriptID, ScriptFile script)
    {
        if (_romService?.CurrentRom?.IsLoaded != true)
        {
            AppLogger.Warn("Cannot save script: ROM not loaded");
            return false;
        }

        try
        {
            string binPath = GetScriptBinaryPath(scriptID);
            string[] textPaths = GetScriptExportPaths(scriptID);

            // Ensure directories exist
            string binDir = Path.GetDirectoryName(binPath)!;
            if (!Directory.Exists(binDir))
            {
                Directory.CreateDirectory(binDir);
            }

            string exportDir = Path.GetDirectoryName(textPaths[0])!;
            if (!Directory.Exists(exportDir))
            {
                Directory.CreateDirectory(exportDir);
            }

            // Save to text files (human-readable)
            ExportScriptToTextFiles(script, textPaths);

            // Save to binary (for ROM packing)
            script.SaveToFile(binPath);

            // Update cache
            _loadedScripts[scriptID] = script;

            AppLogger.Info($"Saved script {scriptID} to both script_export/ and unpacked/");
            return true;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to save script {scriptID}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Build required binary files from script_export folder.
    /// Compiles .script and .action text files back to .bin format.
    /// Called before ROM repacking to ensure all script modifications are included.
    /// </summary>
    public bool BuildRequiredBins()
    {
        if (_romService?.CurrentRom?.IsLoaded != true)
        {
            AppLogger.Warn("Cannot build script bins: ROM not loaded");
            return true; // Not an error if no ROM is loaded
        }

        string romPath = _romService.CurrentRom.RomPath;
        string exportDir = Path.Combine(Path.GetDirectoryName(romPath)!, "script_export");
        string unpackedDir = Path.Combine(romPath, "unpacked", "scripts");

        // If script_export directory doesn't exist, nothing to do
        if (!Directory.Exists(exportDir))
        {
            AppLogger.Info("Script: No script_export directory found, skipping .bin rebuild.");
            return true;
        }

        // Ensure unpacked directory exists
        if (!Directory.Exists(unpackedDir))
        {
            Directory.CreateDirectory(unpackedDir);
        }

        try
        {
            // Get all *_script.script files
            var scriptFiles = Directory.GetFiles(exportDir, "*_script.script", SearchOption.TopDirectoryOnly);
            int rebuiltCount = 0;
            int skippedCount = 0;

            foreach (string scriptFile in scriptFiles)
            {
                // Extract script ID from filename (e.g., "0123_script.script" -> 123)
                string fileName = Path.GetFileNameWithoutExtension(scriptFile);
                string idStr = fileName.Replace("_script", "");

                if (!int.TryParse(idStr, out int scriptID))
                {
                    AppLogger.Error($"Skipping invalid script file name: {fileName}");
                    continue;
                }

                string[] textPaths = GetScriptExportPaths(scriptID);
                string binPath = Path.Combine(unpackedDir, scriptID.ToString("D4"));

                // Check if any text file is newer than binary
                bool shouldRebuild = !File.Exists(binPath);
                foreach (var txtPath in textPaths)
                {
                    if (File.Exists(txtPath) &&
                        File.GetLastWriteTimeUtc(txtPath) > File.GetLastWriteTimeUtc(binPath))
                    {
                        shouldRebuild = true;
                        break;
                    }
                }

                if (!shouldRebuild)
                {
                    skippedCount++;
                    continue;
                }

                // Rebuild the .bin file from text files
                try
                {
                    var script = ImportScriptFromTextFiles(scriptID, textPaths);
                    if (script != null)
                    {
                        script.SaveToFile(binPath);

                        // Update text file timestamps to prevent overwriting
                        foreach (var txtPath in textPaths)
                        {
                            if (File.Exists(txtPath))
                            {
                                File.SetLastWriteTimeUtc(txtPath, DateTime.UtcNow);
                            }
                        }

                        rebuiltCount++;
                        AppLogger.Debug($"Rebuilt script {scriptID} from text files");
                    }
                }
                catch (Exception ex)
                {
                    AppLogger.Error($"Failed to rebuild script {scriptID}: {ex.Message}");
                    return false;
                }
            }

            AppLogger.Info($"Script: {rebuiltCount} .bin files built from text, {skippedCount} .bin files skipped because they were newer");
            return true;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Error during script .bin rebuild: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Get the binary file path for a script ID (unpacked/scripts)
    /// </summary>
    private string GetScriptBinaryPath(int scriptID)
    {
        if (_romService?.CurrentRom?.IsLoaded != true)
        {
            throw new InvalidOperationException("ROM not loaded");
        }

        string romPath = _romService.CurrentRom.RomPath;
        string fileName = scriptID.ToString("D4");
        return Path.Combine(romPath, "unpacked", "scripts", fileName);
    }

    /// <summary>
    /// Get the export text file paths for a script ID (script_export/)
    /// Returns [script, func, action] paths
    /// </summary>
    private string[] GetScriptExportPaths(int scriptID)
    {
        if (_romService?.CurrentRom?.IsLoaded != true)
        {
            throw new InvalidOperationException("ROM not loaded");
        }

        string romPath = _romService.CurrentRom.RomPath;
        string exportDir = Path.Combine(Path.GetDirectoryName(romPath)!, "script_export");

        string baseName = scriptID.ToString("D4");
        return new[]
        {
            Path.Combine(exportDir, $"{baseName}_script.script"),
            Path.Combine(exportDir, $"{baseName}_func.script"),
            Path.Combine(exportDir, $"{baseName}_action.action")
        };
    }

    /// <summary>
    /// Export a script file to text files
    /// </summary>
    private void ExportScriptToTextFiles(ScriptFile script, string[] textPaths)
    {
        // Export scripts
        var scriptText = string.Join("\n\n", script.Scripts.Select(s => ScriptDecompiler.DecompileContainer(s)));
        File.WriteAllText(textPaths[0], scriptText);

        // Export functions
        var functionText = string.Join("\n\n", script.Functions.Select(f => ScriptDecompiler.DecompileContainer(f)));
        File.WriteAllText(textPaths[1], functionText);

        // Export actions
        var actionText = string.Join("\n\n", script.Actions.Select(a => ScriptDecompiler.DecompileContainer(a)));
        File.WriteAllText(textPaths[2], actionText);
    }

    /// <summary>
    /// Import a script file from text files
    /// </summary>
    private ScriptFile? ImportScriptFromTextFiles(int scriptID, string[] textPaths)
    {
        var script = new ScriptFile(scriptID);

        // Import scripts
        if (File.Exists(textPaths[0]))
        {
            var scriptText = File.ReadAllText(textPaths[0]);
            script.Scripts = ParseContainersFromText(scriptText, ContainerType.Script);
        }

        // Import functions
        if (File.Exists(textPaths[1]))
        {
            var functionText = File.ReadAllText(textPaths[1]);
            script.Functions = ParseContainersFromText(functionText, ContainerType.Function);
        }

        // Import actions
        if (File.Exists(textPaths[2]))
        {
            var actionText = File.ReadAllText(textPaths[2]);
            script.Actions = ParseContainersFromText(actionText, ContainerType.Action);
        }

        return script;
    }

    /// <summary>
    /// Parse containers from text (separated by blank lines)
    /// </summary>
    private List<ScriptContainer> ParseContainersFromText(string text, ContainerType type)
    {
        var containers = new List<ScriptContainer>();
        var lines = text.Split('\n');

        var currentText = new List<string>();
        uint currentID = 0;

        foreach (var line in lines)
        {
            // Check for container header comment (// Script 0, // Function 1, etc.)
            if (line.TrimStart().StartsWith($"// {type} "))
            {
                // Save previous container if any
                if (currentText.Count > 0)
                {
                    var containerText = string.Join("\n", currentText);
                    try
                    {
                        var container = ScriptCompiler.CompileContainer(containerText, type, currentID);
                        containers.Add(container);
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Error($"Failed to compile {type} {currentID}: {ex.Message}");
                    }
                    currentText.Clear();
                }

                // Extract ID from header
                var parts = line.Split(' ');
                if (parts.Length >= 3 && uint.TryParse(parts[2], out uint id))
                {
                    currentID = id;
                }
            }
            else
            {
                currentText.Add(line);
            }
        }

        // Save last container
        if (currentText.Count > 0)
        {
            var containerText = string.Join("\n", currentText);
            try
            {
                var container = ScriptCompiler.CompileContainer(containerText, type, currentID);
                containers.Add(container);
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to compile {type} {currentID}: {ex.Message}");
            }
        }

        return containers;
    }

    /// <summary>
    /// Get list of all script IDs in the ROM
    /// </summary>
    public List<int> GetAvailableScriptIDs()
    {
        if (_romService?.CurrentRom?.IsLoaded != true)
        {
            return new List<int>();
        }

        try
        {
            string scriptsPath = Path.Combine(_romService.CurrentRom.RomPath, "unpacked", "scripts");
            if (!Directory.Exists(scriptsPath))
            {
                return new List<int>();
            }

            var files = Directory.GetFiles(scriptsPath)
                .Select(Path.GetFileName)
                .Where(name => name != null && int.TryParse(name, out _))
                .Select(name => int.Parse(name!))
                .OrderBy(id => id)
                .ToList();

            return files;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to get available script IDs: {ex.Message}");
            return new List<int>();
        }
    }

    /// <summary>
    /// Clear all cached data (call when ROM changes)
    /// </summary>
    public void ClearCache()
    {
        _loadedScripts.Clear();
        AppLogger.Debug("ScriptService cache cleared");
    }
}
