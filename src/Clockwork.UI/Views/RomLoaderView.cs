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
    private NdsToolService? _ndsToolService;
    private string _romFolderPath = string.Empty;
    private string _ndsFilePath = string.Empty;
    private string _extractOutputPath = string.Empty;
    private string _statusMessage = string.Empty;
    private System.Numerics.Vector4 _statusColor = new(1.0f, 1.0f, 1.0f, 1.0f);
    private string _extractionLog = string.Empty;
    private bool _isExtracting = false;

    public bool IsVisible { get; set; } = false;

    public RomLoaderView(ApplicationContext appContext)
    {
        _appContext = appContext;
        _romService = _appContext.GetService<RomService>();
        _ndsToolService = _appContext.GetService<NdsToolService>();
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

            // Onglets pour Extraire / Charger
            if (ImGui.BeginTabBar("LoadTabs"))
            {
                // Onglet: Extraire ROM
                if (ImGui.BeginTabItem("Extraire ROM (.nds)"))
                {
                    ImGui.Spacing();

                    // Vérifier si ndstool est disponible
                    if (_ndsToolService?.IsAvailable != true)
                    {
                        ImGui.TextColored(new System.Numerics.Vector4(1.0f, 0.4f, 0.4f, 1.0f),
                            "ndstool.exe n'est pas disponible!");
                        ImGui.TextWrapped("Téléchargez ndstool.exe depuis DSPRE et placez-le dans le dossier Tools/");
                        ImGui.Text("URL: https://github.com/DS-Pokemon-Rom-Editor/DSPRE/raw/master/DS_Map/Tools/ndstool.exe");
                    }
                    else
                    {
                        ImGui.TextColored(new System.Numerics.Vector4(0.5f, 0.8f, 0.5f, 1.0f),
                            $"ndstool.exe disponible: {_ndsToolService.NdsToolPath}");
                    }

                    ImGui.Spacing();
                    ImGui.Separator();
                    ImGui.Spacing();

                    // Input: Fichier .nds
                    ImGui.Text("Fichier ROM (.nds):");
                    ImGui.InputText("##ndsfile", ref _ndsFilePath, 500);
                    ImGui.SameLine();
                    if (ImGui.Button("...##browsends"))
                    {
                        _statusMessage = "Dialogue de sélection non implémenté - Entrez le chemin manuellement";
                        _statusColor = new System.Numerics.Vector4(1.0f, 0.7f, 0.4f, 1.0f);
                    }

                    // Input: Dossier de sortie
                    ImGui.Text("Dossier de sortie:");
                    ImGui.InputText("##extractoutput", ref _extractOutputPath, 500);
                    ImGui.SameLine();
                    if (ImGui.Button("...##browseoutput"))
                    {
                        _statusMessage = "Dialogue de sélection non implémenté - Entrez le chemin manuellement";
                        _statusColor = new System.Numerics.Vector4(1.0f, 0.7f, 0.4f, 1.0f);
                    }

                    ImGui.Spacing();

                    // Bouton Extraire
                    bool canExtract = _ndsToolService?.IsAvailable == true &&
                                     !string.IsNullOrWhiteSpace(_ndsFilePath) &&
                                     !string.IsNullOrWhiteSpace(_extractOutputPath) &&
                                     !_isExtracting;

                    if (!canExtract)
                    {
                        ImGui.BeginDisabled();
                    }

                    if (ImGui.Button("Extraire la ROM", new System.Numerics.Vector2(-1, 40)))
                    {
                        _isExtracting = true;
                        _extractionLog = "";

                        bool success = _ndsToolService!.ExtractRom(_ndsFilePath, _extractOutputPath, (msg) =>
                        {
                            _extractionLog += msg + "\n";
                        });

                        _isExtracting = false;

                        if (success)
                        {
                            _statusMessage = "Extraction réussie! Vous pouvez maintenant charger la ROM.";
                            _statusColor = new System.Numerics.Vector4(0.5f, 0.8f, 0.5f, 1.0f);
                            _romFolderPath = _extractOutputPath;
                        }
                        else
                        {
                            _statusMessage = "Erreur lors de l'extraction. Consultez les logs.";
                            _statusColor = new System.Numerics.Vector4(1.0f, 0.4f, 0.4f, 1.0f);
                        }
                    }

                    if (!canExtract)
                    {
                        ImGui.EndDisabled();
                    }

                    // Afficher les logs d'extraction
                    if (!string.IsNullOrEmpty(_extractionLog))
                    {
                        ImGui.Spacing();
                        ImGui.Separator();
                        ImGui.Text("Logs d'extraction:");
                        ImGui.BeginChild("ExtractionLogs", new System.Numerics.Vector2(0, 150), ImGuiChildFlags.Border);
                        ImGui.TextWrapped(_extractionLog);
                        ImGui.EndChild();
                    }

                    ImGui.EndTabItem();
                }

                // Onglet: Charger ROM extraite
                if (ImGui.BeginTabItem("Charger ROM extraite"))
                {
                    ImGui.Spacing();
                    ImGui.Separator();
                    ImGui.Spacing();

                    // Input pour le chemin du dossier
                    ImGui.Text("Chemin du dossier ROM extrait:");
                    ImGui.InputText("##rompath", ref _romFolderPath, 500);

                    ImGui.SameLine();
                    if (ImGui.Button("Parcourir..."))
                    {
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

                    ImGui.Spacing();
                    ImGui.Separator();
                    ImGui.Spacing();

                    ImGui.TextWrapped("Le dossier doit contenir les fichiers extraits de la ROM (header.bin, arm9.bin, etc.)");

                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
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

        ImGui.End();
        IsVisible = isVisible;
    }
}
