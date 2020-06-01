using System;
using System.Collections.Generic;
using DeluMc.Utils;
using DeluMc.MCEdit;
using Utils.SpatialTrees.QuadTrees;

namespace DeluMc
{
    /// <summary>
    /// Distributes and Places Village Markers 
    /// </summary>
    public static class VillageDistributor
    {
        /// <summary>
        /// Percentage used to calculate minimum village distance between village seeds
        /// </summary>
        private const float kVillageSeparationPercentage = 0.1f;

        /// <summary>
        /// Backup Algorithm to place Villages
        /// </summary>
        /// <param name="acceptableMap">Acceptable Map</param>
        /// <param name="villageMap">Vilage Map</param>
        /// <param name="radius">Village Radius for placement</param>
        /// <param name="expectedVillageSize">Expected Village Size</param>
        /// <param name="villageCount">Expected amount of villages placed</param>
        /// <param name="numberOfTries">Number of Tries to place a new village</param>
        /// <returns>Village Markers Placed</returns>
        private static List<VillageMarker> BackUpPlacement(
            bool[][] acceptableMap, int[][] villageMap,
            int radius, int expectedVillageSize, int villageCount, int numberOfTries)
        {
            int zSize = acceptableMap.Length;
            int xSize = acceptableMap[0].Length;
            List<VillageMarker> villages = new List<VillageMarker>(villageCount);

            DataQuadTree<VillageMarker> placedVillages
                = new DataQuadTree<VillageMarker>(Vector2Int.Zero, new Vector2Int(zSize, xSize));

            Random rand = new Random();
            int minDistance = (int)((float)expectedVillageSize * kVillageSeparationPercentage);
            int z;
            int x;
            Vector2Int currPoint = new Vector2Int();
            DataQuadTree<VillageMarker>.DistanceToDataPoint closest;
            int id = 1;
            while (villageCount != 0 && numberOfTries != 0)
            {
                z = rand.Next(0, zSize);
                x = rand.Next(0, xSize);
                currPoint.Z = z;
                currPoint.X = x;
                closest = placedVillages.NearestNeighbor(currPoint);
                if (
                    acceptableMap[z][x] && villageMap[z][x] <= 0 
                    && (closest.DataNode == null || Vector2Int.Manhattan(closest.DataNode.Point, currPoint) > minDistance)
                )
                {
                    VillageMarker village = VillageMarkerPlacer.CreateVillage(acceptableMap, villageMap, z, x, expectedVillageSize, radius, id);
                    if (village.Points.Count >= expectedVillageSize / 2)
                    {
                        ++id;
                        --villageCount;
                        villages.Add(village);
                        placedVillages.Insert(village.Seed, village);
                    }
                    else
                    {
                        VillageMarkerPlacer.EliminateVillageMarker(village, villageMap);
                    }

                }
                --numberOfTries;
            }
            return villages;
        }

        /// <summary>
        /// Distribute Villages
        /// </summary>
        /// <param name="acceptableMap">Acceptable Map</param>
        /// <param name="villageMap">Village Map</param>
        /// <param name="waterAnalysis">Water Analysis to place villages near water</param>
        /// <param name="minVillageCount">Minimum Village Amount expected</param>
        /// <param name="numberOfTries">Number of Tries to place a new village</param>
        /// <param name="radius">Village Radius for placement</param>
        /// <param name="expectedVillageSize">Expected Village Size</param>
        /// <returns>Village Markers Placed</returns>
        public static List<VillageMarker> DistributeVillageMarkers(
            bool[][] acceptableMap, int[][] villageMap, WaterAnalyzer.WaterAnalysis waterAnalysis,
            int minVillageCount, int numberOfTries, int radius, int expectedVillageSize)
        {
            int possibleVillageSize = 0;
            {
                float possibleVillageSizeF = 0.0f;
                float minValidWaterSize = (float)waterAnalysis.MinValidWaterSize;
                for (int i = 0; i < waterAnalysis.WaterBodies.Count; i++)
                {
                    possibleVillageSizeF += (float)waterAnalysis.WaterBodies[i].Points.Count / minValidWaterSize;
                }
                possibleVillageSize = (int)possibleVillageSizeF;
            }

            if (possibleVillageSize < minVillageCount)
            {
                // Not enough Water for villages. Put random
                return BackUpPlacement(acceptableMap, villageMap, radius, expectedVillageSize, minVillageCount, numberOfTries);
            }

            // TODO: Temp
            return BackUpPlacement(acceptableMap, villageMap, radius, expectedVillageSize, possibleVillageSize, numberOfTries);
        }
    }
}