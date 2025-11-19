using Clockwork.Core;
using Clockwork.Core.Logging;
using Clockwork.Core.Services;
using ImGuiNET;
using System.Numerics;

namespace Clockwork.UI.Views;

/// <summary>
/// Script Command Table Helper - displays script command addresses from overlay 65 (synth overlay).
/// </summary>
public class ScrcmdTableHelperWindow : IView
{
    public bool IsVisible { get; set; }

    private readonly ApplicationContext _appContext;
    private RomService? _romService;
    private AddressHelperWindow? _addressHelperWindow;

    // Script command table configuration
    private const int OVERLAY_ID = 65;
    private const int OFFSET_START = 0x17270; // 94,832 bytes
    private const int OFFSET_END = 0x17FF4;   // 98,292 bytes

    // UI State
    private List<ScriptCommand> _commands = new();
    private string _searchCommandId = "";
    private int _selectedIndex = -1;
    private string _statusMessage = "";
    private Vector4 _statusColor = new(1.0f, 1.0f, 1.0f, 1.0f);
    private bool _isLoaded = false;

    public ScrcmdTableHelperWindow(ApplicationContext appContext)
    {
        _appContext = appContext;
        _romService = _appContext.GetService<RomService>();
    }

    /// <summary>
    /// Set reference to AddressHelperWindow for double-click integration
    /// </summary>
    public void SetAddressHelperWindow(AddressHelperWindow addressHelper)
    {
        _addressHelperWindow = addressHelper;
    }

    public void Draw()
    {
        if (!IsVisible) return;

        ImGui.SetNextWindowSize(new Vector2(800, 600), ImGuiCond.FirstUseEver);
        if (!ImGui.Begin("Script Command Table Helper", ref IsVisible))
        {
            ImGui.End();
            return;
        }

        // Header
        ImGui.TextColored(new Vector4(0.4f, 0.8f, 1.0f, 1.0f), "Script Command Table Helper");
        ImGui.Text($"Displays script command addresses from Overlay {OVERLAY_ID}");
        ImGui.Separator();
        ImGui.Spacing();

        // Check if ROM is loaded
        if (_romService?.CurrentRom?.IsLoaded != true)
        {
            ImGui.TextColored(new Vector4(1.0f, 0.4f, 0.4f, 1.0f), "No ROM loaded. Please load a ROM first.");
            ImGui.End();
            return;
        }

        // Load button
        if (!_isLoaded)
        {
            if (ImGui.Button("Load Script Command Table"))
            {
                LoadScriptCommandTable();
            }

            if (!string.IsNullOrEmpty(_statusMessage))
            {
                ImGui.Spacing();
                ImGui.TextColored(_statusColor, _statusMessage);
            }

            ImGui.End();
            return;
        }

        // Search bar
        ImGui.Text("Search Command ID:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(150);
        ImGui.InputText("##search", ref _searchCommandId, 16, ImGuiInputTextFlags.CharsHexadecimal | ImGuiInputTextFlags.CharsUppercase);
        ImGui.SameLine();
        if (ImGui.Button("Find"))
        {
            FindCommandId();
        }
        ImGui.SameLine();
        ImGui.TextDisabled("(e.g., 000A)");

        ImGui.Spacing();

        // Info
        ImGui.Text($"Total Commands: {_commands.Count}");
        ImGui.SameLine(200);
        ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f), "Double-click a row to open Address Helper");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Status message
        if (!string.IsNullOrEmpty(_statusMessage))
        {
            ImGui.TextColored(_statusColor, _statusMessage);
            ImGui.Spacing();
        }

        // Commands Table
        if (ImGui.BeginTable("commandsTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY))
        {
            // Headers
            ImGui.TableSetupColumn("Command ID", ImGuiTableColumnFlags.WidthFixed, 120);
            ImGui.TableSetupColumn("Offset", ImGuiTableColumnFlags.WidthFixed, 120);
            ImGui.TableSetupColumn("Address", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableHeadersRow();

            // Rows
            for (int i = 0; i < _commands.Count; i++)
            {
                var cmd = _commands[i];
                ImGui.TableNextRow();

                bool isSelected = _selectedIndex == i;
                if (isSelected)
                {
                    ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(new Vector4(0.3f, 0.5f, 0.8f, 0.3f)));
                }

                // Command ID
                ImGui.TableSetColumnIndex(0);
                if (ImGui.Selectable($"{cmd.CommandId}##row{i}", isSelected, ImGuiSelectableFlags.SpanAllColumns))
                {
                    _selectedIndex = i;
                }

                // Double-click to open Address Helper
                if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left))
                {
                    OpenAddressHelper(cmd.Address);
                }

                // Offset
                ImGui.TableSetColumnIndex(1);
                ImGui.Text(cmd.Offset);

                // Address
                ImGui.TableSetColumnIndex(2);
                ImGui.Text(cmd.Address);
            }

            ImGui.EndTable();
        }

        ImGui.End();
    }

    private void LoadScriptCommandTable()
    {
        _commands.Clear();
        _statusMessage = "";
        _isLoaded = false;

        if (!_romService.CurrentRom.GameDirectories.TryGetValue("root", out var romPath))
        {
            _statusMessage = "ROM path not found";
            _statusColor = new Vector4(1.0f, 0.4f, 0.4f, 1.0f);
            return;
        }

        // Try to find overlay 65 (synth overlay)
        string overlayPath = Path.Combine(romPath, $"overlay_{OVERLAY_ID:D4}.bin");

        // Alternative naming conventions
        if (!File.Exists(overlayPath))
        {
            overlayPath = Path.Combine(romPath, "overlay", $"overlay_{OVERLAY_ID:D4}.bin");
        }
        if (!File.Exists(overlayPath))
        {
            overlayPath = Path.Combine(romPath, $"overlay{OVERLAY_ID}.bin");
        }

        if (!File.Exists(overlayPath))
        {
            _statusMessage = $"Overlay {OVERLAY_ID} not found. Expected at: overlay_{OVERLAY_ID:D4}.bin";
            _statusColor = new Vector4(1.0f, 0.4f, 0.4f, 1.0f);
            AppLogger.Error($"[ScrcmdTableHelper] Overlay {OVERLAY_ID} not found");
            return;
        }

        try
        {
            byte[] overlayData = File.ReadAllBytes(overlayPath);

            if (overlayData.Length < OFFSET_END)
            {
                _statusMessage = $"Overlay file is too small. Expected at least {OFFSET_END} bytes, got {overlayData.Length}";
                _statusColor = new Vector4(1.0f, 0.4f, 0.4f, 1.0f);
                return;
            }

            // Read 4-byte addresses from the specified range
            int commandIndex = 0;
            for (int offset = OFFSET_START; offset < OFFSET_END; offset += 4)
            {
                uint address = BitConverter.ToUInt32(overlayData, offset);

                _commands.Add(new ScriptCommand
                {
                    CommandId = $"{commandIndex:X4}",
                    Offset = $"0x{offset:X}",
                    Address = $"0x{address:X8}"
                });

                commandIndex++;
            }

            _isLoaded = true;
            _statusMessage = $"Successfully loaded {_commands.Count} script commands";
            _statusColor = new Vector4(0.4f, 1.0f, 0.4f, 1.0f);
            AppLogger.Info($"[ScrcmdTableHelper] Loaded {_commands.Count} script commands from overlay {OVERLAY_ID}");
        }
        catch (Exception ex)
        {
            _statusMessage = $"Error loading overlay: {ex.Message}";
            _statusColor = new Vector4(1.0f, 0.4f, 0.4f, 1.0f);
            AppLogger.Error($"[ScrcmdTableHelper] Error: {ex.Message}");
        }
    }

    private void FindCommandId()
    {
        if (string.IsNullOrWhiteSpace(_searchCommandId))
        {
            _statusMessage = "Please enter a command ID to search";
            _statusColor = new Vector4(1.0f, 0.7f, 0.4f, 1.0f);
            return;
        }

        string searchId = _searchCommandId.Trim().ToUpper();
        if (!searchId.StartsWith("0x"))
        {
            // Pad to 4 digits if needed
            while (searchId.Length < 4)
                searchId = "0" + searchId;
        }
        else
        {
            searchId = searchId.Substring(2);
        }

        for (int i = 0; i < _commands.Count; i++)
        {
            if (_commands[i].CommandId.Equals(searchId, StringComparison.OrdinalIgnoreCase))
            {
                _selectedIndex = i;
                _statusMessage = $"Found command {searchId} at index {i}";
                _statusColor = new Vector4(0.4f, 1.0f, 0.4f, 1.0f);
                AppLogger.Debug($"[ScrcmdTableHelper] Found command {searchId} at index {i}");
                return;
            }
        }

        _statusMessage = $"Command ID {searchId} not found";
        _statusColor = new Vector4(1.0f, 0.4f, 0.4f, 1.0f);
    }

    private void OpenAddressHelper(string addressStr)
    {
        if (_addressHelperWindow == null)
        {
            _statusMessage = "Address Helper not available";
            _statusColor = new Vector4(1.0f, 0.4f, 0.4f, 1.0f);
            return;
        }

        // Parse address (remove 0x prefix)
        string addr = addressStr.Replace("0x", "").Replace("0X", "");
        if (uint.TryParse(addr, System.Globalization.NumberStyles.HexNumber, null, out uint address))
        {
            _addressHelperWindow.OpenWithAddress(address);
            AppLogger.Info($"[ScrcmdTableHelper] Opening Address Helper with address {addressStr}");
        }
        else
        {
            _statusMessage = "Failed to parse address";
            _statusColor = new Vector4(1.0f, 0.4f, 0.4f, 1.0f);
        }
    }

    private class ScriptCommand
    {
        public string CommandId { get; set; } = "";
        public string Offset { get; set; } = "";
        public string Address { get; set; } = "";
    }
}
