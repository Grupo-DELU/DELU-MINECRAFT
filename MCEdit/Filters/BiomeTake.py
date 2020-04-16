
'''
Debug filter to generate array with each block biomes in a box
'''

displayName = "BiomeBoxCalculation"

inputs = ()
'''
Important: root_tag & TAG
https://minecraft.gamepedia.com/NBT_format
https://minecraft.gamepedia.com/Chunk_format
'''

def perform(level, box, options):
    minx = box.minx // 16 * 16
    minz = box.minz // 16 * 16
    #Bounding Box MAX tiene 1+ siempre
    boxBiomes = [[-1 for i in range(0, abs(box.minz - box.maxz))] for j in range(0, abs(box.minx - box.maxx))]
    for z in xrange(minz, box.maxz, 16):
        for x in xrange(minx, box.maxx, 16):
            chunkx = x // 16
            chunkz = z // 16
            chunk = level.getChunk(chunkx, chunkz)
            # Get blocks from the chunk that are in the box
            intersectionBox, slices = chunk.getChunkSlicesForBox(box)

            '''
            print("({},{})".format(x,z))
            print("chunk: ({},{})".format(chunkx, chunkz))
            print("minx: {} maxx: {} minz: {} maxz: {}".format(
                intersectionBox.minx, 
                intersectionBox.maxx - 1,
                intersectionBox.minz, 
                intersectionBox.maxz - 1))
            '''
            
            chunk_root_tag = chunk.root_tag
            # For Java Edition
            if (chunk_root_tag and "Level" in chunk_root_tag.keys() and "Biomes" in chunk_root_tag["Level"].keys()):
                '''
                Goes from Z to X at https://minecraft.gamepedia.com/Chunk_format
                but MCEDIT has it as Biome[x,z]
                '''
                biomeArray = chunk_root_tag["Level"]["Biomes"].value

                #X Iteration
                for i in range(0, 16):
                    #Z Iteration
                    for j in range(0, 16):
                        worldZ = chunkz * 16 + j
                        worldX = chunkx * 16 + i

                        if ((worldX, box.miny, worldZ) in box):
                            '''
                            worldX - box.minx local position from box origin
                            worldZ - box.minz local position from box origin
                            '''
                            boxBiomes[worldX - box.minx][worldZ - box.minz] = biomeArray[i * 16 + j]
    print(boxBiomes)