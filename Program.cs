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
            const int kMillisecondsToSleep = 15000; // 20 Seconds
            System.Threading.Thread.Sleep(kMillisecondsToSleep);
            Console.WriteLine("You should've launched the Debugger!");
#endif
        }

        static void Main(string[] args)
        {
            // Launch Debugger
            Debugger();

            if (args.Length != 1)
            {
                Console.WriteLine("Pipe requires only one argument");
                return;
            }

            PipeClient pipeClient = new PipeClient(args[0]);
            pipeClient.Init();
            using (BinaryReader reader = pipeClient.ReadMemoryBlock())
            {
                BinaryWriter write = new BinaryWriter(new MemoryStream());

                int ySize = reader.ReadInt32();
                int zSize = reader.ReadInt32();
                int xSize = reader.ReadInt32();
                Console.WriteLine($"Y: {ySize} Z: {zSize} X: {xSize}");
                Material[][][] blocks = new Material[ySize][][];
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
                // Biome, HeightMap, WaterMask and TreeMask arrays/bitmap
                Biomes[][] biomes = new Biomes[zSize][];
                int[][] heightMap = new int[zSize][];
                int[][] waterMap = new int[zSize][];
                int[][] treeMap = new int[zSize][];
                float[][] deltaMap = new float[zSize][];
                bool[][] acceptableMap = new bool[zSize][];
                int[][] villageMap = new int[zSize][];
                int[][] houseMap = new int[zSize][];
                int[][] roadMap = new int[zSize][];
                int[][] mainRoadMap = new int[zSize][];
                int[][] lavaMap = new int[zSize][];

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
                    lavaMap[z] = new int[xSize];
                    for (int x = 0; x < xSize; x++)
                    {
                        biomes[z][x] = (Biomes)reader.ReadInt32();
                        heightMap[z][x] = reader.ReadInt32();
                        waterMap[z][x] = reader.ReadInt32();
                        treeMap[z][x] = 0;
                        deltaMap[z][x] = 0;
                    }
                }

                {
                    Tasker.WorkChunk[] workChunks = {
                        (int zStart, int zEnd, int xStart, int xEnd) =>
                            {HeightMap.FixBoxHeights(blocks, heightMap, treeMap, zStart, xStart, zEnd, xEnd);}
                    };

                    Tasker.Run2DTasks(zSize, xSize, workChunks, null);
                }

                {
                    // Delta Map
                    Tasker.WorkBlock[] workBlocks = {
                        (int z, int x) => {DeltaMap.CalculateDeltaMap(heightMap, waterMap, deltaMap, z, x);
                        TreeMap.ExpandTreeBlock(z, x, treeMap);}
                    };

                    Tasker.Run2DTasks(zSize, xSize, null, workBlocks);
                }

                {
                    // Acceptable Map & Lava map
                    Tasker.WorkBlock[] isAcceptable = {(int z, int x) =>
                    {
                        int y1 = heightMap[z][x]; //Actual lava block
                        int y2; //if the lava is under a tree

                        acceptableMap[z][x] = DeltaMap.IsAcceptableBlock(deltaMap, z, x) && HeightMap.IsAcceptableTreeMapBlock(treeMap, z, x) && waterMap[z][x] != 1;

                        y2 = y1;
                        if (y1 < 0)
                        { //Ground surface below the selected volume
                            y1 = y2 = 0;
                        }
                        else if ( y1 + 1 < blocks.Length) 
                        { //upper block within the selected volume
                            y1 += 1;  
                        }

                        if(LavaMap.isLava(blocks[y1][z][x]) || LavaMap.isLava(blocks[y2][z][x]))
                        {
                            lavaMap[z][x] = 1;
                        }
                    }
                    };

                    Tasker.Run2DTasks(zSize, xSize, null, isAcceptable);
                }

                DataQuadTree<Vector2Int> roadQT = new DataQuadTree<Vector2Int>(new Vector2Int(), new Vector2Int(zSize - 1, xSize - 1));
                List<VillageMarker> villages = new List<VillageMarker>();
                List<List<Vector2Int>> roads = new List<List<Vector2Int>>();
                {
                    Random rand = new Random();
                    int numberOfTries = 10000;
                    int expectedVillageSize = 300;
                    int radius = 2;
                    int villageCount = 4;
                    while (villageCount != 0 && numberOfTries != 0)
                    {
                        int z = rand.Next(0, zSize);
                        int x = rand.Next(0, xSize);
                        if (acceptableMap[z][x] && villageMap[z][x] <= 0)
                        {
                            VillageMarker village = VillageMarkerPlacer.CreateVillage(acceptableMap, villageMap, z, x, expectedVillageSize, radius);
                            if (village.Points.Length >= expectedVillageSize / 2)
                            {
                                --villageCount;
                                villages.Add(village);
                            }
                            else
                            {
                                VillageMarkerPlacer.EliminateVillageMarker(village, villageMap);
                            }

                        }
                        --numberOfTries;
                    }
                    if (villages.Count > 1)
                    {
                        List<Vector2Int> road = RoadGenerator.FirstRoad(
                            villages[0].Seed.Z, villages[0].Seed.X,
                            villages[1].Seed.Z, villages[1].Seed.X,
                            acceptableMap, deltaMap, waterMap, roadMap, treeMap
                        );
                        Console.WriteLine("Main Road lenght: " + road.Count);
                        roads.Add(road);
                        foreach (Vector2Int roadPoint in road)
                        {
                            roadQT.Insert(roadPoint, roadPoint);
                            mainRoadMap[roadPoint.Z][roadPoint.X] = 1;
                        }
                    }
                    for (int i = 2; i < villages.Count; ++i)
                    {
                        Console.WriteLine($"Connecting village: {i} to roads");
                        List<Vector2Int> road = RoadGenerator.PointToRoad(
                            villages[i].Seed.Z, villages[i].Seed.X,
                            acceptableMap, deltaMap, waterMap, roadMap, treeMap,
                            roadQT
                        );
                        roads.Add(road);
                        foreach (Vector2Int roadPoint in road)
                        {
                            roadQT.Insert(roadPoint, roadPoint);
                        }
                    }

                    RoadPlacer.RoadsPlacer(roads, roadMap, heightMap, biomes, blocks);
                }

                HousePlacer.RequestHouseArea(
                    new HousePlacer.HouseAreaInput(
                        0,
                        new Vector2Int(),
                        new Vector2Int(zSize - 1, xSize - 1),
                        roadMap,
                        houseMap,
                        blocks,
                        Orientation.South,
                        PremadePalettes.forestPalette), BuildType.House);

                // Write Data Back to Python
                for (int y = 0; y < ySize; y++)
                {
                    for (int z = 0; z < zSize; z++)
                    {
                        for (int x = 0; x < xSize; x++)
                        {
                            write.Write(blocks[y][z][x].ID);
                            write.Write(blocks[y][z][x].Data);
                        }
                    }
                }

                {
                    // Drawing
                    Mapper.SaveMapInfo[] saveMapInfos =
                    {
                        new Mapper.SaveMapInfo{
                            zSize = zSize, xSize = xSize, name = "villagemap",
                            colorWork = (int z, int x) => {
                                if (villageMap[z][x] == 1)
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
                                if (lavaMap[z][x] == 1)
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

                // Return data To Python
                Console.WriteLine(write.BaseStream.Length);
                pipeClient.WriteMemoryBlock((MemoryStream)write.BaseStream);
            }
            pipeClient.DeInit();
        }
    }
}