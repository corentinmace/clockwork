using Clockwork.Core;
using Clockwork.Core.Services;
using ImGuiNET;

namespace Clockwork.UI.Views;

/// <summary>
/// Data exploration view.
/// </summary>
public class DataViewView : IView
{
    private readonly ApplicationContext _appContext;
    private DataService? _dataService;
    private string _searchText = string.Empty;

    public bool IsVisible { get; set; } = false;

    public DataViewView(ApplicationContext appContext)
    {
        _appContext = appContext;
        _dataService = _appContext.GetService<DataService>();
    }

    public void Draw()
    {
        if (!IsVisible) return;

        bool isVisible = IsVisible;
        ImGui.Begin("Data View", ref isVisible);

        ImGui.Text("Data Exploration");
        ImGui.Separator();
        ImGui.Spacing();

        // Filters
        ImGui.Text("Filters:");
        ImGui.InputText("Search", ref _searchText, 256);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (_dataService != null)
        {
            var dataItems = _dataService.GetDataItems();

            // Data
            ImGui.BeginChild("DataContent", new System.Numerics.Vector2(0, -30), ImGuiChildFlags.Border);

            foreach (var item in dataItems)
            {
                if (!string.IsNullOrEmpty(_searchText) && !item.Name.Contains(_searchText, StringComparison.OrdinalIgnoreCase))
                    continue;

                if (ImGui.TreeNode($"{item.Name}##{item.Id}"))
                {
                    ImGui.Text($"ID: {item.Id}");
                    ImGui.Text($"Timestamp: {item.Timestamp:yyyy-MM-dd HH:mm:ss}");
                    ImGui.Text($"Value: {item.Value}");
                    ImGui.TreePop();
                }
            }

            ImGui.EndChild();

            ImGui.Text($"Total: {dataItems.Count} items");
        }

        ImGui.End();
        IsVisible = isVisible;
    }
}
