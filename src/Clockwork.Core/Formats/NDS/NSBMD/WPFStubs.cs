// Stub classes to replace WPF dependencies in NSBMDGlRenderer
// These are minimal implementations just to make the code compile

using System;

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
    }

    // Stub for WPF DiffuseMaterial
    public class DiffuseMaterial
    {
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
}

// Add HelixToolkit namespace stub
namespace HelixToolkit
{
    public class MeshBuilder
    {
    }
}
