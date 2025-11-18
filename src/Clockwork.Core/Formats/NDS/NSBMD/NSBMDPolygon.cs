// Polygon definition for NSBMD models.
// Code adapted from kiwi.ds' NSBMD Model Viewer.

using System;

namespace Clockwork.Core.Formats.NDS.NSBMD
{
    /// <summary>
    /// Type for NSBMD polygons.
    /// </summary>
    public class NSBMDPolygon
    {
        /// <summary>
        /// Used material ID.
        /// </summary>
        public int MatId { get; set; }

        /// <summary>
        /// Name of polygon.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Polygon data.
        /// </summary>
        public byte[] PolyData { get; set; }

        /// <summary>
        /// StackID of polygon.
        /// </summary>
        public int StackID { get; set; }

        public int JointID { get; set; }
    }
}
