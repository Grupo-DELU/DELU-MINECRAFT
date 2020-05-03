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
        public float PValue { get; private set; }

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
        /// <param name="seed"></param>
        /// <param name="pValue"></param>
        /// <param name="rect"></param>
        /// <param name="points"></param>
        public VillageMarker(in Vector2Int seed, float pValue, in RectInt rect, Vector2Int[] points)
        {
            this.Seed = seed;
            this.PValue = pValue;
            this.Rect = rect;
            this.Points = points;
        }
    }

    public static class VillageMarkerPlacer
    {

        private const int kExpectedCircles = 10;

        public static VillageMarker CreateVillage(bool[][] acceptableMap, int[][] villageMap, int z, int x, int maxNumNodes, int radius)
        {
            Vector2Int N = new Vector2Int { Z = z, X = x };
            List<Vector2Int> openCircles = new List<Vector2Int>(kExpectedCircles);
            openCircles.Add(N);
            List<Vector2Int> selectedNodes = new List<Vector2Int>(maxNumNodes);
            float pValue = 0.0f;

            RectInt coverRect = new RectInt(N, N);

            while (selectedNodes.Count < maxNumNodes)
            {
                openCircles = SelectNewCircles(acceptableMap, villageMap, openCircles, radius);

                if (openCircles.Count == 0)
                {
                    // Nothing to Add
                    break;
                }

                // TODO: Add SelectedNodes here. Intercept each node to all the rects formed around circles (kinda like rasterization)
            }


            return new VillageMarker(N, pValue, coverRect, selectedNodes.ToArray());
        }

        private static List<Vector2Int> SelectNewCircles(bool[][] acceptableMap, int[][] villageMap, List<Vector2Int> openCircles, int radius)
        {
            System.Diagnostics.Debug.Assert(acceptableMap.Length != 0 && acceptableMap[0].Length != 0);
            Mutex mut = new Mutex();
            List<Vector2Int> newCircles = new List<Vector2Int>();

            Parallel.For(0, openCircles.Count,
                   index =>
                   {
                       Vector2Int center = openCircles[index];
                       int zStart = Math.Max(0, center.Z - radius);
                       int zEnd = Math.Min(acceptableMap.Length, center.Z + radius + 1);
                       int xStart = Math.Max(0, center.X - radius);
                       int xEnd = Math.Min(acceptableMap[0].Length, center.X + radius + 1);

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

        private static void GetValidNodes(
            bool[][] acceptableMap, int[][] villageMap, int radius, List<Vector2Int> openCircles,
            in RectInt coverRect, List<Vector2Int> selectedNodes)
        {
            System.Diagnostics.Debug.Assert(openCircles.Count != 0);
            System.Diagnostics.Debug.Assert(acceptableMap.Length != 0 && acceptableMap[0].Length != 0);

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
                    zEnd = Math.Min(acceptableMap.Length, center.Z + radius);
                    xStart = Math.Max(0, center.X - radius);
                    xEnd = Math.Min(acceptableMap[0].Length, center.X + radius);
                    circlesBoxes.Add(new RectInt(new Vector2Int(zStart, xStart), new Vector2Int(zEnd, xEnd)));
                    testCover.Include(circlesBoxes[circlesBoxes.Count - 1]);
                }
            }


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

                for (int i = oldEnd; i < selectedNodes.Count; i++)
                {
                    coverRect.Include(selectedNodes[i]);
                }
            }
        }



    }
}