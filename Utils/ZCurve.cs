namespace DeluMc.Utils
{
    /// <summary>
    /// Z Curve Numbers (Morton Numbers)
    /// </summary>
    public static class ZCurve
    {
        /// <summary>
        /// Converts a 10 bit integer to an 3 bit interleaved version 
        /// From https://stackoverflow.com/a/1024889
        /// </summary>
        /// <param name="x">A 10 bit integer (less or equal to 1023)</param>
        /// <returns>3 bit interleaved integer</returns>
        public static uint VariableTo3BitSpacing(uint x)
        {
            System.Diagnostics.Debug.Assert(0x00003FF >= x); // x must be of less than 10 bits
            x = (x | (x << 16)) & 0x030000FF;
            x = (x | (x << 8)) & 0x0300F00F;
            x = (x | (x << 4)) & 0x030C30C3;
            x = (x | (x << 2)) & 0x09249249;
            return x;
        }

        /// <summary>
        /// Converts a 16 bit integer to an 3 bit interleaved version 
        /// From https://stackoverflow.com/a/14853492
        /// </summary>
        /// <param name="x">A 16 bit integer (less or equal to 65535)</param>
        /// <returns>2 bit interleaved integer</returns>
        public static uint VariableTo2BitSpacing(uint x)
        {
            System.Diagnostics.Debug.Assert(0x000FFFF >= x); // x must be of less than 16 bits
            x = (x | (x << 8)) & 0x00FF00FF;
            x = (x | (x << 4)) & 0x0F0F0F0F;
            x = (x | (x << 2)) & 0x33333333;
            x = (x | (x << 1)) & 0x55555555;
            return x;
        }

        /// <summary>
        /// Convert 3 10 bit integers (less or equal to 1023) to a Z Curve Number (Morton Number)
        /// </summary>
        /// <param name="y">A 10 bit integer</param>
        /// <param name="z">A 10 bit integer</param>
        /// <param name="x">A 10 bit integer</param>
        /// <returns>Z Curve Number (Morton Number)</returns>
        public static uint ZCurve3DPos(uint y, uint z, uint x)
        {
            return VariableTo3BitSpacing(y) | (VariableTo3BitSpacing(z) << 1) | (VariableTo3BitSpacing(x) << 2);
        }

        /// <summary>
        /// Convert 3 16 bit integers (less or equal to 1023) to a Z Curve Number (Morton Number)
        /// </summary>
        /// <param name="z">A 16 bit integer</param>
        /// <param name="x">A 16 bit integer</param>
        /// <returns>Z Curve Number (Morton Number)</returns>
        public static uint ZCurve2DPos(uint z, uint x)
        {
            return VariableTo2BitSpacing(z) | (VariableTo2BitSpacing(x) << 1);
        }
    }
}