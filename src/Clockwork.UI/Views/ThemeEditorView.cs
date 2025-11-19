using ImGuiNET;
using Clockwork.Core;
using Clockwork.Core.Themes;
using Clockwork.Core.Logging;
using Clockwork.UI.Themes;
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
        bool isVisible = IsVisible;
        if (ImGui.Begin("Éditeur de thèmes", ref isVisible))
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
        bool popupOpen = true;
        if (ImGui.BeginPopupModal("NewThemePopup", ref popupOpen, ImGuiWindowFlags.AlwaysAutoResize))
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
        bool popupOpen = true;
        if (ImGui.BeginPopupModal("DeleteThemePopup", ref popupOpen, ImGuiWindowFlags.AlwaysAutoResize))
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
        float windowRounding = _editingTheme.WindowRounding;
        if (ImGui.SliderFloat("Arrondi fenêtres", ref windowRounding, 0f, 12f))
            _hasUnsavedChanges = true;

        float frameRounding = _editingTheme.FrameRounding;
        if (ImGui.SliderFloat("Arrondi frames", ref frameRounding, 0f, 12f))
            _hasUnsavedChanges = true;

        float grabRounding = _editingTheme.GrabRounding;
        if (ImGui.SliderFloat("Arrondi sliders", ref grabRounding, 0f, 12f))
            _hasUnsavedChanges = true;

        float tabRounding = _editingTheme.TabRounding;
        if (ImGui.SliderFloat("Arrondi onglets", ref tabRounding, 0f, 12f))
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

        if (ColorEditProperty("Fond fenêtre", () => _editingTheme.WindowBg, v => _editingTheme.WindowBg = v)) _hasUnsavedChanges = true;
        if (ColorEditProperty("Fond enfant", () => _editingTheme.ChildBg, v => _editingTheme.ChildBg = v)) _hasUnsavedChanges = true;
        if (ColorEditProperty("Fond popup", () => _editingTheme.PopupBg, v => _editingTheme.PopupBg = v)) _hasUnsavedChanges = true;
        if (ColorEditProperty("Bordure", () => _editingTheme.Border, v => _editingTheme.Border = v)) _hasUnsavedChanges = true;
        if (ColorEditProperty("Ombre bordure", () => _editingTheme.BorderShadow, v => _editingTheme.BorderShadow = v)) _hasUnsavedChanges = true;
        if (ColorEditProperty("Barre de titre", () => _editingTheme.TitleBg, v => _editingTheme.TitleBg = v)) _hasUnsavedChanges = true;
        if (ColorEditProperty("Barre de titre active", () => _editingTheme.TitleBgActive, v => _editingTheme.TitleBgActive = v)) _hasUnsavedChanges = true;
        if (ColorEditProperty("Barre de titre réduite", () => _editingTheme.TitleBgCollapsed, v => _editingTheme.TitleBgCollapsed = v)) _hasUnsavedChanges = true;
        if (ColorEditProperty("Barre de menu", () => _editingTheme.MenuBarBg, v => _editingTheme.MenuBarBg = v)) _hasUnsavedChanges = true;
    }

    private void DrawWidgetColorsTab()
    {
        if (_editingTheme == null) return;

        ImGui.Text("Frames:");
        if (ColorEditProperty("Frame", () => _editingTheme.FrameBg, v => _editingTheme.FrameBg = v)) _hasUnsavedChanges = true;
        if (ColorEditProperty("Frame survolé", () => _editingTheme.FrameBgHovered, v => _editingTheme.FrameBgHovered = v)) _hasUnsavedChanges = true;
        if (ColorEditProperty("Frame actif", () => _editingTheme.FrameBgActive, v => _editingTheme.FrameBgActive = v)) _hasUnsavedChanges = true;

        ImGui.Separator();
        ImGui.Text("Boutons:");
        if (ColorEditProperty("Bouton", () => _editingTheme.Button, v => _editingTheme.Button = v)) _hasUnsavedChanges = true;
        if (ColorEditProperty("Bouton survolé", () => _editingTheme.ButtonHovered, v => _editingTheme.ButtonHovered = v)) _hasUnsavedChanges = true;
        if (ColorEditProperty("Bouton actif", () => _editingTheme.ButtonActive, v => _editingTheme.ButtonActive = v)) _hasUnsavedChanges = true;

        ImGui.Separator();
        ImGui.Text("En-têtes:");
        if (ColorEditProperty("En-tête", () => _editingTheme.Header, v => _editingTheme.Header = v)) _hasUnsavedChanges = true;
        if (ColorEditProperty("En-tête survolé", () => _editingTheme.HeaderHovered, v => _editingTheme.HeaderHovered = v)) _hasUnsavedChanges = true;
        if (ColorEditProperty("En-tête actif", () => _editingTheme.HeaderActive, v => _editingTheme.HeaderActive = v)) _hasUnsavedChanges = true;

        ImGui.Separator();
        ImGui.Text("Onglets:");
        if (ColorEditProperty("Onglet", () => _editingTheme.Tab, v => _editingTheme.Tab = v)) _hasUnsavedChanges = true;
        if (ColorEditProperty("Onglet survolé", () => _editingTheme.TabHovered, v => _editingTheme.TabHovered = v)) _hasUnsavedChanges = true;
        if (ColorEditProperty("Onglet actif", () => _editingTheme.TabActive, v => _editingTheme.TabActive = v)) _hasUnsavedChanges = true;
        if (ColorEditProperty("Onglet non focalisé", () => _editingTheme.TabUnfocused, v => _editingTheme.TabUnfocused = v)) _hasUnsavedChanges = true;
        if (ColorEditProperty("Onglet non focalisé actif", () => _editingTheme.TabUnfocusedActive, v => _editingTheme.TabUnfocusedActive = v)) _hasUnsavedChanges = true;

        ImGui.Separator();
        ImGui.Text("Contrôles:");
        if (ColorEditProperty("Case cochée", () => _editingTheme.CheckMark, v => _editingTheme.CheckMark = v)) _hasUnsavedChanges = true;
        if (ColorEditProperty("Slider", () => _editingTheme.SliderGrab, v => _editingTheme.SliderGrab = v)) _hasUnsavedChanges = true;
        if (ColorEditProperty("Slider actif", () => _editingTheme.SliderGrabActive, v => _editingTheme.SliderGrabActive = v)) _hasUnsavedChanges = true;
    }

    private void DrawTextColorsTab()
    {
        if (_editingTheme == null) return;

        if (ColorEditProperty("Texte", () => _editingTheme.Text, v => _editingTheme.Text = v)) _hasUnsavedChanges = true;
        if (ColorEditProperty("Texte désactivé", () => _editingTheme.TextDisabled, v => _editingTheme.TextDisabled = v)) _hasUnsavedChanges = true;
        if (ColorEditProperty("Texte sélectionné", () => _editingTheme.TextSelectedBg, v => _editingTheme.TextSelectedBg = v)) _hasUnsavedChanges = true;
    }

    private void DrawAdvancedColorsTab()
    {
        if (_editingTheme == null) return;

        ImGui.Text("Scrollbar:");
        if (ColorEditProperty("Fond scrollbar", () => _editingTheme.ScrollbarBg, v => _editingTheme.ScrollbarBg = v)) _hasUnsavedChanges = true;
        if (ColorEditProperty("Poignée scrollbar", () => _editingTheme.ScrollbarGrab, v => _editingTheme.ScrollbarGrab = v)) _hasUnsavedChanges = true;
        if (ColorEditProperty("Poignée scrollbar survolée", () => _editingTheme.ScrollbarGrabHovered, v => _editingTheme.ScrollbarGrabHovered = v)) _hasUnsavedChanges = true;
        if (ColorEditProperty("Poignée scrollbar active", () => _editingTheme.ScrollbarGrabActive, v => _editingTheme.ScrollbarGrabActive = v)) _hasUnsavedChanges = true;

        ImGui.Separator();
        ImGui.Text("Séparateurs:");
        if (ColorEditProperty("Séparateur", () => _editingTheme.Separator, v => _editingTheme.Separator = v)) _hasUnsavedChanges = true;
        if (ColorEditProperty("Séparateur survolé", () => _editingTheme.SeparatorHovered, v => _editingTheme.SeparatorHovered = v)) _hasUnsavedChanges = true;
        if (ColorEditProperty("Séparateur actif", () => _editingTheme.SeparatorActive, v => _editingTheme.SeparatorActive = v)) _hasUnsavedChanges = true;

        ImGui.Separator();
        ImGui.Text("Redimensionnement:");
        if (ColorEditProperty("Poignée redimensionnement", () => _editingTheme.ResizeGrip, v => _editingTheme.ResizeGrip = v)) _hasUnsavedChanges = true;
        if (ColorEditProperty("Poignée redim. survolée", () => _editingTheme.ResizeGripHovered, v => _editingTheme.ResizeGripHovered = v)) _hasUnsavedChanges = true;
        if (ColorEditProperty("Poignée redim. active", () => _editingTheme.ResizeGripActive, v => _editingTheme.ResizeGripActive = v)) _hasUnsavedChanges = true;

        ImGui.Separator();
        ImGui.Text("Docking:");
        if (ColorEditProperty("Aperçu docking", () => _editingTheme.DockingPreview, v => _editingTheme.DockingPreview = v)) _hasUnsavedChanges = true;
        if (ColorEditProperty("Fond docking vide", () => _editingTheme.DockingEmptyBg, v => _editingTheme.DockingEmptyBg = v)) _hasUnsavedChanges = true;

        ImGui.Separator();
        ImGui.Text("Tables:");
        if (ColorEditProperty("En-tête table", () => _editingTheme.TableHeaderBg, v => _editingTheme.TableHeaderBg = v)) _hasUnsavedChanges = true;
        if (ColorEditProperty("Bordure table forte", () => _editingTheme.TableBorderStrong, v => _editingTheme.TableBorderStrong = v)) _hasUnsavedChanges = true;
        if (ColorEditProperty("Bordure table légère", () => _editingTheme.TableBorderLight, v => _editingTheme.TableBorderLight = v)) _hasUnsavedChanges = true;
        if (ColorEditProperty("Ligne table", () => _editingTheme.TableRowBg, v => _editingTheme.TableRowBg = v)) _hasUnsavedChanges = true;
        if (ColorEditProperty("Ligne table alternée", () => _editingTheme.TableRowBgAlt, v => _editingTheme.TableRowBgAlt = v)) _hasUnsavedChanges = true;
    }

    private bool ColorEditProperty(string label, Func<Vector4> getter, Action<Vector4> setter)
    {
        var color = getter();
        if (ImGui.ColorEdit4(label, ref color, ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.AlphaPreviewHalf))
        {
            setter(color);
            return true;
        }
        return false;
    }

    private void ShowStatusMessage(string message)
    {
        _statusMessage = message;
        _statusMessageTimer = 3.0f;
    }
}
