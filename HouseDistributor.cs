using System;
using System.Collections.Generic;
using DeluMc.Utils;
using DeluMc.MCEdit;
using DeluMc.Masks;
using DeluMc.MCEdit.Block;

namespace DeluMc
{
    public static class HouseDistributor
    {
        private const int MIN_HOUSE_SEPARATION = 10;
        private const int TERRAFORMING_TRESHOLD = 6;

        public static void Test(in float[][] deltaMap, in VillageMarker village, Material[][][] world)
        {
            DeltaMap.DeltaPair[] sortedDelta = DeltaMap.SortDeltaMap(deltaMap);
            List<RectInt> houseRects = new List<RectInt>();
        }


        /// <summary>
        /// Calculates an APROXIMATION of the amount of terraformation needed
        /// to place a house inside certain RectInt. 
        /// </summary>
        /// <param name="zone">Zone to aproximate</param>
        /// <param name="y">Y of the zone</param>
        /// <param name="world">Blocks of the world</param>
        /// <param name="heightMap">HeightMap of the world</param>
        /// <returns>
        /// Positive number indicating blocks to be modified/
        /// -1 if it is impossible to terraform the zone
        /// </returns>
        private static int CalculateTerraformation(in RectInt zone, int y, in Material[][][] world, in int[][] heightMap)
        {
            int c = 0;
            for (int i = 0; i < zone.Max.Z; ++i)
            {
                for (int j = 0; j < zone.Max.X; ++j)
                {
                    if (heightMap[i][j] < 0)
                    {
                        return -1;
                    }
                    else if (world[y][i][j] != AlphaMaterials.Air_0_0)
                    {
                        c += Math.Abs(heightMap[i][j] - y) + 1;
                    }
                    else if (y - 2 > 0)
                    {
                        if (world[y - 2][i][j] == AlphaMaterials.Air_0_0)
                        {
                            return -1;
                        }
                    }
                    else if (y - 1 > 0)
                    {
                        if (world[y - 1][i][j] == AlphaMaterials.Air_0_0)
                            ++c;
                    } 
                }
            }
            return c;
        }
    }
}
