using Clockwork.Core;
using Clockwork.Core.Logging;
using Clockwork.Core.Services;
using Clockwork.Core.Settings;

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

            appContext.AddService(romService);
            appContext.AddService(new NdsToolService());
            appContext.AddService(new DialogService());
            appContext.AddService(headerService);
            appContext.AddService(mapService);
            appContext.AddService(colorTableService);

            // Initialize context
            AppLogger.Debug("Initializing application context");
            appContext.Initialize();

            // Set service dependencies
            AppLogger.Debug("Configuring service dependencies");
            headerService.SetRomService(romService);
            mapService.SetRomService(romService);

            // Create and run main overlay
            AppLogger.Info("Creating main overlay with multi-viewport support");
            var settings = SettingsManager.Settings;
            var overlay = new MainOverlay(appContext, settings.WindowWidth, settings.WindowHeight);
            overlay.Run();

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
