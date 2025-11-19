# Guide : Créer une Nouvelle Vue avec son Service

**Projet:** Clockwork - Éditeur de ROM Pokémon DS
**Date:** 2025-11-19

---

## Table des Matières

1. [Introduction](#introduction)
2. [Étape 1 : Créer le Service](#étape-1--créer-le-service)
3. [Étape 2 : Créer la Vue](#étape-2--créer-la-vue)
4. [Étape 3 : Enregistrer le Service](#étape-3--enregistrer-le-service)
5. [Étape 4 : Intégrer la Vue](#étape-4--intégrer-la-vue)
6. [Étape 5 : Ajouter un Menu](#étape-5--ajouter-un-menu)
7. [Exemples Complets](#exemples-complets)
8. [Bonnes Pratiques](#bonnes-pratiques)

---

## Introduction

Ce guide vous explique comment ajouter une nouvelle fonctionnalité dans Clockwork en suivant l'architecture Service-Vue du projet.

### Architecture

```
┌─────────────────────────────────────┐
│      Clockwork.UI (Frontend)        │
│        Vue ImGui                    │
└──────────────┬──────────────────────┘
               │ utilise
               ↓
┌─────────────────────────────────────┐
│     Clockwork.Core (Backend)        │
│         Service                     │
└─────────────────────────────────────┘
```

**Principe clé** : La logique métier est dans le service (Core), l'interface utilisateur est dans la vue (UI).

---

## Étape 1 : Créer le Service

### 1.1 Créer le fichier du service

Créez un nouveau fichier dans `src/Clockwork.Core/Services/` :

**Emplacement** : `src/Clockwork.Core/Services/MonNouveauService.cs`

```csharp
using Clockwork.Core.Models;

namespace Clockwork.Core.Services;

/// <summary>
/// Service responsable de [description de la fonctionnalité]
/// </summary>
public class MonNouveauService : IApplicationService
{
    private readonly ApplicationContext _appContext;

    // Dépendances (autres services nécessaires)
    private RomService? _romService;

    // État du service
    private bool _isInitialized = false;

    // Propriétés publiques
    public bool IsReady => _isInitialized && _romService?.IsLoaded == true;

    /// <summary>
    /// Constructeur - injecte le contexte applicatif
    /// </summary>
    public MonNouveauService(ApplicationContext appContext)
    {
        _appContext = appContext;
    }

    /// <summary>
    /// Initialisation - appelée une fois au démarrage
    /// </summary>
    public void Initialize()
    {
        AppLogger.Info("MonNouveauService: Initializing...");

        // Récupérer les dépendances (autres services)
        _romService = _appContext.GetService<RomService>();

        if (_romService == null)
        {
            AppLogger.Error("MonNouveauService: RomService not found!");
            return;
        }

        _isInitialized = true;
        AppLogger.Info("MonNouveauService: Initialized successfully");
    }

    /// <summary>
    /// Mise à jour - appelée chaque frame (60+ fois par seconde)
    /// </summary>
    public void Update(double deltaTime)
    {
        // Logique à exécuter chaque frame
        // ⚠️ Évitez les calculs lourds ici !
    }

    /// <summary>
    /// Nettoyage - appelée à la fermeture de l'application
    /// </summary>
    public void Dispose()
    {
        AppLogger.Info("MonNouveauService: Disposing...");
        // Libérer les ressources (fichiers, connexions, etc.)
    }

    // === MÉTHODES MÉTIER ===

    /// <summary>
    /// Exemple : charger des données
    /// </summary>
    public MyData? LoadData(int id)
    {
        if (!IsReady)
        {
            AppLogger.Warning("MonNouveauService: Service not ready");
            return null;
        }

        try
        {
            AppLogger.Info($"MonNouveauService: Loading data {id}...");

            // Construire le chemin du fichier
            string romPath = _romService!.RomPath;
            string filePath = Path.Combine(romPath, "unpacked", "monDossier", $"{id:D4}");

            if (!File.Exists(filePath))
            {
                AppLogger.Error($"MonNouveauService: File not found: {filePath}");
                return null;
            }

            // Lire et parser le fichier
            byte[] fileData = File.ReadAllBytes(filePath);
            var data = MyData.ReadFromBytes(fileData);

            AppLogger.Info($"MonNouveauService: Data {id} loaded successfully");
            return data;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"MonNouveauService: Error loading data {id}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Exemple : sauvegarder des données
    /// </summary>
    public bool SaveData(int id, MyData data)
    {
        if (!IsReady)
        {
            AppLogger.Warning("MonNouveauService: Service not ready");
            return false;
        }

        try
        {
            AppLogger.Info($"MonNouveauService: Saving data {id}...");

            string romPath = _romService!.RomPath;
            string filePath = Path.Combine(romPath, "unpacked", "monDossier", $"{id:D4}");

            // Convertir en bytes et sauvegarder
            byte[] fileData = data.ToBytes();
            File.WriteAllBytes(filePath, fileData);

            AppLogger.Info($"MonNouveauService: Data {id} saved successfully");
            return true;
        }
        catch (Exception ex)
        {
            AppLogger.Error($"MonNouveauService: Error saving data {id}: {ex.Message}");
            return false;
        }
    }
}
```

### 1.2 Points clés du service

| Élément | Description |
|---------|-------------|
| **IApplicationService** | Interface obligatoire avec Initialize(), Update(), Dispose() |
| **ApplicationContext** | Injecté dans le constructeur pour accéder aux autres services |
| **Initialize()** | Récupérer les dépendances via `GetService<T>()` |
| **Update()** | Éviter la logique lourde (appelé 60+ fois/seconde) |
| **Dispose()** | Libérer les ressources (fichiers, mémoire, etc.) |
| **AppLogger** | Utiliser pour tracer les opérations importantes |

---

## Étape 2 : Créer la Vue

### 2.1 Créer le fichier de la vue

Créez un nouveau fichier dans `src/Clockwork.UI/Views/` :

**Emplacement** : `src/Clockwork.UI/Views/MaVueView.cs`

```csharp
using ImGuiNET;
using Clockwork.Core;
using Clockwork.Core.Services;
using Clockwork.Core.Models;
using System.Numerics;

namespace Clockwork.UI.Views;

/// <summary>
/// Vue ImGui pour [description de la fonctionnalité]
/// </summary>
public class MaVueView : IView
{
    // === PROPRIÉTÉS ===

    /// <summary>
    /// Contrôle la visibilité de la fenêtre
    /// </summary>
    public bool IsVisible { get; set; } = false;

    private ApplicationContext _appContext = null!;

    // Services nécessaires
    private MonNouveauService? _monService;
    private RomService? _romService;

    // État de la vue
    private int _selectedId = 0;
    private MyData? _currentData = null;
    private bool _isLoading = false;
    private string _statusMessage = "";

    // État UI (inputs, checkboxes, etc.)
    private int _inputValue = 0;
    private bool _checkboxValue = false;
    private string _textValue = "";

    // === LIFECYCLE ===

    /// <summary>
    /// Initialisation de la vue - récupérer les services
    /// </summary>
    public void Initialize(ApplicationContext appContext)
    {
        _appContext = appContext;

        // Récupérer les services nécessaires
        _monService = appContext.GetService<MonNouveauService>();
        _romService = appContext.GetService<RomService>();

        if (_monService == null)
        {
            AppLogger.Error("MaVueView: MonNouveauService not found!");
        }

        AppLogger.Info("MaVueView: Initialized");
    }

    /// <summary>
    /// Rendu de la vue - appelé chaque frame si visible
    /// </summary>
    public void Draw()
    {
        // Ne rien faire si la fenêtre n'est pas visible
        if (!IsVisible)
            return;

        // Taille par défaut de la fenêtre (au premier affichage)
        ImGui.SetNextWindowSize(new Vector2(800, 600), ImGuiCond.FirstUseEver);

        // Créer la fenêtre
        if (!ImGui.Begin("Ma Vue", ref IsVisible))
        {
            ImGui.End();
            return;
        }

        // Vérifier que la ROM est chargée
        if (_romService?.IsLoaded != true)
        {
            ImGui.TextColored(new Vector4(1, 0, 0, 1), "⚠ Aucune ROM chargée");
            ImGui.Text("Veuillez charger une ROM dans le chargeur de ROM.");
            ImGui.End();
            return;
        }

        // Dessiner le contenu
        DrawToolbar();
        ImGui.Separator();

        DrawMainContent();

        ImGui.End();
    }

    // === SECTIONS DE L'UI ===

    /// <summary>
    /// Barre d'outils en haut
    /// </summary>
    private void DrawToolbar()
    {
        ImGui.Text("ID:");
        ImGui.SameLine();
        ImGui.SetNextItemWidth(150);
        ImGui.InputInt("##id", ref _selectedId);

        // Limiter l'ID entre 0 et 999
        if (_selectedId < 0) _selectedId = 0;
        if (_selectedId > 999) _selectedId = 999;

        ImGui.SameLine();
        if (ImGui.Button("Charger"))
        {
            LoadData();
        }

        ImGui.SameLine();
        bool canSave = _currentData != null && !_isLoading;
        if (!canSave)
        {
            ImGui.BeginDisabled();
        }

        if (ImGui.Button("Sauvegarder"))
        {
            SaveData();
        }

        if (!canSave)
        {
            ImGui.EndDisabled();
        }

        // Message de statut
        if (!string.IsNullOrEmpty(_statusMessage))
        {
            ImGui.SameLine();
            ImGui.TextColored(new Vector4(0, 1, 0, 1), _statusMessage);
        }
    }

    /// <summary>
    /// Contenu principal de la vue
    /// </summary>
    private void DrawMainContent()
    {
        if (_isLoading)
        {
            ImGui.Text("Chargement...");
            return;
        }

        if (_currentData == null)
        {
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1),
                              "Sélectionnez un ID et cliquez sur Charger");
            return;
        }

        // === ÉDITION DES DONNÉES ===

        ImGui.Text($"Édition de l'élément {_selectedId}");
        ImGui.Spacing();

        // Input numérique
        ImGui.Text("Valeur:");
        ImGui.SetNextItemWidth(200);
        if (ImGui.InputInt("##value", ref _inputValue))
        {
            _currentData.Value = _inputValue;
        }

        // Checkbox
        bool checkbox = _checkboxValue;
        if (ImGui.Checkbox("Option activée", ref checkbox))
        {
            _checkboxValue = checkbox;
            _currentData.IsEnabled = checkbox;
        }

        // Input texte
        ImGui.Text("Nom:");
        ImGui.SetNextItemWidth(300);
        string text = _textValue;
        if (ImGui.InputText("##name", ref text, 256))
        {
            _textValue = text;
            _currentData.Name = text;
        }

        // ComboBox
        string[] options = new[] { "Option 1", "Option 2", "Option 3" };
        int currentOption = _currentData.OptionIndex;
        if (ImGui.BeginCombo("Type", options[currentOption]))
        {
            for (int i = 0; i < options.Length; i++)
            {
                bool isSelected = currentOption == i;
                if (ImGui.Selectable(options[i], isSelected))
                {
                    _currentData.OptionIndex = i;
                }

                if (isSelected)
                {
                    ImGui.SetItemDefaultFocus();
                }
            }
            ImGui.EndCombo();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Section d'informations
        ImGui.TextColored(new Vector4(0.5f, 0.8f, 1, 1), "Informations");
        ImGui.Text($"Taille: {_currentData.Size} bytes");
        ImGui.Text($"Dernière modification: {_currentData.LastModified}");
    }

    // === ACTIONS ===

    /// <summary>
    /// Charger les données depuis le service
    /// </summary>
    private void LoadData()
    {
        if (_monService == null || !_monService.IsReady)
        {
            _statusMessage = "❌ Service non prêt";
            return;
        }

        _isLoading = true;
        _statusMessage = "";

        try
        {
            var data = _monService.LoadData(_selectedId);

            if (data == null)
            {
                _statusMessage = "❌ Erreur de chargement";
                _currentData = null;
            }
            else
            {
                _currentData = data;

                // Synchroniser l'UI avec les données chargées
                _inputValue = data.Value;
                _checkboxValue = data.IsEnabled;
                _textValue = data.Name;

                _statusMessage = "✓ Chargé";
            }
        }
        finally
        {
            _isLoading = false;
        }
    }

    /// <summary>
    /// Sauvegarder les données via le service
    /// </summary>
    private void SaveData()
    {
        if (_monService == null || _currentData == null)
            return;

        _statusMessage = "";

        bool success = _monService.SaveData(_selectedId, _currentData);

        if (success)
        {
            _statusMessage = "✓ Sauvegardé";
        }
        else
        {
            _statusMessage = "❌ Erreur de sauvegarde";
        }
    }
}
```

### 2.2 Points clés de la vue

| Élément | Description |
|---------|-------------|
| **IView** | Interface avec `IsVisible` et `Initialize()` |
| **Draw()** | Appelée chaque frame - redessine toute l'UI |
| **IsVisible** | Si `false`, la fenêtre est fermée |
| **ImGui.Begin/End** | Délimitent la fenêtre |
| **ref IsVisible** | Permet de fermer la fenêtre avec le bouton X |
| **ImGuiCond.FirstUseEver** | Taille par défaut seulement au premier affichage |
| **État UI** | Variables privées pour inputs, checkboxes, etc. |

---

## Étape 3 : Enregistrer le Service

### 3.1 Modifier Program.cs

Ouvrez `src/Clockwork.UI/Program.cs` et enregistrez votre service :

```csharp
// Créer le contexte applicatif
var appContext = new ApplicationContext();

// === ENREGISTRER LES SERVICES ===
// (Services existants)
var romService = new RomService(appContext);
appContext.AddService(romService);

var dialogService = new DialogService(appContext);
appContext.AddService(dialogService);

// ... autres services ...

// AJOUTEZ VOTRE SERVICE ICI
var monNouveauService = new MonNouveauService(appContext);
appContext.AddService(monNouveauService);

// Initialiser tous les services
appContext.Initialize();
```

### 3.2 Ordre d'enregistrement

⚠️ **Important** : Si votre service dépend d'autres services, enregistrez-le **après** ses dépendances.

**Exemple** :
```csharp
// RomService doit être enregistré en premier
appContext.AddService(romService);

// Puis les services qui dépendent de RomService
appContext.AddService(mapService);
appContext.AddService(headerService);
appContext.AddService(monNouveauService); // Dépend de RomService
```

---

## Étape 4 : Intégrer la Vue

### 4.1 Modifier MainWindow.cs

Ouvrez `src/Clockwork.UI/MainWindow.cs` et ajoutez votre vue :

**1. Déclarer la vue comme champ privé :**

```csharp
public class MainWindow
{
    // ... champs existants ...

    private RomLoaderView _romLoaderView = new();
    private MapEditorView _mapEditorView = new();

    // AJOUTEZ VOTRE VUE ICI
    private MaVueView _maVueView = new();
```

**2. Initialiser la vue dans Initialize() :**

```csharp
protected override void Initialize()
{
    base.Initialize();

    // ... initialisations existantes ...

    _romLoaderView.Initialize(_appContext);
    _mapEditorView.Initialize(_appContext);

    // AJOUTEZ L'INITIALISATION ICI
    _maVueView.Initialize(_appContext);
}
```

**3. Dessiner la vue dans DrawUI() :**

```csharp
private void DrawUI()
{
    DrawMenuBar();
    DrawSidebar();

    // === VUES ===
    _romLoaderView.Draw();
    _mapEditorView.Draw();

    // AJOUTEZ LE DESSIN ICI
    _maVueView.Draw();

    // ... autres vues ...
}
```

---

## Étape 5 : Ajouter un Menu

### 5.1 Ajouter un item de menu

Dans `MainWindow.cs`, méthode `DrawMenuBar()` :

```csharp
private void DrawMenuBar()
{
    if (ImGui.BeginMenuBar())
    {
        if (ImGui.BeginMenu("Fichier"))
        {
            // ... items existants ...
            ImGui.EndMenu();
        }

        if (ImGui.BeginMenu("Éditeurs"))
        {
            if (ImGui.MenuItem("Éditeur de Maps"))
            {
                _mapEditorView.IsVisible = true;
            }

            if (ImGui.MenuItem("Éditeur de Headers"))
            {
                _headerEditorView.IsVisible = true;
            }

            // AJOUTEZ VOTRE MENU ICI
            if (ImGui.MenuItem("Ma Vue"))
            {
                _maVueView.IsVisible = true;
            }

            ImGui.EndMenu();
        }

        // ... autres menus ...

        ImGui.EndMenuBar();
    }
}
```

### 5.2 Alternative : Bouton dans la sidebar

Dans `DrawSidebar()` :

```csharp
private void DrawSidebar()
{
    // ... code existant ...

    ImGui.Spacing();
    if (ImGui.Button("Ma Vue", new Vector2(-1, 40)))
    {
        _maVueView.IsVisible = true;
    }
}
```

---

## Exemples Complets

### Exemple 1 : Service Simple (Lecture seule)

**ScriptService.cs** (lecture de scripts)

```csharp
public class ScriptService : IApplicationService
{
    private readonly ApplicationContext _appContext;
    private RomService? _romService;

    public ScriptService(ApplicationContext appContext)
    {
        _appContext = appContext;
    }

    public void Initialize()
    {
        _romService = _appContext.GetService<RomService>();
    }

    public void Update(double deltaTime) { }
    public void Dispose() { }

    public List<Script> GetAllScripts()
    {
        if (_romService?.IsLoaded != true)
            return new List<Script>();

        string scriptsPath = Path.Combine(_romService.RomPath, "unpacked", "scripts");
        var scripts = new List<Script>();

        foreach (var file in Directory.GetFiles(scriptsPath))
        {
            byte[] data = File.ReadAllBytes(file);
            scripts.Add(Script.Parse(data));
        }

        return scripts;
    }
}
```

### Exemple 2 : Vue avec Liste

**ScriptListView.cs**

```csharp
public class ScriptListView : IView
{
    public bool IsVisible { get; set; }
    private ScriptService? _scriptService;
    private List<Script> _scripts = new();
    private int _selectedIndex = -1;

    public void Initialize(ApplicationContext appContext)
    {
        _scriptService = appContext.GetService<ScriptService>();
    }

    public void Draw()
    {
        if (!IsVisible) return;

        ImGui.Begin("Scripts", ref IsVisible);

        if (ImGui.Button("Rafraîchir"))
        {
            _scripts = _scriptService?.GetAllScripts() ?? new List<Script>();
        }

        ImGui.Separator();

        // Liste déroulante
        if (ImGui.BeginChild("ScriptList", new Vector2(250, 0), ImGuiChildFlags.Border))
        {
            for (int i = 0; i < _scripts.Count; i++)
            {
                bool isSelected = _selectedIndex == i;
                if (ImGui.Selectable($"Script {i}", isSelected))
                {
                    _selectedIndex = i;
                }
            }
        }
        ImGui.EndChild();

        ImGui.SameLine();

        // Détails du script sélectionné
        if (ImGui.BeginChild("ScriptDetails"))
        {
            if (_selectedIndex >= 0 && _selectedIndex < _scripts.Count)
            {
                var script = _scripts[_selectedIndex];
                ImGui.Text($"Nom: {script.Name}");
                ImGui.Text($"Taille: {script.Size} bytes");
                ImGui.Text($"Instructions: {script.Instructions.Count}");
            }
            else
            {
                ImGui.Text("Sélectionnez un script");
            }
        }
        ImGui.EndChild();

        ImGui.End();
    }
}
```

### Exemple 3 : Service avec Cache

**TextureService.cs** (avec cache pour performances)

```csharp
public class TextureService : IApplicationService
{
    private readonly ApplicationContext _appContext;
    private RomService? _romService;

    // Cache des textures chargées
    private Dictionary<int, Texture> _textureCache = new();

    public TextureService(ApplicationContext appContext)
    {
        _appContext = appContext;
    }

    public void Initialize()
    {
        _romService = _appContext.GetService<RomService>();
    }

    public void Update(double deltaTime) { }

    public void Dispose()
    {
        // Vider le cache
        _textureCache.Clear();
    }

    public Texture? GetTexture(int id)
    {
        // Vérifier le cache d'abord
        if (_textureCache.TryGetValue(id, out var cached))
        {
            return cached;
        }

        // Charger depuis le fichier
        var texture = LoadTextureFromFile(id);

        if (texture != null)
        {
            // Ajouter au cache
            _textureCache[id] = texture;
        }

        return texture;
    }

    private Texture? LoadTextureFromFile(int id)
    {
        if (_romService?.IsLoaded != true)
            return null;

        string path = Path.Combine(_romService.RomPath, "unpacked", "textures", $"{id:D4}");

        if (!File.Exists(path))
            return null;

        byte[] data = File.ReadAllBytes(path);
        return Texture.Parse(data);
    }

    public void ClearCache()
    {
        _textureCache.Clear();
    }
}
```

---

## Bonnes Pratiques

### ✅ À FAIRE

1. **Logs détaillés**
   ```csharp
   AppLogger.Info($"MonService: Loading item {id}...");
   AppLogger.Error($"MonService: Error: {ex.Message}");
   ```

2. **Vérifier les dépendances**
   ```csharp
   if (_romService?.IsLoaded != true)
   {
       AppLogger.Warning("ROM not loaded");
       return null;
   }
   ```

3. **Gestion d'erreurs**
   ```csharp
   try
   {
       // Code risqué
   }
   catch (Exception ex)
   {
       AppLogger.Error($"Error: {ex.Message}");
       return false;
   }
   ```

4. **Désactiver les boutons invalides**
   ```csharp
   bool canSave = _data != null;
   if (!canSave) ImGui.BeginDisabled();

   if (ImGui.Button("Sauvegarder"))
   {
       Save();
   }

   if (!canSave) ImGui.EndDisabled();
   ```

5. **Cacher les calculs lourds**
   ```csharp
   // Dans la vue
   private List<Item>? _cachedItems = null;
   private bool _needsRefresh = true;

   public void Draw()
   {
       if (_needsRefresh)
       {
           _cachedItems = _service.GetAllItems(); // Lourd
           _needsRefresh = false;
       }

       // Utiliser _cachedItems pour l'affichage
   }
   ```

### ❌ À ÉVITER

1. **Ne pas faire de calculs lourds dans Draw()**
   ```csharp
   // ❌ MAUVAIS - appelé 60 fois/seconde
   public void Draw()
   {
       var items = _service.LoadAllFromDisk(); // TROP LOURD
       foreach (var item in items)
       {
           ImGui.Text(item.Name);
       }
   }
   ```

2. **Ne pas oublier les vérifications null**
   ```csharp
   // ❌ MAUVAIS
   var data = _service.Load(id);
   data.Value = 10; // Crash si null !

   // ✅ BON
   var data = _service.Load(id);
   if (data != null)
   {
       data.Value = 10;
   }
   ```

3. **Ne pas mélanger logique et UI**
   ```csharp
   // ❌ MAUVAIS - logique dans la vue
   public void Draw()
   {
       if (ImGui.Button("Charger"))
       {
           byte[] data = File.ReadAllBytes(path); // ❌ Dans la vue !
           _item = Parse(data); // ❌ Dans la vue !
       }
   }

   // ✅ BON - logique dans le service
   public void Draw()
   {
       if (ImGui.Button("Charger"))
       {
           _item = _service.Load(id); // ✅ Appel au service
       }
   }
   ```

4. **Ne pas enregistrer les services dans le mauvais ordre**
   ```csharp
   // ❌ MAUVAIS
   appContext.AddService(mapService);     // Dépend de romService
   appContext.AddService(romService);     // Enregistré après !

   // ✅ BON
   appContext.AddService(romService);     // D'abord les dépendances
   appContext.AddService(mapService);     // Puis les services dépendants
   ```

---

## Checklist Complète

Utilisez cette checklist pour créer une nouvelle fonctionnalité :

- [ ] **Service créé** dans `src/Clockwork.Core/Services/`
- [ ] Service implémente `IApplicationService`
- [ ] Méthodes `Initialize()`, `Update()`, `Dispose()` implémentées
- [ ] Dépendances récupérées via `GetService<T>()`
- [ ] Logs ajoutés avec `AppLogger`
- [ ] Gestion d'erreurs avec try/catch
- [ ] **Vue créée** dans `src/Clockwork.UI/Views/`
- [ ] Vue implémente `IView`
- [ ] Propriété `IsVisible` ajoutée
- [ ] Méthodes `Initialize()` et `Draw()` implémentées
- [ ] Vérification ROM chargée dans `Draw()`
- [ ] **Service enregistré** dans `Program.cs`
- [ ] Ordre d'enregistrement respecté (dépendances d'abord)
- [ ] **Vue intégrée** dans `MainWindow.cs`
- [ ] Champ privé déclaré
- [ ] Initialisée dans `Initialize()`
- [ ] Dessinée dans `DrawUI()`
- [ ] **Menu ajouté** dans `DrawMenuBar()` ou `DrawSidebar()`
- [ ] **Testé** : compilation sans erreurs
- [ ] **Testé** : ouverture/fermeture de la fenêtre
- [ ] **Testé** : chargement et sauvegarde de données

---

## Ressources

- **CLAUDE.md** - Documentation complète de l'architecture
- **src/Clockwork.UI/Views/MapEditorView.cs** - Exemple de vue complexe (851 lignes)
- **src/Clockwork.Core/Services/MapService.cs** - Exemple de service complet (237 lignes)
- **ImGui.NET Documentation** - https://github.com/ImGuiNET/ImGui.NET

---

**Fin du guide**

Pour toute question, consultez les exemples existants dans le code ou référez-vous à CLAUDE.md.
