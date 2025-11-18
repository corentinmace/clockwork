using Clockwork.Core;
using ImGuiNET;

namespace Clockwork.UI.Views;

/// <summary>
/// Vue des analytiques.
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

        ImGui.Begin("Analytiques", ref Unsafe.AsRef(in IsVisible));

        ImGui.Text("Analyse des données");
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.Text("Métriques clés:");
        ImGui.Spacing();

        // Graphique simple (simulé avec des barres de progression)
        ImGui.Text("Utilisation CPU:");
        ImGui.ProgressBar(0.45f, new System.Numerics.Vector2(-1, 0), "45%");

        ImGui.Text("Utilisation Mémoire:");
        ImGui.ProgressBar(0.67f, new System.Numerics.Vector2(-1, 0), "67%");

        ImGui.Text("Stockage:");
        ImGui.ProgressBar(0.23f, new System.Numerics.Vector2(-1, 0), "23%");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.Text("Tendances:");
        ImGui.BulletText("Augmentation de 15% de l'utilisation");
        ImGui.BulletText("Performance stable sur 7 jours");
        ImGui.BulletText("3 alertes cette semaine");

        ImGui.End();
    }
}
