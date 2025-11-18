using Clockwork.Core.Models;

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
            return false;
        }

        try
        {
            _maps.Clear();

            var rom = _romService.CurrentRom;

            // Load maps from unpacked/maps/ directory
            if (!rom.GameDirectories.TryGetValue("maps", out string? mapsPath))
            {
                return false;
            }

            if (!Directory.Exists(mapsPath))
            {
                return false;
            }

            // Get all map files (numbered 0, 1, 2, etc. or 0000, 0001, etc.)
            var mapFiles = Directory.GetFiles(mapsPath)
                .Where(f => int.TryParse(Path.GetFileName(f), out _))
                .OrderBy(f => int.Parse(Path.GetFileName(f)))
                .ToList();

            foreach (var mapFile in mapFiles)
            {
                int mapID = int.Parse(Path.GetFileName(mapFile));
                var map = MapFile.ReadFromFile(mapFile, mapID);

                if (map != null)
                {
                    _maps.Add(map);
                }
            }

            return _maps.Count > 0;
        }
        catch
        {
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
            return false;
        }

        try
        {
            _matrices.Clear();

            var rom = _romService.CurrentRom;

            // Load matrices from unpacked/matrices/ directory
            if (!rom.GameDirectories.TryGetValue("matrices", out string? matricesPath))
            {
                return false;
            }

            if (!Directory.Exists(matricesPath))
            {
                return false;
            }

            // Get all matrix files
            var matrixFiles = Directory.GetFiles(matricesPath)
                .Where(f => int.TryParse(Path.GetFileName(f), out _))
                .OrderBy(f => int.Parse(Path.GetFileName(f)))
                .ToList();

            foreach (var matrixFile in matrixFiles)
            {
                int matrixID = int.Parse(Path.GetFileName(matrixFile));
                var matrix = GameMatrix.ReadFromFile(matrixFile, matrixID);

                if (matrix != null)
                {
                    _matrices.Add(matrix);
                }
            }

            return _matrices.Count > 0;
        }
        catch
        {
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
            return false;
        }

        try
        {
            var rom = _romService.CurrentRom;
            if (!rom.GameDirectories.TryGetValue("maps", out string? mapsPath))
            {
                return false;
            }

            // Find existing file or create new path
            string? existingFile = Directory.GetFiles(mapsPath)
                .FirstOrDefault(f => int.TryParse(Path.GetFileName(f), out int id) && id == map.MapID);

            string mapPath = existingFile ?? Path.Combine(mapsPath, map.MapID.ToString());

            return map.WriteToFile(mapPath);
        }
        catch
        {
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
            return false;
        }

        try
        {
            var rom = _romService.CurrentRom;
            if (!rom.GameDirectories.TryGetValue("matrices", out string? matricesPath))
            {
                return false;
            }

            // Find existing file or create new path
            string? existingFile = Directory.GetFiles(matricesPath)
                .FirstOrDefault(f => int.TryParse(Path.GetFileName(f), out int id) && id == matrix.MatrixID);

            string matrixPath = existingFile ?? Path.Combine(matricesPath, matrix.MatrixID.ToString());

            return matrix.WriteToFile(matrixPath);
        }
        catch
        {
            return false;
        }
    }
}
