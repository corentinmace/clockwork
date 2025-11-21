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

        // Complete map of unpacked directories to their corresponding NARC file paths
        // Based on LiTRE RomInfo.cs SetNarcDirs() for Platinum (line 1520+)
        var narcMappings = new Dictionary<string, string>
        {
            // === Pokemon Data ===
            ["personalPokeData"] = "poketool/personal/pl_personal.narc",
            ["pokemonBattleSprites"] = "poketool/pokegra/pl_pokegra.narc",
            ["otherPokemonBattleSprites"] = "poketool/pokegra/pl_otherpoke.narc",
            ["monIcons"] = "poketool/icongra/pl_poke_icon.narc",
            ["learnsets"] = "poketool/personal/wotbl.narc",
            ["evolutions"] = "poketool/personal/evo.narc",

            // === Map & Field Data ===
            ["dynamicHeaders"] = "debug/cb_edit/d_test.narc",
            ["matrices"] = "fielddata/mapmatrix/map_matrix.narc",
            ["maps"] = "fielddata/land_data/land_data.narc",
            ["exteriorBuildingModels"] = "fielddata/build_model/build_model.narc",
            ["buildingConfigFiles"] = "fielddata/areadata/area_build_model/area_build.narc",
            ["buildingTextures"] = "fielddata/areadata/area_build_model/areabm_texset.narc",
            ["mapTextures"] = "fielddata/areadata/area_map_tex/map_tex_set.narc",
            ["areaData"] = "fielddata/areadata/area_data.narc",
            ["eventFiles"] = "fielddata/eventdata/zone_event.narc",
            ["scripts"] = "fielddata/script/scr_seq.narc",
            ["encounters"] = "fielddata/encountdata/pl_enc_data.narc",

            // === Trainer & Battle Data ===
            ["trainerProperties"] = "poketool/trainer/trdata.narc",
            ["trainerParty"] = "poketool/trainer/trpoke.narc",
            ["trainerGraphics"] = "poketool/trgra/trfgra.narc",
            ["moveData"] = "poketool/waza/pl_waza_tbl.narc",

            // === Item Data ===
            ["itemData"] = "itemtool/itemdata/pl_item_data.narc",
            ["itemIcons"] = "itemtool/itemdata/item_icon.narc",

            // === Graphics & Sprites ===
            ["OWSprites"] = "data/mmodel/mmodel.narc",

            // === System & Misc ===
            ["synthOverlay"] = "data/weather_sys.narc",
            ["textArchives"] = "msgdata/pl_msg.narc",
            ["tradeData"] = "fielddata/pokemon_trade/fld_trade.narc",
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
