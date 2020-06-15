using System;
using System.Collections.Generic;
using DeluMc.MCEdit;
using DeluMc.MCEdit.Block;

namespace DeluMc.Buildings.Palettes
{
    /*
    Schematics char meaning
        -'a' -> block1 -> Bedrock -> 7
        -'b' -> block2 -> Sponge -> 19
        -'c' -> block3 -> Glass -> 20
        -'d' -> block4 -> Netherack -> 87
        -'e' -> block5 -> Quartz BLOCK (NOT ORE) -> 155
        -'f' -> Road -> Bricks -> 45 (NOT SLAB)
        -'g' -> Door -> CraftingTable -> 58
        -'h' -> Air -> Air -> 0
        -'i' -> Don't replace -> Anything that isn't above
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
            AlphaMaterials.OakFence_85_0,
            AlphaMaterials.OakWood_Upright_17_0,
            AlphaMaterials.OakDoor_Lower_Unopened_East_64_0,
            AlphaMaterials.Cobblestone_4_0,
            AlphaMaterials.Podzol_3_2);

        public static readonly BuildingPalette desertPalette = new BuildingPalette(
            AlphaMaterials.SmoothSandstone_24_2,
            AlphaMaterials.Cobblestone_4_0,
            AlphaMaterials.GlassPane_102_0,
            AlphaMaterials.ChiseledSandstone_24_1,
            AlphaMaterials.BirchDoor_Lower_Unopened_East_194_0,
            AlphaMaterials.Sandstone_24_0,
            AlphaMaterials.Gravel_13_0);

        public static readonly BuildingPalette savannaPalette = new BuildingPalette(
            AlphaMaterials.AcaciaWoodPlanks_5_4,
            AlphaMaterials.OrangeWool_35_1,
            AlphaMaterials.AcaciaFence_192_0,
            AlphaMaterials.AcaciaWood_Upright_Acacia_162_0,
            AlphaMaterials.AcaciaDoor_Lower_Opened_East_196_4,
            AlphaMaterials.Cobblestone_4_0,
            AlphaMaterials.Podzol_3_2);
        
        public static readonly BuildingPalette plazaForestPalette = new BuildingPalette(
            AlphaMaterials.Cobblestone_4_0,
            AlphaMaterials.Water_Still_Level0_9_7,
            AlphaMaterials.OakFence_85_0,
            null,
            null, 
            null, 
            AlphaMaterials.Podzol_3_2
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
            AlphaMaterials.OakWood_Upright_17_0,
            AlphaMaterials.Farmland_Wet_Moisture7_60_7,
            AlphaMaterials.Water_Still_Level0_9_7,
            AlphaMaterials.Wheat_Age7_Max_59_7,
            null, 
            null, 
            AlphaMaterials.Podzol_3_2
        );

        public static readonly BuildingPalette farmSavannaPalette = new BuildingPalette(
            AlphaMaterials.AcaciaWood_Upright_Acacia_162_0,
            AlphaMaterials.Farmland_Wet_Moisture7_60_7,
            AlphaMaterials.Water_Still_Level0_9_7,
            AlphaMaterials.Carrots_Age7_141_7,
            null, 
            null, 
            AlphaMaterials.Podzol_3_2
        );

        public static readonly BuildingPalette farmDesertPalette = new BuildingPalette(
            AlphaMaterials.Sandstone_24_0,
            AlphaMaterials.Farmland_Dry_Moisture4_60_4,
            AlphaMaterials.Water_Still_Level0_9_7,
            AlphaMaterials.Potatoes_Age7_142_7,
            null, 
            null, 
            AlphaMaterials.Gravel_13_0
        );
    }
}