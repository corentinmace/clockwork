using Clockwork.Core;
using ImGuiNET;

namespace Clockwork.UI.Views;

/// <summary>
/// Analytics view.
/// </summary>
public class AnalyticsView : IView
{
    private readonly ApplicationContext _appContext;

    public bool IsVisible { get; set; } = false;

    public AnalyticsView(ApplicationContext appContext)
    {
        _appContext = appContext;
    }

    public void Draw()
    {
        if (!IsVisible) return;

        bool isVisible = IsVisible;
        ImGui.Begin("Analytics", ref isVisible);

        ImGui.Text("Data Analysis");
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.Text("Key Metrics:");
        ImGui.Spacing();

        // Simple chart (simulated with progress bars)
        ImGui.Text("CPU Usage:");
        ImGui.ProgressBar(0.45f, new System.Numerics.Vector2(-1, 0), "45%");

        ImGui.Text("Memory Usage:");
        ImGui.ProgressBar(0.67f, new System.Numerics.Vector2(-1, 0), "67%");

        ImGui.Text("Storage:");
        ImGui.ProgressBar(0.23f, new System.Numerics.Vector2(-1, 0), "23%");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.Text("Trends:");
        ImGui.BulletText("15% increase in usage");
        ImGui.BulletText("Stable performance over 7 days");
        ImGui.BulletText("3 alerts this week");

        ImGui.End();
        IsVisible = isVisible;
    }
}
