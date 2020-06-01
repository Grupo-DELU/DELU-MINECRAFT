using System;
using System.Collections.Generic;
using DeluMc.Utils;
using DeluMc.MCEdit;
using Utils.SpatialTrees.QuadTrees;
using Utils.Collections;

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
        /// For Distance Comparisons
        /// </summary>
        private class PointExt : ZPoint2D
        {
            /// <summary>
            /// Distance to Target
            /// </summary>
            public float Distance { get; set; } = 0.0f;
        }

        /// <summary>
        /// Comparers for Points based on distance to target
        /// </summary>
        private class CoordinatesBasedComparer : Comparer<PointExt>
        {
            /// <summary>
            /// Compare two points by distance to target
            /// </summary>
            /// <param name="lhs">Left Point</param>
            /// <param name="rhs">Right Point</param>
            /// <returns></returns>
            public override int Compare(PointExt lhs, PointExt rhs)
            {
                if (lhs.Distance == rhs.Distance)
                {
                    return 0;
                }
                else if (lhs.Distance < rhs.Distance)
                {
                    return -1;
                }

                return 1;
            }
        }


        /// <summary>
        /// Find Closest Point to Place a Seed starting for a point
        /// </summary>
        /// <param name="sZ">Z Coordinate of Starting Point</param>
        /// <param name="sX">x Coordinate of Starting Point</param>
        /// <param name="rectCover">Rect Cover for Map</param>
        /// <param name="heuristic">Heuristic to use</param>
        /// <param name="isGoal">Goal function</param>
        /// <returns>Point to Place Seed, if any</returns>
        private static PointExt FindVillagePosition(int sZ, int sX, in RectInt rectCover, Func<Vector2Int, float> heuristic, Func<Vector2Int, bool> isGoal)
        {
            Dictionary<PointExt, float> distances = new Dictionary<PointExt, float>();
            MinHeap<PointExt> priorityQueue = new MinHeap<PointExt>(new CoordinatesBasedComparer());

            PointExt startPoint = new PointExt { RealPoint = new Vector2Int(sZ, sX) };
            startPoint.Distance = heuristic(startPoint.RealPoint);

            distances.Add(startPoint, 0);
            priorityQueue.Add(startPoint);

            PointExt curr, child;
            int childZ, childX;
            float childDistance, currDistance;
            while (!priorityQueue.IsEmpty())
            {
                curr = priorityQueue.ExtractDominating();
                currDistance = distances[curr];

                if (float.IsInfinity(currDistance))
                {
                    /// Failed to find water
                    return null;
                }

                if (isGoal(curr.RealPoint))
                {
                    return curr;
                }

                for (int dz = -1; dz <= 1; dz++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        childZ = curr.RealPoint.Z + dz;
                        childX = curr.RealPoint.X + dx;

                        if (rectCover.IsInside(childZ, childX))
                        {
                            child = new PointExt { RealPoint = new Vector2Int(childZ, childX) };
                            childDistance = currDistance + Vector2Int.Manhattan(curr.RealPoint, child.RealPoint);
                            child = new PointExt { RealPoint = new Vector2Int(childZ, childX), Distance = childDistance + heuristic(child.RealPoint) };
                            if (distances.TryGetValue(child, out float currVal))
                            {
                                if (childDistance < currVal)
                                {
                                    distances[child] = childDistance;
                                    priorityQueue.Add(child);
                                }
                            }
                            else
                            {
                                if (!float.IsInfinity(child.Distance))
                                {
                                    distances[child] = childDistance;
                                    priorityQueue.Add(child);
                                }
                            }
                        }
                    }
                }

            }
            return null;
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
            System.Diagnostics.Debug.Assert(acceptableMap.Length > 0 && acceptableMap[0].Length > 0);
            int possibleVillageCount = 0;
            {
                float possibleVillageSizeF = 0.0f;
                float minValidWaterSize = (float)waterAnalysis.MinValidWaterSize;
                for (int i = 0; i < waterAnalysis.WaterBodies.Count; i++)
                {
                    possibleVillageSizeF += (float)waterAnalysis.WaterBodies[i].Points.Count / minValidWaterSize;
                }
                possibleVillageCount = (int)possibleVillageSizeF;
            }

            if (possibleVillageCount < minVillageCount)
            {
                // Not enough Water for villages. Put random
                return BackUpPlacement(acceptableMap, villageMap, radius, expectedVillageSize, minVillageCount, numberOfTries);
            }
            System.Diagnostics.Debug.Assert(waterAnalysis.WaterBodies.Count > 0);
            int minDistance = (int)((float)expectedVillageSize * kVillageSeparationPercentage);

            int zSize = acceptableMap.Length;
            int xSize = acceptableMap[0].Length;
            List<VillageMarker> villages = new List<VillageMarker>(possibleVillageCount);

            DataQuadTree<VillageMarker> placedVillages
                = new DataQuadTree<VillageMarker>(Vector2Int.Zero, new Vector2Int(zSize, xSize));

            RectInt rectCover = new RectInt(Vector2Int.Zero, new Vector2Int(acceptableMap.Length - 1, acceptableMap[0].Length - 1));

            float totalWaterBodySize = 0.0f;
            for (int i = 0; i < waterAnalysis.WaterBodies.Count; i++)
            {
                totalWaterBodySize += (float)waterAnalysis.WaterBodies[i].Points.Count;
            }

            Func<Vector2Int, float> heuristic = (Vector2Int point) =>
            {

                if (!acceptableMap[point.Z][point.X])
                {
                    return float.PositiveInfinity;
                }

                DataQuadTree<VillageMarker>.DistanceToDataPoint nearest = placedVillages.NearestNeighbor(point);
                if (nearest.DataNode != null && Vector2Int.Manhattan(nearest.DataNode.Point, point) < minDistance)
                {
                    return float.PositiveInfinity;
                }

                float h = 0.0f;
                for (int i = 0; i < waterAnalysis.WaterBodies.Count; i++)
                {
                    h += (waterAnalysis.WaterBodies[i].Points.Count
                        * Vector2Int.Manhattan(waterAnalysis.WaterBodies[i].PointsQT.NearestNeighbor(point).DataNode.Point, point))
                        / totalWaterBodySize;
                }

                return h;
            };

            Func<Vector2Int, bool> isGoal = (Vector2Int point) =>
            {
                for (int i = 0; i < waterAnalysis.WaterBodies.Count; i++)
                {
                    if (Vector2Int.Manhattan(waterAnalysis.WaterBodies[i].PointsQT.NearestNeighbor(point).DataNode.Point, point) <= minDistance)
                    {
                        return true;
                    }
                }
                return false;
            };

            Random rand = new Random();
            int z;
            int x;
            PointExt possibleSeed;

            while (possibleVillageCount != 0 && numberOfTries != 0)
            {
                z = rand.Next(0, zSize);
                x = rand.Next(0, xSize);
                possibleSeed = FindVillagePosition(z, x, rectCover, heuristic, isGoal);
                if (possibleSeed != null)
                {
                    VillageMarker village = VillageMarkerPlacer.CreateVillage(acceptableMap, villageMap, z, x, expectedVillageSize, radius);
                    if (village.Points.Length >= expectedVillageSize / 2)
                    {
                        --possibleVillageCount;
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
    }
}