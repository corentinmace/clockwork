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

            if (ImGui.BeginMenu("Affichage"))
            {
                ImGui.MenuItem("Bienvenue", null, ref _showWelcomeWindow);
                ImGui.MenuItem("Propriétés", null, ref _showPropertiesWindow);
                ImGui.MenuItem("Console", null, ref _showConsoleWindow);
                ImGui.MenuItem("Hiérarchie", null, ref _showHierarchyWindow);
                ImGui.Separator();
                ImGui.MenuItem("Métriques ImGui", null, ref _showMetricsWindow);
                ImGui.EndMenu();
            }

            if (ImGui.BeginMenu("Aide"))
            {
                if (ImGui.MenuItem("À propos"))
                {
                    _showAboutWindow = true;
                }
                ImGui.EndMenu();
            }

            ImGui.EndMenuBar();
        }

        ImGui.End();

        // Dessiner toutes les fenêtres dockables
        DrawWelcomeWindow();
        DrawPropertiesWindow();
        DrawConsoleWindow();
        DrawHierarchyWindow();
        DrawAboutWindow();

        if (_showMetricsWindow)
        {
            ImGui.ShowMetricsWindow(ref _showMetricsWindow);
        }
    }

    // États des fenêtres
    private bool _showWelcomeWindow = true;
    private bool _showPropertiesWindow = true;
    private bool _showConsoleWindow = true;
    private bool _showHierarchyWindow = true;
    private bool _showMetricsWindow = false;
    private bool _showAboutWindow = false;

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

    protected override void OnUnload()
    {
        base.OnUnload();

        _imguiController?.Dispose();
        _appContext.Shutdown();
    }
}
