using Clockwork.Core;
using Clockwork.Core.Logging;
using Clockwork.Core.Models;
using Clockwork.Core.Services;
using Clockwork.UI.Icons;
using ImGuiNET;
using System;
using System.Numerics;

namespace Clockwork.UI.Views;

/// <summary>
/// View for editing wild Pokemon encounters (Diamond/Pearl/Platinum).
/// </summary>
public class WildEditorView : IView
{
    private readonly ApplicationContext _appContext;
    private RomService? _romService;
    private HeaderService? _headerService;
    private WildEncounterService? _wildEncounterService;
    private TextArchiveService? _textArchiveService;

    public bool IsVisible { get; set; } = false;

    // Current state
    private int _currentEncounterID = 0;
    private bool _isDirty = false;
    private bool _shouldFocus = false;

    // Encounter selector
    private string _encounterSearchText = "";
    private int _selectedEncounterIndex = -1;

    // Tab selection
    private enum EditorTab
    {
        Walking,
        Water,
        Special
    }
    private EditorTab _currentTab = EditorTab.Walking;

    public WildEditorView(ApplicationContext appContext)
    {
        _appContext = appContext;
    }

    public void Initialize(ApplicationContext appContext)
    {
        _romService = appContext.GetService<RomService>();
        _headerService = appContext.GetService<HeaderService>();
        _wildEncounterService = appContext.GetService<WildEncounterService>();
        _textArchiveService = appContext.GetService<TextArchiveService>();

        AppLogger.Debug("WildEditorView initialized");
    }

    public void Draw()
    {
        if (!IsVisible)
            return;

        bool isVisible = IsVisible;

        if (ImGui.Begin($"{FontAwesomeIcons.Paw}  Wild Encounter Editor{(_isDirty ? " *" : "")}", ref isVisible, ImGuiWindowFlags.MenuBar))
        {
            // Handle focus request (from other editors)
            if (_shouldFocus)
            {
                ImGui.SetWindowFocus();
                _shouldFocus = false;
            }

            // Menu bar
            if (ImGui.BeginMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {
                    if (ImGui.MenuItem("Save", "Ctrl+S", false, _wildEncounterService?.IsLoaded ?? false))
                    {
                        SaveCurrentEncounter();
                    }

                    ImGui.EndMenu();
                }

                ImGui.EndMenuBar();
            }

            // Check if ROM is loaded
            bool romLoaded = _romService?.CurrentRom != null;

            if (!romLoaded)
            {
                ImGui.TextColored(new Vector4(1.0f, 0.7f, 0.4f, 1.0f),
                    "No ROM loaded. Please load a ROM first.");
            }
            else
            {
                DrawEditorContent();
            }

            ImGui.End();
        }

        IsVisible = isVisible;
    }

    private void DrawEditorContent()
    {
        // Encounter selector
        DrawEncounterSelector();

        ImGui.Separator();
        ImGui.Spacing();

        // Check if encounter is loaded
        if (_wildEncounterService?.IsLoaded != true)
        {
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f),
                "Select an encounter file to edit.");
            return;
        }

        // Tabs
        if (ImGui.BeginTabBar("EncounterTabs"))
        {
            if (ImGui.BeginTabItem($"{FontAwesomeIcons.Seedling}  Walking"))
            {
                _currentTab = EditorTab.Walking;
                DrawWalkingTab();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem($"{FontAwesomeIcons.Water}  Water"))
            {
                _currentTab = EditorTab.Water;
                DrawWaterTab();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem($"{FontAwesomeIcons.Star}  Special"))
            {
                _currentTab = EditorTab.Special;
                DrawSpecialTab();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }

    private void DrawEncounterSelector()
    {
        // ID selector
        ImGui.Text("Encounter:");
        ImGui.SameLine();

        ImGui.SetNextItemWidth(80);
        int encounterID = _currentEncounterID;

        if (ImGui.InputInt("##encounterID", ref encounterID))
        {
            if (encounterID < 0)
                encounterID = 0;

            _currentEncounterID = encounterID;
        }

        ImGui.SameLine();

        // Show location names for this encounter
        if (_textArchiveService != null && _wildEncounterService?.EncounterExists(_currentEncounterID) == true)
        {
            string locationNames = _textArchiveService.GetEncounterLocationNames(_currentEncounterID);
            ImGui.TextColored(new Vector4(0.7f, 0.9f, 1.0f, 1.0f), locationNames);
        }
        else if (_wildEncounterService?.EncounterExists(_currentEncounterID) != true)
        {
            ImGui.TextColored(new Vector4(1.0f, 0.4f, 0.4f, 1.0f), "(not found)");
        }

        // Buttons
        if (ImGui.Button("Load"))
        {
            LoadEncounter(_currentEncounterID);
        }

        ImGui.SameLine();

        if (ImGui.Button("Save"))
        {
            SaveCurrentEncounter();
        }

        ImGui.SameLine();

        if (ImGui.Button("Prev") && _currentEncounterID > 0)
        {
            _currentEncounterID--;
            LoadEncounter(_currentEncounterID);
        }

        ImGui.SameLine();

        if (ImGui.Button("Next"))
        {
            _currentEncounterID++;
            LoadEncounter(_currentEncounterID);
        }
    }

    private void DrawWalkingTab()
    {
        var encounter = _wildEncounterService?.CurrentEncounter;
        if (encounter == null)
            return;

        // Walking rate
        ImGui.TextColored(new Vector4(0.4f, 0.7f, 1.0f, 1.0f), "Encounter Rate");
        ImGui.Spacing();

        int walkingRate = encounter.WalkingRate;
        ImGui.SetNextItemWidth(150);
        if (ImGui.InputInt("Walking Rate##walkingRate", ref walkingRate))
        {
            if (walkingRate < 0) walkingRate = 0;
            if (walkingRate > 255) walkingRate = 255;
            encounter.WalkingRate = (byte)walkingRate;
            _isDirty = true;
        }

        ImGui.SameLine();
        ImGui.TextDisabled("(0-255, higher = more frequent)");

        ImGui.Separator();
        ImGui.Spacing();

        // Walking encounters table
        ImGui.TextColored(new Vector4(0.4f, 0.7f, 1.0f, 1.0f), "Pokemon Encounters");
        ImGui.Spacing();

        if (ImGui.BeginTable("WalkingEncounters", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
        {
            ImGui.TableSetupColumn("Slot", ImGuiTableColumnFlags.WidthFixed, 50);
            ImGui.TableSetupColumn("Pokemon ID", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Level", ImGuiTableColumnFlags.WidthFixed, 80);
            ImGui.TableSetupColumn("Rate", ImGuiTableColumnFlags.WidthFixed, 60);
            ImGui.TableHeadersRow();

            string[] rateLabels = new[]
            {
                "20%", "20%", "10%", "10%", "10%", "10%",
                "5%", "5%", "4%", "4%", "1%", "1%"
            };

            for (int i = 0; i < 12; i++)
            {
                ImGui.TableNextRow();

                // Slot number
                ImGui.TableNextColumn();
                ImGui.Text($"{i + 1}");

                // Pokemon ID
                ImGui.TableNextColumn();
                int pokemonID = (int)encounter.WalkingPokemon[i];
                ImGui.SetNextItemWidth(-1);
                if (ImGui.InputInt($"##walkPoke{i}", ref pokemonID))
                {
                    if (pokemonID < 0) pokemonID = 0;
                    encounter.WalkingPokemon[i] = (uint)pokemonID;
                    _isDirty = true;
                }

                // Pokemon Name
                ImGui.TableNextColumn();
                if (_textArchiveService != null && pokemonID > 0)
                {
                    string pokemonName = _textArchiveService.GetPokemonName((uint)pokemonID);
                    ImGui.TextColored(new Vector4(0.7f, 0.9f, 1.0f, 1.0f), pokemonName);
                }
                else
                {
                    ImGui.TextDisabled("---");
                }

                // Level
                ImGui.TableNextColumn();
                int level = encounter.WalkingLevels[i];
                ImGui.SetNextItemWidth(-1);
                if (ImGui.InputInt($"##walkLevel{i}", ref level))
                {
                    if (level < 1) level = 1;
                    if (level > 100) level = 100;
                    encounter.WalkingLevels[i] = (byte)level;
                    _isDirty = true;
                }

                // Rate label
                ImGui.TableNextColumn();
                ImGui.Text(rateLabels[i]);
            }

            ImGui.EndTable();
        }
    }

    private void DrawWaterTab()
    {
        var encounter = _wildEncounterService?.CurrentEncounter;
        if (encounter == null)
            return;

        if (ImGui.BeginTabBar("WaterSubTabs"))
        {
            if (ImGui.BeginTabItem("Surf"))
            {
                byte rate = encounter.SurfRate;
                DrawWaterEncounters("Surf",
                    ref rate,
                    encounter.SurfPokemon,
                    encounter.SurfMinLevels,
                    encounter.SurfMaxLevels);
                encounter.SurfRate = rate;
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Old Rod"))
            {
                byte rate = encounter.OldRodRate;
                DrawWaterEncounters("Old Rod",
                    ref rate,
                    encounter.OldRodPokemon,
                    encounter.OldRodMinLevels,
                    encounter.OldRodMaxLevels);
                encounter.OldRodRate = rate;
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Good Rod"))
            {
                byte rate = encounter.GoodRodRate;
                DrawWaterEncounters("Good Rod",
                    ref rate,
                    encounter.GoodRodPokemon,
                    encounter.GoodRodMinLevels,
                    encounter.GoodRodMaxLevels);
                encounter.GoodRodRate = rate;
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Super Rod"))
            {
                byte rate = encounter.SuperRodRate;
                DrawWaterEncounters("Super Rod",
                    ref rate,
                    encounter.SuperRodPokemon,
                    encounter.SuperRodMinLevels,
                    encounter.SuperRodMaxLevels);
                encounter.SuperRodRate = rate;
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }

    private void DrawWaterEncounters(string name, ref byte rate, ushort[] pokemon, byte[] minLevels, byte[] maxLevels)
    {
        // Encounter rate
        ImGui.TextColored(new Vector4(0.4f, 0.7f, 1.0f, 1.0f), "Encounter Rate");
        ImGui.Spacing();

        int rateValue = rate;
        ImGui.SetNextItemWidth(150);
        if (ImGui.InputInt($"{name} Rate##rate{name}", ref rateValue))
        {
            if (rateValue < 0) rateValue = 0;
            if (rateValue > 255) rateValue = 255;
            rate = (byte)rateValue;
            _isDirty = true;
        }

        ImGui.SameLine();
        ImGui.TextDisabled("(0-255, higher = more frequent)");

        ImGui.Separator();
        ImGui.Spacing();

        // Encounters table
        ImGui.TextColored(new Vector4(0.4f, 0.7f, 1.0f, 1.0f), $"{name} Encounters");
        ImGui.Spacing();

        if (ImGui.BeginTable($"{name}Encounters", 6, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
        {
            ImGui.TableSetupColumn("Slot", ImGuiTableColumnFlags.WidthFixed, 50);
            ImGui.TableSetupColumn("Pokemon ID", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Min Lv", ImGuiTableColumnFlags.WidthFixed, 70);
            ImGui.TableSetupColumn("Max Lv", ImGuiTableColumnFlags.WidthFixed, 70);
            ImGui.TableSetupColumn("Rate", ImGuiTableColumnFlags.WidthFixed, 60);
            ImGui.TableHeadersRow();

            string[] rateLabels = new[] { "60%", "30%", "5%", "4%", "1%" };

            for (int i = 0; i < 5; i++)
            {
                ImGui.TableNextRow();

                // Slot number
                ImGui.TableNextColumn();
                ImGui.Text($"{i + 1}");

                // Pokemon ID
                ImGui.TableNextColumn();
                int pokemonID = pokemon[i];
                ImGui.SetNextItemWidth(-1);
                if (ImGui.InputInt($"##water{name}Poke{i}", ref pokemonID))
                {
                    if (pokemonID < 0) pokemonID = 0;
                    pokemon[i] = (ushort)pokemonID;
                    _isDirty = true;
                }

                // Pokemon Name
                ImGui.TableNextColumn();
                if (_textArchiveService != null && pokemonID > 0)
                {
                    string pokemonName = _textArchiveService.GetPokemonName((uint)pokemonID);
                    ImGui.TextColored(new Vector4(0.7f, 0.9f, 1.0f, 1.0f), pokemonName);
                }
                else
                {
                    ImGui.TextDisabled("---");
                }

                // Min Level
                ImGui.TableNextColumn();
                int minLevel = minLevels[i];
                ImGui.SetNextItemWidth(-1);
                if (ImGui.InputInt($"##water{name}MinLevel{i}", ref minLevel))
                {
                    if (minLevel < 1) minLevel = 1;
                    if (minLevel > 100) minLevel = 100;
                    minLevels[i] = (byte)minLevel;
                    _isDirty = true;
                }

                // Max Level
                ImGui.TableNextColumn();
                int maxLevel = maxLevels[i];
                ImGui.SetNextItemWidth(-1);
                if (ImGui.InputInt($"##water{name}MaxLevel{i}", ref maxLevel))
                {
                    if (maxLevel < 1) maxLevel = 1;
                    if (maxLevel > 100) maxLevel = 100;
                    maxLevels[i] = (byte)maxLevel;
                    _isDirty = true;
                }

                // Rate label
                ImGui.TableNextColumn();
                ImGui.Text(rateLabels[i]);
            }

            ImGui.EndTable();
        }
    }

    private void DrawSpecialTab()
    {
        var encounter = _wildEncounterService?.CurrentEncounter;
        if (encounter == null)
            return;

        ImGui.TextColored(new Vector4(1.0f, 0.7f, 0.4f, 1.0f),
            "Special encounters (Swarms, Day/Night, Radar, Dual Slot, etc.) will be added in future updates.");
        ImGui.Spacing();
        ImGui.TextDisabled("For now, use the Walking and Water tabs to edit basic encounters.");
    }

    private void LoadEncounter(int encounterID)
    {
        if (_wildEncounterService == null)
            return;

        var encounter = _wildEncounterService.LoadEncounter(encounterID);

        if (encounter != null)
        {
            _currentEncounterID = encounterID;
            _isDirty = false;
            AppLogger.Info($"Loaded encounter {encounterID}");
        }
        else
        {
            AppLogger.Warn($"Failed to load encounter {encounterID}");
        }
    }

    private void SaveCurrentEncounter()
    {
        if (_wildEncounterService == null || !_wildEncounterService.IsLoaded)
        {
            AppLogger.Warn("Cannot save: No encounter loaded");
            return;
        }

        if (_wildEncounterService.SaveCurrentEncounter())
        {
            _isDirty = false;
            AppLogger.Info($"Saved encounter {_currentEncounterID}");
        }
        else
        {
            AppLogger.Error($"Failed to save encounter {_currentEncounterID}");
        }
    }

    /// <summary>
    /// Opens the Wild Editor and loads a specific encounter file.
    /// Called from other editors (e.g., Header Editor).
    /// </summary>
    public void OpenWithEncounterID(int encounterID)
    {
        IsVisible = true;
        _shouldFocus = true;
        LoadEncounter(encounterID);
        AppLogger.Info($"Wild Editor opened with encounter ID {encounterID}");
    }
}
