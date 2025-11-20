using ImGuiNET;
using Clockwork.Core;
using Clockwork.Core.Models;
using Clockwork.Core.Services;
using Clockwork.Core.Logging;
using Clockwork.UI.Icons;
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
    private ColorTableService? _colorTableService;

    // Current matrix being edited
    private GameMatrix? _currentMatrix;
    private int _selectedMatrixIndex = 0;
    private bool _matrixLoaded = false;

    // Matrix dimensions
    private int _matrixWidth = 1;
    private int _matrixHeight = 1;
    private string _matrixName = "";

    // Tab names (for reference)
    private readonly string[] _tabNames = { "Headers", "Heights", "MapFiles" };

    // Edit state
    private int _editingRow = -1;
    private int _editingCol = -1;
    private string _editBuffer = "";
    private bool _justStartedEditing = false;

    // Scroll position
    private Vector2 _scrollPos = Vector2.Zero;

    // Cache for GetMatrixCount to avoid logging every frame
    private int _cachedMatrixCount = -1;
    private bool _hasLoggedMatrixPath = false;

    // Cache for matrix names to avoid reading all files multiple times
    private Dictionary<int, string> _matrixNamesCache = new();

    // Confirmation dialog state
    private bool _showDeleteConfirmation = false;
    private int _matrixToDelete = -1;

    // Focus management
    private bool _shouldFocus = false;
    private bool _shouldScrollToSelection = false;

    public void Initialize(ApplicationContext appContext)
    {
        _appContext = appContext;
        _romService = appContext.GetService<RomService>();
        _dialogService = appContext.GetService<DialogService>();
        _colorTableService = appContext.GetService<ColorTableService>();

        // Try to load last color table from settings
        var settings = Clockwork.Core.Settings.SettingsManager.Settings;
        if (!string.IsNullOrEmpty(settings.LastColorTablePath) && _colorTableService != null)
        {
            _colorTableService.LoadColorTable(settings.LastColorTablePath, silent: true);
        }
    }

    public void Draw()
    {
        if (!IsVisible) return;

        ImGui.SetNextWindowSize(new Vector2(900, 700), ImGuiCond.FirstUseEver);

        bool isVisible = IsVisible;
        if (!ImGui.Begin("Matrix Editor", ref isVisible))
        {
            IsVisible = isVisible;
            ImGui.End();
            return;
        }
        IsVisible = isVisible;

        // Apply focus if requested
        if (_shouldFocus)
        {
            ImGui.SetWindowFocus();
            _shouldFocus = false;
        }

        // Check if ROM is loaded
        if (_romService == null || _romService.CurrentRom == null || !_romService.CurrentRom.IsLoaded)
        {
            ImGui.TextColored(new Vector4(1, 0.5f, 0, 1), "Veuillez d'abord charger une ROM.");
            ImGui.End();
            return;
        }

        DrawEditorContent();

        // Delete confirmation dialog
        DrawDeleteConfirmationDialog();

        ImGui.End();
    }

    private void DrawEditorContent()
    {
        DrawToolbar();
        ImGui.Separator();

        DrawMatrixInfo();
        ImGui.Separator();

        DrawMatrixTabs();
    }

    private void DrawToolbar()
    {
        // Matrix selection
        ImGui.Text("Matrice:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(300);

        // Get matrix count from directory
        int matrixCount = GetMatrixCount();
        if (matrixCount > 0)
        {
            // Auto-load first matrix if none is loaded
            if (!_matrixLoaded && _selectedMatrixIndex == 0)
            {
                LoadMatrix(0);
            }

            // Build combo items with matrix names from cache
            string[] comboItems = Enumerable.Range(0, matrixCount)
                .Select(i => {
                    string name = GetMatrixName(i);
                    return string.IsNullOrEmpty(name) ? $"Matrix {i}" : $"{i}: {name}";
                })
                .ToArray();

            if (ImGui.Combo("##matrix", ref _selectedMatrixIndex, comboItems, matrixCount))
            {
                LoadMatrix(_selectedMatrixIndex);
            }
        }
        else
        {
            ImGui.TextColored(new Vector4(1, 0.5f, 0, 1), "Aucune matrice disponible");
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
        if (ImGui.Button("Ajouter"))
        {
            AddNewMatrix();
        }

        ImGui.SameLine();
        if (ImGui.Button("Supprimer"))
        {
            RequestDeleteMatrix(_selectedMatrixIndex);
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

        // ColorTable controls
        ImGui.SameLine();
        ImGui.Separator();
        ImGui.SameLine();

        if (ImGui.Button("Charger ColorTable..."))
        {
            LoadColorTable();
        }

        ImGui.SameLine();
        if (ImGui.Button("Réinitialiser Couleurs"))
        {
            ResetColorTable();
        }

        // Show current color table status
        if (_colorTableService != null && _colorTableService.HasColorTable)
        {
            ImGui.SameLine();
            string filename = Path.GetFileName(_colorTableService.CurrentColorTablePath ?? "");
            ImGui.TextColored(new Vector4(0.4f, 1.0f, 0.4f, 1.0f), $"[{filename}]");
        }
    }

    private void DrawMatrixInfo()
    {
        if (!_matrixLoaded || _currentMatrix == null)
        {
            int matrixCount = GetMatrixCount();
            if (matrixCount > 0)
            {
                ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1),
                    $"Aucune matrice chargée ({matrixCount} matrice(s) disponible(s) - cliquez sur 'Charger')");
            }
            else
            {
                ImGui.TextColored(new Vector4(1, 0.5f, 0, 1),
                    "Aucune matrice trouvée dans le dossier unpacked/matrices/");
            }
            return;
        }

        // Display matrix name from loaded data
        string displayName = _currentMatrix.Name;
        if (string.IsNullOrEmpty(displayName))
            displayName = $"Matrix {_selectedMatrixIndex}";

        ImGui.Text($"Nom: {displayName}");
        ImGui.SameLine(300);

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
                DrawHeadersGrid();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Altitudes"))
            {
                DrawHeightsGrid();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("MapFiles"))
            {
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

        // Arrays use [row, col] indexing
        // Color based on corresponding MapFiles cell
        DrawGrid(_currentMatrix.Headers, "Headers",
            (row, col) => _currentMatrix.Headers[row, col],
            (row, col, value) => _currentMatrix.Headers[row, col] = value,
            isUShort: true,
            useMapColoring: true);
    }

    private void DrawHeightsGrid()
    {
        if (_currentMatrix == null) return;

        ImGui.Text("Altitudes (Altitudes des cartes)");
        ImGui.Separator();

        // Arrays use [row, col] indexing
        // Color based on corresponding MapFiles cell
        DrawGrid(_currentMatrix.Altitudes, "Altitudes",
            (row, col) => _currentMatrix.Altitudes[row, col],
            (row, col, value) => _currentMatrix.Altitudes[row, col] = (byte)value,
            isUShort: false,
            useMapColoring: true);
    }

    private void DrawMapFilesGrid()
    {
        if (_currentMatrix == null) return;

        ImGui.Text("MapFiles (IDs des fichiers de carte)");
        ImGui.Separator();

        // Arrays use [row, col] indexing
        DrawGrid(_currentMatrix.Maps, "MapFiles",
            (row, col) => _currentMatrix.Maps[row, col],
            (row, col, value) => _currentMatrix.Maps[row, col] = value,
            isUShort: true,
            useMapColoring: true);
    }

    private void DrawGrid(Array data, string gridName,
        Func<int, int, ushort> getValue,
        Action<int, int, ushort> setValue,
        bool isUShort,
        bool useMapColoring = false)
    {
        if (_currentMatrix == null) return;

        int width = _currentMatrix.Width;
        int height = _currentMatrix.Height;

        // Table with borders
        ImGuiTableFlags flags = ImGuiTableFlags.Borders |
                                ImGuiTableFlags.RowBg |
                                ImGuiTableFlags.ScrollX | // Enable horizontal scroll for fixed width cells
                                ImGuiTableFlags.ScrollY | // Vertical scroll
                                ImGuiTableFlags.SizingFixedFit | // Fixed width columns
                                ImGuiTableFlags.NoHostExtendY; // Don't expand row height for widgets

        // Calculate available size
        Vector2 availSize = ImGui.GetContentRegionAvail();
        availSize.Y -= 30; // Leave room for instructions

        if (ImGui.BeginTable($"##grid_{gridName}", width + 1, flags, availSize))
        {
            // Setup columns with fixed width of 40px max
            ImGui.TableSetupColumn("Row", ImGuiTableColumnFlags.WidthFixed, 40);
            for (int col = 0; col < width; col++)
            {
                ImGui.TableSetupColumn($"{col}", ImGuiTableColumnFlags.WidthFixed, 40);
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

                    // Create unique ID scope for this cell to avoid conflicts when values are the same
                    ImGui.PushID(row * 10000 + col);

                    ushort value = getValue(row, col);
                    string cellId = "##input";

                    // Color code cells using TableSetBgColor for proper cell background
                    // If useMapColoring, get color based on corresponding map cell
                    Vector4 cellColor = GetCellColor(value, row, col, useMapColoring);
                    if (cellColor.W > 0) // If color has alpha
                    {
                        ImGui.TableSetBgColor(ImGuiTableBgTarget.CellBg, ImGui.ColorConvertFloat4ToU32(cellColor));
                    }

                    // Display value or edit field
                    string displayValue = gridName == "MapFiles" && value == GameMatrix.EMPTY_CELL
                        ? "-"
                        : value.ToString();

                    if (_editingRow == row && _editingCol == col)
                    {
                        // Edit mode - limit width to reasonable size
                        float cellWidth = ImGui.GetColumnWidth();
                        ImGui.SetNextItemWidth(Math.Min(cellWidth - 10, 80)); // Max 80px or column width

                        // Reduce padding to match text height
                        ImGui.PushStyleVar(ImGuiStyleVar.FramePadding, new Vector2(4, 2));

                        // Only set focus on first frame of editing
                        bool shouldSetFocus = _justStartedEditing;
                        if (_justStartedEditing)
                        {
                            AppLogger.Debug($"[MatrixEditor] Starting edit mode for cell ({row}, {col}) with value: {_editBuffer}");
                            ImGui.SetKeyboardFocusHere();
                            _justStartedEditing = false;
                        }

                        if (ImGui.InputText(cellId, ref _editBuffer, 10, ImGuiInputTextFlags.EnterReturnsTrue | ImGuiInputTextFlags.AutoSelectAll))
                        {
                            AppLogger.Debug($"[MatrixEditor] InputText confirmed with value: {_editBuffer}");
                            // Save edit
                            if (ushort.TryParse(_editBuffer, out ushort newValue))
                            {
                                AppLogger.Info($"[MatrixEditor] Saving cell ({row}, {col}) with new value: {newValue}");
                                setValue(row, col, newValue);
                            }
                            _editingRow = -1;
                            _editingCol = -1;
                        }

                        // Pop style after drawing InputText (always)
                        ImGui.PopStyleVar();

                        // Only check for lost focus if we're not on the first frame
                        // (SetKeyboardFocusHere needs one frame to take effect)
                        if (!shouldSetFocus)
                        {
                            bool isActive = ImGui.IsItemActive();
                            if (!isActive)
                            {
                                AppLogger.Debug($"[MatrixEditor] Lost focus on cell ({row}, {col}), canceling edit");
                                // Lost focus, exit edit mode
                                _editingRow = -1;
                                _editingCol = -1;
                            }
                        }
                    }
                    else
                    {
                        // Display mode - use Selectable to make entire cell clickable
                        ImGui.PushStyleVar(ImGuiStyleVar.SelectableTextAlign, new Vector2(0.5f, 0.5f)); // Center text

                        if (ImGui.Selectable(displayValue, false, ImGuiSelectableFlags.None, new Vector2(0, 0)))
                        {
                            AppLogger.Debug($"[MatrixEditor] Cell clicked: ({row}, {col})");
                            // Single click - enter edit mode
                            _editingRow = row;
                            _editingCol = col;
                            _editBuffer = displayValue == "-" ? "" : displayValue;
                            _justStartedEditing = true;
                        }

                        ImGui.PopStyleVar();

                        // Double-click to open map editor (MapFiles only)
                        if (gridName == "MapFiles" && ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                        {
                            if (value != GameMatrix.EMPTY_CELL)
                            {
                                AppLogger.Debug($"[MatrixEditor] Cell double-clicked: opening map {value}");
                                OpenMapEditor(value);
                            }
                        }
                    }

                    // Tooltip
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip($"Position: ({col}, {row})\nValue: {value}\nClick: Éditer\nDouble-click: Ouvrir carte (MapFiles)");
                    }

                    // Pop the unique ID scope
                    ImGui.PopID();
                }
            }

            ImGui.EndTable();
        }

        ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1),
            "[i] Cliquez sur une cellule pour éditer, double-cliquez sur MapFiles pour ouvrir la carte");
    }

    private Vector4 GetCellColor(ushort value, int row, int col, bool useMapColoring)
    {
        if (!useMapColoring)
            return new Vector4(0, 0, 0, 0); // No color

        // Get the map ID at this position for coloring
        ushort mapID = value;
        if (_currentMatrix != null && useMapColoring)
        {
            // Use the corresponding Maps cell for color lookup
            mapID = _currentMatrix.Maps[row, col];
        }

        // Try to get color from ColorTable if available
        if (_colorTableService != null && _colorTableService.HasColorTable)
        {
            var colorPair = _colorTableService.GetColorForMapID(mapID);
            if (colorPair.HasValue)
            {
                // Convert System.Drawing.Color to ImGui Vector4
                var bgColor = colorPair.Value.Background;
                return new Vector4(
                    bgColor.R / 255.0f,
                    bgColor.G / 255.0f,
                    bgColor.B / 255.0f,
                    0.6f // Semi-transparent
                );
            }
        }

        // Fallback colors if no ColorTable
        if (mapID == GameMatrix.EMPTY_CELL)
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

    /// <summary>
    /// Open the matrix editor and load a specific matrix by its ID
    /// </summary>
    /// <param name="matrixID">The matrix ID to load</param>
    public void OpenWithMatrixID(int matrixID)
    {
        // Always open the window and request focus
        IsVisible = true;
        _shouldFocus = true;

        // Load the matrix
        LoadMatrix(matrixID);

        // Scroll to the loaded matrix in the list
        _shouldScrollToSelection = true;
    }

    private void LoadMatrix(int matrixIndex)
    {
        AppLogger.Debug($"[MatrixEditor] LoadMatrix called for index {matrixIndex}");

        if (_romService == null || _romService.CurrentRom == null || !_romService.CurrentRom.IsLoaded)
        {
            AppLogger.Debug("[MatrixEditor] LoadMatrix: ROM not loaded");
            return;
        }

        try
        {
            // Matrix files are named without extension (0000, 0001, etc.)
            string matrixFileName = matrixIndex.ToString("D4"); // Format as 4-digit number: 0000, 0001, etc.
            string matrixPath = Path.Combine(
                _romService.CurrentRom.RomPath,
                "unpacked",
                "matrices",
                matrixFileName
            );

            AppLogger.Debug($"[MatrixEditor] Attempting to load from: {matrixPath}");

            if (!File.Exists(matrixPath))
            {
                AppLogger.Warn($"[MatrixEditor] Matrix file not found: {matrixPath}");
                return;
            }

            byte[] data = File.ReadAllBytes(matrixPath);
            AppLogger.Debug($"[MatrixEditor] Read {data.Length} bytes from file");

            _currentMatrix = GameMatrix.ReadFromBytes(data, matrixIndex);

            if (_currentMatrix != null)
            {
                _matrixWidth = _currentMatrix.Width;
                _matrixHeight = _currentMatrix.Height;
                _matrixName = $"Matrix {matrixIndex}";
                _matrixLoaded = true;
                _selectedMatrixIndex = matrixIndex;

                AppLogger.Info($"[MatrixEditor] Matrix {matrixIndex} loaded successfully: {_matrixWidth}x{_matrixHeight}");
            }
            else
            {
                AppLogger.Error($"[MatrixEditor] GameMatrix.ReadFromBytes returned null");
                _matrixLoaded = false;
            }
        }
        catch (Exception ex)
        {
            AppLogger.Error($"[MatrixEditor] Error loading matrix {matrixIndex}: {ex.Message}");
            AppLogger.Error($"[MatrixEditor] Stack trace: {ex.StackTrace}");
            _matrixLoaded = false;
        }
    }

    private void SaveMatrix()
    {
        if (_currentMatrix == null || _romService == null || _romService.CurrentRom == null || !_romService.CurrentRom.IsLoaded)
            return;

        try
        {
            // Matrix files are named without extension (0000, 0001, etc.)
            string matrixFileName = _selectedMatrixIndex.ToString("D4");
            string matrixPath = Path.Combine(
                _romService.CurrentRom.RomPath,
                "unpacked",
                "matrices",
                matrixFileName
            );

            byte[] data = _currentMatrix.ToBytes();
            File.WriteAllBytes(matrixPath, data);

            AppLogger.Info($"Matrix {_selectedMatrixIndex} saved successfully to {matrixPath}");
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Error saving matrix: {ex.Message}");
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
                _currentMatrix = GameMatrix.ReadFromBytes(data, -1);

                if (_currentMatrix != null)
                {
                    _matrixWidth = _currentMatrix.Width;
                    _matrixHeight = _currentMatrix.Height;
                    _matrixName = Path.GetFileNameWithoutExtension(filePath);
                    _matrixLoaded = true;

                    AppLogger.Info($"Matrix imported from {filePath}");
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error importing matrix: {ex.Message}");
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

                AppLogger.Info($"Matrix exported to {filePath}");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error exporting matrix: {ex.Message}");
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

        AppLogger.Debug($"Matrix resize requested: {_matrixWidth}x{_matrixHeight} (not yet implemented)");
    }

    private void OpenMapEditor(ushort mapId)
    {
        // TODO: Open map editor with the specified map
        AppLogger.Debug($"Opening map editor for map {mapId}");
    }

    private int GetMatrixCount()
    {
        if (_romService == null || _romService.CurrentRom == null || !_romService.CurrentRom.IsLoaded)
        {
            if (!_hasLoggedMatrixPath)
            {
                AppLogger.Debug("[MatrixEditor] GetMatrixCount: ROM not loaded");
            }
            _hasLoggedMatrixPath = true;
            return 0;
        }

        // Return cached value if already computed
        if (_cachedMatrixCount >= 0)
        {
            return _cachedMatrixCount;
        }

        try
        {
            string romPath = _romService.CurrentRom.RomPath;
            AppLogger.Debug($"[MatrixEditor] ROM base path: {romPath}");

            string matricesPath = Path.Combine(
                romPath,
                "unpacked",
                "matrices"
            );

            AppLogger.Debug($"[MatrixEditor] Looking for matrices in: {matricesPath}");
            AppLogger.Debug($"[MatrixEditor] Directory.Exists({matricesPath}): {Directory.Exists(matricesPath)}");

            if (!Directory.Exists(matricesPath))
            {
                AppLogger.Warn($"[MatrixEditor] Directory does not exist: {matricesPath}");

                // Try to list what's in the unpacked directory
                string unpackedPath = Path.Combine(romPath, "unpacked");
                if (Directory.Exists(unpackedPath))
                {
                    var subdirs = Directory.GetDirectories(unpackedPath);
                    AppLogger.Debug($"[MatrixEditor] Subdirectories in 'unpacked': {string.Join(", ", subdirs.Select(Path.GetFileName))}");
                }

                _hasLoggedMatrixPath = true;
                _cachedMatrixCount = 0;
                return 0;
            }

            // Get all files in matrices directory (no extension filter)
            var allFiles = Directory.GetFiles(matricesPath);

            // Filter files that are numeric (matrix files are named 0000, 0001, etc.)
            var matrixFiles = allFiles
                .Where(f => string.IsNullOrEmpty(Path.GetExtension(f))) // No extension
                .Where(f => int.TryParse(Path.GetFileName(f), out _))   // Numeric name
                .OrderBy(f => int.Parse(Path.GetFileName(f)))           // Sort numerically
                .ToArray();

            AppLogger.Debug($"[MatrixEditor] Found {matrixFiles.Length} matrix files (no extension, numeric names)");

            if (matrixFiles.Length > 0)
            {
                var examples = matrixFiles.Take(5).Select(Path.GetFileName);
                AppLogger.Debug($"[MatrixEditor] Example matrix files: {string.Join(", ", examples)}");
            }
            else
            {
                AppLogger.Warn($"[MatrixEditor] No matrix files found. Total files in directory: {allFiles.Length}");
                if (allFiles.Length > 0)
                {
                    var examples = allFiles.Take(10).Select(f => $"{Path.GetFileName(f)} (ext: '{Path.GetExtension(f)}')");
                    AppLogger.Debug($"[MatrixEditor] First 10 files: {string.Join(", ", examples)}");
                }
            }

            _hasLoggedMatrixPath = true;
            _cachedMatrixCount = matrixFiles.Length;
            return matrixFiles.Length;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"[MatrixEditor] Error in GetMatrixCount: {ex.Message}");
            AppLogger.Error($"[MatrixEditor] Stack trace: {ex.StackTrace}");
            _hasLoggedMatrixPath = true;
            _cachedMatrixCount = 0;
            return 0;
        }
    }

    private void LoadColorTable()
    {
        if (_dialogService == null || _colorTableService == null)
            return;

        string? filePath = _dialogService.OpenFileDialog(
            "Charger une ColorTable",
            "ColorTable files (*.ctb)|*.ctb|All files (*.*)|*.*"
        );

        if (!string.IsNullOrEmpty(filePath))
        {
            try
            {
                bool success = _colorTableService.LoadColorTable(filePath, silent: false);

                if (success)
                {
                    // Save path to settings
                    var settings = Clockwork.Core.Settings.SettingsManager.Settings;
                    settings.LastColorTablePath = filePath;
                    Clockwork.Core.Settings.SettingsManager.Save();

                    // Reset cache to force refresh
                    _cachedMatrixCount = -1;
                    _hasLoggedMatrixPath = false;

                    AppLogger.Info($"ColorTable loaded successfully from {filePath}");
                    AppLogger.Debug($"[ColorTable] {_colorTableService.GetDebugInfo()}");

                    // Test: get color for a few map IDs
                    for (uint testID = 0; testID < 10; testID++)
                    {
                        var color = _colorTableService.GetColorForMapID(testID);
                        if (color.HasValue)
                        {
                            AppLogger.Debug($"[ColorTable] Map ID {testID}: RGB({color.Value.Background.R}, {color.Value.Background.G}, {color.Value.Background.B})");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Failed to load ColorTable: {ex.Message}");
            }
        }
    }

    private void ResetColorTable()
    {
        if (_colorTableService == null)
            return;

        _colorTableService.ResetColorTable();

        // Clear saved path
        var settings = Clockwork.Core.Settings.SettingsManager.Settings;
        settings.LastColorTablePath = string.Empty;
        Clockwork.Core.Settings.SettingsManager.Save();

        // Reset cache to force refresh
        _cachedMatrixCount = -1;
        _hasLoggedMatrixPath = false;

        AppLogger.Info("ColorTable reset to defaults");
    }

    /// <summary>
    /// Get the name of a matrix from cache or file.
    /// Only reads the minimal data needed (first 5 bytes + name length + name).
    /// </summary>
    private string GetMatrixName(int matrixIndex)
    {
        // Check cache first
        if (_matrixNamesCache.TryGetValue(matrixIndex, out string? cachedName))
        {
            return cachedName;
        }

        // Read from file
        if (_romService == null || _romService.CurrentRom == null || !_romService.CurrentRom.IsLoaded)
            return string.Empty;

        try
        {
            string matrixFileName = matrixIndex.ToString("D4");
            string matrixPath = Path.Combine(
                _romService.CurrentRom.RomPath,
                "unpacked",
                "matrices",
                matrixFileName
            );

            if (!File.Exists(matrixPath))
                return string.Empty;

            // Read only the name field (skip width, height, hasHeaders, hasAltitudes)
            using var fs = new FileStream(matrixPath, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(fs);

            reader.ReadByte(); // width
            reader.ReadByte(); // height
            reader.ReadByte(); // hasHeaders
            reader.ReadByte(); // hasAltitudes

            // Read name
            byte nameLength = reader.ReadByte();
            string name = System.Text.Encoding.UTF8.GetString(reader.ReadBytes(nameLength));

            // Cache it
            _matrixNamesCache[matrixIndex] = name;

            return name;
        }
        catch
        {
            return string.Empty;
        }
    }

    private void AddNewMatrix()
    {
        if (_romService == null || _romService.CurrentRom == null || !_romService.CurrentRom.IsLoaded)
        {
            AppLogger.Warn("[MatrixEditor] Cannot add matrix: ROM not loaded");
            return;
        }

        try
        {
            // Find next available matrix index
            string matricesPath = Path.Combine(
                _romService.CurrentRom.RomPath,
                "unpacked",
                "matrices"
            );

            if (!Directory.Exists(matricesPath))
            {
                Directory.CreateDirectory(matricesPath);
            }

            // Get existing matrix indices
            var existingMatrices = Directory.GetFiles(matricesPath)
                .Where(f => string.IsNullOrEmpty(Path.GetExtension(f)))
                .Where(f => int.TryParse(Path.GetFileName(f), out _))
                .Select(f => int.Parse(Path.GetFileName(f)))
                .OrderBy(i => i)
                .ToList();

            // Find first gap or use next number
            int newIndex = 0;
            for (int i = 0; i < existingMatrices.Count; i++)
            {
                if (existingMatrices[i] != i)
                {
                    newIndex = i;
                    break;
                }
                newIndex = i + 1;
            }

            // Create new empty matrix (1x1)
            var newMatrix = new GameMatrix
            {
                Width = 1,
                Height = 1,
                Name = $"newMatrix {newIndex}",
                Maps = new ushort[1, 1],
                Headers = new ushort[1, 1],
                Altitudes = new byte[1, 1]
            };

            // Initialize with empty cells
            newMatrix.Maps[0, 0] = GameMatrix.EMPTY_CELL;
            newMatrix.Headers[0, 0] = 0;
            newMatrix.Altitudes[0, 0] = 0;

            // Save to file
            string matrixFileName = newIndex.ToString("D4");
            string matrixPath = Path.Combine(matricesPath, matrixFileName);

            byte[] data = newMatrix.ToBytes();
            File.WriteAllBytes(matrixPath, data);

            AppLogger.Info($"[MatrixEditor] Created new matrix at index {newIndex}");

            // Refresh cache and UI
            _cachedMatrixCount = -1;
            _matrixNamesCache.Clear();
            _hasLoggedMatrixPath = false;

            // Load the new matrix
            _selectedMatrixIndex = newIndex;
            LoadMatrix(newIndex);
        }
        catch (Exception ex)
        {
            AppLogger.Error($"[MatrixEditor] Error creating new matrix: {ex.Message}");
            AppLogger.Error($"[MatrixEditor] Stack trace: {ex.StackTrace}");
        }
    }

    private void RequestDeleteMatrix(int matrixIndex)
    {
        _matrixToDelete = matrixIndex;
        _showDeleteConfirmation = true;
    }

    private void DrawDeleteConfirmationDialog()
    {
        if (!_showDeleteConfirmation)
            return;

        ImGui.OpenPopup("Supprimer la matrice");

        var viewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(viewport.GetCenter(), ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));
        ImGui.SetNextWindowSize(new Vector2(400, 150));

        if (ImGui.BeginPopupModal("Supprimer la matrice", ref _showDeleteConfirmation, ImGuiWindowFlags.NoResize))
        {
            ImGui.TextWrapped($"Êtes-vous sûr de vouloir supprimer la matrice {_matrixToDelete} ?");
            ImGui.TextWrapped("Cette action est irréversible.");

            ImGui.Separator();
            ImGui.Spacing();

            float buttonWidth = 120;
            float spacing = ImGui.GetStyle().ItemSpacing.X;
            float totalWidth = buttonWidth * 2 + spacing;
            float offsetX = (ImGui.GetContentRegionAvail().X - totalWidth) * 0.5f;

            ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offsetX);

            if (ImGui.Button("Confirmer", new Vector2(buttonWidth, 0)))
            {
                DeleteMatrix(_matrixToDelete);
                _showDeleteConfirmation = false;
                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();

            if (ImGui.Button("Annuler", new Vector2(buttonWidth, 0)))
            {
                _showDeleteConfirmation = false;
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }
    }

    private void DeleteMatrix(int matrixIndex)
    {
        if (_romService == null || _romService.CurrentRom == null || !_romService.CurrentRom.IsLoaded)
        {
            AppLogger.Warn("[MatrixEditor] Cannot delete matrix: ROM not loaded");
            return;
        }

        try
        {
            string matrixFileName = matrixIndex.ToString("D4");
            string matrixPath = Path.Combine(
                _romService.CurrentRom.RomPath,
                "unpacked",
                "matrices",
                matrixFileName
            );

            if (!File.Exists(matrixPath))
            {
                AppLogger.Warn($"[MatrixEditor] Matrix file not found for deletion: {matrixPath}");
                return;
            }

            // Delete the file
            File.Delete(matrixPath);
            AppLogger.Info($"[MatrixEditor] Deleted matrix {matrixIndex}");

            // Remove from cache
            _matrixNamesCache.Remove(matrixIndex);

            // Refresh cache
            _cachedMatrixCount = -1;
            _hasLoggedMatrixPath = false;

            // If we deleted the currently loaded matrix, clear it
            if (_selectedMatrixIndex == matrixIndex)
            {
                _currentMatrix = null;
                _matrixLoaded = false;

                // Try to load another matrix if available
                int matrixCount = GetMatrixCount();
                if (matrixCount > 0)
                {
                    _selectedMatrixIndex = 0;
                    LoadMatrix(0);
                }
            }
        }
        catch (Exception ex)
        {
            AppLogger.Error($"[MatrixEditor] Error deleting matrix {matrixIndex}: {ex.Message}");
            AppLogger.Error($"[MatrixEditor] Stack trace: {ex.StackTrace}");
        }
    }
}
