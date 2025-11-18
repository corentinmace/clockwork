using Clockwork.Core;
using ImGuiNET;

namespace Clockwork.UI.Views;

/// <summary>
/// Reports view.
/// </summary>
public class ReportsView : IView
{
    private readonly ApplicationContext _appContext;
    private readonly string[] _reportTypes =
    {
        "Monthly Report - January 2024",
        "Monthly Report - February 2024",
        "Annual Report - 2023",
        "Custom Report - Q4 2023"
    };

    public bool IsVisible { get; set; } = false;

    public ReportsView(ApplicationContext appContext)
    {
        _appContext = appContext;
    }

    public void Draw()
    {
        if (!IsVisible) return;

        bool isVisible = IsVisible;
        ImGui.Begin("Reports", ref isVisible);

        ImGui.Text("Report Generation");
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.Button("Generate Monthly Report"))
        {
            // TODO: Generation action
        }

        ImGui.SameLine();

        if (ImGui.Button("Generate Annual Report"))
        {
            // TODO: Generation action
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.Text("Available Reports:");

        ImGui.BeginChild("ReportsList");

        foreach (var report in _reportTypes)
        {
            if (ImGui.Selectable(report))
            {
                // TODO: Open report
            }
        }

        ImGui.EndChild();

        ImGui.End();
        IsVisible = isVisible;
    }
}
