using System;

namespace DeluMc.Masks
{
    /// <summary>
    /// Delta Map Utilities Class
    /// </summary>
    public static class DeltaMap
    {

        /// <summary>
        /// If the block position is considered acceptable
        /// </summary>
        /// <param name="deltaMap">Deltamap to test</param>
        /// <param name="z">Z position</param>
        /// <param name="x">X position</param>
        /// <returns>If the block position is considered acceptable</returns>
        public static bool IsAcceptableBlock(in float[][] deltaMap, int z, int x)
        {
            return 0 <= z && z < deltaMap.Length && 0 <= x && x < deltaMap[0].Length && 0 <= deltaMap[z][x] && deltaMap[z][x] <= kMaxDelta;
        }

        /// <summary>
        /// Maximum acceptable delta
        /// </summary>
        public const float kMaxDelta = 0.45f;

        /// <summary>
        /// Calculate Delta Map in a Position
        /// </summary>
        /// <param name="heightMap">Heightmap to use</param>
        /// <param name="waterMap">Watermap to use</param>
        /// <param name="deltaMap">Deltamap to generate </param>
        /// <param name="z">Z position</param>
        /// <param name="x">X position</param>
        public static void CalculateDeltaMap(in int[][] heightMap, in int[][] waterMap, float[][] deltaMap, int z, int x)
        {
            if (heightMap[z][x] < 0 || waterMap[z][x] == 1)
            {
                deltaMap[z][x] = -1;
                return;
            }

            int sizeZ = deltaMap.Length;
            int sizeX = deltaMap[0].Length;

            int count = 0;
            int posZ;
            int posX;

            for (int deltaZ = -1; deltaZ < 2; ++deltaZ)
            {
                for (int deltaX = -1; deltaX < 2; ++deltaX)
                {
                    posZ = z + deltaZ;
                    posX = x + deltaX;
                    if (0 <= posZ && posZ < sizeZ && 0 <= posX && posX < sizeX)
                    {
                        if (heightMap[posZ][posX] > -1 || waterMap[posZ][posX] != 1)
                        {
                            deltaMap[z][x] += (float)Math.Abs(heightMap[posZ][posX] - heightMap[z][x]);
                            ++count;
                        }

                    }
                }
            }

            deltaMap[z][x] /= (float)count;

        }
    }
}