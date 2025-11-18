using Clockwork.Core;
using ImGuiNET;

namespace Clockwork.UI.Views;

/// <summary>
/// Properties view.
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

        bool isVisible = IsVisible;
        ImGui.Begin("Properties", ref isVisible);

        ImGui.Text("Properties Window");
        ImGui.Separator();

        ImGui.Text("Name:");
        ImGui.SameLine();
        ImGui.InputText("##name", ref _name, 100);

        ImGui.Text("Type:");
        ImGui.SameLine();
        ImGui.TextColored(new System.Numerics.Vector4(0.5f, 0.5f, 0.5f, 1.0f), "Application");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Details"))
        {
            ImGui.BulletText("Version: 1.0.0");
            ImGui.BulletText("Framework: .NET 8");
            ImGui.BulletText("UI: ImGui.NET 1.90.5.1");
        }

        ImGui.End();
        IsVisible = isVisible;
    }
}
