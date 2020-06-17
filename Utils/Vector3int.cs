using System;

namespace DeluMc.Utils
{
    // <summary>
    /// Point with ZCurve for Hashing
    /// </summary>
    public class ZPoint3D
    {
        /// <summary>
        /// Y Coord
        /// </summary>
        public int Y { get; set; }

        /// <summary>
        /// Z Coord
        /// </summary>
        public int Z { get; set; }

        /// <summary>
        /// X Coord
        /// </summary>
        public int X { get; set; }

        /// <summary>
        /// C# Object Equality
        /// </summary>
        /// <param name="obj">Other Object</param>
        /// <returns>If other object is equals</returns>
        public override bool Equals(Object obj)
        {
            //Check for null and compare run-time types.
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }
            else
            {
                ZPoint3D p = (ZPoint3D)obj;
                return this.Y == p.Y && this.Z == p.Z && this.X == p.X;
            }
        }

        /// <summary>
        /// Hashing for dictionary using ZCurves
        /// </summary>
        public override int GetHashCode()
        {
            System.Diagnostics.Debug.Assert(Y >= 0 && Z >= 0 && X >= 0);
            return (int)ZCurve.Pos3D((uint)Y, (uint)Z, (uint)X);
        }
    }
}