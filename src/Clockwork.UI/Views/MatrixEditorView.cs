using ImGuiNET;
using Clockwork.Core;
using Clockwork.Core.Models;
using Clockwork.Core.Services;
using System.Numerics;

namespace Clockwork.UI.Views;

/// <summary>
/// Vue d'édition des matrices de jeu (organisation spatiale des cartes).
/// </summary>
public class MatrixEditorView : IView
{
    public bool IsVisible { get; set; } = false;

    private ApplicationContext? _appContext;
    private RomService? _romService;
    private DialogService? _dialogService;

    // Current matrix being edited
    private GameMatrix? _currentMatrix;
    private int _selectedMatrixIndex = 0;
    private bool _matrixLoaded = false;

    // Matrix dimensions
    private int _matrixWidth = 1;
    private int _matrixHeight = 1;
    private string _matrixName = "";

    // Tab selection
    private int _selectedTab = 0;
    private readonly string[] _tabNames = { "Headers", "Heights", "MapFiles" };

    // Edit state
    private int _editingRow = -1;
    private int _editingCol = -1;
    private string _editBuffer = "";

    // Scroll position
    private Vector2 _scrollPos = Vector2.Zero;

    public void Initialize(ApplicationContext appContext)
    {
        _appContext = appContext;
        _romService = appContext.GetService<RomService>();
        _dialogService = appContext.GetService<DialogService>();
    }

    public void Draw()
    {
        if (!IsVisible) return;

        ImGui.SetNextWindowSize(new Vector2(900, 700), ImGuiCond.FirstUseEver);
        if (!ImGui.Begin("Matrix Editor", ref IsVisible))
        {
            ImGui.End();
            return;
        }

        // Check if ROM is loaded
        if (_romService == null || !_romService.IsLoaded)
        {
            ImGui.TextColored(new Vector4(1, 0.5f, 0, 1), "Veuillez d'abord charger une ROM.");
            ImGui.End();
            return;
        }

        DrawToolbar();
        ImGui.Separator();

        DrawMatrixInfo();
        ImGui.Separator();

        DrawMatrixTabs();

        ImGui.End();
    }

    private void DrawToolbar()
    {
        // Matrix selection
        ImGui.Text("Matrice:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(200);

        if (_romService!.RomInfo != null)
        {
            int matrixCount = _romService.RomInfo.MatrixCount;
            if (ImGui.Combo("##matrix", ref _selectedMatrixIndex,
                Enumerable.Range(0, matrixCount).Select(i => $"Matrix {i}").ToArray(),
                matrixCount))
            {
                LoadMatrix(_selectedMatrixIndex);
            }
        }

        ImGui.SameLine();
        if (ImGui.Button("Charger"))
        {
            LoadMatrix(_selectedMatrixIndex);
        }

        ImGui.SameLine();
        if (ImGui.Button("Sauvegarder"))
        {
            SaveMatrix();
        }

        ImGui.SameLine();
        if (ImGui.Button("Importer..."))
        {
            ImportMatrix();
        }

        ImGui.SameLine();
        if (ImGui.Button("Exporter..."))
        {
            ExportMatrix();
        }
    }

    private void DrawMatrixInfo()
    {
        if (!_matrixLoaded || _currentMatrix == null)
        {
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1), "Aucune matrice chargée");
            return;
        }

        ImGui.Text($"Nom: {_matrixName}");
        ImGui.SameLine(200);

        // Width control
        ImGui.Text("Largeur:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100);
        if (ImGui.InputInt("##width", ref _matrixWidth, 1, 10))
        {
            _matrixWidth = Math.Clamp(_matrixWidth, 1, 255);
            ResizeMatrix(_matrixWidth, _matrixHeight);
        }

        ImGui.SameLine();
        // Height control
        ImGui.Text("Hauteur:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100);
        if (ImGui.InputInt("##height", ref _matrixHeight, 1, 10))
        {
            _matrixHeight = Math.Clamp(_matrixHeight, 1, 255);
            ResizeMatrix(_matrixWidth, _matrixHeight);
        }
    }

    private void DrawMatrixTabs()
    {
        if (!_matrixLoaded || _currentMatrix == null)
            return;

        if (ImGui.BeginTabBar("MatrixTabs"))
        {
            if (ImGui.BeginTabItem("Headers"))
            {
                _selectedTab = 0;
                DrawHeadersGrid();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Heights"))
            {
                _selectedTab = 1;
                DrawHeightsGrid();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("MapFiles"))
            {
                _selectedTab = 2;
                DrawMapFilesGrid();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }

    private void DrawHeadersGrid()
    {
        if (_currentMatrix == null) return;

        ImGui.Text("Headers (IDs des headers de carte)");
        ImGui.Separator();

        DrawGrid(_currentMatrix.Headers, "Headers",
            (row, col) => _currentMatrix.GetHeader(col, row),
            (row, col, value) => _currentMatrix.SetHeader(col, row, value),
            isUShort: true);
    }

    private void DrawHeightsGrid()
    {
        if (_currentMatrix == null) return;

        ImGui.Text("Heights (Altitudes des cartes)");
        ImGui.Separator();

        DrawGrid(_currentMatrix.Heights, "Heights",
            (row, col) => _currentMatrix.GetHeight(col, row),
            (row, col, value) => _currentMatrix.SetHeight(col, row, (byte)value),
            isUShort: false);
    }

    private void DrawMapFilesGrid()
    {
        if (_currentMatrix == null) return;

        ImGui.Text("MapFiles (IDs des fichiers de carte)");
        ImGui.Separator();

        DrawGrid(_currentMatrix.Maps, "MapFiles",
            (row, col) => _currentMatrix.GetMap(col, row),
            (row, col, value) => _currentMatrix.SetMap(col, row, value),
            isUShort: true);
    }

    private void DrawGrid(ushort[,] data, string gridName,
        Func<int, int, ushort> getValue,
        Action<int, int, ushort> setValue,
        bool isUShort)
    {
        if (_currentMatrix == null) return;

        int width = _currentMatrix.Width;
        int height = _currentMatrix.Height;

        // Table with borders
        ImGuiTableFlags flags = ImGuiTableFlags.Borders |
                                ImGuiTableFlags.RowBg |
                                ImGuiTableFlags.ScrollX |
                                ImGuiTableFlags.ScrollY |
                                ImGuiTableFlags.SizingFixedFit;

        // Calculate available size
        Vector2 availSize = ImGui.GetContentRegionAvail();
        availSize.Y -= 30; // Leave room for instructions

        if (ImGui.BeginTable($"##grid_{gridName}", width + 1, flags, availSize))
        {
            // Setup columns
            ImGui.TableSetupColumn("Row", ImGuiTableColumnFlags.WidthFixed, 40);
            for (int col = 0; col < width; col++)
            {
                ImGui.TableSetupColumn($"{col}", ImGuiTableColumnFlags.WidthFixed, 60);
            }

            // Header row
            ImGui.TableHeadersRow();

            // Data rows
            for (int row = 0; row < height; row++)
            {
                ImGui.TableNextRow();

                // Row label
                ImGui.TableNextColumn();
                ImGui.Text($"{row}");

                // Data cells
                for (int col = 0; col < width; col++)
                {
                    ImGui.TableNextColumn();

                    ushort value = getValue(row, col);
                    string cellId = $"##cell_{row}_{col}";

                    // Color code cells
                    Vector4 cellColor = GetCellColor(value, gridName == "MapFiles");
                    if (cellColor.W > 0) // If color is set
                    {
                        ImGui.PushStyleColor(ImGuiCol.FrameBg, cellColor);
                    }

                    // Display value or edit field
                    string displayValue = gridName == "MapFiles" && value == GameMatrix.EMPTY
                        ? "-"
                        : value.ToString();

                    if (_editingRow == row && _editingCol == col)
                    {
                        // Edit mode
                        ImGui.SetNextItemWidth(50);
                        ImGui.SetKeyboardFocusHere();
                        if (ImGui.InputText(cellId, ref _editBuffer, 10, ImGuiInputTextFlags.EnterReturnsTrue))
                        {
                            // Save edit
                            if (ushort.TryParse(_editBuffer, out ushort newValue))
                            {
                                setValue(row, col, newValue);
                            }
                            _editingRow = -1;
                            _editingCol = -1;
                        }

                        if (!ImGui.IsItemActive() && (_editingRow == row && _editingCol == col))
                        {
                            // Lost focus, exit edit mode
                            _editingRow = -1;
                            _editingCol = -1;
                        }
                    }
                    else
                    {
                        // Display mode
                        ImGui.Text(displayValue);

                        // Check for click to edit
                        if (ImGui.IsItemClicked())
                        {
                            _editingRow = row;
                            _editingCol = col;
                            _editBuffer = displayValue == "-" ? "" : displayValue;
                        }

                        // Double-click to open map editor (MapFiles only)
                        if (gridName == "MapFiles" && ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                        {
                            if (value != GameMatrix.EMPTY)
                            {
                                OpenMapEditor(value);
                            }
                        }
                    }

                    if (cellColor.W > 0)
                    {
                        ImGui.PopStyleColor();
                    }

                    // Tooltip
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip($"Position: ({col}, {row})\nValue: {value}\nClick: Éditer\nDouble-click: Ouvrir carte (MapFiles)");
                    }
                }
            }

            ImGui.EndTable();
        }

        ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1),
            "💡 Cliquez sur une cellule pour éditer, double-cliquez sur MapFiles pour ouvrir la carte");
    }

    private Vector4 GetCellColor(ushort value, bool isMapFiles)
    {
        if (isMapFiles)
        {
            if (value == GameMatrix.EMPTY)
            {
                // Empty cell - gray
                return new Vector4(0.3f, 0.3f, 0.3f, 0.5f);
            }
            else
            {
                // Valid map - light green
                return new Vector4(0.2f, 0.5f, 0.2f, 0.3f);
            }
        }

        // Default - no color
        return new Vector4(0, 0, 0, 0);
    }

    private void LoadMatrix(int matrixIndex)
    {
        if (_romService == null || !_romService.IsLoaded)
            return;

        try
        {
            string matrixPath = Path.Combine(
                _romService.RomDirectory!,
                "unpacked",
                "matrices",
                $"{matrixIndex}.bin"
            );

            if (!File.Exists(matrixPath))
            {
                Console.WriteLine($"Matrix file not found: {matrixPath}");
                return;
            }

            byte[] data = File.ReadAllBytes(matrixPath);
            _currentMatrix = GameMatrix.ReadFromBytes(data);
            _matrixWidth = _currentMatrix.Width;
            _matrixHeight = _currentMatrix.Height;
            _matrixName = $"Matrix {matrixIndex}";
            _matrixLoaded = true;
            _selectedMatrixIndex = matrixIndex;

            Console.WriteLine($"Matrix {matrixIndex} loaded: {_matrixWidth}x{_matrixHeight}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading matrix {matrixIndex}: {ex.Message}");
            _matrixLoaded = false;
        }
    }

    private void SaveMatrix()
    {
        if (_currentMatrix == null || _romService == null || !_romService.IsLoaded)
            return;

        try
        {
            string matrixPath = Path.Combine(
                _romService.RomDirectory!,
                "unpacked",
                "matrices",
                $"{_selectedMatrixIndex}.bin"
            );

            byte[] data = _currentMatrix.ToBytes();
            File.WriteAllBytes(matrixPath, data);

            Console.WriteLine($"Matrix {_selectedMatrixIndex} saved successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error saving matrix: {ex.Message}");
        }
    }

    private void ImportMatrix()
    {
        if (_dialogService == null)
            return;

        string? filePath = _dialogService.OpenFileDialog(
            "Importer une matrice",
            "Binary files (*.bin)|*.bin|All files (*.*)|*.*"
        );

        if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
        {
            try
            {
                byte[] data = File.ReadAllBytes(filePath);
                _currentMatrix = GameMatrix.ReadFromBytes(data);
                _matrixWidth = _currentMatrix.Width;
                _matrixHeight = _currentMatrix.Height;
                _matrixName = Path.GetFileNameWithoutExtension(filePath);
                _matrixLoaded = true;

                Console.WriteLine($"Matrix imported from {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error importing matrix: {ex.Message}");
            }
        }
    }

    private void ExportMatrix()
    {
        if (_currentMatrix == null || _dialogService == null)
            return;

        string? filePath = _dialogService.SaveFileDialog(
            "Exporter la matrice",
            "Binary files (*.bin)|*.bin|All files (*.*)|*.*",
            $"matrix_{_selectedMatrixIndex}.bin"
        );

        if (!string.IsNullOrEmpty(filePath))
        {
            try
            {
                byte[] data = _currentMatrix.ToBytes();
                File.WriteAllBytes(filePath, data);

                Console.WriteLine($"Matrix exported to {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error exporting matrix: {ex.Message}");
            }
        }
    }

    private void ResizeMatrix(int newWidth, int newHeight)
    {
        if (_currentMatrix == null)
            return;

        // TODO: Implement GameMatrix.Resize() method in Core
        // For now, just clamp values
        _matrixWidth = Math.Clamp(newWidth, 1, 255);
        _matrixHeight = Math.Clamp(newHeight, 1, 255);

        Console.WriteLine($"Matrix resize requested: {_matrixWidth}x{_matrixHeight} (not yet implemented)");
    }

    private void OpenMapEditor(ushort mapId)
    {
        // TODO: Open map editor with the specified map
        Console.WriteLine($"Opening map editor for map {mapId}");
    }
}
