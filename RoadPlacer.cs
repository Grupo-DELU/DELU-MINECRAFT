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
using DeluMc.MCEdit.Biomes;

namespace DeluMc
{
    /// <summary>
    /// Utility in charge of placing blocks in the marked roads and bridges
    /// </summary>
    public static class RoadPlacer
    {
        /// <summary>
        /// Place the blocks in the marked roads and bridges
        /// </summary>
        /// <param name="roads">Roads to place</param>
        /// <param name="roadMap">Road Map</param>
        /// <param name="heightMap">Height Map</param>
        /// <param name="waterMap">Water Map</param>
        /// <param name="biomes">Biomes</param>
        /// <param name="world">World</param>
        public static void RoadsPlacer(
            List<List<Vector2Int>> roads, int[][] roadMap, int[][] heightMap, int[][] waterMap, Biomes[][] biomes,
            Differ world)
        {
            RectInt rectCover = new RectInt(Vector2Int.Zero, new Vector2Int(roadMap.Length - 1, roadMap[0].Length - 1));
            HashSet<ZPoint2D> bridges = new HashSet<ZPoint2D>();

            int maxY = world.World.Length;

            List<Vector2Int> road;
            int nz, nx, ny, count;
            float averageHeight;
            int averageHeightInt;
            // Ugly code but dumb fast
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
                            averageHeight += (float)heightMap[road[i - 2].Z][road[i - 2].X] + 0.5f;
                            ++count;
                        }

                        if (i + 2 < road.Count && roadMap[road[i + 2].Z][road[i + 2].X] == RoadGenerator.MainRoadMarker)
                        {
                            averageHeight += (float)heightMap[road[i + 2].Z][road[i + 2].X] + 0.5f;
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
                                    averageHeight += (float)heightMap[nz][nx] + 0.5f;
                                    ++count;
                                }
                            }
                        }
                        averageHeight /= (float)count;
                        averageHeightInt = (int)(averageHeight - 0.25f);

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
                                    world.ChangeBlock(averageHeightInt, nz, nx, AlphaMaterials.Cobblestone_4_0);
                                    heightMap[nz][nx] = averageHeightInt; // TODO: Maybe its wrong
                                    // Clear top
                                    for (int dy = 1; dy <= 2; dy++)
                                    {
                                        ny = averageHeightInt + dy;
                                        if (ny < maxY)
                                        {
                                            world.ChangeBlock(ny, nz, nx, AlphaMaterials.Air_0_0);
                                        }
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

                        ZPoint2D curr = new ZPoint2D { RealPoint = road[i] };
                        ZPoint2D temp;

                        if (bridges.Contains(curr))
                        {
                            // Already processed
                            continue;
                        }
                        Stack<ZPoint2D> bridgeStack = new Stack<ZPoint2D>(); // DFS of bridges
                        List<Vector2Int> pivots = new List<Vector2Int>(); // Bridge land connections
                        List<Vector2Int> bridgeParts = new List<Vector2Int>(); // All parts of the bridge
                        HashSet<ZPoint2D> currBridge = new HashSet<ZPoint2D>(); // Avoid parts repetitions
                        bridgeStack.Push(curr);
                        averageHeight = 0;

                        while (bridgeStack.Count != 0)
                        {
                            curr = bridgeStack.Pop();

                            for (int dz = -1; dz <= 1; dz++)
                            {
                                for (int dx = -1; dx <= 1; dx++)
                                {
                                    nz = curr.RealPoint.Z + dz;
                                    nx = curr.RealPoint.X + dx;
                                    temp = new ZPoint2D { RealPoint = new Vector2Int(nz, nx) };
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
                                            bridgeStack.Push(temp);
                                            currBridge.Add(temp);
                                        }
                                        else if (roadMap[nz][nx] == RoadGenerator.BridgeMarker)
                                        {
                                            bridgeParts.Add(new Vector2Int(nz, nx));
                                            bridgeStack.Push(temp);
                                            currBridge.Add(temp);
                                        }
                                    }
                                }
                            }
                        }

                        if (pivots.Count == 0)
                        {
                            // Bad Bridge
                            continue;
                        }

                        // Get Bridge Average Height, add 1 to put it above water
                        averageHeight /= pivots.Count;
                        averageHeight += 3;

                        Parallel.ForEach(bridgeParts, () => world.CreateCollector(),
                            (Vector2Int point, ParallelLoopState _, Differ.ChangeCollector collector) =>
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
                                collector.ChangeBlock(finalHeight, point.Z, point.X, AlphaMaterials.WoodenDoubleSlab_Seamed_43_2);
                                // Clear top
                                for (int dy = 1; dy <= 2; dy++)
                                {
                                    ny = finalHeight + dy;
                                    if (ny < maxY)
                                    {
                                        collector.ChangeBlock(ny, point.Z, point.X, AlphaMaterials.Air_0_0);
                                    }
                                }

                                if (roadMap[point.Z][point.X] == RoadGenerator.BridgeMarker)
                                {
                                    if (
                                        (rectCover.IsInside(point.Z + 1, point.X + 0) && waterMap[point.Z + 1][point.X + 0] == 1 && roadMap[point.Z + 1][point.X + 0] == 0)
                                        || (rectCover.IsInside(point.Z + 0, point.X + 1) && waterMap[point.Z + 0][point.X + 1] == 1 && roadMap[point.Z + 0][point.X + 1] == 0)
                                        || (rectCover.IsInside(point.Z - 1, point.X + 0) && waterMap[point.Z - 1][point.X + 0] == 1 && roadMap[point.Z - 1][point.X + 0] == 0)
                                        || (rectCover.IsInside(point.Z + 0, point.X - 1) && waterMap[point.Z + 0][point.X - 1] == 1 && roadMap[point.Z + 0][point.X - 1] == 0)
                                        )
                                    {
                                        collector.ChangeBlock(finalHeight + 1, point.Z, point.X, AlphaMaterials.AcaciaFence_192_0);
                                    }
                                }
                                return collector;
                            },
                            (Differ.ChangeCollector collector) => { world.ApplyChangeCollector(collector); }
                        );

                        #endregion
                    }
                }
            }
        }
    }
}