using Clockwork.Core;
using Clockwork.Core.Logging;
using Clockwork.Core.Services;
using Clockwork.Core.Settings;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace Clockwork.UI;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("Starting Clockwork...");

        // Initialize logger
        AppLogger.Initialize();
        AppLogger.Info("Clockwork application starting...");

        // Initialize settings
        SettingsManager.Initialize();

        try
        {
            // Create application context (Backend)
            AppLogger.Debug("Creating application context");
            var appContext = new ApplicationContext();

            // Register services
            AppLogger.Debug("Registering services");
            var romService = new RomService();
            var headerService = new HeaderService();
            var mapService = new MapService();
            var colorTableService = new ColorTableService();
            var levelScriptService = new LevelScriptService(appContext);
            var wildEncounterService = new WildEncounterService(appContext);
            var textArchiveService = new TextArchiveService(appContext);
            var scriptCommandConfigService = new ScriptCommandConfigService();
            var nsbtxService = new NsbtxService(appContext);
            var romPackingService = new RomPackingService(appContext);

            appContext.AddService(romService);
            appContext.AddService(new NdsToolService());
            appContext.AddService(new DialogService());
            appContext.AddService(headerService);
            appContext.AddService(mapService);
            appContext.AddService(colorTableService);
            appContext.AddService(levelScriptService);
            appContext.AddService(wildEncounterService);
            appContext.AddService(textArchiveService);
            appContext.AddService(scriptCommandConfigService);
            appContext.AddService(nsbtxService);
            appContext.AddService(romPackingService);

            // Initialize context
            AppLogger.Debug("Initializing application context");
            appContext.Initialize();

            // Set service dependencies
            AppLogger.Debug("Configuring service dependencies");
            headerService.SetRomService(romService);
            mapService.SetRomService(romService);

            // Initialize script command database with config service
            AppLogger.Debug("Initializing script command database");
            Clockwork.Core.Formats.NDS.Scripts.ScriptDatabase.SetConfigService(scriptCommandConfigService);

            // Veldrid window configuration
            AppLogger.Debug("Configuring Veldrid window");
            var settings = SettingsManager.Settings;

            var windowCreateInfo = new WindowCreateInfo
            {
                X = 100,
                Y = 100,
                WindowWidth = settings.WindowWidth,
                WindowHeight = settings.WindowHeight,
                WindowTitle = "Clockwork - Pokémon: Lost in Time Editor",
                WindowInitialState = settings.WindowMaximized ? WindowState.Maximized : WindowState.Normal
            };

            // Create Veldrid window and graphics device
            AppLogger.Info("Creating Veldrid window and graphics device");
            VeldridStartup.CreateWindowAndGraphicsDevice(
                windowCreateInfo,
                new GraphicsDeviceOptions
                {
                    PreferStandardClipSpaceYDirection = true,
                    PreferDepthRangeZeroToOne = true,
                    SyncToVerticalBlank = true
                },
                GraphicsBackend.OpenGL,
                out Sdl2Window window,
                out GraphicsDevice graphicsDevice);

            // Create and run main window
            var mainWindow = new MainWindow(appContext, window, graphicsDevice);
            mainWindow.Run();

            // Cleanup
            graphicsDevice.WaitForIdle();
            graphicsDevice.Dispose();
            window.Close();

            // Save settings on exit
            SettingsManager.Save();

            AppLogger.Info("Clockwork application closed normally");
            Console.WriteLine("Application closed.");
        }
        catch (Exception ex)
        {
            AppLogger.Fatal($"Fatal error during application startup or runtime: {ex.Message}");
            AppLogger.Debug($"Stack trace: {ex.StackTrace}");
            Console.WriteLine($"Fatal error: {ex.Message}");
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}
