using System;
using DeluMc.MCEdit;
using DeluMc.MCEdit.Block;

namespace DeluMc.Masks
{
    public static class TreeMap
    {
        /// <summary>
        /// Expands a tree block one block around
        /// </summary>
        /// <param name="z">Block to expand Z coordinate</param>
        /// <param name="x">Block to expand X coordinate</param>
        /// <param name="treeMap">Tree map</param>
        public static void ExpandTreeBlock(int z, int x, int[][] treeMap)
        {
            if (treeMap[z][x] != 1)
                return;
            
            for (int dz = -1; dz < 2; ++dz)
            {
                if (z + dz < 0 || dz + z >= treeMap.Length)
                    continue;
                for (int dx = -1; dx < 2; ++dx)
                {
                    if (x + dx < 0 || dx + x >= treeMap[0].Length)
                        continue;
                    if (treeMap[z + dz][x + dx] == 0)
                        treeMap[z + dz][x + dx] = 2;
                }
            }
        }

        
        /// <summary>
        /// Checks if there is a tree in coordinate (z, x)
        /// </summary>
        /// <param name="z">Coordinate Z to check</param>
        /// <param name="x">Coordinate X to check</param>
        /// <param name="treeMap">Tree map</param>
        /// <returns>True if there is a tree/False otherwise</returns>
        public static bool IsTree(int z, int x, in int[][] treeMap)
        {
            return treeMap[z][x] >= 1;
        }

    }
}