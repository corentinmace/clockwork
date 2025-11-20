using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Clockwork.Core.Logging;
using Clockwork.Core.Models.LevelScript;

namespace Clockwork.Core.Services;

/// <summary>
/// Service for managing level script files
/// </summary>
public class LevelScriptService : IApplicationService
{
    private readonly ApplicationContext _appContext;
    private RomService? _romService;

    public LevelScriptFile? CurrentScript { get; private set; }
    public List<int> AvailableScriptIds { get; private set; } = new();

    public LevelScriptService(ApplicationContext appContext)
    {
        _appContext = appContext;
    }

    public void Initialize()
    {
        _romService = _appContext.GetService<RomService>();
        AppLogger.Info("[LevelScriptService] Initialized");
    }

    public void Update(double deltaTime)
    {
        // No per-frame logic needed
    }

    public void Dispose()
    {
        // No resources to dispose
    }

    /// <summary>
    /// Load available level script IDs from ROM
    /// Scripts are in unpacked/scripts/{id:D4}/ folders
    /// </summary>
    public void LoadAvailableScripts()
    {
        AvailableScriptIds.Clear();

        if (_romService?.CurrentRom?.IsLoaded != true)
        {
            AppLogger.Warn("[LevelScriptService] Cannot load scripts - no ROM loaded");
            return;
        }

        string scriptsPath = Path.Combine(_romService.CurrentRom.RomPath, "unpacked", "scripts");

        if (!Directory.Exists(scriptsPath))
        {
            AppLogger.Warn($"[LevelScriptService] Scripts directory not found: {scriptsPath}");
            return;
        }

        // Scripts are in folders named with 4-digit IDs (e.g., 0001, 0042)
        var scriptDirs = Directory.GetDirectories(scriptsPath)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrEmpty(name) && name.Length == 4 && int.TryParse(name, out _))
            .Select(name => int.Parse(name!))
            .OrderBy(id => id)
            .ToList();

        AvailableScriptIds = scriptDirs;
        AppLogger.Info($"[LevelScriptService] Loaded {AvailableScriptIds.Count} level scripts from {scriptsPath}");
    }

    /// <summary>
    /// Load a level script by ID
    /// Scripts are in unpacked/scripts/{id:D4}/ folders
    /// </summary>
    public LevelScriptFile? LoadScript(int scriptId)
    {
        if (_romService?.CurrentRom?.IsLoaded != true)
        {
            AppLogger.Warn("[LevelScriptService] Cannot load script - no ROM loaded");
            return null;
        }

        // Format ID as 4-digit string (e.g., "0001", "0042")
        string scriptFolder = scriptId.ToString("D4");
        string scriptDir = Path.Combine(_romService.CurrentRom.RomPath, "unpacked", "scripts", scriptFolder);

        if (!Directory.Exists(scriptDir))
        {
            AppLogger.Warn($"[LevelScriptService] Script directory not found: {scriptDir}");
            return null;
        }

        // The level script file is typically the first/main file in the directory
        // We look for any file in the directory (they're usually numbered sequentially)
        var files = Directory.GetFiles(scriptDir);
        if (files.Length == 0)
        {
            AppLogger.Warn($"[LevelScriptService] No files found in script directory: {scriptDir}");
            return null;
        }

        // Use the first file (typically the level script)
        string scriptPath = files[0];

        try
        {
            byte[] data = File.ReadAllBytes(scriptPath);
            CurrentScript = LevelScriptFile.ReadFromBytes(data, scriptId);

            int totalTriggers = CurrentScript.MapLoadTriggers.Count + CurrentScript.VariableValueTriggers.Count;
            AppLogger.Info($"[LevelScriptService] Loaded level script {scriptId} with {totalTriggers} triggers " +
                          $"({CurrentScript.MapLoadTriggers.Count} map, {CurrentScript.VariableValueTriggers.Count} var)");
            return CurrentScript;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"[LevelScriptService] Failed to load script {scriptId}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Save the current level script
    /// </summary>
    public bool SaveCurrentScript()
    {
        if (CurrentScript == null)
        {
            AppLogger.Warn("[LevelScriptService] No script loaded to save");
            return false;
        }

        return SaveScript(CurrentScript);
    }

    /// <summary>
    /// Save a level script
    /// </summary>
    public bool SaveScript(LevelScriptFile script)
    {
        if (_romService?.CurrentRom?.IsLoaded != true)
        {
            AppLogger.Warn("[LevelScriptService] Cannot save script - no ROM loaded");
            return false;
        }

        // Format ID as 4-digit string (e.g., "0001", "0042")
        string scriptFolder = script.ScriptID.ToString("D4");
        string scriptDir = Path.Combine(_romService.CurrentRom.RomPath, "unpacked", "scripts", scriptFolder);

        if (!Directory.Exists(scriptDir))
        {
            AppLogger.Warn($"[LevelScriptService] Script directory not found: {scriptDir}");
            return false;
        }

        // Get the first file in the directory (the level script file)
        var files = Directory.GetFiles(scriptDir);
        if (files.Length == 0)
        {
            AppLogger.Warn($"[LevelScriptService] No files found in script directory: {scriptDir}");
            return false;
        }

        string scriptPath = files[0];

        try
        {
            byte[] data = script.ToBytes();
            File.WriteAllBytes(scriptPath, data);
            int totalTriggers = script.MapLoadTriggers.Count + script.VariableValueTriggers.Count;
            AppLogger.Info($"[LevelScriptService] Saved level script {script.ScriptID} with {totalTriggers} triggers");
            return true;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"[LevelScriptService] Failed to save script {script.ScriptID}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Set the current script
    /// </summary>
    public void SetCurrentScript(LevelScriptFile? script)
    {
        CurrentScript = script;
    }

    /// <summary>
    /// Import a level script from external file
    /// </summary>
    public LevelScriptFile? ImportScript(string filePath, int scriptId)
    {
        try
        {
            byte[] data = File.ReadAllBytes(filePath);
            var script = LevelScriptFile.ReadFromBytes(data, scriptId);
            AppLogger.Info($"[LevelScriptService] Imported script from {filePath}");
            return script;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"[LevelScriptService] Failed to import script: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Export current script to external file
    /// </summary>
    public bool ExportScript(LevelScriptFile script, string filePath)
    {
        try
        {
            byte[] data = script.ToBytes();
            File.WriteAllBytes(filePath, data);
            AppLogger.Info($"[LevelScriptService] Exported script to {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"[LevelScriptService] Failed to export script: {ex.Message}");
            return false;
        }
    }
}
