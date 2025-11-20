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
    /// </summary>
    public void LoadAvailableScripts()
    {
        AvailableScriptIds.Clear();

        if (_romService?.CurrentRom?.IsLoaded != true)
        {
            AppLogger.Warn("[LevelScriptService] Cannot load scripts - no ROM loaded");
            return;
        }

        string scriptsPath = Path.Combine(_romService.CurrentRom.RomPath, "unpacked", "levelScripts");

        if (!Directory.Exists(scriptsPath))
        {
            AppLogger.Warn($"[LevelScriptService] Level scripts directory not found: {scriptsPath}");
            return;
        }

        var scriptFiles = Directory.GetFiles(scriptsPath, "*.bin")
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => !string.IsNullOrEmpty(name) && int.TryParse(name, out _))
            .Select(name => int.Parse(name!))
            .OrderBy(id => id)
            .ToList();

        AvailableScriptIds = scriptFiles;
        AppLogger.Info($"[LevelScriptService] Loaded {AvailableScriptIds.Count} level scripts");
    }

    /// <summary>
    /// Load a level script by ID
    /// </summary>
    public LevelScriptFile? LoadScript(int scriptId)
    {
        if (_romService?.CurrentRom?.IsLoaded != true)
        {
            AppLogger.Warn("[LevelScriptService] Cannot load script - no ROM loaded");
            return null;
        }

        string scriptPath = Path.Combine(_romService.CurrentRom.RomPath, "unpacked", "levelScripts", $"{scriptId}.bin");

        if (!File.Exists(scriptPath))
        {
            AppLogger.Warn($"[LevelScriptService] Script file not found: {scriptPath}");
            return null;
        }

        try
        {
            byte[] data = File.ReadAllBytes(scriptPath);
            CurrentScript = LevelScriptFile.ReadFromBytes(data, scriptId);
            AppLogger.Info($"[LevelScriptService] Loaded level script {scriptId} with {CurrentScript.Triggers.Count} triggers");
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

        string scriptPath = Path.Combine(_romService.CurrentRom.RomPath, "unpacked", "levelScripts", $"{script.ScriptID}.bin");

        try
        {
            byte[] data = script.ToBytes();
            File.WriteAllBytes(scriptPath, data);
            AppLogger.Info($"[LevelScriptService] Saved level script {script.ScriptID}");
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
