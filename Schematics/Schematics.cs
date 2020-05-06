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


    // TODO: Better method of finding suitable house
    // TODO: Check if house overlaps water/tree/road/house map (maybe not water, neither tree map) 
    // TODO: Finish comments
    // TODO: Probably the bound/size check can be unified like the building.
    // TODO: Also do a request struct for individual placement
    
    /// <summary>
    /// Static class responsible of the placement of houses
    /// in the map.
    /// </summary>
    public static class HousePlacer
    {
        /// <summary>
        /// Houses loaded from the .json files.
        /// </summary>
        private static HouseSchematic[] houses;

        public struct HouseAreaInput
        {
            public int y;
            public Vector2Int min;
            public Vector2Int max;
            public int[][][] roadMap;
            public int[][][] houseMap;
            public Material[][][] map;
            public HouseSchematic house;
            public Orientation orientation;
            public Palettes.BuildingPalette palettes;

            public HouseAreaInput(
                int y, 
                Vector2Int min, 
                Vector2Int max, 
                int[][][] roadMap, 
                int[][][] houseMap, 
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
            public char[][][] blocks { get; set; } = null;
            public int[] size { get; set; } = null;
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

        
        /// <summary>
        /// Chooses a point in the area (based on the Bottom Left pivot) where
        /// to build a house based on its orientation.
        /// </summary>
        /// <param name="request">House in area request</param>
        private static void BuildInArea(in HouseAreaInput request)
        {
            switch (request.orientation)
            {
                case Orientation.North:
                    BuildHouse(request.map, request.y, request.min.Z,request.min.X, request.house, request.orientation, HousePivot.BottomLeft);
                    break;
                case Orientation.East:
                    BuildHouse(request.map, request.y, request.min.Z, request.max.X, request.house, request.orientation, HousePivot.BottomLeft);
                    break;
                case Orientation.South:
                    BuildHouse(request.map, request.y, request.max.Z, request.max.X, request.house, request.orientation, HousePivot.BottomLeft);
                    break;
                case Orientation.West:
                    BuildHouse(request.map, request.y, request.max.Z, request.min.X, request.house, request.orientation, HousePivot.BottomLeft);
                    break;
            }
        }


        /// <summary>
        /// Tries to place the biggest oriented house that fits in the area denoted by
        /// min and max (bounding box).
        /// </summary>
        /// <param name="request">House in area request</param>
        /// <returns>True if a house was built/False otherwise</returns>
        public static bool RequestHouseArea(HouseAreaInput request)
        {
            int sizeX = Math.Abs(request.min.X - request.max.X) + 1;
            int sizeZ = Math.Abs(request.min.Z - request.max.Z) + 1;
            int reqArea = sizeX * sizeZ;

            Console.WriteLine("Area requested: " + reqArea);
            for (int i = 0; i < houses.Length; i++)
            {
                int houseArea = houses[i].size[1] * houses[i].size[2];
                Console.WriteLine("House area: " + houseArea);
                if (houseArea <= reqArea)
                {
                    if (CheckBoxFit(request, houses[i]))
                    {
                        Console.WriteLine("House chosen: " + i);
                        request.house = houses[i];
                        BuildInArea(request);
                        return true;
                    }
                }
            }
            return false;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="map"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="x"></param>
        /// <param name="house"></param>
        /// <param name="ori"></param>
        /// <returns></returns>
        private static bool CheckBottomLeftBound(in Material[][][] map, int y, int z, int x, HouseSchematic house, Orientation ori)
        {
            bool status = true;

            switch (ori)
            {
                case Orientation.North:
                    status = status && z + house.size[1] - 1 < map[0].Length;
                    status = status && x + house.size[2] < map[0][0].Length;
                    break;
    
                case Orientation.East:
                    status = status && z + house.size[2] - 1 < map[0].Length;
                    status = status && x - house.size[1] + 1 >= 0; 
                    break;
    
                case Orientation.South:
                    status = status && z - house.size[1] + 1 >= 0;
                    status = status && x - house.size[2] + 1 >= 0;
                    break;
    
                case Orientation.West:
                    status = status && x + house.size[1] - 1 < map[0][0].Length;
                    status = status && z - house.size[2] + 1 >= 0; 
                    break;
            }
            
            return status && (y + house.size[0] - 1 < map.Length);
        } 
        

        /// <summary>
        /// Process a block to place in order to build a house. Generally it returns the same
        /// block, except when it is a door block. Also, it draws into the road map if the block
        /// to place is a road block.
        /// </summary>
        /// <param name="y"></param>
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
                case 'e':
                    // Pintar carretera acaa
                    block = palette.GetFromPalette(blockType);
                    break;
                default:
                    block = palette.GetFromPalette(blockType);
                    break;
            }
            return block;
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
        /// 
        /// </summary>
        /// <param name="map"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <param name="x"></param>
        /// <param name="house"></param>
        /// <param name="or"></param>
        private static bool BuildHouse(Material[][][] map, int y, int z, int x, HouseSchematic house, Orientation or, HousePivot pivot)
        {
            int origZ, origX; 
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
                                map[y + i][origZ + k][origX + j] = block;
                                break;
                            case Orientation.East:
                                origZ = z - house.size[2]/2 * mod;
                                origX = x + house.size[1]/2 * mod;
                                map[y + i][origZ + j][origX - k] = block;
                                break;  
                            case Orientation.South:
                                origZ = z + house.size[1]/2 * mod; // Must check if ModZ is needed or not
                                origX = x + house.size[2]/2 * mod;
                                map[y + i][origZ - k][origX - j] = block;
                                break;
                            case Orientation.West:
                                origZ = z + house.size[2]/2 * mod; // Must check if ModZ/X is needed or not 
                                origX = x - house.size[1]/2 * mod; // Must check if ModZ/X is needed or not
                                map[y + i][origZ - j][origX + k] = block;
                                break;
                        }
                    }
                }
            }
            return true;
        }


        /// <summary>
        /// Places a house with its pivot in (y, z, x)
        /// </summary>
        /// <param name="map">Map to place house in</param>
        /// <param name="y">House pivot y</param>
        /// <param name="z">House pivot z</param>
        /// <param name="x">House pivot x</param>
        /// <param name="path">House path file</param>
        /// <param name="orientation">House orientation/rotation</param>
        /// <param name="pivot">House pivot</param>
        /// <returns></returns>
        public static bool PlaceHouse(Material[][][] map, int y, int z, int x, in string path, 
            Orientation orientation, HousePivot pivot = HousePivot.BottomLeft)
        {
            HouseSchematic house = houses[0]; // PlaceHolder
            bool result = false;
            switch (pivot)
            {  
                case HousePivot.BottomLeft:
                    result = CheckBottomLeftBound(map, y, z, x, house, orientation);
                    break;
                case HousePivot.Center:
                    result = CheckBottomLeftBound(map, y, z, x, house, orientation);
                    break;
            }
            if (result)
            {
                BuildHouse(map, y, z, x, house, orientation, pivot);
            }
            return result;
        }


        /// <summary>
        /// Constructor. Loads the houses from the .json files and sort them.
        /// </summary>
        static HousePlacer()
        {
            string housesPath = System.IO.Path.GetDirectoryName(
                System.Reflection.Assembly.GetExecutingAssembly().GetName().CodeBase);
            
            housesPath = new System.Uri(housesPath).LocalPath;    // Removing file: at start
            housesPath = Path.Join(housesPath, "..", "..", "..", "HouseSCH");
            housesPath = Path.GetFullPath(housesPath);

            Console.WriteLine($"Loading house schematics at \"{housesPath}\"");
            
            string[] files = Directory.GetFiles(housesPath, "*.json");
            
            houses = new HouseSchematic[files.Length];

            for (int i = 0; i < files.Length; i++)
            {
                houses[i] = JsonSerializer.Deserialize<HouseSchematic>(File.ReadAllText(files[i]));
                Console.WriteLine("Loaded house " + files[i]);
            }
            Array.Sort(houses, new HouseAreaComparer());
        }
    }
}