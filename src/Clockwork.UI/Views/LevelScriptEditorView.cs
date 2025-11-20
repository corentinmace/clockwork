using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Clockwork.Core;
using Clockwork.Core.Logging;
using Clockwork.Core.Models.LevelScript;
using Clockwork.Core.Services;
using Clockwork.UI.Icons;
using ImGuiNET;

namespace Clockwork.UI.Views;

/// <summary>
/// Level Script Editor View - Edit level script triggers
/// </summary>
public class LevelScriptEditorView : IView
{
    public bool IsVisible { get; set; } = false;

    private readonly ApplicationContext _appContext;
    private LevelScriptService? _levelScriptService;
    private DialogService? _dialogService;

    // UI State
    private int _selectedScriptId = 0;
    private int _selectedTriggerIndex = -1;
    private string _statusMessage = "";
    private Vector4 _statusColor = new(1, 1, 1, 1);

    // Add trigger UI
    private int _selectedTriggerType = 0; // 0 = Map Load, 1 = Variable Value
    private int _newMapLoadScript = 0;
    private int _newVarNumber = 0;
    private int _newVarValue = 0;
    private int _newVarScript = 0;

    // Display format
    private int _displayFormat = 0; // 0 = Auto, 1 = Hex, 2 = Decimal

    public LevelScriptEditorView(ApplicationContext appContext)
    {
        _appContext = appContext;
    }

    public void Initialize(ApplicationContext appContext)
    {
        _levelScriptService = appContext.GetService<LevelScriptService>();
        _dialogService = appContext.GetService<DialogService>();

        if (_levelScriptService != null)
        {
            _levelScriptService.LoadAvailableScripts();
        }
    }

    public void Draw()
    {
        if (!IsVisible) return;

        ImGui.SetNextWindowSize(new Vector2(900, 700), ImGuiCond.FirstUseEver);

        bool isVisible = IsVisible;
        if (ImGui.Begin($"{FontAwesomeIcons.Terminal}  Level Script Editor", ref isVisible))
        {
            // Check services
            if (_levelScriptService == null)
            {
                ImGui.TextColored(new Vector4(1, 0.3f, 0.3f, 1), "Level Script Service not available");
                ImGui.End();
                return;
            }

            DrawToolbar();
            ImGui.Separator();

            // Two column layout
            if (ImGui.BeginTable("LevelScriptEditorLayout", 2, ImGuiTableFlags.Resizable))
            {
                ImGui.TableSetupColumn("Scripts", ImGuiTableColumnFlags.WidthFixed, 250);
                ImGui.TableSetupColumn("Editor", ImGuiTableColumnFlags.WidthStretch);

                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                DrawScriptList();

                ImGui.TableSetColumnIndex(1);
                DrawScriptEditor();

                ImGui.EndTable();
            }

            // Status bar
            ImGui.Separator();
            ImGui.TextColored(_statusColor, _statusMessage);

            ImGui.End();
            IsVisible = isVisible;
        }
    }

    private void DrawToolbar()
    {
        if (ImGui.Button($"{FontAwesomeIcons.Refresh}  Reload Scripts"))
        {
            _levelScriptService?.LoadAvailableScripts();
            _statusMessage = "Scripts reloaded";
            _statusColor = new Vector4(0.4f, 0.7f, 1.0f, 1.0f);
        }

        ImGui.SameLine();

        if (ImGui.Button($"{FontAwesomeIcons.Save}  Save"))
        {
            SaveCurrentScript();
        }

        ImGui.SameLine();

        if (ImGui.Button($"{FontAwesomeIcons.Upload}  Import"))
        {
            ImportScript();
        }

        ImGui.SameLine();

        if (ImGui.Button($"{FontAwesomeIcons.Download}  Export"))
        {
            ExportScript();
        }

        ImGui.SameLine();
        ImGui.Dummy(new Vector2(20, 0));
        ImGui.SameLine();

        ImGui.Text("Format:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(100);
        string[] formats = { "Auto", "Hex", "Decimal" };
        ImGui.Combo("##format", ref _displayFormat, formats, formats.Length);
    }

    private void DrawScriptList()
    {
        ImGui.TextColored(new Vector4(0.4f, 0.7f, 1.0f, 1.0f), "Level Scripts");
        ImGui.Spacing();

        // Search/filter could go here

        ImGui.BeginChild("ScriptList", new Vector2(0, 0), ImGuiChildFlags.Border);

        if (_levelScriptService!.AvailableScriptIds.Count == 0)
        {
            ImGui.TextDisabled("No scripts found");
            ImGui.TextDisabled("Make sure a ROM is loaded");
        }
        else
        {
            foreach (var scriptId in _levelScriptService.AvailableScriptIds)
            {
                bool isSelected = (_levelScriptService.CurrentScript?.ScriptID == scriptId);
                string label = $"Script {scriptId:D4}";

                if (ImGui.Selectable(label, isSelected))
                {
                    LoadScript(scriptId);
                }
            }
        }

        ImGui.EndChild();
    }

    private void DrawScriptEditor()
    {
        var currentScript = _levelScriptService!.CurrentScript;

        if (currentScript == null)
        {
            ImGui.TextDisabled("No script loaded");
            ImGui.TextDisabled("Select a script from the list");
            return;
        }

        ImGui.TextColored(new Vector4(0.4f, 0.7f, 1.0f, 1.0f), $"Editing Script: {currentScript.ScriptID}");
        ImGui.Spacing();

        // Triggers list
        ImGui.BeginChild("TriggersSection", new Vector2(0, -200), ImGuiChildFlags.Border);
        DrawTriggersList(currentScript);
        ImGui.EndChild();

        ImGui.Spacing();

        // Add trigger section
        DrawAddTriggerSection();
    }

    private void DrawTriggersList(LevelScriptFile script)
    {
        ImGui.Text($"Triggers ({script.Triggers.Count}):");
        ImGui.Separator();

        if (script.Triggers.Count == 0)
        {
            ImGui.TextDisabled("No triggers defined");
            return;
        }

        for (int i = 0; i < script.Triggers.Count; i++)
        {
            var trigger = script.Triggers[i];
            bool isSelected = (i == _selectedTriggerIndex);

            string displayText = FormatTriggerDisplay(trigger);

            if (ImGui.Selectable($"{i}: {displayText}##trigger{i}", isSelected))
            {
                _selectedTriggerIndex = i;
            }

            // Context menu
            if (ImGui.BeginPopupContextItem($"triggerContext{i}"))
            {
                if (ImGui.MenuItem($"{FontAwesomeIcons.Trash}  Remove"))
                {
                    script.Triggers.RemoveAt(i);
                    _selectedTriggerIndex = -1;
                    _statusMessage = "Trigger removed";
                    _statusColor = new Vector4(1, 0.7f, 0.3f, 1);
                }

                ImGui.EndPopup();
            }
        }
    }

    private void DrawAddTriggerSection()
    {
        ImGui.TextColored(new Vector4(0.4f, 0.7f, 1.0f, 1.0f), "Add Trigger");
        ImGui.Separator();

        // Trigger type selection
        ImGui.RadioButton("Map Load Trigger", ref _selectedTriggerType, 0);
        ImGui.SameLine();
        ImGui.RadioButton("Variable Value Trigger", ref _selectedTriggerType, 1);

        ImGui.Spacing();

        if (_selectedTriggerType == 0)
        {
            DrawMapLoadTriggerInputs();
        }
        else
        {
            DrawVariableValueTriggerInputs();
        }

        ImGui.Spacing();

        if (ImGui.Button($"{FontAwesomeIcons.Plus}  Add Trigger", new Vector2(150, 30)))
        {
            AddTrigger();
        }

        ImGui.SameLine();

        if (_selectedTriggerIndex >= 0 && ImGui.Button($"{FontAwesomeIcons.Trash}  Remove Selected", new Vector2(150, 30)))
        {
            RemoveSelectedTrigger();
        }
    }

    private void DrawMapLoadTriggerInputs()
    {
        ImGui.Text("Script File ID:");
        ImGui.SetNextItemWidth(200);
        ImGui.InputInt("##maploadscript", ref _newMapLoadScript);
    }

    private void DrawVariableValueTriggerInputs()
    {
        ImGui.Text("Variable Number:");
        ImGui.SetNextItemWidth(200);
        ImGui.InputInt("##varnumber", ref _newVarNumber);

        ImGui.Text("Variable Value:");
        ImGui.SetNextItemWidth(200);
        ImGui.InputInt("##varvalue", ref _newVarValue);

        ImGui.Text("Script File ID:");
        ImGui.SetNextItemWidth(200);
        ImGui.InputInt("##varscript", ref _newVarScript);
    }

    private void AddTrigger()
    {
        var currentScript = _levelScriptService!.CurrentScript;
        if (currentScript == null) return;

        ILevelScriptTrigger? newTrigger = null;

        if (_selectedTriggerType == 0)
        {
            newTrigger = new MapLoadTrigger
            {
                Unknown = 0,
                ScriptFileID = (ushort)Math.Clamp(_newMapLoadScript, 0, ushort.MaxValue)
            };
        }
        else
        {
            newTrigger = new VariableValueTrigger
            {
                Unknown = 0,
                VariableNumber = (ushort)Math.Clamp(_newVarNumber, 0, ushort.MaxValue),
                VariableValue = (ushort)Math.Clamp(_newVarValue, 0, ushort.MaxValue),
                ScriptFileID = (ushort)Math.Clamp(_newVarScript, 0, ushort.MaxValue)
            };
        }

        if (newTrigger != null)
        {
            currentScript.Triggers.Add(newTrigger);
            _statusMessage = "Trigger added";
            _statusColor = new Vector4(0.3f, 1, 0.3f, 1);
        }
    }

    private void RemoveSelectedTrigger()
    {
        var currentScript = _levelScriptService!.CurrentScript;
        if (currentScript == null || _selectedTriggerIndex < 0 || _selectedTriggerIndex >= currentScript.Triggers.Count)
            return;

        currentScript.Triggers.RemoveAt(_selectedTriggerIndex);
        _selectedTriggerIndex = -1;
        _statusMessage = "Trigger removed";
        _statusColor = new Vector4(1, 0.7f, 0.3f, 1);
    }

    private string FormatTriggerDisplay(ILevelScriptTrigger trigger)
    {
        if (_displayFormat == 1) // Hex
        {
            return trigger switch
            {
                MapLoadTrigger mt => $"Map Load → Script: 0x{mt.ScriptFileID:X4}",
                VariableValueTrigger vt => $"Var 0x{vt.VariableNumber:X4} == 0x{vt.VariableValue:X4} → Script: 0x{vt.ScriptFileID:X4}",
                _ => trigger.GetDisplayString()
            };
        }
        else if (_displayFormat == 2) // Decimal
        {
            return trigger switch
            {
                MapLoadTrigger mt => $"Map Load → Script: {mt.ScriptFileID}",
                VariableValueTrigger vt => $"Var {vt.VariableNumber} == {vt.VariableValue} → Script: {vt.ScriptFileID}",
                _ => trigger.GetDisplayString()
            };
        }
        else // Auto
        {
            return trigger.GetDisplayString();
        }
    }

    private void LoadScript(int scriptId)
    {
        var script = _levelScriptService!.LoadScript(scriptId);
        if (script != null)
        {
            _selectedScriptId = scriptId;
            _selectedTriggerIndex = -1;
            _statusMessage = $"Loaded script {scriptId} with {script.Triggers.Count} triggers";
            _statusColor = new Vector4(0.3f, 1, 0.3f, 1);
        }
        else
        {
            _statusMessage = $"Failed to load script {scriptId}";
            _statusColor = new Vector4(1, 0.3f, 0.3f, 1);
        }
    }

    private void SaveCurrentScript()
    {
        if (_levelScriptService!.SaveCurrentScript())
        {
            _statusMessage = $"Script {_levelScriptService.CurrentScript!.ScriptID} saved successfully";
            _statusColor = new Vector4(0.3f, 1, 0.3f, 1);
        }
        else
        {
            _statusMessage = "Failed to save script";
            _statusColor = new Vector4(1, 0.3f, 0.3f, 1);
        }
    }

    private void ImportScript()
    {
        if (_dialogService == null) return;

        string? filePath = _dialogService.OpenFileDialog("Binary Files|*.bin|All Files|*.*", "Import Level Script");
        if (string.IsNullOrEmpty(filePath)) return;

        var script = _levelScriptService!.ImportScript(filePath, _selectedScriptId);
        if (script != null)
        {
            _levelScriptService.CurrentScript = script;
            _statusMessage = $"Imported script from {System.IO.Path.GetFileName(filePath)}";
            _statusColor = new Vector4(0.3f, 1, 0.3f, 1);
        }
        else
        {
            _statusMessage = "Failed to import script";
            _statusColor = new Vector4(1, 0.3f, 0.3f, 1);
        }
    }

    private void ExportScript()
    {
        if (_dialogService == null || _levelScriptService!.CurrentScript == null) return;

        string? filePath = _dialogService.SaveFileDialog(
            "Binary Files|*.bin|All Files|*.*",
            "Export Level Script",
            $"levelscript_{_levelScriptService.CurrentScript.ScriptID}.bin"
        );

        if (string.IsNullOrEmpty(filePath)) return;

        if (_levelScriptService.ExportScript(_levelScriptService.CurrentScript, filePath))
        {
            _statusMessage = $"Exported to {System.IO.Path.GetFileName(filePath)}";
            _statusColor = new Vector4(0.3f, 1, 0.3f, 1);
        }
        else
        {
            _statusMessage = "Failed to export script";
            _statusColor = new Vector4(1, 0.3f, 0.3f, 1);
        }
    }
}
