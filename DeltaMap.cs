using System;
using System.Collections;
using System.Threading.Tasks;
using System.Collections.Generic;
using DeluMc.Utils;

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


        /// <summary>
        /// Creates an array with DeltaPair structs, sorted by
        /// delta map value from lowest to highest (with absolute value)
        /// </summary>
        /// <param name="deltaMap">DeltaMap to sort</param>
        /// <returns>Array with DeltaPairs sorted by delta map value</returns>
        public static DeltaPair[] SortRectDelta(float[][] deltaMap, RectInt rect)
        {
            Console.WriteLine($"{rect.Size.X} x {rect.Size.Z} = {rect.Size.X * rect.Size.Z}");
            DeltaPair[] ordered = new DeltaPair[rect.Size.X * rect.Size.Z];
            // Lo de abajo fallaba y estaba preocupado por otras cosas
            for (int i = 0; i < rect.Size.Z; ++i)
            {
                for (int j = 0; j < rect.Size.X; ++j)
                {
                    ordered[i * rect.Size.X + j] = new DeltaPair(deltaMap[rect.Min.Z + i][rect.Min.X + j],
                                                       new Vector2Int(rect.Min.Z + i, rect.Min.X + j));
                }
            }
            //Parallel.For(0, n, pos =>
            //{
            //    int i = pos % n;
            //    int j = pos / n;
            //    ordered[i + j] = new DeltaPair(deltaMap[rect.Min.Z + i][rect.Min.X + j], 
            //                                   new Vector2Int(rect.Min.Z + i, rect.Min.X + j));
            //}
            //);
            Array.Sort(ordered, new DeltaPairComparer());
            Console.WriteLine("Nro ordered: " + rect.Size.X * rect.Size.Z);
            return ordered;
        }


        /// <summary>
        /// Struct that represents a coordinate and it associated deltamap
        /// value
        /// </summary>
        public struct DeltaPair
        {
            public float delta;
            public Vector2Int coordinates;

            public DeltaPair(float delta, Vector2Int coordinates)
            {
                this.delta = delta;
                this.coordinates = coordinates;
            }
        }


        /// <summary>
        /// Used to compare Delta Pairs by delta amount from min to max 
        /// </summary>
        private class DeltaPairComparer : IComparer<DeltaPair>
        {
            public int Compare(DeltaPair a, DeltaPair b)
            {
                return Math.Abs(a.delta).CompareTo(Math.Abs(b.delta));
            }
        }
    }
}
