// Stub classes to replace WPF dependencies in NSBMDGlRenderer
// These are minimal implementations just to make the code compile

using System;
using System.Collections.Generic;

using Point3D = Clockwork.Core.Formats.NDS.NSBMD.Point3D;
using Vector3D = Clockwork.Core.Formats.NDS.NSBMD.Vector3D;

namespace Clockwork.Core.Formats.NDS.NSBMD
{
    // Stub for WPF DependencyProperty
    public class DependencyProperty
    {
        public static DependencyProperty Register(string name, Type propertyType, Type ownerType)
        {
            return new DependencyProperty();
        }
    }

    // Stub for ImageSource
    public class ImageSource
    {
        public int Width { get; set; }
        public int Height { get; set; }
    }

    // Stub for WPF ImageBrush
    public class ImageBrush
    {
        public ImageBrush() { ImageSource = new ImageSource(); }
        public ImageBrush(object source) { ImageSource = new ImageSource(); }
        public ImageSource ImageSource { get; set; }
        public double Opacity { get; set; }
        public Rect Viewbox { get; set; }
    }

    // Stub for WPF DiffuseMaterial
    public class DiffuseMaterial
    {
        public DiffuseMaterial()
        {
            Brush = new ImageBrush();
            AmbientColor = new Color();
            Color = new Color();
        }
        public ImageBrush Brush { get; set; }
        public Color AmbientColor { get; set; }
        public Color Color { get; set; }

        public void SetValue(DependencyProperty prop, object value) { }
    }

    // Stub for HelixToolkit MeshBuilder
    public class MeshBuilder
    {
    }

    // Stub for MaterialHelper (if needed)
    public static class MaterialHelper
    {
        public static object CreateMaterial(ImageBrush brush)
        {
            return new object();
        }
    }

    // Stub for Group (used in export)
    public class Group : List<object>
    {
    }

    // Stub for Model3D
    public class Model3D
    {
    }

    // Stub for ObjExporter
    public class ObjExporter
    {
        public ObjExporter(string file, string comment) { }
        public void Close() { }
        public void AddModel(object model, object material) { }
    }

    // Stub for Vector3
    public struct Vector3
    {
        public float X, Y, Z;
        public Vector3(float x, float y, float z) { X = x; Y = y; Z = z; }
    }

    // Stub for Vector3D (WPF 3D vector)
    public struct Vector3D
    {
        public double X, Y, Z;
        public Vector3D(double x, double y, double z) { X = x; Y = y; Z = z; }
        public static Vector3D operator +(Vector3D a, Vector3D b) => new Vector3D(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vector3D operator -(Vector3D a, Vector3D b) => new Vector3D(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vector3D operator *(Vector3D a, double d) => new Vector3D(a.X * d, a.Y * d, a.Z * d);
    }

    // Stub for Point3D (WPF 3D point)
    public struct Point3D
    {
        public double X, Y, Z;
        public Point3D(double x, double y, double z) { X = x; Y = y; Z = z; }
    }

    // Stub for Point (System.Windows.Point - 2D point for texture coordinates)
    public struct Point
    {
        public double X, Y;
        public Point(double x, double y) { X = x; Y = y; }
    }

    // Stub for Rect
    public struct Rect
    {
        public double X, Y, Width, Height;
        public Rect(double x, double y, double width, double height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }

    // Stub for PolygonType
    public enum PolygonType
    {
        Triangle,
        Quad,
        TriangleStrip,
        QuadStrip
    }

    // Stub for Polygon (Export3DTools.Polygon)
    public class Polygon
    {
        public PolygonType PolyType { get; set; }
        public object[] Positions { get; set; }
        public object[] Normals { get; set; }
        public object[] TextureCoordinates { get; set; }

        public Polygon(PolygonType type, object[] positions, object[] normals, object[] textureCoords)
        {
            PolyType = type;
            Positions = positions;
            Normals = normals;
            TextureCoordinates = textureCoords;
        }
    }
}

// Add HelixToolkit namespace stub
namespace HelixToolkit
{
    public class MeshBuilder
    {
    }
}

// Add System.Windows namespace stubs
namespace System.Windows
{
    using Clockwork.Core.Formats.NDS.NSBMD;

    // Re-export Point as System.Windows.Point
    public struct Point
    {
        public double X, Y;
        public Point(double x, double y) { X = x; Y = y; }
    }

    // Stub for Int32Rect
    public struct Int32Rect
    {
        public static Int32Rect Empty = new Int32Rect();
    }
}

// Add System.Windows.Media namespace stubs
namespace System.Windows.Media
{
    using Clockwork.Core.Formats.NDS.NSBMD;

    // Re-export ImageSource
    public class Imaging
    {
        public class BitmapSource { }
        public class BitmapImage
        {
            public void BeginInit() { }
            public void EndInit() { }
            public object StreamSource { get; set; }
        }
        public class BitmapSizeOptions
        {
            public static BitmapSizeOptions FromEmptyOptions() => new BitmapSizeOptions();
        }
    }

    // Re-export types


    namespace Media3D
    {
        public struct Point3D
        {
            public double X, Y, Z;
            public Point3D(double x, double y, double z) { X = x; Y = y; Z = z; }
        }

        public struct Vector3D
        {
            public double X, Y, Z;
            public Vector3D(double x, double y, double z) { X = x; Y = z; Z = z; }
        }
    }
}

// Add System.Windows.Interop namespace stub
namespace System.Windows.Interop
{
    public static class Imaging
    {
        public static object CreateBitmapSourceFromHBitmap(IntPtr hBitmap, IntPtr hPalette, object rect, object options)
        {
            return null;
        }
    }
}

// Add System.Drawing stubs
namespace System.Drawing
{
    using Clockwork.Core.Formats.NDS.NSBMD;
    using System.IO;

    public class Bitmap
    {
        private int _width, _height;
        private RGBA[,] _pixels;

        public Bitmap(int width, int height)
        {
            _width = width;
            _height = height;
            _pixels = new RGBA[width, height];
        }

        public void SetPixel(int x, int y, Color color)
        {
            if (x >= 0 && x < _width && y >= 0 && y < _height)
            {
                _pixels[x, y] = new RGBA(color.R, color.G, color.B, color.A);
            }
        }

        public void RotateFlip(RotateFlipType type)
        {
            // Stub - no actual rotation
        }

        public IntPtr GetHbitmap()
        {
            return IntPtr.Zero;
        }

        public void Save(Stream stream, Imaging.ImageFormat format)
        {
            // Stub - no actual save
        }
    }

    public class Graphics : IDisposable
    {
        public static Graphics FromImage(object image) => new Graphics();
        public void DrawImage(object image, int x, int y) { }
        public void Dispose() { }
    }

    public enum RotateFlipType
    {
        RotateNoneFlipX,
        RotateNoneFlipY
    }

    namespace Imaging
    {
        public class ImageFormat
        {
            public static ImageFormat Png = new ImageFormat();
        }
    }
}
