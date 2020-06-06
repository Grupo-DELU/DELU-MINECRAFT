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
        public List<Vector2Int> Points { get; private set; }

        /// <summary>
        /// Village Id
        /// </summary>
        public int ID;

        /// <summary>
        /// Creates a Village Marker
        /// </summary>
        /// <param name="seed">Seed of Vialle</param>
        /// <param name="pValue">Probability of Success Value</param>
        /// <param name="rect">Rect that covers the village</param>
        /// <param name="points">Points Belonging to the village</param>
        /// <param name="id">Village Id</param>
        public VillageMarker(in Vector2Int seed, int pValue, in RectInt rect, List<Vector2Int> points, int id)
        {
            this.Seed = seed;
            this.PValue = pValue;
            this.Rect = rect;
            this.Points = points;
            this.ID = id;

#if DEBUG
            HashSet<ZPoint2D> duplicates = new HashSet<ZPoint2D>(points.Count);
            int duplicatesNum = 0;
            for (int i = 0; i < points.Count; i++)
            {
                if (!duplicates.Add(new ZPoint2D { RealPoint = points[i] }))
                {
                    ++duplicatesNum;
                }
            }
            if (duplicatesNum > 0)
            {
                Console.WriteLine($"Duplicates {duplicatesNum}");

            }
            System.Diagnostics.Debug.Assert(duplicatesNum == 0);
#endif
        }

        public void VillageFiller(in int[][] villagemap, in bool[][] acceptablemap)
        {
            int[][] extmap = new int[villagemap.Length][];
            for (int i = 0; i < villagemap.Length; ++i)
                extmap[i] = new int[villagemap[0].Length];


            for (int i = 0; i < villagemap.Length; ++i)
            {
                Vector2Int leftZ = Rect.Min + new Vector2Int(i, 0);
                Vector2Int rightZ = Rect.Min + new Vector2Int(i, villagemap[0].Length - 1);

                //Console.WriteLine("(" + i + ",0)");
                //Console.WriteLine($"({i},{villagemap[0].Length - 1})");
                if (extmap[i][0] != 1 && villagemap[i][0] <= 0)
                    ExternalBFSFiller(villagemap, extmap, leftZ);
                if (extmap[i][villagemap[0].Length - 1] != 1 && villagemap[i][villagemap[0].Length - 1] <= 0)
                    ExternalBFSFiller(villagemap, extmap, rightZ);
            }
            for (int i = 1; i < villagemap[0].Length - 1; ++i)
            {
                Vector2Int bottomZ = Rect.Min + new Vector2Int(0, i);
                Vector2Int topZ = Rect.Min + new Vector2Int(villagemap.Length - 1, i);
                if (extmap[0][i] != 1 && villagemap[0][i] <= 0)
                    ExternalBFSFiller(villagemap, extmap, bottomZ);
                if (extmap[villagemap.Length - 1][i] != 1 && villagemap[villagemap.Length - 1][i] <= 0)
                    ExternalBFSFiller(villagemap, extmap, topZ);
            }

            for (int i = 0; i < villagemap.Length; ++i)
            {
                for (int j = 0; j < villagemap[0].Length; ++j)
                {
                    Vector2Int point = new Vector2Int(i, j);
                    if (extmap[point.Z][point.X] == 0 && villagemap[point.Z][point.X] <= 0 && acceptablemap[point.Z][point.X])
                    {
                        InternalBFSFiller(villagemap, extmap, acceptablemap, point);
                    }
                }
            }
            //Console.WriteLine("Salimos!");
        }

        private void ExternalBFSFiller(in int[][] villagemap, int[][] extmap, in Vector2Int start)
        {
            //Console.WriteLine("External");
            //Console.WriteLine($"ExternMap en: ({start.Z},{start.X})");
            Queue<Vector2Int> open = new Queue<Vector2Int>();
            HashSet<Vector2Int> closed = new HashSet<Vector2Int>();
            open.Enqueue(start);

            while (open.Count > 0)
            {
                Vector2Int current = open.Dequeue();
                closed.Add(current);
                LinkedList<Vector2Int> neighboors = ExtNeighboors(villagemap, current);
                //Console.WriteLine($"Current local: ({current.Z - Rect.Min.Z},{current.X - Rect.Min.X})");
                extmap[current.Z - Rect.Min.Z][current.X - Rect.Min.X] = 1;
                //Points.Add(current);
                foreach (Vector2Int n in neighboors)
                {
                    if (villagemap[n.Z][n.X] <= 0 && extmap[n.Z - Rect.Min.Z][n.X - Rect.Min.X] == 0)
                    {
                        open.Enqueue(n);
                    }
                }
            }
        }


        private void InternalBFSFiller(in int[][] villagemap, in int[][] extmap, in bool[][] acceptablemap, in Vector2Int start)
        {
            //Console.WriteLine("Internal");
            Queue<Vector2Int> open = new Queue<Vector2Int>();
            HashSet<Vector2Int> closed = new HashSet<Vector2Int>();
            open.Enqueue(start);

            while (open.Count > 0)
            {
                Vector2Int current = open.Dequeue();
                closed.Add(current);
                LinkedList<Vector2Int> neighboors = ExtNeighboors(villagemap, current);
                villagemap[current.Z][current.X] = ID;
                Points.Add(current);
                foreach (Vector2Int n in neighboors)
                {
                    if (villagemap[n.Z][n.X] <= 0 && acceptablemap[n.Z][n.X] && extmap[n.Z - Rect.Min.Z][n.X - Rect.Min.X] == 0)
                    {
                        open.Enqueue(n);
                    }
                }
            }
        }

        private LinkedList<Vector2Int> ExtNeighboors(in int[][] villagemap, in Vector2Int point)
        {
            //Console.WriteLine($"Min: ({this.Rect.Min.Z},{this.Rect.Min.X})");
            //Console.WriteLine($"Max: ({this.Rect.Max.Z},{this.Rect.Max.X})");
            LinkedList<Vector2Int> neighboors = new LinkedList<Vector2Int>();
            for (int i = -1; i <= 1; ++i)
            {
                for (int j = -1; j <= 1; ++j)
                {
                    Vector2Int n = new Vector2Int(point.Z + i, point.X + j);
                    //Console.WriteLine($"Vecino: ({n.Z},{n.X})");
                    if (n == point)
                        continue;

                    if (Rect.IsInside(n))
                    {
                        neighboors.AddLast(n);
                    }
                }
            }
            //Console.WriteLine("nro vecinos: " + neighboors.Count);
            return neighboors;
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
        /// <param name="id">Village Id, Must be greater than 0</param>
        /// <returns>Village Marker</returns>
        public static VillageMarker CreateVillage(bool[][] acceptableMap, int[][] villageMap, int z, int x, int maxNumNodes, int radius, int id)
        {
            System.Diagnostics.Debug.Assert(id > 0);
            Vector2Int seed = new Vector2Int { Z = z, X = x };
            List<ZPoint2D> openCircles = new List<ZPoint2D>(kExpectedCircles);
            HashSet<ZPoint2D> openCirclesSet = new HashSet<ZPoint2D>(kExpectedCircles);
            List<ZPoint2D> newCircles = new List<ZPoint2D>(kExpectedCircles);
            newCircles.Add(new ZPoint2D { RealPoint = seed });
            List<Vector2Int> selectedNodes = new List<Vector2Int>(maxNumNodes);
            int pValue = 0;
            int prevSelectedNodesCount = -1;
            int addedCircles;

            RectInt coverRect = new RectInt(seed, seed);

            /// <summary>
            /// Cover Rect for Current Open Circles
            /// </summary>
            RectInt circleTestCover = new RectInt();
            bool coverInit = false;
            List<RectInt> circlesTestCovers = new List<RectInt>(openCircles.Count);
            int zStartCover;
            int zEndCover;
            int xStartCover;
            int xEndCover;
            ZPoint2D circleCenter;

            while (selectedNodes.Count < maxNumNodes && prevSelectedNodesCount != selectedNodes.Count)
            {
                newCircles = OpenNewCircles(acceptableMap, villageMap, newCircles, radius);

                addedCircles = 0;
                for (int i = 0; i < newCircles.Count; i++)
                {
                    if (openCirclesSet.Add(newCircles[i]))
                    {
                        circleCenter = newCircles[i];
                        zStartCover = Math.Max(0, circleCenter.RealPoint.Z - radius);
                        zEndCover = Math.Min(acceptableMap.Length - 1, circleCenter.RealPoint.Z + radius);
                        xStartCover = Math.Max(0, circleCenter.RealPoint.X - radius);
                        xEndCover = Math.Min(acceptableMap[0].Length - 1, circleCenter.RealPoint.X + radius);
                        circlesTestCovers.Add(new RectInt(new Vector2Int(zStartCover, xStartCover), new Vector2Int(zEndCover, xEndCover)));
                        if (coverInit)
                        {
                            circleTestCover.Include(circlesTestCovers[circlesTestCovers.Count - 1]);
                        }
                        else
                        {
                            circleTestCover = circlesTestCovers[circlesTestCovers.Count - 1];
                            coverInit = true;
                        }

                        openCircles.Add(newCircles[i]);
                        ++addedCircles;
                    }
                }

                if (addedCircles == 0)
                {
                    // Nothing to Add
                    break;
                }

                prevSelectedNodesCount = selectedNodes.Count;
                pValue += GetValidNodes(acceptableMap, villageMap, seed, circlesTestCovers, circleTestCover, ref coverRect, selectedNodes, id);
            }
            return new VillageMarker(seed, pValue, coverRect, selectedNodes, id);
        }

        /// <summary>
        /// Open new Circles for Village generation
        /// </summary>
        /// <param name="acceptableMap">Acceptable Nodes map</param>
        /// <param name="villageMap">Villages Map</param>
        /// <param name="openCircles">Currently open circles</param>
        /// <param name="radius">Circles Radius</param>
        /// <returns>Newly opened circles</returns>
        private static List<ZPoint2D> OpenNewCircles(bool[][] acceptableMap, int[][] villageMap, List<ZPoint2D> openCircles, int radius)
        {
            System.Diagnostics.Debug.Assert(acceptableMap.Length != 0 && acceptableMap[0].Length != 0);
            Mutex mut = new Mutex();
            HashSet<ZPoint2D> newCirclesSet = new HashSet<ZPoint2D>();

            Parallel.For<HashSet<ZPoint2D>>(0, openCircles.Count,
                () => new HashSet<ZPoint2D>(),
                (int index, ParallelLoopState loop, HashSet<ZPoint2D> accumulator) =>
                {
                    ZPoint2D center = openCircles[index];
                    int zStart = Math.Max(0, center.RealPoint.Z - radius);
                    int zEnd = Math.Min(acceptableMap.Length - 1, center.RealPoint.Z + radius);
                    int xStart = Math.Max(0, center.RealPoint.X - radius);
                    int xEnd = Math.Min(acceptableMap[0].Length - 1, center.RealPoint.X + radius);

                    List<ZPoint2D> maxNodes = new List<ZPoint2D>(4);
                    int maxDist = 0;
                    int curDist;

                    /// TODO: This is the naive way to do it. A better way would be like peeling an onion, do that later
                    for (int z = zStart; z < zEnd; z++)
                    {
                        for (int x = xStart; x < xEnd; x++)
                        {
                            if (acceptableMap[z][x] && villageMap[z][x] <= 0)
                            {
                                curDist = ChebyshevDistance(center.RealPoint, z, x);
                                if (maxDist < curDist)
                                {
                                    if (maxNodes.Count > 0)
                                    {
                                        maxNodes.Clear();
                                    }
                                    maxDist = curDist;
                                    maxNodes.Add(new ZPoint2D { RealPoint = new Vector2Int(z, x) });
                                }
                                else if (maxDist == curDist)
                                {
                                    maxNodes.Add(new ZPoint2D { RealPoint = new Vector2Int(z, x) });
                                }
                            }
                        }
                    }

                    if (maxNodes.Count > 0 && maxDist > 0)
                    {
                        for (int i = 0; i < maxNodes.Count; i++)
                        {
                            accumulator.Add(maxNodes[i]);
                        }

                        return accumulator;

                    }
                    return accumulator;
                },
                (HashSet<ZPoint2D> accumulator) =>
                {
                    mut.WaitOne();
                    try
                    {
                        foreach (ZPoint2D point in accumulator)
                        {
                            newCirclesSet.Add(point);
                        }
                    }
                    finally
                    {
                        mut.ReleaseMutex();
                    }
                });

            mut.Dispose();

            List<ZPoint2D> newCircles = new List<ZPoint2D>(newCirclesSet.Count);
            foreach (ZPoint2D point in newCirclesSet)
            {
                newCircles.Add(point);
            }

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
        /// <param name="seed">Village's Seed</param>
        /// <param name="circlesCovers">Covers of currently open circles</param>
        /// <param name="testCover">Cover of all open circles</param>
        /// <param name="coverRect">Cover Rect for the village being generated</param>
        /// <param name="selectedNodes">Selected Nodes for the village</param>
        /// <param name="id">Village ID, must be greater than 0</param>
        /// <returns>PValue of added nodes to Village</returns>
        private static int GetValidNodes(
            bool[][] acceptableMap, int[][] villageMap, in Vector2Int seed, List<RectInt> circlesCovers, RectInt testCover,
            ref RectInt coverRect, List<Vector2Int> selectedNodes, int id)
        {
            System.Diagnostics.Debug.Assert(id > 0);
            System.Diagnostics.Debug.Assert(circlesCovers.Count != 0);
            System.Diagnostics.Debug.Assert(acceptableMap.Length != 0 && acceptableMap[0].Length != 0);


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
                                    for (int i = 0; i < circlesCovers.Count; i++)
                                    {
                                        if (circlesCovers[i].IsInside(z, x))
                                        {
                                            --villageMap[z][x];
                                            if (-kMinCircleInterceptions == villageMap[z][x])
                                            {
                                                villageMap[z][x] = id;
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
            Parallel.For(0, village.Points.Count,
                index =>
                {
                    villageMap[village.Points[index].Z][village.Points[index].X] = 0;
                }
            );
        }
    }
}