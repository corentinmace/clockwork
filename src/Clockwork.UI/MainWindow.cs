using Clockwork.Core;
using Clockwork.UI.Views;
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

    // Views
    private readonly DashboardView _dashboardView;
    private readonly WelcomeView _welcomeView;
    private readonly PropertiesView _propertiesView;
    private readonly ConsoleView _consoleView;
    private readonly HierarchyView _hierarchyView;
    private readonly AboutView _aboutView;
    private readonly SettingsView _settingsView;
    private readonly UserManagementView _userManagementView;
    private readonly DataViewView _dataViewView;
    private readonly ReportsView _reportsView;
    private readonly AnalyticsView _analyticsView;

    public MainWindow(ApplicationContext appContext, GameWindowSettings gameWindowSettings, NativeWindowSettings nativeWindowSettings)
        : base(gameWindowSettings, nativeWindowSettings)
    {
        _appContext = appContext;

        // Initialize views
        _dashboardView = new DashboardView(_appContext) { IsVisible = true };
        _welcomeView = new WelcomeView(_appContext);
        _propertiesView = new PropertiesView(_appContext);
        _consoleView = new ConsoleView(_appContext);
        _hierarchyView = new HierarchyView(_appContext);
        _aboutView = new AboutView(_appContext);
        _settingsView = new SettingsView(_appContext);
        _userManagementView = new UserManagementView(_appContext);
        _dataViewView = new DataViewView(_appContext);
        _reportsView = new ReportsView(_appContext);
        _analyticsView = new AnalyticsView(_appContext);
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

            // Initialiser le layout du dockspace une seule fois
            if (!_dockspaceInitialized)
            {
                _dockspaceInitialized = true;

                // Réinitialiser le dockspace pour être sûr de partir d'une base propre
                ImGui.DockBuilderRemoveNode(dockspaceId);
                ImGui.DockBuilderAddNode(dockspaceId, ImGuiDockNodeFlags.DockSpace);
                ImGui.DockBuilderSetNodeSize(dockspaceId, viewport.WorkSize);

                // Créer un split: gauche pour la sidebar, droite pour le contenu
                float sidebarWidth = _isSidebarCollapsed ? 50 : 250;
                uint dockLeftId = 0;
                uint dockRightId = 0;
                ImGui.DockBuilderSplitNode(dockspaceId, ImGuiDir.Left, sidebarWidth / viewport.WorkSize.X, ref dockLeftId, ref dockRightId);

                // Docker la sidebar à gauche
                ImGui.DockBuilderDockWindow("Navigation", dockLeftId);

                // Finaliser le layout
                ImGui.DockBuilderFinish(dockspaceId);
            }

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
                    _aboutView.IsVisible = true;
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

        // Dessiner toutes les vues
        _welcomeView.Draw();
        _propertiesView.Draw();
        _consoleView.Draw();
        _hierarchyView.Draw();
        _aboutView.Draw();
        _dashboardView.Draw();
        _settingsView.Draw();
        _userManagementView.Draw();
        _dataViewView.Draw();
        _reportsView.Draw();
        _analyticsView.Draw();

        if (_showMetricsWindow)
        {
            ImGui.ShowMetricsWindow(ref _showMetricsWindow);
        }
    }

    private void DrawSidebar()
    {
        // La sidebar est maintenant une fenêtre dockable normale
        ImGuiWindowFlags sidebarFlags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse;

        ImGui.Begin("Navigation", sidebarFlags);

        // Bouton pour rétracter/déplier
        if (ImGui.Button(_isSidebarCollapsed ? "»" : "«", new System.Numerics.Vector2(-1, 30)))
        {
            _isSidebarCollapsed = !_isSidebarCollapsed;
        }

        if (!_isSidebarCollapsed)
        {
            ImGui.Spacing();
            ImGui.TextColored(new System.Numerics.Vector4(0.4f, 0.7f, 1.0f, 1.0f), "NAVIGATION");
            ImGui.Separator();
            ImGui.Spacing();
        }

        if (_isSidebarCollapsed)
        {
            // Mode rétracté - afficher juste des icônes
            ImGui.Spacing();
            if (ImGui.Button("📊", new System.Numerics.Vector2(-1, 40))) _dashboardView.IsVisible = !_dashboardView.IsVisible;
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Tableau de bord");

            if (ImGui.Button("👋", new System.Numerics.Vector2(-1, 40))) _welcomeView.IsVisible = !_welcomeView.IsVisible;
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Bienvenue");

            if (ImGui.Button("📝", new System.Numerics.Vector2(-1, 40))) _propertiesView.IsVisible = !_propertiesView.IsVisible;
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Propriétés");

            if (ImGui.Button("💻", new System.Numerics.Vector2(-1, 40))) _consoleView.IsVisible = !_consoleView.IsVisible;
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Console");

            if (ImGui.Button("🌳", new System.Numerics.Vector2(-1, 40))) _hierarchyView.IsVisible = !_hierarchyView.IsVisible;
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Hiérarchie");

            if (ImGui.Button("📁", new System.Numerics.Vector2(-1, 40))) _dataViewView.IsVisible = !_dataViewView.IsVisible;
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Vue des données");

            if (ImGui.Button("📄", new System.Numerics.Vector2(-1, 40))) _reportsView.IsVisible = !_reportsView.IsVisible;
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Rapports");

            if (ImGui.Button("📈", new System.Numerics.Vector2(-1, 40))) _analyticsView.IsVisible = !_analyticsView.IsVisible;
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Analytiques");

            if (ImGui.Button("⚙️", new System.Numerics.Vector2(-1, 40))) _settingsView.IsVisible = !_settingsView.IsVisible;
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Paramètres");

            if (ImGui.Button("👥", new System.Numerics.Vector2(-1, 40))) _userManagementView.IsVisible = !_userManagementView.IsVisible;
            if (ImGui.IsItemHovered()) ImGui.SetTooltip("Gestion utilisateurs");
        }
        else
        {
            // Mode normal - afficher le menu complet
            // Section: Général
            if (ImGui.CollapsingHeader("Général", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGui.Selectable("  📊 Tableau de bord", _dashboardView.IsVisible))
                {
                    _dashboardView.IsVisible = !_dashboardView.IsVisible;
                }
                if (ImGui.Selectable("  👋 Bienvenue", _welcomeView.IsVisible))
                {
                    _welcomeView.IsVisible = !_welcomeView.IsVisible;
                }
                if (ImGui.Selectable("  📝 Propriétés", _propertiesView.IsVisible))
                {
                    _propertiesView.IsVisible = !_propertiesView.IsVisible;
                }
            }

            ImGui.Spacing();

            // Section: Développement
            if (ImGui.CollapsingHeader("Développement", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGui.Selectable("  💻 Console", _consoleView.IsVisible))
                {
                    _consoleView.IsVisible = !_consoleView.IsVisible;
                }
                if (ImGui.Selectable("  🌳 Hiérarchie", _hierarchyView.IsVisible))
                {
                    _hierarchyView.IsVisible = !_hierarchyView.IsVisible;
                }
            }

            ImGui.Spacing();

            // Section: Données
            if (ImGui.CollapsingHeader("Données"))
            {
                if (ImGui.Selectable("  📁 Vue des données", _dataViewView.IsVisible))
                {
                    _dataViewView.IsVisible = !_dataViewView.IsVisible;
                }
                if (ImGui.Selectable("  📄 Rapports", _reportsView.IsVisible))
                {
                    _reportsView.IsVisible = !_reportsView.IsVisible;
                }
                if (ImGui.Selectable("  📈 Analytiques", _analyticsView.IsVisible))
                {
                    _analyticsView.IsVisible = !_analyticsView.IsVisible;
                }
            }

            ImGui.Spacing();

            // Section: Système
            if (ImGui.CollapsingHeader("Système"))
            {
                if (ImGui.Selectable("  ⚙️ Paramètres", _settingsView.IsVisible))
                {
                    _settingsView.IsVisible = !_settingsView.IsVisible;
                }
                if (ImGui.Selectable("  👥 Gestion utilisateurs", _userManagementView.IsVisible))
                {
                    _userManagementView.IsVisible = !_userManagementView.IsVisible;
                }
            }
        }

        ImGui.End();
    }

    // État de la sidebar et metrics
    private bool _isSidebarCollapsed = false;
    private bool _showMetricsWindow = false;
    private bool _dockspaceInitialized = false;

    protected override void OnUnload()
    {
        base.OnUnload();

        _imguiController?.Dispose();
        _appContext.Shutdown();
    }
}
