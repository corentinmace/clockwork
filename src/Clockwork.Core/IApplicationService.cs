namespace Clockwork.Core;

/// <summary>
/// Interface de base pour les services de l'application.
/// Permet une architecture modulaire et testable.
/// </summary>
public interface IApplicationService
{
    /// <summary>
    /// Initialise le service.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Met à jour le service. Appelé à chaque frame.
    /// </summary>
    /// <param name="deltaTime">Temps écoulé depuis la dernière mise à jour en secondes.</param>
    void Update(double deltaTime);

    /// <summary>
    /// Libère les ressources du service.
    /// </summary>
    void Dispose();
}
