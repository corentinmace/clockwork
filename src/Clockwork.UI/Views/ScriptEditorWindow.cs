using Clockwork.Core;
using Clockwork.Core.Formats.NDS.Scripts;
using Clockwork.Core.Services;
using ImGuiNET;
using System.Numerics;
using System.IO;

namespace Clockwork.UI.Views;

/// <summary>
/// Script editor window for editing NDS scripts
/// </summary>
public class ScriptEditorWindow : IView
{
    private readonly ApplicationContext _appContext;
    private readonly RomService? _romService;

    public bool IsVisible { get; set; } = false;

    // Script file management
    private int _scriptCount = 0;
    private string[] _scriptFileNames = Array.Empty<string>();
    private int _selectedFileIndex = -1;
    private int _currentFileID = -1;

    // Tab management
    private int _selectedTab = 0; // 0 = Scripts, 1 = Functions, 2 = Actions
    private readonly string[] _tabNames = { "Scripts", "Functions", "Actions" };

    // Text content for each tab (loaded from export_script files)
    private string _scriptTabText = string.Empty;
    private string _functionTabText = string.Empty;
    private string _actionTabText = string.Empty;
    private bool _isDirty = false;

    // Paths
    private string _scriptsDir = string.Empty;  // unpacked/scripts (for listing)
    private string _exportDir = string.Empty;   // script_export (for loading/saving)

    // Status
    private string _statusMessage = string.Empty;
    private Vector4 _statusColor = new(1.0f, 1.0f, 1.0f, 1.0f);
    private float _statusTimer = 0f;

    public ScriptEditorWindow(ApplicationContext appContext)
    {
        _appContext = appContext;
        _romService = appContext.GetService<RomService>();
    }

    public void Draw()
    {
        if (!IsVisible)
            return;

        // Update status timer
        if (_statusTimer > 0f)
        {
            _statusTimer -= ImGui.GetIO().DeltaTime;
        }

        // Refresh file list when ROM loads
        if (_scriptCount == 0 && _romService?.CurrentRom?.IsLoaded == true)
        {
            RefreshScriptFilesList();
        }

        bool isVisible = IsVisible;
        ImGui.SetNextWindowSize(new Vector2(1200, 800), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Script Editor", ref isVisible, ImGuiWindowFlags.MenuBar))
        {
            DrawMenuBar();
            DrawToolbar();

            ImGui.Separator();

            if (_currentFileID >= 0)
            {
                DrawEditorArea();
            }
            else
            {
                ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f),
                    "No script file loaded. Select a file from the toolbar above.");
            }

            // Status bar
            ImGui.Separator();
            DrawStatusBar();
        }
        ImGui.End();

        IsVisible = isVisible;
    }

    private void DrawMenuBar()
    {
        if (ImGui.BeginMenuBar())
        {
            if (ImGui.BeginMenu("File"))
            {
                if (ImGui.MenuItem("Save", "Ctrl+S", false, _currentFileID >= 0 && _isDirty))
                {
                    SaveCurrentFile();
                }

                ImGui.Separator();

                if (ImGui.MenuItem("Close"))
                {
                    IsVisible = false;
                }

                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Edit"))
            {
                bool hasSelection = false; // TODO: track text selection
                if (ImGui.MenuItem("Cut", "Ctrl+X", false, hasSelection)) { }
                if (ImGui.MenuItem("Copy", "Ctrl+C", false, hasSelection)) { }
                if (ImGui.MenuItem("Paste", "Ctrl+V")) { }

                ImGui.EndMenu();
            }

            ImGui.EndMenuBar();
        }
    }

    private void DrawToolbar()
    {
        // File selector
        if (_romService?.CurrentRom?.IsLoaded == true && _scriptFileNames.Length > 0)
        {
            ImGui.Text("Script File:");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(200);
            if (ImGui.Combo("##scriptfile", ref _selectedFileIndex, _scriptFileNames, _scriptFileNames.Length))
            {
                if (_selectedFileIndex >= 0 && _selectedFileIndex < _scriptCount)
                {
                    LoadScriptFile(_selectedFileIndex);
                }
            }
        }
        else if (_romService?.CurrentRom?.IsLoaded == true)
        {
            ImGui.TextColored(new Vector4(1.0f, 0.6f, 0.0f, 1.0f), "No script files found");
            ImGui.SameLine();
            if (ImGui.Button("Refresh"))
            {
                RefreshScriptFilesList();
            }
        }
        else
        {
            ImGui.TextColored(new Vector4(1.0f, 0.6f, 0.0f, 1.0f), "No ROM loaded");
        }

        ImGui.SameLine();
        ImGui.Spacing();
        ImGui.SameLine();

        // Save button
        if (_currentFileID >= 0 && _isDirty)
        {
            if (ImGui.Button("Save"))
            {
                SaveCurrentFile();
            }
            ImGui.SameLine();
        }

        // Compile button
        if (_currentFileID >= 0)
        {
            if (ImGui.Button("Compile & Save to ROM"))
            {
                CompileAndSaveToROM();
            }
        }
    }

    private void DrawEditorArea()
    {
        if (_currentFileID < 0) return;

        // Tabs for Scripts/Functions/Actions
        if (ImGui.BeginTabBar("ScriptTabs"))
        {
            if (ImGui.BeginTabItem("Scripts"))
            {
                _selectedTab = 0;
                DrawScriptTextEditor(ref _scriptTabText);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Functions"))
            {
                _selectedTab = 1;
                DrawScriptTextEditor(ref _functionTabText);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Actions"))
            {
                _selectedTab = 2;
                DrawScriptTextEditor(ref _actionTabText);
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }

    private void DrawScriptTextEditor(ref string text)
    {
        float availableHeight = ImGui.GetContentRegionAvail().Y;

        // TODO: Add syntax highlighting here
        ImGui.InputTextMultiline("##scripttext", ref text, 100000,
            new Vector2(-1, availableHeight),
            ImGuiInputTextFlags.AllowTabInput);

        if (ImGui.IsItemEdited())
        {
            _isDirty = true;
        }
    }

    private void DrawStatusBar()
    {
        if (!string.IsNullOrEmpty(_statusMessage) && _statusTimer > 0f)
        {
            ImGui.TextColored(_statusColor, _statusMessage);
        }
        else if (_currentFileID >= 0)
        {
            ImGui.Text($"Script File {_currentFileID} ({_tabNames[_selectedTab]})");
            if (_isDirty)
            {
                ImGui.SameLine();
                ImGui.TextColored(new Vector4(1.0f, 0.7f, 0.3f, 1.0f), " (Modified)");
            }
        }
    }

    private void RefreshScriptFilesList()
    {
        _scriptCount = 0;
        _scriptFileNames = Array.Empty<string>();

        if (_romService?.CurrentRom?.GameDirectories == null)
            return;

        try
        {
            if (_romService.CurrentRom.GameDirectories.TryGetValue("root", out var rootPath))
            {
                // Get unpacked/scripts directory (for counting scripts)
                _scriptsDir = Path.Combine(rootPath, "unpacked", "scripts");

                // Get script_export directory (for loading/saving text files)
                _exportDir = Path.Combine(rootPath, "..", "script_export");
                _exportDir = Path.GetFullPath(_exportDir);

                if (Directory.Exists(_scriptsDir))
                {
                    // Count script files in ROM directory
                    var scriptFiles = Directory.GetFiles(_scriptsDir);
                    _scriptCount = scriptFiles.Length;

                    // Create display names: "Script File 0", "Script File 1", etc.
                    _scriptFileNames = new string[_scriptCount];
                    for (int i = 0; i < _scriptCount; i++)
                    {
                        _scriptFileNames[i] = $"Script File {i}";
                    }

                    // Ensure export directory exists
                    Directory.CreateDirectory(_exportDir);

                    SetStatus($"Found {_scriptCount} script files", new Vector4(0.5f, 0.8f, 0.5f, 1.0f));
                }
                else
                {
                    SetStatus("unpacked/scripts directory not found", new Vector4(1.0f, 0.6f, 0.0f, 1.0f));
                }
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Error listing script files: {ex.Message}", new Vector4(1.0f, 0.4f, 0.4f, 1.0f));
        }
    }

    private void LoadScriptFile(int fileID)
    {
        try
        {
            _currentFileID = fileID;

            // Build file paths for the 3 export files
            string fileIDStr = fileID.ToString("D4");
            string scriptPath = Path.Combine(_exportDir, $"{fileIDStr}_script.script");
            string functionPath = Path.Combine(_exportDir, $"{fileIDStr}_func.script");
            string actionPath = Path.Combine(_exportDir, $"{fileIDStr}_action.action");

            // Load text from export files (or create empty if they don't exist)
            _scriptTabText = File.Exists(scriptPath) ? File.ReadAllText(scriptPath) : string.Empty;
            _functionTabText = File.Exists(functionPath) ? File.ReadAllText(functionPath) : string.Empty;
            _actionTabText = File.Exists(actionPath) ? File.ReadAllText(actionPath) : string.Empty;

            _isDirty = false;

            SetStatus($"Loaded Script File {fileID}", new Vector4(0.5f, 0.8f, 0.5f, 1.0f));
        }
        catch (Exception ex)
        {
            SetStatus($"Error loading file: {ex.Message}", new Vector4(1.0f, 0.4f, 0.4f, 1.0f));
        }
    }

    private void CompileAndSaveToROM()
    {
        if (_currentFileID < 0) return;

        try
        {
            // Compile text to binary
            var scriptFile = ScriptCompiler.CompileScriptFile(
                _currentFileID,
                _scriptTabText,
                _functionTabText,
                _actionTabText
            );

            // Save to ROM unpacked directory
            string binaryPath = Path.Combine(_scriptsDir, _currentFileID.ToString());
            scriptFile.SaveToFile(binaryPath);

            _isDirty = false;
            SetStatus($"Compiled and saved Script File {_currentFileID} to ROM", new Vector4(0.5f, 0.8f, 0.5f, 1.0f));
        }
        catch (Exception ex)
        {
            SetStatus($"Compilation error: {ex.Message}", new Vector4(1.0f, 0.4f, 0.4f, 1.0f));
        }
    }

    private void SaveCurrentFile()
    {
        if (_currentFileID < 0) return;

        try
        {
            // Ensure export directory exists
            Directory.CreateDirectory(_exportDir);

            // Build file paths for the 3 export files
            string fileIDStr = _currentFileID.ToString("D4");
            string scriptPath = Path.Combine(_exportDir, $"{fileIDStr}_script.script");
            string functionPath = Path.Combine(_exportDir, $"{fileIDStr}_func.script");
            string actionPath = Path.Combine(_exportDir, $"{fileIDStr}_action.action");

            // Save all 3 text files
            File.WriteAllText(scriptPath, _scriptTabText);
            File.WriteAllText(functionPath, _functionTabText);
            File.WriteAllText(actionPath, _actionTabText);

            _isDirty = false;
            SetStatus($"Saved Script File {_currentFileID} to export directory", new Vector4(0.5f, 0.8f, 0.5f, 1.0f));
        }
        catch (Exception ex)
        {
            SetStatus($"Error saving file: {ex.Message}", new Vector4(1.0f, 0.4f, 0.4f, 1.0f));
        }
    }

    private void SetStatus(string message, Vector4 color)
    {
        _statusMessage = message;
        _statusColor = color;
        _statusTimer = 5f;
    }
}
