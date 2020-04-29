using System.Threading.Tasks;
using System;

namespace DeluMc.Utils
{
    /// <summary>
    /// Task Helper
    /// </summary>
    public static class Tasker
    {
        /// <summary>
        /// Work over a Chunk in a 2D Map
        /// The Chunk goes from [zStart, zEnd) to [xStart, xEnd)
        /// </summary>
        /// <param name="zStart">Z Start (Inclusive)</param>
        /// <param name="zEnd">Z End (Exclusive)</param>
        /// <param name="xStart">X Start (Inclusive)</param>
        /// <param name="xEnd">X End (Exclusive)</param>
        public delegate void WorkChunk(int zStart, int zEnd, int xStart, int xEnd);

        /// <summary>
        /// Work Over a Block in (z, x)
        /// </summary>
        /// <param name="z">Z Position</param>
        /// <param name="x">X Position</param>
        public delegate void WorkBlock(int z, int x);

        /// <summary>
        /// Number of Subdivision to do in Z
        /// </summary>
        public const int kZSubdivisions = 4;

        /// <summary>
        /// Number of Subdivision to do in X
        /// </summary>
        public const int kXSubdivisions = 4;

        /// <summary>
        /// Input Data Helper Struct (POD)
        /// </summary>
        private struct InputData
        {
            /// <summary>
            /// Z Start (Inclusive)
            /// </summary>
            public int zStart;

            /// <summary>
            /// Z End (Exclusive)
            /// </summary>
            public int zEnd;

            /// <summary>
            /// X Start (Inclusive)
            /// </summary>
            public int xStart;

            /// <summary>
            /// X End (Exclusive)
            /// </summary>
            public int xEnd;
        }

        /// <summary>
        /// Run Tasks on a 2D map
        /// </summary>
        /// <param name="zSize">Z Axis Size</param>
        /// <param name="xSize">X Axis Size</param>
        /// <param name="perChunk">Work Per Chunk to do (can be null)</param>
        /// <param name="perBlock">Work Per Block to do (can be null)</param>
        public static void Run2DTasks(int zSize, int xSize, WorkChunk[] perChunk, WorkBlock[] perBlock)
        {
            bool requiresPerChunk = perChunk != null && perChunk.Length > 0;
            bool requiresPerBlock = perBlock != null && perBlock.Length > 0;

            Action<Object> runAction = null;

            if (requiresPerChunk && requiresPerBlock)
            {
                runAction =
                    (Object input) =>
                {
                    InputData data = (InputData)input;
                    int i;
                    for (i = 0; i < perChunk.Length; i++)
                    {
                        perChunk[i].Invoke(data.zStart, data.zEnd, data.xStart, data.xEnd);
                    }

                    int x;
                    for (int z = data.zStart; z < data.zEnd; z++)
                    {
                        for (x = data.xStart; x < data.xEnd; x++)
                        {
                            for (i = 0; i < perBlock.Length; i++)
                            {
                                perBlock[i].Invoke(z, x);
                            }
                        }
                    }
                };
            }
            else if (requiresPerChunk)
            {
                runAction =
                    (Object input) =>
                {
                    InputData data = (InputData)input;
                    for (int i = 0; i < perChunk.Length; i++)
                    {
                        perChunk[i].Invoke(data.zStart, data.zEnd, data.xStart, data.xEnd);
                    }
                };
            }
            else if (requiresPerBlock)
            {
                runAction =
                    (Object input) =>
                {
                    InputData data = (InputData)input;
                    int x;
                    int i;
                    for (int z = data.zStart; z < data.zEnd; z++)
                    {
                        for (x = data.xStart; x < data.xEnd; x++)
                        {
                            for (i = 0; i < perBlock.Length; i++)
                            {
                                perBlock[i].Invoke(z, x);
                            }
                        }
                    }
                };
            }
            else
            {
                Console.Error.WriteLine("Nothing to do");
                return;
            }

            int zSubdivisions = Math.Min(kZSubdivisions, zSize);
            int xSubdivisions = Math.Min(kXSubdivisions, xSize);

            int zStep = (zSize / zSubdivisions);
            int xStep = (xSize / xSubdivisions);
            int taskIter = 0;
            Task[] tasks = new Task[zSubdivisions * xSubdivisions];

            int zStart;
            int zEnd;
            int xStart;
            int xEnd;
            int xIter;

            // Be carefull with lambda variable catch
            for (int zIter = 0; zIter < zSubdivisions; ++zIter)
            {
                zStart = zIter * zStep;
                for (xIter = 0; xIter < xSubdivisions; ++xIter)
                {
                    zEnd = zStep * (zIter + 1);
                    xStart = xIter * xStep;
                    xEnd = xStep * (xIter + 1);

                    if (zIter == kZSubdivisions - 1)
                    {
                        zEnd = zSize;
                    }
                    if (xIter == kZSubdivisions - 1)
                    {
                        xEnd = xSize;
                    }

                    tasks[taskIter] =
                        Task.Factory.StartNew(
                            runAction,
                            new InputData { zStart = zStart, zEnd = zEnd, xStart = xStart, xEnd = xEnd }
                    );

                    ++taskIter;
                }
            }
            Task.WaitAll(tasks);
        }
    }
}