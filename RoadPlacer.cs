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
using static DeluMc.MCEdit.Biomes.BiomeUtils;

namespace DeluMc
{
    /// <summary>
    /// Utility in charge of placing blocks in the marked roads and bridges
    /// </summary>
    public static class RoadPlacer
    {
        /// <summary>
        /// Minimum Valid Distance Between Lights
        /// </summary>
        private const int kLightMinDistance = 10;

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
            List<int> newHeight = new List<int>();
            List<Vector2Int> torchPoint = new List<Vector2Int>();
            int nz, nx, ny;
            float averageHeight;
            bool torchPlaced;
            DataQuadTree<int>.DistanceToDataPoint closestLight;
            Random sharedRnd = new Random();
            DataQuadTree<int> lights = new DataQuadTree<int>(rectCover.Min, rectCover.Max);
            Dictionary<ZPoint2D, int> lightPoints = new Dictionary<ZPoint2D, int>();
            // Ugly code but dumb fast
            for (int j = 0; j < roads.Count; j++)
            {
                road = roads[j];

                if (road.Count < 1)
                {
                    continue;
                }

                newHeight.Capacity = Math.Max(newHeight.Capacity, road.Count);
                torchPoint.Capacity = Math.Max(newHeight.Capacity, road.Count);

                int diff = road.Count - newHeight.Count;
                for (int i = 0; i < diff; i++)
                {
                    newHeight.Add(0);
                    torchPoint.Add(Vector2Int.Zero);
                }

                // Pre Pass for land roads
                Parallel.For(0, road.Count, () => new Random(),
                    (int i, ParallelLoopState _, Random rnd) =>
                    {
                        if (roadMap[road[i].Z][road[i].X] == RoadGenerator.MainRoadMarker)
                        {
                            // Road
                            int count = 0;
                            float averageHeight = 0;
                            if (0 <= i - 2 && roadMap[road[i - 2].Z][road[i - 2].X] == RoadGenerator.MainRoadMarker)
                            {
                                averageHeight += (float)heightMap[road[i - 2].Z][road[i - 2].X];
                                ++count;
                            }

                            if (i + 2 < road.Count && roadMap[road[i + 2].Z][road[i + 2].X] == RoadGenerator.MainRoadMarker)
                            {
                                averageHeight += (float)heightMap[road[i + 2].Z][road[i + 2].X];
                                ++count;
                            }

                            List<Vector2Int> sides = new List<Vector2Int>(8);

                            int nz, nx;
                            for (int dz = -1; dz <= 1; dz++)
                            {
                                for (int dx = -1; dx <= 1; dx++)
                                {
                                    nz = road[i].Z + dz;
                                    nx = road[i].X + dx;
                                    if (rectCover.IsInside(nz, nx))
                                    {
                                        if (roadMap[nz][nx] == RoadGenerator.MainRoadMarker)
                                        {
                                            averageHeight += (float)heightMap[nz][nx];
                                            ++count;
                                        }
                                        else if (roadMap[nz][nx] == RoadGenerator.RoadMarker)
                                        {
                                            sides.Add(new Vector2Int(dz, dx));
                                        }
                                    }
                                }
                            }
                            if (sides.Count > 0)
                            {
                                torchPoint[i] = sides[rnd.Next(0, sides.Count - 1)];
                            }
                            else
                            {
                                torchPoint[i] = new Vector2Int(-2, -2);
                            }
                            averageHeight /= (float)count;
                            newHeight[i] = (int)Math.Round((double)averageHeight);
                        }
                        return rnd;
                    },
                    (Random rnd) => { return; }
                );

                for (int i = 0; i < road.Count; i++)
                {
                    if (roadMap[road[i].Z][road[i].X] == RoadGenerator.MainRoadMarker)
                    {
                        // Normal Road
                        #region ROAD_PLACEMENT
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
                                    world.ChangeBlock(newHeight[i], nz, nx, GetBiomeRoadBlock(biomes[nz][nx]));
                                    heightMap[nz][nx] = newHeight[i];
                                    // Clear top
                                    for (int dy = 1; dy <= 2; dy++)
                                    {
                                        ny = newHeight[i] + dy;
                                        if (ny < maxY)
                                        {
                                            world.ChangeBlock(ny, nz, nx, AlphaMaterials.Air_0_0);
                                        }
                                    }
                                }
                            }

                            closestLight = lights.NearestNeighbor(road[i]);
                            torchPlaced = closestLight.DataNode != null && closestLight.ManClosestDistance <= kLightMinDistance;
                            if (!torchPlaced && torchPoint[i].Z != -2)
                            {
                                ZPoint2D tPoint = new ZPoint2D { RealPoint = road[i] + torchPoint[i] };
                                lights.Insert(tPoint.RealPoint, 0);
                                lightPoints[tPoint] = newHeight[i];
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

                            closestLight = lights.NearestNeighbor(curr.RealPoint);
                            torchPlaced = closestLight.DataNode != null && closestLight.ManClosestDistance <= kLightMinDistance;

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
                        averageHeight /= (float)pivots.Count;
                        averageHeight += 3;

                        Parallel.ForEach(bridgeParts, () => new Tuple<Differ.ChangeCollector, Random>(world.CreateCollector(), new Random()),
                            (Vector2Int point, ParallelLoopState _, Tuple<Differ.ChangeCollector, Random> tData) =>
                            {
                                Differ.ChangeCollector collector = tData.Item1;
                                Random rnd = tData.Item2;
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
                                for (int dy = 1; dy <= 3; dy++)
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
                                        int ny = finalHeight + 1;
                                        if (ny < maxY)
                                        {
                                            collector.ChangeBlock(ny, point.Z, point.X, AlphaMaterials.AcaciaFence_192_0);
                                        }

                                        if (rnd.Next(0, 5) == 0)
                                        {
                                            ny = finalHeight + 2;
                                            if (ny < maxY)
                                            {
                                                collector.ChangeBlock(ny, point.Z, point.X, AlphaMaterials.Torch_Up_50_5);
                                            }
                                        }
                                    }
                                }
                                return tData;
                            },
                            (Tuple<Differ.ChangeCollector, Random> tData) => { world.ApplyChangeCollector(tData.Item1); }
                        );

                        #endregion
                    }
                }
            }

            // Place Lights
            foreach (var iter in lightPoints)
            {
                // Place Base
                nz = iter.Key.RealPoint.Z;
                nx = iter.Key.RealPoint.X;
                ny = heightMap[nz][nx] + 1;
                if (ny < maxY)
                {
                    world.ChangeBlock(ny, nz, nx, AlphaMaterials.AcaciaFence_192_0);
                }
                // Place Torch
                ny += 1;
                if (ny < maxY)
                {
                    world.ChangeBlock(ny, nz, nx, AlphaMaterials.Torch_Up_50_5);
                }
            }
        }
    }
}