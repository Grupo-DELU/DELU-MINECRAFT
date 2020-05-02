using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using DeluMc.Pipes;
using DeluMc.MCEdit;
using DeluMc.MCEdit.Block;
using DeluMc.Masks;
using DeluMc.Utils;

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

                Bitmap waterMask = new Bitmap(zSize, xSize);
                Bitmap hm = new Bitmap(zSize, xSize);
                Bitmap tm = new Bitmap(zSize, xSize);
                Bitmap deltaMapBit = new Bitmap(zSize, xSize);
                Bitmap acceptableMapBit = new Bitmap(zSize, xSize);

                for (int z = 0; z < zSize; z++)
                {
                    biomes[z] = new Biomes[xSize];
                    heightMap[z] = new int[xSize];
                    waterMap[z] = new int[xSize];
                    treeMap[z] = new int[xSize];
                    deltaMap[z] = new int[xSize];
                    acceptableMap[z] = new bool[xSize];
                    for (int x = 0; x < xSize; x++)
                    {
                        biomes[z][x] = (Biomes)reader.ReadInt32();
                        heightMap[z][x] = reader.ReadInt32();
                        waterMap[z][x] = reader.ReadInt32();
                        treeMap[z][x] = 0;
                        deltaMap[z][x] = 0;

                        // Nota, esto esta al reves tambien. Esta flipped en z (screen cords I guess)
                        if (waterMap[z][x] == 1)
                        {
                            waterMask.SetPixel(z, x, Color.Blue);
                        }
                    }
                }
                waterMask.Save(@"waterMask.png", System.Drawing.Imaging.ImageFormat.Png);

                /*
                {
                    // Example: Remove Later
                    Tasker.WorkBlock[] workBlocks = {
                        (int z, int x) => {Console.WriteLine($"Block ({z}, {x})");}
                    };

                    Tasker.WorkChunk[] workChunks = {
                        (int zStart, int zEnd, int xStart, int xEnd) => {Console.WriteLine($"Chunk ({zStart}, {xStart}) -> ({zEnd}, {xEnd})");}
                    };

                    Tasker.Run2DTasks(zSize, xSize, workChunks, workBlocks);
                }
                */
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


                // Write Data Back to Python
                for (int y = 0; y < ySize; y++)
                {
                    for (int z = 0; z < zSize; z++)
                    {
                        for (int x = 0; x < xSize; x++)
                        {
                            {
                                // Drawing
                                tm.SetPixel(z, x, Color.FromArgb(255, 0, 255 * treeMap[z][x], 0));

                                if (heightMap[z][x] >= 0)
                                {
                                    hm.SetPixel(z, x, Color.FromArgb(255, heightMap[z][x], heightMap[z][x], heightMap[z][x]));
                                }
                                else
                                {
                                    hm.SetPixel(z, x, Color.FromArgb(255, 255, 0, 0));
                                }

                                if (0 <= deltaMap[z][x] && deltaMap[z][x] <= DeltaMap.kMaxDelta)
                                {
                                    float tVal = 1.0f - (float)(deltaMap[z][x]) / (float)DeltaMap.kMaxDelta;
                                    deltaMapBit.SetPixel(z, x,
                                        Color.FromArgb(255, 0, (int)(255.0f * tVal + 200.0f * (1.0f - tVal)), 0)
                                    );
                                }
                                else if (deltaMap[z][x] > DeltaMap.kMaxDelta)
                                {
                                    deltaMapBit.SetPixel(z, x, Color.FromArgb(255, 255, 0, 0));
                                }
                                else
                                {
                                    deltaMapBit.SetPixel(z, x, Color.FromArgb(255, 0, 0, 255));
                                }

                                if (acceptableMap[z][x])
                                {
                                    acceptableMapBit.SetPixel(z, x, Color.FromArgb(255, 0, 255, 0));
                                }
                                else
                                {
                                    acceptableMapBit.SetPixel(z, x, Color.FromArgb(255, 255, 0, 0));
                                }
                            }


                            write.Write(blocks[y][z][x].ID);
                            write.Write(blocks[y][z][x].Data);
                        }
                    }
                }

                acceptableMapBit.Save(@"acceptablemap.png", System.Drawing.Imaging.ImageFormat.Png);
                deltaMapBit.Save(@"deltamap.png", System.Drawing.Imaging.ImageFormat.Png);
                tm.Save(@"treeMask.png", System.Drawing.Imaging.ImageFormat.Png);
                hm.Save(@"NO_TREE_Heightmap.png", System.Drawing.Imaging.ImageFormat.Png);

                // Return data To Python
                Console.WriteLine(write.BaseStream.Length);
                pipeClient.WriteMemoryBlock((MemoryStream)write.BaseStream);
            }
            pipeClient.DeInit();
        }
    }
}