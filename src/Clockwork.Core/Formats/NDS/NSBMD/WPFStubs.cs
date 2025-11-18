// Stub classes to replace WPF dependencies in NSBMDGlRenderer
// These are minimal implementations just to make the code compile

using System;
using System.Collections.Generic;

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

    // Stub for WPF ImageBrush
    public class ImageBrush
    {
        public ImageBrush() { }
        public ImageBrush(object source) { }
        public object ImageSource { get; set; }
        public double Opacity { get; set; }
        public object Viewbox { get; set; }
    }

    // Stub for WPF DiffuseMaterial
    public class DiffuseMaterial
    {
        public object Brush { get; set; }
        public object AmbientColor { get; set; }
        public object Color { get; set; }
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

    // Stub for PolygonType
    public enum PolygonType
    {
        Triangle,
        Quad,
        TriangleStrip,
        QuadStrip
    }
}

// Add HelixToolkit namespace stub
namespace HelixToolkit
{
    public class MeshBuilder
    {
    }
}
