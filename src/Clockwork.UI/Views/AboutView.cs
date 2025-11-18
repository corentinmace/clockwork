using Clockwork.Core;
using ImGuiNET;

namespace Clockwork.UI.Views;

/// <summary>
/// Vue à propos.
/// </summary>
public class AboutView : IView
{
    private readonly ApplicationContext _appContext;

    public bool IsVisible { get; set; } = false;

    public AboutView(ApplicationContext appContext)
    {
        _appContext = appContext;
    }

    public void Draw()
    {
        if (!IsVisible) return;

        bool isVisible = IsVisible;
        ImGui.Begin("À propos", ref isVisible, ImGuiWindowFlags.AlwaysAutoResize);

        ImGui.Text("Clockwork");
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.Text("Version: 1.0.0");
        ImGui.Text("Framework: .NET 8");
        ImGui.Text("UI Library: ImGui.NET 1.90.5.1");
        ImGui.Text("Graphics: OpenTK 4.8.2 (OpenGL)");
        ImGui.Spacing();

        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.Button("Fermer"))
        {
            isVisible = false;
        }

        ImGui.End();
        IsVisible = isVisible;
    }
}
