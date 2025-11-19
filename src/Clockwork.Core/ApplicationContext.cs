using Clockwork.Core.Logging;

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
        AppLogger.Debug($"Service registered: {service.GetType().Name}");
    }

    /// <summary>
    /// Récupère un service par son type.
    /// </summary>
    public T? GetService<T>() where T : class, IApplicationService
    {
        return _services.OfType<T>().FirstOrDefault();
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

        AppLogger.Info($"Initializing application context with {_services.Count} services...");

        foreach (var service in _services)
        {
            AppLogger.Debug($"Initializing service: {service.GetType().Name}");
            service.Initialize();
        }

        _isInitialized = true;
        AppLogger.Info("Application context initialized successfully");
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
        AppLogger.Info("Shutting down application context...");

        foreach (var service in _services)
        {
            AppLogger.Debug($"Disposing service: {service.GetType().Name}");
            service.Dispose();
        }

        _services.Clear();
        _isInitialized = false;
        AppLogger.Info("Application context shutdown complete");
    }
}
