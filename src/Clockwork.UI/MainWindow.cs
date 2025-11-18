using Clockwork.Core;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace Clockwork.UI;

/// <summary>
/// Fenêtre principale de l'application utilisant OpenTK et ImGui.
/// </summary>
public class MainWindow : GameWindow
{
    private ImGuiController? _imguiController;
    private ApplicationContext _appContext;

    public MainWindow(ApplicationContext appContext, GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
        : base(gameWindowSettings, nativeWindowSettings)
    {
        _appContext = appContext;
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        Title = "Clockwork - .NET 8 + ImGui";

        _imguiController = new ImGuiController(ClientSize.X, ClientSize.Y);

        // Configuration du style ImGui
        ConfigureImGuiStyle();

        Console.WriteLine("Application started successfully!");
    }

    private void ConfigureImGuiStyle()
    {
        var style = ImGui.GetStyle();

        // Arrondis
        style.WindowRounding = 6.0f;
        style.FrameRounding = 3.0f;
        style.GrabRounding = 3.0f;
        style.TabRounding = 3.0f;

        // Espacements
        style.WindowPadding = new System.Numerics.Vector2(10, 10);
        style.FramePadding = new System.Numerics.Vector2(8, 4);
        style.ItemSpacing = new System.Numerics.Vector2(8, 4);

        // Couleurs (thème sombre moderne)
        var colors = style.Colors;
        colors[(int)ImGuiCol.WindowBg] = new System.Numerics.Vector4(0.11f, 0.11f, 0.11f, 0.94f);
        colors[(int)ImGuiCol.ChildBg] = new System.Numerics.Vector4(0.15f, 0.15f, 0.15f, 1.00f);
        colors[(int)ImGuiCol.PopupBg] = new System.Numerics.Vector4(0.11f, 0.11f, 0.11f, 0.94f);
        colors[(int)ImGuiCol.Border] = new System.Numerics.Vector4(0.25f, 0.25f, 0.27f, 0.50f);
        colors[(int)ImGuiCol.FrameBg] = new System.Numerics.Vector4(0.20f, 0.21f, 0.22f, 1.00f);
        colors[(int)ImGuiCol.FrameBgHovered] = new System.Numerics.Vector4(0.30f, 0.31f, 0.32f, 1.00f);
        colors[(int)ImGuiCol.FrameBgActive] = new System.Numerics.Vector4(0.25f, 0.26f, 0.27f, 1.00f);
        colors[(int)ImGuiCol.TitleBg] = new System.Numerics.Vector4(0.08f, 0.08f, 0.09f, 1.00f);
        colors[(int)ImGuiCol.TitleBgActive] = new System.Numerics.Vector4(0.15f, 0.15f, 0.16f, 1.00f);
        colors[(int)ImGuiCol.MenuBarBg] = new System.Numerics.Vector4(0.15f, 0.15f, 0.16f, 1.00f);
        colors[(int)ImGuiCol.CheckMark] = new System.Numerics.Vector4(0.40f, 0.70f, 1.00f, 1.00f);
        colors[(int)ImGuiCol.SliderGrab] = new System.Numerics.Vector4(0.40f, 0.70f, 1.00f, 1.00f);
        colors[(int)ImGuiCol.SliderGrabActive] = new System.Numerics.Vector4(0.50f, 0.80f, 1.00f, 1.00f);
        colors[(int)ImGuiCol.Button] = new System.Numerics.Vector4(0.25f, 0.26f, 0.27f, 1.00f);
        colors[(int)ImGuiCol.ButtonHovered] = new System.Numerics.Vector4(0.35f, 0.36f, 0.37f, 1.00f);
        colors[(int)ImGuiCol.ButtonActive] = new System.Numerics.Vector4(0.40f, 0.70f, 1.00f, 1.00f);
        colors[(int)ImGuiCol.Header] = new System.Numerics.Vector4(0.25f, 0.26f, 0.27f, 1.00f);
        colors[(int)ImGuiCol.HeaderHovered] = new System.Numerics.Vector4(0.35f, 0.36f, 0.37f, 1.00f);
        colors[(int)ImGuiCol.HeaderActive] = new System.Numerics.Vector4(0.40f, 0.70f, 1.00f, 1.00f);
        colors[(int)ImGuiCol.Tab] = new System.Numerics.Vector4(0.15f, 0.15f, 0.16f, 1.00f);
        colors[(int)ImGuiCol.TabHovered] = new System.Numerics.Vector4(0.40f, 0.70f, 1.00f, 1.00f);
        colors[(int)ImGuiCol.TabActive] = new System.Numerics.Vector4(0.30f, 0.50f, 0.80f, 1.00f);
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);

        GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
        _imguiController?.WindowResized(ClientSize.X, ClientSize.Y);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        _imguiController?.Update(this, args.Time);

        // Mettre à jour le contexte de l'application
        _appContext.Update(args.Time);

        // Si l'application doit se fermer
        if (!_appContext.IsRunning)
        {
            Close();
        }
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        // Effacer l'écran
        GL.ClearColor(new Color4(0.1f, 0.1f, 0.1f, 1.0f));
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // Dessiner l'interface ImGui
        DrawUI();

        // Rendu ImGui
        _imguiController?.Render();

        SwapBuffers();
    }

    private void DrawUI()
    {
        // Créer un DockSpace fullscreen qui occupe toute la fenêtre
        var viewport = ImGui.GetMainViewport();
        ImGui.SetNextWindowPos(viewport.WorkPos);
        ImGui.SetNextWindowSize(viewport.WorkSize);
        ImGui.SetNextWindowViewport(viewport.ID);

        ImGuiWindowFlags windowFlags = ImGuiWindowFlags.MenuBar | ImGuiWindowFlags.NoDocking;
        windowFlags |= ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove;
        windowFlags |= ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus;

        ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0.0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0.0f);
        ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(0.0f, 0.0f));

        ImGui.Begin("DockSpaceWindow", windowFlags);
        ImGui.PopStyleVar(3);

        // DockSpace
        ImGuiIOPtr io = ImGui.GetIO();
        if ((io.ConfigFlags & ImGuiConfigFlags.DockingEnable) != 0)
        {
            uint dockspaceId = ImGui.GetID("MainDockSpace");
            ImGui.DockSpace(dockspaceId, new System.Numerics.Vector2(0.0f, 0.0f), ImGuiDockNodeFlags.None);
        }

        // Menu principal
        if (ImGui.BeginMenuBar())
        {
            if (ImGui.BeginMenu("Fichier"))
            {
                if (ImGui.MenuItem("Quitter", "Alt+F4"))
                {
                    _appContext.IsRunning = false;
                }
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Aide"))
            {
                if (ImGui.MenuItem("À propos"))
                {
                    _showAboutWindow = true;
                }
                if (ImGui.MenuItem("Métriques ImGui"))
                {
                    _showMetricsWindow = !_showMetricsWindow;
                }
                ImGui.EndMenu();
            }

            ImGui.EndMenuBar();
        }

        ImGui.End();

        // Menu latéral
        DrawSidebar();

        // Dessiner toutes les fenêtres
        DrawWelcomeWindow();
        DrawPropertiesWindow();
        DrawConsoleWindow();
        DrawHierarchyWindow();
        DrawAboutWindow();
        DrawDashboardWindow();
        DrawSettingsWindow();
        DrawUserManagementWindow();
        DrawDataViewWindow();
        DrawReportsWindow();
        DrawAnalyticsWindow();

        if (_showMetricsWindow)
        {
            ImGui.ShowMetricsWindow(ref _showMetricsWindow);
        }
    }

    private void DrawSidebar()
    {
        ImGui.SetNextWindowPos(new System.Numerics.Vector2(0, ImGui.GetFrameHeight()));
        ImGui.SetNextWindowSize(new System.Numerics.Vector2(250, ImGui.GetIO().DisplaySize.Y - ImGui.GetFrameHeight()));

        ImGuiWindowFlags sidebarFlags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoCollapse;

        ImGui.Begin("Sidebar", sidebarFlags);

        ImGui.TextColored(new System.Numerics.Vector4(0.4f, 0.7f, 1.0f, 1.0f), "NAVIGATION");
        ImGui.Separator();
        ImGui.Spacing();

        // Section: Général
        if (ImGui.CollapsingHeader("Général", ImGuiTreeNodeFlags.DefaultOpen))
        {
            if (ImGui.Selectable("  Tableau de bord", _showDashboardWindow))
            {
                _showDashboardWindow = !_showDashboardWindow;
            }
            if (ImGui.Selectable("  Bienvenue", _showWelcomeWindow))
            {
                _showWelcomeWindow = !_showWelcomeWindow;
            }
            if (ImGui.Selectable("  Propriétés", _showPropertiesWindow))
            {
                _showPropertiesWindow = !_showPropertiesWindow;
            }
        }

        ImGui.Spacing();

        // Section: Développement
        if (ImGui.CollapsingHeader("Développement", ImGuiTreeNodeFlags.DefaultOpen))
        {
            if (ImGui.Selectable("  Console", _showConsoleWindow))
            {
                _showConsoleWindow = !_showConsoleWindow;
            }
            if (ImGui.Selectable("  Hiérarchie", _showHierarchyWindow))
            {
                _showHierarchyWindow = !_showHierarchyWindow;
            }
        }

        ImGui.Spacing();

        // Section: Données
        if (ImGui.CollapsingHeader("Données"))
        {
            if (ImGui.Selectable("  Vue des données", _showDataViewWindow))
            {
                _showDataViewWindow = !_showDataViewWindow;
            }
            if (ImGui.Selectable("  Rapports", _showReportsWindow))
            {
                _showReportsWindow = !_showReportsWindow;
            }
            if (ImGui.Selectable("  Analytiques", _showAnalyticsWindow))
            {
                _showAnalyticsWindow = !_showAnalyticsWindow;
            }
        }

        ImGui.Spacing();

        // Section: Système
        if (ImGui.CollapsingHeader("Système"))
        {
            if (ImGui.Selectable("  Paramètres", _showSettingsWindow))
            {
                _showSettingsWindow = !_showSettingsWindow;
            }
            if (ImGui.Selectable("  Gestion utilisateurs", _showUserManagementWindow))
            {
                _showUserManagementWindow = !_showUserManagementWindow;
            }
        }

        ImGui.End();
    }

    // États des fenêtres
    private bool _showWelcomeWindow = false;
    private bool _showPropertiesWindow = false;
    private bool _showConsoleWindow = false;
    private bool _showHierarchyWindow = false;
    private bool _showMetricsWindow = false;
    private bool _showAboutWindow = false;
    private bool _showDashboardWindow = true;
    private bool _showSettingsWindow = false;
    private bool _showUserManagementWindow = false;
    private bool _showDataViewWindow = false;
    private bool _showReportsWindow = false;
    private bool _showAnalyticsWindow = false;

    private void DrawWelcomeWindow()
    {
        if (!_showWelcomeWindow) return;

        ImGui.Begin("Bienvenue", ref _showWelcomeWindow);

        ImGui.Text("Bienvenue dans Clockwork!");
        ImGui.Spacing();

        ImGui.TextColored(new System.Numerics.Vector4(0.4f, 0.7f, 1.0f, 1.0f), "Architecture:");
        ImGui.BulletText(".NET 8");
        ImGui.BulletText("ImGui.NET pour l'interface");
        ImGui.BulletText("OpenTK pour OpenGL");
        ImGui.BulletText("Séparation Frontend/Backend");
        ImGui.BulletText("Docking fullscreen activé");
        ImGui.Spacing();

        ImGui.Separator();
        ImGui.Spacing();

        ImGui.Text($"FPS: {ImGui.GetIO().Framerate:F1}");
        ImGui.Text($"Frame Time: {1000.0f / ImGui.GetIO().Framerate:F2} ms");
        ImGui.Spacing();

        ImGui.TextWrapped("Vous pouvez déplacer et docker toutes les fenêtres où vous voulez dans l'interface.");

        ImGui.End();
    }

    private void DrawPropertiesWindow()
    {
        if (!_showPropertiesWindow) return;

        ImGui.Begin("Propriétés", ref _showPropertiesWindow);

        ImGui.Text("Fenêtre de propriétés");
        ImGui.Separator();

        ImGui.Text("Nom:");
        ImGui.SameLine();
        string name = "Clockwork";
        ImGui.InputText("##name", ref name, 100);

        ImGui.Text("Type:");
        ImGui.SameLine();
        ImGui.TextColored(new System.Numerics.Vector4(0.5f, 0.5f, 0.5f, 1.0f), "Application");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Détails"))
        {
            ImGui.BulletText("Version: 1.0.0");
            ImGui.BulletText("Framework: .NET 8");
            ImGui.BulletText("UI: ImGui.NET 1.90.5.1");
        }

        ImGui.End();
    }

    private void DrawConsoleWindow()
    {
        if (!_showConsoleWindow) return;

        ImGui.Begin("Console", ref _showConsoleWindow);

        ImGui.TextColored(new System.Numerics.Vector4(0.5f, 0.8f, 0.5f, 1.0f), "[INFO]");
        ImGui.SameLine();
        ImGui.Text("Application démarrée avec succès");

        ImGui.TextColored(new System.Numerics.Vector4(0.5f, 0.8f, 0.5f, 1.0f), "[INFO]");
        ImGui.SameLine();
        ImGui.Text("Backend initialisé");

        ImGui.TextColored(new System.Numerics.Vector4(0.5f, 0.8f, 0.5f, 1.0f), "[INFO]");
        ImGui.SameLine();
        ImGui.Text("Frontend prêt");

        ImGui.Separator();

        ImGui.TextColored(new System.Numerics.Vector4(0.4f, 0.7f, 1.0f, 1.0f), "[DEBUG]");
        ImGui.SameLine();
        ImGui.Text("Toutes les fenêtres peuvent être dockées librement");

        ImGui.End();
    }

    private void DrawHierarchyWindow()
    {
        if (!_showHierarchyWindow) return;

        ImGui.Begin("Hiérarchie", ref _showHierarchyWindow);

        ImGui.Text("Structure de l'application:");
        ImGui.Spacing();

        if (ImGui.TreeNode("Clockwork.Core (Backend)"))
        {
            if (ImGui.TreeNode("ApplicationContext"))
            {
                ImGui.BulletText("ExampleService");
                ImGui.TreePop();
            }
            ImGui.TreePop();
        }

        if (ImGui.TreeNode("Clockwork.UI (Frontend)"))
        {
            ImGui.BulletText("MainWindow");
            ImGui.BulletText("ImGuiController");
            ImGui.TreePop();
        }

        ImGui.End();
    }

    private void DrawAboutWindow()
    {
        if (!_showAboutWindow) return;

        ImGui.Begin("À propos", ref _showAboutWindow, ImGuiWindowFlags.AlwaysAutoResize);

        ImGui.Text("Clockwork");
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.Text("Version: 1.0.0");
        ImGui.Text("Framework: .NET 8");
        ImGui.Text("UI Library: ImGui.NET 1.90.5.1");
        ImGui.Text("Graphics: OpenTK 4.8.2 (OpenGL)");
        ImGui.Spacing();

        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.Button("Fermer"))
        {
            _showAboutWindow = false;
        }

        ImGui.End();
    }

    private void DrawDashboardWindow()
    {
        if (!_showDashboardWindow) return;

        ImGui.Begin("Tableau de bord", ref _showDashboardWindow);

        ImGui.TextColored(new System.Numerics.Vector4(0.4f, 0.7f, 1.0f, 1.0f), "Tableau de bord principal");
        ImGui.Separator();
        ImGui.Spacing();

        // Statistiques
        ImGui.Text("Statistiques rapides:");
        ImGui.Columns(3, "stats", false);

        ImGui.BeginChild("stat1", new System.Numerics.Vector2(0, 80), true);
        ImGui.TextColored(new System.Numerics.Vector4(0.5f, 0.8f, 0.5f, 1.0f), "Utilisateurs");
        ImGui.Text("1,234");
        ImGui.EndChild();

        ImGui.NextColumn();

        ImGui.BeginChild("stat2", new System.Numerics.Vector2(0, 80), true);
        ImGui.TextColored(new System.Numerics.Vector4(0.4f, 0.7f, 1.0f, 1.0f), "Projets");
        ImGui.Text("42");
        ImGui.EndChild();

        ImGui.NextColumn();

        ImGui.BeginChild("stat3", new System.Numerics.Vector2(0, 80), true);
        ImGui.TextColored(new System.Numerics.Vector4(1.0f, 0.7f, 0.4f, 1.0f), "Tâches");
        ImGui.Text("789");
        ImGui.EndChild();

        ImGui.Columns(1);
        ImGui.Spacing();

        ImGui.Separator();
        ImGui.Spacing();

        ImGui.Text($"FPS: {ImGui.GetIO().Framerate:F1}");
        ImGui.Text($"Frame Time: {1000.0f / ImGui.GetIO().Framerate:F2} ms");

        ImGui.End();
    }

    private void DrawSettingsWindow()
    {
        if (!_showSettingsWindow) return;

        ImGui.Begin("Paramètres", ref _showSettingsWindow);

        ImGui.Text("Paramètres de l'application");
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Général", ImGuiTreeNodeFlags.DefaultOpen))
        {
            bool autoSave = true;
            ImGui.Checkbox("Sauvegarde automatique", ref autoSave);

            int interval = 5;
            ImGui.SliderInt("Intervalle (min)", ref interval, 1, 30);
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Apparence"))
        {
            ImGui.Text("Thème:");
            string[] themes = { "Sombre", "Clair", "Système" };
            int currentTheme = 0;
            ImGui.Combo("##theme", ref currentTheme, themes, themes.Length);
        }

        ImGui.Spacing();

        if (ImGui.CollapsingHeader("Avancé"))
        {
            bool debugMode = false;
            ImGui.Checkbox("Mode debug", ref debugMode);

            bool showFps = true;
            ImGui.Checkbox("Afficher les FPS", ref showFps);
        }

        ImGui.End();
    }

    private void DrawUserManagementWindow()
    {
        if (!_showUserManagementWindow) return;

        ImGui.Begin("Gestion des utilisateurs", ref _showUserManagementWindow);

        ImGui.Text("Liste des utilisateurs");
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.Button("Ajouter utilisateur"))
        {
            // Action d'ajout
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Table des utilisateurs
        if (ImGui.BeginTable("users_table", 3, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
        {
            ImGui.TableSetupColumn("Nom");
            ImGui.TableSetupColumn("Email");
            ImGui.TableSetupColumn("Rôle");
            ImGui.TableHeadersRow();

            for (int i = 0; i < 5; i++)
            {
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.Text($"Utilisateur {i + 1}");
                ImGui.TableSetColumnIndex(1);
                ImGui.Text($"user{i + 1}@example.com");
                ImGui.TableSetColumnIndex(2);
                ImGui.Text(i == 0 ? "Admin" : "Utilisateur");
            }

            ImGui.EndTable();
        }

        ImGui.End();
    }

    private void DrawDataViewWindow()
    {
        if (!_showDataViewWindow) return;

        ImGui.Begin("Vue des données", ref _showDataViewWindow);

        ImGui.Text("Exploration des données");
        ImGui.Separator();
        ImGui.Spacing();

        // Filtres
        ImGui.Text("Filtres:");
        string searchText = "";
        ImGui.InputText("Recherche", ref searchText, 256);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Données
        ImGui.BeginChild("DataContent", new System.Numerics.Vector2(0, -30), true);

        for (int i = 0; i < 20; i++)
        {
            if (ImGui.TreeNode($"Élément {i + 1}"))
            {
                ImGui.Text($"ID: {i + 1}");
                ImGui.Text($"Timestamp: 2024-01-{(i % 30) + 1:D2}");
                ImGui.Text($"Valeur: {(i * 123) % 1000}");
                ImGui.TreePop();
            }
        }

        ImGui.EndChild();

        ImGui.Text($"Total: 20 éléments");

        ImGui.End();
    }

    private void DrawReportsWindow()
    {
        if (!_showReportsWindow) return;

        ImGui.Begin("Rapports", ref _showReportsWindow);

        ImGui.Text("Génération de rapports");
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.Button("Générer rapport mensuel"))
        {
            // Action de génération
        }

        ImGui.SameLine();

        if (ImGui.Button("Générer rapport annuel"))
        {
            // Action de génération
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.Text("Rapports disponibles:");

        ImGui.BeginChild("ReportsList");

        string[] reportTypes = { "Rapport mensuel - Janvier 2024", "Rapport mensuel - Février 2024",
                                "Rapport annuel - 2023", "Rapport personnalisé - Q4 2023" };

        foreach (var report in reportTypes)
        {
            if (ImGui.Selectable(report))
            {
                // Ouvrir le rapport
            }
        }

        ImGui.EndChild();

        ImGui.End();
    }

    private void DrawAnalyticsWindow()
    {
        if (!_showAnalyticsWindow) return;

        ImGui.Begin("Analytiques", ref _showAnalyticsWindow);

        ImGui.Text("Analyse des données");
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.Text("Métriques clés:");
        ImGui.Spacing();

        // Graphique simple (simulé avec des barres de progression)
        ImGui.Text("Utilisation CPU:");
        ImGui.ProgressBar(0.45f, new System.Numerics.Vector2(-1, 0), "45%");

        ImGui.Text("Utilisation Mémoire:");
        ImGui.ProgressBar(0.67f, new System.Numerics.Vector2(-1, 0), "67%");

        ImGui.Text("Stockage:");
        ImGui.ProgressBar(0.23f, new System.Numerics.Vector2(-1, 0), "23%");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.Text("Tendances:");
        ImGui.BulletText("Augmentation de 15% de l'utilisation");
        ImGui.BulletText("Performance stable sur 7 jours");
        ImGui.BulletText("3 alertes cette semaine");

        ImGui.End();
    }

    protected override void OnUnload()
    {
        base.OnUnload();

        _imguiController?.Dispose();
        _appContext.Shutdown();
    }
}
