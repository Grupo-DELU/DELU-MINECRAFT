using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DeluMc.Utils;
using Utils.Collections;
using Utils.SpatialTrees.QuadTrees;
using DeluMc.Masks;
using DeluMc.MCEdit;
using DeluMc.MCEdit.Block;

namespace DeluMc
{
    /// <summary>
    /// Utility in charge of placing blocks in the marked roads and bridges
    /// </summary>
    public static class WaterAnalyzer
    {
        // <summary>
        /// Point with ZCurve for Hashing
        /// </summary>
        public class Point
        {
            /// <summary>
            /// This exists because C# doesn't like inheritance for structs
            /// </summary>
            public Vector2Int RealPoint { get; internal set; }

            /// <summary>
            /// C# Object Equality
            /// </summary>
            /// <param name="obj">Other Object</param>
            /// <returns>If other object is equals</returns>
            public override bool Equals(Object obj)
            {
                //Check for null and compare run-time types.
                if ((obj == null) || !this.GetType().Equals(obj.GetType()))
                {
                    return false;
                }
                else
                {
                    Point p = (Point)obj;
                    return this.RealPoint.Equals(p.RealPoint);
                }
            }

            /// <summary>
            /// Hashing for dictionary using ZCurves
            /// </summary>
            public override int GetHashCode()
            {
                System.Diagnostics.Debug.Assert(RealPoint.Z >= 0 && RealPoint.X >= 0);
                return (int)ZCurve.Pos2D((uint)RealPoint.Z, (uint)RealPoint.X);
            }
        }

        /// <summary>
        /// Water Body
        /// </summary>
        public class WaterBody
        {
            /// <summary>
            /// Points Belonging to the Water Body
            /// </summary>
            public HashSet<Point> Points { get; internal set; }

            /// <summary>
            /// Quadtree of Points in the Body of Water
            /// </summary>
            public DataQuadTree<Point> PointsQT { get; internal set; }

        }

        /// <summary>
        /// Compares WaterBody by area
        /// </summary>
        public class WaterBodyComparer : IComparer<WaterBody>
        {
            public int Compare(WaterBody a, WaterBody b)
            {
                return b.Points.Count.CompareTo(a.Points.Count);
            }
        }

        /// <summary>
        /// Results of Water Analysis
        /// </summary>
        public class WaterAnalysis
        {
            /// <summary>
            /// Valid Water Bodies Found
            /// </summary>
            public List<WaterBody> WaterBodies { get; internal set; }

            /// <summary>
            /// Water Bodies Considered to be invalid
            /// </summary>
            public HashSet<Point> InvalidWaterBodiesSet { get; internal set; }

            /// <summary>
            /// Valid Water Bodies Found
            /// </summary>
            public List<WaterBody> InvalidWaterBodies { get; internal set; }

            /// <summary>
            /// Create a new Water Analysis
            /// </summary>
            public WaterAnalysis()
            {
                WaterBodies = new List<WaterBody>();
                InvalidWaterBodies = new List<WaterBody>();
                InvalidWaterBodiesSet = new HashSet<Point>();
            }
        }

        /// <summary>
        /// Analyze the WaterMap to get the water bodies
        /// </summary>
        /// <param name="waterMap">Water Map to Analyze</param>
        /// <param name="minValidWaterSize">Minimum Valid size for a Body of Water</param>
        /// <returns>Analysis of Water Map</returns>
        public static WaterAnalysis AnalyzeWater(in int[][] waterMap, int minValidWaterSize)
        {
            System.Diagnostics.Debug.Assert(waterMap.Length > 0 && waterMap[0].Length > 0);

            WaterAnalysis analysis = new WaterAnalysis();

            RectInt mapCover = new RectInt { Min = Vector2Int.Zero, Max = new Vector2Int(waterMap.Length - 1, waterMap[0].Length - 1) };

            HashSet<Point> visited = new HashSet<Point>();
            List<Point> currBodyOfWater = new List<Point>();

            Point curr, child;
            int currPos, nz, nx;

            for (int z = 0; z < waterMap.Length; z++)
            {
                for (int x = 0; x < waterMap[0].Length; x++)
                {
                    curr = new Point { RealPoint = new Vector2Int(z, x) };
                    if (waterMap[z][x] == 1 && !visited.Contains(curr))
                    {
                        // New Body of Water
                        currBodyOfWater.Clear();
                        visited.Add(curr);
                        currBodyOfWater.Add(curr);
                        currPos = 0;
                        // BFS to get all the water that is connected
                        while (currPos != currBodyOfWater.Count)
                        {
                            curr = currBodyOfWater[currPos];
                            for (int dz = -1; dz <= 1; dz++)
                            {
                                for (int dx = -1; dx <= 1; dx++)
                                {
                                    nz = curr.RealPoint.Z + dz;
                                    nx = curr.RealPoint.X + dx;
                                    child = new Point { RealPoint = new Vector2Int(nz, nx) };
                                    if (mapCover.IsInside(child.RealPoint) && waterMap[nz][nx] == 1 && !visited.Contains(child))
                                    {
                                        visited.Add(child);
                                        currBodyOfWater.Add(child);
                                    }
                                }
                            }
                            ++currPos;
                        }

                        WaterBody body = new WaterBody();
                        body.PointsQT = new DataQuadTree<Point>(mapCover.Min, mapCover.Max);
                        body.Points = new HashSet<Point>(currBodyOfWater.Count);
                        for (int i = 0; i < currBodyOfWater.Count; i++)
                        {
                            body.PointsQT.Insert(currBodyOfWater[i].RealPoint, currBodyOfWater[i]);
                            body.Points.Add(currBodyOfWater[i]);
                        }

                        if (currBodyOfWater.Count >= minValidWaterSize)
                        {
                            analysis.WaterBodies.Add(body);
                        }
                        else
                        {
                            for (int i = 0; i < currBodyOfWater.Count; i++)
                            {
                                analysis.InvalidWaterBodiesSet.Add(currBodyOfWater[i]);
                            }
                            analysis.InvalidWaterBodies.Add(body);
                        }
                    }
                }
            }

            WaterBodyComparer waterBodyComparer = new WaterBodyComparer();
            analysis.InvalidWaterBodies.Sort(waterBodyComparer);
            analysis.WaterBodies.Sort(waterBodyComparer);
            return analysis;
        }
    }
}