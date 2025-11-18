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
    private DialogService? _dialogService;

    private MapHeader? _currentHeader;
    private int _currentHeaderID = 0;
    private string _headerFilePath = string.Empty;
    private string _statusMessage = string.Empty;
    private System.Numerics.Vector4 _statusColor = new(1.0f, 1.0f, 1.0f, 1.0f);

    public bool IsVisible { get; set; } = false;

    public HeaderEditorView(ApplicationContext appContext)
    {
        _appContext = appContext;
        _romService = _appContext.GetService<RomService>();
        _dialogService = _appContext.GetService<DialogService>();
    }

    public void Draw()
    {
        if (!IsVisible) return;

        bool isVisible = IsVisible;
        ImGui.Begin("Header Editor", ref isVisible);

        ImGui.TextColored(new System.Numerics.Vector4(0.4f, 0.7f, 1.0f, 1.0f), "Map Header Editor");
        ImGui.Separator();
        ImGui.Spacing();

        // Load header section
        ImGui.Text("Header File:");
        ImGui.SetNextItemWidth(-80);
        ImGui.InputText("##headerpath", ref _headerFilePath, 500);
        ImGui.SameLine();
        if (ImGui.Button("Browse##header", new System.Numerics.Vector2(70, 0)))
        {
            string? selectedFile = _dialogService?.OpenFileDialog("All Files|*.*", "Select Header File");
            if (selectedFile != null)
            {
                _headerFilePath = selectedFile;
                LoadHeader();
            }
        }

        ImGui.SameLine();
        if (ImGui.Button("Load", new System.Numerics.Vector2(80, 0)))
        {
            LoadHeader();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Only show editor if header is loaded
        if (_currentHeader != null)
        {
            DrawHeaderEditor();
        }
        else
        {
            ImGui.TextColored(new System.Numerics.Vector4(0.7f, 0.7f, 0.7f, 1.0f),
                "No header loaded. Please load a header file.");
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

    private void LoadHeader()
    {
        if (string.IsNullOrWhiteSpace(_headerFilePath))
        {
            _statusMessage = "Error: Please specify a header file path";
            _statusColor = new System.Numerics.Vector4(1.0f, 0.4f, 0.4f, 1.0f);
            return;
        }

        _currentHeader = MapHeader.ReadFromFile(_headerFilePath);
        if (_currentHeader != null)
        {
            _statusMessage = $"Header loaded successfully from: {Path.GetFileName(_headerFilePath)}";
            _statusColor = new System.Numerics.Vector4(0.5f, 0.8f, 0.5f, 1.0f);
        }
        else
        {
            _statusMessage = "Error: Failed to load header file";
            _statusColor = new System.Numerics.Vector4(1.0f, 0.4f, 0.4f, 1.0f);
        }
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
                _currentHeader.WildPokemon = (ushort)Math.Clamp(wildPokemon, 0, ushort.MaxValue);
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
        if (ImGui.Button("Save Header", new System.Numerics.Vector2(-1, 40)))
        {
            if (_currentHeader.WriteToFile(_headerFilePath))
            {
                _statusMessage = "Header saved successfully!";
                _statusColor = new System.Numerics.Vector4(0.5f, 0.8f, 0.5f, 1.0f);
            }
            else
            {
                _statusMessage = "Error: Failed to save header";
                _statusColor = new System.Numerics.Vector4(1.0f, 0.4f, 0.4f, 1.0f);
            }
        }
    }
}
