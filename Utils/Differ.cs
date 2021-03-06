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
        /// <summary>
        /// Collect Changes to Apply
        /// </summary>
        public class ChangeCollector
        {
            /// <summary>
            /// Collected Changes
            /// </summary>
            internal Dictionary<ZPoint3D, Material> mCollectedChanges;

            /// <summary>
            /// Create a new Change Collector
            /// </summary>
            internal ChangeCollector()
            {
                mCollectedChanges = new Dictionary<ZPoint3D, Material>();
            }

            /// <summary>
            /// Change a Block in the World (Note that changes are only visible after the collector is applied)
            /// </summary>
            /// <param name="Y">Y Coord</param>
            /// <param name="Z">Z Coord</param>
            /// <param name="X">X Coord</param>
            /// <param name="mat">New Material</param>
            public void ChangeBlock(int Y, int Z, int X, in Material mat)
            {
                mCollectedChanges[new ZPoint3D { Y = Y, Z = Z, X = X }] = mat;
            }
        }

        /// <summary>
        /// Only use this for readonly
        /// </summary>
        public Material[][][] World { get; private set; }

        /// <summary>
        /// Points that have changed
        /// </summary>
        private HashSet<ZPoint3D> mChanges;

        /// <summary>
        /// Create a differ for a world
        /// </summary>
        /// <param name="world">MC World</param>
        public Differ(Material[][][] world)
        {
            World = world;
            mChanges = new HashSet<ZPoint3D>();
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
            mChanges.Add(new ZPoint3D { Y = Y, Z = Z, X = X });
        }

        /// <summary>
        /// Create a Change Collector for multithreaded collection
        /// </summary>
        /// <returns>Change Collector</returns>
        public ChangeCollector CreateCollector()
        {
            return new ChangeCollector();
        }

        /// <summary>
        /// Apply Changes From a Collector for multithreaded collection
        /// </summary>
        /// <param name="coll">Change Collector to apply</param>
        public void ApplyChangeCollector(ChangeCollector coll)
        {
            lock (this)
            {
                foreach (KeyValuePair<ZPoint3D, Material> change in coll.mCollectedChanges)
                {
                    World[change.Key.Y][change.Key.Z][change.Key.X] = change.Value;
                    mChanges.Add(change.Key);
                }
            }
        }

        /// <summary>
        /// Serialize Changes to a binary Writer
        /// </summary>
        /// <param name="writer">Binary Writer to use</param>
        public void SerializeChanges(BinaryWriter writer)
        {
            writer.Write((int)mChanges.Count);
            foreach (ZPoint3D p in mChanges)
            {
                writer.Write((short)p.Y);
                writer.Write((short)p.Z);
                writer.Write((short)p.X);
                writer.Write((short)World[p.Y][p.Z][p.X].ID);
                writer.Write((short)World[p.Y][p.Z][p.X].Data);
            }
        }
    }
}