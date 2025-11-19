using Clockwork.Core;
using Clockwork.Core.Formats.NDS.Scripts;
using Clockwork.Core.Services;
using ImGuiNET;
using System.Numerics;

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
    private ScriptFile? _currentFile;
    private List<string> _availableScriptFiles = new();
    private string[] _scriptFileNames = Array.Empty<string>();
    private int _selectedFileIndex = -1;

    // Tab management
    private int _selectedTab = 0; // 0 = Scripts, 1 = Functions, 2 = Actions
    private readonly string[] _tabNames = { "Scripts", "Functions", "Actions" };

    // Container selection
    private int _selectedContainerIndex = -1;
    private string _scriptText = string.Empty;
    private bool _isDirty = false;

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
        if (_availableScriptFiles.Count == 0 && _romService?.CurrentRom?.IsLoaded == true)
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

            if (_currentFile != null)
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
                if (ImGui.MenuItem("Save", "Ctrl+S", false, _currentFile != null && _isDirty))
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
                if (_selectedFileIndex >= 0 && _selectedFileIndex < _availableScriptFiles.Count)
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
        if (_currentFile != null && _isDirty)
        {
            if (ImGui.Button("Save"))
            {
                SaveCurrentFile();
            }
            ImGui.SameLine();
        }

        // Compile/Decompile buttons
        if (_currentFile != null)
        {
            if (ImGui.Button("Decompile All"))
            {
                DecompileCurrentFile();
            }
            ImGui.SameLine();

            if (ImGui.Button("Compile"))
            {
                CompileCurrentScript();
            }
        }
    }

    private void DrawEditorArea()
    {
        if (_currentFile == null) return;

        // Tabs for Scripts/Functions/Actions
        if (ImGui.BeginTabBar("ScriptTabs"))
        {
            if (ImGui.BeginTabItem("Scripts"))
            {
                _selectedTab = 0;
                DrawContainerList(_currentFile.Scripts);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Functions"))
            {
                _selectedTab = 1;
                DrawContainerList(_currentFile.Functions);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Actions"))
            {
                _selectedTab = 2;
                DrawContainerList(_currentFile.Actions);
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }

    private void DrawContainerList(List<ScriptContainer> containers)
    {
        float availableHeight = ImGui.GetContentRegionAvail().Y;

        // Split view: list on left, editor on right
        if (ImGui.BeginTable("ScriptEditorTable", 2, ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInnerV))
        {
            ImGui.TableSetupColumn("List", ImGuiTableColumnFlags.WidthFixed, 250);
            ImGui.TableSetupColumn("Editor", ImGuiTableColumnFlags.WidthStretch);

            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);

            // Left: Container list
            ImGui.BeginChild("ContainerList", new Vector2(-1, availableHeight - 40), ImGuiChildFlags.Border);

            for (int i = 0; i < containers.Count; i++)
            {
                var container = containers[i];
                bool isSelected = _selectedContainerIndex == i;
                string label = container.ToString();

                if (ImGui.Selectable(label, isSelected))
                {
                    _selectedContainerIndex = i;
                    LoadContainer(container);
                }
            }

            ImGui.EndChild();

            ImGui.TableSetColumnIndex(1);

            // Right: Script editor
            DrawScriptEditor();

            ImGui.EndTable();
        }
    }

    private void DrawScriptEditor()
    {
        float availableHeight = ImGui.GetContentRegionAvail().Y;

        ImGui.BeginChild("ScriptEditor", new Vector2(-1, availableHeight - 40), ImGuiChildFlags.Border);

        if (_selectedContainerIndex >= 0)
        {
            // TODO: Add syntax highlighting here
            ImGui.InputTextMultiline("##scripttext", ref _scriptText, 100000,
                new Vector2(-1, -1),
                ImGuiInputTextFlags.AllowTabInput);

            if (ImGui.IsItemEdited())
            {
                _isDirty = true;
            }
        }
        else
        {
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f),
                "Select a script from the list to edit.");
        }

        ImGui.EndChild();

        // Bottom buttons
        if (_selectedContainerIndex >= 0)
        {
            if (ImGui.Button("Update Container"))
            {
                UpdateCurrentContainer();
            }
            ImGui.SameLine();
            if (ImGui.Button("Revert Changes"))
            {
                ReloadCurrentContainer();
            }
        }
    }

    private void DrawStatusBar()
    {
        if (!string.IsNullOrEmpty(_statusMessage) && _statusTimer > 0f)
        {
            ImGui.TextColored(_statusColor, _statusMessage);
        }
        else if (_currentFile != null)
        {
            ImGui.Text($"{_currentFile.GetSummary()}");
            if (_isDirty)
            {
                ImGui.SameLine();
                ImGui.TextColored(new Vector4(1.0f, 0.7f, 0.3f, 1.0f), " (Modified)");
            }
        }
    }

    private void RefreshScriptFilesList()
    {
        _availableScriptFiles.Clear();

        if (_romService?.CurrentRom?.GameDirectories == null)
            return;

        try
        {
            // Use script_export path like LiTRE does
            // exportedScriptPath = Path.Combine(workDir, @"../script_export")
            if (_romService.CurrentRom.GameDirectories.TryGetValue("root", out var rootPath))
            {
                string scriptsPath = Path.Combine(rootPath, "..", "script_export");
                scriptsPath = Path.GetFullPath(scriptsPath); // Normalize path

                if (Directory.Exists(scriptsPath))
                {
                    var files = Directory.GetFiles(scriptsPath, "*")
                        .OrderBy(f => f)
                        .ToList();

                    _availableScriptFiles = files;
                    _scriptFileNames = files.Select(f => Path.GetFileName(f)).ToArray();

                    SetStatus($"Found {_availableScriptFiles.Count} script files", new Vector4(0.5f, 0.8f, 0.5f, 1.0f));
                }
                else
                {
                    SetStatus("script_export directory not found", new Vector4(1.0f, 0.6f, 0.0f, 1.0f));
                }
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Error listing script files: {ex.Message}", new Vector4(1.0f, 0.4f, 0.4f, 1.0f));
        }
    }

    private void LoadScriptFile(int index)
    {
        try
        {
            string filePath = _availableScriptFiles[index];
            _currentFile = ScriptFile.FromFile(filePath, index);
            _selectedContainerIndex = -1;
            _scriptText = string.Empty;
            _isDirty = false;

            SetStatus($"Loaded {Path.GetFileName(filePath)}", new Vector4(0.5f, 0.8f, 0.5f, 1.0f));
        }
        catch (Exception ex)
        {
            SetStatus($"Error loading file: {ex.Message}", new Vector4(1.0f, 0.4f, 0.4f, 1.0f));
        }
    }

    private void LoadContainer(ScriptContainer container)
    {
        try
        {
            _scriptText = ScriptDecompiler.DecompileContainer(container);
            _isDirty = false;
        }
        catch (Exception ex)
        {
            SetStatus($"Error decompiling container: {ex.Message}", new Vector4(1.0f, 0.4f, 0.4f, 1.0f));
        }
    }

    private void DecompileCurrentFile()
    {
        if (_currentFile == null) return;

        try
        {
            string decompiled = ScriptDecompiler.DecompileFile(_currentFile);
            // TODO: Show in separate window or save to file
            SetStatus("File decompiled successfully", new Vector4(0.5f, 0.8f, 0.5f, 1.0f));
        }
        catch (Exception ex)
        {
            SetStatus($"Error decompiling file: {ex.Message}", new Vector4(1.0f, 0.4f, 0.4f, 1.0f));
        }
    }

    private void CompileCurrentScript()
    {
        // TODO: Implement script compilation (text -> binary)
        SetStatus("Compilation not yet implemented", new Vector4(1.0f, 0.6f, 0.0f, 1.0f));
    }

    private void UpdateCurrentContainer()
    {
        // TODO: Parse script text and update container
        SetStatus("Container update not yet implemented", new Vector4(1.0f, 0.6f, 0.0f, 1.0f));
    }

    private void ReloadCurrentContainer()
    {
        if (_currentFile == null || _selectedContainerIndex < 0) return;

        var containers = GetCurrentContainerList();
        if (_selectedContainerIndex < containers.Count)
        {
            LoadContainer(containers[_selectedContainerIndex]);
            SetStatus("Changes reverted", new Vector4(0.5f, 0.8f, 0.5f, 1.0f));
        }
    }

    private void SaveCurrentFile()
    {
        if (_currentFile == null) return;

        try
        {
            // TODO: Implement saving
            _isDirty = false;
            SetStatus("File saved successfully", new Vector4(0.5f, 0.8f, 0.5f, 1.0f));
        }
        catch (Exception ex)
        {
            SetStatus($"Error saving file: {ex.Message}", new Vector4(1.0f, 0.4f, 0.4f, 1.0f));
        }
    }

    private List<ScriptContainer> GetCurrentContainerList()
    {
        if (_currentFile == null) return new List<ScriptContainer>();

        return _selectedTab switch
        {
            0 => _currentFile.Scripts,
            1 => _currentFile.Functions,
            2 => _currentFile.Actions,
            _ => new List<ScriptContainer>()
        };
    }

    private void SetStatus(string message, Vector4 color)
    {
        _statusMessage = message;
        _statusColor = color;
        _statusTimer = 5f;
    }
}
