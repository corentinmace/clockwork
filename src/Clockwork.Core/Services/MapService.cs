using Clockwork.Core.Models;
using Clockwork.Core.Logging;

namespace Clockwork.Core.Services;

/// <summary>
/// Service for loading and managing map files from ROM.
/// </summary>
public class MapService : IApplicationService
{
    private RomService? _romService;
    private List<MapFile> _maps = new();
    private List<GameMatrix> _matrices = new();

    public IReadOnlyList<MapFile> Maps => _maps.AsReadOnly();
    public IReadOnlyList<GameMatrix> Matrices => _matrices.AsReadOnly();
    public bool IsLoaded => _maps.Count > 0;

    public void Initialize()
    {
        AppLogger.Info("MapService initialized");
        _maps.Clear();
        _matrices.Clear();
    }

    public void Update(double deltaTime)
    {
        // Nothing to update
    }

    public void Shutdown()
    {
        _maps.Clear();
        _matrices.Clear();
    }

    public void Dispose()
    {
        _maps.Clear();
        _matrices.Clear();
    }

    /// <summary>
    /// Sets the ROM service dependency.
    /// </summary>
    public void SetRomService(RomService romService)
    {
        AppLogger.Debug("RomService dependency set for MapService");
        _romService = romService;
    }

    /// <summary>
    /// Loads all maps from the currently loaded ROM.
    /// Following LiTRE: Maps are in unpacked/maps/
    /// </summary>
    public bool LoadMapsFromRom()
    {
        if (_romService?.CurrentRom == null)
        {
            AppLogger.Error("Cannot load maps: No ROM is loaded");
            return false;
        }

        try
        {
            AppLogger.Info("Loading maps from ROM...");
            _maps.Clear();

            var rom = _romService.CurrentRom;

            // Load maps from unpacked/maps/ directory
            if (!rom.GameDirectories.TryGetValue("maps", out string? mapsPath))
            {
                AppLogger.Error("Maps directory not found in ROM structure");
                return false;
            }

            if (!Directory.Exists(mapsPath))
            {
                AppLogger.Error($"Maps directory does not exist: {mapsPath}");
                return false;
            }

            AppLogger.Debug($"Scanning for map files in: {mapsPath}");

            // Get all map files (numbered 0, 1, 2, etc. or 0000, 0001, etc.)
            var mapFiles = Directory.GetFiles(mapsPath)
                .Where(f => int.TryParse(Path.GetFileName(f), out _))
                .OrderBy(f => int.Parse(Path.GetFileName(f)))
                .ToList();

            AppLogger.Debug($"Found {mapFiles.Count} map files to load");

            int loadedCount = 0;
            foreach (var mapFile in mapFiles)
            {
                int mapID = int.Parse(Path.GetFileName(mapFile));
                var map = MapFile.ReadFromFile(mapFile, mapID);

                if (map != null)
                {
                    _maps.Add(map);
                    loadedCount++;
                }
                else
                {
                    AppLogger.Warn($"Failed to load map file: {mapFile}");
                }
            }

            AppLogger.Info($"Successfully loaded {loadedCount} maps");
            return _maps.Count > 0;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Exception while loading maps: {ex.Message}");
            _maps.Clear();
            return false;
        }
    }

    /// <summary>
    /// Loads all matrices from the currently loaded ROM.
    /// Following LiTRE: Matrices are in unpacked/matrices/
    /// </summary>
    public bool LoadMatricesFromRom()
    {
        if (_romService?.CurrentRom == null)
        {
            AppLogger.Error("Cannot load matrices: No ROM is loaded");
            return false;
        }

        try
        {
            AppLogger.Info("Loading matrices from ROM...");
            _matrices.Clear();

            var rom = _romService.CurrentRom;

            // Load matrices from unpacked/matrices/ directory
            if (!rom.GameDirectories.TryGetValue("matrices", out string? matricesPath))
            {
                AppLogger.Error("Matrices directory not found in ROM structure");
                return false;
            }

            if (!Directory.Exists(matricesPath))
            {
                AppLogger.Error($"Matrices directory does not exist: {matricesPath}");
                return false;
            }

            AppLogger.Debug($"Scanning for matrix files in: {matricesPath}");

            // Get all matrix files
            var matrixFiles = Directory.GetFiles(matricesPath)
                .Where(f => int.TryParse(Path.GetFileName(f), out _))
                .OrderBy(f => int.Parse(Path.GetFileName(f)))
                .ToList();

            AppLogger.Debug($"Found {matrixFiles.Count} matrix files to load");

            int loadedCount = 0;
            foreach (var matrixFile in matrixFiles)
            {
                int matrixID = int.Parse(Path.GetFileName(matrixFile));
                var matrix = GameMatrix.ReadFromFile(matrixFile, matrixID);

                if (matrix != null)
                {
                    _matrices.Add(matrix);
                    loadedCount++;
                }
                else
                {
                    AppLogger.Warn($"Failed to load matrix file: {matrixFile}");
                }
            }

            AppLogger.Info($"Successfully loaded {loadedCount} matrices");
            return _matrices.Count > 0;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Exception while loading matrices: {ex.Message}");
            _matrices.Clear();
            return false;
        }
    }

    /// <summary>
    /// Gets a map by ID.
    /// </summary>
    public MapFile? GetMap(int mapID)
    {
        return _maps.FirstOrDefault(m => m.MapID == mapID);
    }

    /// <summary>
    /// Gets a matrix by ID.
    /// </summary>
    public GameMatrix? GetMatrix(int matrixID)
    {
        return _matrices.FirstOrDefault(m => m.MatrixID == matrixID);
    }

    /// <summary>
    /// Saves a map back to its file in unpacked/maps/.
    /// </summary>
    public bool SaveMap(MapFile map)
    {
        if (_romService?.CurrentRom == null)
        {
            AppLogger.Error("Cannot save map: No ROM is loaded");
            return false;
        }

        try
        {
            AppLogger.Debug($"Saving map ID {map.MapID}...");

            var rom = _romService.CurrentRom;
            if (!rom.GameDirectories.TryGetValue("maps", out string? mapsPath))
            {
                AppLogger.Error("Maps directory not found in ROM structure");
                return false;
            }

            // Find existing file or create new path
            string? existingFile = Directory.GetFiles(mapsPath)
                .FirstOrDefault(f => int.TryParse(Path.GetFileName(f), out int id) && id == map.MapID);

            string mapPath = existingFile ?? Path.Combine(mapsPath, map.MapID.ToString());

            bool success = map.WriteToFile(mapPath);

            if (success)
            {
                AppLogger.Info($"Map ID {map.MapID} saved successfully");
            }
            else
            {
                AppLogger.Error($"Failed to save map ID {map.MapID}");
            }

            return success;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Exception while saving map ID {map.MapID}: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Saves a matrix back to its file in unpacked/matrices/.
    /// </summary>
    public bool SaveMatrix(GameMatrix matrix)
    {
        if (_romService?.CurrentRom == null)
        {
            AppLogger.Error("Cannot save matrix: No ROM is loaded");
            return false;
        }

        try
        {
            AppLogger.Debug($"Saving matrix ID {matrix.MatrixID}...");

            var rom = _romService.CurrentRom;
            if (!rom.GameDirectories.TryGetValue("matrices", out string? matricesPath))
            {
                AppLogger.Error("Matrices directory not found in ROM structure");
                return false;
            }

            // Find existing file or create new path
            string? existingFile = Directory.GetFiles(matricesPath)
                .FirstOrDefault(f => int.TryParse(Path.GetFileName(f), out int id) && id == matrix.MatrixID);

            string matrixPath = existingFile ?? Path.Combine(matricesPath, matrix.MatrixID.ToString());

            bool success = matrix.WriteToFile(matrixPath);

            if (success)
            {
                AppLogger.Info($"Matrix ID {matrix.MatrixID} saved successfully");
            }
            else
            {
                AppLogger.Error($"Failed to save matrix ID {matrix.MatrixID}");
            }

            return success;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Exception while saving matrix ID {matrix.MatrixID}: {ex.Message}");
            return false;
        }
    }
}
