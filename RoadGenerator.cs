using System;
using System.Collections.Generic;
using DeluMc.Utils;
using Utils.Collections;

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
                return distance * (1.0f + deltaMap[z][x] * SlopeMultiplier + (1 - roadMap[z][x]));
            }
            else if (waterMap[z][x] == 1)
            {
                return distance * (1.0f + WaterMultiplier);
            }
            return float.PositiveInfinity;
        }


        public static List<Vector2Int> FirstRoad(int sZ, int sX, int tZ, int tX, bool[][] acceptableMap, float[][] deltaMap, int[][] waterMap, int[][] roadMap)
        {
            System.Diagnostics.Debug.Assert(acceptableMap.Length > 0 && acceptableMap[0].Length > 0);
            RectInt rectCover = new RectInt(Vector2Int.Zero, new Vector2Int(acceptableMap.Length, acceptableMap[0].Length));
            Dictionary<Point, Point> parents = new Dictionary<Point, Point>();
            Dictionary<Point, float> distances = new Dictionary<Point, float>();
            MinHeap<Point> priorityQueue = new MinHeap<Point>(new CoordinatesBasedComparer());

            Func<int, int, int, int, float> distanceHeuristicFunc
                = (int z, int x, int tZ, int tX) => { return Distance(z, x, tZ, tX, acceptableMap, deltaMap, waterMap, roadMap); };

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
                        road.Add(curr.RealPoint);
                        curr = parents[curr];
                    }
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
    }
}