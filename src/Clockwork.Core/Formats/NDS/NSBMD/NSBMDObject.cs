// Object definition for NSBMD models.
// Code adapted from kiwi.ds' NSBMD Model Viewer.

using System;
using System.Collections.Generic;

namespace Clockwork.Core.Formats.NDS.NSBMD
{
    /// <summary>
    /// Type for NSBMD objects.
    /// </summary>
    public class NSBMDObject
    {
        private readonly float[] _transVect = new float[3];
        private float _x;
        private float _y;
        private float _z;
        private const float FACTOR1 = 1f;

        // StackID used by this object
        public int RestoreID = -1;
        // rotation
        public int StackID = -1;

        public bool visible = true;

        public List<int> childs = new List<int>();

        public float[] rotate_mtx = Matrix4x4Util.LoadIdentity();
        public float[] scale = new float[3];

        public float[] materix = Matrix4x4Util.LoadIdentity();

        public bool isBillboard = false;
        public bool isYBillboard = false;

        public bool IsRotated { get; set; }
        public bool IsRotated2 { get; set; }

        public bool IsScaled { get; set; }

        // this object's ParentID object ID
        public String Name { get; set; }

        public int Neg { get; set; }

        // RestoreID is the ID of the matrix in stack to be restored as current matrix
        public int ParentID { get; set; }

        public int Pivot { get; set; }

        // applies to rotation matrix
        public float RotA { get; set; }

        // rotation
        public float RotB { get; set; }

        // Name of this object
        public bool Trans { get; set; }

        public float[] TransVect
        {
            get { return _transVect; }
        }

        public float X
        {
            get { return _x; }
            set
            {
                _x = value;
                TransVect[0] = value/FACTOR1;
            }
        }

        public float Y
        {
            get { return _y; }
            set
            {
                _y = value;
                TransVect[1] = value/FACTOR1;
            }
        }

        public float Z
        {
            get { return _z; }
            set
            {
                _z = value;
                TransVect[2] = value/FACTOR1;
            }
        }
    }
}
