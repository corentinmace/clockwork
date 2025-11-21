using Clockwork.Core.Logging;
using Clockwork.Core.Models;

namespace Clockwork.Core.Services;

/// <summary>
/// Service for managing NSBTX (Nintendo DS Texture) files
/// Handles loading, editing, and saving texture packs
/// </summary>
public class NsbtxService : IApplicationService
{
    private readonly ApplicationContext _appContext;
    private RomService? _romService;

    /// <summary>
    /// Currently loaded NSBTX file
    /// </summary>
    public NsbtxFile? CurrentNsbtx { get; private set; }

    /// <summary>
    /// List of available NSBTX file IDs in the ROM
    /// </summary>
    public List<int> AvailableNsbtxIds { get; private set; } = new();

    /// <summary>
    /// Type of texture pack (Map or Building)
    /// </summary>
    public enum TexturePackType
    {
        Map,
        Building
    }

    /// <summary>
    /// Currently selected texture pack type
    /// </summary>
    public TexturePackType CurrentPackType { get; set; } = TexturePackType.Map;

    public NsbtxService(ApplicationContext appContext)
    {
        _appContext = appContext;
    }

    public void Initialize()
    {
        _romService = _appContext.GetService<RomService>();
        AppLogger.Info("[NsbtxService] Initialized");
    }

    public void Update(double deltaTime)
    {
        // No per-frame updates needed
    }

    public void Dispose()
    {
        CurrentNsbtx = null;
        AppLogger.Debug("[NsbtxService] Disposed");
    }

    /// <summary>
    /// Loads available NSBTX files from the ROM
    /// </summary>
    public void LoadAvailableNsbtx()
    {
        AvailableNsbtxIds.Clear();

        if (_romService?.CurrentRom == null)
        {
            AppLogger.Warn("[NsbtxService] Cannot load NSBTX files - no ROM loaded");
            return;
        }

        string texturesPath = GetTexturesPath();
        if (!Directory.Exists(texturesPath))
        {
            AppLogger.Warn($"[NsbtxService] Textures directory not found: {texturesPath}");
            return;
        }

        // Find all .nsbtx files in the directory
        var files = Directory.GetFiles(texturesPath)
            .Select(Path.GetFileNameWithoutExtension)
            .Where(name => int.TryParse(name, out _))
            .Select(int.Parse)
            .OrderBy(id => id)
            .ToList();

        AvailableNsbtxIds = files;
        AppLogger.Info($"[NsbtxService] Found {AvailableNsbtxIds.Count} NSBTX files in {texturesPath}");
    }

    /// <summary>
    /// Loads an NSBTX file by ID
    /// </summary>
    public NsbtxFile? LoadNsbtx(int nsbtxId)
    {
        if (_romService?.CurrentRom == null)
        {
            AppLogger.Warn("[NsbtxService] Cannot load NSBTX - no ROM loaded");
            return null;
        }

        string nsbtxPath = GetNsbtxPath(nsbtxId);
        if (!File.Exists(nsbtxPath))
        {
            AppLogger.Warn($"[NsbtxService] NSBTX file not found: {nsbtxPath}");
            return null;
        }

        try
        {
            var data = File.ReadAllBytes(nsbtxPath);
            CurrentNsbtx = NsbtxFile.FromBytes(data, nsbtxPath);
            AppLogger.Info($"[NsbtxService] Loaded NSBTX {nsbtxId} ({CurrentPackType}) with " +
                          $"{CurrentNsbtx.TextureNames.Count} textures and {CurrentNsbtx.PaletteNames.Count} palettes");
            return CurrentNsbtx;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"[NsbtxService] Failed to load NSBTX {nsbtxId}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Saves the current NSBTX file
    /// </summary>
    public bool SaveCurrentNsbtx()
    {
        if (CurrentNsbtx == null)
        {
            AppLogger.Warn("[NsbtxService] No NSBTX loaded to save");
            return false;
        }

        try
        {
            CurrentNsbtx.SaveToFile(CurrentNsbtx.FilePath);
            AppLogger.Info($"[NsbtxService] Saved NSBTX to {CurrentNsbtx.FilePath}");
            return true;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"[NsbtxService] Failed to save NSBTX: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Imports an NSBTX file from an external source
    /// </summary>
    public NsbtxFile? ImportNsbtx(string sourcePath, int targetId)
    {
        if (_romService?.CurrentRom == null)
        {
            AppLogger.Warn("[NsbtxService] Cannot import NSBTX - no ROM loaded");
            return null;
        }

        if (!File.Exists(sourcePath))
        {
            AppLogger.Error($"[NsbtxService] Source file not found: {sourcePath}");
            return null;
        }

        try
        {
            string targetPath = GetNsbtxPath(targetId);

            // Copy file to ROM directory
            File.Copy(sourcePath, targetPath, true);

            // Load and return the imported file
            var imported = LoadNsbtx(targetId);
            AppLogger.Info($"[NsbtxService] Imported NSBTX from {sourcePath} to ID {targetId}");
            return imported;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"[NsbtxService] Failed to import NSBTX: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Exports the current NSBTX file to an external location
    /// </summary>
    public bool ExportNsbtx(string targetPath)
    {
        if (CurrentNsbtx == null)
        {
            AppLogger.Warn("[NsbtxService] No NSBTX loaded to export");
            return false;
        }

        try
        {
            File.Copy(CurrentNsbtx.FilePath, targetPath, true);
            AppLogger.Info($"[NsbtxService] Exported NSBTX to {targetPath}");
            return true;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"[NsbtxService] Failed to export NSBTX: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Adds a new NSBTX file by duplicating an existing one
    /// </summary>
    public bool AddNsbtx(int sourceId, int newId)
    {
        if (_romService?.CurrentRom == null)
        {
            AppLogger.Warn("[NsbtxService] Cannot add NSBTX - no ROM loaded");
            return false;
        }

        string sourcePath = GetNsbtxPath(sourceId);
        string newPath = GetNsbtxPath(newId);

        if (!File.Exists(sourcePath))
        {
            AppLogger.Error($"[NsbtxService] Source NSBTX not found: {sourcePath}");
            return false;
        }

        if (File.Exists(newPath))
        {
            AppLogger.Warn($"[NsbtxService] NSBTX ID {newId} already exists");
            return false;
        }

        try
        {
            File.Copy(sourcePath, newPath);
            LoadAvailableNsbtx(); // Refresh the list
            AppLogger.Info($"[NsbtxService] Created new NSBTX {newId} from {sourceId}");
            return true;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"[NsbtxService] Failed to add NSBTX: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Removes an NSBTX file
    /// </summary>
    public bool RemoveNsbtx(int nsbtxId)
    {
        if (_romService?.CurrentRom == null)
        {
            AppLogger.Warn("[NsbtxService] Cannot remove NSBTX - no ROM loaded");
            return false;
        }

        string nsbtxPath = GetNsbtxPath(nsbtxId);
        if (!File.Exists(nsbtxPath))
        {
            AppLogger.Warn($"[NsbtxService] NSBTX {nsbtxId} not found");
            return false;
        }

        try
        {
            File.Delete(nsbtxPath);
            LoadAvailableNsbtx(); // Refresh the list

            if (CurrentNsbtx?.FilePath == nsbtxPath)
            {
                CurrentNsbtx = null;
            }

            AppLogger.Info($"[NsbtxService] Removed NSBTX {nsbtxId}");
            return true;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"[NsbtxService] Failed to remove NSBTX: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets the directory path for texture files based on current pack type
    /// </summary>
    private string GetTexturesPath()
    {
        string romPath = _romService!.CurrentRom!.RomPath;
        string subDir = CurrentPackType == TexturePackType.Map ? "mapTextures" : "buildingTextures";
        return Path.Combine(romPath, "unpacked", subDir);
    }

    /// <summary>
    /// Gets the file path for a specific NSBTX ID
    /// </summary>
    private string GetNsbtxPath(int nsbtxId)
    {
        string texturesPath = GetTexturesPath();
        string filename = nsbtxId.ToString("D4");
        return Path.Combine(texturesPath, filename);
    }
}
