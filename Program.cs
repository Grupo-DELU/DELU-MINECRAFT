using System;
using System.IO;
using System.Drawing;
using System.Collections.Generic;
using DeluMc.Pipes;
using DeluMc.Masks;
using DeluMc.Utils;
using DeluMc.MCEdit;
using DeluMc.Buildings;
using DeluMc.MCEdit.Block;
using DeluMc.MCEdit.Biomes;
using DeluMc.Buildings.Palettes;
using Utils.SpatialTrees.QuadTrees;
namespace DeluMc
{
    class Program
    {
        /// <summary>
        /// Handle Debugger
        /// </summary>
        [System.Diagnostics.Conditional("DEBUG")]
        static void Debugger()
        {
#if !LINUX
            if (System.Diagnostics.Debugger.Launch())
            {
                Console.WriteLine("Debugger Launched!");
            }
            else
            {
                Console.Error.WriteLine("Failed to Launch Debugger!");
            }
#else
            Console.WriteLine("Sleeping Thread to Launch Debugger!");
            const int kMillisecondsToSleep = 10000; // 20 Seconds
            System.Threading.Thread.Sleep(kMillisecondsToSleep);
            Console.WriteLine("You should've launched the Debugger!");
#endif
        }

        static void Main(string[] args)
        {
            // Launch Debugger
            Debugger();
            Clocker.AddAndStartClock("StartClock");

            if (args.Length != 1)
            {
                Console.WriteLine("Pipe requires only one argument");
                return;
            }

            PipeClient pipeClient = new PipeClient(args[0]);
            pipeClient.Init();

            int ySize;
            int zSize;
            int xSize;
            Differ differ;

            // Biome, HeightMap, WaterMask and TreeMask arrays/bitmap
            Biomes[][] biomes;
            int[][] heightMap;
            int[][] waterMap;
            int[][] treeMap;
            float[][] deltaMap;
            bool[][] acceptableMap;
            int[][] villageMap;
            int[][] houseMap;
            int[][] roadMap;
            int[][] mainRoadMap;
            bool[][] lavaMap;

            using (BinaryReader reader = pipeClient.ReadMemoryBlock())
            {
                Material[][][] blocks;
                ySize = reader.ReadInt32();
                zSize = reader.ReadInt32();
                xSize = reader.ReadInt32();
                Console.WriteLine($"Y: {ySize} Z: {zSize} X: {xSize}");
                blocks = new Material[ySize][][];
                for (int y = 0; y < ySize; y++)
                {
                    blocks[y] = new Material[zSize][];
                    for (int z = 0; z < zSize; z++)
                    {
                        blocks[y][z] = new Material[xSize];
                        for (int x = 0; x < xSize; x++)
                        {
                            blocks[y][z][x] = AlphaMaterials.Set.GetMaterial(reader.ReadInt32(), reader.ReadInt32());
                        }
                    }
                }
                differ = new Differ(blocks);

                // Biome, HeightMap, WaterMask and TreeMask arrays/bitmap
                biomes = new Biomes[zSize][];
                heightMap = new int[zSize][];
                waterMap = new int[zSize][];
                treeMap = new int[zSize][];
                deltaMap = new float[zSize][];
                acceptableMap = new bool[zSize][];
                villageMap = new int[zSize][];
                houseMap = new int[zSize][];
                roadMap = new int[zSize][];
                mainRoadMap = new int[zSize][];
                lavaMap = new bool[zSize][];

                for (int z = 0; z < zSize; z++)
                {
                    biomes[z] = new Biomes[xSize];
                    heightMap[z] = new int[xSize];
                    waterMap[z] = new int[xSize];
                    treeMap[z] = new int[xSize];
                    deltaMap[z] = new float[xSize];
                    acceptableMap[z] = new bool[xSize];
                    villageMap[z] = new int[xSize];
                    houseMap[z] = new int[xSize];
                    roadMap[z] = new int[xSize];
                    mainRoadMap[z] = new int[xSize];
                    lavaMap[z] = new bool[xSize];
                    for (int x = 0; x < xSize; x++)
                    {
                        biomes[z][x] = (Biomes)reader.ReadInt32();
                        heightMap[z][x] = reader.ReadInt32();
                        waterMap[z][x] = reader.ReadInt32();
                        treeMap[z][x] = 0;
                        deltaMap[z][x] = 0;
                    }
                }
            }

            {
                Tasker.WorkChunk[] workChunks = {
                        (int zStart, int zEnd, int xStart, int xEnd) =>
                            {HeightMap.FixBoxHeights(differ.World, heightMap, treeMap, zStart, xStart, zEnd, xEnd);}
                    };

                Tasker.Run2DTasks(zSize, xSize, workChunks, null);
            }

            {
                // Delta Map & Lava Map
                Tasker.WorkBlock[] workBlocks = { (int z, int x) =>
                        {
                        DeltaMap.CalculateDeltaMap(heightMap, waterMap, deltaMap, z, x);
                        TreeMap.ExpandTreeBlock(z, x, treeMap);
                        lavaMap[z][x] = LavaMap.isAcceptableLavaMapBlock(heightMap, differ.World, z,x);
                        }
                    };

                Tasker.Run2DTasks(zSize, xSize, null, workBlocks);
            }

            {
                // Acceptable Map
                Tasker.WorkBlock[] isAcceptable = {(int z, int x) =>
                    {
                        acceptableMap[z][x] =   DeltaMap.IsAcceptableBlock(deltaMap, z, x)          &&
                                                HeightMap.IsAcceptableTreeMapBlock(treeMap, z, x)   &&
                                                waterMap[z][x] != 1                                 &&
                                                !lavaMap[z][x];
                    }
                    };

                Tasker.Run2DTasks(zSize, xSize, null, isAcceptable);
            }

            WaterAnalyzer.WaterAnalysis waterAnalysis;
            {
                int minWaterSize = 20;
                waterAnalysis = WaterAnalyzer.AnalyzeWater(waterMap, minWaterSize);
                Console.WriteLine($"Min Water Body Size {minWaterSize}");
                Console.WriteLine($"Found {waterAnalysis.WaterBodies.Count} valid Water Bodies and {waterAnalysis.InvalidWaterBodies.Count} Invalid ones");
                Console.WriteLine("Valid Water Bodies Sizes");
                foreach (var waterBody in waterAnalysis.WaterBodies)
                {
                    Console.WriteLine($"\tSize {waterBody.Points.Count}");
                }
                Console.WriteLine("Invalid Water Bodies Sizes");
                foreach (var waterBody in waterAnalysis.InvalidWaterBodies)
                {
                    Console.WriteLine($"\tSize {waterBody.Points.Count}");
                }
            }
            
            DataQuadTree<Vector2Int> roadQT = new DataQuadTree<Vector2Int>(new Vector2Int(), new Vector2Int(zSize - 1, xSize - 1));
            List<VillageMarker> villages;
            List<List<Vector2Int>> roads = new List<List<Vector2Int>>();
            {
                int numberOfTries = 1000;
                int expectedVillageSize = 1500;
                int radius = 2;
                int villageCount = 4;
                int maxVillageCount = 5;
                villages = VillageDistributor.DistributeVillageMarkers(
                    acceptableMap, villageMap, waterAnalysis,
                    villageCount, maxVillageCount, numberOfTries, radius, expectedVillageSize
                );

                bool mainRoadPlaced = false;
                int mainRoadVillageStart = -1, mainRoadVillageEnd = -1;
                if (villages.Count > 1)
                {
                    for (int i = 0; i < villages.Count - 1; i++)
                    {
                        for (int j = i + 1; j < villages.Count; j++)
                        {

                            List<Vector2Int> road = RoadGenerator.FirstRoad(
                                villages[i].Seed.Z, villages[i].Seed.X,
                                villages[j].Seed.Z, villages[j].Seed.X,
                                acceptableMap, deltaMap, waterMap, roadMap, treeMap, houseMap
                            );
                            if (road.Count > 0)
                            {
                                mainRoadPlaced = true;
                                mainRoadVillageStart = i;
                                mainRoadVillageEnd = j;
                                Console.WriteLine("Main Road length: " + road.Count);
                                roads.Add(road);
                                foreach (Vector2Int roadPoint in road)
                                {
                                    roadQT.Insert(roadPoint, roadPoint);
                                    mainRoadMap[roadPoint.Z][roadPoint.X] = 1;
                                }
                                break;
                            }
                        }
                        if (mainRoadPlaced)
                        {
                            break;
                        }
                    }
                }

                if (mainRoadPlaced)
                {
                    for (int i = 0; i < villages.Count; ++i)
                    {
                        if (i != mainRoadVillageStart && i != mainRoadVillageEnd)
                        {
                            Console.WriteLine($"Connecting village: {i} to roads");
                            List<Vector2Int> road = RoadGenerator.PointToRoad(
                                villages[i].Seed.Z, villages[i].Seed.X,
                                acceptableMap, deltaMap, waterMap, roadMap, treeMap, houseMap,
                                roadQT
                            );
                            roads.Add(road);
                            foreach (Vector2Int roadPoint in road)
                            {
                                roadQT.Insert(roadPoint, roadPoint);
                            }
                        }
                    }

                    //RoadPlacer.RoadsPlacer(roads, roadMap, heightMap, waterMap, biomes, differ);
                }
                else
                {
                    Console.WriteLine("Failed to Place all the roads");
                }
            }


            DataQuadTree<RectInt> villagesQT = new DataQuadTree<RectInt>(new Vector2Int(0,0), new Vector2Int(zSize, xSize));
            foreach (VillageMarker village in villages)
            {
                HouseDistributor.FillVillage(deltaMap, heightMap, acceptableMap, houseMap, roadMap, villageMap, 
                                             waterMap, treeMap, biomes, village, differ.World, new Vector2Int(7,7), differ, 
                                             villagesQT, roadQT, ref roads);
            }
            
            RoadPlacer.RoadsPlacer(roads, roadMap, heightMap, waterMap, biomes, differ);
            // Write Data Back to Python
            {
                using (BinaryWriter writer = new BinaryWriter(new MemoryStream()))
                {
                    differ.SerializeChanges(writer);

                    // Return data To Python
                    Console.WriteLine(writer.BaseStream.Length);
                    pipeClient.WriteMemoryBlock((MemoryStream)writer.BaseStream);
                }
            }

            // Close Pipe
            pipeClient.DeInit();

            {
                // Drawing
                Mapper.SaveMapInfo[] saveMapInfos =
                {
                        new Mapper.SaveMapInfo{
                            zSize = zSize, xSize = xSize, name = "villagemap",
                            colorWork = (int z, int x) => {
                                if (villageMap[z][x] >= 1)
                                {
                                    return Color.Yellow;
                                }
                                else if (acceptableMap[z][x] && villageMap[z][x] <= 0)
                                {
                                    return Color.Orange;
                                }
                                return Color.Transparent;
                                },
                            specialColors = (Mapper.ColorApplier colorApplier) =>
                            {
                                for (int i = 0; i < villages.Count; i++)
                                {
                                    colorApplier.Invoke(villages[i].Seed.Z, villages[i].Seed.X, Color.Blue);
                                }
                            }
                        },
                        new Mapper.SaveMapInfo{
                            zSize = zSize, xSize = xSize, name = "acceptablemap",
                            colorWork = (int z, int x) => {
                                if (acceptableMap[z][x])
                                {
                                    return Color.Green;
                                }
                                else
                                {
                                    return Color.Red;
                                }
                                },
                            specialColors = null
                        },
                        new Mapper.SaveMapInfo{
                            zSize = zSize, xSize = xSize, name = "deltamap",
                            colorWork = (int z, int x) => {
                                if (0 <= deltaMap[z][x] && deltaMap[z][x] <= DeltaMap.kMaxDelta)
                                {
                                    float tVal = 1.0f - deltaMap[z][x] / DeltaMap.kMaxDelta;
                                    return Color.FromArgb(255, 0, (int)(255.0f * tVal + 100.0f * (1.0f - tVal)), 0);
                                }
                                else if (deltaMap[z][x] > DeltaMap.kMaxDelta)
                                {
                                    return Color.Red;
                                }
                                return Color.Transparent;
                                },
                            specialColors = null
                        },
                        new Mapper.SaveMapInfo{
                            zSize = zSize, xSize = xSize, name = "treemap",
                            colorWork = (int z, int x) => {
                                if (treeMap[z][x] == 1)
                                {
                                    return Color.Green;
                                }
                                else if (treeMap[z][x] == 2)
                                {
                                    return Color.Brown;
                                }
                                else if (treeMap[z][x] == 3)
                                {
                                    return Color.DarkSeaGreen;
                                }
                                return Color.Transparent;
                                },
                            specialColors = null
                        },
                        new Mapper.SaveMapInfo{
                            zSize = zSize, xSize = xSize, name = "heightmap",
                            colorWork = (int z, int x) => {
                                if (heightMap[z][x] >= 0)
                                {
                                    return Color.FromArgb(255, heightMap[z][x], heightMap[z][x], heightMap[z][x]);
                                }
                                else
                                {
                                    return Color.Red;
                                }
                                },
                            specialColors = null
                        },
                        new Mapper.SaveMapInfo{
                            zSize = zSize, xSize = xSize, name = "watermap",
                            colorWork = (int z, int x) => {
                                if (waterMap[z][x] == 1)
                                {
                                    return Color.Blue;
                                }
                                return Color.Transparent;
                                },
                            specialColors = null
                        },
                        new Mapper.SaveMapInfo{
                            zSize = zSize, xSize = xSize, name = "houseMap",
                            colorWork = (int z, int x) => {
                                if (houseMap[z][x] == 1)
                                {
                                    return Color.Brown;
                                }
                                return Color.Transparent;
                                },
                            specialColors = null
                        },
                        new Mapper.SaveMapInfo{
                            zSize = zSize, xSize = xSize, name = "roadMap",
                            colorWork = (int z, int x) => {
                                switch (roadMap[z][x])
                                {
                                    case RoadGenerator.MainRoadMarker:
                                        return Color.PaleVioletRed;
                                    case RoadGenerator.RoadMarker:
                                        return Color.Purple;
                                    case RoadGenerator.BridgeMarker:
                                        return Color.Brown;
                                    case RoadGenerator.MainBridgeMarker:
                                        return Color.BurlyWood;
                                    default:
                                        return Color.Transparent;
                                }
                                },
                            specialColors = null
                        },
                        new Mapper.SaveMapInfo{
                            zSize = zSize, xSize = xSize, name = "mainRoadMap",
                            colorWork = (int z, int x) => {
                                if (mainRoadMap[z][x] == 1)
                                {
                                    return Color.Turquoise;
                                }
                                return Color.Transparent;
                                },
                            specialColors = null
                        },
                        new Mapper.SaveMapInfo{
                            zSize = zSize, xSize = xSize, name = "lavaMap",
                            colorWork = (int z, int x) => {
                                if ( lavaMap[z][x] )
                                {
                                    return Color.DarkRed;
                                }
                                return Color.Transparent;
                                },
                            specialColors = null
                        }

                    };

                Mapper.SaveMaps(saveMapInfos);
            }
            Clocker.RemoveClock("StartClock", true, true, true);
        }
    }
}