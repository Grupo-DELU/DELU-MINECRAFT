using System;
using System.Collections.Generic;
using DeluMc.Utils;
using Utils.Collections;
using Utils.SpatialTrees.QuadTrees;

namespace DeluMc
{
    public static class RoadGenerator
    {
        /// <summary>
        /// Point with ZCurve for Hashing
        /// </summary>
        private class Point
        {
            /// <summary>
            /// This exists because C# doesn't like inheritance for structs
            /// </summary>
            public Vector2Int RealPoint { get; set; }

            /// <summary>
            /// Distance to target
            /// </summary>
            public float Distance { get; set; }

            /// <summary>
            /// C# Object Equality
            /// </summary>
            /// <param name="obj">Other Object</param>
            /// <returns>If other object is equals</returns>
            public override bool Equals(Object obj)
            {
                //Check for null and compare run-time types.
                if ((obj == null) || !this.GetType().Equals(obj.GetType()))
                {
                    return false;
                }
                else
                {
                    Point p = (Point)obj;
                    return this.RealPoint.Equals(p.RealPoint);
                }
            }

            /// <summary>
            /// Hashing for dictionary using ZCurves
            /// </summary>
            public override int GetHashCode()
            {
                System.Diagnostics.Debug.Assert(RealPoint.Z >= 0 && RealPoint.X >= 0);
                return (int)ZCurve.Pos2D((uint)RealPoint.Z, (uint)RealPoint.X);
            }
        }

        /// <summary>
        /// Comparers for Points based on distance to target
        /// </summary>
        private class CoordinatesBasedComparer : Comparer<Point>
        {
            /// <summary>
            /// Compare two points by distance to target
            /// </summary>
            /// <param name="lhs">Left Point</param>
            /// <param name="rhs">Right Point</param>
            /// <returns></returns>
            public override int Compare(Point lhs, Point rhs)
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
        /// Slope Multiplier for Heuristic Function
        /// The More Slope the harder it is to move
        /// </summary>
        public static float SlopeMultiplier = 1.5f;

        /// <summary>
        /// Wave Multiplier for Heuristic Function
        /// It is hard to cross water
        /// </summary>
        public static float WaterMultiplier = 4.0f;

        /// <summary>
        /// Wave Multiplier for Heuristic Function
        /// It is cheaper to move through other roads
        /// </summary>
        public static float NotRoadMultiplier = 0.5f;

        /// <summary>
        /// Cost of failing to position parts of the 3x1 road
        /// </summary>
        public static float FailedSideOfRoadCost = 3.0f;

        /// <summary>
        /// Calculate distance between two points
        /// Uses Manhattan distance
        /// </summary>
        /// <param name="z">Start Point Z</param>
        /// <param name="x">Start Point X</param>
        /// <param name="tZ">Target Point Z</param>
        /// <param name="tX">Target Point X</param>
        /// <param name="acceptableMap">Acceptable Map</param>
        /// <param name="waterMap">Water Map</param>
        /// <returns>Distancce to Point</returns>
        public static float Metric(int z, int x, int tZ, int tX, bool[][] acceptableMap, int[][] waterMap)
        {
            int dz = tZ - z;
            int dx = tX - x;
            float distance = (float)(Math.Abs(dz) + Math.Abs(dx));

            if (acceptableMap[z][x] || waterMap[z][x] == 1)
            {
                return distance;
            }
            return float.PositiveInfinity;
        }

        /// <summary>
        /// Distance Heuristic Function for A*
        /// </summary>
        /// <param name="z">Current Z</param>
        /// <param name="x">Current X</param>
        /// <param name="tZ">Target Z</param>
        /// <param name="tX">Target X</param>
        /// <param name="acceptableMap">Acceptable Map</param>
        /// <param name="deltaMap">Delta Map for Slopes</param>
        /// <param name="waterMap">Water Map</param>
        /// <param name="roadMap">Road Map</param>
        /// <returns>Calculated Heuristic</returns>
        public static float Distance(int z, int x, int tZ, int tX, bool[][] acceptableMap, float[][] deltaMap, int[][] waterMap, int[][] roadMap)
        {
            float distance = Metric(z, x, tZ, tX, acceptableMap, waterMap);

            if (acceptableMap[z][x])
            {
                float noRoad = roadMap[z][x] == 0 ? 1.0f : 0.0f;
                return distance * (1.0f + deltaMap[z][x] * SlopeMultiplier + NotRoadMultiplier * noRoad);
            }
            else if (waterMap[z][x] == 1)
            {
                return distance * (1.0f + WaterMultiplier);
            }
            return float.PositiveInfinity;
        }

        /// <summary>
        /// Distance Heuristic Function for A*
        /// </summary>
        /// <param name="z">Current Z</param>
        /// <param name="x">Current X</param>
        /// <param name="tZ">Target Z</param>
        /// <param name="tX">Target X</param>
        /// <param name="acceptableMap">Acceptable Map</param>
        /// <param name="deltaMap">Delta Map for Slopes</param>
        /// <param name="waterMap">Water Map</param>
        /// <param name="roadMap">Road Map</param>
        /// <returns>Calculated Heuristic</returns>
        public static float Heuristic(int z, int x, int tZ, int tX, in RectInt rectCover, in bool[][] acceptableMap, float[][] deltaMap, int[][] waterMap, int[][] roadMap)
        {
            int nZ, nX;
            int count = 0;
            float temp;
            float acc = 0.0f;
            float failedAcc = 0.0f;

            for (int dz = -1; dz <= 1; dz++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    nZ = z + dz;
                    nX = x + dx;
                    if (rectCover.IsInside(nZ, nX))
                    {
                        temp = Distance(nZ, nX, tZ, tX, acceptableMap, deltaMap, waterMap, roadMap);
                        if (temp == float.PositiveInfinity)
                        {
                            if (nZ == z && nX == x)
                            {
                                return float.PositiveInfinity;
                            }
                            failedAcc += FailedSideOfRoadCost;
                        }
                        else
                        {
                            acc += temp;
                            ++count;
                        }
                    }
                }
            }
            return (acc / (float)count) + failedAcc;
        }

        /// <summary>
        /// Place a Road Patch
        /// </summary>
        /// <param name="Z">Z Coord</param>
        /// <param name="X">X Coord</param>
        /// <param name="rectCover">Cover for Map</param>
        /// <param name="acceptableMap">Acceptable Map</param>
        /// <param name="waterMap">Water Map</param>
        /// <param name="roadMap">Road Map</param>
        private static void RoadPatchPlacement(
            int Z, int X, in RectInt rectCover, bool[][] acceptableMap, int[][] waterMap, int[][] roadMap
        )
        {
            if (rectCover.IsInside(Z, X) && roadMap[Z][X] == 0)
            {
                if (acceptableMap[Z][X])
                {
                    // Road
                    roadMap[Z][X] = 1;
                }
                else if (waterMap[Z][X] == 1)
                {
                    // Bridge
                    roadMap[Z][X] = 2;
                }
            }
        }

        /// <summary>
        /// Paint a Road
        /// </summary>
        /// <param name="road">Road to Paint</param>
        /// <param name="acceptableMap">Acceptable Map</param>
        /// <param name="waterMap">Water Map</param>
        /// <param name="roadMap">Road Map</param>
        private static void PaintRoad(in List<Vector2Int> road, bool[][] acceptableMap, int[][] waterMap, int[][] roadMap)
        {
            RectInt rectCover = new RectInt(Vector2Int.Zero, new Vector2Int(acceptableMap.Length - 1, acceptableMap[0].Length - 1));

            if (road.Count <= 1)
            {
                // Nothing to Paint
                return;
            }

            int roadZ, roadX;

            for (int i = 0; i < road.Count - 1; i++)
            {
                Vector2Int dir = road[i + 1] - road[i];
                if (dir.Z == 0)
                {
                    // No change in Z
                    // Horizontal Movement
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        roadZ = road[i].Z + dz;
                        RoadPatchPlacement(roadZ, road[i].X, rectCover, acceptableMap, waterMap, roadMap);
                    }
                }
                else if (dir.Z > 0)
                {
                    // Increase in Z
                    if (dir.X == 0)
                    {
                        // No change in X
                        // Vertical Movement
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            roadX = road[i].X + dx;
                            RoadPatchPlacement(road[i].Z, roadX, rectCover, acceptableMap, waterMap, roadMap);
                        }
                    }
                    else if (dir.X > 0)
                    {
                        // Increase in X
                        // x x 0
                        // x x x
                        // 0 x x
                        RoadPatchPlacement(road[i].Z + 1, road[i].X - 1, rectCover, acceptableMap, waterMap, roadMap);
                        RoadPatchPlacement(road[i].Z, road[i].X - 1, rectCover, acceptableMap, waterMap, roadMap);
                        RoadPatchPlacement(road[i].Z + 1, road[i].X, rectCover, acceptableMap, waterMap, roadMap);
                        RoadPatchPlacement(road[i].Z, road[i].X, rectCover, acceptableMap, waterMap, roadMap);
                        RoadPatchPlacement(road[i].Z, road[i].X + 1, rectCover, acceptableMap, waterMap, roadMap);
                        RoadPatchPlacement(road[i].Z - 1, road[i].X, rectCover, acceptableMap, waterMap, roadMap);
                        RoadPatchPlacement(road[i].Z - 1, road[i].X + 1, rectCover, acceptableMap, waterMap, roadMap);
                    }
                    else if (dir.X < 0)
                    {
                        // Decrease in X
                        // 0 x x
                        // x x x
                        // x x 0
                        RoadPatchPlacement(road[i].Z - 1, road[i].X - 1, rectCover, acceptableMap, waterMap, roadMap);
                        RoadPatchPlacement(road[i].Z, road[i].X - 1, rectCover, acceptableMap, waterMap, roadMap);
                        RoadPatchPlacement(road[i].Z - 1, road[i].X, rectCover, acceptableMap, waterMap, roadMap);
                        RoadPatchPlacement(road[i].Z, road[i].X, rectCover, acceptableMap, waterMap, roadMap);
                        RoadPatchPlacement(road[i].Z + 1, road[i].X, rectCover, acceptableMap, waterMap, roadMap);
                        RoadPatchPlacement(road[i].Z, road[i].X + 1, rectCover, acceptableMap, waterMap, roadMap);
                        RoadPatchPlacement(road[i].Z + 1, road[i].X + 1, rectCover, acceptableMap, waterMap, roadMap);
                    }
                }
                else if (dir.Z < 0)
                {
                    // Decrease in Z
                    if (dir.X == 0)
                    {
                        // No change in X
                        // Vertical Movement
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            roadX = road[i].X + dx;
                            RoadPatchPlacement(road[i].Z, roadX, rectCover, acceptableMap, waterMap, roadMap);
                        }
                    }
                    else if (dir.X > 0)
                    {
                        // Increase in X
                        RoadPatchPlacement(road[i].Z - 1, road[i].X - 1, rectCover, acceptableMap, waterMap, roadMap);
                        RoadPatchPlacement(road[i].Z, road[i].X - 1, rectCover, acceptableMap, waterMap, roadMap);
                        RoadPatchPlacement(road[i].Z - 1, road[i].X, rectCover, acceptableMap, waterMap, roadMap);
                        RoadPatchPlacement(road[i].Z, road[i].X, rectCover, acceptableMap, waterMap, roadMap);
                        RoadPatchPlacement(road[i].Z + 1, road[i].X, rectCover, acceptableMap, waterMap, roadMap);
                        RoadPatchPlacement(road[i].Z, road[i].X + 1, rectCover, acceptableMap, waterMap, roadMap);
                        RoadPatchPlacement(road[i].Z + 1, road[i].X + 1, rectCover, acceptableMap, waterMap, roadMap);
                    }
                    else if (dir.X < 0)
                    {
                        // Decrease in X
                        RoadPatchPlacement(road[i].Z + 1, road[i].X - 1, rectCover, acceptableMap, waterMap, roadMap);
                        RoadPatchPlacement(road[i].Z, road[i].X - 1, rectCover, acceptableMap, waterMap, roadMap);
                        RoadPatchPlacement(road[i].Z + 1, road[i].X, rectCover, acceptableMap, waterMap, roadMap);
                        RoadPatchPlacement(road[i].Z, road[i].X, rectCover, acceptableMap, waterMap, roadMap);
                        RoadPatchPlacement(road[i].Z, road[i].X + 1, rectCover, acceptableMap, waterMap, roadMap);
                        RoadPatchPlacement(road[i].Z - 1, road[i].X, rectCover, acceptableMap, waterMap, roadMap);
                        RoadPatchPlacement(road[i].Z - 1, road[i].X + 1, rectCover, acceptableMap, waterMap, roadMap);
                    }
                }
            }

            {
                Vector2Int dir = road[road.Count - 1] - road[road.Count - 2];
                int i = road.Count - 1;
                if (dir.Z == 0)
                {
                    // No change in Z
                    // Horizontal Movement
                    for (int dz = -1; dz <= 1; dz++)
                    {
                        roadZ = road[road.Count - 1].Z + dz;
                        RoadPatchPlacement(roadZ, road[i].X, rectCover, acceptableMap, waterMap, roadMap);
                    }
                }
                else if (dir.Z > 0)
                {
                    // Increase in Z
                    if (dir.X == 0)
                    {
                        // No change in X
                        // Vertical Movement
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            roadX = road[i].X + dx;
                            RoadPatchPlacement(road[i].Z, roadX, rectCover, acceptableMap, waterMap, roadMap);
                        }
                    }
                    else if (dir.X > 0)
                    {
                        // Increase in X
                        // x x 0
                        // x x x
                        // 0 x x
                        RoadPatchPlacement(road[i].Z + 1, road[i].X - 1, rectCover, acceptableMap, waterMap, roadMap);
                        RoadPatchPlacement(road[i].Z, road[i].X - 1, rectCover, acceptableMap, waterMap, roadMap);
                        RoadPatchPlacement(road[i].Z + 1, road[i].X, rectCover, acceptableMap, waterMap, roadMap);
                        RoadPatchPlacement(road[i].Z, road[i].X, rectCover, acceptableMap, waterMap, roadMap);
                        RoadPatchPlacement(road[i].Z, road[i].X + 1, rectCover, acceptableMap, waterMap, roadMap);
                        RoadPatchPlacement(road[i].Z - 1, road[i].X, rectCover, acceptableMap, waterMap, roadMap);
                        RoadPatchPlacement(road[i].Z - 1, road[i].X + 1, rectCover, acceptableMap, waterMap, roadMap);
                    }
                    else if (dir.X < 0)
                    {
                        // Decrease in X
                        // 0 x x
                        // x x x
                        // x x 0
                        RoadPatchPlacement(road[i].Z - 1, road[i].X - 1, rectCover, acceptableMap, waterMap, roadMap);
                        RoadPatchPlacement(road[i].Z, road[i].X - 1, rectCover, acceptableMap, waterMap, roadMap);
                        RoadPatchPlacement(road[i].Z - 1, road[i].X, rectCover, acceptableMap, waterMap, roadMap);
                        RoadPatchPlacement(road[i].Z, road[i].X, rectCover, acceptableMap, waterMap, roadMap);
                        RoadPatchPlacement(road[i].Z + 1, road[i].X, rectCover, acceptableMap, waterMap, roadMap);
                        RoadPatchPlacement(road[i].Z, road[i].X + 1, rectCover, acceptableMap, waterMap, roadMap);
                        RoadPatchPlacement(road[i].Z + 1, road[i].X + 1, rectCover, acceptableMap, waterMap, roadMap);
                    }
                }
                else if (dir.Z < 0)
                {
                    // Decrease in Z
                    if (dir.X == 0)
                    {
                        // No change in X
                        // Vertical Movement
                        for (int dx = -1; dx <= 1; dx++)
                        {
                            roadX = road[i].X + dx;
                            RoadPatchPlacement(road[i].Z, roadX, rectCover, acceptableMap, waterMap, roadMap);
                        }
                    }
                    else if (dir.X > 0)
                    {
                        // Increase in X
                        RoadPatchPlacement(road[i].Z - 1, road[i].X - 1, rectCover, acceptableMap, waterMap, roadMap);
                        RoadPatchPlacement(road[i].Z, road[i].X - 1, rectCover, acceptableMap, waterMap, roadMap);
                        RoadPatchPlacement(road[i].Z - 1, road[i].X, rectCover, acceptableMap, waterMap, roadMap);
                        RoadPatchPlacement(road[i].Z, road[i].X, rectCover, acceptableMap, waterMap, roadMap);
                        RoadPatchPlacement(road[i].Z + 1, road[i].X, rectCover, acceptableMap, waterMap, roadMap);
                        RoadPatchPlacement(road[i].Z, road[i].X + 1, rectCover, acceptableMap, waterMap, roadMap);
                        RoadPatchPlacement(road[i].Z + 1, road[i].X + 1, rectCover, acceptableMap, waterMap, roadMap);
                    }
                    else if (dir.X < 0)
                    {
                        // Decrease in X
                        RoadPatchPlacement(road[i].Z + 1, road[i].X - 1, rectCover, acceptableMap, waterMap, roadMap);
                        RoadPatchPlacement(road[i].Z, road[i].X - 1, rectCover, acceptableMap, waterMap, roadMap);
                        RoadPatchPlacement(road[i].Z + 1, road[i].X, rectCover, acceptableMap, waterMap, roadMap);
                        RoadPatchPlacement(road[i].Z, road[i].X, rectCover, acceptableMap, waterMap, roadMap);
                        RoadPatchPlacement(road[i].Z, road[i].X + 1, rectCover, acceptableMap, waterMap, roadMap);
                        RoadPatchPlacement(road[i].Z - 1, road[i].X, rectCover, acceptableMap, waterMap, roadMap);
                        RoadPatchPlacement(road[i].Z - 1, road[i].X + 1, rectCover, acceptableMap, waterMap, roadMap);
                    }
                }
            }
        }

        /// <summary>
        /// Generate the first road between two points
        /// </summary>
        /// <param name="sZ">Starting Point Z Coordinate</param>
        /// <param name="sX">Starting Point X Coordinate</param>
        /// <param name="tZ">Target Point Z Coordinate</param>
        /// <param name="tX">Target Point X Coordinate</param>
        /// <param name="acceptableMap">Acceptable Map</param>
        /// <param name="deltaMap">Delta Map</param>
        /// <param name="waterMap">Water Map</param>
        /// <param name="roadMap">Road Map</param>
        /// <returns>Road Connecting the two points, if any</returns>
        public static List<Vector2Int> FirstRoad(int sZ, int sX, int tZ, int tX, bool[][] acceptableMap, float[][] deltaMap, int[][] waterMap, int[][] roadMap)
        {
            System.Diagnostics.Debug.Assert(acceptableMap.Length > 0 && acceptableMap[0].Length > 0);
            RectInt rectCover = new RectInt(Vector2Int.Zero, new Vector2Int(acceptableMap.Length - 1, acceptableMap[0].Length - 1));
            Dictionary<Point, Point> parents = new Dictionary<Point, Point>();
            Dictionary<Point, float> distances = new Dictionary<Point, float>();
            MinHeap<Point> priorityQueue = new MinHeap<Point>(new CoordinatesBasedComparer());

            Func<int, int, int, int, float> distanceHeuristicFunc
                = (int z, int x, int tZ, int tX) => { return Heuristic(z, x, tZ, tX, rectCover, acceptableMap, deltaMap, waterMap, roadMap); };

            Func<int, int, float> heuristicFunc
                = (int z, int x) => { return distanceHeuristicFunc(z, x, tZ, tX); };

            Point startPoint = new Point { RealPoint = new Vector2Int(sZ, sX), Distance = distanceHeuristicFunc(sZ, sX, tZ, tX) };

            parents.Add(startPoint, null);
            distances.Add(startPoint, 0);
            priorityQueue.Add(startPoint);

            Point curr, child;
            int childZ, childX;
            float childDistance, currDistance;

            while (!priorityQueue.IsEmpty())
            {
                curr = priorityQueue.ExtractDominating();

                if (curr.RealPoint.Z == tZ && curr.RealPoint.X == tX)
                {
                    // We found goal
                    List<Vector2Int> road = new List<Vector2Int>();
                    while (curr != null)
                    {
                        // Add Point to Road
                        road.Add(curr.RealPoint);
                        curr = parents[curr];
                    }
                    PaintRoad(road, acceptableMap, waterMap, roadMap);
                    return road;
                }

                currDistance = distances[curr];

                for (int dz = -1; dz <= 1; dz++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        childZ = curr.RealPoint.Z + dz;
                        childX = curr.RealPoint.X + dx;
                        if (rectCover.IsInside(childZ, childX))
                        {
                            childDistance = currDistance + distanceHeuristicFunc(curr.RealPoint.Z, curr.RealPoint.X, childZ, childX);
                            child = new Point { RealPoint = new Vector2Int(childZ, childX), Distance = heuristicFunc(childZ, childX) };
                            if (distances.TryGetValue(child, out float currVal))
                            {
                                if (childDistance < currVal)
                                {
                                    distances[child] = childDistance;
                                    parents[child] = curr;
                                    priorityQueue.Add(child);
                                }
                            }
                            else
                            {
                                distances[child] = childDistance;
                                parents[child] = curr;
                                priorityQueue.Add(child);
                            }
                        }
                    }
                }

            }
            return null;
        }

        
        /// <summary>
        /// Generates a road from a point to the roads
        /// </summary>
        /// <param name="sZ"></param>
        /// <param name="sX"></param>
        /// <param name="acceptableMap">Acceptable Map</param>
        /// <param name="deltaMap">Delta Map</param>
        /// <param name="waterMap">Water Map</param>
        /// <param name="roadMap">Road Map</param>
        /// <param name="roadQT">Road Quadtree</param>
        /// <returns></returns>
        public static List<Vector2Int> PointToRoad(int sZ, int sX, bool[][] acceptableMap, float[][] deltaMap, int[][] waterMap, int[][] roadMap, DataQuadTree<Vector2Int> roadQT)
        {
            System.Diagnostics.Debug.Assert(acceptableMap.Length > 0 && acceptableMap[0].Length > 0);
            RectInt rectCover = new RectInt(Vector2Int.Zero, new Vector2Int(acceptableMap.Length - 1, acceptableMap[0].Length - 1));
            Dictionary<Point, Point> parents = new Dictionary<Point, Point>();
            Dictionary<Point, float> distances = new Dictionary<Point, float>();
            MinHeap<Point> priorityQueue = new MinHeap<Point>(new CoordinatesBasedComparer());
            Vector2Int target = roadQT.NearestNeighbor(new Vector2Int(sZ, sX)).DataNode.Data;
            
            Func<int, int, int, int, float> distanceHeuristicFunc
                = (int z, int x, int tZ, int tX) => { return Heuristic(z, x, tZ, tX, rectCover, acceptableMap, deltaMap, waterMap, roadMap); };

            Func<int, int, float> heuristicFunc
                = (int z, int x) => { return distanceHeuristicFunc(z, x, target.Z, target.X); };

            Point startPoint = new Point { RealPoint = new Vector2Int(sZ, sX), Distance = distanceHeuristicFunc(sZ, sX, target.Z, target.X) };

            parents.Add(startPoint, null);
            distances.Add(startPoint, 0);
            priorityQueue.Add(startPoint);

            Point curr, child;
            int childZ, childX;
            float childDistance, currDistance;
            while (!priorityQueue.IsEmpty())
            {
                curr = priorityQueue.ExtractDominating();

                if (curr.RealPoint.Z == target.Z && curr.RealPoint.X == target.X)
                {
                    // We found goal
                    List<Vector2Int> road = new List<Vector2Int>();
                    while (curr != null)
                    {
                        // Add Point to Road
                        road.Add(curr.RealPoint);
                        curr = parents[curr];
                    }
                    PaintRoad(road, acceptableMap, waterMap, roadMap);
                    return road;
                }

                currDistance = distances[curr];

                for (int dz = -1; dz <= 1; dz++)
                {
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        childZ = curr.RealPoint.Z + dz;
                        childX = curr.RealPoint.X + dx;
                        // Target of the child position is the nearest point of the road to the child
                        target = roadQT.NearestNeighbor(new Vector2Int(childZ, childX)).DataNode.Data;

                        if (rectCover.IsInside(childZ, childX))
                        {
                            
                            childDistance = currDistance + distanceHeuristicFunc(curr.RealPoint.Z, curr.RealPoint.X, childZ, childX);
                            child = new Point { RealPoint = new Vector2Int(childZ, childX), Distance = heuristicFunc(childZ, childX) };
                            if (distances.TryGetValue(child, out float currVal))
                            {
                                if (childDistance < currVal)
                                {
                                    distances[child] = childDistance;
                                    parents[child] = curr;
                                    priorityQueue.Add(child);
                                }
                            }
                            else
                            {
                                distances[child] = childDistance;
                                parents[child] = curr;
                                priorityQueue.Add(child);
                            }
                        }
                    }
                }

            }
            return null;
        }
    }
}