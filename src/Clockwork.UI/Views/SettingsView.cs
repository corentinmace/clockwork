using Clockwork.Core;
using ImGuiNET;

namespace Clockwork.UI.Views;

/// <summary>
/// Vue des paramètres.
/// </summary>
public class SettingsView : IView
{
    private readonly ApplicationContext _appContext;
    private bool _autoSave = true;
    private int _interval = 5;
    private int _currentTheme = 0;
    private bool _debugMode = false;
    private bool _showFps = true;
    private readonly string[] _themes = { "Sombre", "Clair", "Système" };

    public bool IsVisible { get; set; } = false;

    public SettingsView(ApplicationContext appContext)
    {
        _appContext = appContext;
    }

    public void Draw()
    {
        if (!IsVisible) return;

        ImGui.Begin("Paramètres", ref Unsafe.AsRef(in IsVisible));

        ImGui.Text("Paramètres de l'application");
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Général", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Checkbox("Sauvegarde automatique", ref _autoSave);
            ImGui.SliderInt("Intervalle (min)", ref _interval, 1, 30);
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Apparence"))
        {
            ImGui.Text("Thème:");
            ImGui.Combo("##theme", ref _currentTheme, _themes, _themes.Length);
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Avancé"))
        {
            ImGui.Checkbox("Mode debug", ref _debugMode);
            ImGui.Checkbox("Afficher les FPS", ref _showFps);
        }

        ImGui.End();
    }
}
