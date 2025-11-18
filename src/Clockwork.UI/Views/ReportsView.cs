using Clockwork.Core;
using ImGuiNET;

namespace Clockwork.UI.Views;

/// <summary>
/// Vue des rapports.
/// </summary>
public class ReportsView : IView
{
    private readonly ApplicationContext _appContext;
    private readonly string[] _reportTypes =
    {
        "Rapport mensuel - Janvier 2024",
        "Rapport mensuel - Février 2024",
        "Rapport annuel - 2023",
        "Rapport personnalisé - Q4 2023"
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
        ImGui.Begin("Rapports", ref isVisible);

        ImGui.Text("Génération de rapports");
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.Button("Générer rapport mensuel"))
        {
            // TODO: Action de génération
        }

        ImGui.SameLine();

        if (ImGui.Button("Générer rapport annuel"))
        {
            // TODO: Action de génération
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.Text("Rapports disponibles:");

        ImGui.BeginChild("ReportsList");

        foreach (var report in _reportTypes)
        {
            if (ImGui.Selectable(report))
            {
                // TODO: Ouvrir le rapport
            }
        }

        ImGui.EndChild();

        ImGui.End();
        IsVisible = isVisible;
    }
}
