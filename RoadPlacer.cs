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
    public class RoadPlacer
    {
        // <summary>
        /// Point with ZCurve for Hashing
        /// </summary>
        private class Point
        {
            /// <summary>
            /// This exists because C# doesn't like inheritance for structs
            /// </summary>
            public Vector2Int RealPoint { get; set; }

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

        public static void RoadsPlacer(
            List<List<Vector2Int>> roads, int[][] roadMap, int[][] heightMap, Biomes[][] biomes,
            Material[][][] world)
        {
            RectInt rectCover = new RectInt(Vector2Int.Zero, new Vector2Int(roadMap.Length - 1, roadMap[0].Length - 1));
            HashSet<Point> bridge = new HashSet<Point>();

            List<Vector2Int> road;
            int nz, nx, count;
            int averageHeight;

            for (int j = 0; j < roads.Count; j++)
            {
                road = roads[j];

                if (road.Count < 1)
                {
                    continue;
                }

                for (int i = 0; i < road.Count; i++)
                {
                    if (roadMap[road[i].Z][road[i].X] == RoadGenerator.MainRoadMarker)
                    {
                        count = 0;
                        averageHeight = 0;

                        if (0 <= i - 2 && roadMap[road[i - 2].Z][road[i - 2].X] == RoadGenerator.MainRoadMarker)
                        {
                            averageHeight += heightMap[road[i - 2].Z][road[i - 2].X];
                            ++count;
                        }

                        if (i + 2 < road.Count && roadMap[road[i + 2].Z][road[i + 2].X] == RoadGenerator.MainRoadMarker)
                        {
                            averageHeight += heightMap[road[i + 2].Z][road[i + 2].X];
                            ++count;
                        }

                        for (int dz = -1; dz <= 1; dz++)
                        {
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                nz = road[i].Z + dz;
                                nx = road[i].X + dx;
                                if (rectCover.IsInside(nz, nx) && roadMap[nz][nx] == RoadGenerator.MainRoadMarker)
                                {
                                    averageHeight += heightMap[nz][nx];
                                    ++count;
                                }
                            }
                        }
                        averageHeight /= count;

                        // Normal Road
                        for (int dz = -1; dz <= 1; dz++)
                        {
                            for (int dx = -1; dx <= 1; dx++)
                            {
                                nz = road[i].Z + dz;
                                nx = road[i].X + dx;
                                if (
                                    rectCover.IsInside(nz, nx)
                                    && (roadMap[nz][nx] == RoadGenerator.MainRoadMarker || roadMap[nz][nx] == RoadGenerator.RoadMarker))
                                {
                                    world[averageHeight][nz][nx] = AlphaMaterials.Stone_1_0;
                                    for (int dy = 1; dy <= 2; dy++)
                                    {
                                        world[averageHeight + dy][nz][nx] = AlphaMaterials.Air_0_0;
                                    }
                                }
                            }
                        }

                    }
                    else
                    {
                        // Bridge
                    }
                }
            }
        }
    }
}