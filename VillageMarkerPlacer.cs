using DeluMc.Utils;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
namespace DeluMc
{
    /// <summary>
    /// Village Marker
    /// </summary>
    public class VillageMarker
    {
        /// <summary>
        /// Seed of the Village
        /// </summary>
        public Vector2Int Seed { get; private set; }

        /// <summary>
        /// Probability of Success of Seed
        /// It is the sum of the distance from the seed to all the nodes in the Village
        /// </summary>
        public int PValue { get; private set; }

        /// <summary>
        /// Rect that covers the village
        /// </summary>
        public RectInt Rect { get; private set; }

        /// <summary>
        /// Points belonging to the village
        /// </summary>
        public Vector2Int[] Points { get; private set; }

        /// <summary>
        /// Creates a Village Marker
        /// </summary>
        /// <param name="seed">Seed of Vialle</param>
        /// <param name="pValue">Probability of Success Value</param>
        /// <param name="rect">Rect that covers the village</param>
        /// <param name="points">Points Belonging to the village</param>
        public VillageMarker(in Vector2Int seed, int pValue, in RectInt rect, Vector2Int[] points)
        {
            this.Seed = seed;
            this.PValue = pValue;
            this.Rect = rect;
            this.Points = points;
        }
    }

    /// <summary>
    /// Village Marker Placer Algorithm Class
    /// </summary>
    public static class VillageMarkerPlacer
    {
        /// <summary>
        /// If the the point is part of a village
        /// </summary>
        /// <param name="villageMap">Villages Map</param>
        /// <param name="z">Z Coordinate</param>
        /// <param name="x">X Coordinate</param>
        /// <returns></returns>
        public static bool IsVillage(int[][] villageMap, int z, int x)
        {
            return villageMap[z][x] == 1;
        }

        /// <summary>
        /// Expected number of circles to be found
        /// </summary>
        private const int kExpectedCircles = 10;

        /// <summary>
        /// Create a new Village Marker
        /// </summary>
        /// <param name="acceptableMap">Acceptable Nodes map</param>
        /// <param name="villageMap">Villages Map</param>
        /// <param name="z">Z Coordinate for Seed of Village</param>
        /// <param name="x">X Coordinate of Village</param>
        /// <param name="maxNumNodes">Maximum Number of Nodes expected to be part of the village (there might be more or less)</param>
        /// <param name="radius">Radius for Circle Generation and Collection</param>
        /// <returns>Village Marker</returns>
        public static VillageMarker CreateVillage(bool[][] acceptableMap, int[][] villageMap, int z, int x, int maxNumNodes, int radius)
        {
            Vector2Int seed = new Vector2Int { Z = z, X = x };
            List<Vector2Int> openCircles = new List<Vector2Int>(kExpectedCircles);
            openCircles.Add(seed);
            List<Vector2Int> selectedNodes = new List<Vector2Int>(maxNumNodes);
            int pValue = 0;
            int prevSelectedNodesCount = -1;

            RectInt coverRect = new RectInt(seed, seed);

            while (selectedNodes.Count < maxNumNodes && prevSelectedNodesCount != selectedNodes.Count)
            {
                openCircles = OpenNewCircles(acceptableMap, villageMap, openCircles, radius);

                if (openCircles.Count == 0)
                {
                    // Nothing to Add
                    break;
                }

                prevSelectedNodesCount = selectedNodes.Count;
                pValue += GetValidNodes(acceptableMap, villageMap, radius, seed, openCircles, coverRect, selectedNodes);
            }

            return new VillageMarker(seed, pValue, coverRect, selectedNodes.ToArray());
        }

        /// <summary>
        /// Open new Circles for Village generation
        /// </summary>
        /// <param name="acceptableMap">Acceptable Nodes map</param>
        /// <param name="villageMap">Villages Map</param>
        /// <param name="openCircles">Currently open circles</param>
        /// <param name="radius">Circles Radius</param>
        /// <returns>Newly opened circles</returns>
        private static List<Vector2Int> OpenNewCircles(bool[][] acceptableMap, int[][] villageMap, List<Vector2Int> openCircles, int radius)
        {
            System.Diagnostics.Debug.Assert(acceptableMap.Length != 0 && acceptableMap[0].Length != 0);
            Mutex mut = new Mutex();
            List<Vector2Int> newCircles = new List<Vector2Int>();

            Parallel.For(0, openCircles.Count,
                index =>
                {
                    Vector2Int center = openCircles[index];
                    int zStart = Math.Max(0, center.Z - radius);
                    int zEnd = Math.Min(acceptableMap.Length - 1, center.Z + radius);
                    int xStart = Math.Max(0, center.X - radius);
                    int xEnd = Math.Min(acceptableMap[0].Length - 1, center.X + radius);

                    List<Vector2Int> maxNodes = new List<Vector2Int>();
                    int maxDist = 0;
                    int curDist;

                    /// TODO: This is the naive way to do it. A better way would be like peeling an onion, do tat later
                    for (int z = zStart; z < zEnd; z++)
                    {
                        for (int x = xStart; x < xEnd; x++)
                        {
                            if (acceptableMap[z][x] && villageMap[z][x] <= 0)
                            {
                                curDist = ChebyshevDistance(center, z, x);
                                if (maxDist < curDist)
                                {
                                    if (maxNodes.Count > 0)
                                    {
                                        maxNodes.RemoveRange(0, maxNodes.Count);
                                    }
                                    maxDist = curDist;
                                    maxNodes.Add(new Vector2Int(z, x));
                                }
                                else if (maxDist == curDist)
                                {
                                    maxNodes.Add(new Vector2Int(z, x));
                                }
                            }
                        }
                    }

                    if (maxNodes.Count > 0 && maxDist > 0)
                    {
                        mut.WaitOne();
                        try
                        {
                            newCircles.AddRange(maxNodes);
                        }
                        finally
                        {
                            mut.ReleaseMutex();
                        }
                    }
                });

            mut.Dispose();
            return newCircles;
        }

        /// <summary>
        /// Calculate the Chebyshev Distance between two points
        /// Circles are squares under this metric
        /// https://en.wikipedia.org/wiki/Chebyshev_distance
        /// </summary>
        /// <param name="from">Starting Point</param>
        /// <param name="toZ">Ending Point Z Coordinate</param>
        /// <param name="toX">Ending Point X Coordinate</param>
        /// <returns>The Chebyshev Distance between the two points</returns>
        private static int ChebyshevDistance(in Vector2Int from, int toZ, int toX)
        {
            return Math.Max(Math.Abs(from.Z - toZ), Math.Abs(from.X - toX));
        }

        /// <summary>
        /// Minimum circle interceptions required to add a new node
        /// </summary>
        private const int kMinCircleInterceptions = 3;

        /// <summary>
        /// Get Valid Nodes from Open Circles
        /// </summary>
        /// <param name="acceptableMap">Acceptable Nodes map</param>
        /// <param name="villageMap">Villages Map</param>
        /// <param name="radius">Circles Radius</param>
        /// <param name="seed">Seed of Village</param>
        /// <param name="openCircles">Currently open circles</param>
        /// <param name="coverRect">Cover Rect for the village being generated</param>
        /// <param name="selectedNodes">Selected Nodes for the village</param>
        /// <returns>PValue of added nodes to Village</returns>
        private static int GetValidNodes(
            bool[][] acceptableMap, int[][] villageMap, int radius, in Vector2Int seed, List<Vector2Int> openCircles,
            in RectInt coverRect, List<Vector2Int> selectedNodes)
        {
            System.Diagnostics.Debug.Assert(openCircles.Count != 0);
            System.Diagnostics.Debug.Assert(acceptableMap.Length != 0 && acceptableMap[0].Length != 0);

            /// <summary>
            /// Cover Rect for Current Open Circles
            /// </summary>
            RectInt testCover = new RectInt(openCircles[0]);
            List<RectInt> circlesBoxes = new List<RectInt>(openCircles.Count);

            {
                // Create circle boxes for intersections
                // Fill testCover to iterate over it
                int zStart;
                int zEnd;
                int xStart;
                int xEnd;
                for (int i = 0; i < openCircles.Count; i++)
                {
                    Vector2Int center = openCircles[i];
                    zStart = Math.Max(0, center.Z - radius);
                    zEnd = Math.Min(acceptableMap.Length - 1, center.Z + radius);
                    xStart = Math.Max(0, center.X - radius);
                    xEnd = Math.Min(acceptableMap[0].Length - 1, center.X + radius);
                    circlesBoxes.Add(new RectInt(new Vector2Int(zStart, xStart), new Vector2Int(zEnd, xEnd)));
                    testCover.Include(circlesBoxes[circlesBoxes.Count - 1]);
                }
            }

            /// <summary>
            /// Size of Cover Rect for Current Open Circles
            /// </summary>
            Vector2Int testCoverSize = testCover.Size;

            {
                // Clean Village Map of unused places
                Tasker.WorkBlock[] cleanVillageMap = {
                    (int z, int x) =>
                    {
                        // Originaly is relative to min of cover box
                        z += testCover.Min.Z;
                        x += testCover.Min.X;

                        if (acceptableMap[z][x] && villageMap[z][x] < 0)
                        {
                            villageMap[z][x] = 0;
                        }
                    }
                };

                Tasker.Run2DTasks(testCoverSize.Z, testCoverSize.X, null, cleanVillageMap);
            }

            int newPValue = 0;
            {
                int oldEnd = selectedNodes.Count;
                Mutex mut = new Mutex();
                Tasker.WorkChunk[] getSelected =
                {
                    (int zStart, int zEnd, int xStart, int xEnd) =>
                    {
                        // Originaly is relative to min of cover box
                        zStart += testCover.Min.Z;
                        zEnd += testCover.Min.Z;
                        xStart += testCover.Min.X;
                        xEnd += testCover.Min.X;
                        List<Vector2Int> newSelected = new List<Vector2Int>();

                        for (int z = zStart; z < zEnd; z++)
                        {
                            for (int x = xStart; x < xEnd; x++)
                            {
                                if (acceptableMap[z][x] && villageMap[z][x] <= 0)
                                {
                                    for (int i = 0; i < circlesBoxes.Count; i++)
                                    {
                                        if (circlesBoxes[i].IsInside(z, x))
                                        {
                                            --villageMap[z][x];
                                            if (-kMinCircleInterceptions == villageMap[z][x])
                                            {
                                                villageMap[z][x] = 1;
                                                newSelected.Add(new Vector2Int(z, x));
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        if (newSelected.Count > 0)
                        {
                            mut.WaitOne();
                            selectedNodes.AddRange(newSelected);
                            mut.ReleaseMutex();
                        }
                    }
                };

                Tasker.Run2DTasks(testCoverSize.Z, testCoverSize.X, getSelected, null);

                mut.Dispose();

                for (int i = oldEnd; i < selectedNodes.Count; i++)
                {
                    coverRect.Include(selectedNodes[i]);
                    newPValue += ChebyshevDistance(seed, selectedNodes[i].Z, selectedNodes[i].X);
                }
            }
            return newPValue;
        }

        /// <summary>
        /// Eliminates a Village Marker from the Village Map
        /// </summary>
        /// <param name="village">Village to eliminate</param>
        /// <param name="villageMap">Current Village Map</param>
        public static void EliminateVillageMarker(VillageMarker village, int[][] villageMap)
        {
            Parallel.For(0, village.Points.Length,
                index =>
                {
                    villageMap[village.Points[index].Z][village.Points[index].X] = 0;
                }
            );
        }
    }
}