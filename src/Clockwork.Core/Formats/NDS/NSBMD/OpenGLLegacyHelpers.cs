// Legacy OpenGL compatibility layer for NSBMD renderer
// This provides stub implementations for OpenGL 1.x/2.x fixed-function pipeline
// that is not available in OpenTK 4.x (which only supports modern OpenGL 3.0+)

using OpenTK.Graphics.OpenGL;
using OpenTK.Mathematics;
using System;
using System.Collections.Generic;

namespace Clockwork.Core.Formats.NDS.NSBMD
{
    /// <summary>
    /// Legacy OpenGL helper class to bridge old fixed-function pipeline code
    /// to modern OpenGL. Many functions are stubs/noops since full conversion
    /// to modern OpenGL requires complete renderer rewrite.
    /// </summary>
    public static class GLLegacy
    {
        // OpenGL Legacy constants that don't exist in OpenTK 4
        public const int GL_MODULATE = 0x2100;
        public const int GL_DECAL = 0x2101;
        public const int GL_SHADOW_AMBIENT_SGIX = 0x80BF;
        public const int GL_TRIANGLES = (int)PrimitiveType.Triangles;
        public const int GL_QUADS = (int)PrimitiveType.Quads;
        public const int GL_TRIANGLE_STRIP = (int)PrimitiveType.TriangleStrip;
        public const int GL_QUAD_STRIP = (int)PrimitiveType.QuadStrip;
        public const int GL_POINTS = (int)PrimitiveType.Points;
        public const int GL_LINES = (int)PrimitiveType.Lines;
        public const int GL_LINE_STRIP = (int)PrimitiveType.LineStrip;

        public const int GL_TEXTURE_2D = (int)TextureTarget.Texture2D;
        public const int GL_TEXTURE = 0x1702;
        public const int GL_TEXTURE_ENV = 0x2300;
        public const int GL_TEXTURE_ENV_MODE = 0x2200;
        public const int GL_TEXTURE_MIN_FILTER = (int)TextureParameterName.TextureMinFilter;
        public const int GL_TEXTURE_MAG_FILTER = (int)TextureParameterName.TextureMagFilter;
        public const int GL_TEXTURE_WRAP_S = (int)TextureParameterName.TextureWrapS;
        public const int GL_TEXTURE_WRAP_T = (int)TextureParameterName.TextureWrapT;
        public const int GL_NEAREST = (int)TextureMinFilter.Nearest;
        public const int GL_LINEAR = (int)TextureMinFilter.Linear;
        public const int GL_REPEAT = (int)TextureWrapMode.Repeat;
        public const int GL_CLAMP = 0x2900;
        public const int GL_MIRRORED_REPEAT = (int)TextureWrapMode.MirroredRepeat;
        public const int GL_RGBA = (int)PixelFormat.Rgba;
        public const int GL_RGB = (int)PixelFormat.Rgb;
        public const int GL_UNSIGNED_BYTE = (int)PixelType.UnsignedByte;

        public const int GL_LIGHTING = (int)EnableCap.Lighting;
        public const int GL_LIGHT0 = (int)EnableCap.Light0;
        public const int GL_LIGHT1 = (int)EnableCap.Light1;
        public const int GL_LIGHT2 = (int)EnableCap.Light2;
        public const int GL_LIGHT3 = (int)EnableCap.Light3;
        public const int GL_ALPHA_TEST = (int)EnableCap.AlphaTest;
        public const int GL_BLEND = (int)EnableCap.Blend;
        public const int GL_COLOR_MATERIAL = (int)EnableCap.ColorMaterial;
        public const int GL_TEXTURE_GEN_S = (int)EnableCap.TextureGenS;
        public const int GL_TEXTURE_GEN_T = (int)EnableCap.TextureGenT;

        public const int GL_DIFFUSE = 0x1201;
        public const int GL_AMBIENT = 0x1200;
        public const int GL_SPECULAR = 0x1202;
        public const int GL_EMISSION = 0x1600;
        public const int GL_POSITION = 0x1203;

        public const int GL_FRONT = (int)MaterialFace.Front;
        public const int GL_BACK = (int)MaterialFace.Back;
        public const int GL_FRONT_AND_BACK = (int)MaterialFace.FrontAndBack;
        public const int GL_NONE = 0;

        public const int GL_MODELVIEW = 0x1700;
        public const int GL_MODELVIEW_MATRIX = 0x0BA6;
        public const int GL_TEXTURE_MATRIX = 0x0BA8;
        public const int GL_PROJECTION = 0x1701;

        public const int GL_SRC_ALPHA = (int)BlendingFactor.SrcAlpha;
        public const int GL_ONE_MINUS_SRC_ALPHA = (int)BlendingFactor.OneMinusSrcAlpha;
        public const int GL_GREATER = (int)AlphaFunction.Greater;

        public const int GL_S = 0x2000;
        public const int GL_T = 0x2001;
        public const int GL_TEXTURE_GEN_MODE = 0x2500;
        public const int GL_SPHERE_MAP = 0x2402;

        // Stub implementations for legacy OpenGL functions
        public static void Lightfv(int light, int pname, float[] parameters) { /* noop */ }
        public static void Color3f(float r, float g, float b) { /* noop */ }
        public static void Color4f(float r, float g, float b, float a) { /* noop */ }
        public static void Normal3f(float nx, float ny, float nz) { /* noop */ }
        public static void TexCoord2f(float s, float t) { /* noop */ }
        public static void Vertex3fv(float[] v) { /* noop */ }
        public static void LoadMatrixf(float[] m) { /* noop */ }
        public static void MultMatrixf(float[] m) { /* noop */ }
        public static void Rotatef(float angle, float x, float y, float z) { /* noop */ }
        public static void Scalef(float x, float y, float z) { /* noop */ }
        public static void TexEnvi(int target, int pname, int param) { /* noop */ }
        public static void TexGeni(int coord, int pname, int param) { /* noop */ }
        public static void TexParameterf(int target, int pname, float param) { /* noop */ }
        public static void TexParameteri(int target, int pname, int param)
        {
            // Forward to actual OpenGL for texture parameters
            GL.TexParameter((TextureTarget)target, (TextureParameterName)pname, param);
        }
        public static void GetFloatv(int pname, float[] parameters) { /* noop - fill with identity */ }
        public static void Begin(int mode) { /* noop */ }
        public static void End() { /* noop */ }
    }
}
