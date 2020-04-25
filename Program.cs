using System;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
using DeluMc.Pipes;
using DeluMc.MCEdit;
using DeluMc.MCEdit.Block;
using DeluMc.Masks;

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

            Console.WriteLine("Hello World!");
            Console.WriteLine(MCEdit.Block.ClassicMaterials.Stone_1_0);

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

                Bitmap waterMask = new Bitmap(zSize, xSize);
                Bitmap hm = new Bitmap(zSize, xSize);
                Bitmap tm = new Bitmap(zSize, xSize);

                for (int z = 0; z < zSize; z++)
                {
                    biomes[z] = new Biomes[xSize];
                    heightMap[z] = new int[xSize];
                    waterMap[z] = new int[xSize];
                    treeMap[z] = new int[xSize];
                    for (int x = 0; x < xSize; x++)
                    {
                        biomes[z][x] = (Biomes)reader.ReadInt32();
                        heightMap[z][x] = reader.ReadInt32();
                        waterMap[z][x] = reader.ReadInt32();
                        treeMap[z][x] = 0;

                        // Nota, esto esta al reves tambien. Esta flipped en z (screen cords I guess)
                        if (waterMap[z][x] == 1) waterMask.SetPixel(z, x, Color.Blue);
                    }
                }
                waterMask.Save(@"waterMask.png", System.Drawing.Imaging.ImageFormat.Png);

                // Testing tasks
                Action<int, int, int, int> hmtmAction = (fz, fx, z, x) =>
                       HeightMap.FixBoxHeights(blocks, heightMap, treeMap, fz, fx, z, x);
                int ax = (xSize / 4);
                int az = (zSize / 4);

                Console.WriteLine("AX: " + ax + "AZ: " + az);

                int c = 0;
                Task[] tasks = new Task[16];

                // Be carefull with lambda variable catch
                for (int i = 0; i < 4; ++i)
                {
                    int ti = i;
                    for (int j = 0; j < 4; ++j)
                    {
                        int tj = j;

                        int fz = az * (ti + 1) - 1;
                        int fx = ax * (tj + 1) - 1;
                        if (i == 3) fz = zSize - 1;
                        if (j == 3) fx = xSize - 1;

                        tasks[c] = Task.Run(() => hmtmAction(az * ti, ax * tj, fz, fx));

                        ++c;
                    }
                }
                Task.WaitAll(tasks);

                for (int i = 0; i < zSize; i++)
                {
                    for (int j = 0; j < xSize; j++)
                    {
                        Console.Write(treeMap[i][j] + " ");
                    }
                    Console.WriteLine();
                }
                // Do stuff here

                for (int y = 0; y < ySize; y++)
                {
                    for (int z = 0; z < zSize; z++)
                    {
                        for (int x = 0; x < xSize; x++)
                        {
                            tm.SetPixel(z, x, Color.FromArgb(255, 0, 255 * treeMap[z][x], 0));

                            if (heightMap[z][x] >= 0) hm.SetPixel(z, x, Color.FromArgb(255, heightMap[z][x], heightMap[z][x], heightMap[z][x]));
                            else hm.SetPixel(z, x, Color.FromArgb(255, 255, 0, 0));

                            write.Write(blocks[y][z][x].ID);
                            write.Write(blocks[y][z][x].Data);
                        }
                    }
                }
                tm.Save(@"treeMask.png", System.Drawing.Imaging.ImageFormat.Png);
                hm.Save(@"NO_TREE_Heightmap.png", System.Drawing.Imaging.ImageFormat.Png);
                Console.WriteLine(write.BaseStream.Length);
                pipeClient.WriteMemoryBlock((MemoryStream)write.BaseStream);
            }
            pipeClient.DeInit();
        }
    }
}