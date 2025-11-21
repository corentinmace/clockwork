using Clockwork.Core;
using Clockwork.Core.Data;
using Clockwork.Core.Logging;
using Clockwork.Core.Models;
using Clockwork.Core.Services;
using Clockwork.UI.Graphics;
using Clockwork.UI.Icons;
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
    private TextArchiveService? _textArchiveService;
    private TextureManager? _textureManager;

    // References to other editors for navigation
    private TextEditorWindow? _textEditorWindow;
    private LevelScriptEditorView? _levelScriptEditorView;
    private MatrixEditorView? _matrixEditorView;
    private WildEditorView? _wildEditorView;

    private MapHeader? _currentHeader;
    private string _statusMessage = string.Empty;
    private System.Numerics.Vector4 _statusColor = new(1.0f, 1.0f, 1.0f, 1.0f);
    private string _searchFilter = string.Empty;
    private string? _lastLoadedRomPath;
    private bool _shouldScrollToSelection = false;

    // Location names loaded from text archives
    private List<string> _locationNames = new();
    private bool _locationNamesLoaded = false;

    public bool IsVisible { get; set; } = false;

    public HeaderEditorView(ApplicationContext appContext)
    {
        _appContext = appContext;
        _romService = _appContext.GetService<RomService>();
        _headerService = _appContext.GetService<HeaderService>();
        _textArchiveService = _appContext.GetService<TextArchiveService>();
    }

    /// <summary>
    /// Set references to other editor windows for navigation
    /// </summary>
    public void SetEditorReferences(TextEditorWindow textEditor, ScriptEditorWindow? scriptEditor, LevelScriptEditorView levelScriptEditor, MatrixEditorView matrixEditor, WildEditorView wildEditor)
    {
        _textEditorWindow = textEditor;
        // ScriptEditorWindow removed - parameter kept for compatibility
        _levelScriptEditorView = levelScriptEditor;
        _matrixEditorView = matrixEditor;
        _wildEditorView = wildEditor;
    }

    /// <summary>
    /// Set texture manager for loading preview images
    /// </summary>
    public void SetTextureManager(TextureManager textureManager)
    {
        _textureManager = textureManager;
    }

    public void Draw()
    {
        if (!IsVisible) return;

        bool isVisible = IsVisible;
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(900, 600), ImGuiCond.FirstUseEver);
        ImGui.Begin($"{FontAwesomeIcons.BookOpenReader} Header Editor", ref isVisible);

        // Check if ROM is loaded
        bool romLoaded = _romService?.CurrentRom != null;
        bool headersLoaded = _headerService?.IsLoaded ?? false;

        if (!romLoaded)
        {
            ImGui.TextColored(new System.Numerics.Vector4(1.0f, 0.7f, 0.4f, 1.0f),
                "No ROM loaded. Please load a ROM first (ROM > Open ROM...)");
            _lastLoadedRomPath = null;
        }
        else
        {
            // Auto-load headers when ROM changes
            string currentRomPath = _romService!.CurrentRom!.RomPath;
            if (_lastLoadedRomPath != currentRomPath)
            {
                LoadHeadersFromRom();
                _lastLoadedRomPath = currentRomPath;
            }

            if (headersLoaded)
            {
                DrawEditorContent();
            }
            else
            {
                ImGui.TextColored(new System.Numerics.Vector4(1.0f, 0.7f, 0.4f, 1.0f),
                    "Loading headers...");
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

        // Detached window is managed automatically by DetachedWindowManager.UpdateAll()
        // No need to draw it here - independent windows render themselves
    }

    private void DrawEditorContent()
    {
        // Two-column layout: header list on left, editor on right
        float availableHeight = ImGui.GetContentRegionAvail().Y;

        if (ImGui.BeginTable("HeaderEditorTable", 2, ImGuiTableFlags.Resizable | ImGuiTableFlags.BordersInnerV))
        {
            ImGui.TableSetupColumn("Headers", ImGuiTableColumnFlags.WidthFixed, 300);
            ImGui.TableSetupColumn("Editor", ImGuiTableColumnFlags.WidthStretch);

            ImGui.TableNextRow();
            ImGui.TableSetColumnIndex(0);

            // Left column: Header list
            DrawHeaderList(availableHeight - 40); // Leave space for status message

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

    private void LoadHeadersFromRom()
    {
        if (_headerService == null) return;

        if (_headerService.LoadHeadersFromRom())
        {
            _statusMessage = $"Loaded {_headerService.Headers.Count} headers from ROM";
            _statusColor = new System.Numerics.Vector4(0.5f, 0.8f, 0.5f, 1.0f);

            // Load location names
            LoadLocationNames();
        }
        else
        {
            _statusMessage = "Error: Failed to load headers from ROM";
            _statusColor = new System.Numerics.Vector4(1.0f, 0.4f, 0.4f, 1.0f);
        }
    }

    private void LoadLocationNames()
    {
        if (_textArchiveService == null || _romService?.CurrentRom?.IsLoaded != true)
        {
            _locationNames = new List<string>();
            _locationNamesLoaded = false;
            return;
        }

        try
        {
            // Use TextArchiveService to load location names (handles auto-extraction)
            var locationNames = _textArchiveService.GetLocationNames();

            if (locationNames != null && locationNames.Length > 0)
            {
                _locationNames = locationNames.ToList();
                _locationNamesLoaded = true;

                _statusMessage += $" | Loaded {_locationNames.Count} location names";
                AppLogger.Debug($"HeaderEditorView: Loaded {_locationNames.Count} location names via TextArchiveService");
            }
            else
            {
                _locationNames = new List<string>();
                _locationNamesLoaded = false;
                AppLogger.Warn("HeaderEditorView: Failed to load location names - array is null or empty");
            }
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Error loading location names: {ex.Message}");
            _locationNames = new List<string>();
            _locationNamesLoaded = false;
        }
    }

    private void DrawHeaderList(float availableHeight)
    {
        if (_headerService == null) return;

        ImGui.TextColored(new System.Numerics.Vector4(0.4f, 0.7f, 1.0f, 1.0f), "Map Headers");
        ImGui.Spacing();

        // Search filter
        ImGui.SetNextItemWidth(-1);
        ImGui.InputTextWithHint("##search", "Search...", ref _searchFilter, 256);
        ImGui.Spacing();

        // Header list with scrolling
        ImGui.BeginChild("HeaderList", new System.Numerics.Vector2(-1, -60), ImGuiChildFlags.Border);

        var headers = _headerService.Headers;
        foreach (var header in headers)
        {
            // Apply search filter
            if (!string.IsNullOrWhiteSpace(_searchFilter))
            {
                bool matchesID = header.HeaderID.ToString().Contains(_searchFilter, StringComparison.OrdinalIgnoreCase);
                bool matchesName = header.InternalName.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase);

                // Check if location name matches
                bool matchesLocation = false;
                if (_locationNamesLoaded && header.LocationName < _locationNames.Count)
                {
                    string locationName = _locationNames[header.LocationName];
                    matchesLocation = locationName.Contains(_searchFilter, StringComparison.OrdinalIgnoreCase);
                }

                if (!matchesID && !matchesName && !matchesLocation)
                    continue;
            }

            bool isSelected = _currentHeader != null && _currentHeader.HeaderID == header.HeaderID;
            string label = $"{header.HeaderID:D3}: {header.InternalName}";

            if (ImGui.Selectable(label, isSelected))
            {
                _currentHeader = header;
                _statusMessage = $"Selected header {header.HeaderID}: {header.InternalName}";
                _statusColor = new System.Numerics.Vector4(0.4f, 0.7f, 1.0f, 1.0f);
                _shouldScrollToSelection = true;
            }

            // Scroll to selected item
            if (isSelected && _shouldScrollToSelection)
            {
                ImGui.SetScrollHereY(0.5f); // Center the item
                _shouldScrollToSelection = false;
            }
        }

        ImGui.EndChild();
    }

    private void DrawHeaderEditor()
    {
        if (_currentHeader == null) return;

        // Scrollable editor area
        ImGui.BeginChild("HeaderEditorScroll", new System.Numerics.Vector2(0, 0), ImGuiChildFlags.None);

        // Map Structure section
        if (ImGui.CollapsingHeader("Map Structure", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Text("Matrix ID:");
            ImGui.SameLine(150);
            int matrixID = _currentHeader.MatrixID;
            ImGui.SetNextItemWidth(200);
            if (ImGui.InputInt("##matrixid", ref matrixID, 1, 10))
            {
                _currentHeader.MatrixID = (ushort)Math.Clamp(matrixID, 0, ushort.MaxValue);
            }

            // Button to open Matrix Editor
            if (_matrixEditorView != null)
            {
                DrawOpenEditorButton("openMatrix", "Ouvrir dans l'éditeur de matrices", "Matrix Editor", () =>
                {
                    _matrixEditorView.OpenWithMatrixID(_currentHeader.MatrixID);
                });
            }

            ImGui.Text("Area Data ID:");
            ImGui.SameLine(150);
            int areaDataID = _currentHeader.AreaDataID;
            ImGui.SetNextItemWidth(200);
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
            ImGui.SetNextItemWidth(200);
            if (ImGui.InputInt("##musicdayid", ref musicDayID, 1, 10))
            {
                _currentHeader.MusicDayID = (ushort)Math.Clamp(musicDayID, 0, ushort.MaxValue);
            }

            ImGui.Text("Music Night ID:");
            ImGui.SameLine(150);
            int musicNightID = _currentHeader.MusicNightID;
            ImGui.SetNextItemWidth(200);
            if (ImGui.InputInt("##musicnightid", ref musicNightID, 1, 10))
            {
                _currentHeader.MusicNightID = (ushort)Math.Clamp(musicNightID, 0, ushort.MaxValue);
            }

            // Weather dropdown with names (using Platinum weather)
            ImGui.Text("Weather:");
            ImGui.SameLine(150);
            string weatherName = GameConstants.GetWeatherName(_currentHeader.WeatherID);
            ImGui.SetNextItemWidth(300);
            if (ImGui.BeginCombo("##weathercombo", weatherName))
            {
                foreach (var kvp in GameConstants.PtWeatherNames.OrderBy(x => x.Key))
                {
                    bool isSelected = _currentHeader.WeatherID == kvp.Key;
                    if (ImGui.Selectable($"{kvp.Key}: {kvp.Value}", isSelected))
                    {
                        _currentHeader.WeatherID = kvp.Key;
                    }

                    // Show image tooltip on hover
                    if (ImGui.IsItemHovered())
                    {
                        if (_textureManager == null)
                        {
                            AppLogger.Warn("[HeaderEditor] TextureManager is null!");
                        }
                        else
                        {
                            var texture = _textureManager.LoadWeatherImage(kvp.Key);
                            if (texture != null)
                            {
                                ImGui.BeginTooltip();
                                ImGui.Image(texture.Value, new System.Numerics.Vector2(256, 192));
                                ImGui.EndTooltip();
                            }
                            else
                            {
                                AppLogger.Warn($"[HeaderEditor] Failed to load weather image for ID {kvp.Key}");
                            }
                        }
                    }

                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }

            // Show image tooltip when hovering the closed combo
            if (ImGui.IsItemHovered() && _textureManager != null)
            {
                var texture = _textureManager.LoadWeatherImage(_currentHeader.WeatherID);
                if (texture != null)
                {
                    ImGui.BeginTooltip();
                    ImGui.Image(texture.Value, new System.Numerics.Vector2(256, 192));
                    ImGui.EndTooltip();
                }
            }

            // Camera Angle dropdown with names (using DPPt cameras)
            ImGui.Text("Camera Angle:");
            ImGui.SameLine(150);
            string cameraName = GameConstants.GetCameraAngleName(_currentHeader.CameraAngleID);
            ImGui.SetNextItemWidth(300);
            if (ImGui.BeginCombo("##cameracombo", cameraName))
            {
                foreach (var kvp in GameConstants.DPPtCameraAngles.OrderBy(x => x.Key))
                {
                    bool isSelected = _currentHeader.CameraAngleID == kvp.Key;
                    if (ImGui.Selectable($"{kvp.Key}: {kvp.Value}", isSelected))
                    {
                        _currentHeader.CameraAngleID = kvp.Key;
                    }

                    // Show image tooltip on hover
                    if (ImGui.IsItemHovered())
                    {
                        if (_textureManager == null)
                        {
                            AppLogger.Warn("[HeaderEditor] TextureManager is null for camera!");
                        }
                        else
                        {
                            var texture = _textureManager.LoadCameraImage(kvp.Key);
                            if (texture != null)
                            {
                                ImGui.BeginTooltip();
                                ImGui.Image(texture.Value, new System.Numerics.Vector2(256, 192));
                                ImGui.EndTooltip();
                            }
                            else
                            {
                                AppLogger.Warn($"[HeaderEditor] Failed to load camera image for ID {kvp.Key}");
                            }
                        }
                    }

                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
                ImGui.EndCombo();
            }

            // Show image tooltip when hovering the closed combo
            if (ImGui.IsItemHovered() && _textureManager != null)
            {
                var texture = _textureManager.LoadCameraImage(_currentHeader.CameraAngleID);
                if (texture != null)
                {
                    ImGui.BeginTooltip();
                    ImGui.Image(texture.Value, new System.Numerics.Vector2(256, 192));
                    ImGui.EndTooltip();
                }
            }
        }

        ImGui.Spacing();

        // Scripts & Events section
        if (ImGui.CollapsingHeader("Scripts & Events", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Text("Script File ID:");
            ImGui.SameLine(150);
            int scriptID = _currentHeader.ScriptFileID;
            ImGui.SetNextItemWidth(200);
            if (ImGui.InputInt("##scriptid", ref scriptID, 1, 10))
            {
                _currentHeader.ScriptFileID = (ushort)Math.Clamp(scriptID, 0, ushort.MaxValue);
            }

            ImGui.Text("Level Script ID:");
            ImGui.SameLine(150);
            int levelScriptID = _currentHeader.LevelScriptID;
            ImGui.SetNextItemWidth(200);
            if (ImGui.InputInt("##levelscriptid", ref levelScriptID, 1, 10))
            {
                _currentHeader.LevelScriptID = (ushort)Math.Clamp(levelScriptID, 0, ushort.MaxValue);
            }

            // Button to open Level Script Editor
            if (_levelScriptEditorView != null)
            {
                DrawOpenEditorButton("openLevelScript", "Ouvrir dans l'éditeur de level scripts", "Level Script Editor", () =>
                {
                    _levelScriptEditorView.OpenWithScriptID(_currentHeader.LevelScriptID);
                });
            }

            ImGui.Text("Event File ID:");
            ImGui.SameLine(150);
            int eventID = _currentHeader.EventFileID;
            ImGui.SetNextItemWidth(200);
            if (ImGui.InputInt("##eventid", ref eventID, 1, 10))
            {
                _currentHeader.EventFileID = (ushort)Math.Clamp(eventID, 0, ushort.MaxValue);
            }

            ImGui.Text("Text Archive ID:");
            ImGui.SameLine(150);
            int textArchiveID = _currentHeader.TextArchiveID;
            ImGui.SetNextItemWidth(200);
            if (ImGui.InputInt("##textarchiveid", ref textArchiveID, 1, 10))
            {
                _currentHeader.TextArchiveID = (ushort)Math.Clamp(textArchiveID, 0, ushort.MaxValue);
            }

            // Button to open Text Editor
            if (_textEditorWindow != null)
            {
                DrawOpenEditorButton("openText", "Ouvrir dans l'éditeur de textes", "Text Editor", () =>
                {
                    _textEditorWindow.OpenWithArchiveID(_currentHeader.TextArchiveID);
                });
            }
        }

        ImGui.Spacing();

        // Location & Encounters section
        if (ImGui.CollapsingHeader("Location & Encounters"))
        {
            // Location Name dropdown with names from text archive
            ImGui.Text("Location Name:");
            ImGui.SameLine(150);

            if (_locationNamesLoaded && _locationNames.Count > 0)
            {
                // Display location name from text archive
                string currentLocationName = _currentHeader.LocationName < _locationNames.Count
                    ? _locationNames[_currentHeader.LocationName]
                    : $"<Unknown {_currentHeader.LocationName}>";

                ImGui.SetNextItemWidth(300);
                if (ImGui.BeginCombo("##locationcombo", $"{_currentHeader.LocationName}: {currentLocationName}"))
                {
                    for (int i = 0; i < Math.Min(_locationNames.Count, 256); i++)
                    {
                        bool isSelected = _currentHeader.LocationName == i;
                        string locationText = !string.IsNullOrWhiteSpace(_locationNames[i])
                            ? _locationNames[i]
                            : $"<Empty {i}>";

                        if (ImGui.Selectable($"{i}: {locationText}", isSelected))
                        {
                            _currentHeader.LocationName = (byte)i;
                        }
                        if (isSelected)
                            ImGui.SetItemDefaultFocus();
                    }
                    ImGui.EndCombo();
                }
            }
            else
            {
                // Fallback to simple input if location names not loaded
                int locationName = _currentHeader.LocationName;
                if (ImGui.InputInt("##locationname", ref locationName, 1, 10))
                {
                    _currentHeader.LocationName = (byte)Math.Clamp(locationName, 0, byte.MaxValue);
                }
            }

            ImGui.Text("Area Icon:");
            ImGui.SameLine(150);
            int areaIcon = _currentHeader.AreaIcon;
            ImGui.SetNextItemWidth(200);
            if (ImGui.InputInt("##areaicon", ref areaIcon, 1, 10))
            {
                _currentHeader.AreaIcon = (byte)Math.Clamp(areaIcon, 0, byte.MaxValue);
            }

            ImGui.Text("Wild Pokémon:");
            ImGui.SameLine(150);
            int wildPokemon = _currentHeader.WildPokemon;
            ImGui.SetNextItemWidth(200);
            if (ImGui.InputInt("##wildpokemon", ref wildPokemon, 1, 10))
            {
                _currentHeader.WildPokemon = (byte)Math.Clamp(wildPokemon, 0, byte.MaxValue);
            }

            // Button to open Wild Editor (only if not 0xFF = no encounters)
            if (_wildEditorView != null && _currentHeader.WildPokemon != MapHeader.NO_WILD_ENCOUNTERS)
            {
                DrawOpenEditorButton("openWild", "Ouvrir dans l'éditeur de rencontres sauvages", "Wild Encounter Editor", () =>
                {
                    _wildEditorView.OpenWithEncounterID(_currentHeader.WildPokemon);
                });
            }

            ImGui.Text("Time ID:");
            ImGui.SameLine(150);
            int timeID = _currentHeader.TimeID;
            ImGui.SetNextItemWidth(200);
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
            ImGui.SetNextItemWidth(200);
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

        if (ImGui.Button($"{FontAwesomeIcons.Save} Save Header", new System.Numerics.Vector2(-1, 40)))
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

        ImGui.EndChild(); // End HeaderEditorScroll
    }

    /// <summary>
    /// Draw a navigation button that opens an editor with a specific ID
    /// </summary>
    private void DrawOpenEditorButton(string uniqueId, string tooltip, string windowName, Action openAction)
    {
        ImGui.SameLine();

        // Draw a small arrow button with unique ID (##uniqueId makes it invisible to user but unique to ImGui)
        ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.3f, 0.6f, 0.9f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, new System.Numerics.Vector4(0.4f, 0.7f, 1.0f, 1.0f));
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, new System.Numerics.Vector4(0.2f, 0.5f, 0.8f, 1.0f));

        if (ImGui.Button($"{FontAwesomeIcons.ExternalLink}##{uniqueId}"))
        {
            AppLogger.Info($"[HeaderEditor] Navigation button clicked: {tooltip}");
            openAction();

            // Force focus on the target window (works better for docked tabs than internal _shouldFocus)
            ImGui.SetWindowFocus(windowName);
        }

        ImGui.PopStyleColor(3);

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(tooltip);
        }
    }
}
