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

        public Vector2Int Center { get {return Min + new Vector2Int(Size.Z/2, Size.X/2);}}

        /// <summary>
        /// Creates a Integer Rect from a point
        /// </summary>
        /// <param name="point">Point to start the Integer Rect</param>    
        public RectInt(in Vector2Int point)
        {
            this.Min = point;
            this.Max = point;
        }

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
        /// Makes this rect include the point
        /// </summary>
        /// <param name="point">Point</param>
        public void Include(in Vector2Int point)
        {
            this.Min = Vector2Int.Min(this.Min, point);
            this.Max = Vector2Int.Max(this.Max, point);
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

        
        /// <summary>
        /// Calculates the Manhattan Distance from a RectInt
        /// to another.
        /// </summary>
        /// <param name="a">RectInt a</param>
        /// <param name="b">RectInt b</param>
        /// <returns>Manhattan Distance between RectsInt 'a' and 'b'</returns>
        public static int Distance(RectInt a, RectInt b)
        {
            int zDist = 0;
            int xDist = 0;
            
            if (a.Max.Z <= b.Min.Z)
                zDist = b.Min.Z - a.Max.Z;
            else if (a.Min.Z >= b.Max.Z)
                zDist = a.Min.Z - b.Max.Z;
            
            if (a.Max.X <= b.Min.X)
                xDist = b.Min.X - a.Max.X;
            else if (a.Min.X >= b.Max.X)
                xDist = a.Min.X - b.Max.X;

            return xDist + zDist;
        }
    }
}