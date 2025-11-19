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
    private PaintMode _paintMode = PaintMode.Collision;
    private byte _selectedCollisionType = 0x00; // Walkable by default
    private byte _selectedTerrainType = 0x00;   // Default by default

    // Building editing state
    private int _selectedBuildingIndex = -1;
    private bool _showBuildingEditor = false;
    private Building? _editingBuilding = null;

    private enum PaintMode
    {
        Collision,
        Terrain
    }

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
        ImGui.BeginChild("MapList", new System.Numerics.Vector2(-1, listHeight), true);

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
        ImGui.BeginChild("MapEditorScroll", new System.Numerics.Vector2(0, 0), false);

        // Map info
        ImGui.TextColored(new System.Numerics.Vector4(0.4f, 0.7f, 1.0f, 1.0f), $"Map {_currentMap.MapID}");
        ImGui.Separator();
        ImGui.Spacing();

        // Paint mode selection
        if (ImGui.CollapsingHeader("Paint Mode", ImGuiTreeNodeFlags.DefaultOpen))
        {
            if (ImGui.RadioButton("Paint Collision Types", _paintMode == PaintMode.Collision))
            {
                _paintMode = PaintMode.Collision;
            }
            ImGui.SameLine();
            if (ImGui.RadioButton("Paint Terrain Types", _paintMode == PaintMode.Terrain))
            {
                _paintMode = PaintMode.Terrain;
            }
        }

        ImGui.Spacing();

        // Collision type selection
        if (_paintMode == PaintMode.Collision)
        {
            if (ImGui.CollapsingHeader("Collision Type Palette", ImGuiTreeNodeFlags.DefaultOpen))
            {
                DrawCollisionTypePalette();
            }
        }
        else
        {
            if (ImGui.CollapsingHeader("Terrain Type Palette", ImGuiTreeNodeFlags.DefaultOpen))
            {
                DrawTerrainTypePalette();
            }
        }

        ImGui.Spacing();

        // Grid display section
        if (ImGui.CollapsingHeader("Map Grid (32x32)", ImGuiTreeNodeFlags.DefaultOpen))
        {
            if (_paintMode == PaintMode.Collision)
            {
                DrawCollisionGrid();
            }
            else
            {
                DrawTerrainGrid();
            }
        }

        ImGui.Spacing();

        // Buildings section
        if (ImGui.CollapsingHeader("Buildings"))
        {
            ImGui.Text($"Building count: {_currentMap.Buildings.Count}");
            ImGui.Spacing();

            // Add building button
            if (ImGui.Button("Add New Building", new System.Numerics.Vector2(-1, 30)))
            {
                var newBuilding = new Building
                {
                    ModelID = 0,
                    XPosition = 0,
                    YPosition = 0,
                    ZPosition = 0,
                    XFraction = 0,
                    YFraction = 0,
                    ZFraction = 0,
                    XRotation = 0,
                    YRotation = 0,
                    ZRotation = 0,
                    Width = 1,
                    Height = 1,
                    Length = 1
                };
                _currentMap.Buildings.Add(newBuilding);
                _selectedBuildingIndex = _currentMap.Buildings.Count - 1;
                _showBuildingEditor = true;
                _editingBuilding = newBuilding;
            }

            ImGui.Spacing();

            // Building list
            ImGui.BeginChild("BuildingList", new System.Numerics.Vector2(0, 200), true);

            for (int i = 0; i < _currentMap.Buildings.Count; i++)
            {
                var building = _currentMap.Buildings[i];
                bool isSelected = _selectedBuildingIndex == i;

                string label = $"Building {i}: Model {building.ModelID} at ({building.FullX:F2}, {building.FullY:F2}, {building.FullZ:F2})";

                if (ImGui.Selectable(label, isSelected))
                {
                    _selectedBuildingIndex = i;
                    _showBuildingEditor = true;
                    _editingBuilding = building;
                }
            }

            ImGui.EndChild();

            // Building editor
            if (_showBuildingEditor && _editingBuilding != null)
            {
                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                DrawBuildingEditor();
            }
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
                _currentMap.SetCollision(gridX, gridY, _selectedCollisionType);
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

                // Get collision type color
                byte collisionValue = _currentMap.GetCollision(x, y);
                var (r, g, b, a) = CollisionTypeHelper.GetColor(collisionValue);
                uint cellColor = ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(r / 255f, g / 255f, b / 255f, a / 255f));

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

    private void DrawCollisionTypePalette()
    {
        ImGui.Text("Select collision type to paint:");
        ImGui.Spacing();

        var allTypes = CollisionTypeHelper.GetAllTypes();

        for (int i = 0; i < allTypes.Length; i++)
        {
            byte typeValue = allTypes[i];
            string typeName = CollisionTypeHelper.GetName(typeValue);
            var (r, g, b, a) = CollisionTypeHelper.GetColor(typeValue);

            bool isSelected = _selectedCollisionType == typeValue;

            // Color indicator box
            var drawList = ImGui.GetWindowDrawList();
            var cursorPos = ImGui.GetCursorScreenPos();
            var boxSize = 20f;

            var boxMin = cursorPos;
            var boxMax = new System.Numerics.Vector2(cursorPos.X + boxSize, cursorPos.Y + boxSize);

            uint cellColor = ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(r / 255f, g / 255f, b / 255f, a / 255f));
            drawList.AddRectFilled(boxMin, boxMax, cellColor);

            uint borderColor = isSelected
                ? ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(1.0f, 1.0f, 0.0f, 1.0f)) // Yellow for selected
                : ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.3f, 0.3f, 0.3f, 1.0f));
            drawList.AddRect(boxMin, boxMax, borderColor, 0, ImDrawFlags.None, isSelected ? 2.0f : 1.0f);

            ImGui.Dummy(new System.Numerics.Vector2(boxSize, boxSize));
            ImGui.SameLine();

            // Selectable label
            if (ImGui.Selectable($"{typeName} (0x{typeValue:X2})", isSelected))
            {
                _selectedCollisionType = typeValue;
            }
        }
    }

    private void DrawTerrainTypePalette()
    {
        ImGui.Text("Select terrain type to paint:");
        ImGui.Spacing();

        // Common types
        ImGui.TextColored(new System.Numerics.Vector4(0.7f, 0.7f, 1.0f, 1.0f), "Common Types:");
        var commonTypes = TerrainTypeHelper.GetCommonTypes();

        for (int i = 0; i < commonTypes.Length; i++)
        {
            byte typeValue = commonTypes[i];
            DrawTerrainTypePaletteItem(typeValue);
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Custom input
        ImGui.TextColored(new System.Numerics.Vector4(0.7f, 0.7f, 1.0f, 1.0f), "Custom Type:");

        int customValue = _selectedTerrainType;
        ImGui.SetNextItemWidth(100);
        if (ImGui.InputInt("Hex Value (0x00-0xFF)", ref customValue))
        {
            if (customValue >= 0 && customValue <= 255)
            {
                _selectedTerrainType = (byte)customValue;
            }
        }

        ImGui.SameLine();
        ImGui.Text($"= 0x{_selectedTerrainType:X2}");

        // Show preview of custom type
        if (_selectedTerrainType != 0x00)
        {
            DrawTerrainTypePaletteItem(_selectedTerrainType);
        }
    }

    private void DrawTerrainTypePaletteItem(byte typeValue)
    {
        string typeName = TerrainTypeHelper.GetName(typeValue);
        var (r, g, b, a) = TerrainTypeHelper.GetColor(typeValue);

        bool isSelected = _selectedTerrainType == typeValue;

        // Color indicator box
        var drawList = ImGui.GetWindowDrawList();
        var cursorPos = ImGui.GetCursorScreenPos();
        var boxSize = 20f;

        var boxMin = cursorPos;
        var boxMax = new System.Numerics.Vector2(cursorPos.X + boxSize, cursorPos.Y + boxSize);

        uint cellColor = ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(r / 255f, g / 255f, b / 255f, a / 255f));
        drawList.AddRectFilled(boxMin, boxMax, cellColor);

        uint borderColor = isSelected
            ? ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(1.0f, 1.0f, 0.0f, 1.0f)) // Yellow for selected
            : ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.3f, 0.3f, 0.3f, 1.0f));
        drawList.AddRect(boxMin, boxMax, borderColor, 0, ImDrawFlags.None, isSelected ? 2.0f : 1.0f);

        ImGui.Dummy(new System.Numerics.Vector2(boxSize, boxSize));
        ImGui.SameLine();

        // Selectable label
        if (ImGui.Selectable($"{typeName} (0x{typeValue:X2})", isSelected))
        {
            _selectedTerrainType = typeValue;
        }
    }

    private void DrawTerrainGrid()
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
                _currentMap.SetTerrainType(gridX, gridY, _selectedTerrainType);
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

                // Get terrain type color
                byte terrainValue = _currentMap.GetTerrainType(x, y);
                var (r, g, b, a) = TerrainTypeHelper.GetColor(terrainValue);
                uint cellColor = ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(r / 255f, g / 255f, b / 255f, a / 255f));

                // Draw filled cell
                drawList.AddRectFilled(cellMin, cellMax, cellColor);

                // Draw cell border
                uint borderColor = ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0.3f, 0.3f, 0.3f, 1.0f));
                drawList.AddRect(cellMin, cellMax, borderColor);

                // Draw hex value on cell (like LiTRE)
                if (cellSize >= 16)
                {
                    string hexText = $"{terrainValue:X2}";
                    var textPos = new System.Numerics.Vector2(cellMin.X + 2, cellMin.Y + 2);

                    // Draw text with shadow for readability
                    drawList.AddText(new System.Numerics.Vector2(textPos.X + 1, textPos.Y + 1),
                        ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(0, 0, 0, 1)), hexText);
                    drawList.AddText(textPos,
                        ImGui.ColorConvertFloat4ToU32(new System.Numerics.Vector4(1, 1, 1, 1)), hexText);
                }
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

    private void DrawBuildingEditor()
    {
        if (_editingBuilding == null || _selectedBuildingIndex < 0)
        {
            _showBuildingEditor = false;
            return;
        }

        ImGui.TextColored(new System.Numerics.Vector4(0.4f, 0.7f, 1.0f, 1.0f), $"Editing Building {_selectedBuildingIndex}");
        ImGui.Spacing();

        ImGui.BeginChild("BuildingEditorScroll", new System.Numerics.Vector2(0, 300), true);

        // Model ID
        int modelID = (int)_editingBuilding.ModelID;
        ImGui.SetNextItemWidth(150);
        if (ImGui.InputInt("Model ID", ref modelID))
        {
            if (modelID >= 0)
                _editingBuilding.ModelID = (uint)modelID;
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Position section
        ImGui.TextColored(new System.Numerics.Vector4(0.7f, 0.7f, 1.0f, 1.0f), "Position:");

        // X Position
        int xPos = _editingBuilding.XPosition;
        int xFrac = _editingBuilding.XFraction;
        ImGui.SetNextItemWidth(100);
        if (ImGui.InputInt("X (Integer)", ref xPos))
            _editingBuilding.XPosition = (short)xPos;
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100);
        if (ImGui.InputInt("X (Fraction/65536)", ref xFrac))
        {
            if (xFrac >= 0 && xFrac <= 65535)
                _editingBuilding.XFraction = (ushort)xFrac;
        }
        ImGui.Text($"  = {_editingBuilding.FullX:F5}");

        // Y Position
        int yPos = _editingBuilding.YPosition;
        int yFrac = _editingBuilding.YFraction;
        ImGui.SetNextItemWidth(100);
        if (ImGui.InputInt("Y (Integer)", ref yPos))
            _editingBuilding.YPosition = (short)yPos;
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100);
        if (ImGui.InputInt("Y (Fraction/65536)", ref yFrac))
        {
            if (yFrac >= 0 && yFrac <= 65535)
                _editingBuilding.YFraction = (ushort)yFrac;
        }
        ImGui.Text($"  = {_editingBuilding.FullY:F5}");

        // Z Position
        int zPos = _editingBuilding.ZPosition;
        int zFrac = _editingBuilding.ZFraction;
        ImGui.SetNextItemWidth(100);
        if (ImGui.InputInt("Z (Integer)", ref zPos))
            _editingBuilding.ZPosition = (short)zPos;
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100);
        if (ImGui.InputInt("Z (Fraction/65536)", ref zFrac))
        {
            if (zFrac >= 0 && zFrac <= 65535)
                _editingBuilding.ZFraction = (ushort)zFrac;
        }
        ImGui.Text($"  = {_editingBuilding.FullZ:F5}");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Rotation section (ushort format: 65536 = 360 degrees)
        ImGui.TextColored(new System.Numerics.Vector4(0.7f, 0.7f, 1.0f, 1.0f), "Rotation (65536 = 360°):");

        int xRot = _editingBuilding.XRotation;
        ImGui.SetNextItemWidth(150);
        if (ImGui.InputInt("X Rotation", ref xRot))
        {
            if (xRot >= 0 && xRot <= 65535)
                _editingBuilding.XRotation = (ushort)xRot;
        }
        ImGui.SameLine();
        ImGui.Text($"= {_editingBuilding.XRotation * 360.0 / 65536.0:F2}°");

        int yRot = _editingBuilding.YRotation;
        ImGui.SetNextItemWidth(150);
        if (ImGui.InputInt("Y Rotation", ref yRot))
        {
            if (yRot >= 0 && yRot <= 65535)
                _editingBuilding.YRotation = (ushort)yRot;
        }
        ImGui.SameLine();
        ImGui.Text($"= {_editingBuilding.YRotation * 360.0 / 65536.0:F2}°");

        int zRot = _editingBuilding.ZRotation;
        ImGui.SetNextItemWidth(150);
        if (ImGui.InputInt("Z Rotation", ref zRot))
        {
            if (zRot >= 0 && zRot <= 65535)
                _editingBuilding.ZRotation = (ushort)zRot;
        }
        ImGui.SameLine();
        ImGui.Text($"= {_editingBuilding.ZRotation * 360.0 / 65536.0:F2}°");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Scale section
        ImGui.TextColored(new System.Numerics.Vector4(0.7f, 0.7f, 1.0f, 1.0f), "Scale:");

        int width = (int)_editingBuilding.Width;
        ImGui.SetNextItemWidth(150);
        if (ImGui.InputInt("Width", ref width))
        {
            if (width >= 0)
                _editingBuilding.Width = (uint)width;
        }

        int height = (int)_editingBuilding.Height;
        ImGui.SetNextItemWidth(150);
        if (ImGui.InputInt("Height", ref height))
        {
            if (height >= 0)
                _editingBuilding.Height = (uint)height;
        }

        int length = (int)_editingBuilding.Length;
        ImGui.SetNextItemWidth(150);
        if (ImGui.InputInt("Length", ref length))
        {
            if (length >= 0)
                _editingBuilding.Length = (uint)length;
        }

        ImGui.EndChild(); // End BuildingEditorScroll

        ImGui.Spacing();

        // Action buttons
        if (ImGui.BeginTable("BuildingActions", 3))
        {
            ImGui.TableNextColumn();

            // Duplicate button
            ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.25f, 0.45f, 0.65f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new System.Numerics.Vector4(0.35f, 0.55f, 0.75f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new System.Numerics.Vector4(0.20f, 0.40f, 0.60f, 1.0f));

            if (ImGui.Button("Duplicate", new System.Numerics.Vector2(-1, 30)))
            {
                var duplicate = new Building
                {
                    ModelID = _editingBuilding.ModelID,
                    XPosition = _editingBuilding.XPosition,
                    YPosition = _editingBuilding.YPosition,
                    ZPosition = _editingBuilding.ZPosition,
                    XFraction = _editingBuilding.XFraction,
                    YFraction = _editingBuilding.YFraction,
                    ZFraction = _editingBuilding.ZFraction,
                    XRotation = _editingBuilding.XRotation,
                    YRotation = _editingBuilding.YRotation,
                    ZRotation = _editingBuilding.ZRotation,
                    Width = _editingBuilding.Width,
                    Height = _editingBuilding.Height,
                    Length = _editingBuilding.Length
                };
                _currentMap!.Buildings.Add(duplicate);
                _selectedBuildingIndex = _currentMap.Buildings.Count - 1;
                _editingBuilding = duplicate;
                _statusMessage = "Building duplicated";
                _statusColor = new System.Numerics.Vector4(0.4f, 0.7f, 1.0f, 1.0f);
            }

            ImGui.PopStyleColor(3);

            ImGui.TableNextColumn();

            // Close button
            if (ImGui.Button("Close", new System.Numerics.Vector2(-1, 30)))
            {
                _showBuildingEditor = false;
                _editingBuilding = null;
                _selectedBuildingIndex = -1;
            }

            ImGui.TableNextColumn();

            // Delete button
            ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.65f, 0.25f, 0.25f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new System.Numerics.Vector4(0.75f, 0.35f, 0.35f, 1.0f));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, new System.Numerics.Vector4(0.60f, 0.20f, 0.20f, 1.0f));

            if (ImGui.Button("Delete", new System.Numerics.Vector2(-1, 30)))
            {
                if (_currentMap != null && _selectedBuildingIndex >= 0 && _selectedBuildingIndex < _currentMap.Buildings.Count)
                {
                    _currentMap.Buildings.RemoveAt(_selectedBuildingIndex);
                    _showBuildingEditor = false;
                    _editingBuilding = null;
                    _selectedBuildingIndex = -1;
                    _statusMessage = "Building deleted";
                    _statusColor = new System.Numerics.Vector4(0.8f, 0.5f, 0.5f, 1.0f);
                }
            }

            ImGui.PopStyleColor(3);

            ImGui.EndTable();
        }
    }
}
