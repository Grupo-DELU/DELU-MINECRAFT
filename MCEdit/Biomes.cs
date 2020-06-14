using DeluMc.MCEdit.Block;
using DeluMc.Buildings.Palettes;
using static DeluMc.Buildings.Palettes.PremadePalettes;

namespace DeluMc.MCEdit.Biomes
{
    public enum Biomes {
    	Ocean = 0,
    	Plains = 1,
    	Desert = 2,
    	ExtremeHills = 3,
    	Forest = 4,
    	Taiga = 5,
    	Swamppland = 6,
    	River = 7,
    	Hell_Nether = 8,
    	Sky_End = 9,
    	FrozenOcean = 10,
    	FrozenRiver = 11,
    	IcePlains = 12,
    	IceMountains = 13,
    	MushroomIsland = 14,
    	MushroomIslandShore = 15,
    	Beach = 16,
    	DesertHills = 17,
    	ForestHills = 18,
    	TaigaHills = 19,
    	ExtremeHillsEdge = 20,
    	Jungle = 21,
    	JungleHills = 22,
    	JungleEdge = 23,
    	DeepOcean = 24,
    	StoneBeach = 25,
    	ColdBeach = 26,
    	BirchForest = 27,
    	BirchForestHills = 28,
    	RoofedForest = 29,
    	ColdTaiga = 30,
    	ColdTaigaHills = 31,
    	MegaTaiga = 32,
    	MegaTaigaHills = 33,
    	ExtremeHillsPlus = 34,
    	Savanna = 35,
    	SavannaPlateau = 36,
    	Messa = 37,
    	MessaPlateauF = 38,
    	MessaPlateau = 39,
    	SunflowerPlains = 129,
    	DesertM = 130,
    	ExtremeHillsM = 131,
    	FlowerForest = 132,
    	TaigaM = 133,
    	SwamplandM = 134,
    	IcePlainsSpikes = 140,
    	IceMountainsSpikes = 141,
    	JungleM = 149,
    	JungleEdgeM = 151,
    	BirchForestM = 155,
    	BirchForestHillsM = 156,
    	RoofedForestM = 157,
    	ColdTaigaM = 158,
    	MegaSpruceTaiga = 160,
    	MegaSpruceTaiga2 = 161,
    	ExtremeHillsPlusM = 162,
    	SavannaM = 163,
    	SavannaPlateauM = 164,
    	Mesa_Bryce = 165,
    	MesaPlateauFM = 166,
    	MesaPlateauM = 167,
    }

	public static class BiomeUtils
	{
		/// <summary>
		/// Returns a block to place as the biome floor block.
		/// Used for terraformation
		/// </summary>
		/// <param name="biome">Biome to get block from</param>
		/// <returns>A block to place for floor terraformation</returns>
		public static Material GetBiomeFloorBlock(Biomes biome)
		{
			switch (biome)
			{
				// Desert
				case Biomes.Desert:
				case Biomes.DesertHills:
				case Biomes.DesertM:
				case Biomes.Beach:
					return AlphaMaterials.Sand_12_0;

				// Messa
				case Biomes.Mesa_Bryce:
				case Biomes.MesaPlateauFM:
				case Biomes.MesaPlateauM:
				case Biomes.Messa:
					return AlphaMaterials.RedSand_12_1;

				case Biomes.MessaPlateau:
				case Biomes.MessaPlateauF:
					return AlphaMaterials.HardenedClay_172_0;
				
				default:
					return AlphaMaterials.GrassBlock_2_0;
			}
		}


		/// <summary>
		/// Returns a building palette taking account the biome and
		/// buildtype of the house.
		/// </summary>
		/// <param name="biomes">Build biome</param>
		/// <param name="build">Build type</param>
		/// <returns></returns>
		public static BuildingPalette GetBiomeBuildPalette(Biomes biomes, Buildings.BuildType build)
		{
			switch (biomes)
			{
				case Biomes.Desert:
				case Biomes.DesertHills:
				case Biomes.DesertM:
					switch (build)
					{
						case Buildings.BuildType.House:
							return desertPalette;
						case Buildings.BuildType.Farm:
							return farmDesertPalette;
						case Buildings.BuildType.Plaza:
							return plazaDesertPalette;
						default:
							return null;
					}

				// Messa
				//case Biomes.Mesa_Bryce:
				//case Biomes.MesaPlateauFM:
				//case Biomes.MesaPlateauM:
				//case Biomes.Messa:
				//case Biomes.MessaPlateau:
				//case Biomes.MessaPlateauF:
				default:
					switch (build)
					{
						case Buildings.BuildType.House:
							return forestPalette;
						case Buildings.BuildType.Farm:
							return farmForestPalette;
						case Buildings.BuildType.Plaza:
							return plazaForestPalette;
						default:
							return null;
					}
			}
		}


		/// <summary>
		/// Returns a block to build the roads with depending on the biome
		/// the road is on.
		/// </summary>
		/// <param name="biome"></param>
		/// <returns></returns>
		public static Material GetBiomeRoadBlock(Biomes biome)
		{
			switch (biome)
			{
				// Desert
				case Biomes.Desert:
				case Biomes.DesertHills:
				case Biomes.DesertM:
				case Biomes.Beach:
					return AlphaMaterials.Granite_1_1;
					
				// Messa
				case Biomes.Mesa_Bryce:
				case Biomes.MesaPlateauFM:
				case Biomes.MesaPlateauM:
				case Biomes.Messa:
				case Biomes.MessaPlateau:
				case Biomes.MessaPlateauF:
					return AlphaMaterials.Bricks_45_0;
				
				default:
					return AlphaMaterials.Path_208_0;
			}
		}
	}
}
