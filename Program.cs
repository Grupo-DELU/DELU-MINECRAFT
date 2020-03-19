using System;
using static Tensorflow.Binding;
using Keras;
using Tensorflow;

namespace DELU_CSharp
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            var hello = tf.constant("Hello, TensorFlow!");

            // Start tf session
            using (var sess = tf.Session())
            {
                // Run the op
                var result = sess.run(hello);
                Console.WriteLine($"{result}");
            }

        }
    }
}