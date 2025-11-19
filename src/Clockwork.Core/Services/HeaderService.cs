using Clockwork.Core.Models;
using Clockwork.Core.Logging;

namespace Clockwork.Core.Services;

/// <summary>
/// Service for loading and managing map headers from ROM.
/// </summary>
public class HeaderService : IApplicationService
{
    private RomService? _romService;
    private List<MapHeader> _headers = new();
    private Dictionary<int, string> _internalNames = new();

    public IReadOnlyList<MapHeader> Headers => _headers.AsReadOnly();
    public bool IsLoaded => _headers.Count > 0;

    public void Initialize()
    {
        AppLogger.Info("HeaderService initialized");
        _headers.Clear();
        _internalNames.Clear();
    }

    public void Update(double deltaTime)
    {
        // Nothing to update
    }

    public void Shutdown()
    {
        _headers.Clear();
        _internalNames.Clear();
    }

    public void Dispose()
    {
        _headers.Clear();
        _internalNames.Clear();
    }

    /// <summary>
    /// Sets the ROM service dependency.
    /// </summary>
    public void SetRomService(RomService romService)
    {
        AppLogger.Debug("RomService dependency set for HeaderService");
        _romService = romService;
    }

    /// <summary>
    /// Loads all headers from the currently loaded ROM.
    /// Following LiTRE structure:
    /// - Headers are in: unpacked/dynamicHeaders/{headerID}
    /// - Internal names are in: data/fielddata/maptable/mapname.bin
    /// </summary>
    public bool LoadHeadersFromRom()
    {
        if (_romService?.CurrentRom == null)
        {
            AppLogger.Error("Cannot load headers: No ROM is loaded");
            return false;
        }

        try
        {
            AppLogger.Info("Loading headers from ROM...");
            _headers.Clear();
            _internalNames.Clear();

            var rom = _romService.CurrentRom;

            // Load internal names first
            LoadInternalNames();

            // Load headers from unpacked/dynamicHeaders/ directory (LiTRE structure)
            if (!rom.GameDirectories.TryGetValue("dynamicHeaders", out string? headersPath))
            {
                AppLogger.Error("dynamicHeaders directory not found in ROM structure");
                return false;
            }

            if (!Directory.Exists(headersPath))
            {
                AppLogger.Error($"dynamicHeaders directory does not exist: {headersPath}");
                return false;
            }

            AppLogger.Debug($"Scanning for header files in: {headersPath}");

            // Get all header files (numbered 0, 1, 2, etc. or 0000, 0001, etc.)
            var headerFiles = Directory.GetFiles(headersPath)
                .Where(f => int.TryParse(Path.GetFileName(f), out _))
                .OrderBy(f => int.Parse(Path.GetFileName(f)))
                .ToList();

            AppLogger.Debug($"Found {headerFiles.Count} header files to load");

            int loadedCount = 0;
            foreach (var headerFile in headerFiles)
            {
                int headerID = int.Parse(Path.GetFileName(headerFile));
                var header = MapHeader.ReadFromFile(headerFile, headerID);

                if (header != null)
                {
                    // Set internal name if available
                    if (_internalNames.TryGetValue(headerID, out string? internalName))
                    {
                        header.InternalName = internalName;
                    }
                    else
                    {
                        header.InternalName = $"Header {headerID}";
                    }

                    _headers.Add(header);
                    loadedCount++;
                }
                else
                {
                    AppLogger.Warn($"Failed to load header file: {headerFile}");
                }
            }

            AppLogger.Info($"Successfully loaded {loadedCount} headers");
            return _headers.Count > 0;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Exception while loading headers: {ex.Message}");
            _headers.Clear();
            return false;
        }
    }

    /// <summary>
    /// Loads internal names from mapname.bin.
    /// Each name is 16 bytes.
    /// Following LiTRE: data/fielddata/maptable/mapname.bin
    /// </summary>
    private void LoadInternalNames()
    {
        if (_romService?.CurrentRom == null) return;

        try
        {
            AppLogger.Debug("Loading internal map names from mapname.bin...");

            var rom = _romService.CurrentRom;
            if (!rom.GameDirectories.TryGetValue("maptable", out string? maptablePath))
            {
                AppLogger.Warn("maptable directory not found in ROM structure");
                return;
            }

            string mapnamePath = Path.Combine(maptablePath, "mapname.bin");
            if (!File.Exists(mapnamePath))
            {
                AppLogger.Warn($"mapname.bin not found at: {mapnamePath}");
                return;
            }

            using var fs = new FileStream(mapnamePath, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(fs);

            int headerID = 0;
            while (fs.Position < fs.Length)
            {
                byte[] nameBytes = reader.ReadBytes(16);

                // Find null terminator
                int nullIndex = Array.IndexOf(nameBytes, (byte)0);
                if (nullIndex < 0) nullIndex = 16;

                // Convert to string (ASCII encoding)
                string name = System.Text.Encoding.ASCII.GetString(nameBytes, 0, nullIndex);
                _internalNames[headerID] = name;

                headerID++;
            }

            AppLogger.Debug($"Loaded {_internalNames.Count} internal map names");
        }
        catch (Exception ex)
        {
            AppLogger.Warn($"Failed to load internal names: {ex.Message}");
            // If we can't load internal names, headers will just have default names
        }
    }

    /// <summary>
    /// Gets a header by ID.
    /// </summary>
    public MapHeader? GetHeader(int headerID)
    {
        return _headers.FirstOrDefault(h => h.HeaderID == headerID);
    }

    /// <summary>
    /// Saves a header back to its file in unpacked/dynamicHeaders/.
    /// Following LiTRE structure.
    /// </summary>
    public bool SaveHeader(MapHeader header)
    {
        if (_romService?.CurrentRom == null)
        {
            AppLogger.Error("Cannot save header: No ROM is loaded");
            return false;
        }

        try
        {
            AppLogger.Debug($"Saving header ID {header.HeaderID}...");

            var rom = _romService.CurrentRom;
            if (!rom.GameDirectories.TryGetValue("dynamicHeaders", out string? headersPath))
            {
                AppLogger.Error("dynamicHeaders directory not found in ROM structure");
                return false;
            }

            // File name can be with or without leading zeros (0 or 0000)
            // Try to find existing file first
            string? existingFile = Directory.GetFiles(headersPath)
                .FirstOrDefault(f => int.TryParse(Path.GetFileName(f), out int id) && id == header.HeaderID);

            string headerPath = existingFile ?? Path.Combine(headersPath, header.HeaderID.ToString());

            bool success = header.WriteToFile(headerPath);

            if (success)
            {
                AppLogger.Info($"Header ID {header.HeaderID} saved successfully");
            }
            else
            {
                AppLogger.Error($"Failed to save header ID {header.HeaderID}");
            }

            return success;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Exception while saving header ID {header.HeaderID}: {ex.Message}");
            return false;
        }
    }
}
