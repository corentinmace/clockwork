using Clockwork.Core.Logging;
using Clockwork.Core.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Clockwork.Core.Services;

/// <summary>
/// Service for loading and managing text archives (Pokemon names, location names, etc.).
/// </summary>
public class TextArchiveService : IApplicationService
{
    private readonly ApplicationContext _appContext;
    private RomService? _romService;
    private HeaderService? _headerService;

    // Cached text archives
    private string[]? _pokemonNames;
    private string[]? _locationNames;
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
        _encounterFileToLocations = null;
        AppLogger.Debug("TextArchiveService disposed");
    }

    /// <summary>
    /// Load a text archive by ID.
    /// </summary>
    public TextArchive? LoadTextArchive(int archiveID)
    {
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

            byte[] data = File.ReadAllBytes(archivePath);
            var archive = TextArchive.ReadFromBytes(data);

            AppLogger.Debug($"Loaded text archive {archiveID} with {archive.Messages.Count} messages");
            return archive;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to load text archive {archiveID}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Get Pokemon names (cached).
    /// </summary>
    public string[] GetPokemonNames()
    {
        if (_pokemonNames != null)
            return _pokemonNames;

        try
        {
            var archive = LoadTextArchive(PLATINUM_POKEMON_NAMES_ID);
            if (archive != null)
            {
                _pokemonNames = archive.Messages.ToArray();
                AppLogger.Info($"Loaded {_pokemonNames.Length} Pokemon names");
                return _pokemonNames;
            }
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to load Pokemon names: {ex.Message}");
        }

        // Return empty array if loading failed
        _pokemonNames = new string[0];
        return _pokemonNames;
    }

    /// <summary>
    /// Get location names (cached).
    /// </summary>
    public string[] GetLocationNames()
    {
        if (_locationNames != null)
            return _locationNames;

        try
        {
            var archive = LoadTextArchive(PLATINUM_LOCATION_NAMES_ID);
            if (archive != null)
            {
                _locationNames = archive.Messages.ToArray();
                AppLogger.Info($"Loaded {_locationNames.Length} location names");
                return _locationNames;
            }
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to load location names: {ex.Message}");
        }

        // Return empty array if loading failed
        _locationNames = new string[0];
        return _locationNames;
    }

    /// <summary>
    /// Get Pokemon name by ID.
    /// </summary>
    public string GetPokemonName(uint pokemonID)
    {
        var names = GetPokemonNames();
        if (pokemonID < names.Length)
            return names[pokemonID];

        return $"Pokemon #{pokemonID}";
    }

    /// <summary>
    /// Get location name by ID.
    /// </summary>
    public string GetLocationName(int locationID)
    {
        var names = GetLocationNames();
        if (locationID >= 0 && locationID < names.Length)
            return names[locationID];

        return $"Location #{locationID}";
    }

    /// <summary>
    /// Build mapping of encounter file IDs to location names.
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
    /// Get location names for an encounter file ID.
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
    /// Get the file path for a text archive ID.
    /// </summary>
    private string GetTextArchivePath(int archiveID)
    {
        if (_romService?.CurrentRom?.IsLoaded != true)
        {
            throw new InvalidOperationException("ROM not loaded");
        }

        string romPath = _romService.CurrentRom.RomPath;

        // Text archives are in: unpacked/textArchives/[ID].bin
        string archiveFileName = archiveID.ToString("D4") + ".bin";
        return Path.Combine(romPath, "unpacked", "textArchives", archiveFileName);
    }

    /// <summary>
    /// Clear all cached data (call when ROM changes).
    /// </summary>
    public void ClearCache()
    {
        _pokemonNames = null;
        _locationNames = null;
        _encounterFileToLocations = null;
        AppLogger.Debug("TextArchiveService cache cleared");
    }
}
