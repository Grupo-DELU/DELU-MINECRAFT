using System;
using System.IO;
using System.Text.Json;

using DeluMc.MCEdit;
using DeluMc.MCEdit.Block;
using static DeluMc.HouseSchematics.Palettes.Palettes;

namespace DeluMc.HouseSchematics
{
    // TODO: Initialize a list sorted by house area/x/z.
    // TODO: Placement by area and biggest house.
    // TODO: Check if house overlaps water/tree/road/house map (maybe not water, neither tree map) 
    // TODO: Finish comments
    
    /// <summary>
    /// Static class responsible of the placement of houses
    /// in the map.
    /// </summary>
    public static class HousePlacer
    {
        // TODO: Decide if we should take the enums out

        /// <summary>
        /// House orientation enum
        /// </summary>
        public enum Orientation
        {
            // Door facing X+
            North,
            // Door facing Z+
            East,
            South,
            West,
        };

        /// <summary>
        /// House pivot enum
        /// </summary>
        public enum HousePivot
        {
            BottomLeft,
            Center,
        };

        /// <summary>
        /// Auxiliary recipient class for the JSON house
        /// schematic deserialization. Size is sent in YZX.
        /// </summary>
        private class HouseSchematic
        {
            public char[][][] blocks { get; set; } = null;
            public int[] size { get; set; } = null;
        }

        /// <summary>
        /// Loads a house schematic from a JSON and returns it.
        /// </summary>
        /// <param name="path">Path of the JSON to load.</param>
        /// <returns></returns>
        private static HouseSchematic LoadHouse(in string path)
        {
            HouseSchematic house = JsonSerializer.Deserialize<HouseSchematic>(File.ReadAllText(path));
            return house;
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
        /// <returns></returns>
        private static bool CheckCenteredBound(in Material[][][] map, int y, int z, int x, HouseSchematic house, Orientation or)
        {
            bool status = true;
            
            int halfZ = house.size[1]/2;
            int halfX = house.size[2]/2;

            int modZ = (house.size[1] % 2 == 0 ? 1 : 0);
            int modX = (house.size[2] % 2 == 0 ? 1 : 0);

            switch (or)
            {
                case Orientation.North:
                    status = status && z + halfZ - modZ < map[0].Length && z - halfZ >= 0;
                    status = status && x + halfX - modX < map[0][0].Length && x - halfX >= 0;
                    break;
                case Orientation.East:
                    status = status && (x - halfZ + modZ >= 0 && x + halfZ < map[0][0].Length);
                    status = status && (z + halfX - modX < map[0].Length && z - halfX >= 0);
                    break;
                case Orientation.South:
                    status = status && z + halfZ < map[0].Length && z - halfZ + modZ >= 0;
                    status = status && x + halfX < map[0][0].Length && x - halfX + modX >= 0;
                    break;
                case Orientation.West:
                    status = status && x + halfZ + modZ < map[0].Length && x - halfZ >= 0;
                    status = status && z + halfX - modX < map[0][0].Length && z + halfX >= 0;
                    break;
            }
            
            return status && (y + house.size[0] - 1 < map.Length);;
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
        /// <returns></returns>
        private static bool BuildCenteredHouse(in Material[][][] map, int y, int z, int x, HouseSchematic house, Orientation or)
        {
            if (!CheckCenteredBound(map, y, z, x, house, or))
                return false;

            //int modZ = (house.size[1] % 2 == 0 ? 1 : 0);        
            //int modX = (house.size[2] % 2 == 0 ? 1 : 0);

            int origZ, origX;

            // Y iteration
            for (int i = 0; i < house.size[0]; ++i)
            {
                // Z iteration
                for (int k = 0; k < house.size[1]; ++k)
                {
                    // X iteration
                    for (int j = 0; j < house.size[2]; ++j)
                    {
                        switch (or)
                        {
                            case Orientation.North:
                                origZ = z - house.size[1]/2;
                                origX = x - house.size[2]/2;
                                map[y + i][origZ + k][origX + j] = forestPalette.GetFromPalette(house.blocks[i][k][j]);
                                break;
                            case Orientation.East:
                                origZ = z - house.size[2]/2;
                                origX = x + house.size[1]/2;
                                map[y + i][origZ + j][origX - k] = forestPalette.GetFromPalette(house.blocks[i][k][j]);
                                break;  
                            case Orientation.South:
                                origZ = z + house.size[1]/2; // Must check if ModZ is needed or not
                                origX = x + house.size[2]/2;
                                map[y + i][origZ - k][origX - j] = forestPalette.GetFromPalette(house.blocks[i][k][j]);
                                break;
                            case Orientation.West:
                                origZ = z + house.size[2]/2; // Must check if ModZ/X is needed or not 
                                origX = x - house.size[1]/2; // Must check if ModZ/X is needed or not
                                map[y + i][origZ - j][origX + k] = forestPalette.GetFromPalette(house.blocks[i][k][j]);
                                break;
                        }
                    }
                }
            }
            return true;
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
        /// <returns></returns>
        private static bool CheckBottomLeftBound(in Material[][][] map, int y, int z, int x, HouseSchematic house, Orientation or)
        {
            bool status = true;

            switch (or)
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
        /// Builds a house with it bottom left corner in (y, z, x)
        /// </summary>
        /// <param name="map">Map to place the house in</param>
        /// <param name="y">House bottom left corner y</param>
        /// <param name="z">House bottom left corner z</param>
        /// <param name="x">House bottom left corner x</param>
        /// <param name="house">House schematic</param>
        /// <param name="orientation">House rotation/orientation</param>
        /// <returns>True if the house can be placed/False otherwise</returns>
        private static bool BuildBottomLeftHouse(in Material[][][] map, int y, int z, int x, HouseSchematic house, Orientation orientation)
        {
            if (!CheckBottomLeftBound(map, y, z, x, house, orientation))
                return false;      

            // TODO; This could be fused with center one (+ +, + -, - -, - +)
            // Y iteration
            for (int i = 0; i < house.size[0]; ++i)
            {
                // Z iteration
                for (int k = 0; k < house.size[1]; ++k)
                {
                    // X iteration
                    for (int j = 0; j < house.size[2]; ++j)
                    {
                        switch (orientation)
                        {
                            case Orientation.North:
                                map[y + i][z + k][x + j] = forestPalette.GetFromPalette(house.blocks[i][k][j]);
                                break;
                            case Orientation.East:
                                map[y + i][z + j][x - k] = forestPalette.GetFromPalette(house.blocks[i][k][j]);
                                break;  
                            case Orientation.South:
                                map[y + i][z - k][x - j] = forestPalette.GetFromPalette(house.blocks[i][k][j]);
                                break;
                            case Orientation.West:
                                map[y + i][z - j][x + k] = forestPalette.GetFromPalette(house.blocks[i][k][j]);
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
            HouseSchematic house = LoadHouse(path);
            bool result = false;
            switch (pivot)
            {  
                case HousePivot.BottomLeft:
                    result = BuildBottomLeftHouse(map, y, z, x, house, orientation);
                    break;
                case HousePivot.Center:
                    result = BuildCenteredHouse(map, y, z, x, house, orientation);
                    break;
            }
            return result;
        }
    }
}