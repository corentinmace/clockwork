using Clockwork.Core;
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
        AppLogger.Initialize("clockwork.log", LogLevel.Debug);
        AppLogger.Info("Clockwork application starting...");

        // Create application context (Backend)
        var appContext = new ApplicationContext();

        // Register services
        var romService = new RomService();
        var headerService = new HeaderService();
        var mapService = new MapService();

        appContext.AddService(romService);
        appContext.AddService(new NdsToolService());
        appContext.AddService(new DialogService());
        appContext.AddService(headerService);
        appContext.AddService(mapService);

        // Initialize context
        appContext.Initialize();

        // Set service dependencies
        headerService.SetRomService(romService);
        mapService.SetRomService(romService);

        // OpenTK window configuration
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
        using (var window = new MainWindow(appContext, gameWindowSettings, nativeWindowSettings))
        {
            window.Run();
        }

        AppLogger.Info("Clockwork application closed.");
        Console.WriteLine("Application closed.");
    }
}
