using Clockwork.Core;
using ImGuiNET;

namespace Clockwork.UI.Views;

/// <summary>
/// Hierarchy view.
/// </summary>
public class HierarchyView : IView
{
    private readonly ApplicationContext _appContext;

    public bool IsVisible { get; set; } = false;

    public HierarchyView(ApplicationContext appContext)
    {
        _appContext = appContext;
    }

    public void Draw()
    {
        if (!IsVisible) return;

        bool isVisible = IsVisible;
        ImGui.Begin("Hierarchy", ref isVisible);

        ImGui.Text("Application Structure:");
        ImGui.Spacing();

        if (ImGui.TreeNode("Clockwork.Core (Backend)"))
        {
            if (ImGui.TreeNode("ApplicationContext"))
            {
                ImGui.BulletText("DashboardService");
                ImGui.BulletText("UserService");
                ImGui.BulletText("DataService");
                ImGui.BulletText("ExampleService");
                ImGui.TreePop();
            }
            ImGui.TreePop();
        }

        if (ImGui.TreeNode("Clockwork.UI (Frontend)"))
        {
            ImGui.BulletText("MainWindow");
            ImGui.BulletText("ImGuiController");
            if (ImGui.TreeNode("Views"))
            {
                ImGui.BulletText("DashboardView");
                ImGui.BulletText("UserManagementView");
                ImGui.BulletText("DataViewView");
                ImGui.BulletText("WelcomeView");
                ImGui.BulletText("PropertiesView");
                ImGui.BulletText("ConsoleView");
                ImGui.BulletText("HierarchyView");
                ImGui.BulletText("AboutView");
                ImGui.BulletText("SettingsView");
                ImGui.BulletText("ReportsView");
                ImGui.BulletText("AnalyticsView");
                ImGui.TreePop();
            }
            ImGui.TreePop();
        }

        ImGui.End();
        IsVisible = isVisible;
    }
}
