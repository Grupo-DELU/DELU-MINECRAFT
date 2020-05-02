namespace DeluMc.Utils
{
    public struct RectInt
    {
        /// <summary>
        /// Minimum of Rect
        /// </summary>
        public Vector2Int Min { get; set; }

        /// <summary>
        /// Maximum of Rect
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
        /// <param name="Min">Minimum of Rect</param>
        /// <param name="Max">Maximum of Rect</param>
        public RectInt(in Vector2Int Min, in Vector2Int Max)
        {
            this.Min = Min;
            this.Max = Max;
        }

        /// <summary>
        /// Includes another Rect inside this rect
        /// </summary>
        /// <param name="other">Other Rect</param>
        public void Include(in RectInt other)
        {
            this.Min = Vector2Int.Min(this.Min, other.Min);
            this.Max = Vector2Int.Max(this.Max, other.Max);
        }
    }
}