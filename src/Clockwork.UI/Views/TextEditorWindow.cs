using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using ImGuiNET;
using Clockwork.Core.Formats.NDS.MessageEnc;

namespace Clockwork.UI.Views
{
    /// <summary>
    /// Text Editor window for editing Nintendo DS message archives
    /// </summary>
    public class TextEditorWindow
    {
        private List<string> messages = new List<string>();
        private string currentFilePath = "";
        private int selectedMessageIndex = -1;
        private string editBuffer = "";
        private bool isDirty = false;
        private string statusMessage = "";
        private float statusMessageTimer = 0f;

        // Search functionality
        private string searchQuery = "";
        private List<int> searchResults = new List<int>();
        private int currentSearchIndex = -1;

        public TextEditorWindow()
        {
        }

        /// <summary>
        /// Load a message archive file
        /// </summary>
        public void LoadFile(string filePath)
        {
            try
            {
                messages = EncryptText.ReadMessageArchive(filePath, false);
                currentFilePath = filePath;
                selectedMessageIndex = -1;
                editBuffer = "";
                isDirty = false;
                SetStatusMessage($"Loaded {messages.Count} messages from {Path.GetFileName(filePath)}");
            }
            catch (Exception ex)
            {
                SetStatusMessage($"Error loading file: {ex.Message}");
                messages = new List<string>();
            }
        }

        /// <summary>
        /// Save the current message archive
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

                EncryptText.WriteMessageArchive(currentFilePath, messages, false);
                isDirty = false;
                SetStatusMessage($"Saved {messages.Count} messages to {Path.GetFileName(currentFilePath)}");
            }
            catch (Exception ex)
            {
                SetStatusMessage($"Error saving file: {ex.Message}");
            }
        }

        /// <summary>
        /// Export messages to a text file
        /// </summary>
        public void ExportToText(string textPath)
        {
            try
            {
                EncryptText.ExportToTextFile(currentFilePath, textPath);
                SetStatusMessage($"Exported to {Path.GetFileName(textPath)}");
            }
            catch (Exception ex)
            {
                SetStatusMessage($"Error exporting: {ex.Message}");
            }
        }

        /// <summary>
        /// Import messages from a text file
        /// </summary>
        public void ImportFromText(string textPath)
        {
            try
            {
                if (string.IsNullOrEmpty(currentFilePath))
                {
                    SetStatusMessage("Load a binary file first to get the encryption key");
                    return;
                }

                EncryptText.ImportFromTextFile(textPath, currentFilePath, false);
                messages = EncryptText.ReadMessageArchive(currentFilePath, false);
                isDirty = false;
                SetStatusMessage($"Imported {messages.Count} messages from {Path.GetFileName(textPath)}");
            }
            catch (Exception ex)
            {
                SetStatusMessage($"Error importing: {ex.Message}");
            }
        }

        /// <summary>
        /// Draw the ImGui window
        /// </summary>
        public void Draw()
        {
            // Update status message timer
            if (statusMessageTimer > 0f)
            {
                statusMessageTimer -= ImGui.GetIO().DeltaTime;
            }

            if (ImGui.Begin("Text Editor"))
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
        }

        private void DrawToolbar()
        {
            if (ImGui.Button("Load"))
            {
                // TODO: Open file dialog
                SetStatusMessage("File dialog not implemented yet - use LoadFile() method");
            }

            ImGui.SameLine();
            if (ImGui.Button("Save") && !string.IsNullOrEmpty(currentFilePath))
            {
                SaveFile();
            }

            ImGui.SameLine();
            if (ImGui.Button("Export to TXT"))
            {
                // TODO: Save file dialog
                if (!string.IsNullOrEmpty(currentFilePath))
                {
                    string txtPath = Path.ChangeExtension(currentFilePath, ".txt");
                    ExportToText(txtPath);
                }
            }

            ImGui.SameLine();
            if (ImGui.Button("Import from TXT"))
            {
                // TODO: Open file dialog
                SetStatusMessage("File dialog not implemented yet");
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
            ImGui.BeginChild("MessageList", new Vector2(listWidth, availableRegion.Y - 30), ImGuiChildFlags.Border);
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
            ImGui.BeginChild("MessageEditor", new Vector2(editorWidth, availableRegion.Y - 30), ImGuiChildFlags.Border);
            {
                if (selectedMessageIndex >= 0 && selectedMessageIndex < messages.Count)
                {
                    ImGui.Text($"Editing Message {selectedMessageIndex:D4}");
                    ImGui.Separator();

                    // Multi-line text editor
                    if (ImGui.InputTextMultiline("##editor", ref editBuffer, 10000,
                        new Vector2(editorWidth - 20, availableRegion.Y - 100)))
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
