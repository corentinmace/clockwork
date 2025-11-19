using Clockwork.Core;
using Clockwork.Core.Services;
using ImGuiNET;

namespace Clockwork.UI.Views;

/// <summary>
/// View for loading Pokémon ROMs.
/// </summary>
public class RomLoaderView : IView
{
    private readonly ApplicationContext _appContext;
    private RomService? _romService;
    private NdsToolService? _ndsToolService;
    private DialogService? _dialogService;
    private string _romFolderPath = string.Empty;
    private string _ndsFilePath = string.Empty;
    private string _extractOutputPath = string.Empty;
    private string _statusMessage = string.Empty;
    private System.Numerics.Vector4 _statusColor = new(1.0f, 1.0f, 1.0f, 1.0f);
    private string _extractionLog = string.Empty;
    private bool _isExtracting = false;

    public bool IsVisible { get; set; } = false;

    public RomLoaderView(ApplicationContext appContext)
    {
        _appContext = appContext;
        _romService = _appContext.GetService<RomService>();
        _ndsToolService = _appContext.GetService<NdsToolService>();
        _dialogService = _appContext.GetService<DialogService>();
    }

    public void Draw()
    {
        if (!IsVisible) return;

        bool isVisible = IsVisible;
        ImGui.Begin("Load ROM", ref isVisible);

        ImGui.TextColored(new System.Numerics.Vector4(0.4f, 0.7f, 1.0f, 1.0f), "Pokémon ROM Loading");
        ImGui.Separator();
        ImGui.Spacing();

        // Display ROM info if loaded
        if (_romService?.CurrentRom?.IsLoaded == true)
        {
            var rom = _romService.CurrentRom;

            ImGui.TextColored(new System.Numerics.Vector4(0.5f, 0.8f, 0.5f, 1.0f), "ROM Loaded:");
            ImGui.Spacing();

            ImGui.Text($"Game: {rom.GameName}");
            ImGui.Text($"Code: {rom.GameCode}");
            ImGui.Text($"Version: {rom.Version}");
            ImGui.Text($"Language: {rom.Language}");
            ImGui.Text($"Family: {rom.Family}");
            ImGui.Text($"Path: {rom.RomPath}");

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            if (ImGui.Button("Unload ROM", new System.Numerics.Vector2(-1, 30)))
            {
                _romService.UnloadRom();
                _statusMessage = "ROM unloaded";
                _statusColor = new System.Numerics.Vector4(0.5f, 0.8f, 0.5f, 1.0f);
            }
        }
        else
        {
            ImGui.Text("No ROM loaded");
            ImGui.Spacing();

            // Tabs for Extract / Load
            if (ImGui.BeginTabBar("LoadTabs"))
            {
                // Tab: Extract ROM
                if (ImGui.BeginTabItem("Extract ROM (.nds)"))
                {
                    ImGui.Spacing();

                    // Check if ndstool is available
                    if (_ndsToolService?.IsAvailable != true)
                    {
                        ImGui.TextColored(new System.Numerics.Vector4(1.0f, 0.4f, 0.4f, 1.0f),
                            "ndstool.exe is not available!");
                        ImGui.TextWrapped("Download ndstool.exe from DSPRE and place it in the Tools/ folder");
                        ImGui.Text("URL: https://github.com/DS-Pokemon-Rom-Editor/DSPRE/raw/master/DS_Map/Tools/ndstool.exe");
                    }
                    else
                    {
                        ImGui.TextColored(new System.Numerics.Vector4(0.5f, 0.8f, 0.5f, 1.0f),
                            $"ndstool.exe available: {_ndsToolService.NdsToolPath}");
                    }

                    ImGui.Spacing();
                    ImGui.Separator();
                    ImGui.Spacing();

                    // Input: .nds file
                    ImGui.Text("ROM File (.nds):");
                    ImGui.SetNextItemWidth(-80);
                    ImGui.InputText("##ndsfile", ref _ndsFilePath, 500);
                    ImGui.SameLine();
                    if (ImGui.Button("Browse##nds", new System.Numerics.Vector2(70, 0)))
                    {
                        string? selectedFile = _dialogService?.OpenFileDialog(
                            "NDS ROM Files|*.nds|All Files|*.*",
                            "Select NDS ROM File"
                        );
                        if (selectedFile != null)
                        {
                            _ndsFilePath = selectedFile;
                        }
                    }

                    // Input: Output folder
                    ImGui.Text("Output Folder:");
                    ImGui.SetNextItemWidth(-80);
                    ImGui.InputText("##extractoutput", ref _extractOutputPath, 500);
                    ImGui.SameLine();
                    if (ImGui.Button("Browse##out", new System.Numerics.Vector2(70, 0)))
                    {
                        string? selectedFolder = _dialogService?.OpenFolderDialog("Select Output Folder");
                        if (selectedFolder != null)
                        {
                            _extractOutputPath = selectedFolder;
                        }
                    }

                    ImGui.Spacing();

                    // Extract button
                    bool canExtract = _ndsToolService?.IsAvailable == true &&
                                     !string.IsNullOrWhiteSpace(_ndsFilePath) &&
                                     !string.IsNullOrWhiteSpace(_extractOutputPath) &&
                                     !_isExtracting;

                    if (!canExtract)
                    {
                        ImGui.BeginDisabled();
                    }

                    if (ImGui.Button("Extract ROM", new System.Numerics.Vector2(-1, 40)))
                    {
                        _isExtracting = true;
                        _extractionLog = "";

                        bool success = _ndsToolService!.ExtractRom(_ndsFilePath, _extractOutputPath, (msg) =>
                        {
                            _extractionLog += msg + "\n";
                        });

                        _isExtracting = false;

                        if (success)
                        {
                            _statusMessage = "Extraction successful! You can now load the ROM.";
                            _statusColor = new System.Numerics.Vector4(0.5f, 0.8f, 0.5f, 1.0f);
                            _romFolderPath = _extractOutputPath;
                        }
                        else
                        {
                            _statusMessage = "Error during extraction. Check logs.";
                            _statusColor = new System.Numerics.Vector4(1.0f, 0.4f, 0.4f, 1.0f);
                        }
                    }

                    if (!canExtract)
                    {
                        ImGui.EndDisabled();
                    }

                    // Display extraction logs
                    if (!string.IsNullOrEmpty(_extractionLog))
                    {
                        ImGui.Spacing();
                        ImGui.Separator();
                        ImGui.Text("Extraction Logs:");
                        ImGui.BeginChild("ExtractionLogs", new System.Numerics.Vector2(0, 150), ImGuiChildFlags.Borders);
                        ImGui.TextWrapped(_extractionLog);
                        ImGui.EndChild();
                    }

                    ImGui.EndTabItem();
                }

                // Tab: Load Extracted ROM
                if (ImGui.BeginTabItem("Load Extracted ROM"))
                {
                    ImGui.Spacing();
                    ImGui.Separator();
                    ImGui.Spacing();

                    // Input for folder path
                    ImGui.Text("Extracted ROM Folder Path:");
                    ImGui.SetNextItemWidth(-80);
                    ImGui.InputText("##rompath", ref _romFolderPath, 500);
                    ImGui.SameLine();
                    if (ImGui.Button("Browse##rom", new System.Numerics.Vector2(70, 0)))
                    {
                        string? selectedFolder = _dialogService?.OpenFolderDialog("Select Extracted ROM Folder");
                        if (selectedFolder != null)
                        {
                            _romFolderPath = selectedFolder;
                        }
                    }

                    ImGui.Spacing();

                    if (ImGui.Button("Load ROM", new System.Numerics.Vector2(-1, 40)))
                    {
                        if (string.IsNullOrWhiteSpace(_romFolderPath))
                        {
                            _statusMessage = "Error: Please specify a path";
                            _statusColor = new System.Numerics.Vector4(1.0f, 0.4f, 0.4f, 1.0f);
                        }
                        else
                        {
                            bool success = _romService?.LoadRomFromFolder(_romFolderPath) ?? false;
                            if (success)
                            {
                                _statusMessage = $"ROM loaded successfully: {_romService?.CurrentRom?.GameName}";
                                _statusColor = new System.Numerics.Vector4(0.5f, 0.8f, 0.5f, 1.0f);
                            }
                            else
                            {
                                _statusMessage = "Error: Unable to load ROM. Check that header.bin exists.";
                                _statusColor = new System.Numerics.Vector4(1.0f, 0.4f, 0.4f, 1.0f);
                            }
                        }
                    }

                    ImGui.Spacing();
                    ImGui.Separator();
                    ImGui.Spacing();

                    ImGui.TextWrapped("The folder must contain extracted ROM files (header.bin, arm9.bin, etc.)");

                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }
        }

        // Display status message
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
}
