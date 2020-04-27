using System.Threading.Tasks;

namespace DeluMc.Utils
{
    public static class Tasker
    {

        public delegate void IterateChunk(int zStart, int zEnd, int xStart, int xEnd);

        public delegate void IterateBlock(int x, int z);

        public const int kZSubdivisions = 4;
        public const int kXSubdivisions = 4;
        public const int kAmountOfTasks = kZSubdivisions * kXSubdivisions;


        public static void Run2DTasks(int zSize, int xSize, IterateChunk[] perChunk, IterateBlock[] perBlock)
        {
            int zStep = (zSize / kZSubdivisions);
            int xStep = (xSize / kXSubdivisions);
            int c = 0;
            Task[] tasks = new Task[kAmountOfTasks];

            // Be carefull with lambda variable catch
            for (int zIter = 0; zIter < kZSubdivisions; ++zIter)
            {
                int zStart = zIter * zStep;
                for (int xIter = 0; xIter < kXSubdivisions; ++xIter)
                {
                    int xStart = xIter * xStep;

                    int zEnd = zStep * (zIter + 1);
                    int xEnd = xStep * (xIter + 1);
                    if (zIter == kZSubdivisions - 1)
                    {
                        zEnd = zSize - 1;
                    }
                    if (xIter == kZSubdivisions - 1)
                    {
                        xEnd = xSize - 1;
                    }

                    tasks[c] = Task.Run(
                        () =>
                        {
                            for (int i = 0; i < perChunk.Length; i++)
                            {
                                perChunk[i].Invoke(zStart, zEnd, xStart, xEnd);
                            }
                            if (perBlock.Length > 0)
                            {
                                for (int z = zStart; z < zEnd; z++)
                                {
                                    for (int x = xStart; x < xEnd; x++)
                                    {
                                        for (int i = 0; i < perBlock.Length; i++)
                                        {
                                            perBlock[i].Invoke(z, x);
                                        }
                                    }
                                }
                            }
                        }
                        );

                    ++c;
                }
            }
            Task.WaitAll(tasks);
        }
    }
}