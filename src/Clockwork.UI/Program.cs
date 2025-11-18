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
        Console.WriteLine("Démarrage de Clockwork...");

        // Créer le contexte de l'application (Backend)
        var appContext = new ApplicationContext();

        // Enregistrer les services métier
        appContext.AddService(new ExampleService());
        appContext.AddService(new DashboardService());
        appContext.AddService(new UserService());
        appContext.AddService(new DataService());

        // Initialiser le contexte
        appContext.Initialize();

        // Configuration de la fenêtre OpenTK
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

        // Créer et lancer la fenêtre (Frontend)
        using (var window = new MainWindow(appContext, gameWindowSettings, nativeWindowSettings))
        {
            window.Run();
        }

        Console.WriteLine("Application fermée.");
    }
}
