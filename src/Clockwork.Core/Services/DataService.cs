using Clockwork.Core.Models;

namespace Clockwork.Core.Services;

/// <summary>
/// Service gérant les données de l'application.
/// </summary>
public class DataService : IApplicationService
{
    private List<DataItem> _dataItems = new();

    public IReadOnlyList<DataItem> GetDataItems() => _dataItems.AsReadOnly();

    public void Initialize()
    {
        // Générer des données de démonstration
        for (int i = 0; i < 20; i++)
        {
            _dataItems.Add(new DataItem
            {
                Id = i + 1,
                Name = $"Élément {i + 1}",
                Timestamp = DateTime.Now.AddDays(-i),
                Value = (i * 123) % 1000
            });
        }

        Console.WriteLine($"[DataService] Initialized with {_dataItems.Count} data items");
    }

    public void Update(double deltaTime)
    {
        // Pas de mise à jour nécessaire
    }

    public void Dispose()
    {
        Console.WriteLine("[DataService] Disposing");
        _dataItems.Clear();
    }
}
