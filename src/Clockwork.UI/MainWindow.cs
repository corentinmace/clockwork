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
        // Menu principal
        if (ImGui.BeginMainMenuBar())
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
                    // TODO: Afficher une fenêtre "À propos"
                }
                ImGui.EndMenu();
            }

            ImGui.EndMainMenuBar();
        }

        // Fenêtre de démonstration
        ShowDemoWindow();
    }

    private bool _showDemoWindow = true;
    private bool _showMetricsWindow = false;

    private void ShowDemoWindow()
    {
        ImGui.Begin("Bienvenue dans Clockwork", ref _showDemoWindow);

        ImGui.Text("Bienvenue dans votre nouvelle application!");
        ImGui.Spacing();

        ImGui.TextColored(new System.Numerics.Vector4(0.4f, 0.7f, 1.0f, 1.0f), "Architecture:");
        ImGui.BulletText(".NET 8");
        ImGui.BulletText("ImGui.NET pour l'interface");
        ImGui.BulletText("OpenTK pour OpenGL");
        ImGui.BulletText("Séparation Frontend/Backend");
        ImGui.Spacing();

        ImGui.Separator();
        ImGui.Spacing();

        ImGui.Text($"FPS: {ImGui.GetIO().Framerate:F1}");
        ImGui.Text($"Frame Time: {1000.0f / ImGui.GetIO().Framerate:F2} ms");
        ImGui.Spacing();

        if (ImGui.Checkbox("Afficher les métriques ImGui", ref _showMetricsWindow))
        {
            // Toggle metrics window
        }

        if (_showMetricsWindow)
        {
            ImGui.ShowMetricsWindow(ref _showMetricsWindow);
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.Button("Quitter l'application"))
        {
            _appContext.IsRunning = false;
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
