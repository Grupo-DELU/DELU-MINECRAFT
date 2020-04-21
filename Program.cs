using System;
//using static Tensorflow.Binding;
//using Tensorflow;
//using Keras;
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

                // Do stuff here

                for (int y = 0; y < ySize; y++)
                {
                    for (int z = 0; z < zSize; z++)
                    {
                        for (int x = 0; x < xSize; x++)
                        {
                            // TODO: Remove this test
                            if (blocks[y][z][x] == AlphaMaterials.Air_0_0)
                            {
                                write.Write(AlphaMaterials.Stone_1_0.ID);
                                write.Write(AlphaMaterials.Stone_1_0.Data);
                            }
                            else
                            {
                                write.Write(blocks[y][z][x].ID);
                                write.Write(blocks[y][z][x].Data);
                            }
                        }
                    }
                }
                pipeClient.WriteMemoryBlock((MemoryStream)write.BaseStream);
            }
            pipeClient.DeInit();
        }
    }
}