using System.Diagnostics;
using Clockwork.Core.Logging;

namespace Clockwork.Core.Services;

/// <summary>
/// Service pour utiliser ndstool.exe pour extraire des ROMs NDS.
/// </summary>
public class NdsToolService : IApplicationService
{
    private string _ndsToolPath = string.Empty;

    public void Initialize()
    {
        AppLogger.Info("NdsToolService initializing...");

        // Chercher ndstool.exe dans le dossier Tools
        string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;

        // Chemin principal: Tools/ dans le répertoire de l'exécutable (déployé)
        string toolsPath = Path.Combine(baseDirectory, "Tools", "ndstool.exe");

        if (File.Exists(toolsPath))
        {
            _ndsToolPath = toolsPath;
            AppLogger.Info($"ndstool.exe found at: {_ndsToolPath}");
        }
        else
        {
            // Chemin alternatif: remonter au dossier racine du projet (développement)
            // Depuis bin/Debug/net8.0 -> ../../../../Tools/ndstool.exe
            toolsPath = Path.Combine(baseDirectory, "..", "..", "..", "..", "Tools", "ndstool.exe");
            toolsPath = Path.GetFullPath(toolsPath);

            if (File.Exists(toolsPath))
            {
                _ndsToolPath = toolsPath;
                AppLogger.Info($"ndstool.exe found at: {_ndsToolPath}");
            }
            else
            {
                AppLogger.Warn("ndstool.exe not found - ROM extraction will not be available");
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
        AppLogger.Info($"Starting ROM extraction: {romPath}");

        if (!IsAvailable)
        {
            AppLogger.Error("ndstool.exe is not available");
            progress?.Invoke("Erreur: ndstool.exe n'est pas disponible");
            return false;
        }

        if (!File.Exists(romPath))
        {
            AppLogger.Error($"ROM file does not exist: {romPath}");
            progress?.Invoke($"Erreur: Le fichier ROM n'existe pas: {romPath}");
            return false;
        }

        try
        {
            AppLogger.Debug($"Output directory: {outputPath}");

            // Créer le dossier de sortie s'il n'existe pas
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
                AppLogger.Debug($"Created output directory: {outputPath}");
            }

            // Créer le sous-dossier "data" pour les fichiers de données
            string dataPath = Path.Combine(outputPath, "data");
            if (!Directory.Exists(dataPath))
            {
                Directory.CreateDirectory(dataPath);
                AppLogger.Debug($"Created data directory: {dataPath}");
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

            AppLogger.Debug($"ndstool command: {_ndsToolPath} {arguments}");
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
                AppLogger.Error("Failed to start ndstool process");
                progress?.Invoke("Erreur: Impossible de démarrer ndstool");
                return false;
            }

            AppLogger.Debug("ndstool process started, waiting for completion...");

            // Lire la sortie
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            if (!string.IsNullOrWhiteSpace(output))
            {
                AppLogger.Debug($"ndstool output: {output}");
                progress?.Invoke(output);
            }

            if (!string.IsNullOrWhiteSpace(error))
            {
                AppLogger.Warn($"ndstool error output: {error}");
                progress?.Invoke($"Erreur: {error}");
            }

            if (process.ExitCode != 0)
            {
                AppLogger.Error($"ndstool exited with code: {process.ExitCode}");
                progress?.Invoke($"ndstool a terminé avec le code d'erreur: {process.ExitCode}");
                return false;
            }

            AppLogger.Info("ROM extraction completed successfully");
            progress?.Invoke("Extraction terminée avec succès!");
            return true;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Exception during ROM extraction: {ex.Message}");
            AppLogger.Debug($"Stack trace: {ex.StackTrace}");
            progress?.Invoke($"Exception: {ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// Obtient le chemin vers ndstool.exe.
    /// </summary>
    public string NdsToolPath => _ndsToolPath;

    /// <summary>
    /// Repack une ROM NDS à partir d'un dossier extrait.
    /// </summary>
    /// <param name="extractedPath">Dossier contenant les fichiers extraits</param>
    /// <param name="outputRomPath">Chemin vers le fichier .nds à créer</param>
    /// <param name="progress">Callback pour les messages de progression</param>
    /// <returns>True si le repacking a réussi</returns>
    public bool PackRom(string extractedPath, string outputRomPath, Action<string>? progress = null)
    {
        AppLogger.Info($"Starting ROM packing: {outputRomPath}");

        if (!IsAvailable)
        {
            AppLogger.Error("ndstool.exe is not available");
            progress?.Invoke("Erreur: ndstool.exe n'est pas disponible");
            return false;
        }

        if (!Directory.Exists(extractedPath))
        {
            AppLogger.Error($"Source directory does not exist: {extractedPath}");
            progress?.Invoke($"Erreur: Le dossier source n'existe pas: {extractedPath}");
            return false;
        }

        // Vérifier que les fichiers requis existent
        string[] requiredFiles = { "header.bin", "arm9.bin", "arm7.bin" };
        foreach (string file in requiredFiles)
        {
            string filePath = Path.Combine(extractedPath, file);
            if (!File.Exists(filePath))
            {
                AppLogger.Error($"Required file missing: {file}");
                progress?.Invoke($"Erreur: Fichier requis manquant: {file}");
                return false;
            }
        }

        AppLogger.Debug($"All required files present, proceeding with packing");

        try
        {
            string dataPath = Path.Combine(extractedPath, "data");

            // Construire la commande ndstool pour créer la ROM
            // ndstool -c <rom.nds> -9 arm9.bin -7 arm7.bin -y9 y9.bin -y7 y7.bin -d data -y overlay -t banner.bin -h header.bin
            string arguments = $"-c \"{outputRomPath}\" " +
                             $"-9 \"{Path.Combine(extractedPath, "arm9.bin")}\" " +
                             $"-7 \"{Path.Combine(extractedPath, "arm7.bin")}\" " +
                             $"-y9 \"{Path.Combine(extractedPath, "y9.bin")}\" " +
                             $"-y7 \"{Path.Combine(extractedPath, "y7.bin")}\" " +
                             $"-d \"{dataPath}\" " +
                             $"-y \"{Path.Combine(extractedPath, "overlay")}\" " +
                             $"-t \"{Path.Combine(extractedPath, "banner.bin")}\" " +
                             $"-h \"{Path.Combine(extractedPath, "header.bin")}\"";

            AppLogger.Debug($"ndstool command: {_ndsToolPath} {arguments}");
            progress?.Invoke("Démarrage du repacking de la ROM...");

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
                AppLogger.Error("Failed to start ndstool process");
                progress?.Invoke("Erreur: Impossible de démarrer ndstool");
                return false;
            }

            AppLogger.Debug("ndstool process started, waiting for completion...");

            // Lire la sortie
            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            process.WaitForExit();

            if (!string.IsNullOrWhiteSpace(output))
            {
                AppLogger.Debug($"ndstool output: {output}");
                progress?.Invoke(output);
            }

            if (!string.IsNullOrWhiteSpace(error))
            {
                AppLogger.Warn($"ndstool error output: {error}");
                progress?.Invoke($"Erreur: {error}");
            }

            if (process.ExitCode != 0)
            {
                AppLogger.Error($"ndstool exited with code: {process.ExitCode}");
                progress?.Invoke($"ndstool a terminé avec le code d'erreur: {process.ExitCode}");
                return false;
            }

            AppLogger.Info($"ROM packing completed successfully: {outputRomPath}");
            progress?.Invoke($"ROM repacked avec succès: {outputRomPath}");
            return true;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"Exception during ROM packing: {ex.Message}");
            AppLogger.Debug($"Stack trace: {ex.StackTrace}");
            progress?.Invoke($"Exception: {ex.Message}");
            return false;
        }
    }
}
