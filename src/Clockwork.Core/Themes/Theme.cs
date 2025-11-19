using System.Numerics;

namespace Clockwork.Core.Themes;

/// <summary>
/// Represents a complete ImGui theme with all colors and style properties.
/// </summary>
public class Theme
{
    public string Name { get; set; } = "Unnamed Theme";
    public string Author { get; set; } = "Unknown";
    public bool IsReadOnly { get; set; } = false; // Predefined themes are read-only

    // Style properties
    public float WindowRounding { get; set; } = 6.0f;
    public float FrameRounding { get; set; } = 3.0f;
    public float GrabRounding { get; set; } = 3.0f;
    public float TabRounding { get; set; } = 3.0f;
    public Vector2 WindowPadding { get; set; } = new Vector2(10, 10);
    public Vector2 FramePadding { get; set; } = new Vector2(8, 4);
    public Vector2 ItemSpacing { get; set; } = new Vector2(8, 4);

    // Colors (stored as RGBA floats 0.0-1.0)
    // Main colors
    public Vector4 WindowBg { get; set; } = new Vector4(0.11f, 0.11f, 0.11f, 0.94f);
    public Vector4 ChildBg { get; set; } = new Vector4(0.15f, 0.15f, 0.15f, 1.00f);
    public Vector4 PopupBg { get; set; } = new Vector4(0.11f, 0.11f, 0.11f, 0.94f);
    public Vector4 Border { get; set; } = new Vector4(0.25f, 0.25f, 0.27f, 0.50f);
    public Vector4 BorderShadow { get; set; } = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);

    // Frame colors
    public Vector4 FrameBg { get; set; } = new Vector4(0.20f, 0.21f, 0.22f, 1.00f);
    public Vector4 FrameBgHovered { get; set; } = new Vector4(0.30f, 0.31f, 0.32f, 1.00f);
    public Vector4 FrameBgActive { get; set; } = new Vector4(0.25f, 0.26f, 0.27f, 1.00f);

    // Title colors
    public Vector4 TitleBg { get; set; } = new Vector4(0.08f, 0.08f, 0.09f, 1.00f);
    public Vector4 TitleBgActive { get; set; } = new Vector4(0.15f, 0.15f, 0.16f, 1.00f);
    public Vector4 TitleBgCollapsed { get; set; } = new Vector4(0.08f, 0.08f, 0.09f, 0.75f);

    // Menu colors
    public Vector4 MenuBarBg { get; set; } = new Vector4(0.15f, 0.15f, 0.16f, 1.00f);

    // Scrollbar colors
    public Vector4 ScrollbarBg { get; set; } = new Vector4(0.10f, 0.10f, 0.10f, 0.53f);
    public Vector4 ScrollbarGrab { get; set; } = new Vector4(0.30f, 0.30f, 0.30f, 1.00f);
    public Vector4 ScrollbarGrabHovered { get; set; } = new Vector4(0.40f, 0.40f, 0.40f, 1.00f);
    public Vector4 ScrollbarGrabActive { get; set; } = new Vector4(0.50f, 0.50f, 0.50f, 1.00f);

    // Checkbox/Slider colors
    public Vector4 CheckMark { get; set; } = new Vector4(0.40f, 0.70f, 1.00f, 1.00f);
    public Vector4 SliderGrab { get; set; } = new Vector4(0.40f, 0.70f, 1.00f, 1.00f);
    public Vector4 SliderGrabActive { get; set; } = new Vector4(0.50f, 0.80f, 1.00f, 1.00f);

    // Button colors
    public Vector4 Button { get; set; } = new Vector4(0.25f, 0.26f, 0.27f, 1.00f);
    public Vector4 ButtonHovered { get; set; } = new Vector4(0.35f, 0.36f, 0.37f, 1.00f);
    public Vector4 ButtonActive { get; set; } = new Vector4(0.40f, 0.70f, 1.00f, 1.00f);

    // Header colors
    public Vector4 Header { get; set; } = new Vector4(0.25f, 0.26f, 0.27f, 1.00f);
    public Vector4 HeaderHovered { get; set; } = new Vector4(0.35f, 0.36f, 0.37f, 1.00f);
    public Vector4 HeaderActive { get; set; } = new Vector4(0.40f, 0.70f, 1.00f, 1.00f);

    // Separator colors
    public Vector4 Separator { get; set; } = new Vector4(0.25f, 0.25f, 0.27f, 0.50f);
    public Vector4 SeparatorHovered { get; set; } = new Vector4(0.40f, 0.70f, 1.00f, 0.78f);
    public Vector4 SeparatorActive { get; set; } = new Vector4(0.40f, 0.70f, 1.00f, 1.00f);

    // Resize grip colors
    public Vector4 ResizeGrip { get; set; } = new Vector4(0.25f, 0.25f, 0.27f, 0.20f);
    public Vector4 ResizeGripHovered { get; set; } = new Vector4(0.40f, 0.70f, 1.00f, 0.67f);
    public Vector4 ResizeGripActive { get; set; } = new Vector4(0.40f, 0.70f, 1.00f, 0.95f);

    // Tab colors
    public Vector4 Tab { get; set; } = new Vector4(0.15f, 0.15f, 0.16f, 1.00f);
    public Vector4 TabHovered { get; set; } = new Vector4(0.40f, 0.70f, 1.00f, 1.00f);
    public Vector4 TabActive { get; set; } = new Vector4(0.30f, 0.50f, 0.80f, 1.00f);
    public Vector4 TabUnfocused { get; set; } = new Vector4(0.12f, 0.12f, 0.13f, 1.00f);
    public Vector4 TabUnfocusedActive { get; set; } = new Vector4(0.20f, 0.20f, 0.21f, 1.00f);

    // Docking colors
    public Vector4 DockingPreview { get; set; } = new Vector4(0.40f, 0.70f, 1.00f, 0.70f);
    public Vector4 DockingEmptyBg { get; set; } = new Vector4(0.20f, 0.20f, 0.20f, 1.00f);

    // Plot colors
    public Vector4 PlotLines { get; set; } = new Vector4(0.61f, 0.61f, 0.61f, 1.00f);
    public Vector4 PlotLinesHovered { get; set; } = new Vector4(1.00f, 0.43f, 0.35f, 1.00f);
    public Vector4 PlotHistogram { get; set; } = new Vector4(0.90f, 0.70f, 0.00f, 1.00f);
    public Vector4 PlotHistogramHovered { get; set; } = new Vector4(1.00f, 0.60f, 0.00f, 1.00f);

    // Table colors
    public Vector4 TableHeaderBg { get; set; } = new Vector4(0.19f, 0.19f, 0.20f, 1.00f);
    public Vector4 TableBorderStrong { get; set; } = new Vector4(0.31f, 0.31f, 0.35f, 1.00f);
    public Vector4 TableBorderLight { get; set; } = new Vector4(0.23f, 0.23f, 0.25f, 1.00f);
    public Vector4 TableRowBg { get; set; } = new Vector4(0.00f, 0.00f, 0.00f, 0.00f);
    public Vector4 TableRowBgAlt { get; set; } = new Vector4(1.00f, 1.00f, 1.00f, 0.06f);

    // Text colors
    public Vector4 Text { get; set; } = new Vector4(1.00f, 1.00f, 1.00f, 1.00f);
    public Vector4 TextDisabled { get; set; } = new Vector4(0.50f, 0.50f, 0.50f, 1.00f);
    public Vector4 TextSelectedBg { get; set; } = new Vector4(0.40f, 0.70f, 1.00f, 0.35f);

    // Drag and drop colors
    public Vector4 DragDropTarget { get; set; } = new Vector4(1.00f, 1.00f, 0.00f, 0.90f);

    // Navigation colors
    public Vector4 NavHighlight { get; set; } = new Vector4(0.40f, 0.70f, 1.00f, 1.00f);
    public Vector4 NavWindowingHighlight { get; set; } = new Vector4(1.00f, 1.00f, 1.00f, 0.70f);
    public Vector4 NavWindowingDimBg { get; set; } = new Vector4(0.80f, 0.80f, 0.80f, 0.20f);

    // Modal colors
    public Vector4 ModalWindowDimBg { get; set; } = new Vector4(0.80f, 0.80f, 0.80f, 0.35f);

    /// <summary>
    /// Creates a deep copy of this theme.
    /// </summary>
    public Theme Clone()
    {
        return new Theme
        {
            Name = Name + " (Copy)",
            Author = Author,
            IsReadOnly = false, // Clones are never read-only

            // Style properties
            WindowRounding = WindowRounding,
            FrameRounding = FrameRounding,
            GrabRounding = GrabRounding,
            TabRounding = TabRounding,
            WindowPadding = WindowPadding,
            FramePadding = FramePadding,
            ItemSpacing = ItemSpacing,

            // All colors
            WindowBg = WindowBg,
            ChildBg = ChildBg,
            PopupBg = PopupBg,
            Border = Border,
            BorderShadow = BorderShadow,
            FrameBg = FrameBg,
            FrameBgHovered = FrameBgHovered,
            FrameBgActive = FrameBgActive,
            TitleBg = TitleBg,
            TitleBgActive = TitleBgActive,
            TitleBgCollapsed = TitleBgCollapsed,
            MenuBarBg = MenuBarBg,
            ScrollbarBg = ScrollbarBg,
            ScrollbarGrab = ScrollbarGrab,
            ScrollbarGrabHovered = ScrollbarGrabHovered,
            ScrollbarGrabActive = ScrollbarGrabActive,
            CheckMark = CheckMark,
            SliderGrab = SliderGrab,
            SliderGrabActive = SliderGrabActive,
            Button = Button,
            ButtonHovered = ButtonHovered,
            ButtonActive = ButtonActive,
            Header = Header,
            HeaderHovered = HeaderHovered,
            HeaderActive = HeaderActive,
            Separator = Separator,
            SeparatorHovered = SeparatorHovered,
            SeparatorActive = SeparatorActive,
            ResizeGrip = ResizeGrip,
            ResizeGripHovered = ResizeGripHovered,
            ResizeGripActive = ResizeGripActive,
            Tab = Tab,
            TabHovered = TabHovered,
            TabActive = TabActive,
            TabUnfocused = TabUnfocused,
            TabUnfocusedActive = TabUnfocusedActive,
            DockingPreview = DockingPreview,
            DockingEmptyBg = DockingEmptyBg,
            PlotLines = PlotLines,
            PlotLinesHovered = PlotLinesHovered,
            PlotHistogram = PlotHistogram,
            PlotHistogramHovered = PlotHistogramHovered,
            TableHeaderBg = TableHeaderBg,
            TableBorderStrong = TableBorderStrong,
            TableBorderLight = TableBorderLight,
            TableRowBg = TableRowBg,
            TableRowBgAlt = TableRowBgAlt,
            Text = Text,
            TextDisabled = TextDisabled,
            TextSelectedBg = TextSelectedBg,
            DragDropTarget = DragDropTarget,
            NavHighlight = NavHighlight,
            NavWindowingHighlight = NavWindowingHighlight,
            NavWindowingDimBg = NavWindowingDimBg,
            ModalWindowDimBg = ModalWindowDimBg
        };
    }
}
