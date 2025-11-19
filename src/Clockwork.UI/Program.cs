using Clockwork.Core;
using Clockwork.Core.Logging;
using Clockwork.Core.Services;
using Clockwork.Core.Settings;
using Silk.NET.Maths;
using Silk.NET.Windowing;

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

            appContext.AddService(romService);
            appContext.AddService(new NdsToolService());
            appContext.AddService(new DialogService());
            appContext.AddService(headerService);
            appContext.AddService(mapService);

            // Initialize context
            AppLogger.Debug("Initializing application context");
            appContext.Initialize();

            // Set service dependencies
            AppLogger.Debug("Configuring service dependencies");
            headerService.SetRomService(romService);
            mapService.SetRomService(romService);

            // Silk.NET window configuration
            AppLogger.Debug("Configuring Silk.NET window");
            var settings = SettingsManager.Settings;

            var options = WindowOptions.Default;
            options.Size = new Vector2D<int>(settings.WindowWidth, settings.WindowHeight);
            options.Title = "Clockwork - Pokémon ROM Editor";
            options.VSync = true;
            options.ShouldSwapAutomatically = true;
            options.WindowState = settings.WindowMaximized ? WindowState.Maximized : WindowState.Normal;
            options.API = new GraphicsAPI(
                ContextAPI.OpenGL,
                new APIVersion(3, 3));

            // Create and run window (Frontend)
            AppLogger.Info("Creating main window");
            var window = Window.Create(options);

            var mainWindow = new MainWindow(appContext, window);
            mainWindow.Run();

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
