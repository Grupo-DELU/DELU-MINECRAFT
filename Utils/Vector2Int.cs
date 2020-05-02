using System;
namespace DeluMc.Utils
{

    /// <summary>
    /// Vector 2 of Ints
    /// </summary>
    public struct Vector2Int
    {
        /// <summary>
        /// Z Position of Vector 2
        /// </summary>
        public int Z;

        /// <summary>
        /// X Position of Vector 2
        /// </summary>
        public int X;

        /// <summary>
        /// Vector2Int Constructor
        /// </summary>
        /// <param name="z">Z Position of Vector 2</param>
        /// <param name="x">X Position of Vector 2</param>
        public Vector2Int(int z = 0, int x = 0)
        {
            this.Z = z;
            this.X = x;
        }

        /// <summary>
        /// Vector Zero (0, 0)
        /// </summary>
        public static readonly Vector2Int Zero = new Vector2Int(0, 0);

        /// <summary>
        /// Vector One (1, 1)
        /// </summary>
        public static readonly Vector2Int One = new Vector2Int(1, 1);

        /// <summary>
        /// Vector Axis Z (1, 0)
        /// </summary>
        public static readonly Vector2Int AxisZ = new Vector2Int(1, 0);

        /// <summary>
        /// Vector Axis X (0, 1)
        /// </summary>
        public static readonly Vector2Int AxisX = new Vector2Int(0, 1);

        /// <summary>
        /// Negate a Vector
        /// </summary>
        /// <param name="v">Vector to negate</param>
        /// <returns>Negated Vector</returns>
        public static Vector2Int operator -(Vector2Int v)
        {
            v.Z = -v.Z;
            v.Z = -v.Z;
            return v;
        }

        /// <summary>
        /// Compares two Vector2 Int component wise for equality
        /// </summary>
        /// <param name="a">First Vector</param>
        /// <param name="b">Second Vector</param>
        /// <returns>True if a == b</returns>
        public static bool operator ==(in Vector2Int a, in Vector2Int b)
        {
            return a.Z == b.Z && a.X == b.X;
        }

        /// <summary>
        /// Compares two Vector2 Int component wise for inequality
        /// </summary>
        /// <param name="a">First Vector</param>
        /// <param name="b">Second Vector</param>
        /// <returns>True if a!=b</returns>
        public static bool operator !=(in Vector2Int a, in Vector2Int b)
        {
            return a.Z != b.Z || a.X != b.X;
        }

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
                Vector2Int p = (Vector2Int)obj;
                return this == p;
            }
        }

        public override int GetHashCode()
        {
            return (X << 2) ^ Z;
        }


        /// <summary>
        /// Add two Vector2 Int component wise
        /// </summary>
        /// <param name="a">First Vector</param>
        /// <param name="b">Second Vector</param>
        /// <returns>a+b</returns>
        public static Vector2Int operator +(in Vector2Int a, in Vector2Int b)
        {
            return new Vector2Int(a.Z + b.Z, a.X + b.X);
        }

        /// <summary>
        /// Subtracts two Vector2 Int component wise
        /// </summary>
        /// <param name="a">First Vector</param>
        /// <param name="b">Second Vector</param>
        /// <returns>a-b</returns>
        public static Vector2Int operator -(in Vector2Int a, in Vector2Int b)
        {
            return new Vector2Int(a.Z - b.Z, a.X - b.X);
        }

        /// <summary>
        /// Component Wise Min of two vectors
        /// </summary>
        /// <param name="a">First Vector</param>
        /// <param name="b">Second Vector</param>
        /// <returns>Component Wise Min of the two vectors</returns>
        public static Vector2Int Min(in Vector2Int a, in Vector2Int b)
        {
            return new Vector2Int(Math.Min(a.Z, b.Z), Math.Min(a.X, b.X));
        }

        /// <summary>
        /// Component Wise Max of two vectors
        /// </summary>
        /// <param name="a">First Vector</param>
        /// <param name="b">Second Vector</param>
        /// <returns>Component Wise Max of the two vectors</returns>
        public static Vector2Int Max(in Vector2Int a, in Vector2Int b)
        {
            return new Vector2Int(Math.Max(a.Z, b.Z), Math.Max(a.X, b.X));
        }
    }
}