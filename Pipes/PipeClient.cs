using System.IO;
using System.IO.Pipes;

namespace DeluMc.Pipes
{
    public class PipeClient
    {
        /// <summary>
        /// Pipe General Name
        /// </summary>
        private string mName;

        /// <summary>
        /// Pipe Client
        /// </summary>
        private NamedPipeClientStream mPipeClient;

        /// <summary>
        /// Create a Pipe Cliente with a General Name
        /// </summary>
        /// <param name="name">General Pipe Name</param>
        public PipeClient(in string name)
        {
            mName = name;
        }

        /// <summary>
        /// Init the underlying pipe
        /// </summary>
        public void Init()
        {
            mPipeClient = new NamedPipeClientStream(".", mName, PipeDirection.InOut);
            mPipeClient.Connect();
        }

        /// <summary>
        /// Deinit underlying pipe
        /// </summary>
        public void DeInit()
        {
            mPipeClient.Close();
        }

        /// <summary>
        /// Read a memory block from the pipe
        /// </summary>
        /// <returns>Binary Reader to interpret the memory block</returns>
        public BinaryReader ReadMemoryBlock()
        {
            MemoryStream memory;
            int memSize;
            using (BinaryReader reader = new BinaryReader(mPipeClient, System.Text.Encoding.UTF8, true))
            {
                memSize = reader.ReadInt32();
                memory = new MemoryStream(reader.ReadBytes(memSize));
            }

            return new BinaryReader(memory, System.Text.Encoding.UTF8);
        }

        /// <summary>
        /// Write a Memory Block to the pipe Stream
        /// </summary>
        /// <param name="memory"></param>
        public void WriteMemoryBlock(MemoryStream memory)
        {
            using (BinaryWriter writer = new BinaryWriter(mPipeClient, System.Text.Encoding.UTF8, true))
            {
                writer.Write((int)memory.Length); // Write memory block as size an integer
            }
            memory.WriteTo(mPipeClient);
            mPipeClient.Flush();
        }

        ~PipeClient()
        {
            mPipeClient.Dispose();
            mPipeClient = null;
        }
    }
}