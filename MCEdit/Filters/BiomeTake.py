
'''
Debug filter to generate array with each block biomes in a box
'''

displayName = "BiomeBoxCalculation"

inputs = ()
'''
Important: root_tag & TAG
https://minecraft.gamepedia.com/NBT_format
https://minecraft.gamepedia.com/Chunk_format

The Biome array from the NBT is provided with a ZX swizzle
'''

def perform(level, box, inputs):
    minx = box.minx // 16 * 16
    minz = box.minz // 16 * 16

    #Bounding Box MAX tiene 1+ siempre
    boxBiomes = [[-1 for i in range(0, abs(box.minx - box.maxx))] for j in range(0, abs(box.minz - box.maxz))]
    
    for z in xrange(minz, box.maxz, 16):
        for x in xrange(minx, box.maxx, 16):
            # Chunk position
            chunkx = x // 16
            chunkz = z // 16
            chunk = level.getChunk(chunkx, chunkz)
            
            # Get blocks from the chunk that are in the box
            intersectionBox, slices = chunk.getChunkSlicesForBox(box)

            chunk_root_tag = chunk.root_tag
            '''
            For Java edition.
            Extracts from the chunk Level NBT file the chunk biome array
            '''
            if (chunk_root_tag and "Level" in chunk_root_tag.keys() and 
                "Biomes" in chunk_root_tag["Level"].keys()):

                biomeArray = chunk_root_tag["Level"]["Biomes"].value

                #Z Iteration
                for i in range(0, 16):
                    #X Iteration
                    for j in range(0, 16):
                        worldZ = chunkz * 16 + i
                        worldX = chunkx * 16 + j
                        if ((worldX, box.miny, worldZ) in box):
                            # ZX Position local to the box
                            localZ = worldZ - box.minz
                            localX = worldX - box.minx
                            boxBiomes[localZ][localX] = biomeArray[i * 16 + j]

    return boxBiomes