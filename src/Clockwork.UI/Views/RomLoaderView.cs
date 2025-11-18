using Clockwork.Core;
using Clockwork.Core.Services;
using ImGuiNET;

namespace Clockwork.UI.Views;

/// <summary>
/// Vue pour charger une ROM Pokémon.
/// </summary>
public class RomLoaderView : IView
{
    private readonly ApplicationContext _appContext;
    private RomService? _romService;
    private string _romFolderPath = string.Empty;
    private string _statusMessage = string.Empty;
    private System.Numerics.Vector4 _statusColor = new(1.0f, 1.0f, 1.0f, 1.0f);

    public bool IsVisible { get; set; } = false;

    public RomLoaderView(ApplicationContext appContext)
    {
        _appContext = appContext;
        _romService = _appContext.GetService<RomService>();
    }

    public void Draw()
    {
        if (!IsVisible) return;

        bool isVisible = IsVisible;
        ImGui.Begin("Charger une ROM", ref isVisible);

        ImGui.TextColored(new System.Numerics.Vector4(0.4f, 0.7f, 1.0f, 1.0f), "Chargement de ROM Pokémon");
        ImGui.Separator();
        ImGui.Spacing();

        // Afficher les informations de la ROM si chargée
        if (_romService?.CurrentRom?.IsLoaded == true)
        {
            var rom = _romService.CurrentRom;

            ImGui.TextColored(new System.Numerics.Vector4(0.5f, 0.8f, 0.5f, 1.0f), "ROM chargée:");
            ImGui.Spacing();

            ImGui.Text($"Jeu: {rom.GameName}");
            ImGui.Text($"Code: {rom.GameCode}");
            ImGui.Text($"Version: {rom.Version}");
            ImGui.Text($"Langue: {rom.Language}");
            ImGui.Text($"Famille: {rom.Family}");
            ImGui.Text($"Chemin: {rom.RomPath}");

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            if (ImGui.Button("Décharger la ROM", new System.Numerics.Vector2(-1, 30)))
            {
                _romService.UnloadRom();
                _statusMessage = "ROM déchargée";
                _statusColor = new System.Numerics.Vector4(0.5f, 0.8f, 0.5f, 1.0f);
            }
        }
        else
        {
            ImGui.Text("Aucune ROM chargée");
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            // Input pour le chemin du dossier
            ImGui.Text("Chemin du dossier ROM extrait:");
            ImGui.InputText("##rompath", ref _romFolderPath, 500);

            ImGui.SameLine();
            if (ImGui.Button("Parcourir..."))
            {
                // TODO: Implémenter un dialogue de sélection de dossier
                // Pour l'instant, l'utilisateur doit entrer le chemin manuellement
                _statusMessage = "Dialogue de sélection non implémenté - Entrez le chemin manuellement";
                _statusColor = new System.Numerics.Vector4(1.0f, 0.7f, 0.4f, 1.0f);
            }

            ImGui.Spacing();

            if (ImGui.Button("Charger la ROM", new System.Numerics.Vector2(-1, 40)))
            {
                if (string.IsNullOrWhiteSpace(_romFolderPath))
                {
                    _statusMessage = "Erreur: Veuillez spécifier un chemin";
                    _statusColor = new System.Numerics.Vector4(1.0f, 0.4f, 0.4f, 1.0f);
                }
                else
                {
                    bool success = _romService?.LoadRomFromFolder(_romFolderPath) ?? false;
                    if (success)
                    {
                        _statusMessage = $"ROM chargée avec succès: {_romService?.CurrentRom?.GameName}";
                        _statusColor = new System.Numerics.Vector4(0.5f, 0.8f, 0.5f, 1.0f);
                    }
                    else
                    {
                        _statusMessage = "Erreur: Impossible de charger la ROM. Vérifiez le chemin et que header.bin existe.";
                        _statusColor = new System.Numerics.Vector4(1.0f, 0.4f, 0.4f, 1.0f);
                    }
                }
            }
        }

        // Afficher le message de statut
        if (!string.IsNullOrEmpty(_statusMessage))
        {
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
            ImGui.TextColored(_statusColor, _statusMessage);
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Instructions
        ImGui.TextWrapped("Instructions: Extrayez d'abord votre ROM Pokémon avec un outil comme ndstool ou DSLazy, " +
                         "puis sélectionnez le dossier contenant les fichiers extraits (doit contenir header.bin).");

        ImGui.End();
        IsVisible = isVisible;
    }
}
