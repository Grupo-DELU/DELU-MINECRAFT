using System;
using System.Drawing;
using System.IO;
using System.Collections.Generic;
using DeluMc.Pipes;
using DeluMc.MCEdit;
using DeluMc.MCEdit.Block;
using DeluMc.Masks;
using DeluMc.Utils;
using DeluMc.Buildings;
using DeluMc.Buildings.Palettes;
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
                int[][] deltaMap = new int[zSize][];
                bool[][] acceptableMap = new bool[zSize][];
                int[][] villageMap = new int[zSize][];

                for (int z = 0; z < zSize; z++)
                {
                    biomes[z] = new Biomes[xSize];
                    heightMap[z] = new int[xSize];
                    waterMap[z] = new int[xSize];
                    treeMap[z] = new int[xSize];
                    deltaMap[z] = new int[xSize];
                    acceptableMap[z] = new bool[xSize];
                    villageMap[z] = new int[xSize];
                    for (int x = 0; x < xSize; x++)
                    {
                        biomes[z][x] = (Biomes)reader.ReadInt32();
                        heightMap[z][x] = reader.ReadInt32();
                        waterMap[z][x] = reader.ReadInt32();
                        treeMap[z][x] = 0;
                        deltaMap[z][x] = 0;
                    }
                }

                HousePlacer.RequestHouseArea(
                    new HousePlacer.HouseAreaInput(
                        0,
                        new Vector2Int(), 
                        new Vector2Int(zSize-1,xSize-1),
                        null,
                        null,
                        blocks,
                        Orientation.South,
                        PremadePalettes.forestPalette));
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
                        (int z, int x) => {DeltaMap.CalculateDeltaMap(heightMap, waterMap, deltaMap, z, x);}
                    };

                    Tasker.Run2DTasks(zSize, xSize, null, workBlocks);
                }

                {
                    // Acceptable Map
                    Tasker.WorkBlock[] isAcceptable = {(int z, int x) =>
                    {
                        acceptableMap[z][x] = DeltaMap.IsAcceptableBlock(deltaMap, z, x) && HeightMap.IsAcceptableTreeMapBlock(treeMap, z, x) && waterMap[z][x] != 1;
                    }
                    };

                    Tasker.Run2DTasks(zSize, xSize, null, isAcceptable);
                }

                List<VillageMarker> villages = new List<VillageMarker>();
                {
                    Random rand = new Random();
                    int numberOfTries = 10000;
                    int expectedVillageSize = 100;
                    int radius = 4;
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
                            }
                            villages.Add(village);
                        }
                        --numberOfTries;
                    }
                }

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
                                    float tVal = 1.0f - (float)(deltaMap[z][x]) / (float)DeltaMap.kMaxDelta;
                                    return Color.FromArgb(255, 0, (int)(255.0f * tVal + 200.0f * (1.0f - tVal)), 0);
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