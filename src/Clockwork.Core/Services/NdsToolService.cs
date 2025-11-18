using System.Diagnostics;

namespace Clockwork.Core.Services;

/// <summary>
/// Service pour utiliser ndstool.exe pour extraire des ROMs NDS.
/// </summary>
public class NdsToolService : IApplicationService
{
    private string _ndsToolPath = string.Empty;

    public void Initialize()
    {
        // Chercher ndstool.exe dans le dossier Tools
        string projectRoot = AppDomain.CurrentDomain.BaseDirectory;

        // Remonter au dossier racine du projet (depuis bin/Debug/net8.0)
        string toolsPath = Path.Combine(projectRoot, "..", "..", "..", "..", "Tools", "ndstool.exe");
        toolsPath = Path.GetFullPath(toolsPath);

        if (File.Exists(toolsPath))
        {
            _ndsToolPath = toolsPath;
        }
        else
        {
            // Essayer un chemin alternatif (au cas où on est dans un autre contexte)
            toolsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Tools", "ndstool.exe");
            if (File.Exists(toolsPath))
            {
                _ndsToolPath = toolsPath;
            }
        }
    }

    public void Update(double deltaTime)
    {
        // Rien à faire
    }

    public void Shutdown()
    {
        // Rien à faire
    }

    public void Dispose()
    {
        // Rien à faire
    }

    /// <summary>
    /// Vérifie si ndstool.exe est disponible.
    /// </summary>
    public bool IsAvailable => !string.IsNullOrEmpty(_ndsToolPath) && File.Exists(_ndsToolPath);

    /// <summary>
    /// Extrait une ROM NDS dans un dossier.
    /// </summary>
    /// <param name="romPath">Chemin vers le fichier .nds</param>
    /// <param name="outputPath">Dossier de destination</param>
    /// <param name="progress">Callback pour les messages de progression</param>
    /// <returns>True si l'extraction a réussi</returns>
    public bool ExtractRom(string romPath, string outputPath, Action<string>? progress = null)
    {
        if (!IsAvailable)
        {
            progress?.Invoke("Erreur: ndstool.exe n'est pas disponible");
            return false;
        }

        if (!File.Exists(romPath))
        {
            progress?.Invoke($"Erreur: Le fichier ROM n'existe pas: {romPath}");
            return false;
        }

        try
        {
            // Créer le dossier de sortie s'il n'existe pas
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            // Créer le sous-dossier "data" pour les fichiers de données
            string dataPath = Path.Combine(outputPath, "data");
            if (!Directory.Exists(dataPath))
            {
                Directory.CreateDirectory(dataPath);
            }

            // Construire la commande ndstool
            // ndstool -x <rom.nds> -9 arm9.bin -7 arm7.bin -y9 y9.bin -y7 y7.bin -d data -y overlay -t banner.bin -h header.bin
            string arguments = $"-x \"{romPath}\" " +
                             $"-9 \"{Path.Combine(outputPath, "arm9.bin")}\" " +
                             $"-7 \"{Path.Combine(outputPath, "arm7.bin")}\" " +
                             $"-y9 \"{Path.Combine(outputPath, "y9.bin")}\" " +
                             $"-y7 \"{Path.Combine(outputPath, "y7.bin")}\" " +
                             $"-d \"{dataPath}\" " +
                             $"-y \"{Path.Combine(outputPath, "overlay")}\" " +
                             $"-t \"{Path.Combine(outputPath, "banner.bin")}\" " +
                             $"-h \"{Path.Combine(outputPath, "header.bin")}\"";

            progress?.Invoke("Démarrage de l'extraction...");

            var processStartInfo = new ProcessStartInfo
            {
                FileName = _ndsToolPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = Process.Start(processStartInfo);
            if (process == null)
            {
                progress?.Invoke("Erreur: Impossible de démarrer ndstool");
                return false;
            }

            // Lire la sortie
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            if (!string.IsNullOrWhiteSpace(output))
            {
                progress?.Invoke(output);
            }

            if (!string.IsNullOrWhiteSpace(error))
            {
                progress?.Invoke($"Erreur: {error}");
            }

            if (process.ExitCode != 0)
            {
                progress?.Invoke($"ndstool a terminé avec le code d'erreur: {process.ExitCode}");
                return false;
            }

            progress?.Invoke("Extraction terminée avec succès!");
            return true;
        }
        catch (Exception ex)
        {
            progress?.Invoke($"Exception: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Obtient le chemin vers ndstool.exe.
    /// </summary>
    public string NdsToolPath => _ndsToolPath;
}
