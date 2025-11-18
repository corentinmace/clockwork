using Clockwork.Core.Models;

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
        _romService = romService;
    }

    /// <summary>
    /// Loads all headers from the currently loaded ROM.
    /// Headers are expected to be in: data/a/0/5/0/{headerID}
    /// Internal names are in: data/fielddata/maptable/mapname.bin
    /// </summary>
    public bool LoadHeadersFromRom()
    {
        if (_romService?.CurrentRom == null)
        {
            return false;
        }

        try
        {
            _headers.Clear();
            _internalNames.Clear();

            string romPath = _romService.CurrentRom.RomPath;

            // Load internal names first
            LoadInternalNames(romPath);

            // Load headers from a/0/5/0 directory
            string headersPath = Path.Combine(romPath, "data", "a", "0", "5", "0");
            if (!Directory.Exists(headersPath))
            {
                return false;
            }

            // Get all header files (numbered 0, 1, 2, etc.)
            var headerFiles = Directory.GetFiles(headersPath)
                .Where(f => int.TryParse(Path.GetFileName(f), out _))
                .OrderBy(f => int.Parse(Path.GetFileName(f)))
                .ToList();

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
                }
            }

            return _headers.Count > 0;
        }
        catch
        {
            _headers.Clear();
            return false;
        }
    }

    /// <summary>
    /// Loads internal names from mapname.bin.
    /// Each name is 16 bytes.
    /// </summary>
    private void LoadInternalNames(string romPath)
    {
        try
        {
            string mapnamePath = Path.Combine(romPath, "data", "fielddata", "maptable", "mapname.bin");
            if (!File.Exists(mapnamePath))
            {
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
        }
        catch
        {
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
    /// Saves a header back to its file.
    /// </summary>
    public bool SaveHeader(MapHeader header)
    {
        if (_romService?.CurrentRom == null)
        {
            return false;
        }

        try
        {
            string romPath = _romService.CurrentRom.RomPath;
            string headerPath = Path.Combine(romPath, "data", "a", "0", "5", "0", header.HeaderID.ToString());

            return header.WriteToFile(headerPath);
        }
        catch
        {
            return false;
        }
    }
}
