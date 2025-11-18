using Clockwork.Core.Models;

namespace Clockwork.Core.Services;

/// <summary>
/// Service gérant les utilisateurs.
/// </summary>
public class UserService : IApplicationService
{
    private List<User> _users = new();

    public IReadOnlyList<User> GetUsers() => _users.AsReadOnly();

    public void AddUser(User user)
    {
        user.Id = _users.Count + 1;
        _users.Add(user);
    }

    public void Initialize()
    {
        // Données de démonstration
        _users.Add(new User { Id = 1, Name = "Alice Martin", Email = "alice.martin@example.com", Role = "Admin" });
        _users.Add(new User { Id = 2, Name = "Bob Dupont", Email = "bob.dupont@example.com", Role = "Utilisateur" });
        _users.Add(new User { Id = 3, Name = "Charlie Blanc", Email = "charlie.blanc@example.com", Role = "Utilisateur" });
        _users.Add(new User { Id = 4, Name = "Diana Noir", Email = "diana.noir@example.com", Role = "Utilisateur" });
        _users.Add(new User { Id = 5, Name = "Ethan Vert", Email = "ethan.vert@example.com", Role = "Utilisateur" });

        Console.WriteLine($"[UserService] Initialized with {_users.Count} users");
    }

    public void Update(double deltaTime)
    {
        // Pas de mise à jour nécessaire pour ce service
    }

    public void Dispose()
    {
        Console.WriteLine("[UserService] Disposing");
        _users.Clear();
    }
}
