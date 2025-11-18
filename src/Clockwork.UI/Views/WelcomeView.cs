using Clockwork.Core;
using ImGuiNET;

namespace Clockwork.UI.Views;

/// <summary>
/// Welcome view.
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
        ImGui.Begin("Welcome", ref isVisible);

        ImGui.Text("Welcome to Clockwork!");
        ImGui.Spacing();

        ImGui.TextColored(new System.Numerics.Vector4(0.4f, 0.7f, 1.0f, 1.0f), "Architecture:");
        ImGui.BulletText(".NET 8");
        ImGui.BulletText("ImGui.NET for interface");
        ImGui.BulletText("OpenTK for OpenGL");
        ImGui.BulletText("Frontend/Backend separation");
        ImGui.BulletText("Fullscreen docking enabled");
        ImGui.Spacing();

        ImGui.Separator();
        ImGui.Spacing();

        ImGui.Text($"FPS: {ImGui.GetIO().Framerate:F1}");
        ImGui.Text($"Frame Time: {1000.0f / ImGui.GetIO().Framerate:F2} ms");
        ImGui.Spacing();

        ImGui.TextWrapped("You can move and dock all windows anywhere in the interface.");

        ImGui.End();
        IsVisible = isVisible;
    }
}
