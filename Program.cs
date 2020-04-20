using System;
//using static Tensorflow.Binding;
//using Tensorflow;
//using Keras;
using System.IO;
using Delu_Mc.Pipes;

namespace Delu_Mc
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            //var hello = tf.constant("Hello, TensorFlow!");

            // Start tf session
            //using (var sess = tf.Session())
            //{
            //    // Run the op
            //    var result = sess.run(hello);
            //    Console.WriteLine($"{result}");
            //}
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

                int arrSize = reader.ReadInt32();
                Console.WriteLine($"Size: {arrSize}");
                write.Write(arrSize);

                for (int i = 0; i < arrSize; i++)
                {
                    int num = reader.ReadInt32();
                    Console.WriteLine(num);
                    write.Write(num);
                }

                pipeClient.WriteMemoryBlock((MemoryStream)write.BaseStream);
            }
            pipeClient.DeInit();
        }
    }
}