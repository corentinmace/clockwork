using System.Runtime.CompilerServices;
using ImGuiNET;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Silk.NET.Input;

namespace Clockwork.UI;

/// <summary>
/// Contrôleur ImGui qui gère le rendu et l'intégration avec OpenGL/Silk.NET.
/// </summary>
public class ImGuiController : IDisposable
{
    private readonly GL _gl;
    private readonly IWindow _window;
    private readonly IInputContext _inputContext;

    private bool _disposed;
    private uint _vertexArray;
    private uint _vertexBuffer;
    private uint _elementBuffer;
    private int _vertexBufferSize;
    private int _elementBufferSize;
    private uint _fontTexture;
    private uint _shader;
    private int _shaderFontTexture;
    private int _shaderProjectionMatrix;
    private int _windowWidth;
    private int _windowHeight;
    private System.Numerics.Vector2 _scrollDelta;

    // Input state
    private IKeyboard? _keyboard;
    private IMouse? _mouse;
    private readonly List<char> _pressedChars = new();

    /// <summary>
    /// Crée une nouvelle instance du contrôleur ImGui.
    /// </summary>
    public ImGuiController(GL gl, IWindow window, int width, int height)
    {
        _gl = gl;
        _window = window;
        _windowWidth = width;
        _windowHeight = height;

        // Get input context
        _inputContext = window.CreateInput();

        // Set up input callbacks
        foreach (var keyboard in _inputContext.Keyboards)
        {
            _keyboard = keyboard;
            _keyboard.KeyChar += OnKeyChar;
        }

        foreach (var mouse in _inputContext.Mice)
        {
            _mouse = mouse;
            _mouse.Scroll += OnMouseScroll;
        }

        IntPtr context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);

        var io = ImGui.GetIO();
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
        io.BackendFlags |= ImGuiBackendFlags.PlatformHasViewports;
        io.BackendFlags |= ImGuiBackendFlags.RendererHasViewports;
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;

        // Enable multi-viewport support with Silk.NET
        io.ConfigFlags |= ImGuiConfigFlags.ViewportsEnable;

        CreateDeviceResources();
        SetupPlatformCallbacks();

        SetPerFrameImGuiData(1f / 60f);
    }

    private void OnKeyChar(IKeyboard keyboard, char c)
    {
        _pressedChars.Add(c);
    }

    private void OnMouseScroll(IMouse mouse, ScrollWheel scrollWheel)
    {
        _scrollDelta = new System.Numerics.Vector2(scrollWheel.X, scrollWheel.Y);
    }

    private unsafe void CreateDeviceResources()
    {
        _vertexBufferSize = 10000;
        _elementBufferSize = 2000;

        // Vertex Array
        _vertexArray = _gl.GenVertexArray();
        _gl.BindVertexArray(_vertexArray);

        // Vertex Buffer
        _vertexBuffer = _gl.GenBuffer();
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vertexBuffer);
        _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)_vertexBufferSize, null, BufferUsageARB.DynamicDraw);

        // Element Buffer
        _elementBuffer = _gl.GenBuffer();
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _elementBuffer);
        _gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)_elementBufferSize, null, BufferUsageARB.DynamicDraw);

        RecreateFontDeviceTexture();
        CreateShader();

        _gl.BindVertexArray(_vertexArray);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vertexBuffer);

        // Position
        _gl.EnableVertexAttribArray(0);
        _gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, (uint)Unsafe.SizeOf<ImDrawVert>(), null);

        // UV
        _gl.EnableVertexAttribArray(1);
        _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, (uint)Unsafe.SizeOf<ImDrawVert>(), (void*)8);

        // Color
        _gl.EnableVertexAttribArray(2);
        _gl.VertexAttribPointer(2, 4, VertexAttribPointerType.UnsignedByte, true, (uint)Unsafe.SizeOf<ImDrawVert>(), (void*)16);

        _gl.BindVertexArray(0);
    }

    private void CreateShader()
    {
        const string vertexShader = @"#version 330 core
layout (location = 0) in vec2 Position;
layout (location = 1) in vec2 UV;
layout (location = 2) in vec4 Color;

uniform mat4 ProjMtx;

out vec2 Frag_UV;
out vec4 Frag_Color;

void main()
{
    Frag_UV = UV;
    Frag_Color = Color;
    gl_Position = ProjMtx * vec4(Position.xy, 0, 1);
}";

        const string fragmentShader = @"#version 330 core
in vec2 Frag_UV;
in vec4 Frag_Color;

uniform sampler2D Texture;

layout (location = 0) out vec4 Out_Color;

void main()
{
    Out_Color = Frag_Color * texture(Texture, Frag_UV.st);
}";

        _shader = CreateProgram("ImGui", vertexShader, fragmentShader);
        _shaderProjectionMatrix = _gl.GetUniformLocation(_shader, "ProjMtx");
        _shaderFontTexture = _gl.GetUniformLocation(_shader, "Texture");
    }

    private uint CreateProgram(string name, string vertexSource, string fragmentSource)
    {
        uint program = _gl.CreateProgram();
        uint vertex = CompileShader(name, ShaderType.VertexShader, vertexSource);
        uint fragment = CompileShader(name, ShaderType.FragmentShader, fragmentSource);

        _gl.AttachShader(program, vertex);
        _gl.AttachShader(program, fragment);

        _gl.LinkProgram(program);

        _gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out int success);
        if (success == 0)
        {
            string info = _gl.GetProgramInfoLog(program);
            Console.WriteLine($"GL.LinkProgram had info log [{name}]:\n{info}");
        }

        _gl.DetachShader(program, vertex);
        _gl.DetachShader(program, fragment);

        _gl.DeleteShader(vertex);
        _gl.DeleteShader(fragment);

        return program;
    }

    private uint CompileShader(string name, ShaderType type, string source)
    {
        uint shader = _gl.CreateShader(type);
        _gl.ShaderSource(shader, source);
        _gl.CompileShader(shader);

        _gl.GetShader(shader, ShaderParameterName.CompileStatus, out int success);
        if (success == 0)
        {
            string info = _gl.GetShaderInfoLog(shader);
            Console.WriteLine($"GL.CompileShader for shader '{name}' [{type}] had info log:\n{info}");
        }

        return shader;
    }

    private unsafe void RecreateFontDeviceTexture()
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out int bytesPerPixel);

        _fontTexture = _gl.GenTexture();
        _gl.BindTexture(TextureTarget.Texture2D, _fontTexture);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        _gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, (uint)width, (uint)height, 0,
            PixelFormat.Rgba, PixelType.UnsignedByte, (void*)pixels);

        io.Fonts.SetTexID((IntPtr)_fontTexture);
        io.Fonts.ClearTexData();
    }

    /// <summary>
    /// Redimensionne le contrôleur ImGui.
    /// </summary>
    public void WindowResized(int width, int height)
    {
        _windowWidth = width;
        _windowHeight = height;
    }

    /// <summary>
    /// Met à jour ImGui pour la frame actuelle.
    /// </summary>
    public void Update(float deltaTime)
    {
        SetPerFrameImGuiData(deltaTime);
        UpdateImGuiInput();

        ImGui.NewFrame();
    }

    private void SetPerFrameImGuiData(float deltaSeconds)
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.DisplaySize = new System.Numerics.Vector2(_windowWidth, _windowHeight);

        if (_windowWidth > 0 && _windowHeight > 0)
        {
            io.DisplayFramebufferScale = System.Numerics.Vector2.One;
        }

        io.DeltaTime = deltaSeconds;
    }

    private void UpdateImGuiInput()
    {
        ImGuiIOPtr io = ImGui.GetIO();

        if (_mouse != null)
        {
            io.MouseDown[0] = _mouse.IsButtonPressed(MouseButton.Left);
            io.MouseDown[1] = _mouse.IsButtonPressed(MouseButton.Right);
            io.MouseDown[2] = _mouse.IsButtonPressed(MouseButton.Middle);

            io.MousePos = new System.Numerics.Vector2(_mouse.Position.X, _mouse.Position.Y);

            // Mouse wheel scroll
            io.MouseWheel = _scrollDelta.Y;
            io.MouseWheelH = _scrollDelta.X;
            _scrollDelta = System.Numerics.Vector2.Zero;
        }

        if (_keyboard != null)
        {
            io.AddKeyEvent(ImGuiKey.ModCtrl, _keyboard.IsKeyPressed(Key.ControlLeft) || _keyboard.IsKeyPressed(Key.ControlRight));
            io.AddKeyEvent(ImGuiKey.ModAlt, _keyboard.IsKeyPressed(Key.AltLeft) || _keyboard.IsKeyPressed(Key.AltRight));
            io.AddKeyEvent(ImGuiKey.ModShift, _keyboard.IsKeyPressed(Key.ShiftLeft) || _keyboard.IsKeyPressed(Key.ShiftRight));
            io.AddKeyEvent(ImGuiKey.ModSuper, _keyboard.IsKeyPressed(Key.SuperLeft) || _keyboard.IsKeyPressed(Key.SuperRight));

            // Map keyboard keys to ImGui keys
            foreach (Key key in Enum.GetValues(typeof(Key)))
            {
                if (key == Key.Unknown)
                    continue;

                ImGuiKey imguiKey = TranslateKey(key);
                if (imguiKey != ImGuiKey.None)
                {
                    io.AddKeyEvent(imguiKey, _keyboard.IsKeyPressed(key));
                }
            }

            // Handle text input
            foreach (char c in _pressedChars)
            {
                io.AddInputCharacter(c);
            }
            _pressedChars.Clear();
        }
    }

    private static ImGuiKey TranslateKey(Key key)
    {
        return key switch
        {
            Key.Tab => ImGuiKey.Tab,
            Key.Left => ImGuiKey.LeftArrow,
            Key.Right => ImGuiKey.RightArrow,
            Key.Up => ImGuiKey.UpArrow,
            Key.Down => ImGuiKey.DownArrow,
            Key.PageUp => ImGuiKey.PageUp,
            Key.PageDown => ImGuiKey.PageDown,
            Key.Home => ImGuiKey.Home,
            Key.End => ImGuiKey.End,
            Key.Insert => ImGuiKey.Insert,
            Key.Delete => ImGuiKey.Delete,
            Key.Backspace => ImGuiKey.Backspace,
            Key.Space => ImGuiKey.Space,
            Key.Enter => ImGuiKey.Enter,
            Key.Escape => ImGuiKey.Escape,
            Key.Apostrophe => ImGuiKey.Apostrophe,
            Key.Comma => ImGuiKey.Comma,
            Key.Minus => ImGuiKey.Minus,
            Key.Period => ImGuiKey.Period,
            Key.Slash => ImGuiKey.Slash,
            Key.Semicolon => ImGuiKey.Semicolon,
            Key.Equal => ImGuiKey.Equal,
            Key.LeftBracket => ImGuiKey.LeftBracket,
            Key.BackSlash => ImGuiKey.Backslash,
            Key.RightBracket => ImGuiKey.RightBracket,
            Key.GraveAccent => ImGuiKey.GraveAccent,
            Key.CapsLock => ImGuiKey.CapsLock,
            Key.ScrollLock => ImGuiKey.ScrollLock,
            Key.NumLock => ImGuiKey.NumLock,
            Key.PrintScreen => ImGuiKey.PrintScreen,
            Key.Pause => ImGuiKey.Pause,
            Key.Keypad0 => ImGuiKey.Keypad0,
            Key.Keypad1 => ImGuiKey.Keypad1,
            Key.Keypad2 => ImGuiKey.Keypad2,
            Key.Keypad3 => ImGuiKey.Keypad3,
            Key.Keypad4 => ImGuiKey.Keypad4,
            Key.Keypad5 => ImGuiKey.Keypad5,
            Key.Keypad6 => ImGuiKey.Keypad6,
            Key.Keypad7 => ImGuiKey.Keypad7,
            Key.Keypad8 => ImGuiKey.Keypad8,
            Key.Keypad9 => ImGuiKey.Keypad9,
            Key.KeypadDecimal => ImGuiKey.KeypadDecimal,
            Key.KeypadDivide => ImGuiKey.KeypadDivide,
            Key.KeypadMultiply => ImGuiKey.KeypadMultiply,
            Key.KeypadSubtract => ImGuiKey.KeypadSubtract,
            Key.KeypadAdd => ImGuiKey.KeypadAdd,
            Key.KeypadEnter => ImGuiKey.KeypadEnter,
            Key.KeypadEqual => ImGuiKey.KeypadEqual,
            Key.ShiftLeft => ImGuiKey.LeftShift,
            Key.ControlLeft => ImGuiKey.LeftCtrl,
            Key.AltLeft => ImGuiKey.LeftAlt,
            Key.SuperLeft => ImGuiKey.LeftSuper,
            Key.ShiftRight => ImGuiKey.RightShift,
            Key.ControlRight => ImGuiKey.RightCtrl,
            Key.AltRight => ImGuiKey.RightAlt,
            Key.SuperRight => ImGuiKey.RightSuper,
            Key.Menu => ImGuiKey.Menu,
            Key.Number0 => ImGuiKey._0,
            Key.Number1 => ImGuiKey._1,
            Key.Number2 => ImGuiKey._2,
            Key.Number3 => ImGuiKey._3,
            Key.Number4 => ImGuiKey._4,
            Key.Number5 => ImGuiKey._5,
            Key.Number6 => ImGuiKey._6,
            Key.Number7 => ImGuiKey._7,
            Key.Number8 => ImGuiKey._8,
            Key.Number9 => ImGuiKey._9,
            Key.A => ImGuiKey.A,
            Key.B => ImGuiKey.B,
            Key.C => ImGuiKey.C,
            Key.D => ImGuiKey.D,
            Key.E => ImGuiKey.E,
            Key.F => ImGuiKey.F,
            Key.G => ImGuiKey.G,
            Key.H => ImGuiKey.H,
            Key.I => ImGuiKey.I,
            Key.J => ImGuiKey.J,
            Key.K => ImGuiKey.K,
            Key.L => ImGuiKey.L,
            Key.M => ImGuiKey.M,
            Key.N => ImGuiKey.N,
            Key.O => ImGuiKey.O,
            Key.P => ImGuiKey.P,
            Key.Q => ImGuiKey.Q,
            Key.R => ImGuiKey.R,
            Key.S => ImGuiKey.S,
            Key.T => ImGuiKey.T,
            Key.U => ImGuiKey.U,
            Key.V => ImGuiKey.V,
            Key.W => ImGuiKey.W,
            Key.X => ImGuiKey.X,
            Key.Y => ImGuiKey.Y,
            Key.Z => ImGuiKey.Z,
            Key.F1 => ImGuiKey.F1,
            Key.F2 => ImGuiKey.F2,
            Key.F3 => ImGuiKey.F3,
            Key.F4 => ImGuiKey.F4,
            Key.F5 => ImGuiKey.F5,
            Key.F6 => ImGuiKey.F6,
            Key.F7 => ImGuiKey.F7,
            Key.F8 => ImGuiKey.F8,
            Key.F9 => ImGuiKey.F9,
            Key.F10 => ImGuiKey.F10,
            Key.F11 => ImGuiKey.F11,
            Key.F12 => ImGuiKey.F12,
            _ => ImGuiKey.None
        };
    }

    /// <summary>
    /// Effectue le rendu d'ImGui.
    /// </summary>
    public void Render()
    {
        ImGui.Render();
        RenderImDrawData(ImGui.GetDrawData());

        // Render additional viewports for multi-viewport support
        var io = ImGui.GetIO();
        if ((io.ConfigFlags & ImGuiConfigFlags.ViewportsEnable) != 0)
        {
            ImGui.UpdatePlatformWindows();
            ImGui.RenderPlatformWindowsDefault();
        }
    }

    private unsafe void RenderImDrawData(ImDrawDataPtr drawData)
    {
        if (drawData.CmdListsCount == 0)
            return;

        // Backup GL state
        _gl.GetInteger(GetPName.ActiveTexture, out int prevActiveTexture);
        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.GetInteger(GetPName.CurrentProgram, out int prevProgram);
        _gl.GetInteger(GetPName.TextureBinding2D, out int prevTexture);
        _gl.GetInteger(GetPName.ArrayBufferBinding, out int prevArrayBuffer);
        _gl.GetInteger(GetPName.VertexArrayBinding, out int prevVertexArray);

        Span<int> prevScissorBox = stackalloc int[4];
        _gl.GetInteger(GetPName.ScissorBox, prevScissorBox);

        Span<int> prevBlendSrcRgb = stackalloc int[1];
        _gl.GetInteger(GetPName.BlendSrcRgb, prevBlendSrcRgb);
        Span<int> prevBlendDstRgb = stackalloc int[1];
        _gl.GetInteger(GetPName.BlendDstRgb, prevBlendDstRgb);
        Span<int> prevBlendSrcAlpha = stackalloc int[1];
        _gl.GetInteger(GetPName.BlendSrcAlpha, prevBlendSrcAlpha);
        Span<int> prevBlendDstAlpha = stackalloc int[1];
        _gl.GetInteger(GetPName.BlendDstAlpha, prevBlendDstAlpha);
        Span<int> prevBlendEquationRgb = stackalloc int[1];
        _gl.GetInteger(GetPName.BlendEquationRgb, prevBlendEquationRgb);
        Span<int> prevBlendEquationAlpha = stackalloc int[1];
        _gl.GetInteger(GetPName.BlendEquationAlpha, prevBlendEquationAlpha);

        bool prevEnableBlend = _gl.IsEnabled(EnableCap.Blend);
        bool prevEnableCullFace = _gl.IsEnabled(EnableCap.CullFace);
        bool prevEnableDepthTest = _gl.IsEnabled(EnableCap.DepthTest);
        bool prevEnableScissorTest = _gl.IsEnabled(EnableCap.ScissorTest);

        // Setup render state
        _gl.Enable(EnableCap.Blend);
        _gl.Enable(EnableCap.ScissorTest);
        _gl.BlendEquation(BlendEquationModeEXT.FuncAdd);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        _gl.Disable(EnableCap.CullFace);
        _gl.Disable(EnableCap.DepthTest);

        // Setup viewport and projection matrix
        _gl.Viewport(0, 0, (uint)_windowWidth, (uint)_windowHeight);
        float L = drawData.DisplayPos.X;
        float R = drawData.DisplayPos.X + drawData.DisplaySize.X;
        float T = drawData.DisplayPos.Y;
        float B = drawData.DisplayPos.Y + drawData.DisplaySize.Y;

        Span<float> orthoProjection = stackalloc float[]
        {
            2.0f / (R - L), 0.0f, 0.0f, 0.0f,
            0.0f, 2.0f / (T - B), 0.0f, 0.0f,
            0.0f, 0.0f, -1.0f, 0.0f,
            (R + L) / (L - R), (T + B) / (B - T), 0.0f, 1.0f,
        };

        _gl.UseProgram(_shader);
        _gl.UniformMatrix4(_shaderProjectionMatrix, 1, false, orthoProjection);
        _gl.Uniform1(_shaderFontTexture, 0);

        _gl.BindVertexArray(_vertexArray);

        drawData.ScaleClipRects(ImGui.GetIO().DisplayFramebufferScale);

        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            ImDrawListPtr cmdList = drawData.CmdLists[n];

            int vertexSize = cmdList.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>();
            if (vertexSize > _vertexBufferSize)
            {
                _vertexBufferSize = (int)(vertexSize * 1.5f);
                _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vertexBuffer);
                _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)_vertexBufferSize, null, BufferUsageARB.DynamicDraw);
            }

            int indexSize = cmdList.IdxBuffer.Size * sizeof(ushort);
            if (indexSize > _elementBufferSize)
            {
                _elementBufferSize = (int)(indexSize * 1.5f);
                _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _elementBuffer);
                _gl.BufferData(BufferTargetARB.ElementArrayBuffer, (nuint)_elementBufferSize, null, BufferUsageARB.DynamicDraw);
            }

            _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vertexBuffer);
            _gl.BufferSubData(BufferTargetARB.ArrayBuffer, 0, (nuint)vertexSize, (void*)cmdList.VtxBuffer.Data);

            _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _elementBuffer);
            _gl.BufferSubData(BufferTargetARB.ElementArrayBuffer, 0, (nuint)indexSize, (void*)cmdList.IdxBuffer.Data);

            for (int cmdI = 0; cmdI < cmdList.CmdBuffer.Size; cmdI++)
            {
                ImDrawCmdPtr pcmd = cmdList.CmdBuffer[cmdI];

                if (pcmd.UserCallback != IntPtr.Zero)
                {
                    throw new NotImplementedException("User callbacks not supported");
                }

                _gl.ActiveTexture(TextureUnit.Texture0);
                _gl.BindTexture(TextureTarget.Texture2D, (uint)pcmd.TextureId);

                var clip = pcmd.ClipRect;
                _gl.Scissor((int)clip.X, _windowHeight - (int)clip.W, (uint)(clip.Z - clip.X), (uint)(clip.W - clip.Y));

                if ((ImGui.GetIO().BackendFlags & ImGuiBackendFlags.RendererHasVtxOffset) != 0)
                {
                    _gl.DrawElementsBaseVertex(PrimitiveType.Triangles, (uint)pcmd.ElemCount, DrawElementsType.UnsignedShort,
                        (void*)(pcmd.IdxOffset * sizeof(ushort)), (int)pcmd.VtxOffset);
                }
                else
                {
                    _gl.DrawElements(PrimitiveType.Triangles, (uint)pcmd.ElemCount, DrawElementsType.UnsignedShort,
                        (void*)(pcmd.IdxOffset * sizeof(ushort)));
                }
            }
        }

        // Restore GL state
        _gl.UseProgram((uint)prevProgram);
        _gl.BindTexture(TextureTarget.Texture2D, (uint)prevTexture);
        _gl.ActiveTexture((TextureUnit)prevActiveTexture);
        _gl.BindVertexArray((uint)prevVertexArray);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, (uint)prevArrayBuffer);
        _gl.BlendEquationSeparate((BlendEquationModeEXT)prevBlendEquationRgb[0], (BlendEquationModeEXT)prevBlendEquationAlpha[0]);
        _gl.BlendFuncSeparate((BlendingFactor)prevBlendSrcRgb[0], (BlendingFactor)prevBlendDstRgb[0],
            (BlendingFactor)prevBlendSrcAlpha[0], (BlendingFactor)prevBlendDstAlpha[0]);

        if (prevEnableBlend) _gl.Enable(EnableCap.Blend); else _gl.Disable(EnableCap.Blend);
        if (prevEnableCullFace) _gl.Enable(EnableCap.CullFace); else _gl.Disable(EnableCap.CullFace);
        if (prevEnableDepthTest) _gl.Enable(EnableCap.DepthTest); else _gl.Disable(EnableCap.DepthTest);
        if (prevEnableScissorTest) _gl.Enable(EnableCap.ScissorTest); else _gl.Disable(EnableCap.ScissorTest);

        _gl.Scissor(prevScissorBox[0], prevScissorBox[1], (uint)prevScissorBox[2], (uint)prevScissorBox[3]);
    }

    private unsafe void SetupPlatformCallbacks()
    {
        var platformIO = ImGui.GetPlatformIO();

        // Note: For now, we'll use the default ImGui multi-viewport implementation
        // which should work with most windowing systems. Silk.NET handles the
        // native window creation through its IWindow interface.
        //
        // If we need custom window management in the future, we can implement
        // the platform callbacks here (CreateWindow, DestroyWindow, etc.)
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _gl.DeleteVertexArray(_vertexArray);
            _gl.DeleteBuffer(_vertexBuffer);
            _gl.DeleteBuffer(_elementBuffer);
            _gl.DeleteTexture(_fontTexture);
            _gl.DeleteProgram(_shader);

            _inputContext?.Dispose();

            ImGui.DestroyContext();
            _disposed = true;
        }
    }
}
