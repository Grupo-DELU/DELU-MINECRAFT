using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

using DeluMc.Utils;
using DeluMc.Masks;
using DeluMc.MCEdit;
using DeluMc.Buildings;
using DeluMc.MCEdit.Block;
using DeluMc.MCEdit.Biomes;
using static DeluMc.MCEdit.Biomes.BiomeUtils;

using Utils.SpatialTrees.QuadTrees;

namespace DeluMc
{
    public static class HouseDistributor
    {
        // NOTE: We aren't using West because the algorith is going crazy
        // when using houses oriented to the West!
        private static Orientation[] orientations = new Orientation[]{Orientation.North,
                                                                      Orientation.South,
                                                                      Orientation.East};
                                
        private const int MIN_HOUSE_SEPARATION = 10;
        private const int TERRAFORMING_TRESHOLD = 10;

        private static bool placedPlaza = false;
        private const int PLAZA_RADIUS = 30;
        private const int HOUSE_RADIUS = 60;

        public static void FillVillage(in float[][] deltaMap, in int[][] heightMap, in bool[][] acceptable,
                                int[][] houseMap, int[][] roadMap, in int[][] villageMap, in int[][] waterMap,
                                in int[][] treeMap, in Biomes[][] biomes, VillageMarker village, Material[][][] world, in Vector2Int size, 
                                Differ differ, DataQuadTree<RectInt> rectTree, DataQuadTree<Vector2Int> roadQT,
                                ref List<List<Vector2Int>> roads)
        {
            int count = 0;
            placedPlaza = false;

            List<RectInt> houseRects = new List<RectInt>();
            DeltaMap.DeltaPair[] sortedDelta = DeltaMap.SortRectDelta(deltaMap, village.Rect);

            for (int i = 0; i < village.Points.Count; ++i)
            {
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
                        if (!IsSeparated(point, rect, rectTree))
                            continue;
                        
                        int radius = Vector2Int.Manhattan(rect.Center, village.Seed);
                        List<BuildType> buildings = CreateBuldTypeList(radius);
                        // Lo ponemos en el Y pelado por el cambio al chequeo que el road no este bloqueado
                        HousePlacer.HouseAreaInput req = new HousePlacer.HouseAreaInput(heightMap[point.Z][point.X], rect.Min, rect.Max, roadMap, houseMap, world, Orientation.North, Buildings.Palettes.PremadePalettes.forestPalette);
                        bool finish = false;
                        foreach (Orientation or in orientations)
                        {
                            req.orientation = or;
                            foreach (BuildType build in buildings)
                            {
                                BuildResult result = HousePlacer.RequestHouseArea(req, build, differ);
                                if (result.success)
                                {
                                    if (build == BuildType.Plaza)
                                        placedPlaza = true;
    
                                    PlaceFloorBelow(rect.Min, rect.Max, heightMap[point.Z][point.X], 
                                                    heightMap, biomes, differ);
                                    roads.Add(RoadGenerator.PointToRoad(result.doorPos.Z, result.doorPos.X, acceptable, 
                                                              deltaMap, waterMap, roadMap, treeMap, houseMap, roadQT));
                                    rectTree.Insert(point, rect);
                                    finish = true;
                                    break;
                                }
                            }
                            if (finish)
                                break;
                        }
                    }
                }
            }
            placedPlaza = false;
        }


        /// <summary>
        /// Test four Rects rotated around a point to see if 
        /// it could be usable for house placement.
        /// </summary>
        /// <param name="point">Point to rotate around</param>
        /// <param name="size">Size of the rect</param>
        /// <param name="world">World blocks</param>
        /// <param name="heightMap">Height map</param>
        /// <returns>A list of rects that could be usable for placing houses</returns>
        private static List<RectInt> TestRectPlacement(in Vector2Int point, in Vector2Int size, in Material[][][] world, in int[][] heightMap)
        {
            List<RectInt> rects = new List<RectInt>();

            // We substract (1, 1) to make them inclusive
            Vector2Int minA = point;
            Vector2Int maxA = point + new Vector2Int(size.Z, size.X);
            maxA -= Vector2Int.One;

            Vector2Int minB = point - new Vector2Int(0, size.X);
            Vector2Int maxB = point + new Vector2Int(size.Z, 0);
            maxB -= Vector2Int.One;

            Vector2Int minC = point - new Vector2Int(size.Z, 0);
            Vector2Int maxC = point + new Vector2Int(0, size.X);
            maxC -= Vector2Int.One;

            Vector2Int minD = point - new Vector2Int(size.Z, size.X);
            Vector2Int maxD = point;
            maxD -= Vector2Int.One;

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
        /// Checks if the position at (z,x) is usable for a house
        /// </summary>
        /// <param name="z">Z position to check</param>
        /// <param name="x">X position to chck</param>
        /// <param name="acceptableMap">Acceptable map</param>
        /// <param name="houseMap">House map</param>
        /// <param name="roadMap">Road map</param>
        /// <returns>true if the block is usable/false otherwise</returns>
        public static bool IsUsable(int z, int x, in bool[][] acceptableMap, in int[][] houseMap, in int[][] roadMap, in int[][] villageMap, in RectInt vRect)
        {
            return roadMap[z][x] != RoadGenerator.MainRoadMarker && acceptableMap[z][x] &&
                   houseMap[z][x] == 0 && villageMap[z][x] >= 1 && vRect.IsInside(z, x);
        }


        /// <summary>
        /// Checks if a rect is usable for placing a house
        /// </summary>
        /// <param name="rect">Rect to check</param>
        /// <param name="acceptableMap">Acceptable map</param>
        /// <param name="houseMap">House map</param>
        /// <param name="roadMap">Road map</param>
        /// <returns>true if the rect is usable/false otherwise</returns>
        private static bool IsRectUsable(in RectInt rect, in VillageMarker village, in bool[][] acceptableMap, in int[][] houseMap, in int[][] roadMap, in int[][] villageMap)
        {
            // Chequear rangos aca
            for (int i = rect.Min.Z; i <= rect.Max.Z; ++i)
            {
                for (int j = rect.Min.X; j <= rect.Max.X; ++j)
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
        /// <param name="min">Min of the rect (inclusive)</param>
        /// <param name="max">Max of the rect (inclusive)</param>
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
            for (int i = min.Z; i <= max.Z; ++i)
            {
                for (int j = min.X; j <= max.X; ++j)
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


        /// <summary>
        /// FIlls a rect with blocks and updates the heightmap
        /// </summary>
        /// <param name="min">Rect min (inclusive)</param>
        /// <param name="max">Rect max (inclusive)</param>
        /// <param name="y">Rect Y</param>
        /// <param name="heightMap">Heightmap</param>
        /// <param name="differ">Differ</param>
        private static void PlaceFloorBelow(in Vector2Int min, in Vector2Int max, int y, int[][] heightMap, 
                                            in Biomes[][] biomes, Differ differ)
        {
            // Modify heightmap
            Vector2Int size = max - min + Vector2Int.One;
            for (int i = min.Z; i <= min.Z + size.Z - 1; ++i)
            {
                for (int j = min.X; j <= min.X + size.X - 1; ++j)
                {
                    heightMap[i][j] = y;
                    differ.ChangeBlock(y, i, j, GetBiomeFloorBlock(biomes[i][j]));
                } 
            }
        }


        /// <summary>
        /// Checks if a rect got enough separation from the other ones
        /// </summary>
        /// <param name="point">Point of the rect in the QT</param>
        /// <param name="rect">Rect to check</param>
        /// <param name="housesQT">Houses QuadTree</param>
        /// <returns>true if the rect has enough separation/false otherwise</returns>
        private static bool IsSeparated(Vector2Int point, RectInt rect, DataQuadTree<RectInt> housesQT)
        {
            DataQuadTree<RectInt>.DistanceToDataPoint[] results = new DataQuadTree<RectInt>.DistanceToDataPoint[4];
            int found = housesQT.KNearestNeighbor(point, 4, results);
            for (int i = 0; i < found; ++i)
            {
                RectInt hRect = results[i].DataNode.Data;
                if (RectInt.Distance(hRect, rect) <= MIN_HOUSE_SEPARATION)
                    return false;
            }
            return true;
        }


        /// <summary>
        /// Return a list with the buildings to try depending on the radius
        /// where the house is going to be placed.
        /// </summary>
        /// <param name="radius">Radius to the center of the rect</param>
        /// <returns>A list containing build types</returns>
        private static List<BuildType> CreateBuldTypeList(int radius)
        {
            List<BuildType> l = new List<BuildType>();
            if (radius > HOUSE_RADIUS)
                l.Add(BuildType.Farm);
            
            if (radius <= PLAZA_RADIUS && !placedPlaza)
                l.Add(BuildType.Plaza);
            
            l.Add(BuildType.House);
            return l;
        }


        /// <summary>
        /// Shuffles the orientation array.
        /// </summary>
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
