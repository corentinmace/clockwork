using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using ImGuiNET;
using Clockwork.Core;
using Clockwork.Core.Models;
using Clockwork.Core.Services;
using Clockwork.Core.Logging;
using Clockwork.UI.Icons;

namespace Clockwork.UI.Views
{
    /// <summary>
    /// Text Editor window for editing Nintendo DS message archives
    /// </summary>
    public class TextEditorWindow : IView
    {
        private readonly ApplicationContext _appContext;
        private RomService? _romService;
        private TextArchiveService? _textArchiveService;

        // Current archive being edited
        private TextArchive? _currentArchive;
        private int _currentArchiveID = -1;
        private int selectedMessageIndex = -1;
        private string editBuffer = "";
        private bool isDirty = false;
        private string statusMessage = "";
        private float statusMessageTimer = 0f;

        // Available archives in ROM
        private List<int> _availableArchiveIDs = new List<int>();
        private int selectedArchiveIndex = -1;
        private string[] archiveNames = Array.Empty<string>();

        // Search functionality
        private string searchQuery = "";
        private List<int> searchResults = new List<int>();
        private int currentSearchIndex = -1;

        // Focus management
        private bool _shouldFocus = false;

        public bool IsVisible { get; set; } = false;

        public TextEditorWindow(ApplicationContext appContext)
        {
            _appContext = appContext;
            _romService = _appContext.GetService<RomService>();
            _textArchiveService = _appContext.GetService<TextArchiveService>();
        }

        /// <summary>
        /// Refresh the list of available text archives from the loaded ROM
        /// </summary>
        private void RefreshTextArchivesList()
        {
            _availableArchiveIDs.Clear();
            archiveNames = Array.Empty<string>();
            selectedArchiveIndex = -1;

            if (_textArchiveService == null || _romService?.CurrentRom?.IsLoaded != true)
            {
                SetStatusMessage("No ROM loaded");
                return;
            }

            try
            {
                _availableArchiveIDs = _textArchiveService.GetAvailableArchiveIDs();
                archiveNames = _availableArchiveIDs.Select(id => $"{id:D4}").ToArray();

                if (_availableArchiveIDs.Count > 0)
                {
                    SetStatusMessage($"Found {_availableArchiveIDs.Count} text archives");
                }
                else
                {
                    SetStatusMessage("No text archives found in ROM");
                }

                AppLogger.Info($"Refreshed text archives list: {_availableArchiveIDs.Count} archives found");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error listing text archives: {ex.Message}");
                SetStatusMessage($"Error listing text archives: {ex.Message}");
            }
        }

        /// <summary>
        /// Open the text editor and load a specific text archive by its ID
        /// </summary>
        public void OpenWithArchiveID(int archiveID)
        {
            AppLogger.Info($"[TextEditor] OpenWithArchiveID called with ID: {archiveID}");

            // Always open the window and request focus
            IsVisible = true;
            _shouldFocus = true;

            // Refresh archive list if not already loaded
            if (_availableArchiveIDs.Count == 0)
            {
                AppLogger.Debug("[TextEditor] Refreshing text archives list...");
                RefreshTextArchivesList();
            }

            // Check if ROM is loaded
            if (_textArchiveService == null || _romService?.CurrentRom?.IsLoaded != true)
            {
                AppLogger.Warn("[TextEditor] No ROM loaded or TextArchiveService is null");
                SetStatusMessage("No ROM loaded. Please load a ROM first.");
                return;
            }

            // Load the archive via the service (auto-extracts if needed)
            LoadArchive(archiveID);
        }

        /// <summary>
        /// Load a text archive by ID using TextArchiveService
        /// </summary>
        private void LoadArchive(int archiveID)
        {
            if (_textArchiveService == null)
            {
                SetStatusMessage("TextArchiveService not available");
                return;
            }

            try
            {
                AppLogger.Info($"Loading text archive {archiveID}...");

                // Load via service (handles auto-extraction)
                _currentArchive = _textArchiveService.LoadTextArchive(archiveID);

                if (_currentArchive == null)
                {
                    AppLogger.Error($"Failed to load text archive {archiveID}");
                    SetStatusMessage($"Failed to load text archive {archiveID}");
                    return;
                }

                _currentArchiveID = archiveID;
                selectedMessageIndex = -1;
                editBuffer = "";
                isDirty = false;

                // Update selection in list
                selectedArchiveIndex = _availableArchiveIDs.IndexOf(archiveID);

                SetStatusMessage($"Loaded archive {archiveID}: {_currentArchive.MessageCount} messages (Key: 0x{_currentArchive.Key:X4})");
                AppLogger.Info($"Successfully loaded text archive {archiveID}: {_currentArchive.MessageCount} messages");
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error loading text archive {archiveID}: {ex.Message}");
                SetStatusMessage($"Error loading archive: {ex.Message}");
                _currentArchive = null;
                _currentArchiveID = -1;
            }
        }

        /// <summary>
        /// Save the current text archive
        /// </summary>
        public void SaveArchive()
        {
            if (_currentArchive == null || _currentArchiveID < 0)
            {
                SetStatusMessage("No archive loaded");
                return;
            }

            if (_textArchiveService == null)
            {
                SetStatusMessage("TextArchiveService not available");
                return;
            }

            try
            {
                // Commit current edit
                if (selectedMessageIndex >= 0 && selectedMessageIndex < _currentArchive.MessageCount)
                {
                    _currentArchive.SetMessage(selectedMessageIndex, editBuffer);
                }

                // Save via service (saves to both .txt and .bin)
                bool success = _textArchiveService.SaveTextArchive(_currentArchiveID, _currentArchive);

                if (success)
                {
                    isDirty = false;
                    SetStatusMessage($"Saved archive {_currentArchiveID}");
                    AppLogger.Info($"Saved text archive {_currentArchiveID}");
                }
                else
                {
                    SetStatusMessage($"Failed to save archive {_currentArchiveID}");
                }
            }
            catch (Exception ex)
            {
                AppLogger.Error($"Error saving text archive: {ex.Message}");
                SetStatusMessage($"Error saving: {ex.Message}");
            }
        }

        /// <summary>
        /// Draw the ImGui window
        /// </summary>
        public void Draw()
        {
            if (!IsVisible)
                return;

            // Refresh text archives list when window becomes visible
            if (_availableArchiveIDs.Count == 0 && _romService?.CurrentRom?.IsLoaded == true)
            {
                RefreshTextArchivesList();
            }

            // Update status message timer
            if (statusMessageTimer > 0f)
            {
                statusMessageTimer -= ImGui.GetIO().DeltaTime;
            }

            bool isVisible = IsVisible;
            if (ImGui.Begin($"{FontAwesomeIcons.Font} Text Editor", ref isVisible))
            {
                // Apply focus if requested
                if (_shouldFocus)
                {
                    ImGui.SetWindowFocus();
                    _shouldFocus = false;
                }

                DrawToolbar();
                ImGui.Separator();

                if (_currentArchive != null && _currentArchive.MessageCount > 0)
                {
                    // Split view: message list on left, editor on right
                    DrawSplitView();
                }
                else
                {
                    ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f),
                        "No archive loaded. Select an archive from the dropdown above.");
                }

                // Status bar at bottom
                ImGui.Separator();
                DrawStatusBar();
            }
            ImGui.End();

            // Update visibility state from window close button
            IsVisible = isVisible;
        }

        private void DrawToolbar()
        {
            // Text Archive selector (if ROM is loaded)
            if (_romService?.CurrentRom?.IsLoaded == true && archiveNames.Length > 0)
            {
                ImGui.Text("Archive:");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(150);
                if (ImGui.Combo("##archive", ref selectedArchiveIndex, archiveNames, archiveNames.Length))
                {
                    // Archive selection changed - auto-load the selected archive
                    if (selectedArchiveIndex >= 0 && selectedArchiveIndex < _availableArchiveIDs.Count)
                    {
                        int archiveID = _availableArchiveIDs[selectedArchiveIndex];
                        LoadArchive(archiveID);
                    }
                }

                ImGui.SameLine();
            }
            else if (_romService?.CurrentRom?.IsLoaded == true)
            {
                ImGui.TextColored(new Vector4(1.0f, 0.6f, 0.0f, 1.0f), "No text archives found");
                ImGui.SameLine();
                if (ImGui.Button($"{FontAwesomeIcons.Refresh} Refresh"))
                {
                    RefreshTextArchivesList();
                }
                ImGui.SameLine();
            }
            else
            {
                ImGui.TextColored(new Vector4(1.0f, 0.6f, 0.0f, 1.0f), "No ROM loaded");
                ImGui.SameLine();
            }

            if (ImGui.Button($"{FontAwesomeIcons.Save} Save") && _currentArchive != null)
            {
                SaveArchive();
            }

            ImGui.SameLine();
            if (ImGui.Button($"{FontAwesomeIcons.Plus} Add Message") && _currentArchive != null)
            {
                _currentArchive.Messages.Add("");
                selectedMessageIndex = _currentArchive.MessageCount - 1;
                editBuffer = "";
                isDirty = true;
            }

            // Search bar
            ImGui.SameLine();
            ImGui.SetNextItemWidth(200);
            if (ImGui.InputText("##search", ref searchQuery, 256, ImGuiInputTextFlags.EnterReturnsTrue))
            {
                PerformSearch();
            }

            ImGui.SameLine();
            if (ImGui.Button($"{FontAwesomeIcons.Search} Search"))
            {
                PerformSearch();
            }

            if (searchResults.Count > 0)
            {
                ImGui.SameLine();
                ImGui.Text($"{searchResults.Count} results");

                if (currentSearchIndex >= 0)
                {
                    ImGui.SameLine();
                    if (ImGui.Button("Previous") && currentSearchIndex > 0)
                    {
                        currentSearchIndex--;
                        selectedMessageIndex = searchResults[currentSearchIndex];
                        if (_currentArchive != null)
                            editBuffer = _currentArchive.GetMessage(selectedMessageIndex);
                    }

                    ImGui.SameLine();
                    if (ImGui.Button("Next") && currentSearchIndex < searchResults.Count - 1)
                    {
                        currentSearchIndex++;
                        selectedMessageIndex = searchResults[currentSearchIndex];
                        if (_currentArchive != null)
                            editBuffer = _currentArchive.GetMessage(selectedMessageIndex);
                    }
                }
            }
        }

        private void DrawSplitView()
        {
            if (_currentArchive == null)
                return;

            var availableRegion = ImGui.GetContentRegionAvail();
            float listWidth = availableRegion.X * 0.3f;
            float editorWidth = availableRegion.X * 0.7f - 10;

            // Left panel: Message list
            ImGui.BeginChild("MessageList", new Vector2(listWidth, -30), ImGuiChildFlags.Border);
            {
                ImGui.Text($"Messages ({_currentArchive.MessageCount})");
                ImGui.Separator();

                for (int i = 0; i < _currentArchive.MessageCount; i++)
                {
                    bool isSelected = selectedMessageIndex == i;
                    bool isSearchResult = searchResults.Contains(i);

                    // Highlight search results
                    if (isSearchResult && !isSelected)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 1.0f, 0.0f, 1.0f));
                    }

                    string message = _currentArchive.GetMessage(i);
                    string preview = message;
                    if (preview.Length > 50)
                    {
                        preview = preview.Substring(0, 50) + "...";
                    }
                    preview = preview.Replace("\n", " ").Replace("\r", " ");

                    if (ImGui.Selectable($"{i:D4}: {preview}##msg{i}", isSelected))
                    {
                        // Save current edit before switching
                        if (selectedMessageIndex >= 0 && selectedMessageIndex < _currentArchive.MessageCount)
                        {
                            string currentMessage = _currentArchive.GetMessage(selectedMessageIndex);
                            if (currentMessage != editBuffer)
                            {
                                _currentArchive.SetMessage(selectedMessageIndex, editBuffer);
                                isDirty = true;
                            }
                        }

                        selectedMessageIndex = i;
                        editBuffer = message;
                    }

                    if (isSearchResult && !isSelected)
                    {
                        ImGui.PopStyleColor();
                    }

                    // Context menu for each message
                    if (ImGui.BeginPopupContextItem($"msgctx{i}"))
                    {
                        if (ImGui.MenuItem("Delete"))
                        {
                            _currentArchive.Messages.RemoveAt(i);
                            if (selectedMessageIndex == i)
                            {
                                selectedMessageIndex = -1;
                                editBuffer = "";
                            }
                            else if (selectedMessageIndex > i)
                            {
                                selectedMessageIndex--;
                            }
                            isDirty = true;
                        }

                        if (ImGui.MenuItem("Duplicate"))
                        {
                            _currentArchive.Messages.Insert(i + 1, message);
                            isDirty = true;
                        }

                        ImGui.EndPopup();
                    }
                }
            }
            ImGui.EndChild();

            ImGui.SameLine();

            // Right panel: Message editor
            ImGui.BeginChild("MessageEditor", new Vector2(editorWidth, -30), ImGuiChildFlags.Border);
            {
                if (selectedMessageIndex >= 0 && selectedMessageIndex < _currentArchive.MessageCount)
                {
                    ImGui.Text($"Editing Message {selectedMessageIndex:D4}");
                    ImGui.Separator();

                    // Multi-line text editor
                    if (ImGui.InputTextMultiline("##editor", ref editBuffer, 10000,
                        new Vector2(-20, -100)))
                    {
                        string currentMessage = _currentArchive.GetMessage(selectedMessageIndex);
                        if (currentMessage != editBuffer)
                        {
                            isDirty = true;
                        }
                    }

                    ImGui.Separator();
                    ImGui.TextWrapped("Special sequences: \\n (newline), {COMMAND, param1, param2}, {TRAINER_NAME:name}");

                    if (ImGui.Button("Apply Changes"))
                    {
                        _currentArchive.SetMessage(selectedMessageIndex, editBuffer);
                        isDirty = true;
                        SetStatusMessage("Changes applied");
                    }

                    ImGui.SameLine();
                    if (ImGui.Button("Revert"))
                    {
                        editBuffer = _currentArchive.GetMessage(selectedMessageIndex);
                        SetStatusMessage("Changes reverted");
                    }
                }
                else
                {
                    ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f),
                        "Select a message from the list to edit");
                }
            }
            ImGui.EndChild();
        }

        private void DrawStatusBar()
        {
            string status = "";

            if (_currentArchive != null && _currentArchiveID >= 0)
            {
                status += $"Archive: {_currentArchiveID:D4} ({_currentArchive.MessageCount} messages, Key: 0x{_currentArchive.Key:X4})";
            }

            if (isDirty)
            {
                status += " [Modified]";
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 0.5f, 0.0f, 1.0f));
            }

            ImGui.Text(status);

            if (isDirty)
            {
                ImGui.PopStyleColor();
            }

            if (statusMessageTimer > 0f && !string.IsNullOrEmpty(statusMessage))
            {
                ImGui.SameLine();
                ImGui.TextColored(new Vector4(0.0f, 1.0f, 0.0f, 1.0f), $" | {statusMessage}");
            }
        }

        private void PerformSearch()
        {
            searchResults.Clear();
            currentSearchIndex = -1;

            if (string.IsNullOrWhiteSpace(searchQuery) || _currentArchive == null)
            {
                return;
            }

            for (int i = 0; i < _currentArchive.MessageCount; i++)
            {
                string message = _currentArchive.GetMessage(i);
                if (message.Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
                {
                    searchResults.Add(i);
                }
            }

            if (searchResults.Count > 0)
            {
                currentSearchIndex = 0;
                selectedMessageIndex = searchResults[0];
                editBuffer = _currentArchive.GetMessage(selectedMessageIndex);
                SetStatusMessage($"Found {searchResults.Count} matches");
            }
            else
            {
                SetStatusMessage("No matches found");
            }
        }

        private void SetStatusMessage(string message)
        {
            statusMessage = message;
            statusMessageTimer = 3.0f; // Show for 3 seconds
        }

        public bool IsDirty => isDirty;
        public int MessageCount => _currentArchive?.MessageCount ?? 0;
        public int CurrentArchiveID => _currentArchiveID;
    }
}
