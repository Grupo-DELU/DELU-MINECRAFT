using System;
using System.Drawing;
using System.IO;
using Delu_Mc.Pipes;
using Delu_Mc.MCEdit;
using Delu_Mc.MCEdit.Block;


namespace Delu_Mc
{
    class Program
    {
        static void Main(string[] args)
        {
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

                
                // Biome and HeightMap taking
                Biomes[][] biomes = new Biomes[zSize][];
                int[][] heightMap = new int[zSize][];
                for (int z = 0; z < zSize; z++)
                {
                    biomes[z] = new Biomes[xSize];
                    heightMap[z] = new int[xSize];
                    for (int x = 0; x < xSize; x++)
                    {
                        biomes[z][x] = (Biomes)reader.ReadInt32();
                        heightMap[z][x] = reader.ReadInt32();
                    }
                }
                
                //Bitmap bm = new Bitmap(zSize, xSize);
                

                // Do stuff here
                for (int y = 0; y < ySize; y++)
                {
                    for (int z = 0; z < zSize; z++)
                    {
                        for (int x = 0; x < xSize; x++)
                        {
                            //bm.SetPixel(z,x, Color.FromArgb(255, heightMap[z][x], 0, 0));
                            if (z < zSize/2)
                            {
                                switch (biomes[z][x])
                                {
                                    case Biomes.FlowerForest:
                                        write.Write(AlphaMaterials.Beacon_138_0.ID);
                                        write.Write(AlphaMaterials.Beacon_138_0.Data);
                                        break;
                                    case Biomes.RoofedForestM:
                                        write.Write(AlphaMaterials.NetherBrick_112_0.ID);
                                        write.Write(AlphaMaterials.NetherBrick_112_0.Data);
                                        break;
                                    case Biomes.River:
                                        write.Write(AlphaMaterials.Water_Flowing_Level0_8_7.ID);
                                        write.Write(AlphaMaterials.Water_Flowing_Level0_8_7.Data);
                                        break;
                                    default:
                                        write.Write(AlphaMaterials.Bedrock_7_0.ID);
                                        write.Write(AlphaMaterials.Bedrock_7_0.Data);
                                        break;
                                }
                            }
                            else
                            {
                                write.Write(blocks[y][z][x].ID);
                                write.Write(blocks[y][z][x].Data);
                            }
                        }
                    }
                }
                //bm.Save(@"/home/gorgola/Desktop/DELU-MINECRAFT/testimage", System.Drawing.Imaging.ImageFormat.Bmp);
                pipeClient.WriteMemoryBlock((MemoryStream)write.BaseStream);
            }
            pipeClient.DeInit();
        }
    }
}