using Clockwork.Core.Logging;
using Clockwork.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Clockwork.Core.Services;

/// <summary>
/// Service for loading and managing text archives (Pokemon names, location names, etc.).
/// Uses the new TextConverter system from LiTRE.
/// </summary>
public class TextArchiveService : IApplicationService
{
    private readonly ApplicationContext _appContext;
    private RomService? _romService;
    private HeaderService? _headerService;

    // Cached text archives
    private TextArchive? _pokemonNames;
    private TextArchive? _locationNames;
    private Dictionary<int, TextArchive> _loadedArchives = new();
    private Dictionary<int, List<string>>? _encounterFileToLocations;

    // Text archive IDs for Platinum (DPPt use different IDs)
    private const int PLATINUM_POKEMON_NAMES_ID = 412;
    private const int PLATINUM_LOCATION_NAMES_ID = 433;

    public TextArchiveService(ApplicationContext appContext)
    {
        _appContext = appContext;
    }

    public void Initialize()
    {
        _romService = _appContext.GetService<RomService>();
        _headerService = _appContext.GetService<HeaderService>();
        AppLogger.Debug("TextArchiveService initialized");
    }

    public void Update(double deltaTime)
    {
        // No per-frame updates needed
    }

    public void Dispose()
    {
        _pokemonNames = null;
        _locationNames = null;
        _loadedArchives.Clear();
        _encounterFileToLocations = null;
        AppLogger.Debug("TextArchiveService disposed");
    }

    /// <summary>
    /// Load a text archive by ID using the new TextConverter system
    /// </summary>
    public TextArchive? LoadTextArchive(int archiveID)
    {
        // Check cache first
        if (_loadedArchives.TryGetValue(archiveID, out var cached))
        {
            return cached;
        }

        if (_romService?.CurrentRom?.IsLoaded != true)
        {
            AppLogger.Warn("Cannot load text archive: ROM not loaded");
            return null;
        }

        try
        {
            string archivePath = GetTextArchivePath(archiveID);

            if (!File.Exists(archivePath))
            {
                AppLogger.Warn($"Text archive {archiveID} not found at {archivePath}");
                return null;
            }

            // Use the new TextArchive.ReadFromFile method
            var archive = TextArchive.ReadFromFile(archivePath, archiveID);

            // Cache it
            _loadedArchives[archiveID] = archive;

            AppLogger.Debug($"Loaded text archive {archiveID} with {archive.MessageCount} messages (key: 0x{archive.Key:X4})");
            return archive;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to load text archive {archiveID}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Save a text archive by ID
    /// </summary>
    public bool SaveTextArchive(int archiveID, TextArchive archive)
    {
        if (_romService?.CurrentRom?.IsLoaded != true)
        {
            AppLogger.Warn("Cannot save text archive: ROM not loaded");
            return false;
        }

        try
        {
            string archivePath = GetTextArchivePath(archiveID);
            archive.SaveToFile(archivePath);

            // Update cache
            _loadedArchives[archiveID] = archive;

            AppLogger.Info($"Saved text archive {archiveID}");
            return true;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to save text archive {archiveID}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Export a text archive to plain text format
    /// </summary>
    public bool ExportTextArchive(int archiveID, string outputPath)
    {
        var archive = LoadTextArchive(archiveID);
        if (archive == null)
            return false;

        try
        {
            archive.ExportToTextFile(outputPath);
            return true;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to export text archive {archiveID}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Import a text archive from plain text format
    /// </summary>
    public bool ImportTextArchive(int archiveID, string inputPath)
    {
        try
        {
            var archive = TextArchive.ImportFromTextFile(inputPath, archiveID);
            return SaveTextArchive(archiveID, archive);
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to import text archive {archiveID}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Get Pokemon names archive (cached)
    /// </summary>
    public TextArchive? GetPokemonNamesArchive()
    {
        if (_pokemonNames != null)
            return _pokemonNames;

        _pokemonNames = LoadTextArchive(PLATINUM_POKEMON_NAMES_ID);
        if (_pokemonNames != null)
        {
            AppLogger.Info($"Loaded {_pokemonNames.MessageCount} Pokemon names");
        }
        return _pokemonNames;
    }

    /// <summary>
    /// Get location names archive (cached)
    /// </summary>
    public TextArchive? GetLocationNamesArchive()
    {
        if (_locationNames != null)
            return _locationNames;

        _locationNames = LoadTextArchive(PLATINUM_LOCATION_NAMES_ID);
        if (_locationNames != null)
        {
            AppLogger.Info($"Loaded {_locationNames.MessageCount} location names");
        }
        return _locationNames;
    }

    /// <summary>
    /// Get Pokemon names as string array
    /// </summary>
    public string[] GetPokemonNames()
    {
        var archive = GetPokemonNamesArchive();
        return archive?.Messages.ToArray() ?? Array.Empty<string>();
    }

    /// <summary>
    /// Get location names as string array
    /// </summary>
    public string[] GetLocationNames()
    {
        var archive = GetLocationNamesArchive();
        return archive?.Messages.ToArray() ?? Array.Empty<string>();
    }

    /// <summary>
    /// Get Pokemon name by ID
    /// </summary>
    public string GetPokemonName(uint pokemonID)
    {
        var names = GetPokemonNames();
        if (pokemonID < names.Length)
            return names[pokemonID];

        return $"Pokemon #{pokemonID}";
    }

    /// <summary>
    /// Get location name by ID
    /// </summary>
    public string GetLocationName(int locationID)
    {
        var names = GetLocationNames();
        if (locationID >= 0 && locationID < names.Length)
            return names[locationID];

        return $"Location #{locationID}";
    }

    /// <summary>
    /// Build mapping of encounter file IDs to location names
    /// </summary>
    public Dictionary<int, List<string>> GetEncounterFileToLocationMapping()
    {
        if (_encounterFileToLocations != null)
            return _encounterFileToLocations;

        _encounterFileToLocations = new Dictionary<int, List<string>>();

        if (_headerService == null || !_headerService.IsLoaded)
        {
            AppLogger.Warn("Cannot build encounter-location mapping: Headers not loaded");
            return _encounterFileToLocations;
        }

        var locationNames = GetLocationNames();

        // Iterate through all headers and map encounter IDs to locations
        foreach (var header in _headerService.Headers)
        {
            int encounterID = header.WildPokemon;

            if (encounterID == MapHeader.NO_WILD_ENCOUNTERS)
                continue;

            if (!_encounterFileToLocations.ContainsKey(encounterID))
            {
                _encounterFileToLocations[encounterID] = new List<string>();
            }

            // Get location name
            string locationName = GetLocationName(header.LocationName);
            if (!_encounterFileToLocations[encounterID].Contains(locationName))
            {
                _encounterFileToLocations[encounterID].Add(locationName);
            }
        }

        AppLogger.Info($"Built encounter-location mapping for {_encounterFileToLocations.Count} encounter files");
        return _encounterFileToLocations;
    }

    /// <summary>
    /// Get location names for an encounter file ID
    /// </summary>
    public string GetEncounterLocationNames(int encounterID)
    {
        var mapping = GetEncounterFileToLocationMapping();

        if (mapping.TryGetValue(encounterID, out var locations) && locations.Count > 0)
        {
            if (locations.Count == 1)
                return locations[0];

            return string.Join(" + ", locations);
        }

        return "Unused";
    }

    /// <summary>
    /// Get the file path for a text archive ID
    /// </summary>
    private string GetTextArchivePath(int archiveID)
    {
        if (_romService?.CurrentRom?.IsLoaded != true)
        {
            throw new InvalidOperationException("ROM not loaded");
        }

        string romPath = _romService.CurrentRom.RomPath;

        // Text archives are in: unpacked/textArchives/[ID] (no .bin extension)
        string archiveFileName = archiveID.ToString("D4");
        return Path.Combine(romPath, "unpacked", "textArchives", archiveFileName);
    }

    /// <summary>
    /// Clear all cached data (call when ROM changes)
    /// </summary>
    public void ClearCache()
    {
        _pokemonNames = null;
        _locationNames = null;
        _loadedArchives.Clear();
        _encounterFileToLocations = null;
        AppLogger.Debug("TextArchiveService cache cleared");
    }

    /// <summary>
    /// Get list of all text archive IDs in the ROM
    /// </summary>
    public List<int> GetAvailableArchiveIDs()
    {
        if (_romService?.CurrentRom?.IsLoaded != true)
        {
            return new List<int>();
        }

        try
        {
            string textArchivesPath = Path.Combine(_romService.CurrentRom.RomPath, "unpacked", "textArchives");
            if (!Directory.Exists(textArchivesPath))
            {
                return new List<int>();
            }

            var files = Directory.GetFiles(textArchivesPath)
                .Select(Path.GetFileName)
                .Where(name => name != null && int.TryParse(name, out _))
                .Select(name => int.Parse(name!))
                .OrderBy(id => id)
                .ToList();

            return files;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to get available archive IDs: {ex.Message}");
            return new List<int>();
        }
    }
}
