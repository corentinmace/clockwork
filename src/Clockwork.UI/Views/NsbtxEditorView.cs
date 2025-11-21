using Clockwork.Core;
using Clockwork.Core.Services;
using Clockwork.UI.Icons;
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

    public bool IsVisible { get; set; } = false;

    // UI State
    private int _selectedNsbtxId = 0;
    private string _statusMessage = string.Empty;
    private Vector4 _statusColor = new(1.0f, 1.0f, 1.0f, 1.0f);
    private float _statusTimer = 0.0f;
    private string _searchFilter = string.Empty;

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
                    foreach (var texName in currentNsbtx.TextureNames)
                    {
                        ImGui.Text($"  {FontAwesomeIcons.Image} {texName}");
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
                    foreach (var palName in currentNsbtx.PaletteNames)
                    {
                        ImGui.Text($"  {FontAwesomeIcons.Palette} {palName}");
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

    private void SetStatus(string message, Vector4 color)
    {
        _statusMessage = message;
        _statusColor = color;
        _statusTimer = 5.0f; // Show for 5 seconds
    }
}
