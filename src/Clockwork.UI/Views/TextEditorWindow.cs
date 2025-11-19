using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Linq;
using ImGuiNET;
using Clockwork.Core;
using Clockwork.Core.Services;
using Clockwork.Core.Formats.NDS.MessageEnc;

namespace Clockwork.UI.Views
{
    /// <summary>
    /// Text Editor window for editing Nintendo DS message archives
    /// </summary>
    public class TextEditorWindow : IView
    {
        private readonly ApplicationContext _appContext;
        private RomService? _romService;

        private List<string> messages = new List<string>();
        private string currentFilePath = "";
        private int selectedMessageIndex = -1;
        private string editBuffer = "";
        private bool isDirty = false;
        private string statusMessage = "";
        private float statusMessageTimer = 0f;
        private ushort encryptionKey = 0; // Store the encryption key from .txt file

        // ROM text archives
        private List<string> availableTextArchives = new List<string>();
        private int selectedArchiveIndex = -1;
        private string[] archiveNames = Array.Empty<string>();

        // Search functionality
        private string searchQuery = "";
        private List<int> searchResults = new List<int>();
        private int currentSearchIndex = -1;

        public bool IsVisible { get; set; } = false;

        public TextEditorWindow(ApplicationContext appContext)
        {
            _appContext = appContext;
            _romService = _appContext.GetService<RomService>();
        }

        /// <summary>
        /// Refresh the list of available text archives from the loaded ROM
        /// </summary>
        private void RefreshTextArchivesList()
        {
            availableTextArchives.Clear();
            archiveNames = Array.Empty<string>();
            selectedArchiveIndex = -1;

            if (_romService?.CurrentRom?.GameDirectories == null)
                return;

            if (!_romService.CurrentRom.GameDirectories.TryGetValue("expandedTextArchives", out string? textArchivesPath))
                return;

            if (!Directory.Exists(textArchivesPath))
                return;

            try
            {
                var files = Directory.GetFiles(textArchivesPath, "*.txt") // Only .txt files from expanded/
                    .OrderBy(f => f)
                    .ToList();

                availableTextArchives = files;
                archiveNames = files.Select(f => Path.GetFileName(f)).ToArray();

                if (availableTextArchives.Count > 0)
                {
                    SetStatusMessage($"Found {availableTextArchives.Count} text archives");
                }
                else
                {
                    SetStatusMessage("No text archives found in ROM");
                }
            }
            catch (Exception ex)
            {
                SetStatusMessage($"Error listing text archives: {ex.Message}");
            }
        }

        /// <summary>
        /// Load a message archive file (.txt format from expanded/)
        /// Format:
        /// # Key: 0x1234
        /// message 1
        /// message 2
        /// ...
        /// </summary>
        public void LoadFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    SetStatusMessage($"File not found: {filePath}");
                    return;
                }

                var lines = File.ReadAllLines(filePath).ToList();

                if (lines.Count == 0)
                {
                    SetStatusMessage("File is empty");
                    return;
                }

                // Parse first line to extract encryption key
                string firstLine = lines[0];
                if (firstLine.StartsWith("# Key: 0x") || firstLine.StartsWith("# Key: "))
                {
                    string keyStr = firstLine.Substring(7).Trim();
                    if (keyStr.StartsWith("0x"))
                        keyStr = keyStr.Substring(2);

                    if (ushort.TryParse(keyStr, System.Globalization.NumberStyles.HexNumber, null, out ushort key))
                    {
                        encryptionKey = key;
                    }

                    // Remove first line (key line)
                    lines.RemoveAt(0);
                }
                else
                {
                    encryptionKey = 0;
                }

                messages = lines;
                currentFilePath = filePath;
                selectedMessageIndex = -1;
                editBuffer = "";
                isDirty = false;
                SetStatusMessage($"Loaded {messages.Count} messages from {Path.GetFileName(filePath)} (Key: 0x{encryptionKey:X4})");
            }
            catch (Exception ex)
            {
                SetStatusMessage($"Error loading file: {ex.Message}");
                messages = new List<string>();
            }
        }

        /// <summary>
        /// Save the current message archive (.txt format to expanded/ and repack to binary in unpacked/)
        /// </summary>
        public void SaveFile()
        {
            if (string.IsNullOrEmpty(currentFilePath))
            {
                SetStatusMessage("No file loaded");
                return;
            }

            try
            {
                // Commit current edit
                if (selectedMessageIndex >= 0 && selectedMessageIndex < messages.Count)
                {
                    messages[selectedMessageIndex] = editBuffer;
                }

                // Write with key in first line to expanded/ .txt file
                var lines = new List<string>();
                lines.Add($"# Key: 0x{encryptionKey:X4}");
                lines.AddRange(messages);

                File.WriteAllLines(currentFilePath, lines);
                isDirty = false;

                // Repack to binary format in unpacked/
                RepackToBinary();

                SetStatusMessage($"Saved {messages.Count} messages to {Path.GetFileName(currentFilePath)} and repacked to ROM");
            }
            catch (Exception ex)
            {
                SetStatusMessage($"Error saving file: {ex.Message}");
            }
        }

        /// <summary>
        /// Repacks the current text archive to binary format in unpacked/
        /// </summary>
        private void RepackToBinary()
        {
            try
            {
                // Determine the binary file path
                // Example: expanded/textArchives/0000.txt -> unpacked/textArchives/0000

                // Use Path methods for cross-platform compatibility
                string normalizedPath = currentFilePath.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar);

                // Check if path contains "expanded"
                if (!normalizedPath.Contains($"{Path.DirectorySeparatorChar}expanded{Path.DirectorySeparatorChar}"))
                {
                    SetStatusMessage("Warning: File is not in expanded/ directory, cannot repack");
                    return;
                }

                string binaryPath = normalizedPath
                    .Replace($"{Path.DirectorySeparatorChar}expanded{Path.DirectorySeparatorChar}",
                             $"{Path.DirectorySeparatorChar}unpacked{Path.DirectorySeparatorChar}");

                // Remove .txt extension if present
                if (binaryPath.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
                {
                    binaryPath = binaryPath.Substring(0, binaryPath.Length - 4);
                }

                // Ensure the directory exists
                string? binaryDir = Path.GetDirectoryName(binaryPath);
                if (!string.IsNullOrEmpty(binaryDir) && !Directory.Exists(binaryDir))
                {
                    Directory.CreateDirectory(binaryDir);
                }

                // Repack using EncryptText
                bool success = EncryptText.RepackTextArchive(currentFilePath, binaryPath, false);

                if (!success)
                {
                    SetStatusMessage("Warning: Failed to repack to binary format");
                }
            }
            catch (Exception ex)
            {
                SetStatusMessage($"Warning: Failed to repack binary: {ex.Message}");
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
            if (availableTextArchives.Count == 0 && _romService?.CurrentRom?.IsLoaded == true)
            {
                RefreshTextArchivesList();
            }

            // Update status message timer
            if (statusMessageTimer > 0f)
            {
                statusMessageTimer -= ImGui.GetIO().DeltaTime;
            }

            bool isVisible = IsVisible;
            if (ImGui.Begin("Text Editor", ref isVisible))
            {
                DrawToolbar();
                ImGui.Separator();

                if (messages.Count > 0)
                {
                    // Split view: message list on left, editor on right
                    DrawSplitView();
                }
                else
                {
                    ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1.0f),
                        "No file loaded. Use File > Load to open a message archive.");
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
                ImGui.Text("Text Archive:");
                ImGui.SameLine();
                ImGui.SetNextItemWidth(200);
                if (ImGui.Combo("##archive", ref selectedArchiveIndex, archiveNames, archiveNames.Length))
                {
                    // Archive selection changed - auto-load the selected archive
                    if (selectedArchiveIndex >= 0 && selectedArchiveIndex < availableTextArchives.Count)
                    {
                        string archivePath = availableTextArchives[selectedArchiveIndex];
                        LoadFile(archivePath);
                    }
                }

                ImGui.SameLine();
            }
            else if (_romService?.CurrentRom?.IsLoaded == true)
            {
                ImGui.TextColored(new Vector4(1.0f, 0.6f, 0.0f, 1.0f), "No text archives found");
                ImGui.SameLine();
                if (ImGui.Button("Refresh"))
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

            ImGui.SameLine();
            if (ImGui.Button("Save") && !string.IsNullOrEmpty(currentFilePath))
            {
                SaveFile();
            }

            ImGui.SameLine();
            if (ImGui.Button("Add Message"))
            {
                messages.Add("");
                selectedMessageIndex = messages.Count - 1;
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
            if (ImGui.Button("Search"))
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
                        editBuffer = messages[selectedMessageIndex];
                    }

                    ImGui.SameLine();
                    if (ImGui.Button("Next") && currentSearchIndex < searchResults.Count - 1)
                    {
                        currentSearchIndex++;
                        selectedMessageIndex = searchResults[currentSearchIndex];
                        editBuffer = messages[selectedMessageIndex];
                    }
                }
            }
        }

        private void DrawSplitView()
        {
            var availableRegion = ImGui.GetContentRegionAvail();
            float listWidth = availableRegion.X * 0.3f;
            float editorWidth = availableRegion.X * 0.7f - 10;

            // Left panel: Message list
            ImGui.BeginChild("MessageList", new Vector2(listWidth, -30), ImGuiChildFlags.Border);
            {
                ImGui.Text($"Messages ({messages.Count})");
                ImGui.Separator();

                for (int i = 0; i < messages.Count; i++)
                {
                    bool isSelected = selectedMessageIndex == i;
                    bool isSearchResult = searchResults.Contains(i);

                    // Highlight search results
                    if (isSearchResult && !isSelected)
                    {
                        ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1.0f, 1.0f, 0.0f, 1.0f));
                    }

                    string preview = messages[i];
                    if (preview.Length > 50)
                    {
                        preview = preview.Substring(0, 50) + "...";
                    }
                    preview = preview.Replace("\n", " ").Replace("\r", " ");

                    if (ImGui.Selectable($"{i:D4}: {preview}##msg{i}", isSelected))
                    {
                        // Save current edit before switching
                        if (selectedMessageIndex >= 0 && selectedMessageIndex < messages.Count)
                        {
                            if (messages[selectedMessageIndex] != editBuffer)
                            {
                                messages[selectedMessageIndex] = editBuffer;
                                isDirty = true;
                            }
                        }

                        selectedMessageIndex = i;
                        editBuffer = messages[i];
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
                            messages.RemoveAt(i);
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
                            messages.Insert(i + 1, messages[i]);
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
                if (selectedMessageIndex >= 0 && selectedMessageIndex < messages.Count)
                {
                    ImGui.Text($"Editing Message {selectedMessageIndex:D4}");
                    ImGui.Separator();

                    // Multi-line text editor
                    if (ImGui.InputTextMultiline("##editor", ref editBuffer, 10000,
                        new Vector2(-20, -100)))
                    {
                        if (messages[selectedMessageIndex] != editBuffer)
                        {
                            isDirty = true;
                        }
                    }

                    ImGui.Separator();
                    ImGui.TextWrapped("Special characters: \\n (newline), \\r (return), \\f (form feed), [PK] (Pokémon), [MN] (Move name)");

                    if (ImGui.Button("Apply Changes"))
                    {
                        messages[selectedMessageIndex] = editBuffer;
                        isDirty = true;
                        SetStatusMessage("Changes applied");
                    }

                    ImGui.SameLine();
                    if (ImGui.Button("Revert"))
                    {
                        editBuffer = messages[selectedMessageIndex];
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

            if (!string.IsNullOrEmpty(currentFilePath))
            {
                status += $"File: {Path.GetFileName(currentFilePath)}";
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

            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                return;
            }

            for (int i = 0; i < messages.Count; i++)
            {
                if (messages[i].Contains(searchQuery, StringComparison.OrdinalIgnoreCase))
                {
                    searchResults.Add(i);
                }
            }

            if (searchResults.Count > 0)
            {
                currentSearchIndex = 0;
                selectedMessageIndex = searchResults[0];
                editBuffer = messages[selectedMessageIndex];
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
        public int MessageCount => messages.Count;
        public string CurrentFile => currentFilePath;
    }
}
