using ImGuiNET;
using System.Numerics;
using Clockwork.Core;
using Clockwork.Core.Formats.NDS.Scripts;

namespace Clockwork.UI.Views;

/// <summary>
/// View for displaying the entire script command database
/// Shows all loaded commands with their IDs, names, parameters, and descriptions
/// </summary>
public class ScriptCommandDatabaseView : IView
{
    public bool IsVisible { get; set; } = false;

    private ApplicationContext? _appContext;
    private string _searchFilter = "";
    private List<ScriptCommandInfo> _filteredCommands = new();
    private List<ScriptCommandInfo> _allCommands = new();
    private bool _needsRefresh = true;

    public void Initialize(ApplicationContext appContext)
    {
        _appContext = appContext;
    }

    public void Draw()
    {
        if (!IsVisible)
            return;

        // Refresh commands list if needed
        if (_needsRefresh)
        {
            RefreshCommandsList();
            _needsRefresh = false;
        }

        ImGui.SetNextWindowSize(new Vector2(1000, 600), ImGuiCond.FirstUseEver);
        if (ImGui.Begin("Script Command Database", ref IsVisible))
        {
            // Header with search and count
            ImGui.Text($"Loaded Commands: {_allCommands.Count}");
            ImGui.SameLine();
            ImGui.SetCursorPosX(ImGui.GetWindowWidth() - 320);

            // Search filter
            ImGui.PushItemWidth(300);
            if (ImGui.InputTextWithHint("##search", "Search by name or ID...", ref _searchFilter, 256))
            {
                FilterCommands();
            }
            ImGui.PopItemWidth();

            ImGui.Separator();

            // Table for commands
            if (ImGui.BeginTable("CommandsTable", 4,
                ImGuiTableFlags.Borders |
                ImGuiTableFlags.RowBg |
                ImGuiTableFlags.ScrollY |
                ImGuiTableFlags.Resizable |
                ImGuiTableFlags.Sortable))
            {
                // Setup columns
                ImGui.TableSetupColumn("ID", ImGuiTableColumnFlags.WidthFixed, 60.0f);
                ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthFixed, 180.0f);
                ImGui.TableSetupColumn("Parameters", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Description", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupScrollFreeze(0, 1); // Freeze header row
                ImGui.TableHeadersRow();

                // Display commands
                var commandsToShow = string.IsNullOrWhiteSpace(_searchFilter) ? _allCommands : _filteredCommands;

                foreach (var cmd in commandsToShow)
                {
                    ImGui.TableNextRow();

                    // ID column
                    ImGui.TableSetColumnIndex(0);
                    ImGui.Text($"0x{cmd.ID:X4}");

                    // Name column
                    ImGui.TableSetColumnIndex(1);
                    ImGui.Text(cmd.Name);

                    // Parameters column
                    ImGui.TableSetColumnIndex(2);
                    if (cmd.Parameters.Count == 0)
                    {
                        ImGui.TextDisabled("(none)");
                    }
                    else
                    {
                        var paramList = new List<string>();
                        for (int i = 0; i < cmd.Parameters.Count; i++)
                        {
                            string paramName = i < cmd.ParameterNames.Count && !string.IsNullOrEmpty(cmd.ParameterNames[i])
                                ? cmd.ParameterNames[i]
                                : $"param{i + 1}";
                            string paramType = GetParameterTypeDisplay(cmd.Parameters[i]);
                            paramList.Add($"{paramName}:{paramType}");
                        }
                        ImGui.TextWrapped(string.Join(", ", paramList));
                    }

                    // Description column
                    ImGui.TableSetColumnIndex(3);
                    if (!string.IsNullOrEmpty(cmd.Description))
                    {
                        ImGui.TextWrapped(cmd.Description);
                    }
                    else
                    {
                        ImGui.TextDisabled("(no description)");
                    }
                }

                ImGui.EndTable();
            }

            // Footer with statistics
            ImGui.Separator();
            if (string.IsNullOrWhiteSpace(_searchFilter))
            {
                ImGui.Text($"Showing all {_allCommands.Count} commands");
            }
            else
            {
                ImGui.Text($"Showing {_filteredCommands.Count} of {_allCommands.Count} commands (filtered)");
            }

            ImGui.End();
        }
    }

    /// <summary>
    /// Refreshes the commands list from the database
    /// </summary>
    private void RefreshCommandsList()
    {
        _allCommands.Clear();

        try
        {
            var commands = ScriptDatabase.PlatinumCommands;

            // Convert dictionary to sorted list
            _allCommands = commands.Values
                .OrderBy(cmd => cmd.ID)
                .ToList();

            FilterCommands();
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Failed to load script commands: {ex.Message}");
        }
    }

    /// <summary>
    /// Filters commands based on search text
    /// </summary>
    private void FilterCommands()
    {
        _filteredCommands.Clear();

        if (string.IsNullOrWhiteSpace(_searchFilter))
        {
            return;
        }

        var searchLower = _searchFilter.ToLower();

        foreach (var cmd in _allCommands)
        {
            // Search by name
            if (cmd.Name.ToLower().Contains(searchLower))
            {
                _filteredCommands.Add(cmd);
                continue;
            }

            // Search by hex ID (e.g., "0x0002" or just "2")
            string hexId = $"0x{cmd.ID:X4}".ToLower();
            if (hexId.Contains(searchLower) || cmd.ID.ToString().Contains(searchLower))
            {
                _filteredCommands.Add(cmd);
                continue;
            }

            // Search by description
            if (!string.IsNullOrEmpty(cmd.Description) &&
                cmd.Description.ToLower().Contains(searchLower))
            {
                _filteredCommands.Add(cmd);
            }
        }
    }

    /// <summary>
    /// Gets a display-friendly name for parameter types
    /// </summary>
    private string GetParameterTypeDisplay(ScriptParameterType type)
    {
        return type switch
        {
            ScriptParameterType.Byte => "byte",
            ScriptParameterType.Word => "word",
            ScriptParameterType.DWord => "dword",
            ScriptParameterType.Offset => "offset",
            ScriptParameterType.Variable => "variable",
            _ => type.ToString().ToLower()
        };
    }

    /// <summary>
    /// Refreshes the view (called when database is reloaded)
    /// </summary>
    public void Refresh()
    {
        _needsRefresh = true;
    }
}
