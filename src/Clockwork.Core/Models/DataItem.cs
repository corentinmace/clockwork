namespace Clockwork.Core.Models;

/// <summary>
/// Élément de données générique.
/// </summary>
public class DataItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public int Value { get; set; }
}
