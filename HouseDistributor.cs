using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using DeluMc.Utils;
using DeluMc.Masks;
using DeluMc.MCEdit;
using DeluMc.Buildings;
using DeluMc.MCEdit.Block;
using Utils.SpatialTrees.QuadTrees;

namespace DeluMc
{
    public static class HouseDistributor
    {
        private static Orientation[] orientations = new Orientation[]{Orientation.North,
                                                                      Orientation.South,
                                                                      Orientation.East};
                                                                      //(Orientation[])Enum.GetValues(typeof(Orientation));
        private const int MIN_HOUSE_SEPARATION = 10;
        private const int TERRAFORMING_TRESHOLD = 10;

        public static void FillVillage(in float[][] deltaMap, in int[][] heightMap, in bool[][] acceptable,
                                int[][] houseMap, int[][] roadMap, in int[][] villageMap, in int[][] waterMap,
                                in int[][] treeMap, VillageMarker village, Material[][][] world, in Vector2Int size, 
                                Differ differ, DataQuadTree<RectInt> rectTree, DataQuadTree<Vector2Int> roadQT,
                                ref List<List<Vector2Int>> roads)
        {
            int count = 0;
            List<RectInt> houseRects = new List<RectInt>();
            DeltaMap.DeltaPair[] sortedDelta = DeltaMap.SortRectDelta(deltaMap, village.Rect);

            for (int i = 0; i < village.Points.Count; ++i)
            {
                //Console.WriteLine("Punto " + sortedDelta[i].coordinates);
                Vector2Int point = sortedDelta[i].coordinates;
                List<RectInt> possibilities = TestRectPlacement(point, size, world, heightMap);
                if (possibilities.Count > 0)
                {
                    // Let's try all of them
                    for (int j = 0; j < possibilities.Count; ++j)
                    {
                        if (!IsRectUsable(possibilities[j], village, acceptable, houseMap, roadMap, villageMap))
                        {
                            possibilities.RemoveAt(j);
                            --j;
                            if (possibilities.Count == 0)
                                break;
                        }
                    }
                    count += possibilities.Count;

                    foreach (RectInt rect in possibilities)
                    {
                        ShuffleOrientations();
                        Console.WriteLine("======================================");
                        Console.WriteLine("Min: " + rect.Min);
                        Console.WriteLine("Max: " + rect.Max);
                        if (!IsSeparated(point, rect, rectTree))
                            continue;
                        
                        
                        // Lo ponemos en el Y pelado por el cambio al chequeo que el road no este bloqueado
                        HousePlacer.HouseAreaInput req = new HousePlacer.HouseAreaInput(heightMap[point.Z][point.X], rect.Min, rect.Max, roadMap, houseMap, world, Orientation.North, Buildings.Palettes.PremadePalettes.forestPalette);
                        
                        foreach (Orientation or in orientations)
                        {
                            req.orientation = or;
                            BuildResult result = HousePlacer.RequestHouseArea(req, BuildType.House, differ);
                            if (result.success)
                            {
                                // tratar de poner carretera.
                                // cuidado con esto por la carretera
                                PlaceFloorBelow(rect.Min, rect.Max, heightMap[point.Z][point.X], differ);
                                roads.Add(RoadGenerator.PointToRoad(result.doorPos.Z, result.doorPos.X, acceptable, deltaMap,
                                                          waterMap, roadMap, treeMap, houseMap, roadQT));
                                rectTree.Insert(point, rect);
                                break;
                            }
                        }
                    }
                }
            }
            Console.WriteLine(count);
        }


        private static List<RectInt> TestRectPlacement(in Vector2Int point, in Vector2Int size, in Material[][][] world, in int[][] heightMap)
        {
            List<RectInt> rects = new List<RectInt>();
            // Son 8 posibles orientaciones
            // Solo usare 4
            // TODO: This can go out of bounds. Must clamp or ignore box.
            Vector2Int minA = point;
            Vector2Int maxA = point + new Vector2Int(size.Z, size.X);

            Vector2Int minB = point - new Vector2Int(0, size.X + 1);
            Vector2Int maxB = point + new Vector2Int(size.Z, 0);

            Vector2Int minC = point - new Vector2Int(size.Z + 1, 0);
            Vector2Int maxC = point + new Vector2Int(0, size.X);

            Vector2Int minD = point - new Vector2Int(size.Z + 1, size.X + 1);
            Vector2Int maxD = point;


            if (maxA.Z < heightMap.Length && maxA.X < heightMap[0].Length && minA.Z >= 0 && minA.X >= 0)
            {
                if (CalculateTerraformation(minA, maxA, heightMap[point.Z][point.X] + 1, world, heightMap) < TERRAFORMING_TRESHOLD)
                {
                    rects.Add(new RectInt(minA, maxA));
                }
            }
            if (maxB.Z < heightMap.Length && maxB.X < heightMap[0].Length && minB.Z >= 0 && minB.X >= 0)
            {
                if (CalculateTerraformation(minB, maxB, heightMap[point.Z][point.X] + 1, world, heightMap) < TERRAFORMING_TRESHOLD)
                {
                    rects.Add(new RectInt(minB, maxB));
                }
            }
            if (maxC.Z < heightMap.Length && maxC.X < heightMap[0].Length && minC.Z >= 0 && minC.X >= 0)
            {
                if (CalculateTerraformation(minC, maxC, heightMap[point.Z][point.X] + 1, world, heightMap) < TERRAFORMING_TRESHOLD)
                {
                    rects.Add(new RectInt(minC, maxC));
                }
            }
            if (maxD.Z < heightMap.Length && maxD.X < heightMap[0].Length && minD.Z >= 0 && minD.X >= 0)
            {
                if (CalculateTerraformation(minD, maxD, heightMap[point.Z][point.X] + 1, world, heightMap) < TERRAFORMING_TRESHOLD)
                {
                    rects.Add(new RectInt(minD, maxD));
                }
            }
            return rects;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="z"></param>
        /// <param name="x"></param>
        /// <param name="acceptableMap"></param>
        /// <param name="houseMap"></param>
        /// <param name="roadMap"></param>
        /// <returns></returns>
        public static bool IsUsable(int z, int x, in bool[][] acceptableMap, in int[][] houseMap, in int[][] roadMap, in int[][] villageMap, in RectInt vRect)
        {
            return roadMap[z][x] != RoadGenerator.MainRoadMarker && acceptableMap[z][x] &&
                   houseMap[z][x] == 0 && villageMap[z][x] >= 1 && vRect.IsInside(z, x);
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="acceptableMap"></param>
        /// <param name="houseMap"></param>
        /// <param name="roadMap"></param>
        /// <returns></returns>
        private static bool IsRectUsable(in RectInt rect, in VillageMarker village, in bool[][] acceptableMap, in int[][] houseMap, in int[][] roadMap, in int[][] villageMap)
        {
            for (int i = rect.Min.Z; i < rect.Max.Z; ++i)
            {
                for (int j = rect.Min.X; j < rect.Max.X; ++j)
                {
                    if (!IsUsable(i, j, acceptableMap, houseMap, roadMap, villageMap, village.Rect))
                        return false;
                }
            }
            return true;
        }


        /// <summary>
        /// Calculates an APROXIMATION of the amount of terraformation needed
        /// to place a house inside certain RectInt. 
        /// </summary>
        /// <param name="min">Min of the rect</param>
        /// <param name="max">Max of the rect</param>
        /// <param name="y">Y of the zone</param>
        /// <param name="world">Blocks of the world</param>
        /// <param name="heightMap">HeightMap of the world</param>
        /// <returns>
        /// Positive number indicating blocks to be modified/
        /// int.MaxValue if it is impossible
        /// </returns>
        public static int CalculateTerraformation(in Vector2Int min, in Vector2Int max, int y, in Material[][][] world, in int[][] heightMap)
        {
            // Can go out of bounds, be carefull
            if (y == 0 || y >= world.Length)
                return int.MaxValue;

            int c = 0;
            for (int i = min.Z; i < max.Z; ++i)
            {
                for (int j = min.X; j < max.X; ++j)
                {
                    if (heightMap[i][j] < 0)
                        return int.MaxValue;

                    if (world[y][i][j] != AlphaMaterials.Air_0_0)
                    {
                        // Just in case there is grass or flowers
                        if (heightMap[i][j] == y - 1)
                            continue;
                        c += Math.Abs(heightMap[i][j] - y) + 1;
                    }
                    else
                    {
                        if (y - 2 >= 0)
                        {
                            if (world[y - 2][i][j] == AlphaMaterials.Air_0_0)
                            {
                                return int.MaxValue;
                            }
                        }
                        if (world[y - 1][i][j] == AlphaMaterials.Air_0_0)
                            ++c;
                    }
                }
            }
            return c;
        }


        private static void PlaceFloorBelow(in Vector2Int min, in Vector2Int max, int y, Differ differ)
        {
            Vector2Int size = max - min + Vector2Int.One;
            for (int i = min.Z; i < min.Z + size.Z; ++i)
            {
                for (int j = min.X; j < min.X + size.X; ++j)
                {
                    differ.ChangeBlock(y, i, j, AlphaMaterials.RedWool_35_14);
                } 
            }
        }


        public static bool IsSeparated(Vector2Int point, RectInt rect, DataQuadTree<RectInt> housesQT)
        {

            DataQuadTree<RectInt>.DistanceToDataPoint nearest = housesQT.NearestNeighbor(point);
            if (nearest.DataNode == null)
                return true;
            
            return RectInt.Distance(nearest.DataNode.Data, rect) >= MIN_HOUSE_SEPARATION;

            /*
            DataQuadTree<RectInt>.DistanceToDataPoint[] results = new DataQuadTree<RectInt>.DistanceToDataPoint[4];
            int found = housesQT.KNearestNeighbor(point, 4, results);
            for (int i = 0; i < found; ++i)
            {
                RectInt hRect = results[i].DataNode.Data;
                Console.WriteLine("Distance to kNear house: " + RectInt.Distance(hRect, rect));
                if (RectInt.Distance(hRect, rect) <= MIN_HOUSE_SEPARATION)
                {
                    Console.WriteLine("Too close! :(");
                    return false;
                }
            }
            return true;
            */
        }

        private static void ShuffleOrientations()
        {
            Random rand = new Random();
            for (int i = 0; i < 3; ++i)
            {
                Orientation temp = orientations[i];
                int ind = rand.Next(3);
                orientations[i] = orientations[ind];
                orientations[ind] = temp;
            }
        }
    }
}
