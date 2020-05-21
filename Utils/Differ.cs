using System;
using System.Collections.Generic;
using System.IO;

using DeluMc.MCEdit.Block;
using DeluMc.MCEdit;


namespace DeluMc.Utils
{
    /// <summary>
    /// Differ Class to help only send the world
    /// </summary>
    public class Differ
    {

        // <summary>
        /// Point with ZCurve for Hashing
        /// </summary>
        private class Point
        {
            /// <summary>
            /// Y Coord
            /// </summary>
            public int Y {get; set;}

            /// <summary>
            /// Z Coord
            /// </summary>
            public int Z {get; set;}

            /// <summary>
            /// X Coord
            /// </summary>
            public int X {get; set;}

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
                    return this.Y == p.Y && this.Z == p.Z && this.X == p.X;
                }
            }

            /// <summary>
            /// Hashing for dictionary using ZCurves
            /// </summary>
            public override int GetHashCode()
            {
                System.Diagnostics.Debug.Assert(Y >= 0 && Z >= 0 && X >= 0);
                return (int)ZCurve.Pos3D((uint)Y, (uint)Z, (uint)X);
            }
        }

        /// <summary>
        /// Only use this for readonly
        /// </summary>
        public Material[][][] World {get; private set;}

        /// <summary>
        /// Points that have changed
        /// </summary>
        private HashSet<Point> mChanges;

        /// <summary>
        /// Create a differ for a world
        /// </summary>
        /// <param name="world">MC World</param>
        public Differ(Material[][][] world)
        {
            World = world;
            mChanges = new HashSet<Point>();
        }

        /// <summary>
        /// Change a Block in the World
        /// </summary>
        /// <param name="Y">Y Coord</param>
        /// <param name="Z">Z Coord</param>
        /// <param name="X">X Coord</param>
        /// <param name="mat">New Material</param>
        public void ChangeBlock(int Y, int Z, int X, in Material mat)
        {
            World[Y][Z][X] = mat;
            mChanges.Add(new Point {Y=Y, Z=Z, X=X});
        }

        /// <summary>
        /// Serialize Changes to a binary Writer
        /// </summary>
        /// <param name="writer">Binary Writer to use</param>
        public void SerializeChanges(BinaryWriter writer)
        {
            writer.Write(mChanges.Count);
            foreach (Point p in mChanges)
            {
                writer.Write(p.Y);
                writer.Write(p.Z);
                writer.Write(p.X);
                writer.Write(World[p.Y][p.Z][p.X].ID);
                writer.Write(World[p.Y][p.Z][p.X].Data);
            }
        }
    }
}