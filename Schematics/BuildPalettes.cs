using System;
using System.Collections.Generic;
using DeluMc.MCEdit;
using DeluMc.MCEdit.Block;

namespace DeluMc.Buildings.Palettes
{
    /*
    Schematics char meaning
        -'w' -> block1 -> Bedrock -> 7
        -'f' -> block2 -> Sponge -> 19
        -'v' -> block3 -> Glass -> 20
        -'c' -> block4 -> Netherack -> 87
        -'r' -> block5 -> Quartz BLOCK (NOT ORE) -> 155
        -'e' -> Road -> Bricks -> 45 (NOT SLAB)
        -'d' -> Door -> CraftingTable -> 58
        -'o' -> Air -> Air -> 0
        -'n' -> Don't replace -> Anything that isn't above
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
            Material block1 = null, 
            Material block2 = null, 
            Material block3 = null,
            Material block4 = null,
            Material door = null,
            Material block5 = null,
            Material road = null)
        {
            palette.Add('a', block1);
            palette.Add('b', block2);
            palette.Add('c', block3);
            palette.Add('d', block4);
            palette.Add('e', block5);
            palette.Add('f', road);
            palette.Add('g', door);
            palette.Add('h', AlphaMaterials.Air_0_0);
        }
    }

    /// <summary>
    /// Multiple predefined palettess
    /// </summary>
    public static class PremadePalettes
    {
        public static readonly BuildingPalette forestPalette = new BuildingPalette(
            AlphaMaterials.DarkOakWoodPlanks_5_5,
            AlphaMaterials.RedWool_35_14,
            AlphaMaterials.AcaciaFence_192_0,
            AlphaMaterials.OakWood_BarkOnly_17_12,
            AlphaMaterials.OakDoor_Lower_Unopened_East_64_0,
            AlphaMaterials.Cobblestone_4_0,
            AlphaMaterials.Gravel_13_0);

        public static readonly BuildingPalette desertPalette = new BuildingPalette(
            AlphaMaterials.SmoothSandstone_24_2,
            AlphaMaterials.Cobblestone_4_0,
            AlphaMaterials.GlassPane_102_0,
            AlphaMaterials.ChiseledSandstone_24_1,
            AlphaMaterials.BirchDoor_Lower_Unopened_East_194_0,
            AlphaMaterials.Sandstone_24_0,
            AlphaMaterials.Gravel_13_0);
        
        public static readonly BuildingPalette plazaForestPalette = new BuildingPalette(
            AlphaMaterials.Cobblestone_4_0,
            AlphaMaterials.Water_Still_Level0_9_7,
            AlphaMaterials.AcaciaFence_192_0,
            null,
            null, 
            null, 
            AlphaMaterials.Gravel_13_0
        );

        public static readonly BuildingPalette plazaDesertPalette = new BuildingPalette(
            AlphaMaterials.SmoothSandstone_24_2,
            AlphaMaterials.Water_Still_Level0_9_7,
            AlphaMaterials.BirchFence_189_0,
            null,
            null, 
            null, 
            AlphaMaterials.Gravel_13_0
        );

        public static readonly BuildingPalette farmForestPalette = new BuildingPalette(
            AlphaMaterials.OakWood_BarkOnly_17_12,
            AlphaMaterials.Farmland_Wet_Moisture7_60_7,
            AlphaMaterials.Water_Still_Level0_9_7,
            AlphaMaterials.Wheat_Age7_Max_59_7,
            null, 
            null, 
            AlphaMaterials.Gravel_13_0
        );

        public static readonly BuildingPalette farmDesertPalette = new BuildingPalette(
            AlphaMaterials.OakWood_BarkOnly_17_12,
            AlphaMaterials.Farmland_Dry_Moisture4_60_4,
            AlphaMaterials.Water_Still_Level0_9_7,
            AlphaMaterials.Wheat_Age7_Max_59_7,
            null, 
            null, 
            AlphaMaterials.Gravel_13_0
        );
    }
}