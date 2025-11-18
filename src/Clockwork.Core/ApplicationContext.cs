namespace Clockwork.Core;

/// <summary>
/// Contexte de l'application qui gère les services et l'état global.
/// Point d'entrée principal pour la logique métier.
/// </summary>
public class ApplicationContext
{
    private readonly List<IApplicationService> _services = new();
    private bool _isInitialized;

    /// <summary>
    /// Indique si l'application est en cours d'exécution.
    /// </summary>
    public bool IsRunning { get; set; } = true;

    /// <summary>
    /// Ajoute un service à l'application.
    /// </summary>
    public void AddService(IApplicationService service)
    {
        if (_isInitialized)
        {
            throw new InvalidOperationException("Cannot add services after initialization.");
        }
        _services.Add(service);
    }

    /// <summary>
    /// Initialise tous les services enregistrés.
    /// </summary>
    public void Initialize()
    {
        if (_isInitialized)
        {
            throw new InvalidOperationException("Application already initialized.");
        }

        foreach (var service in _services)
        {
            service.Initialize();
        }

        _isInitialized = true;
    }

    /// <summary>
    /// Met à jour tous les services.
    /// </summary>
    public void Update(double deltaTime)
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("Application not initialized.");
        }

        foreach (var service in _services)
        {
            service.Update(deltaTime);
        }
    }

    /// <summary>
    /// Libère les ressources de tous les services.
    /// </summary>
    public void Shutdown()
    {
        foreach (var service in _services)
        {
            service.Dispose();
        }

        _services.Clear();
        _isInitialized = false;
    }
}
