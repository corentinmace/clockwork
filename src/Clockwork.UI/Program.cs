using Clockwork.Core;
using Clockwork.Core.Logging;
using Clockwork.Core.Services;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace Clockwork.UI;

internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("Starting Clockwork...");

        // Initialize logger
        AppLogger.Initialize();
        AppLogger.Info("Clockwork application starting...");

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

            // OpenTK window configuration
            AppLogger.Debug("Configuring OpenTK window");
            var nativeWindowSettings = new NativeWindowSettings()
            {
                ClientSize = new Vector2i(1280, 720),
                Title = "Clockwork",
                Flags = ContextFlags.ForwardCompatible,
                WindowBorder = WindowBorder.Resizable,
                WindowState = WindowState.Normal,
                StartVisible = true,
            };

            var gameWindowSettings = new GameWindowSettings()
            {
                UpdateFrequency = 60,
            };

            // Create and run window (Frontend)
            AppLogger.Info("Creating main window");
            using (var window = new MainWindow(appContext, gameWindowSettings, nativeWindowSettings))
            {
                AppLogger.Info("Starting main loop");
                window.Run();
            }

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
