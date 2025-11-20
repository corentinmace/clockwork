using Clockwork.Core.Formats.NDS;
using Clockwork.Core.Logging;

namespace Clockwork.Core.Services;

/// <summary>
/// Service responsible for packing all NARC archives before ROM creation.
/// </summary>
public class RomPackingService : IApplicationService
{
    private readonly ApplicationContext _appContext;
    private RomService? _romService;

    public RomPackingService(ApplicationContext appContext)
    {
        _appContext = appContext;
    }

    public void Initialize()
    {
        _romService = _appContext.GetService<RomService>();
        AppLogger.Info("RomPackingService initialized");
    }

    public void Update(double deltaTime)
    {
        // Nothing to do
    }

    public void Shutdown()
    {
        // Nothing to cleanup
    }

    public void Dispose()
    {
        // Nothing to dispose
    }

    /// <summary>
    /// Packs all unpacked NARC directories back into their NARC files.
    /// This must be called before creating the final ROM file.
    /// </summary>
    /// <param name="progressCallback">Optional callback for progress updates</param>
    /// <returns>True if packing succeeded</returns>
    public bool PackAllNarcs(Action<string>? progressCallback = null)
    {
        if (_romService?.CurrentRom?.IsLoaded != true)
        {
            AppLogger.Error("Cannot pack NARCs: No ROM loaded");
            return false;
        }

        AppLogger.Info("Starting NARC packing process...");
        progressCallback?.Invoke("Packing NARC archives...");

        try
        {
            var romPath = _romService.CurrentRom.RomPath;
            int packedCount = 0;
            int totalDirectories = 0;

            // Get all game directories that need packing
            var directoriesToPack = GetDirectoriesToPack();
            totalDirectories = directoriesToPack.Count;

            AppLogger.Debug($"Found {totalDirectories} directories to pack");

            foreach (var (unpackedPath, packedPath, name) in directoriesToPack)
            {
                try
                {
                    progressCallback?.Invoke($"Packing {name}...");
                    AppLogger.Debug($"Packing NARC: {name} from {unpackedPath} to {packedPath}");

                    // Create NARC from unpacked folder
                    using var narc = NarcFile.FromFolder(unpackedPath);

                    // Ensure the output directory exists
                    var outputDir = Path.GetDirectoryName(packedPath);
                    if (!string.IsNullOrEmpty(outputDir))
                    {
                        Directory.CreateDirectory(outputDir);
                    }

                    // Save the NARC file
                    narc.Save(packedPath);

                    packedCount++;
                    progressCallback?.Invoke($"Packed {packedCount}/{totalDirectories}: {name}");
                }
                catch (Exception ex)
                {
                    AppLogger.Error($"Failed to pack NARC {name}: {ex.Message}");
                    progressCallback?.Invoke($"ERROR packing {name}: {ex.Message}");
                    // Continue with other NARCs even if one fails
                }
            }

            AppLogger.Info($"NARC packing completed: {packedCount}/{totalDirectories} archives packed");
            progressCallback?.Invoke($"Packing complete: {packedCount}/{totalDirectories} archives");

            return packedCount == totalDirectories;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"NARC packing failed: {ex.Message}");
            progressCallback?.Invoke($"ERROR: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Gets the list of directories that need to be packed into NARC files.
    /// </summary>
    private List<(string unpackedPath, string packedPath, string name)> GetDirectoriesToPack()
    {
        var directories = new List<(string, string, string)>();

        if (_romService?.CurrentRom == null)
            return directories;

        var romPath = _romService.CurrentRom.RomPath;
        var unpackedBasePath = Path.Combine(romPath, "unpacked");
        var dataBasePath = Path.Combine(romPath, "data");

        // Map of unpacked directories to their corresponding NARC file paths
        // Based on LiTRE structure and Platinum file layout
        var narcMappings = new Dictionary<string, string>
        {
            // Fielddata NARCs
            ["areadata"] = "fielddata/areadata/area_data.narc",
            ["build_model"] = "fielddata/build_model/build_model.narc",

            // Main game NARCs - Platinum paths
            ["dynamicHeaders"] = "debug/cb_edit/d_test.narc",           // Map headers
            ["scripts"] = "fielddata/script/scr_seq.narc",              // Scripts
            ["eventFiles"] = "fielddata/eventdata/zone_event.narc",     // Event files
            ["matrices"] = "fielddata/mapmatrix/map_matrix.narc",       // Matrices
            ["maps"] = "fielddata/land_data/land_data.narc",            // Map files
            ["textArchives"] = "msgdata/pl_msg.narc",                   // Text archives
            ["encounters"] = "fielddata/encountdata/pl_enc_data.narc",  // Wild encounters
        };

        foreach (var (dirName, narcRelativePath) in narcMappings)
        {
            string unpackedPath = Path.Combine(unpackedBasePath, dirName);
            string packedPath = Path.Combine(dataBasePath, narcRelativePath);

            // Only add if the unpacked directory exists
            if (Directory.Exists(unpackedPath))
            {
                directories.Add((unpackedPath, packedPath, dirName));
            }
            else
            {
                AppLogger.Debug($"Skipping {dirName}: directory does not exist");
            }
        }

        return directories;
    }
}
