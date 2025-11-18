using Clockwork.Core.Models;

namespace Clockwork.Core.Services;

/// <summary>
/// Service gérant les statistiques du tableau de bord.
/// </summary>
public class DashboardService : IApplicationService
{
    private DashboardStats _stats = new();
    private Random _random = new();

    public DashboardStats GetStats() => _stats;

    public void Initialize()
    {
        // Initialiser avec des valeurs de démo
        _stats.TotalUsers = 1234;
        _stats.TotalProjects = 42;
        _stats.TotalTasks = 789;
        Console.WriteLine("[DashboardService] Initialized");
    }

    public void Update(double deltaTime)
    {
        // Simuler des changements de stats
        if (_random.NextDouble() < 0.001) // Rare mise à jour
        {
            _stats.TotalTasks += _random.Next(-5, 10);
        }
    }

    public void Dispose()
    {
        Console.WriteLine("[DashboardService] Disposing");
    }
}
