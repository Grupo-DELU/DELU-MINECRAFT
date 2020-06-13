using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;

using DeluMc.Utils;
using DeluMc.MCEdit;
using DeluMc.MCEdit.Block;
using static DeluMc.Buildings.Palettes.PremadePalettes;

namespace DeluMc.Buildings
{
    /// <summary>
    /// House orientation enum
    /// </summary>
    public enum Orientation
    {
        // Door facing X+
        North = 1,
        // Door facing Z+
        East = 2,
        South = 3,
        West = 4,
    };


    /// <summary>
    /// House pivot enum
    /// </summary>
    public enum HousePivot
    {
        BottomLeft = 1,
        Center = 2,
    };


    /// <summary>
    /// Different build types
    /// </summary>
    public enum BuildType
    {
        House = 0,
        Farm = 1,
        Plaza = 2,
    };

    public struct BuildResult
    {
        public bool success;
        public Vector2Int doorPos;
    }


    /// <summary>
    /// Compares houses by area (is inverted to sort in descending order)
    /// </summary>
    public class HouseAreaComparer : IComparer<HousePlacer.HouseSchematic>
    {
        public int Compare(HousePlacer.HouseSchematic a, HousePlacer.HouseSchematic b)
        {
            int areaA = a.size[1] * a.size[2];
            int areaB = b.size[1] * b.size[2];
            return -areaA.CompareTo(areaB);
        }
    }

    
    /// <summary>
    /// Static class responsible of the placement of houses
    /// in the map.
    /// </summary>
    public static class HousePlacer
    {
        private static Dictionary<BuildType, List<HouseSchematic>> houses = new Dictionary<BuildType, List<HouseSchematic>>();

        public struct HouseAreaInput
        {
            public int y;
            public Vector2Int min;
            public Vector2Int max;
            public int[][] roadMap;
            public int[][] houseMap;
            public Material[][][] map;
            public HouseSchematic house;
            public Orientation orientation;
            public Palettes.BuildingPalette palettes;

            public HouseAreaInput(
                int y, 
                Vector2Int min, 
                Vector2Int max, 
                int[][] roadMap, 
                int[][] houseMap, 
                Material[][][] map,
                Orientation orientation,
                Palettes.BuildingPalette palettes) 
            {
                this.y = y;
                this.min = min;
                this.max = max;
                this.map = map;
                this.house = null;
                this.roadMap = roadMap;
                this.houseMap = houseMap;
                this.orientation = orientation;
                this.palettes = palettes;
            }
        }


        /// <summary>
        /// Auxiliary recipient class for the JSON house
        /// schematic deserialization. Size is sent in YZX.
        /// </summary>
        public class HouseSchematic
        {
            public BuildType buildType { get; set; } = 0;
            public char[][][] blocks { get; set; } = null;
            public int[] size { get; set; } = null;
            public int roadStartZ { get; set; }
            public int roadStartX { get; set; }
        }


        /// <summary>
        /// Checks if a specific house fits in an area with
        /// an specific orientation.
        /// </summary>
        /// <param name="request">House in area request</param>
        /// <returns>True if the house fits/False otherwise</returns>
        private static bool CheckBoxFit(in HouseAreaInput request, HouseSchematic house)
        {
            if (request.y + house.size[0] - 1 > request.map.Length)
            {
                return false;
            }
            int sizeX = Math.Abs(request.min.X - request.max.X) + 1;
            int sizeZ = Math.Abs(request.min.Z - request.max.Z) + 1;
            if ((request.orientation & (Orientation.South | Orientation.North)) != 0)
            {
                return house.size[1] <= sizeZ && house.size[2] <= sizeX;
            }
            else
            {
                return house.size[2] <= sizeZ && house.size[1] <= sizeX;
            }
        }      


        private static bool CheckRoadNotBlocked(in HouseAreaInput request, in HouseSchematic house, 
                                                ref BuildResult results, in Differ differ)
        {
            Vector2Int roadPos = new Vector2Int(house.roadStartZ, house.roadStartX);
            Vector2Int pivot = CalculateLeftBottomPivotPlacement(request);
            Console.WriteLine("Roadpost: " + roadPos);
            Console.WriteLine("pivot: " + pivot);
            switch (request.orientation)
            {
                case Orientation.North:
                    roadPos += pivot;
                    break;
                case Orientation.South:
                    pivot += new Vector2Int(-roadPos.Z, -roadPos.X);
                    roadPos = pivot;
                    break;
                case Orientation.East:
                    pivot += new Vector2Int(roadPos.X, -roadPos.Z);
                    roadPos = pivot;
                    break;
                // ??? Should be -X, +Z
                case Orientation.West:
                    pivot += new Vector2Int(-roadPos.X, roadPos.Z);
                    roadPos = pivot;
                    break;
            }
            results.doorPos = roadPos;
            Console.WriteLine("supuesta road pos: " + roadPos);
            return differ.World[request.y + 1][roadPos.Z][roadPos.X] == AlphaMaterials.Air_0_0;
        }


        private static Vector2Int CalculateLeftBottomPivotPlacement(in HouseAreaInput request)
        {
            Vector2Int placement = new Vector2Int();
            switch (request.orientation)
            {
                case Orientation.North:
                    placement.Z = request.min.Z;
                    placement.X = request.min.X;
                    break;
                case Orientation.East:
                    placement.Z = request.min.Z;
                    placement.X = request.max.X;
                    break;
                case Orientation.South:
                    placement.Z = request.max.Z;
                    placement.X = request.max.X;
                    break;
                case Orientation.West:
                    placement.Z = request.max.Z;
                    placement.X = request.min.X;
                    break;
            }
            return placement;
        }


        /// <summary>
        /// Chooses a point in the area (based on the Bottom Left pivot) where
        /// to build a house based on its orientation.
        /// </summary>
        /// <param name="request">House in area request</param>
        private static void BuildInArea(in HouseAreaInput request, Differ differ)
        {
            switch (request.orientation)
            {
                case Orientation.North:
                    BuildHouse(request.map, request.houseMap, request.roadMap, request.y, request.min.Z, request.min.X, request.house, request.orientation, HousePivot.BottomLeft, differ);
                    break;
                case Orientation.East:
                    BuildHouse(request.map, request.houseMap, request.roadMap, request.y, request.min.Z, request.max.X, request.house, request.orientation, HousePivot.BottomLeft, differ);
                    break;
                case Orientation.South:
                    BuildHouse(request.map, request.houseMap, request.roadMap, request.y, request.max.Z, request.max.X, request.house, request.orientation, HousePivot.BottomLeft, differ);
                    break;
                case Orientation.West:
                    BuildHouse(request.map, request.houseMap, request.roadMap, request.y, request.max.Z, request.min.X, request.house, request.orientation, HousePivot.BottomLeft, differ);
                    break;
            }
        }


        /// <summary>
        /// Tries to place the biggest oriented house that fits in the area denoted by
        /// min and max (bounding box).
        /// </summary>
        /// <param name="request">House in area request</param>
        /// <returns>True if a house was built/False otherwise</returns>
        public static BuildResult RequestHouseArea(HouseAreaInput request, BuildType houseType, Differ differ)
        {
            int sizeX = Math.Abs(request.min.X - request.max.X) + 1;
            int sizeZ = Math.Abs(request.min.Z - request.max.Z) + 1;
            int reqArea = sizeX * sizeZ;
            BuildResult results = new BuildResult();

            Console.WriteLine("Area requested: " + reqArea);
            Console.WriteLine("House type requested: " + Enum.GetName(typeof(BuildType), houseType));
            for (int i = 0; i < houses[houseType].Count; i++)
            {
                HouseSchematic house = houses[houseType][i];
                int houseArea = house.size[1] * house.size[2];
                Console.WriteLine("House area: " + houseArea);
                if (houseArea <= reqArea)
                {
                    if (CheckBoxFit(request, house) && CheckRoadNotBlocked(request, house, ref results, differ))
                    {
                        Console.WriteLine("House chosen: " + i);
                        request.house = house;
                        BuildInArea(request, differ);
                        results.success = true;
                        return results;
                    }
                }
            }
            results.success = false;
            return results;
        }


        /// <summary>
        /// Process a block to place in order to build a house. Generally it returns the same
        /// block, except when it is a door block. Also, it draws into the road map if the block
        /// to place is a road block.
        /// </summary>
        /// <param name="y"></param>null
        /// <param name="z"></param>
        /// <param name="x"></param>
        /// <param name="house"></param>
        /// <param name="palette"></param>
        /// <param name="ori"></param>
        /// <returns></returns>
        private static Material ProcessBlock(int y, int z, int x, HouseSchematic house, Palettes.BuildingPalette palette, Orientation ori)
        {
            char blockType = house.blocks[y][z][x];
            Material block = null;
            switch (blockType)
            {
                case 'd':
                    block = ProcessDoor(y, z, x, house, palette, ori);
                    break;
                case 'n':
                    block = null;
                    break;
                default:
                    block = palette.GetFromPalette(blockType);
                    break;
            }
            return block;
        }

        /// <summary>
        /// Identifies the house block type and paints the house/road map accordingly
        /// </summary>
        /// <param name="hy">House block Y position</param>
        /// <param name="hz">House block Z position</param>
        /// <param name="hx">House block X position</param>
        /// <param name="wz">World block Z position</param>
        /// <param name="wx">World block X position</param>
        /// <param name="houseMap">Houses map</param>
        /// <param name="roadMap">Road maps</param>
        /// <param name="house">House to be placed</param>
        private static void PaintMaps(int hy, int hz, int hx, int wz, int wx, int[][] houseMap, int[][] roadMap, HouseSchematic house)
        {
            // Important: Road block must be at the bottom of the schematic box or else, it could be
            // marked as home (applies when doing schematics).
            char blockType = house.blocks[hy][hz][hx];
            if (blockType == 'e')
            {
                Console.WriteLine("World road block: " + wz + ", " + wx);
                roadMap[wz][wx] = 1;
            }
            else
            {
                if (roadMap[wz][wx] == 0)
                {
                    houseMap[wz][wx] = 1;
                }
            }
        }


        /// <summary>
        /// Sets a house door block correct orientation and correct half (top/low)
        /// </summary>
        /// <param name="y">Block house coordinate Y</param>
        /// <param name="z">Block house coordinate Z</param>
        /// <param name="x">Block house coordinate X</param>
        /// <param name="house">House to place</param>
        /// <param name="palette">House palette</param>
        /// <param name="ori">House orientation</param>
        /// <returns>Correct door block material</returns>
        private static Material ProcessDoor(int y, int z, int x, HouseSchematic house, Palettes.BuildingPalette palette, Orientation ori)
        {
            Material door = palette.GetFromPalette('d');
            // Uppder door block
            if (house.blocks[y+1][z][x] != 'd')
            {
                return AlphaMaterials.Set.GetMaterial(door.ID, 8);
            }
            
            switch (ori)
            {
                // IMPORTANT: The doors orientation are inverted in minecraft
                case Orientation.North:
                    return  AlphaMaterials.Set.GetMaterial(door.ID, 2);
                case Orientation.East:
                    return AlphaMaterials.Set.GetMaterial(door.ID, 3);
                case Orientation.South:
                    return door;
                case Orientation.West:
                    return AlphaMaterials.Set.GetMaterial(door.ID, 1);
                default:
                    return null;
            }
        }


        /// <summary>
        /// Builds/places blocks of a schematic house in the map at position (y, z, x)
        /// with certain orientation and relative to a pivot in the house.
        /// </summary>
        /// <param name="map">Map to place house on</param>
        /// <param name="y">House y position</param>
        /// <param name="z">House z pivot position</param>
        /// <param name="x">House x pivot position</param>
        /// <param name="house">House to place</param>
        /// <param name="or">House orientation</param>
        /// <param name="pivot">House pivot</param>
        private static void BuildHouse(Material[][][] map, int[][] houseMap, int[][] roadMap, int y, int z, int x, HouseSchematic house, Orientation or, HousePivot pivot, Differ differ)
        {
            // Orig is the house "left bottom corner" in world position (something like min)
            int origZ, origX; 
            // Used to substract half of the house lenght in xz axis
            // in case the pivot is the center of the house
            int mod = (pivot == HousePivot.Center ? 1: 0);

            // Y iteration
            for (int i = 0; i < house.size[0]; ++i)
            {
                // Z iteration
                for (int k = 0; k < house.size[1]; ++k)
                {
                    // X iteration
                    for (int j = 0; j < house.size[2]; ++j)
                    {
                        Material block = ProcessBlock(i,k,j, house, forestPalette, or);
                        switch (or)
                        {
                            case Orientation.North:
                                origZ = z - house.size[1]/2 * mod;
                                origX = x - house.size[2]/2 * mod;
                                differ.ChangeBlock(y + i, origZ + k, origX + j, (block != null ? block : map[y + i][origZ + k][origX + j]));
                                PaintMaps(i, k, j, origZ + k, origX + j, houseMap, roadMap, house);
                                break;
                            case Orientation.East:
                                origZ = z - house.size[2]/2 * mod;
                                origX = x + house.size[1]/2 * mod;
                                differ.ChangeBlock(y + i, origZ + j, origX - k, (block != null ? block : map[y + i][origZ + j][origX - k]));
                                PaintMaps(i, k, j, origZ + j, origX - k, houseMap, roadMap, house);
                                break;  
                            case Orientation.South:
                                origZ = z + house.size[1]/2 * mod; // Must check if ModZ is needed or not
                                origX = x + house.size[2]/2 * mod;
                                differ.ChangeBlock(y + i, origZ - k, origX - j, (block != null ? block : map[y + i][origZ - k][origX - j]));
                                PaintMaps(i, k, j, origZ - k, origX - j, houseMap, roadMap, house);
                                break;
                            case Orientation.West:
                                origZ = z + house.size[2]/2 * mod; // Must check if ModZ/X is needed or not 
                                origX = x - house.size[1]/2 * mod; // Must check if ModZ/X is needed or not
                                differ.ChangeBlock(y + i,origZ - j,origX + k, (block != null ? block : map[y + i][origZ - j][origX + k]));
                                PaintMaps(i, k, j, origZ - k, origX - j, houseMap, roadMap, house);
                                break;
                        }
                    }
                }
            }
        }


        /// <summary>
        /// Constructor. Loads the houses from the .json files and sort them.
        /// </summary>
        static HousePlacer()
        {
            // Builds the dictionary that contais the houses
            foreach (BuildType t in Enum.GetValues(typeof(BuildType)))
            {
                houses[t] = new List<HouseSchematic>();
            }

            string housesPath = System.IO.Path.GetDirectoryName(
                System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);
            
            try
            {
#if LINUX 
                housesPath = housesPath.Replace("file:","");
#endif
                housesPath = new System.Uri(housesPath).LocalPath;
            }
            catch (UriFormatException e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
            housesPath = new System.Uri(housesPath).LocalPath;    // Removing file: at start
            housesPath = Path.Join(housesPath, "..", "..", "schematics");
            housesPath = Path.GetFullPath(housesPath);

            Console.WriteLine($"Loading house schematics at \"{housesPath}\"");
            
            string[] files = Directory.GetFiles(housesPath, "*.json");

            for (int i = 0; i < files.Length; i++)
            {
                HouseSchematic house = JsonSerializer.Deserialize<HouseSchematic>(File.ReadAllText(files[i]));
                houses[house.buildType].Add(house);
                Console.WriteLine($"Loaded house {files[i]}");
            }

            // Sorts every house list by area (descending order)
            foreach (var key in houses.Keys)
            {
                houses[(BuildType)key].Sort(new HouseAreaComparer());
            }
        }
    }
}