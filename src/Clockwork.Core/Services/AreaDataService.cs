using Clockwork.Core.Models;
using Clockwork.Core.Logging;

namespace Clockwork.Core.Services;

/// <summary>
/// Service for managing area data files
/// Area data links game areas to NSBTX tilesets
/// </summary>
public class AreaDataService : IApplicationService
{
    private readonly ApplicationContext _appContext;
    private RomService? _romService;

    public List<int> AvailableAreaDataIds { get; private set; } = new();
    public AreaData? CurrentAreaData { get; private set; }
    public int CurrentAreaDataId { get; private set; } = -1;

    // Area data directory path (relative to ROM root)
    private const string AREA_DATA_DIR = "unpacked/areaData";

    public AreaDataService(ApplicationContext appContext)
    {
        _appContext = appContext;
    }

    public void Initialize()
    {
        _romService = _appContext.GetService<RomService>();
        AppLogger.Info("[AreaDataService] Initialized");
    }

    public void Update(double deltaTime)
    {
        // No per-frame updates needed
    }

    public void Dispose()
    {
        // No cleanup needed
    }

    /// <summary>
    /// Loads the list of available area data files
    /// </summary>
    public void LoadAvailableAreaData()
    {
        AvailableAreaDataIds.Clear();

        if (_romService?.CurrentRom == null)
        {
            AppLogger.Warn("[AreaDataService] No ROM loaded");
            return;
        }

        var areaDataDir = GetAreaDataDirectory();
        if (string.IsNullOrEmpty(areaDataDir) || !Directory.Exists(areaDataDir))
        {
            AppLogger.Warn($"[AreaDataService] Area data directory not found: {areaDataDir}");
            return;
        }

        // Find all .bin files in directory
        var files = Directory.GetFiles(areaDataDir)
            .Select(f => Path.GetFileNameWithoutExtension(f))
            .Where(name => int.TryParse(name, out _))
            .Select(int.Parse)
            .OrderBy(id => id)
            .ToList();

        AvailableAreaDataIds.AddRange(files);
        AppLogger.Info($"[AreaDataService] Found {AvailableAreaDataIds.Count} area data files");
    }

    /// <summary>
    /// Loads area data by ID
    /// </summary>
    public AreaData? LoadAreaData(int areaDataId)
    {
        if (_romService?.CurrentRom == null)
        {
            AppLogger.Warn("[AreaDataService] No ROM loaded");
            return null;
        }

        var filePath = GetAreaDataPath(areaDataId);
        if (!File.Exists(filePath))
        {
            AppLogger.Warn($"[AreaDataService] Area data file not found: {filePath}");
            return null;
        }

        try
        {
            byte[] data = File.ReadAllBytes(filePath);

            AppLogger.Debug($"[AreaDataService] Loading area data {areaDataId}, file size: {data.Length} bytes");

            CurrentAreaData = AreaData.ReadFromBytes(data);
            CurrentAreaDataId = areaDataId;

            AppLogger.Info($"[AreaDataService] Loaded area data {areaDataId} (Buildings: {CurrentAreaData.BuildingsTileset}, Seasonal: {CurrentAreaData.MapTilesetSpring}/{CurrentAreaData.MapTilesetSummer}/{CurrentAreaData.MapTilesetFall}/{CurrentAreaData.MapTilesetWinter}, Light: {CurrentAreaData.LightType})");
            return CurrentAreaData;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"[AreaDataService] Failed to load area data {areaDataId}: {ex.Message}");
            AppLogger.Debug($"[AreaDataService] File path was: {filePath}");
            return null;
        }
    }

    /// <summary>
    /// Saves the current area data
    /// </summary>
    public bool SaveCurrentAreaData()
    {
        if (CurrentAreaData == null || CurrentAreaDataId < 0)
        {
            AppLogger.Warn("[AreaDataService] No area data loaded");
            return false;
        }

        return SaveAreaData(CurrentAreaDataId, CurrentAreaData);
    }

    /// <summary>
    /// Saves area data to file
    /// </summary>
    public bool SaveAreaData(int areaDataId, AreaData areaData)
    {
        if (_romService?.CurrentRom == null)
        {
            AppLogger.Warn("[AreaDataService] No ROM loaded");
            return false;
        }

        var filePath = GetAreaDataPath(areaDataId);

        try
        {
            byte[] data = areaData.ToBytes();
            File.WriteAllBytes(filePath, data);

            AppLogger.Info($"[AreaDataService] Saved area data {areaDataId} to {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"[AreaDataService] Failed to save area data {areaDataId}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Adds a new area data file (duplicates an existing one)
    /// </summary>
    public bool AddAreaData(int sourceId, int newId)
    {
        var sourceData = LoadAreaData(sourceId);
        if (sourceData == null)
        {
            AppLogger.Warn($"[AreaDataService] Failed to load source area data {sourceId}");
            return false;
        }

        if (SaveAreaData(newId, sourceData))
        {
            LoadAvailableAreaData();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Removes an area data file
    /// </summary>
    public bool RemoveAreaData(int areaDataId)
    {
        var filePath = GetAreaDataPath(areaDataId);
        if (!File.Exists(filePath))
        {
            AppLogger.Warn($"[AreaDataService] Area data file not found: {filePath}");
            return false;
        }

        try
        {
            File.Delete(filePath);
            LoadAvailableAreaData();
            AppLogger.Info($"[AreaDataService] Removed area data {areaDataId}");
            return true;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"[AreaDataService] Failed to remove area data {areaDataId}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets the area data directory (DP/Pt only)
    /// </summary>
    private string GetAreaDataDirectory()
    {
        if (_romService?.CurrentRom == null)
            return string.Empty;

        string romPath = _romService!.CurrentRom!.RomPath;
        return Path.Combine(romPath, AREA_DATA_DIR);
    }

    /// <summary>
    /// Gets the full path to an area data file
    /// </summary>
    private string GetAreaDataPath(int areaDataId)
    {
        var directory = GetAreaDataDirectory();
        return Path.Combine(directory, $"{areaDataId:X4}");
    }
}
