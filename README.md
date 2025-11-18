# Clockwork

Application moderne .NET 8 avec interface graphique ImGui et OpenGL.

## Architecture

Ce projet suit une architecture claire avec séparation des responsabilités :

### Backend - `Clockwork.Core`
- Contient toute la logique métier
- Indépendant de l'interface utilisateur
- Services modulaires et testables
- Architecture basée sur les interfaces

### Frontend - `Clockwork.UI`
- Interface utilisateur avec ImGui.NET
- Rendu OpenGL via OpenTK 4
- Intégration complète ImGui/OpenGL
- Thème sombre moderne

## Stack Technique

- **.NET 8** - Framework moderne et performant
- **ImGui.NET 1.90.4** - Interface graphique immédiate
- **OpenTK 4.8.2** - Bindings OpenGL pour .NET
- **OpenGL 3.3+** - Rendu graphique

## Prérequis

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Système d'exploitation compatible OpenGL 3.3+ :
  - Windows 7+
  - Linux (avec drivers graphiques à jour)
  - macOS 10.13+

## Installation

### Cloner le projet
```bash
git clone <url-du-repo>
cd clockwork
```

### Restaurer les dépendances
```bash
dotnet restore
```

## Compilation

### Mode Debug
```bash
dotnet build
```

### Mode Release
```bash
dotnet build -c Release
```

## Exécution

### Depuis la racine
```bash
dotnet run --project src/Clockwork.UI
```

### Depuis le dossier UI
```bash
cd src/Clockwork.UI
dotnet run
```

## Structure du Projet

```
clockwork/
├── src/
│   ├── Clockwork.Core/              # Backend - Logique métier
│   │   ├── IApplicationService.cs   # Interface pour les services
│   │   ├── ApplicationContext.cs    # Contexte global de l'application
│   │   └── Services/
│   │       └── ExampleService.cs    # Exemple de service
│   │
│   └── Clockwork.UI/                # Frontend - Interface graphique
│       ├── Program.cs               # Point d'entrée
│       ├── MainWindow.cs            # Fenêtre principale
│       └── ImGuiController.cs       # Contrôleur ImGui/OpenGL
│
├── Clockwork.sln                    # Solution Visual Studio
├── .gitignore
└── README.md
```

## Développement

### Ajouter un nouveau service

1. Créer une classe implémentant `IApplicationService` dans `Clockwork.Core/Services/`
2. Enregistrer le service dans `Program.cs` :

```csharp
var monService = new MonService();
appContext.AddService(monService);
```

### Ajouter une fenêtre ImGui

Dans `MainWindow.cs`, dans la méthode `DrawUI()` :

```csharp
private void DrawUI()
{
    ImGui.Begin("Ma Fenêtre");
    ImGui.Text("Bonjour!");
    if (ImGui.Button("Mon Bouton"))
    {
        // Action
    }
    ImGui.End();
}
```

### Personnaliser le style

Modifier la méthode `ConfigureImGuiStyle()` dans `MainWindow.cs` pour ajuster les couleurs, espacements, et arrondis.

## Fonctionnalités Actuelles

- Fenêtre OpenGL avec gestion des événements
- Interface ImGui fonctionnelle avec thème sombre
- Menu principal
- Fenêtre de démonstration
- Affichage des métriques (FPS, frame time)
- Architecture modulaire backend/frontend
- Système de services extensible

## Prochaines Étapes

Suggestions pour étendre le projet :

- [ ] Ajouter un système de logging
- [ ] Implémenter la sauvegarde/chargement de configuration
- [ ] Créer des services métier spécifiques
- [ ] Ajouter des tests unitaires
- [ ] Implémenter un système de plugins
- [ ] Ajouter le support multi-fenêtres

## Ressources

- [ImGui Documentation](https://github.com/ocornut/imgui)
- [ImGui.NET](https://github.com/ImGuiNET/ImGui.NET)
- [OpenTK](https://opentk.net/)
- [.NET 8 Documentation](https://learn.microsoft.com/dotnet/)

## Licence

À définir selon vos besoins.
