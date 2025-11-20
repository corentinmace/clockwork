using Clockwork.Core;
using Clockwork.Core.Logging;
using Clockwork.Core.Models;
using Clockwork.Core.Services;
using ImGuiNET;
using System.Numerics;

namespace Clockwork.UI.Views;

/// <summary>
/// Address Helper tool - translates memory addresses to overlay files and offsets.
/// </summary>
public class AddressHelperWindow : IView
{
    public bool IsVisible { get; set; }

    private readonly ApplicationContext _appContext;
    private RomService? _romService;

    // UI State
    private string _addressInput = "";
    private uint _searchAddress = 0;
    private List<AddressResult> _results = new();
    private string _statusMessage = "";
    private Vector4 _statusColor = new(1.0f, 1.0f, 1.0f, 1.0f);

    public AddressHelperWindow(ApplicationContext appContext)
    {
        _appContext = appContext;
        _romService = _appContext.GetService<RomService>();
    }

    public void Draw()
    {
        if (!IsVisible) return;

        ImGui.SetNextWindowSize(new Vector2(700, 500), ImGuiCond.FirstUseEver);
        bool isVisible = IsVisible;
        if (!ImGui.Begin("Address Helper", ref isVisible))
        {
            ImGui.End();
            return;
        }

        // Header
        ImGui.TextColored(new Vector4(0.4f, 0.8f, 1.0f, 1.0f), "Address Helper");
        ImGui.Text("Translates memory addresses to overlay files and offsets");
        ImGui.Separator();
        ImGui.Spacing();

        // Check if ROM is loaded
        if (_romService?.CurrentRom?.IsLoaded != true)
        {
            ImGui.TextColored(new Vector4(1.0f, 0.4f, 0.4f, 1.0f), "No ROM loaded. Please load a ROM first.");
            ImGui.End();
            return;
        }

        // Address Input
        ImGui.Text("Memory Address (hex):");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(200);
        if (ImGui.InputText("##address", ref _addressInput, 32, ImGuiInputTextFlags.CharsHexadecimal | ImGuiInputTextFlags.CharsUppercase))
        {
            // Auto-update as user types
        }

        ImGui.SameLine();
        if (ImGui.Button("Search") || ImGui.IsKeyPressed(ImGuiKey.Enter))
        {
            SearchAddress();
        }

        ImGui.SameLine();
        ImGui.TextDisabled("(e.g., 0x02001234)");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Status Message
        if (!string.IsNullOrEmpty(_statusMessage))
        {
            ImGui.TextColored(_statusColor, _statusMessage);
            ImGui.Spacing();
        }

        // Results Table
        if (_results.Count > 0)
        {
            ImGui.Text($"Address: 0x{_searchAddress:X8}");
            ImGui.Spacing();

            if (ImGui.BeginTable("resultsTable", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY))
            {
                // Headers
                ImGui.TableSetupColumn("Location", ImGuiTableColumnFlags.WidthFixed, 150);
                ImGui.TableSetupColumn("Offset", ImGuiTableColumnFlags.WidthFixed, 120);
                ImGui.TableSetupColumn("File", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableHeadersRow();

                // Rows
                foreach (var result in _results)
                {
                    ImGui.TableNextRow();

                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text(result.LocationName);

                    ImGui.TableSetColumnIndex(1);
                    ImGui.Text(result.Offset);

                    ImGui.TableSetColumnIndex(2);
                    ImGui.Text(result.FileName);
                }

                ImGui.EndTable();
            }
        }

        ImGui.End();
        IsVisible = isVisible;
    }

    /// <summary>
    /// Opens the window with a specific address pre-loaded
    /// </summary>
    public void OpenWithAddress(uint address)
    {
        IsVisible = true;
        _addressInput = $"0x{address:X8}";
        _searchAddress = address;
        SearchAddress();
    }

    private void SearchAddress()
    {
        _results.Clear();
        _statusMessage = "";

        // Parse address
        if (!TryParseHexAddress(_addressInput, out uint address))
        {
            _statusMessage = "Invalid address format. Use hex format like 0x02001234";
            _statusColor = new Vector4(1.0f, 0.4f, 0.4f, 1.0f);
            return;
        }

        _searchAddress = address;
        AppLogger.Info($"[AddressHelper] Searching address: 0x{address:X8}");

        // Get ROM paths
        if (!_romService.CurrentRom.GameDirectories.TryGetValue("root", out var romPath))
        {
            _statusMessage = "ROM path not found";
            _statusColor = new Vector4(1.0f, 0.4f, 0.4f, 1.0f);
            return;
        }

        // Check if address is in Synth Overlay range (>= 0x023C8000)
        if (address >= DsConstants.SYNTH_OVERLAY_BASE_ADDRESS)
        {
            CheckSynthOverlay(address);
        }
        else
        {
            // Check ARM9
            CheckARM9(romPath, address);

            // Check overlays
            CheckOverlays(romPath, address);
        }

        // Results
        if (_results.Count == 0)
        {
            _statusMessage = "No matching overlay or section found for this address";
            _statusColor = new Vector4(1.0f, 0.7f, 0.4f, 1.0f);
        }
        else
        {
            _statusMessage = $"Found {_results.Count} possible location(s)";
            _statusColor = new Vector4(0.4f, 1.0f, 0.4f, 1.0f);
        }
    }

    private void CheckARM9(string romPath, uint address)
    {
        string arm9Path = Path.Combine(romPath, "arm9.bin");
        if (!File.Exists(arm9Path))
        {
            return;
        }

        try
        {
            var arm9Info = new FileInfo(arm9Path);
            uint arm9Size = (uint)arm9Info.Length;
            uint arm9End = DsConstants.ARM9_LOAD_ADDRESS + arm9Size;

            if (address >= DsConstants.ARM9_LOAD_ADDRESS && address < arm9End)
            {
                uint offset = address - DsConstants.ARM9_LOAD_ADDRESS;
                _results.Add(new AddressResult
                {
                    LocationName = "ARM9",
                    Offset = $"0x{offset:X}",
                    FileName = "arm9.bin"
                });
                AppLogger.Debug($"[AddressHelper] Found in ARM9 at offset 0x{offset:X}");
            }
        }
        catch (Exception ex)
        {
            AppLogger.Error($"[AddressHelper] Error checking ARM9: {ex.Message}");
        }
    }

    private void CheckOverlays(string romPath, uint address)
    {
        // Try to find overlay table
        string overlayTablePath = Path.Combine(romPath, "y9.bin");
        if (!File.Exists(overlayTablePath))
        {
            AppLogger.Debug("[AddressHelper] No overlay table found (y9.bin)");
            return;
        }

        try
        {
            byte[] overlayTable = File.ReadAllBytes(overlayTablePath);
            int overlayCount = overlayTable.Length / 32; // Each overlay entry is 32 bytes

            for (int i = 0; i < overlayCount; i++)
            {
                int offset = i * 32;

                // Read overlay info (little-endian)
                uint overlayId = BitConverter.ToUInt32(overlayTable, offset + 0);
                uint ramAddress = BitConverter.ToUInt32(overlayTable, offset + 4);
                uint ramSize = BitConverter.ToUInt32(overlayTable, offset + 8);
                uint ramEnd = ramAddress + ramSize;

                // Check if address falls in this overlay's RAM range
                if (address >= ramAddress && address < ramEnd)
                {
                    uint localOffset = address - ramAddress;
                    string overlayFileName = $"overlay_{overlayId:D4}.bin";

                    _results.Add(new AddressResult
                    {
                        LocationName = $"Overlay {overlayId}",
                        Offset = $"0x{localOffset:X}",
                        FileName = overlayFileName
                    });

                    AppLogger.Debug($"[AddressHelper] Found in Overlay {overlayId} at offset 0x{localOffset:X}");
                }
            }
        }
        catch (Exception ex)
        {
            AppLogger.Error($"[AddressHelper] Error checking overlays: {ex.Message}");
        }
    }

    private void CheckSynthOverlay(uint address)
    {
        if (!_romService.CurrentRom.GameDirectories.TryGetValue("unpacked", out var unpackedPath))
        {
            AppLogger.Warn("[AddressHelper] Unpacked directory not found");
            return;
        }

        // SynthOverlay is specifically the file 0065 in unpacked/synthOverlay/
        string synthOverlayPath = Path.Combine(unpackedPath, "synthOverlay", "0065");

        if (!File.Exists(synthOverlayPath))
        {
            AppLogger.Warn($"[AddressHelper] SynthOverlay file not found at {synthOverlayPath}");
            return;
        }

        try
        {
            // Calculate offset from synth overlay base address
            uint offset = address - DsConstants.SYNTH_OVERLAY_BASE_ADDRESS;

            var fileInfo = new FileInfo(synthOverlayPath);
            uint fileSize = (uint)fileInfo.Length;

            // Check if offset is within file bounds
            if (offset < fileSize)
            {
                _results.Add(new AddressResult
                {
                    LocationName = "SynthOverlay",
                    Offset = $"0x{offset:X}",
                    FileName = "0065"
                });

                AppLogger.Debug($"[AddressHelper] Found in SynthOverlay (0065) at offset 0x{offset:X}");
            }
            else
            {
                // Address is in synth overlay range but beyond file size
                _results.Add(new AddressResult
                {
                    LocationName = "SynthOverlay",
                    Offset = $"0x{offset:X} (beyond file size)",
                    FileName = "0065"
                });
                AppLogger.Warn($"[AddressHelper] Address in SynthOverlay range but offset 0x{offset:X} exceeds file size {fileSize}");
            }
        }
        catch (Exception ex)
        {
            AppLogger.Error($"[AddressHelper] Error checking SynthOverlay: {ex.Message}");
        }
    }

    private bool TryParseHexAddress(string input, out uint address)
    {
        address = 0;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        // Remove common prefixes
        input = input.Trim();
        if (input.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            input = input.Substring(2);
        if (input.StartsWith("$"))
            input = input.Substring(1);

        return uint.TryParse(input, System.Globalization.NumberStyles.HexNumber, null, out address);
    }

    private class AddressResult
    {
        public string LocationName { get; set; } = "";
        public string Offset { get; set; } = "";
        public string FileName { get; set; } = "";
    }
}
