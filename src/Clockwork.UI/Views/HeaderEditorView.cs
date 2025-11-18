using Clockwork.Core;
using Clockwork.Core.Models;
using Clockwork.Core.Services;
using ImGuiNET;

namespace Clockwork.UI.Views;

/// <summary>
/// View for editing Pokémon map headers.
/// </summary>
public class HeaderEditorView : IView
{
    private readonly ApplicationContext _appContext;
    private RomService? _romService;
    private HeaderService? _headerService;

    private MapHeader? _currentHeader;
    private string _statusMessage = string.Empty;
    private System.Numerics.Vector4 _statusColor = new(1.0f, 1.0f, 1.0f, 1.0f);
    private string _searchFilter = string.Empty;

    public bool IsVisible { get; set; } = false;

    public HeaderEditorView(ApplicationContext appContext)
    {
        _appContext = appContext;
        _romService = _appContext.GetService<RomService>();
        _headerService = _appContext.GetService<HeaderService>();
    }

    public void Draw()
    {
        if (!IsVisible) return;

        bool isVisible = IsVisible;
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(900, 600), ImGuiCond.FirstUseEver);
        ImGui.Begin("Header Editor", ref isVisible);

        // Check if ROM is loaded
        bool romLoaded = _romService?.CurrentRom != null;
        bool headersLoaded = _headerService?.IsLoaded ?? false;

        if (!romLoaded)
        {
            ImGui.TextColored(new System.Numerics.Vector4(1.0f, 0.7f, 0.4f, 1.0f),
                "No ROM loaded. Please load a ROM first (ROM > Open ROM...)");
        }
        else
        {
            // Load headers button
            if (!headersLoaded)
            {
                if (ImGui.Button("Load Headers from ROM", new System.Numerics.Vector2(-1, 40)))
                {
                    LoadHeadersFromRom();
                }
            }
            else
            {
                // Two-column layout: header list on left, editor on right
                if (ImGui.BeginTable("HeaderEditorTable", 2, ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInnerV))
                {
                    ImGui.TableSetupColumn("Headers", ImGuiTableColumnFlags.WidthFixed, 300);
                    ImGui.TableSetupColumn("Editor", ImGuiTableColumnFlags.WidthStretch);

                    ImGui.TableNextRow();
                    ImGui.TableSetColumnIndex(0);

                    // Left column: Header list
                    DrawHeaderList();

                    ImGui.TableSetColumnIndex(1);

                    // Right column: Header editor
                    if (_currentHeader != null)
                    {
                        DrawHeaderEditor();
                    }
                    else
                    {
                        ImGui.TextColored(new System.Numerics.Vector4(0.7f, 0.7f, 0.7f, 1.0f),
                            "Select a header from the list to edit.");
                    }

                    ImGui.EndTable();
                }
            }
        }

        // Status message
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

    private void LoadHeadersFromRom()
    {
        if (_headerService == null) return;

        if (_headerService.LoadHeadersFromRom())
        {
            _statusMessage = $"Loaded {_headerService.Headers.Count} headers from ROM";
            _statusColor = new System.Numerics.Vector4(0.5f, 0.8f, 0.5f, 1.0f);
        }
        else
        {
            _statusMessage = "Error: Failed to load headers from ROM";
            _statusColor = new System.Numerics.Vector4(1.0f, 0.4f, 0.4f, 1.0f);
        }
    }

    private void DrawHeaderList()
    {
        if (_headerService == null) return;

        ImGui.TextColored(new System.Numerics.Vector4(0.4f, 0.7f, 1.0f, 1.0f), "Map Headers");
        ImGui.Spacing();

        // Search filter
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##search", "Search...", ref _searchFilter, 256);
        ImGui.Spacing();

        // Header list
        ImGui.BeginChild("HeaderList", new System.Numerics.Vector2(0, 0), ImGuiChildFlags.Border);

        var headers = _headerService.Headers;
        foreach (var header in headers)
        {
            // Apply search filter
            if (!string.IsNullOrWhiteSpace(_searchFilter))
            {
                bool matchesID = header.HeaderID.ToString().Contains(_searchFilter, StringComparison.OrdinalIgnoreCase);
                bool matchesName = header.InternalName.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase);
                if (!matchesID && !matchesName)
                    continue;
            }

            bool isSelected = _currentHeader != null && _currentHeader.HeaderID == header.HeaderID;
            string label = $"{header.HeaderID:D3}: {header.InternalName}";

            if (ImGui.Selectable(label, isSelected))
            {
                _currentHeader = header;
                _statusMessage = $"Selected header {header.HeaderID}: {header.InternalName}";
                _statusColor = new System.Numerics.Vector4(0.4f, 0.7f, 1.0f, 1.0f);
            }
        }

        ImGui.EndChild();
    }

    private void DrawHeaderEditor()
    {
        if (_currentHeader == null) return;

        // Map Structure section
        if (ImGui.CollapsingHeader("Map Structure", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Text("Matrix ID:");
            ImGui.SameLine(150);
            int matrixID = _currentHeader.MatrixID;
            if (ImGui.InputInt("##matrixid", ref matrixID, 1, 10))
            {
                _currentHeader.MatrixID = (ushort)Math.Clamp(matrixID, 0, ushort.MaxValue);
            }

            ImGui.Text("Area Data ID:");
            ImGui.SameLine(150);
            int areaDataID = _currentHeader.AreaDataID;
            if (ImGui.InputInt("##areadataid", ref areaDataID, 1, 10))
            {
                _currentHeader.AreaDataID = (byte)Math.Clamp(areaDataID, 0, byte.MaxValue);
            }
        }

        ImGui.Spacing();

        // Music & Appearance section
        if (ImGui.CollapsingHeader("Music & Appearance", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Text("Music Day ID:");
            ImGui.SameLine(150);
            int musicDayID = _currentHeader.MusicDayID;
            if (ImGui.InputInt("##musicdayid", ref musicDayID, 1, 10))
            {
                _currentHeader.MusicDayID = (ushort)Math.Clamp(musicDayID, 0, ushort.MaxValue);
            }

            ImGui.Text("Music Night ID:");
            ImGui.SameLine(150);
            int musicNightID = _currentHeader.MusicNightID;
            if (ImGui.InputInt("##musicnightid", ref musicNightID, 1, 10))
            {
                _currentHeader.MusicNightID = (ushort)Math.Clamp(musicNightID, 0, ushort.MaxValue);
            }

            ImGui.Text("Weather ID:");
            ImGui.SameLine(150);
            int weatherID = _currentHeader.WeatherID;
            if (ImGui.InputInt("##weatherid", ref weatherID, 1, 10))
            {
                _currentHeader.WeatherID = (byte)Math.Clamp(weatherID, 0, byte.MaxValue);
            }

            ImGui.Text("Camera Angle ID:");
            ImGui.SameLine(150);
            int cameraID = _currentHeader.CameraAngleID;
            if (ImGui.InputInt("##cameraid", ref cameraID, 1, 10))
            {
                _currentHeader.CameraAngleID = (byte)Math.Clamp(cameraID, 0, byte.MaxValue);
            }
        }

        ImGui.Spacing();

        // Scripts & Events section
        if (ImGui.CollapsingHeader("Scripts & Events"))
        {
            ImGui.Text("Script File ID:");
            ImGui.SameLine(150);
            int scriptID = _currentHeader.ScriptFileID;
            if (ImGui.InputInt("##scriptid", ref scriptID, 1, 10))
            {
                _currentHeader.ScriptFileID = (ushort)Math.Clamp(scriptID, 0, ushort.MaxValue);
            }

            ImGui.Text("Level Script ID:");
            ImGui.SameLine(150);
            int levelScriptID = _currentHeader.LevelScriptID;
            if (ImGui.InputInt("##levelscriptid", ref levelScriptID, 1, 10))
            {
                _currentHeader.LevelScriptID = (ushort)Math.Clamp(levelScriptID, 0, ushort.MaxValue);
            }

            ImGui.Text("Event File ID:");
            ImGui.SameLine(150);
            int eventID = _currentHeader.EventFileID;
            if (ImGui.InputInt("##eventid", ref eventID, 1, 10))
            {
                _currentHeader.EventFileID = (ushort)Math.Clamp(eventID, 0, ushort.MaxValue);
            }

            ImGui.Text("Text Archive ID:");
            ImGui.SameLine(150);
            int textArchiveID = _currentHeader.TextArchiveID;
            if (ImGui.InputInt("##textarchiveid", ref textArchiveID, 1, 10))
            {
                _currentHeader.TextArchiveID = (ushort)Math.Clamp(textArchiveID, 0, ushort.MaxValue);
            }
        }

        ImGui.Spacing();

        // Location & Encounters section
        if (ImGui.CollapsingHeader("Location & Encounters"))
        {
            ImGui.Text("Location Name:");
            ImGui.SameLine(150);
            int locationName = _currentHeader.LocationName;
            if (ImGui.InputInt("##locationname", ref locationName, 1, 10))
            {
                _currentHeader.LocationName = (byte)Math.Clamp(locationName, 0, byte.MaxValue);
            }

            ImGui.Text("Area Icon:");
            ImGui.SameLine(150);
            int areaIcon = _currentHeader.AreaIcon;
            if (ImGui.InputInt("##areaicon", ref areaIcon, 1, 10))
            {
                _currentHeader.AreaIcon = (byte)Math.Clamp(areaIcon, 0, byte.MaxValue);
            }

            ImGui.Text("Wild Pokémon:");
            ImGui.SameLine(150);
            int wildPokemon = _currentHeader.WildPokemon;
            if (ImGui.InputInt("##wildpokemon", ref wildPokemon, 1, 10))
            {
                _currentHeader.WildPokemon = (byte)Math.Clamp(wildPokemon, 0, byte.MaxValue);
            }

            ImGui.Text("Time ID:");
            ImGui.SameLine(150);
            int timeID = _currentHeader.TimeID;
            if (ImGui.InputInt("##timeid", ref timeID, 1, 10))
            {
                _currentHeader.TimeID = (byte)Math.Clamp(timeID, 0, byte.MaxValue);
            }
        }

        ImGui.Spacing();

        // Map Flags section
        if (ImGui.CollapsingHeader("Map Flags"))
        {
            bool allowFly = _currentHeader.AllowFly;
            bool allowEscapeRope = _currentHeader.AllowEscapeRope;
            bool allowRunningShoes = _currentHeader.AllowRunningShoes;
            bool allowBike = _currentHeader.AllowBike;

            ImGui.Checkbox("Allow Fly/Teleport", ref allowFly);
            ImGui.Checkbox("Allow Escape Rope", ref allowEscapeRope);
            ImGui.Checkbox("Allow Running Shoes", ref allowRunningShoes);
            ImGui.Checkbox("Allow Bike", ref allowBike);

            // Update MapSettings based on checkboxes
            byte newFlags = 0;
            if (allowFly) newFlags |= 0x01;
            if (allowEscapeRope) newFlags |= 0x02;
            if (allowRunningShoes) newFlags |= 0x04;
            if (allowBike) newFlags |= 0x08;

            // Preserve other bits and update flags
            _currentHeader.MapSettings = (ushort)((_currentHeader.MapSettings & 0x0FFF) | (newFlags << 12));

            ImGui.Spacing();

            ImGui.Text("Battle Background:");
            ImGui.SameLine(150);
            int battleBg = _currentHeader.BattleBackground;
            if (ImGui.InputInt("##battlebg", ref battleBg, 1, 1))
            {
                battleBg = Math.Clamp(battleBg, 0, 31);
                // Update MapSettings: preserve location specifier and flags, update battle background
                _currentHeader.MapSettings = (ushort)((_currentHeader.MapSettings & 0xF07F) | (battleBg << 7));
            }
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Save button
        ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.25f, 0.55f, 0.25f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new System.Numerics.Vector4(0.35f, 0.65f, 0.35f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new System.Numerics.Vector4(0.20f, 0.50f, 0.20f, 1.0f));

        if (ImGui.Button("Save Header", new System.Numerics.Vector2(-1, 40)))
        {
            if (_headerService != null && _headerService.SaveHeader(_currentHeader))
            {
                _statusMessage = $"Header {_currentHeader.HeaderID} saved successfully!";
                _statusColor = new System.Numerics.Vector4(0.5f, 0.8f, 0.5f, 1.0f);
            }
            else
            {
                _statusMessage = "Error: Failed to save header";
                _statusColor = new System.Numerics.Vector4(1.0f, 0.4f, 0.4f, 1.0f);
            }
        }

        ImGui.PopStyleColor(3);
    }
}
