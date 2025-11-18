// WPF and Windows Forms Compatibility stubs for cross-platform .NET 8
// These types replace WPF/WinForms dependencies used in the codebase

using System;
using System.Collections.Generic;
using OpenTK.Mathematics;

// System.Windows namespace
namespace System.Windows
{
    public class DependencyProperty
    {
        public static DependencyProperty Register(string name, Type propertyType, Type ownerType)
        {
            return new DependencyProperty();
        }
    }

    public struct Int32Rect
    {
        public static Int32Rect Empty = new Int32Rect();
    }

    public struct Point
    {
        public double X, Y;
        public Point(double x, double y) { X = x; Y = y; }
    }
}

// System.Windows.Media namespace
namespace System.Windows.Media
{
    using System.IO;

    public class ImageSource
    {
        public int Width { get; set; }
        public int Height { get; set; }
    }

    public class ImageBrush
    {
        public ImageBrush() { ImageSource = new ImageSource(); }
        public ImageBrush(object source) { ImageSource = new ImageSource(); }
        public ImageSource ImageSource { get; set; }
        public double Opacity { get; set; }
        public object Viewbox { get; set; }
        public object ViewboxUnits { get; set; }
        public object Viewport { get; set; }
        public object ViewportUnits { get; set; }
        public object Stretch { get; set; }
    }

    public class DiffuseMaterial
    {
        public ImageBrush Brush { get; set; } = new ImageBrush();
        public object AmbientColor { get; set; }
        public object Color { get; set; }

        public DiffuseMaterial() { }
        public DiffuseMaterial(object brush) { }

        public void SetValue(DependencyProperty prop, object value) { }
    }

    public class SolidColorBrush
    {
        public SolidColorBrush() { }
        public SolidColorBrush(object color) { }
    }

    namespace Imaging
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
}

// System.Windows.Media.Media3D namespace
namespace System.Windows.Media.Media3D
{
    public struct Point3D
    {
        public double X, Y, Z;
        public Point3D(double x, double y, double z) { X = x; Y = y; Z = z; }
    }

    public struct Vector3D
    {
        public double X, Y, Z;
        public Vector3D(double x, double y, double z) { X = x; Y = y; Z = z; }
    }

    public class Model3D { }

    public class GeometryModel3D : Model3D
    {
        public object Geometry { get; set; }
        public object Material { get; set; }

        public GeometryModel3D() { }
        public GeometryModel3D(object geometry, object material)
        {
            Geometry = geometry;
            Material = material;
        }
    }
}

// System.Windows.Interop namespace
namespace System.Windows.Interop
{
    public static class Imaging
    {
        public static object CreateBitmapSourceFromHBitmap(IntPtr hBitmap, IntPtr hPalette, object rect, object options)
        {
            // Stub - returns null in cross-platform
            return null;
        }
    }
}

// HelixToolkit.Wpf namespace
namespace HelixToolkit.Wpf
{
    using System.Windows;
    using System.Windows.Media.Media3D;

    public class MeshBuilder
    {
        public void AddTriangle(Point3D p0, Point3D p1, Point3D p2) { }
        public void AddTriangle(Point3D p0, Point3D p1, Point3D p2, Point t0, Point t1, Point t2) { }

        public void AddQuad(Point3D p0, Point3D p1, Point3D p2, Point3D p3) { }
        public void AddQuad(Point3D p0, Point3D p1, Point3D p2, Point3D p3, Point t0, Point t1, Point t2, Point t3) { }

        public void AddTriangles(IList<Point3D> positions) { }
        public void AddTriangles(IList<Point3D> positions, IList<Vector3D> normals, IList<Point> textureCoords) { }

        public void AddQuads(IList<Point3D> positions) { }
        public void AddQuads(IList<Point3D> positions, IList<Vector3D> normals, IList<Point> textureCoords) { }

        public void AddTriangleStrip(IList<Point3D> positions) { }
        public void AddTriangleStrip(IList<Point3D> positions, IList<Vector3D> normals, IList<Point> textureCoords) { }

        public object ToMesh(bool freeze) { return new object(); }
    }
}

// System.Windows.Forms namespace
namespace System.Windows.Forms
{
    using System.ComponentModel;
    using System.Drawing;

    public class Form
    {
        public Form() { }
    }

    public class Control
    {
        public Control() { }
    }
}
