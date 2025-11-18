using System.Runtime.CompilerServices;
using ImGuiNET;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

namespace Clockwork.UI;

/// <summary>
/// Contrôleur ImGui qui gère le rendu et l'intégration avec OpenGL/OpenTK.
/// </summary>
public class ImGuiController : IDisposable
{
    private bool _disposed;
    private int _vertexArray;
    private int _vertexBuffer;
    private int _elementBuffer;
    private int _vertexBufferSize;
    private int _elementBufferSize;
    private int _fontTexture;
    private int _shader;
    private int _shaderFontTexture;
    private int _shaderProjectionMatrix;
    private int _windowWidth;
    private int _windowHeight;

    /// <summary>
    /// Crée une nouvelle instance du contrôleur ImGui.
    /// </summary>
    public ImGuiController(int width, int height)
    {
        _windowWidth = width;
        _windowHeight = height;

        IntPtr context = ImGui.CreateContext();
        ImGui.SetCurrentContext(context);

        var io = ImGui.GetIO();
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;
        io.ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
        io.ConfigFlags |= ImGuiConfigFlags.DockingEnable;

        CreateDeviceResources();

        SetPerFrameImGuiData(1f / 60f);

        ImGui.NewFrame();
    }

    private void CreateDeviceResources()
    {
        _vertexBufferSize = 10000;
        _elementBufferSize = 2000;

        // Vertex Array
        _vertexArray = GL.GenVertexArray();
        GL.BindVertexArray(_vertexArray);

        // Vertex Buffer
        _vertexBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
        GL.BufferData(BufferTarget.ArrayBuffer, _vertexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);

        // Element Buffer
        _elementBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _elementBuffer);
        GL.BufferData(BufferTarget.ElementArrayBuffer, _elementBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);

        RecreateFontDeviceTexture();
        CreateShader();

        GL.BindVertexArray(_vertexArray);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);

        // Position
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, Unsafe.SizeOf<ImDrawVert>(), 0);

        // UV
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, Unsafe.SizeOf<ImDrawVert>(), 8);

        // Color
        GL.EnableVertexAttribArray(2);
        GL.VertexAttribPointer(2, 4, VertexAttribPointerType.UnsignedByte, true, Unsafe.SizeOf<ImDrawVert>(), 16);

        GL.BindVertexArray(0);
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
        _shaderProjectionMatrix = GL.GetUniformLocation(_shader, "ProjMtx");
        _shaderFontTexture = GL.GetUniformLocation(_shader, "Texture");
    }

    private static int CreateProgram(string name, string vertexSource, string fragmentSource)
    {
        int program = GL.CreateProgram();
        int vertex = CompileShader(name, ShaderType.VertexShader, vertexSource);
        int fragment = CompileShader(name, ShaderType.FragmentShader, fragmentSource);

        GL.AttachShader(program, vertex);
        GL.AttachShader(program, fragment);

        GL.LinkProgram(program);

        GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int success);
        if (success == 0)
        {
            string info = GL.GetProgramInfoLog(program);
            Console.WriteLine($"GL.LinkProgram had info log [{name}]:\n{info}");
        }

        GL.DetachShader(program, vertex);
        GL.DetachShader(program, fragment);

        GL.DeleteShader(vertex);
        GL.DeleteShader(fragment);

        return program;
    }

    private static int CompileShader(string name, ShaderType type, string source)
    {
        int shader = GL.CreateShader(type);
        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);

        GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
        if (success == 0)
        {
            string info = GL.GetShaderInfoLog(shader);
            Console.WriteLine($"GL.CompileShader for shader '{name}' [{type}] had info log:\n{info}");
        }

        return shader;
    }

    private void RecreateFontDeviceTexture()
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out int bytesPerPixel);

        _fontTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _fontTexture);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0,
            PixelFormat.Rgba, PixelType.UnsignedByte, pixels);

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
    public void Update(GameWindow window, double deltaTime)
    {
        SetPerFrameImGuiData((float)deltaTime);
        UpdateImGuiInput(window);

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

    private void UpdateImGuiInput(GameWindow window)
    {
        ImGuiIOPtr io = ImGui.GetIO();

        var mouseState = window.MouseState;
        var keyboardState = window.KeyboardState;

        io.MouseDown[0] = mouseState.IsButtonDown(MouseButton.Left);
        io.MouseDown[1] = mouseState.IsButtonDown(MouseButton.Right);
        io.MouseDown[2] = mouseState.IsButtonDown(MouseButton.Middle);

        var screenPoint = new Vector2i((int)mouseState.X, (int)mouseState.Y);
        io.MousePos = new System.Numerics.Vector2(screenPoint.X, screenPoint.Y);

        io.AddKeyEvent(ImGuiKey.ModCtrl, keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl));
        io.AddKeyEvent(ImGuiKey.ModAlt, keyboardState.IsKeyDown(Keys.LeftAlt) || keyboardState.IsKeyDown(Keys.RightAlt));
        io.AddKeyEvent(ImGuiKey.ModShift, keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift));
        io.AddKeyEvent(ImGuiKey.ModSuper, keyboardState.IsKeyDown(Keys.LeftSuper) || keyboardState.IsKeyDown(Keys.RightSuper));
    }

    /// <summary>
    /// Effectue le rendu d'ImGui.
    /// </summary>
    public void Render()
    {
        ImGui.Render();
        RenderImDrawData(ImGui.GetDrawData());
    }

    private void RenderImDrawData(ImDrawDataPtr drawData)
    {
        if (drawData.CmdListsCount == 0)
            return;

        // Backup GL state
        GL.GetInteger(GetPName.ActiveTexture, out int prevActiveTexture);
        GL.ActiveTexture(TextureUnit.Texture0);
        GL.GetInteger(GetPName.CurrentProgram, out int prevProgram);
        GL.GetInteger(GetPName.TextureBinding2D, out int prevTexture);
        GL.GetInteger(GetPName.ArrayBufferBinding, out int prevArrayBuffer);
        GL.GetInteger(GetPName.VertexArrayBinding, out int prevVertexArray);

        int[] prevScissorBox = new int[4];
        GL.GetInteger(GetPName.ScissorBox, prevScissorBox);

        int[] prevBlendSrcRgb = new int[1];
        GL.GetInteger(GetPName.BlendSrcRgb, prevBlendSrcRgb);
        int[] prevBlendDstRgb = new int[1];
        GL.GetInteger(GetPName.BlendDstRgb, prevBlendDstRgb);
        int[] prevBlendSrcAlpha = new int[1];
        GL.GetInteger(GetPName.BlendSrcAlpha, prevBlendSrcAlpha);
        int[] prevBlendDstAlpha = new int[1];
        GL.GetInteger(GetPName.BlendDstAlpha, prevBlendDstAlpha);
        int[] prevBlendEquationRgb = new int[1];
        GL.GetInteger(GetPName.BlendEquationRgb, prevBlendEquationRgb);
        int[] prevBlendEquationAlpha = new int[1];
        GL.GetInteger(GetPName.BlendEquationAlpha, prevBlendEquationAlpha);

        bool prevEnableBlend = GL.IsEnabled(EnableCap.Blend);
        bool prevEnableCullFace = GL.IsEnabled(EnableCap.CullFace);
        bool prevEnableDepthTest = GL.IsEnabled(EnableCap.DepthTest);
        bool prevEnableScissorTest = GL.IsEnabled(EnableCap.ScissorTest);

        // Setup render state
        GL.Enable(EnableCap.Blend);
        GL.Enable(EnableCap.ScissorTest);
        GL.BlendEquation(BlendEquationMode.FuncAdd);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.Disable(EnableCap.CullFace);
        GL.Disable(EnableCap.DepthTest);

        // Setup viewport and projection matrix
        GL.Viewport(0, 0, _windowWidth, _windowHeight);
        float L = drawData.DisplayPos.X;
        float R = drawData.DisplayPos.X + drawData.DisplaySize.X;
        float T = drawData.DisplayPos.Y;
        float B = drawData.DisplayPos.Y + drawData.DisplaySize.Y;

        float[] orthoProjection = new float[]
        {
            2.0f / (R - L), 0.0f, 0.0f, 0.0f,
            0.0f, 2.0f / (T - B), 0.0f, 0.0f,
            0.0f, 0.0f, -1.0f, 0.0f,
            (R + L) / (L - R), (T + B) / (B - T), 0.0f, 1.0f,
        };

        GL.UseProgram(_shader);
        GL.UniformMatrix4(_shaderProjectionMatrix, 1, false, orthoProjection);
        GL.Uniform1(_shaderFontTexture, 0);

        GL.BindVertexArray(_vertexArray);

        drawData.ScaleClipRects(ImGui.GetIO().DisplayFramebufferScale);

        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            ImDrawListPtr cmdList = drawData.CmdLists[n];

            int vertexSize = cmdList.VtxBuffer.Size * Unsafe.SizeOf<ImDrawVert>();
            if (vertexSize > _vertexBufferSize)
            {
                _vertexBufferSize = (int)(vertexSize * 1.5f);
                GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
                GL.BufferData(BufferTarget.ArrayBuffer, _vertexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
            }

            int indexSize = cmdList.IdxBuffer.Size * sizeof(ushort);
            if (indexSize > _elementBufferSize)
            {
                _elementBufferSize = (int)(indexSize * 1.5f);
                GL.BindBuffer(BufferTarget.ElementArrayBuffer, _elementBuffer);
                GL.BufferData(BufferTarget.ElementArrayBuffer, _elementBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
            }

            GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, vertexSize, cmdList.VtxBuffer.Data);

            GL.BindBuffer(BufferTarget.ElementArrayBuffer, _elementBuffer);
            GL.BufferSubData(BufferTarget.ElementArrayBuffer, IntPtr.Zero, indexSize, cmdList.IdxBuffer.Data);

            for (int cmdI = 0; cmdI < cmdList.CmdBuffer.Size; cmdI++)
            {
                ImDrawCmdPtr pcmd = cmdList.CmdBuffer[cmdI];

                if (pcmd.UserCallback != IntPtr.Zero)
                {
                    throw new NotImplementedException("User callbacks not supported");
                }

                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, (int)pcmd.TextureId);

                var clip = pcmd.ClipRect;
                GL.Scissor((int)clip.X, _windowHeight - (int)clip.W, (int)(clip.Z - clip.X), (int)(clip.W - clip.Y));

                if ((ImGui.GetIO().BackendFlags & ImGuiBackendFlags.RendererHasVtxOffset) != 0)
                {
                    GL.DrawElementsBaseVertex(PrimitiveType.Triangles, (int)pcmd.ElemCount, DrawElementsType.UnsignedShort,
                        (IntPtr)(pcmd.IdxOffset * sizeof(ushort)), (int)pcmd.VtxOffset);
                }
                else
                {
                    GL.DrawElements(PrimitiveType.Triangles, (int)pcmd.ElemCount, DrawElementsType.UnsignedShort,
                        (int)pcmd.IdxOffset * sizeof(ushort));
                }
            }
        }

        // Restore GL state
        GL.UseProgram(prevProgram);
        GL.BindTexture(TextureTarget.Texture2D, prevTexture);
        GL.ActiveTexture((TextureUnit)prevActiveTexture);
        GL.BindVertexArray(prevVertexArray);
        GL.BindBuffer(BufferTarget.ArrayBuffer, prevArrayBuffer);
        GL.BlendEquationSeparate((BlendEquationMode)prevBlendEquationRgb[0], (BlendEquationMode)prevBlendEquationAlpha[0]);
        GL.BlendFuncSeparate((BlendingFactorSrc)prevBlendSrcRgb[0], (BlendingFactorDest)prevBlendDstRgb[0],
            (BlendingFactorSrc)prevBlendSrcAlpha[0], (BlendingFactorDest)prevBlendDstAlpha[0]);

        if (prevEnableBlend) GL.Enable(EnableCap.Blend); else GL.Disable(EnableCap.Blend);
        if (prevEnableCullFace) GL.Enable(EnableCap.CullFace); else GL.Disable(EnableCap.CullFace);
        if (prevEnableDepthTest) GL.Enable(EnableCap.DepthTest); else GL.Disable(EnableCap.DepthTest);
        if (prevEnableScissorTest) GL.Enable(EnableCap.ScissorTest); else GL.Disable(EnableCap.ScissorTest);

        GL.Scissor(prevScissorBox[0], prevScissorBox[1], prevScissorBox[2], prevScissorBox[3]);
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            GL.DeleteVertexArray(_vertexArray);
            GL.DeleteBuffer(_vertexBuffer);
            GL.DeleteBuffer(_elementBuffer);
            GL.DeleteTexture(_fontTexture);
            GL.DeleteProgram(_shader);

            ImGui.DestroyContext();
            _disposed = true;
        }
    }
}
