using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DeluMc.Utils;
using Utils.Collections;
using Utils.SpatialTrees.QuadTrees;
using DeluMc.Masks;
using DeluMc.MCEdit;
using DeluMc.MCEdit.Block;

namespace DeluMc
{
    public class RoadPlacer
    {
        // <summary>
        /// Point with ZCurve for Hashing
        /// </summary>
        private class Point
        {
            /// <summary>
            /// This exists because C# doesn't like inheritance for structs
            /// </summary>
            public Vector2Int RealPoint { get; set; }

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

        private static void PlaceRoad()
        {

        }

        public static void RoadsPlacer(
            List<List<Vector2Int>> roads, int[][] roadMap, int[][] heightMap, int[][] waterMap, Biomes[][] biomes,
            Material[][][] world)
        {
            RectInt rectCover = new RectInt(Vector2Int.Zero, new Vector2Int(roadMap.Length - 1, roadMap[0].Length - 1));
            HashSet<Point> bridges = new HashSet<Point>();

            List<Vector2Int> road;
            int nz, nx, count;
            int averageHeight;

            for (int j = 0; j < roads.Count; j++)
            {
                road = roads[j];

                if (road.Count < 1)
                {
                    continue;
                }

                for (int i = 0; i < road.Count; i++)
                {
                    if (roadMap[road[i].Z][road[i].X] == RoadGenerator.MainRoadMarker)
                    {
                        // Normal Road
                        #region ROAD_PLACEMENT
                        count = 0;
                        averageHeight = 0;

                        if (0 <= i - 2 && roadMap[road[i - 2].Z][road[i - 2].X] == RoadGenerator.MainRoadMarker)
                        {
                            averageHeight += heightMap[road[i - 2].Z][road[i - 2].X];
                            ++count;
                        }

                        if (i + 2 < road.Count && roadMap[road[i + 2].Z][road[i + 2].X] == RoadGenerator.MainRoadMarker)
                        {
                            averageHeight += heightMap[road[i + 2].Z][road[i + 2].X];
                            ++count;
                        }

                        for (int dz = -1; dz <= 1; dz++)
                        {
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                nz = road[i].Z + dz;
                                nx = road[i].X + dx;
                                if (rectCover.IsInside(nz, nx) && roadMap[nz][nx] == RoadGenerator.MainRoadMarker)
                                {
                                    averageHeight += heightMap[nz][nx];
                                    ++count;
                                }
                            }
                        }
                        averageHeight /= count;

                        for (int dz = -1; dz <= 1; dz++)
                        {
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                nz = road[i].Z + dz;
                                nx = road[i].X + dx;
                                if (
                                    rectCover.IsInside(nz, nx)
                                    && (roadMap[nz][nx] == RoadGenerator.MainRoadMarker || roadMap[nz][nx] == RoadGenerator.RoadMarker))
                                {
                                    world[averageHeight][nz][nx] = AlphaMaterials.Stone_1_0;
                                    // Clear top
                                    for (int dy = 1; dy <= 2; dy++)
                                    {
                                        world[averageHeight + dy][nz][nx] = AlphaMaterials.Air_0_0;
                                    }
                                }
                            }
                        }
                        #endregion // ROAD_PLACEMENT
                    }
                    else
                    {
                        // Bridge
                        #region BRIDGE_PLACEMENT

                        Point curr = new Point { RealPoint = road[i] };
                        Point temp;

                        if (bridges.Contains(curr))
                        {
                            // Already processed
                            continue;
                        }
                        Stack<Point> bridgePoints = new Stack<Point>(); // DFS of bridges
                        List<Vector2Int> pivots = new List<Vector2Int>(); // Bridge land connections
                        List<Vector2Int> bridgeParts = new List<Vector2Int>(); // All parts of the bridge
                        HashSet<Point> currBridge = new HashSet<Point>(); // Avoid parts repetitions
                        bridgePoints.Push(curr);
                        averageHeight = 0;

                        while (bridgePoints.Count != 0)
                        {
                            curr = bridgePoints.Pop();

                            for (int dz = -1; dz <= 1; dz++)
                            {
                                for (int dx = -1; dx <= 1; dx++)
                                {
                                    nz = curr.RealPoint.Z + dz;
                                    nx = curr.RealPoint.X + dx;
                                    temp = new Point { RealPoint = new Vector2Int(nz, nx) };
                                    if (rectCover.IsInside(nz, nx) && !currBridge.Contains(temp))
                                    {
                                        if (roadMap[nz][nx] == RoadGenerator.MainRoadMarker)
                                        {
                                            averageHeight += heightMap[nz][nx];
                                            pivots.Add(new Vector2Int(nz, nx));
                                            currBridge.Add(temp);
                                        }
                                        else if (roadMap[nz][nx] == RoadGenerator.MainBridgeMarker)
                                        {
                                            bridges.Add(temp);
                                            bridgeParts.Add(temp.RealPoint);
                                            bridgePoints.Push(temp);
                                            currBridge.Add(temp);
                                        }
                                        else if (roadMap[nz][nx] == RoadGenerator.BridgeMarker)
                                        {
                                            bridgeParts.Add(new Vector2Int(nz, nx));
                                            currBridge.Add(temp);
                                        }
                                    }
                                }
                            }
                        }

                        // Get Bridge Average Height, add 1 to put it above water
                        averageHeight /= pivots.Count;
                        averageHeight += 3;

                        Parallel.ForEach(bridgeParts,
                            (Vector2Int point) =>
                            {
                                float height = 1.0f * Math.Max(heightMap[point.Z][point.X] + 1, averageHeight);
                                float factor = 1.0f;
                                float temp;
                                for (int k = 0; k < pivots.Count; k++)
                                {
                                    temp = 1.0f / (float)Vector2Int.Manhattan(point, pivots[k]);
                                    factor += temp;
                                    height += temp * heightMap[pivots[k].Z][pivots[k].X];
                                }
                                height /= factor;
                                int finalHeight = (int)height;
                                world[finalHeight][point.Z][point.X] = AlphaMaterials.WoodenDoubleSlab_Seamed_43_2;
                                for (int dy = 1; dy <= 2; dy++)
                                {
                                    world[finalHeight + dy][point.Z][point.X] = AlphaMaterials.Air_0_0;
                                }
                                if (roadMap[point.Z][point.X] == RoadGenerator.BridgeMarker)
                                {
                                    if (
                                        (rectCover.IsInside(point.Z + 1, point.X + 0) && waterMap[point.Z + 1][point.X + 0] == 1 && roadMap[point.Z + 1][point.X + 0] == 0)
                                        || (rectCover.IsInside(point.Z + 0, point.X + 1) && waterMap[point.Z + 0][point.X + 1] == 1 && roadMap[point.Z + 0][point.X + 1]  == 0)
                                        || (rectCover.IsInside(point.Z - 1, point.X + 0) && waterMap[point.Z - 1][point.X + 0] == 1 && roadMap[point.Z - 1][point.X + 0]  == 0)
                                        || (rectCover.IsInside(point.Z + 0, point.X - 1) && waterMap[point.Z + 0][point.X - 1] == 1 && roadMap[point.Z + 0][point.X - 1]  == 0)
                                        )
                                    {
                                        world[finalHeight + 1][point.Z][point.X] = AlphaMaterials.AcaciaFence_192_0;
                                    }
                                }
                            }
                        );

                        #endregion
                    }
                }
            }
        }
    }
}