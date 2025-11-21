using Clockwork.Core.Logging;
using Clockwork.Core.Models;
using System;
using System.IO;

namespace Clockwork.Core.Services;

/// <summary>
/// Service for loading and saving wild Pokemon encounter files.
/// </summary>
public class WildEncounterService : IApplicationService
{
    private readonly ApplicationContext _appContext;
    private RomService? _romService;

    public EncounterFile? CurrentEncounter { get; private set; }
    public int CurrentEncounterID { get; private set; } = -1;
    public bool IsLoaded => CurrentEncounter != null;

    public WildEncounterService(ApplicationContext appContext)
    {
        _appContext = appContext;
    }

    public void Initialize()
    {
        _romService = _appContext.GetService<RomService>();
        AppLogger.Debug("WildEncounterService initialized");
    }

    public void Update(double deltaTime)
    {
        // No per-frame updates needed
    }

    public void Dispose()
    {
        CurrentEncounter = null;
        AppLogger.Debug("WildEncounterService disposed");
    }

    /// <summary>
    /// Load encounter file by ID.
    /// </summary>
    public EncounterFile? LoadEncounter(int encounterID)
    {
        if (_romService?.CurrentRom?.IsLoaded != true)
        {
            AppLogger.Warn("Cannot load encounter: ROM not loaded");
            return null;
        }

        try
        {
            string encounterPath = GetEncounterPath(encounterID);

            if (!File.Exists(encounterPath))
            {
                AppLogger.Warn($"Encounter file not found: {encounterPath}");
                return null;
            }

            byte[] data = File.ReadAllBytes(encounterPath);
            CurrentEncounter = EncounterFile.ReadFromBytes(data);
            CurrentEncounterID = encounterID;

            AppLogger.Info($"Loaded encounter file {encounterID} from {encounterPath}");
            return CurrentEncounter;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to load encounter {encounterID}: {ex.Message}");
            CurrentEncounter = null;
            CurrentEncounterID = -1;
            return null;
        }
    }

    /// <summary>
    /// Save current encounter file.
    /// </summary>
    public bool SaveCurrentEncounter()
    {
        if (!IsLoaded || CurrentEncounterID < 0)
        {
            AppLogger.Warn("Cannot save: No encounter loaded");
            return false;
        }

        return SaveEncounter(CurrentEncounterID, CurrentEncounter!);
    }

    /// <summary>
    /// Save encounter file by ID.
    /// </summary>
    public bool SaveEncounter(int encounterID, EncounterFile encounter)
    {
        if (_romService?.CurrentRom?.IsLoaded != true)
        {
            AppLogger.Warn("Cannot save encounter: ROM not loaded");
            return false;
        }

        try
        {
            string encounterPath = GetEncounterPath(encounterID);
            string? directory = Path.GetDirectoryName(encounterPath);

            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
                AppLogger.Debug($"Created directory: {directory}");
            }

            byte[] data = encounter.ToBytes();
            File.WriteAllBytes(encounterPath, data);

            AppLogger.Info($"Saved encounter file {encounterID} to {encounterPath}");
            return true;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to save encounter {encounterID}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Get the file path for an encounter ID.
    /// </summary>
    public string GetEncounterPath(int encounterID)
    {
        if (_romService?.CurrentRom?.IsLoaded != true)
        {
            throw new InvalidOperationException("ROM not loaded");
        }

        string romPath = _romService.CurrentRom.RomPath;

        // Encounter files are in: unpacked/dynamicHeaders/encounters/[ID].bin
        string encounterFileName = encounterID.ToString("D4");
        return Path.Combine(romPath, "unpacked", "encounters", encounterFileName);
    }

    /// <summary>
    /// Check if an encounter file exists.
    /// </summary>
    public bool EncounterExists(int encounterID)
    {
        if (_romService?.CurrentRom?.IsLoaded != true)
        {
            return false;
        }

        try
        {
            string path = GetEncounterPath(encounterID);
            return File.Exists(path);
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Get the number of encounter files available.
    /// </summary>
    public int GetEncounterCount()
    {
        if (_romService?.CurrentRom?.IsLoaded != true)
        {
            return 0;
        }

        try
        {
            string romPath = _romService.CurrentRom.RomPath;
            string encountersDir = Path.Combine(romPath, "unpacked", "dynamicHeaders", "encounters");

            if (!Directory.Exists(encountersDir))
            {
                return 0;
            }

            string[] files = Directory.GetFiles(encountersDir);
            return files.Length;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to get encounter count: {ex.Message}");
            return 0;
        }
    }

    /// <summary>
    /// Create a new empty encounter file.
    /// </summary>
    public EncounterFile CreateEmptyEncounter()
    {
        var encounter = new EncounterFile();

        // Set default values (all zeros is valid)
        AppLogger.Debug("Created new empty encounter file");

        return encounter;
    }
}
