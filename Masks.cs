using System;
using DeluMc.MCEdit;
using DeluMc.MCEdit.Block;

namespace DeluMc.Masks
{
    public static class HeightMap
    {

        /// <summary>
        /// If the block position is considered acceptable
        /// </summary>
        /// <param name="treeMap">Treemap to test</param>
        /// <param name="z">Z position</param>
        /// <param name="x">X position</param>
        /// <returns>If the block position is considered acceptable</returns>
        public static bool IsAcceptableTreeMapBlock(in int[][] treeMap, int z, int x)
        {
            return 0 <= z && z < treeMap.Length && 0 <= x && x < treeMap[0].Length && treeMap[z][x] != 2;
        }

        /// <summary>
        /// Iterates the box from [iz,ix] to [fz,fx] and calculates the
        /// real height in every (z,x) position of the box.
        /// </summary>
        /// <remarks>
        /// The box MUST contain the highest block of the heightmap.
        /// </remarks>
        /// <param name="box">Block box</param>
        /// <param name="heightmap">HeightMap to fix</param>
        /// <param name="treeMap"></param>
        /// <param name="iz">Initial z coordinate</param>
        /// <param name="ix">Initial x coordinate</param>
        /// <param name="fz">Final z coordinate</param>
        /// <param name="fx">Final z coordinate</param>
        public static void FixBoxHeights(Material[][][] box, int[][] heightmap, int[][] treeMap,
                                    int iz, int ix, int fz, int fx)
        {
            int leavesID = AlphaMaterials.OakLeaves_NoDecay_18_4.ID;
            int woodRootID = AlphaMaterials.JungleWood_East_West_17_7.ID;

            for (int i = iz; i < fz; ++i)
            {
                for (int j = ix; j < fx; ++j)
                {
                    int y = heightmap[i][j];
                    if (y >= 0)
                    {
                        Material block = box[y][i][j];
                        if (block.ID == leavesID || block.ID == woodRootID)
                        {
                            treeMap[i][j] = 1;
                            heightmap[i][j] = FindGroundHeight(box, heightmap, treeMap, i, j);
                        }
                        else
                        {
                            treeMap[i][j] = 0;
                        }
                    }
                    else
                    {
                        treeMap[i][j] = 0;
                    }
                }
            }
        }


        /// <summary>
        /// Updates heightMap[z][x] with the real highest ground position.
        /// It is usefull when the heightMap indicates a leaves block instead
        /// of the ground.
        /// </summary>
        /// <param name="box">Block blox</param>
        /// <param name="heightMap">HeightMap</param>
        /// <param name="z">Z pos to check</param>
        /// <param name="x">X pos to check</param>
        public static int FindGroundHeight(Material[][][] box, int[][] heightMap, int[][] treeMap, int z, int x)
        {
            int leavesID = AlphaMaterials.OakLeaves_NoDecay_18_4.ID;
            int woodRootID = AlphaMaterials.JungleWood_East_West_17_7.ID;
            int airID = AlphaMaterials.Air_0_0.ID;

            int y = heightMap[z][x];
            if (y == -1)
            {
                return -1;
            }
            
            Material block = box[y][z][x];

            // Replace for a IsTree function
            while ((block.ID == leavesID || block.ID == woodRootID || block.ID == airID) && y > -1)
            {
                if (block.ID == woodRootID && treeMap[z][x] != 2)
                    treeMap[z][x] = 2;
                // Maybe we must be carefull with what is considered ground in this case.
                y -= 1;
                if (y != -1)
                {
                    block = box[y][z][x];
                }
            }
            return y;
        }
    }

    public static class LavaMap
    {
        /// <summary>
        ///  Checks if a given material is some kind of lava
        /// </summary>
        /// <param name="m"> material to check </param>
        public static bool isLava(Material m)
        {
            int lavaId1 = 10;
            int lavaId2 = 11;

            return m.ID == lavaId1 || m.ID == lavaId2;
        }

    }
}