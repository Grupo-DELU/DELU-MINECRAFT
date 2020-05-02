using System;
using System.Collections.Generic;
using DeluMc.MCEdit;
using DeluMc.MCEdit.Block;

namespace DeluMc.Schematics.Palettes
{
    /*  Schematics char meaning
     *      -'w' -> Walls
     *      -'f' -> Floor
     *      -'v' -> Windows
     *      -'d' -> Doors
     *      -'c' -> Columns
     *      -'r' -> Roof
     *      -'o' -> Air
     */     
    
    /// <summary>
    /// Class that contains a dictionary that maps char to blocks (Material).
    /// </summary>
    public class BuildingPalette
    {
        private readonly Dictionary<char, Material> palette = new Dictionary<char, Material>();
        
        /// <summary>
        /// Returns the block that blockType is mapped to.
        /// </summary>
        /// <param name="blockType">Schematic block type</param>
        /// <returns>block that blocktype is mapped to</returns>
        public Material GetFromPalette(char blockType)
        {
            if (!(palette.ContainsKey(blockType)))
            {
                throw new KeyNotFoundException("blockType \' " + blockType + "\' not legal in building" +
                " palette format");
            }
            else if (palette[blockType] == null)
            {
                throw new Exception("\'" + blockType + "\' not found in BuildingPalette used.");
            }

            return palette[blockType];
        }

        public BuildingPalette(
            Material wall = null, 
            Material floor = null, 
            Material windows = null,
            Material columns = null,
            Material roof = null)
        {
            palette.Add('w', wall);
            palette.Add('f', floor);
            palette.Add('v', windows);
            palette.Add('c', columns);
            palette.Add('r', roof);
            palette.Add('o', AlphaMaterials.Air_0_0);
        }
    }

    /// <summary>
    /// Multiple predefined palettess
    /// </summary>
    public static class Palettes
    {
        public static readonly BuildingPalette forestPalette = new BuildingPalette(
            AlphaMaterials.DarkOakWoodPlanks_5_5,
            AlphaMaterials.RedWool_35_14,
            AlphaMaterials.AcaciaFence_192_0,
            AlphaMaterials.OakWood_BarkOnly_17_12,
            AlphaMaterials.Cobblestone_4_0);

        public static readonly BuildingPalette desertPalette = new BuildingPalette(
            AlphaMaterials.SmoothSandstone_24_2,
            AlphaMaterials.Cobblestone_4_0,
            AlphaMaterials.GlassPane_102_0,
            AlphaMaterials.ChiseledSandstone_24_1,
            AlphaMaterials.Sandstone_24_0);
    }
}