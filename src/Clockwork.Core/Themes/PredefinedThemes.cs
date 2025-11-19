using System.Numerics;

namespace Clockwork.Core.Themes;

/// <summary>
/// Provides predefined themes for Clockwork.
/// </summary>
public static class PredefinedThemes
{
    /// <summary>
    /// Modern dark theme with blue accents (default).
    /// </summary>
    public static Theme Dark()
    {
        return new Theme
        {
            Name = "Dark",
            Author = "Clockwork",
            IsReadOnly = true,

            // Style properties
            WindowRounding = 6.0f,
            FrameRounding = 3.0f,
            GrabRounding = 3.0f,
            TabRounding = 3.0f,
            WindowPadding = new Vector2(10, 10),
            FramePadding = new Vector2(8, 4),
            ItemSpacing = new Vector2(8, 4),

            // Main colors
            WindowBg = new Vector4(0.11f, 0.11f, 0.11f, 0.94f),
            ChildBg = new Vector4(0.15f, 0.15f, 0.15f, 1.00f),
            PopupBg = new Vector4(0.11f, 0.11f, 0.11f, 0.94f),
            Border = new Vector4(0.25f, 0.25f, 0.27f, 0.50f),
            BorderShadow = new Vector4(0.00f, 0.00f, 0.00f, 0.00f),

            // Frame colors
            FrameBg = new Vector4(0.20f, 0.21f, 0.22f, 1.00f),
            FrameBgHovered = new Vector4(0.30f, 0.31f, 0.32f, 1.00f),
            FrameBgActive = new Vector4(0.25f, 0.26f, 0.27f, 1.00f),

            // Title colors
            TitleBg = new Vector4(0.08f, 0.08f, 0.09f, 1.00f),
            TitleBgActive = new Vector4(0.15f, 0.15f, 0.16f, 1.00f),
            TitleBgCollapsed = new Vector4(0.08f, 0.08f, 0.09f, 0.75f),

            // Menu colors
            MenuBarBg = new Vector4(0.15f, 0.15f, 0.16f, 1.00f),

            // Scrollbar colors
            ScrollbarBg = new Vector4(0.10f, 0.10f, 0.10f, 0.53f),
            ScrollbarGrab = new Vector4(0.30f, 0.30f, 0.30f, 1.00f),
            ScrollbarGrabHovered = new Vector4(0.40f, 0.40f, 0.40f, 1.00f),
            ScrollbarGrabActive = new Vector4(0.50f, 0.50f, 0.50f, 1.00f),

            // Checkbox/Slider colors (blue accent)
            CheckMark = new Vector4(0.40f, 0.70f, 1.00f, 1.00f),
            SliderGrab = new Vector4(0.40f, 0.70f, 1.00f, 1.00f),
            SliderGrabActive = new Vector4(0.50f, 0.80f, 1.00f, 1.00f),

            // Button colors
            Button = new Vector4(0.25f, 0.26f, 0.27f, 1.00f),
            ButtonHovered = new Vector4(0.35f, 0.36f, 0.37f, 1.00f),
            ButtonActive = new Vector4(0.40f, 0.70f, 1.00f, 1.00f),

            // Header colors
            Header = new Vector4(0.25f, 0.26f, 0.27f, 1.00f),
            HeaderHovered = new Vector4(0.35f, 0.36f, 0.37f, 1.00f),
            HeaderActive = new Vector4(0.40f, 0.70f, 1.00f, 1.00f),

            // Separator colors
            Separator = new Vector4(0.25f, 0.25f, 0.27f, 0.50f),
            SeparatorHovered = new Vector4(0.40f, 0.70f, 1.00f, 0.78f),
            SeparatorActive = new Vector4(0.40f, 0.70f, 1.00f, 1.00f),

            // Resize grip colors
            ResizeGrip = new Vector4(0.25f, 0.25f, 0.27f, 0.20f),
            ResizeGripHovered = new Vector4(0.40f, 0.70f, 1.00f, 0.67f),
            ResizeGripActive = new Vector4(0.40f, 0.70f, 1.00f, 0.95f),

            // Tab colors
            Tab = new Vector4(0.15f, 0.15f, 0.16f, 1.00f),
            TabHovered = new Vector4(0.40f, 0.70f, 1.00f, 1.00f),
            TabActive = new Vector4(0.30f, 0.50f, 0.80f, 1.00f),
            TabUnfocused = new Vector4(0.12f, 0.12f, 0.13f, 1.00f),
            TabUnfocusedActive = new Vector4(0.20f, 0.20f, 0.21f, 1.00f),

            // Docking colors
            DockingPreview = new Vector4(0.40f, 0.70f, 1.00f, 0.70f),
            DockingEmptyBg = new Vector4(0.20f, 0.20f, 0.20f, 1.00f),

            // Plot colors
            PlotLines = new Vector4(0.61f, 0.61f, 0.61f, 1.00f),
            PlotLinesHovered = new Vector4(1.00f, 0.43f, 0.35f, 1.00f),
            PlotHistogram = new Vector4(0.90f, 0.70f, 0.00f, 1.00f),
            PlotHistogramHovered = new Vector4(1.00f, 0.60f, 0.00f, 1.00f),

            // Table colors
            TableHeaderBg = new Vector4(0.19f, 0.19f, 0.20f, 1.00f),
            TableBorderStrong = new Vector4(0.31f, 0.31f, 0.35f, 1.00f),
            TableBorderLight = new Vector4(0.23f, 0.23f, 0.25f, 1.00f),
            TableRowBg = new Vector4(0.00f, 0.00f, 0.00f, 0.00f),
            TableRowBgAlt = new Vector4(1.00f, 1.00f, 1.00f, 0.06f),

            // Text colors
            Text = new Vector4(1.00f, 1.00f, 1.00f, 1.00f),
            TextDisabled = new Vector4(0.50f, 0.50f, 0.50f, 1.00f),
            TextSelectedBg = new Vector4(0.40f, 0.70f, 1.00f, 0.35f),

            // Drag and drop colors
            DragDropTarget = new Vector4(1.00f, 1.00f, 0.00f, 0.90f),

            // Navigation colors
            NavHighlight = new Vector4(0.40f, 0.70f, 1.00f, 1.00f),
            NavWindowingHighlight = new Vector4(1.00f, 1.00f, 1.00f, 0.70f),
            NavWindowingDimBg = new Vector4(0.80f, 0.80f, 0.80f, 0.20f),

            // Modal colors
            ModalWindowDimBg = new Vector4(0.80f, 0.80f, 0.80f, 0.35f)
        };
    }

    /// <summary>
    /// Clean light theme with subtle colors.
    /// </summary>
    public static Theme Light()
    {
        return new Theme
        {
            Name = "Light",
            Author = "Clockwork",
            IsReadOnly = true,

            // Style properties
            WindowRounding = 6.0f,
            FrameRounding = 3.0f,
            GrabRounding = 3.0f,
            TabRounding = 3.0f,
            WindowPadding = new Vector2(10, 10),
            FramePadding = new Vector2(8, 4),
            ItemSpacing = new Vector2(8, 4),

            // Main colors (light backgrounds)
            WindowBg = new Vector4(0.94f, 0.94f, 0.94f, 1.00f),
            ChildBg = new Vector4(0.90f, 0.90f, 0.90f, 1.00f),
            PopupBg = new Vector4(1.00f, 1.00f, 1.00f, 0.98f),
            Border = new Vector4(0.70f, 0.70f, 0.70f, 0.50f),
            BorderShadow = new Vector4(0.00f, 0.00f, 0.00f, 0.00f),

            // Frame colors
            FrameBg = new Vector4(1.00f, 1.00f, 1.00f, 1.00f),
            FrameBgHovered = new Vector4(0.86f, 0.86f, 0.86f, 1.00f),
            FrameBgActive = new Vector4(0.92f, 0.92f, 0.92f, 1.00f),

            // Title colors
            TitleBg = new Vector4(0.96f, 0.96f, 0.96f, 1.00f),
            TitleBgActive = new Vector4(0.82f, 0.82f, 0.82f, 1.00f),
            TitleBgCollapsed = new Vector4(0.96f, 0.96f, 0.96f, 0.75f),

            // Menu colors
            MenuBarBg = new Vector4(0.86f, 0.86f, 0.86f, 1.00f),

            // Scrollbar colors
            ScrollbarBg = new Vector4(0.98f, 0.98f, 0.98f, 0.53f),
            ScrollbarGrab = new Vector4(0.69f, 0.69f, 0.69f, 0.80f),
            ScrollbarGrabHovered = new Vector4(0.49f, 0.49f, 0.49f, 0.80f),
            ScrollbarGrabActive = new Vector4(0.49f, 0.49f, 0.49f, 1.00f),

            // Checkbox/Slider colors (blue accent)
            CheckMark = new Vector4(0.26f, 0.59f, 0.98f, 1.00f),
            SliderGrab = new Vector4(0.26f, 0.59f, 0.98f, 0.78f),
            SliderGrabActive = new Vector4(0.26f, 0.59f, 0.98f, 1.00f),

            // Button colors
            Button = new Vector4(0.26f, 0.59f, 0.98f, 0.40f),
            ButtonHovered = new Vector4(0.26f, 0.59f, 0.98f, 1.00f),
            ButtonActive = new Vector4(0.06f, 0.53f, 0.98f, 1.00f),

            // Header colors
            Header = new Vector4(0.26f, 0.59f, 0.98f, 0.31f),
            HeaderHovered = new Vector4(0.26f, 0.59f, 0.98f, 0.80f),
            HeaderActive = new Vector4(0.26f, 0.59f, 0.98f, 1.00f),

            // Separator colors
            Separator = new Vector4(0.39f, 0.39f, 0.39f, 0.62f),
            SeparatorHovered = new Vector4(0.26f, 0.59f, 0.98f, 0.78f),
            SeparatorActive = new Vector4(0.26f, 0.59f, 0.98f, 1.00f),

            // Resize grip colors
            ResizeGrip = new Vector4(0.35f, 0.35f, 0.35f, 0.17f),
            ResizeGripHovered = new Vector4(0.26f, 0.59f, 0.98f, 0.67f),
            ResizeGripActive = new Vector4(0.26f, 0.59f, 0.98f, 0.95f),

            // Tab colors
            Tab = new Vector4(0.76f, 0.80f, 0.84f, 0.93f),
            TabHovered = new Vector4(0.26f, 0.59f, 0.98f, 0.80f),
            TabActive = new Vector4(0.60f, 0.73f, 0.88f, 1.00f),
            TabUnfocused = new Vector4(0.92f, 0.93f, 0.94f, 0.99f),
            TabUnfocusedActive = new Vector4(0.74f, 0.82f, 0.91f, 1.00f),

            // Docking colors
            DockingPreview = new Vector4(0.26f, 0.59f, 0.98f, 0.22f),
            DockingEmptyBg = new Vector4(0.20f, 0.20f, 0.20f, 1.00f),

            // Plot colors
            PlotLines = new Vector4(0.39f, 0.39f, 0.39f, 1.00f),
            PlotLinesHovered = new Vector4(1.00f, 0.43f, 0.35f, 1.00f),
            PlotHistogram = new Vector4(0.90f, 0.70f, 0.00f, 1.00f),
            PlotHistogramHovered = new Vector4(1.00f, 0.45f, 0.00f, 1.00f),

            // Table colors
            TableHeaderBg = new Vector4(0.78f, 0.87f, 0.98f, 1.00f),
            TableBorderStrong = new Vector4(0.57f, 0.57f, 0.64f, 1.00f),
            TableBorderLight = new Vector4(0.68f, 0.68f, 0.74f, 1.00f),
            TableRowBg = new Vector4(0.00f, 0.00f, 0.00f, 0.00f),
            TableRowBgAlt = new Vector4(0.30f, 0.30f, 0.30f, 0.09f),

            // Text colors
            Text = new Vector4(0.00f, 0.00f, 0.00f, 1.00f),
            TextDisabled = new Vector4(0.60f, 0.60f, 0.60f, 1.00f),
            TextSelectedBg = new Vector4(0.26f, 0.59f, 0.98f, 0.35f),

            // Drag and drop colors
            DragDropTarget = new Vector4(0.26f, 0.59f, 0.98f, 0.95f),

            // Navigation colors
            NavHighlight = new Vector4(0.26f, 0.59f, 0.98f, 0.80f),
            NavWindowingHighlight = new Vector4(0.70f, 0.70f, 0.70f, 0.70f),
            NavWindowingDimBg = new Vector4(0.20f, 0.20f, 0.20f, 0.20f),

            // Modal colors
            ModalWindowDimBg = new Vector4(0.20f, 0.20f, 0.20f, 0.35f)
        };
    }

    /// <summary>
    /// Nord theme - inspired by the Nord color palette (arctic, north-bluish).
    /// </summary>
    public static Theme Nord()
    {
        // Nord color palette
        // Polar Night (dark backgrounds)
        var nord0 = new Vector4(0.180f, 0.204f, 0.251f, 1.00f);  // #2E3440
        var nord1 = new Vector4(0.231f, 0.259f, 0.322f, 1.00f);  // #3B4252
        var nord2 = new Vector4(0.267f, 0.298f, 0.369f, 1.00f);  // #434C5E
        var nord3 = new Vector4(0.298f, 0.337f, 0.416f, 1.00f);  // #4C566A

        // Snow Storm (light foregrounds)
        var nord4 = new Vector4(0.847f, 0.871f, 0.914f, 1.00f);  // #D8DEE9
        var nord5 = new Vector4(0.898f, 0.914f, 0.941f, 1.00f);  // #E5E9F0
        var nord6 = new Vector4(0.925f, 0.937f, 0.957f, 1.00f);  // #ECEFF4

        // Frost (blue/cyan accents)
        var nord7 = new Vector4(0.557f, 0.737f, 0.733f, 1.00f);  // #8FBCBB
        var nord8 = new Vector4(0.537f, 0.753f, 0.816f, 1.00f);  // #88C0D0
        var nord9 = new Vector4(0.506f, 0.631f, 0.757f, 1.00f);  // #81A1C1
        var nord10 = new Vector4(0.365f, 0.506f, 0.675f, 1.00f); // #5E81AC

        // Aurora (colorful accents)
        var nord11 = new Vector4(0.749f, 0.380f, 0.416f, 1.00f); // #BF616A (red)
        var nord12 = new Vector4(0.816f, 0.529f, 0.439f, 1.00f); // #D08770 (orange)
        var nord13 = new Vector4(0.922f, 0.796f, 0.545f, 1.00f); // #EBCB8B (yellow)
        var nord14 = new Vector4(0.639f, 0.745f, 0.549f, 1.00f); // #A3BE8C (green)
        var nord15 = new Vector4(0.710f, 0.557f, 0.678f, 1.00f); // #B48EAD (purple)

        return new Theme
        {
            Name = "Nord",
            Author = "Clockwork (inspired by Nord theme)",
            IsReadOnly = true,

            // Style properties
            WindowRounding = 6.0f,
            FrameRounding = 3.0f,
            GrabRounding = 3.0f,
            TabRounding = 3.0f,
            WindowPadding = new Vector2(10, 10),
            FramePadding = new Vector2(8, 4),
            ItemSpacing = new Vector2(8, 4),

            // Main colors (Polar Night backgrounds)
            WindowBg = new Vector4(nord0.X, nord0.Y, nord0.Z, 0.94f),
            ChildBg = nord1,
            PopupBg = new Vector4(nord0.X, nord0.Y, nord0.Z, 0.94f),
            Border = new Vector4(nord3.X, nord3.Y, nord3.Z, 0.50f),
            BorderShadow = new Vector4(0.00f, 0.00f, 0.00f, 0.00f),

            // Frame colors
            FrameBg = nord2,
            FrameBgHovered = nord3,
            FrameBgActive = new Vector4(nord3.X * 1.1f, nord3.Y * 1.1f, nord3.Z * 1.1f, 1.00f),

            // Title colors
            TitleBg = nord0,
            TitleBgActive = nord1,
            TitleBgCollapsed = new Vector4(nord0.X, nord0.Y, nord0.Z, 0.75f),

            // Menu colors
            MenuBarBg = nord1,

            // Scrollbar colors
            ScrollbarBg = new Vector4(nord0.X, nord0.Y, nord0.Z, 0.53f),
            ScrollbarGrab = nord2,
            ScrollbarGrabHovered = nord3,
            ScrollbarGrabActive = nord10,

            // Checkbox/Slider colors (Frost blue accent)
            CheckMark = nord9,
            SliderGrab = nord9,
            SliderGrabActive = nord10,

            // Button colors
            Button = nord2,
            ButtonHovered = nord3,
            ButtonActive = nord9,

            // Header colors
            Header = nord2,
            HeaderHovered = nord3,
            HeaderActive = nord9,

            // Separator colors
            Separator = new Vector4(nord3.X, nord3.Y, nord3.Z, 0.50f),
            SeparatorHovered = new Vector4(nord9.X, nord9.Y, nord9.Z, 0.78f),
            SeparatorActive = nord9,

            // Resize grip colors
            ResizeGrip = new Vector4(nord3.X, nord3.Y, nord3.Z, 0.20f),
            ResizeGripHovered = new Vector4(nord9.X, nord9.Y, nord9.Z, 0.67f),
            ResizeGripActive = new Vector4(nord9.X, nord9.Y, nord9.Z, 0.95f),

            // Tab colors
            Tab = nord1,
            TabHovered = nord9,
            TabActive = nord10,
            TabUnfocused = nord0,
            TabUnfocusedActive = nord2,

            // Docking colors
            DockingPreview = new Vector4(nord9.X, nord9.Y, nord9.Z, 0.70f),
            DockingEmptyBg = nord0,

            // Plot colors (Aurora colors)
            PlotLines = nord4,
            PlotLinesHovered = nord11,
            PlotHistogram = nord13,
            PlotHistogramHovered = nord12,

            // Table colors
            TableHeaderBg = nord1,
            TableBorderStrong = nord3,
            TableBorderLight = nord2,
            TableRowBg = new Vector4(0.00f, 0.00f, 0.00f, 0.00f),
            TableRowBgAlt = new Vector4(nord6.X, nord6.Y, nord6.Z, 0.06f),

            // Text colors (Snow Storm)
            Text = nord6,
            TextDisabled = nord3,
            TextSelectedBg = new Vector4(nord9.X, nord9.Y, nord9.Z, 0.35f),

            // Drag and drop colors
            DragDropTarget = new Vector4(nord13.X, nord13.Y, nord13.Z, 0.90f),

            // Navigation colors
            NavHighlight = nord9,
            NavWindowingHighlight = new Vector4(nord6.X, nord6.Y, nord6.Z, 0.70f),
            NavWindowingDimBg = new Vector4(nord0.X, nord0.Y, nord0.Z, 0.60f),

            // Modal colors
            ModalWindowDimBg = new Vector4(nord0.X, nord0.Y, nord0.Z, 0.73f)
        };
    }

    /// <summary>
    /// Gets all predefined themes.
    /// </summary>
    public static List<Theme> GetAllPredefined()
    {
        return new List<Theme>
        {
            Dark(),
            Light(),
            Nord()
        };
    }
}
