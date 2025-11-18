using Clockwork.Core;
using Clockwork.Core.Services;
using ImGuiNET;

namespace Clockwork.UI.Views;

/// <summary>
/// Vue du tableau de bord.
/// </summary>
public class DashboardView : IView
{
    private readonly ApplicationContext _appContext;
    private DashboardService? _dashboardService;

    public bool IsVisible { get; set; } = true;

    public DashboardView(ApplicationContext appContext)
    {
        _appContext = appContext;
        _dashboardService = _appContext.GetService<DashboardService>();
    }

    public void Draw()
    {
        if (!IsVisible) return;

        ImGui.Begin("Tableau de bord", ref Unsafe.AsRef(in IsVisible));

        ImGui.TextColored(new System.Numerics.Vector4(0.4f, 0.7f, 1.0f, 1.0f), "Tableau de bord principal");
        ImGui.Separator();
        ImGui.Spacing();

        if (_dashboardService != null)
        {
            var stats = _dashboardService.GetStats();

            // Statistiques
            ImGui.Text("Statistiques rapides:");
            ImGui.Columns(3, "stats", false);

            ImGui.BeginChild("stat1", new System.Numerics.Vector2(0, 80), ImGuiChildFlags.Border);
            ImGui.TextColored(new System.Numerics.Vector4(0.5f, 0.8f, 0.5f, 1.0f), "Utilisateurs");
            ImGui.Text(stats.TotalUsers.ToString("N0"));
            ImGui.EndChild();

            ImGui.NextColumn();

            ImGui.BeginChild("stat2", new System.Numerics.Vector2(0, 80), ImGuiChildFlags.Border);
            ImGui.TextColored(new System.Numerics.Vector4(0.4f, 0.7f, 1.0f, 1.0f), "Projets");
            ImGui.Text(stats.TotalProjects.ToString());
            ImGui.EndChild();

            ImGui.NextColumn();

            ImGui.BeginChild("stat3", new System.Numerics.Vector2(0, 80), ImGuiChildFlags.Border);
            ImGui.TextColored(new System.Numerics.Vector4(1.0f, 0.7f, 0.4f, 1.0f), "Tâches");
            ImGui.Text(stats.TotalTasks.ToString("N0"));
            ImGui.EndChild();

            ImGui.Columns(1);
            ImGui.Spacing();
        }

        ImGui.Separator();
        ImGui.Spacing();

        ImGui.Text($"FPS: {ImGui.GetIO().Framerate:F1}");
        ImGui.Text($"Frame Time: {1000.0f / ImGui.GetIO().Framerate:F2} ms");

        ImGui.End();
    }
}
