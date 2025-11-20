using System;
using System.Runtime.InteropServices;

namespace Clockwork.UI;

/// <summary>
/// P/Invoke bindings for SDL2 native functions needed for OpenGL context management.
/// These are required for multi-viewport support as Veldrid's Sdl2Window doesn't expose them.
/// </summary>
internal static class SDL2Native
{
    private const string LibName = "SDL2";

    /// <summary>
    /// Get the SDL OpenGL context from a window
    /// </summary>
    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr SDL_GL_GetCurrentContext();

    /// <summary>
    /// Get the address of an OpenGL function
    /// </summary>
    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    public static extern IntPtr SDL_GL_GetProcAddress(string proc);

    /// <summary>
    /// Make an OpenGL context current on a window
    /// </summary>
    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int SDL_GL_MakeCurrent(IntPtr window, IntPtr context);

    /// <summary>
    /// Swap the OpenGL buffers for a window (present rendered content)
    /// </summary>
    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void SDL_GL_SwapWindow(IntPtr window);

    /// <summary>
    /// Delete an OpenGL context
    /// </summary>
    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void SDL_GL_DeleteContext(IntPtr context);

    /// <summary>
    /// Create an OpenGL context for a window
    /// </summary>
    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr SDL_GL_CreateContext(IntPtr window);

    /// <summary>
    /// Set the swap interval for the current OpenGL context (0 = no vsync, 1 = vsync)
    /// </summary>
    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int SDL_GL_SetSwapInterval(int interval);
}
