using Clockwork.Core;
using Clockwork.Core.Models;
using Clockwork.Core.Services;
using Clockwork.UI.Icons;
using Clockwork.UI.Graphics;
using ImGuiNET;
using System.Numerics;

namespace Clockwork.UI.Views;

/// <summary>
/// View for editing NSBTX (Nintendo DS Texture) files
/// Allows importing, exporting, and managing texture packs
/// </summary>
public class NsbtxEditorView : IView
{
    private readonly ApplicationContext _appContext;
    private NsbtxService? _nsbtxService;
    private DialogService? _dialogService;
    private RomService? _romService;
    private TextureManager? _textureManager;

    public bool IsVisible { get; set; } = false;

    // UI State
    private int _selectedNsbtxId = 0;
    private int _selectedTextureIndex = -1;
    private int _selectedPaletteIndex = -1;
    private string _statusMessage = string.Empty;
    private Vector4 _statusColor = new(1.0f, 1.0f, 1.0f, 1.0f);
    private float _statusTimer = 0.0f;
    private string _searchFilter = string.Empty;

    // Texture display
    private IntPtr _currentTextureHandle = IntPtr.Zero;
    private int _currentTextureWidth = 0;
    private int _currentTextureHeight = 0;
    private int _currentPaletteIndex = 0;
    private int _zoomLevel = 4; // Default 4x zoom
    private bool _useTiledFormat = false; // Toggle between tiled and linear format

    // Add/Remove dialogs
    private bool _showAddDialog = false;
    private int _newNsbtxId = 0;
    private int _sourceNsbtxId = 0;
    private bool _showRemoveConfirm = false;
    private int _nsbtxToRemove = 0;

    public NsbtxEditorView(ApplicationContext appContext)
    {
        _appContext = appContext;
    }

    public void Initialize()
    {
        _nsbtxService = _appContext.GetService<NsbtxService>();
        _dialogService = _appContext.GetService<DialogService>();
        _romService = _appContext.GetService<RomService>();

        // Load available NSBTX files when ROM is loaded
        if (_romService?.CurrentRom != null && _nsbtxService != null)
        {
            _nsbtxService.LoadAvailableNsbtx();
        }
    }

    public void SetTextureManager(TextureManager textureManager)
    {
        _textureManager = textureManager;
    }

    public void Draw()
    {
        if (!IsVisible) return;

        ImGui.SetNextWindowSize(new Vector2(900, 600), ImGuiCond.FirstUseEver);
        bool isVisible = IsVisible;
        if (ImGui.Begin($"{FontAwesomeIcons.Image} NSBTX Texture Editor", ref isVisible))
        {
            // Check if ROM is loaded
            bool romLoaded = _romService?.CurrentRom != null;

            if (!romLoaded)
            {
                ImGui.TextColored(new Vector4(1.0f, 0.7f, 0.4f, 1.0f),
                    "No ROM loaded. Please load a ROM first.");
                ImGui.End();
                return;
            }

            // Reload button
            if (ImGui.Button($"{FontAwesomeIcons.Refresh} Reload List"))
            {
                _nsbtxService?.LoadAvailableNsbtx();
                SetStatus("NSBTX list reloaded", new Vector4(0.4f, 1.0f, 0.4f, 1.0f));
            }

            ImGui.SameLine();

            // Texture pack type selector
            var packType = _nsbtxService?.CurrentPackType ?? NsbtxService.TexturePackType.Map;
            if (ImGui.RadioButton("Map Textures", packType == NsbtxService.TexturePackType.Map))
            {
                if (_nsbtxService != null)
                {
                    _nsbtxService.CurrentPackType = NsbtxService.TexturePackType.Map;
                    _nsbtxService.LoadAvailableNsbtx();
                }
            }

            ImGui.SameLine();

            if (ImGui.RadioButton("Building Textures", packType == NsbtxService.TexturePackType.Building))
            {
                if (_nsbtxService != null)
                {
                    _nsbtxService.CurrentPackType = NsbtxService.TexturePackType.Building;
                    _nsbtxService.LoadAvailableNsbtx();
                }
            }

            ImGui.Separator();

            // Main layout: List on left, details on right
            if (ImGui.BeginChild("MainContent", new Vector2(0, -30)))
            {
                if (ImGui.BeginChild("LeftPanel", new Vector2(250, 0), ImGuiChildFlags.None))
                {
                    DrawNsbtxList();
                    ImGui.EndChild();
                }

                ImGui.SameLine();

                if (ImGui.BeginChild("RightPanel", new Vector2(0, 0), ImGuiChildFlags.None))
                {
                    DrawNsbtxDetails();
                    ImGui.EndChild();
                }

                ImGui.EndChild();
            }

            // Status bar
            DrawStatusBar();

            // Dialogs
            DrawAddDialog();
            DrawRemoveConfirmDialog();

            ImGui.End();
            IsVisible = isVisible;
        }

        // Update status timer
        if (_statusTimer > 0)
        {
            _statusTimer -= (float)ImGui.GetIO().DeltaTime;
            if (_statusTimer <= 0)
            {
                _statusMessage = string.Empty;
            }
        }
    }

    private void DrawNsbtxList()
    {
        ImGui.Text("Texture Packs");
        ImGui.Separator();

        // Search filter
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##search", "Search...", ref _searchFilter, 256);

        ImGui.Spacing();

        // List of NSBTX files
        if (ImGui.BeginChild("NsbtxList", new Vector2(0, -30)))
        {
            var nsbtxList = _nsbtxService?.AvailableNsbtxIds ?? new List<int>();

            // Apply search filter
            var filteredList = string.IsNullOrWhiteSpace(_searchFilter)
                ? nsbtxList
                : nsbtxList.Where(id => id.ToString().Contains(_searchFilter)).ToList();

            if (filteredList.Count == 0)
            {
                ImGui.TextDisabled("No texture packs found");
            }
            else
            {
                foreach (var nsbtxId in filteredList)
                {
                    bool isSelected = (_selectedNsbtxId == nsbtxId);
                    string label = $"{nsbtxId:D4}";

                    if (ImGui.Selectable(label, isSelected))
                    {
                        _selectedNsbtxId = nsbtxId;
                        LoadNsbtx(nsbtxId);
                    }

                    if (isSelected)
                    {
                        ImGui.SetItemDefaultFocus();
                    }
                }
            }

            ImGui.EndChild();
        }

        // Add/Remove buttons
        if (ImGui.Button($"{FontAwesomeIcons.Plus} Add", new Vector2(-1, 0)))
        {
            _showAddDialog = true;
            _newNsbtxId = 0;
            _sourceNsbtxId = _selectedNsbtxId;
        }
    }

    private void DrawNsbtxDetails()
    {
        var currentNsbtx = _nsbtxService?.CurrentNsbtx;

        if (currentNsbtx == null)
        {
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f),
                "Select a texture pack from the list");
            return;
        }

        ImGui.Text($"NSBTX ID: {_selectedNsbtxId:D4}");
        ImGui.Separator();
        ImGui.Spacing();

        // File info
        if (ImGui.CollapsingHeader("File Information", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Text($"Magic: {currentNsbtx.Magic}");
            ImGui.Text($"Version: {currentNsbtx.Version}");
            ImGui.Text($"File Size: {currentNsbtx.FileSize} bytes ({currentNsbtx.FileSize / 1024.0:F2} KB)");
            ImGui.Text($"Block Count: {currentNsbtx.BlockCount}");
        }

        ImGui.Spacing();

        // Textures list
        if (ImGui.CollapsingHeader("Textures", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Text($"Count: {currentNsbtx.TextureNames.Count}");
            ImGui.Spacing();

            if (currentNsbtx.TextureNames.Count > 0)
            {
                if (ImGui.BeginChild("TexturesList", new Vector2(0, 150), ImGuiChildFlags.None))
                {
                    for (int i = 0; i < currentNsbtx.TextureNames.Count; i++)
                    {
                        string texName = currentNsbtx.TextureNames[i];
                        bool isSelected = (_selectedTextureIndex == i);

                        if (ImGui.Selectable($"{FontAwesomeIcons.Image} {texName}", isSelected))
                        {
                            _selectedTextureIndex = i;
                            _selectedPaletteIndex = -1; // Deselect palette when selecting texture
                            _currentPaletteIndex = 0; // Reset to first palette
                            LoadTexturePreview(i, _currentPaletteIndex);
                        }

                        if (isSelected)
                        {
                            ImGui.SetItemDefaultFocus();
                        }
                    }
                    ImGui.EndChild();
                }
            }
            else
            {
                ImGui.TextDisabled("  (no texture names found)");
            }
        }

        ImGui.Spacing();

        // Palettes list
        if (ImGui.CollapsingHeader("Palettes", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Text($"Count: {currentNsbtx.PaletteNames.Count}");
            ImGui.Spacing();

            if (currentNsbtx.PaletteNames.Count > 0)
            {
                if (ImGui.BeginChild("PalettesList", new Vector2(0, 150), ImGuiChildFlags.None))
                {
                    for (int i = 0; i < currentNsbtx.PaletteNames.Count; i++)
                    {
                        string palName = currentNsbtx.PaletteNames[i];
                        bool isSelected = (_selectedPaletteIndex == i);

                        if (ImGui.Selectable($"{FontAwesomeIcons.Palette} {palName}", isSelected))
                        {
                            _selectedPaletteIndex = i;
                            _selectedTextureIndex = -1; // Deselect texture when selecting palette
                        }

                        if (isSelected)
                        {
                            ImGui.SetItemDefaultFocus();
                        }
                    }
                    ImGui.EndChild();
                }
            }
            else
            {
                ImGui.TextDisabled("  (no palette names found)");
            }
        }

        ImGui.Spacing();

        // Preview section
        if (ImGui.CollapsingHeader("Preview", ImGuiTreeNodeFlags.DefaultOpen))
        {
            if (_selectedTextureIndex >= 0 && _selectedTextureIndex < currentNsbtx.TextureNames.Count)
            {
                ImGui.Text($"{FontAwesomeIcons.Image} Texture Preview");
                ImGui.Separator();
                ImGui.Spacing();

                string texName = currentNsbtx.TextureNames[_selectedTextureIndex];
                ImGui.Text($"Name: {texName}");
                ImGui.Text($"Index: {_selectedTextureIndex}");

                if (_selectedTextureIndex < currentNsbtx.Textures.Count)
                {
                    var texInfo = currentNsbtx.Textures[_selectedTextureIndex];
                    ImGui.Spacing();
                    ImGui.Text($"Dimensions: {texInfo.ActualWidth}x{texInfo.ActualHeight}");
                    ImGui.Text($"Format: {GetFormatName(texInfo.Format)}");
                    ImGui.Text($"Offset: 0x{texInfo.TextureOffset:X}");
                    ImGui.Text($"Color0 Transparent: {texInfo.Color0Transparent}");

                    // Palette selector for palette-based formats
                    if (texInfo.Format >= 2 && texInfo.Format <= 4 && currentNsbtx.PaletteNames.Count > 0)
                    {
                        ImGui.Spacing();
                        ImGui.Text("Palette:");
                        ImGui.SameLine();

                        // Create combo items
                        string[] paletteItems = new string[currentNsbtx.PaletteNames.Count];
                        for (int i = 0; i < currentNsbtx.PaletteNames.Count; i++)
                        {
                            paletteItems[i] = $"{i}: {currentNsbtx.PaletteNames[i]}";
                        }

                        ImGui.SetNextItemWidth(200);
                        if (ImGui.Combo("##palette", ref _currentPaletteIndex, paletteItems, paletteItems.Length))
                        {
                            // Palette changed, reload texture
                            LoadTexturePreview(_selectedTextureIndex, _currentPaletteIndex);
                        }
                    }

                    // Tiling mode toggle
                    ImGui.Spacing();
                    if (ImGui.Checkbox("8x8 Tiled Format", ref _useTiledFormat))
                    {
                        // Tiling mode changed, reload texture
                        LoadTexturePreview(_selectedTextureIndex, _currentPaletteIndex);
                    }
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("Toggle between linear (line-by-line) and tiled (8x8 blocks) texture format.\nMost 3D textures use linear format.");
                    }
                }

                ImGui.Spacing();
                ImGui.Separator();
                ImGui.Spacing();

                // Display texture if loaded
                if (_currentTextureHandle != IntPtr.Zero)
                {
                    // Zoom controls
                    ImGui.Text("Zoom:");
                    ImGui.SameLine();
                    if (ImGui.Button("1x")) _zoomLevel = 1;
                    ImGui.SameLine();
                    if (ImGui.Button("2x")) _zoomLevel = 2;
                    ImGui.SameLine();
                    if (ImGui.Button("4x")) _zoomLevel = 4;
                    ImGui.SameLine();
                    if (ImGui.Button("8x")) _zoomLevel = 8;
                    ImGui.SameLine();
                    if (ImGui.Button("16x")) _zoomLevel = 16;

                    int displayWidth = _currentTextureWidth * _zoomLevel;
                    int displayHeight = _currentTextureHeight * _zoomLevel;

                    ImGui.Text($"Display size: {displayWidth}x{displayHeight} (texture: {_currentTextureWidth}x{_currentTextureHeight})");
                    ImGui.Spacing();

                    // Draw texture with checkerboard background for transparency
                    var drawList = ImGui.GetWindowDrawList();
                    var cursorPos = ImGui.GetCursorScreenPos();

                    // Draw checkerboard pattern
                    int checkSize = 8;
                    for (int y = 0; y < displayHeight; y += checkSize)
                    {
                        for (int x = 0; x < displayWidth; x += checkSize)
                        {
                            bool isDark = ((x / checkSize) + (y / checkSize)) % 2 == 0;
                            uint color = isDark ? 0xFF808080 : 0xFFC0C0C0;
                            drawList.AddRectFilled(
                                new Vector2(cursorPos.X + x, cursorPos.Y + y),
                                new Vector2(cursorPos.X + x + checkSize, cursorPos.Y + y + checkSize),
                                color
                            );
                        }
                    }

                    // Draw texture on top
                    ImGui.Image(_currentTextureHandle, new Vector2(displayWidth, displayHeight));

                    // Debug info
                    ImGui.Spacing();
                    if (ImGui.CollapsingHeader("Debug Info"))
                    {
                        if (_selectedTextureIndex >= 0 && _selectedTextureIndex < currentNsbtx.Textures.Count)
                        {
                            var texInfo = currentNsbtx.Textures[_selectedTextureIndex];
                            var rawData = currentNsbtx.GetTextureData(_selectedTextureIndex);
                            if (rawData != null)
                            {
                                ImGui.Text($"Raw texture data size: {rawData.Length} bytes");

                                // Show first 64 bytes as hex
                                int bytesToShow = Math.Min(64, rawData.Length);
                                ImGui.Text("First bytes (hex):");
                                string hexStr = BitConverter.ToString(rawData, 0, bytesToShow).Replace("-", " ");
                                ImGui.TextWrapped(hexStr);

                                // Decode and show first 4 pixels
                                var decoded = currentNsbtx.DecodeTextureToRGBA(_selectedTextureIndex, _currentPaletteIndex, _useTiledFormat);
                                if (decoded != null && decoded.Length >= 16)
                                {
                                    ImGui.Spacing();
                                    ImGui.Text("First 4 pixels (RGBA):");
                                    for (int i = 0; i < 4 && i * 4 < decoded.Length; i++)
                                    {
                                        int idx = i * 4;
                                        ImGui.Text($"Pixel {i}: R={decoded[idx]:X2} G={decoded[idx+1]:X2} B={decoded[idx+2]:X2} A={decoded[idx+3]:X2}");
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f),
                        "Texture preview not available (unsupported format or loading failed)");
                }
            }
            else if (_selectedPaletteIndex >= 0 && _selectedPaletteIndex < currentNsbtx.PaletteNames.Count)
            {
                ImGui.Text($"{FontAwesomeIcons.Palette} Palette Preview");
                ImGui.Separator();
                ImGui.Spacing();

                string palName = currentNsbtx.PaletteNames[_selectedPaletteIndex];
                ImGui.Text($"Name: {palName}");
                ImGui.Text($"Index: {_selectedPaletteIndex}");

                ImGui.Spacing();
                ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f),
                    "Palette preview not yet implemented.");
            }
            else
            {
                ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f),
                    "Select a texture or palette to preview");
            }
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Action buttons
        if (ImGui.Button($"{FontAwesomeIcons.Download} Import NSBTX", new Vector2(-1, 0)))
        {
            ImportNsbtx();
        }

        if (ImGui.Button($"{FontAwesomeIcons.Upload} Export NSBTX", new Vector2(-1, 0)))
        {
            ExportNsbtx();
        }

        ImGui.Spacing();

        if (ImGui.Button($"{FontAwesomeIcons.Trash} Remove", new Vector2(-1, 0)))
        {
            _showRemoveConfirm = true;
            _nsbtxToRemove = _selectedNsbtxId;
        }
    }

    private void DrawStatusBar()
    {
        if (!string.IsNullOrEmpty(_statusMessage))
        {
            ImGui.Separator();
            ImGui.TextColored(_statusColor, _statusMessage);
        }
    }

    private void DrawAddDialog()
    {
        if (!_showAddDialog) return;

        ImGui.OpenPopup("Add Texture Pack");

        var center = ImGui.GetMainViewport().GetCenter();
        ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));

        if (ImGui.BeginPopupModal("Add Texture Pack", ref _showAddDialog, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.Text("Create a new texture pack by duplicating an existing one.");
            ImGui.Spacing();

            ImGui.Text("New ID:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(200);
            ImGui.InputInt("##newId", ref _newNsbtxId);

            ImGui.Text("Copy from ID:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(200);
            ImGui.InputInt("##sourceId", ref _sourceNsbtxId);

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            if (ImGui.Button("Create", new Vector2(120, 0)))
            {
                if (_nsbtxService?.AddNsbtx(_sourceNsbtxId, _newNsbtxId) == true)
                {
                    SetStatus($"Created texture pack {_newNsbtxId:D4}", new Vector4(0.4f, 1.0f, 0.4f, 1.0f));
                    _showAddDialog = false;
                }
                else
                {
                    SetStatus("Failed to create texture pack", new Vector4(1.0f, 0.4f, 0.4f, 1.0f));
                }
            }

            ImGui.SameLine();

            if (ImGui.Button("Cancel", new Vector2(120, 0)))
            {
                _showAddDialog = false;
            }

            ImGui.EndPopup();
        }
    }

    private void DrawRemoveConfirmDialog()
    {
        if (!_showRemoveConfirm) return;

        ImGui.OpenPopup("Confirm Remove");

        var center = ImGui.GetMainViewport().GetCenter();
        ImGui.SetNextWindowPos(center, ImGuiCond.Appearing, new Vector2(0.5f, 0.5f));

        if (ImGui.BeginPopupModal("Confirm Remove", ref _showRemoveConfirm, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.Text($"Are you sure you want to remove texture pack {_nsbtxToRemove:D4}?");
            ImGui.TextColored(new Vector4(1.0f, 0.4f, 0.4f, 1.0f), "This action cannot be undone!");
            ImGui.Spacing();

            if (ImGui.Button("Remove", new Vector2(120, 0)))
            {
                if (_nsbtxService?.RemoveNsbtx(_nsbtxToRemove) == true)
                {
                    SetStatus($"Removed texture pack {_nsbtxToRemove:D4}", new Vector4(0.4f, 1.0f, 0.4f, 1.0f));
                    _showRemoveConfirm = false;
                }
                else
                {
                    SetStatus("Failed to remove texture pack", new Vector4(1.0f, 0.4f, 0.4f, 1.0f));
                }
            }

            ImGui.SameLine();

            if (ImGui.Button("Cancel", new Vector2(120, 0)))
            {
                _showRemoveConfirm = false;
            }

            ImGui.EndPopup();
        }
    }

    private void LoadNsbtx(int nsbtxId)
    {
        if (_nsbtxService?.LoadNsbtx(nsbtxId) != null)
        {
            SetStatus($"Loaded texture pack {nsbtxId:D4}", new Vector4(0.4f, 1.0f, 0.4f, 1.0f));
            // Reset selection and texture when loading new NSBTX
            _selectedTextureIndex = -1;
            _selectedPaletteIndex = -1;
            _currentTextureHandle = IntPtr.Zero;
            _currentPaletteIndex = 0;
        }
        else
        {
            SetStatus($"Failed to load texture pack {nsbtxId:D4}", new Vector4(1.0f, 0.4f, 0.4f, 1.0f));
        }
    }

    private void ImportNsbtx()
    {
        if (_dialogService == null) return;

        string? filePath = _dialogService.OpenFileDialog(
            "Import NSBTX File",
            "NSBTX Files (*.nsbtx)|*.nsbtx|All Files (*.*)|*.*"
        );

        if (string.IsNullOrEmpty(filePath)) return;

        if (_nsbtxService?.ImportNsbtx(filePath, _selectedNsbtxId) != null)
        {
            SetStatus($"Imported NSBTX to {_selectedNsbtxId:D4}", new Vector4(0.4f, 1.0f, 0.4f, 1.0f));
        }
        else
        {
            SetStatus("Failed to import NSBTX", new Vector4(1.0f, 0.4f, 0.4f, 1.0f));
        }
    }

    private void ExportNsbtx()
    {
        if (_dialogService == null || _nsbtxService?.CurrentNsbtx == null) return;

        string? filePath = _dialogService.SaveFileDialog(
            "Export NSBTX File",
            $"texture_{_selectedNsbtxId:D4}.nsbtx",
            "NSBTX Files (*.nsbtx)|*.nsbtx|All Files (*.*)|*.*"
        );

        if (string.IsNullOrEmpty(filePath)) return;

        if (_nsbtxService.ExportNsbtx(filePath))
        {
            SetStatus($"Exported NSBTX to {Path.GetFileName(filePath)}", new Vector4(0.4f, 1.0f, 0.4f, 1.0f));
        }
        else
        {
            SetStatus("Failed to export NSBTX", new Vector4(1.0f, 0.4f, 0.4f, 1.0f));
        }
    }

    private string GetFormatName(int format)
    {
        return format switch
        {
            0 => "None",
            1 => "A3I5 (Translucent)",
            2 => "4-Color Palette",
            3 => "16-Color Palette",
            4 => "256-Color Palette",
            5 => "4x4 Compressed",
            6 => "A5I3 (Translucent)",
            7 => "Direct Color",
            _ => $"Unknown ({format})"
        };
    }

    private void LoadTexturePreview(int textureIndex, int paletteIndex = 0)
    {
        try
        {
            // Reset previous texture
            _currentTextureHandle = IntPtr.Zero;

            if (_textureManager == null)
            {
                Core.Logging.AppLogger.Error("[NsbtxEditor] TextureManager not initialized");
                SetStatus("TextureManager not initialized", new Vector4(1.0f, 0.4f, 0.4f, 1.0f));
                return;
            }

            var currentNsbtx = _nsbtxService?.CurrentNsbtx;
            if (currentNsbtx == null || textureIndex < 0 || textureIndex >= currentNsbtx.Textures.Count)
            {
                Core.Logging.AppLogger.Warn($"[NsbtxEditor] Invalid texture index: {textureIndex}");
                return;
            }

            var texInfo = currentNsbtx.Textures[textureIndex];
            Core.Logging.AppLogger.Debug($"[NsbtxEditor] Loading texture {textureIndex}: {currentNsbtx.TextureNames[textureIndex]}");
            Core.Logging.AppLogger.Debug($"[NsbtxEditor] Format: {texInfo.Format}, Dimensions: {texInfo.ActualWidth}x{texInfo.ActualHeight}");
            Core.Logging.AppLogger.Debug($"[NsbtxEditor] Palette index: {paletteIndex}, Palette count: {currentNsbtx.Palettes.Count}");

            // Decode texture to RGBA
            var rgbaData = currentNsbtx.DecodeTextureToRGBA(textureIndex, paletteIndex, _useTiledFormat);
            if (rgbaData == null)
            {
                Core.Logging.AppLogger.Error($"[NsbtxEditor] Failed to decode texture {textureIndex}");
                SetStatus($"Failed to decode texture (unsupported format {texInfo.Format})", new Vector4(1.0f, 0.4f, 0.4f, 1.0f));
                return;
            }

            _currentTextureWidth = texInfo.ActualWidth;
            _currentTextureHeight = texInfo.ActualHeight;

            Core.Logging.AppLogger.Debug($"[NsbtxEditor] Decoded {rgbaData.Length} bytes, expected {_currentTextureWidth * _currentTextureHeight * 4}");

            if (rgbaData.Length != _currentTextureWidth * _currentTextureHeight * 4)
            {
                Core.Logging.AppLogger.Error($"[NsbtxEditor] Data size mismatch! Got {rgbaData.Length}, expected {_currentTextureWidth * _currentTextureHeight * 4}");
                SetStatus("Texture data size mismatch", new Vector4(1.0f, 0.4f, 0.4f, 1.0f));
                return;
            }

            // Create texture using TextureManager (Veldrid-based)
            string textureName = $"nsbtx_{_selectedNsbtxId:D4}_tex{textureIndex}_{currentNsbtx.TextureNames[textureIndex]}";
            _currentTextureHandle = _textureManager.CreateTextureFromRGBA(
                rgbaData,
                _currentTextureWidth,
                _currentTextureHeight,
                textureName
            );

            Core.Logging.AppLogger.Info($"[NsbtxEditor] Successfully loaded texture {textureIndex}");
            SetStatus($"Loaded texture: {currentNsbtx.TextureNames[textureIndex]}", new Vector4(0.4f, 1.0f, 0.4f, 1.0f));
        }
        catch (Exception ex)
        {
            Core.Logging.AppLogger.Error($"[NsbtxEditor] Exception loading texture {textureIndex}: {ex.Message}");
            Core.Logging.AppLogger.Error($"[NsbtxEditor] Stack trace: {ex.StackTrace}");
            SetStatus($"Error loading texture: {ex.Message}", new Vector4(1.0f, 0.4f, 0.4f, 1.0f));

            _currentTextureHandle = IntPtr.Zero;
        }
    }

    private void SetStatus(string message, Vector4 color)
    {
        _statusMessage = message;
        _statusColor = color;
        _statusTimer = 5.0f; // Show for 5 seconds
    }
}
