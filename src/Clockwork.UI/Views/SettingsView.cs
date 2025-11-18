using Clockwork.Core;
using ImGuiNET;

namespace Clockwork.UI.Views;

/// <summary>
/// Settings view.
/// </summary>
public class SettingsView : IView
{
    private readonly ApplicationContext _appContext;
    private bool _autoSave = true;
    private int _interval = 5;
    private int _currentTheme = 0;
    private bool _debugMode = false;
    private bool _showFps = true;
    private readonly string[] _themes = { "Dark", "Light", "System" };

    public bool IsVisible { get; set; } = false;

    public SettingsView(ApplicationContext appContext)
    {
        _appContext = appContext;
    }

    public void Draw()
    {
        if (!IsVisible) return;

        bool isVisible = IsVisible;
        ImGui.Begin("Settings", ref isVisible);

        ImGui.Text("Application Settings");
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.CollapsingHeader("General", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Checkbox("Auto-save", ref _autoSave);
            ImGui.SliderInt("Interval (min)", ref _interval, 1, 30);
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Appearance"))
        {
            ImGui.Text("Theme:");
            ImGui.Combo("##theme", ref _currentTheme, _themes, _themes.Length);
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Advanced"))
        {
            ImGui.Checkbox("Debug mode", ref _debugMode);
            ImGui.Checkbox("Show FPS", ref _showFps);
        }

        ImGui.End();
        IsVisible = isVisible;
    }
}
