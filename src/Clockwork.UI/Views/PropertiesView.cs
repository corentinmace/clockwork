using Clockwork.Core;
using ImGuiNET;

namespace Clockwork.UI.Views;

/// <summary>
/// Vue de propriétés.
/// </summary>
public class PropertiesView : IView
{
    private readonly ApplicationContext _appContext;
    private string _name = "Clockwork";

    public bool IsVisible { get; set; } = false;

    public PropertiesView(ApplicationContext appContext)
    {
        _appContext = appContext;
    }

    public void Draw()
    {
        if (!IsVisible) return;

        ImGui.Begin("Propriétés", ref Unsafe.AsRef(in IsVisible));

        ImGui.Text("Fenêtre de propriétés");
        ImGui.Separator();

        ImGui.Text("Nom:");
        ImGui.SameLine();
        ImGui.InputText("##name", ref _name, 100);

        ImGui.Text("Type:");
        ImGui.SameLine();
        ImGui.TextColored(new System.Numerics.Vector4(0.5f, 0.5f, 0.5f, 1.0f), "Application");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Détails"))
        {
            ImGui.BulletText("Version: 1.0.0");
            ImGui.BulletText("Framework: .NET 8");
            ImGui.BulletText("UI: ImGui.NET 1.90.5.1");
        }

        ImGui.End();
    }
}
