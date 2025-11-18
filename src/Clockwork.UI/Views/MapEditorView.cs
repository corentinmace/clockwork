using Clockwork.Core;
using Clockwork.Core.Models;
using Clockwork.Core.Services;
using ImGuiNET;

namespace Clockwork.UI.Views;

/// <summary>
/// View for editing Pokémon map files (32x32 collision grid).
/// </summary>
public class MapEditorView : IView
{
    private readonly ApplicationContext _appContext;
    private RomService? _romService;
    private MapService? _mapService;

    private MapFile? _currentMap;
    private string _statusMessage = string.Empty;
    private System.Numerics.Vector4 _statusColor = new(1.0f, 1.0f, 1.0f, 1.0f);
    private string _searchFilter = string.Empty;
    private string? _lastLoadedRomPath;

    // Grid painting state
    private bool _paintWalkable = true;

    public bool IsVisible { get; set; } = false;

    public MapEditorView(ApplicationContext appContext)
    {
        _appContext = appContext;
        _romService = _appContext.GetService<RomService>();
        _mapService = _appContext.GetService<MapService>();
    }

    public void Draw()
    {
        if (!IsVisible) return;

        bool isVisible = IsVisible;
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(1000, 700), ImGuiCond.FirstUseEver);
        ImGui.Begin("Map Editor", ref isVisible);

        // Check if ROM is loaded
        bool romLoaded = _romService?.CurrentRom != null;
        bool mapsLoaded = _mapService?.IsLoaded ?? false;

        if (!romLoaded)
        {
            ImGui.TextColored(new System.Numerics.Vector4(1.0f, 0.7f, 0.4f, 1.0f),
                "No ROM loaded. Please load a ROM first (ROM > Open ROM...)");
            _lastLoadedRomPath = null;
        }
        else
        {
            // Auto-load maps when ROM changes
            string currentRomPath = _romService!.CurrentRom!.RomPath;
            if (_lastLoadedRomPath != currentRomPath)
            {
                LoadMapsFromRom();
                _lastLoadedRomPath = currentRomPath;
            }

            if (mapsLoaded)
            {
                // Two-column layout: map list on left, editor on right
                float availableHeight = ImGui.GetContentRegionAvail().Y;

                if (ImGui.BeginTable("MapEditorTable", 2, ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInnerV))
                {
                    ImGui.TableSetupColumn("Maps", ImGuiTableColumnFlags.WidthFixed, 250);
                    ImGui.TableSetupColumn("Editor", ImGuiTableColumnFlags.WidthStretch);

                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);

                    // Left column: Map list
                    DrawMapList(availableHeight - 40);

                    ImGui.TableSetColumnIndex(1);

                    // Right column: Map editor
                    if (_currentMap != null)
                    {
                        DrawMapEditor();
                    }
                    else
                    {
                        ImGui.TextColored(new System.Numerics.Vector4(0.7f, 0.7f, 0.7f, 1.0f),
                            "Select a map from the list to edit.");
                    }

                    ImGui.EndTable();
                }
            }
            else
            {
                ImGui.TextColored(new System.Numerics.Vector4(1.0f, 0.7f, 0.4f, 1.0f),
                    "Loading maps...");
            }
        }

        // Status message
        if (!string.IsNullOrEmpty(_statusMessage))
        {
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            ImGui.TextColored(_statusColor, _statusMessage);
        }

        ImGui.End();
        IsVisible = isVisible;
    }

    private void LoadMapsFromRom()
    {
        if (_mapService == null) return;

        if (_mapService.LoadMapsFromRom())
        {
            _statusMessage = $"Loaded {_mapService.Maps.Count} maps from ROM";
            _statusColor = new System.Numerics.Vector4(0.5f, 0.8f, 0.5f, 1.0f);
        }
        else
        {
            _statusMessage = "Error: Failed to load maps from ROM";
            _statusColor = new System.Numerics.Vector4(1.0f, 0.4f, 0.4f, 1.0f);
        }
    }

    private void DrawMapList(float availableHeight)
    {
        if (_mapService == null) return;

        ImGui.TextColored(new System.Numerics.Vector4(0.4f, 0.7f, 1.0f, 1.0f), "Maps");
        ImGui.Spacing();

        // Search filter
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##search", "Search Map ID...", ref _searchFilter, 256);
        ImGui.Spacing();

        // Calculate list height
        float listHeight = availableHeight - 60;

        // Map list with scrolling
        ImGui.BeginChild("MapList", new System.Numerics.Vector2(-1, listHeight), ImGuiChildFlags.Border);

        var maps = _mapService.Maps;
        foreach (var map in maps)
        {
            // Apply search filter
            if (!string.IsNullOrWhiteSpace(_searchFilter))
            {
                bool matchesID = map.MapID.ToString().Contains(_searchFilter, StringComparison.OrdinalIgnoreCase);
                if (!matchesID)
                    continue;
            }

            bool isSelected = _currentMap != null && _currentMap.MapID == map.MapID;
            string label = $"Map {map.MapID:D3} ({map.Buildings.Count} buildings)";

            if (ImGui.Selectable(label, isSelected))
            {
                _currentMap = map;
                _statusMessage = $"Selected map {map.MapID}";
                _statusColor = new System.Numerics.Vector4(0.4f, 0.7f, 1.0f, 1.0f);
            }
        }

        ImGui.EndChild();
    }

    private void DrawMapEditor()
    {
        if (_currentMap == null) return;

        // Scrollable editor area
        ImGui.BeginChild("MapEditorScroll", new System.Numerics.Vector2(0, 0), ImGuiChildFlags.None);

        // Map info
        ImGui.TextColored(new System.Numerics.Vector4(0.4f, 0.7f, 1.0f, 1.0f), $"Map {_currentMap.MapID}");
        ImGui.Separator();
        ImGui.Spacing();

        // Tools section
        if (ImGui.CollapsingHeader("Collision Tools", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Text("Paint Mode:");
            if (ImGui.RadioButton("Walkable (Green)", _paintWalkable))
            {
                _paintWalkable = true;
            }
            ImGui.SameLine();
            if (ImGui.RadioButton("Blocked (Red)", !_paintWalkable))
            {
                _paintWalkable = false;
            }
        }

        ImGui.Spacing();

        // Collision grid section
        if (ImGui.CollapsingHeader("Collision Grid (32x32)", ImGuiTreeNodeFlags.DefaultOpen))
        {
            DrawCollisionGrid();
        }

        ImGui.Spacing();

        // Buildings section
        if (ImGui.CollapsingHeader("Buildings"))
        {
            ImGui.Text($"Building count: {_currentMap.Buildings.Count}");

            ImGui.BeginChild("BuildingList", new System.Numerics.Vector2(0, 200), ImGuiChildFlags.Border);

            for (int i = 0; i < _currentMap.Buildings.Count; i++)
            {
                var building = _currentMap.Buildings[i];
                ImGui.Text($"Building {i}: Model {building.ModelID} at ({building.FullX:F2}, {building.FullY:F2}, {building.FullZ:F2})");
            }

            ImGui.EndChild();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Save button
        ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.25f, 0.55f, 0.25f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new System.Numerics.Vector4(0.35f, 0.65f, 0.35f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new System.Numerics.Vector4(0.20f, 0.50f, 0.20f, 1.0f));

        if (ImGui.Button("Save Map", new System.Numerics.Vector2(-1, 40)))
        {
            if (_mapService != null && _mapService.SaveMap(_currentMap))
            {
                _statusMessage = $"Map {_currentMap.MapID} saved successfully!";
                _statusColor = new System.Numerics.Vector4(0.5f, 0.8f, 0.5f, 1.0f);
            }
            else
            {
                _statusMessage = "Error: Failed to save map";
                _statusColor = new System.Numerics.Vector4(1.0f, 0.4f, 0.4f, 1.0f);
            }
        }

        ImGui.PopStyleColor(3);

        ImGui.EndChild(); // End MapEditorScroll
    }

    private void DrawCollisionGrid()
    {
        if (_currentMap == null) return;

        const float cellSize = 16f;
        const int gridSize = MapFile.MAP_SIZE;

        var drawList = ImGui.GetWindowDrawList();
        var cursorPos = ImGui.GetCursorScreenPos();

        // Calculate grid bounds
        float gridWidth = gridSize * cellSize;
        float gridHeight = gridSize * cellSize;

        // Reserve space for grid
        ImGui.Dummy(new System.Numerics.Vector2(gridWidth, gridHeight));

        // Check if mouse is over grid
        var mousePos = ImGui.GetMousePos();
        bool isHovered = mousePos.X >= cursorPos.X && mousePos.X < cursorPos.X + gridWidth &&
                         mousePos.Y >= cursorPos.Y && mousePos.Y < cursorPos.Y + gridHeight;

        // Handle painting
        if (isHovered && ImGui.IsMouseDown(ImGuiMouseButton.Left))
        {
            int gridX = (int)((mousePos.X - cursorPos.X) / cellSize);
            int gridY = (int)((mousePos.Y - cursorPos.Y) / cellSize);

            if (gridX >= 0 && gridX < gridSize && gridY >= 0 && gridY < gridSize)
            {
                _currentMap.SetWalkable(gridX, gridY, _paintWalkable);
            }
        }

        // Draw grid cells
        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                var cellMin = new System.Numerics.Vector2(
                    cursorPos.X + x * cellSize,
                    cursorPos.Y + y * cellSize);
                var cellMax = new System.Numerics.Vector2(
                    cellMin.X + cellSize,
                    cellMin.Y + cellSize);

                // Color based on walkability
                bool isWalkable = _currentMap.IsWalkable(x, y);
                uint cellColor = isWalkable
                    ? ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.2f, 0.8f, 0.2f, 0.6f))
                    : ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.8f, 0.2f, 0.2f, 0.6f));

                // Draw filled cell
                drawList.AddRectFilled(cellMin, cellMax, cellColor);

                // Draw cell border
                uint borderColor = ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.3f, 0.3f, 0.3f, 1.0f));
                drawList.AddRect(cellMin, cellMax, borderColor);
            }
        }

        // Highlight hovered cell
        if (isHovered)
        {
            int gridX = (int)((mousePos.X - cursorPos.X) / cellSize);
            int gridY = (int)((mousePos.Y - cursorPos.Y) / cellSize);

            if (gridX >= 0 && gridX < gridSize && gridY >= 0 && gridY < gridSize)
            {
                var cellMin = new System.Numerics.Vector2(
                    cursorPos.X + gridX * cellSize,
                    cursorPos.Y + gridY * cellSize);
                var cellMax = new System.Numerics.Vector2(
                    cellMin.X + cellSize,
                    cellMin.Y + cellSize);

                uint highlightColor = ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(1.0f, 1.0f, 1.0f, 0.5f));
                drawList.AddRect(cellMin, cellMax, highlightColor, 0, ImDrawFlags.None, 2.0f);
            }
        }
    }
}
