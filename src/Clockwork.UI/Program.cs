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

        // Create application context (Backend)
        var appContext = new ApplicationContext();

        // Register services
        appContext.AddService(new RomService());
        appContext.AddService(new NdsToolService());
        appContext.AddService(new DialogService());

        // Initialize context
        appContext.Initialize();

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

        Console.WriteLine("Application closed.");
    }
}
