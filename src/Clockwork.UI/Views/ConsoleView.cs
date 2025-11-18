using Clockwork.Core;
using ImGuiNET;

namespace Clockwork.UI.Views;

/// <summary>
/// Vue de la console.
/// </summary>
public class ConsoleView : IView
{
    private readonly ApplicationContext _appContext;

    public bool IsVisible { get; set; } = false;

    public ConsoleView(ApplicationContext appContext)
    {
        _appContext = appContext;
    }

    public void Draw()
    {
        if (!IsVisible) return;

        bool isVisible = IsVisible;
        ImGui.Begin("Console", ref isVisible);

        ImGui.TextColored(new System.Numerics.Vector4(0.5f, 0.8f, 0.5f, 1.0f), "[INFO]");
        ImGui.SameLine();
        ImGui.Text("Application démarrée avec succès");

        ImGui.TextColored(new System.Numerics.Vector4(0.5f, 0.8f, 0.5f, 1.0f), "[INFO]");
        ImGui.SameLine();
        ImGui.Text("Backend initialisé");

        ImGui.TextColored(new System.Numerics.Vector4(0.5f, 0.8f, 0.5f, 1.0f), "[INFO]");
        ImGui.SameLine();
        ImGui.Text("Frontend prêt");

        ImGui.Separator();

        ImGui.TextColored(new System.Numerics.Vector4(0.4f, 0.7f, 1.0f, 1.0f), "[DEBUG]");
        ImGui.SameLine();
        ImGui.Text("Toutes les fenêtres peuvent être dockées librement");

        ImGui.End();
        IsVisible = isVisible;
    }
}
