namespace DeluMc.Utils
{
    /// <summary>
    /// Rectangle of Ints
    /// </summary>
    public struct RectInt
    {
        /// <summary>
        /// Minimum of Rect
        /// </summary>
        public Vector2Int Min { get; set; }

        /// <summary>
        /// Maximum of Rect (Inclusive)
        /// </summary>
        public Vector2Int Max { get; set; }

        /// <summary>
        /// Returns the Size of the Rectable (It is exclusive)
        /// </summary>
        /// <value>Size of the Rectable (It is exclusive)</value>
        public Vector2Int Size { get { return Max - Min + Vector2Int.One; } }

        /// <summary>
        /// Creates a Integer Rect
        /// </summary>
        /// <param name="min">Minimum of Rect</param>
        /// <param name="max">Maximum of Rect</param>
        public RectInt(in Vector2Int min, in Vector2Int max)
        {
            this.Min = min;
            this.Max = max;
        }

        /// <summary>
        /// Makes this rect include the other rect
        /// </summary>
        /// <param name="other">Other Rect</param>
        public void Include(in RectInt other)
        {
            this.Min = Vector2Int.Min(this.Min, other.Min);
            this.Max = Vector2Int.Max(this.Max, other.Max);
        }

        /// <summary>
        /// If a point is inside the rect
        /// </summary>
        /// <param name="point">Point to Test</param>
        /// <returns>If the point is inside the rect</returns>
        public bool IsInside(in Vector2Int point)
        {
            return Min.Z <= point.Z && point.Z <= Max.Z && Min.X <= point.X && point.X <= Max.X;
        }

        /// <summary>
        /// If a point is inside the rect
        /// </summary>
        /// <param name="z">Z Coordinate</param>
        /// <param name="x">X Coordinate</param>
        /// <returns>If the point is inside the rect</returns>
        public bool IsInside(int z, int x)
        {
            return Min.Z <= z && z <= Max.Z && Min.X <= x && x <= Max.X;
        }
    }
}