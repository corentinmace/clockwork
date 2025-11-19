using ImGuiNET;
using Clockwork.Core;
using Clockwork.Core.Themes;
using Clockwork.Core.Logging;
using System.Numerics;

namespace Clockwork.UI.Views;

/// <summary>
/// Theme editor view for customizing and creating themes.
/// </summary>
public class ThemeEditorView : IView
{
    public bool IsVisible { get; set; } = false;

    private Theme? _editingTheme;
    private string _newThemeName = "";
    private string _selectedThemeName = "";
    private bool _hasUnsavedChanges = false;
    private string _statusMessage = "";
    private float _statusMessageTimer = 0f;

    public void Initialize(ApplicationContext appContext)
    {
        // Select the current theme by default
        if (ThemeManager.CurrentTheme != null)
        {
            _selectedThemeName = ThemeManager.CurrentTheme.Name;
        }
    }

    public void Draw()
    {
        if (!IsVisible) return;

        ImGui.SetNextWindowSize(new Vector2(900, 700), ImGuiCond.FirstUseEver);

        if (ImGui.Begin("Éditeur de thèmes", ref IsVisible))
        {
            DrawThemeSelector();

            ImGui.Separator();

            if (_editingTheme != null)
            {
                DrawThemeEditor();
            }
            else
            {
                ImGui.TextWrapped("Sélectionnez un thème à modifier ou créez-en un nouveau.");
            }

            // Status message
            if (_statusMessageTimer > 0f)
            {
                ImGui.Separator();
                ImGui.TextColored(new Vector4(0.4f, 0.8f, 0.4f, 1.0f), _statusMessage);
                _statusMessageTimer -= ImGui.GetIO().DeltaTime;
            }
        }

        ImGui.End();
    }

    private void DrawThemeSelector()
    {
        ImGui.Text("Thème:");
        ImGui.SameLine();

        ImGui.SetNextItemWidth(250);
        if (ImGui.BeginCombo("##ThemeSelector", _selectedThemeName))
        {
            foreach (var theme in ThemeManager.AvailableThemes.Values)
            {
                bool isSelected = _selectedThemeName == theme.Name;

                if (ImGui.Selectable($"{theme.Name}{(theme.IsReadOnly ? " (prédéfini)" : "")}", isSelected))
                {
                    _selectedThemeName = theme.Name;
                    _editingTheme = null;
                    _hasUnsavedChanges = false;
                }

                if (isSelected)
                {
                    ImGui.SetItemDefaultFocus();
                }
            }

            ImGui.EndCombo();
        }

        ImGui.SameLine();

        if (ImGui.Button("Modifier"))
        {
            var selectedTheme = ThemeManager.GetTheme(_selectedThemeName);
            if (selectedTheme != null)
            {
                // Clone the theme so we can edit it
                _editingTheme = selectedTheme.Clone();
                _editingTheme.Name = selectedTheme.Name;
                _editingTheme.IsReadOnly = selectedTheme.IsReadOnly;
                _hasUnsavedChanges = false;
                AppLogger.Debug($"Started editing theme: {_editingTheme.Name}");
            }
        }

        ImGui.SameLine();

        if (ImGui.Button("Nouveau"))
        {
            ImGui.OpenPopup("NewThemePopup");
        }

        ImGui.SameLine();

        if (ImGui.Button("Appliquer"))
        {
            var selectedTheme = ThemeManager.GetTheme(_selectedThemeName);
            if (selectedTheme != null)
            {
                ThemeManager.ApplyTheme(selectedTheme);
                ShowStatusMessage($"Thème '{selectedTheme.Name}' appliqué");
            }
        }

        ImGui.SameLine();

        var currentTheme = ThemeManager.GetTheme(_selectedThemeName);
        bool canDelete = currentTheme != null && !currentTheme.IsReadOnly;

        if (!canDelete)
        {
            ImGui.BeginDisabled();
        }

        if (ImGui.Button("Supprimer"))
        {
            ImGui.OpenPopup("DeleteThemePopup");
        }

        if (!canDelete)
        {
            ImGui.EndDisabled();

            if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
            {
                ImGui.SetTooltip("Les thèmes prédéfinis ne peuvent pas être supprimés");
            }
        }

        // New theme popup
        DrawNewThemePopup();

        // Delete theme popup
        DrawDeleteThemePopup();
    }

    private void DrawNewThemePopup()
    {
        if (ImGui.BeginPopupModal("NewThemePopup", ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.Text("Créer un nouveau thème");
            ImGui.Separator();

            ImGui.Text("Nom du thème:");
            ImGui.InputText("##NewThemeName", ref _newThemeName, 100);

            ImGui.Text("Basé sur:");
            ImGui.SetNextItemWidth(200);
            if (ImGui.BeginCombo("##BaseTheme", _selectedThemeName))
            {
                foreach (var theme in ThemeManager.AvailableThemes.Values)
                {
                    if (ImGui.Selectable(theme.Name))
                    {
                        _selectedThemeName = theme.Name;
                    }
                }

                ImGui.EndCombo();
            }

            ImGui.Separator();

            if (ImGui.Button("Créer"))
            {
                if (!string.IsNullOrWhiteSpace(_newThemeName))
                {
                    var baseTheme = ThemeManager.GetTheme(_selectedThemeName);
                    if (baseTheme != null)
                    {
                        _editingTheme = baseTheme.Clone();
                        _editingTheme.Name = _newThemeName;
                        _editingTheme.Author = "Custom";
                        _editingTheme.IsReadOnly = false;
                        _hasUnsavedChanges = true;

                        AppLogger.Info($"Created new theme '{_newThemeName}' based on '{baseTheme.Name}'");
                        ShowStatusMessage($"Nouveau thème '{_newThemeName}' créé");

                        _newThemeName = "";
                        ImGui.CloseCurrentPopup();
                    }
                }
            }

            ImGui.SameLine();

            if (ImGui.Button("Annuler"))
            {
                _newThemeName = "";
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }
    }

    private void DrawDeleteThemePopup()
    {
        if (ImGui.BeginPopupModal("DeleteThemePopup", ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.Text($"Êtes-vous sûr de vouloir supprimer le thème '{_selectedThemeName}' ?");
            ImGui.Separator();

            if (ImGui.Button("Supprimer"))
            {
                if (ThemeManager.DeleteTheme(_selectedThemeName))
                {
                    ShowStatusMessage($"Thème '{_selectedThemeName}' supprimé");
                    _selectedThemeName = "Dark";
                    _editingTheme = null;
                }

                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();

            if (ImGui.Button("Annuler"))
            {
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }
    }

    private void DrawThemeEditor()
    {
        if (_editingTheme == null) return;

        // Warn if editing read-only theme
        if (_editingTheme.IsReadOnly)
        {
            ImGui.TextColored(new Vector4(1.0f, 0.7f, 0.0f, 1.0f), "⚠ Les thèmes prédéfinis ne peuvent pas être sauvegardés. Créez une copie pour les modifier.");
            ImGui.Separator();
        }

        // Save/Reset buttons
        if (_editingTheme.IsReadOnly)
        {
            ImGui.BeginDisabled();
        }

        if (ImGui.Button("Sauvegarder"))
        {
            ThemeManager.SaveTheme(_editingTheme);
            _hasUnsavedChanges = false;
            _selectedThemeName = _editingTheme.Name;
            ShowStatusMessage($"Thème '{_editingTheme.Name}' sauvegardé");
        }

        if (_editingTheme.IsReadOnly)
        {
            ImGui.EndDisabled();
        }

        ImGui.SameLine();

        if (ImGui.Button("Réinitialiser"))
        {
            var originalTheme = ThemeManager.GetTheme(_editingTheme.Name);
            if (originalTheme != null)
            {
                _editingTheme = originalTheme.Clone();
                _editingTheme.Name = originalTheme.Name;
                _editingTheme.IsReadOnly = originalTheme.IsReadOnly;
                _hasUnsavedChanges = false;
                ShowStatusMessage("Modifications annulées");
            }
        }

        ImGui.SameLine();

        if (ImGui.Button("Prévisualiser"))
        {
            ThemeManager.ApplyTheme(_editingTheme);
            ShowStatusMessage("Aperçu appliqué (pas encore sauvegardé)");
        }

        if (_hasUnsavedChanges)
        {
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(1.0f, 0.7f, 0.0f, 1.0f), "(modifications non sauvegardées)");
        }

        ImGui.Separator();

        // Tabs for different color categories
        if (ImGui.BeginTabBar("ThemeEditorTabs"))
        {
            if (ImGui.BeginTabItem("Style"))
            {
                DrawStyleTab();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Fenêtres"))
            {
                DrawWindowColorsTab();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Widgets"))
            {
                DrawWidgetColorsTab();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Texte"))
            {
                DrawTextColorsTab();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Avancé"))
            {
                DrawAdvancedColorsTab();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }

    private void DrawStyleTab()
    {
        if (_editingTheme == null) return;

        ImGui.Text("Propriétés de style:");

        if (ImGui.SliderFloat("Arrondi fenêtres", ref _editingTheme.WindowRounding, 0f, 12f))
            _hasUnsavedChanges = true;

        if (ImGui.SliderFloat("Arrondi frames", ref _editingTheme.FrameRounding, 0f, 12f))
            _hasUnsavedChanges = true;

        if (ImGui.SliderFloat("Arrondi sliders", ref _editingTheme.GrabRounding, 0f, 12f))
            _hasUnsavedChanges = true;

        if (ImGui.SliderFloat("Arrondi onglets", ref _editingTheme.TabRounding, 0f, 12f))
            _hasUnsavedChanges = true;

        var windowPadding = _editingTheme.WindowPadding;
        if (ImGui.SliderFloat2("Padding fenêtres", ref windowPadding, 0f, 20f))
        {
            _editingTheme.WindowPadding = windowPadding;
            _hasUnsavedChanges = true;
        }

        var framePadding = _editingTheme.FramePadding;
        if (ImGui.SliderFloat2("Padding frames", ref framePadding, 0f, 20f))
        {
            _editingTheme.FramePadding = framePadding;
            _hasUnsavedChanges = true;
        }

        var itemSpacing = _editingTheme.ItemSpacing;
        if (ImGui.SliderFloat2("Espacement items", ref itemSpacing, 0f, 20f))
        {
            _editingTheme.ItemSpacing = itemSpacing;
            _hasUnsavedChanges = true;
        }
    }

    private void DrawWindowColorsTab()
    {
        if (_editingTheme == null) return;

        if (ColorEdit("Fond fenêtre", ref _editingTheme.WindowBg)) _hasUnsavedChanges = true;
        if (ColorEdit("Fond enfant", ref _editingTheme.ChildBg)) _hasUnsavedChanges = true;
        if (ColorEdit("Fond popup", ref _editingTheme.PopupBg)) _hasUnsavedChanges = true;
        if (ColorEdit("Bordure", ref _editingTheme.Border)) _hasUnsavedChanges = true;
        if (ColorEdit("Ombre bordure", ref _editingTheme.BorderShadow)) _hasUnsavedChanges = true;
        if (ColorEdit("Barre de titre", ref _editingTheme.TitleBg)) _hasUnsavedChanges = true;
        if (ColorEdit("Barre de titre active", ref _editingTheme.TitleBgActive)) _hasUnsavedChanges = true;
        if (ColorEdit("Barre de titre réduite", ref _editingTheme.TitleBgCollapsed)) _hasUnsavedChanges = true;
        if (ColorEdit("Barre de menu", ref _editingTheme.MenuBarBg)) _hasUnsavedChanges = true;
    }

    private void DrawWidgetColorsTab()
    {
        if (_editingTheme == null) return;

        ImGui.Text("Frames:");
        if (ColorEdit("Frame", ref _editingTheme.FrameBg)) _hasUnsavedChanges = true;
        if (ColorEdit("Frame survolé", ref _editingTheme.FrameBgHovered)) _hasUnsavedChanges = true;
        if (ColorEdit("Frame actif", ref _editingTheme.FrameBgActive)) _hasUnsavedChanges = true;

        ImGui.Separator();
        ImGui.Text("Boutons:");
        if (ColorEdit("Bouton", ref _editingTheme.Button)) _hasUnsavedChanges = true;
        if (ColorEdit("Bouton survolé", ref _editingTheme.ButtonHovered)) _hasUnsavedChanges = true;
        if (ColorEdit("Bouton actif", ref _editingTheme.ButtonActive)) _hasUnsavedChanges = true;

        ImGui.Separator();
        ImGui.Text("En-têtes:");
        if (ColorEdit("En-tête", ref _editingTheme.Header)) _hasUnsavedChanges = true;
        if (ColorEdit("En-tête survolé", ref _editingTheme.HeaderHovered)) _hasUnsavedChanges = true;
        if (ColorEdit("En-tête actif", ref _editingTheme.HeaderActive)) _hasUnsavedChanges = true;

        ImGui.Separator();
        ImGui.Text("Onglets:");
        if (ColorEdit("Onglet", ref _editingTheme.Tab)) _hasUnsavedChanges = true;
        if (ColorEdit("Onglet survolé", ref _editingTheme.TabHovered)) _hasUnsavedChanges = true;
        if (ColorEdit("Onglet actif", ref _editingTheme.TabActive)) _hasUnsavedChanges = true;
        if (ColorEdit("Onglet non focalisé", ref _editingTheme.TabUnfocused)) _hasUnsavedChanges = true;
        if (ColorEdit("Onglet non focalisé actif", ref _editingTheme.TabUnfocusedActive)) _hasUnsavedChanges = true;

        ImGui.Separator();
        ImGui.Text("Contrôles:");
        if (ColorEdit("Case cochée", ref _editingTheme.CheckMark)) _hasUnsavedChanges = true;
        if (ColorEdit("Slider", ref _editingTheme.SliderGrab)) _hasUnsavedChanges = true;
        if (ColorEdit("Slider actif", ref _editingTheme.SliderGrabActive)) _hasUnsavedChanges = true;
    }

    private void DrawTextColorsTab()
    {
        if (_editingTheme == null) return;

        if (ColorEdit("Texte", ref _editingTheme.Text)) _hasUnsavedChanges = true;
        if (ColorEdit("Texte désactivé", ref _editingTheme.TextDisabled)) _hasUnsavedChanges = true;
        if (ColorEdit("Texte sélectionné", ref _editingTheme.TextSelectedBg)) _hasUnsavedChanges = true;
    }

    private void DrawAdvancedColorsTab()
    {
        if (_editingTheme == null) return;

        ImGui.Text("Scrollbar:");
        if (ColorEdit("Fond scrollbar", ref _editingTheme.ScrollbarBg)) _hasUnsavedChanges = true;
        if (ColorEdit("Poignée scrollbar", ref _editingTheme.ScrollbarGrab)) _hasUnsavedChanges = true;
        if (ColorEdit("Poignée scrollbar survolée", ref _editingTheme.ScrollbarGrabHovered)) _hasUnsavedChanges = true;
        if (ColorEdit("Poignée scrollbar active", ref _editingTheme.ScrollbarGrabActive)) _hasUnsavedChanges = true;

        ImGui.Separator();
        ImGui.Text("Séparateurs:");
        if (ColorEdit("Séparateur", ref _editingTheme.Separator)) _hasUnsavedChanges = true;
        if (ColorEdit("Séparateur survolé", ref _editingTheme.SeparatorHovered)) _hasUnsavedChanges = true;
        if (ColorEdit("Séparateur actif", ref _editingTheme.SeparatorActive)) _hasUnsavedChanges = true;

        ImGui.Separator();
        ImGui.Text("Redimensionnement:");
        if (ColorEdit("Poignée redimensionnement", ref _editingTheme.ResizeGrip)) _hasUnsavedChanges = true;
        if (ColorEdit("Poignée redim. survolée", ref _editingTheme.ResizeGripHovered)) _hasUnsavedChanges = true;
        if (ColorEdit("Poignée redim. active", ref _editingTheme.ResizeGripActive)) _hasUnsavedChanges = true;

        ImGui.Separator();
        ImGui.Text("Docking:");
        if (ColorEdit("Aperçu docking", ref _editingTheme.DockingPreview)) _hasUnsavedChanges = true;
        if (ColorEdit("Fond docking vide", ref _editingTheme.DockingEmptyBg)) _hasUnsavedChanges = true;

        ImGui.Separator();
        ImGui.Text("Tables:");
        if (ColorEdit("En-tête table", ref _editingTheme.TableHeaderBg)) _hasUnsavedChanges = true;
        if (ColorEdit("Bordure table forte", ref _editingTheme.TableBorderStrong)) _hasUnsavedChanges = true;
        if (ColorEdit("Bordure table légère", ref _editingTheme.TableBorderLight)) _hasUnsavedChanges = true;
        if (ColorEdit("Ligne table", ref _editingTheme.TableRowBg)) _hasUnsavedChanges = true;
        if (ColorEdit("Ligne table alternée", ref _editingTheme.TableRowBgAlt)) _hasUnsavedChanges = true;
    }

    private bool ColorEdit(string label, ref Vector4 color)
    {
        return ImGui.ColorEdit4(label, ref color, ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.AlphaPreviewHalf);
    }

    private void ShowStatusMessage(string message)
    {
        _statusMessage = message;
        _statusMessageTimer = 3.0f;
    }
}
