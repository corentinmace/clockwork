using Clockwork.Core;
using ImGuiNET;

namespace Clockwork.UI.Views;

/// <summary>
/// Vue d'accueil.
/// </summary>
public class WelcomeView : IView
{
    private readonly ApplicationContext _appContext;

    public bool IsVisible { get; set; } = false;

    public WelcomeView(ApplicationContext appContext)
    {
        _appContext = appContext;
    }

    public void Draw()
    {
        if (!IsVisible) return;

        bool isVisible = IsVisible;
        ImGui.Begin("Bienvenue", ref isVisible);

        ImGui.Text("Bienvenue dans Clockwork!");
        ImGui.Spacing();

        ImGui.TextColored(new System.Numerics.Vector4(0.4f, 0.7f, 1.0f, 1.0f), "Architecture:");
        ImGui.BulletText(".NET 8");
        ImGui.BulletText("ImGui.NET pour l'interface");
        ImGui.BulletText("OpenTK pour OpenGL");
        ImGui.BulletText("Séparation Frontend/Backend");
        ImGui.BulletText("Docking fullscreen activé");
        ImGui.Spacing();

        ImGui.Separator();
        ImGui.Spacing();

        ImGui.Text($"FPS: {ImGui.GetIO().Framerate:F1}");
        ImGui.Text($"Frame Time: {1000.0f / ImGui.GetIO().Framerate:F2} ms");
        ImGui.Spacing();

        ImGui.TextWrapped("Vous pouvez déplacer et docker toutes les fenêtres où vous voulez dans l'interface.");

        ImGui.End();
        IsVisible = isVisible;
    }
}
