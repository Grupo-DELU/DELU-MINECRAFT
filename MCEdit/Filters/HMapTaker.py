
'''
Debug filter to generate array with the highest altitude that contains a movement blocker
or water block.
'''

displayName = "HeightMapTaker"

inputs = ()
'''
Important: root_tag & TAG
https://minecraft.gamepedia.com/NBT_format
https://minecraft.gamepedia.com/Chunk_format

Ignore Chunk_format HeightMaps, that for Minecaft versions greater equal than v1.13
and MCEdit works with v1.12. The HM counts water as a solid block for the HM but not
the lava. Must test if grass or flowers also count as solid blocks!

The HeightMap array from the NBT is provided with a ZX swizzle
'''

def perform(level, box, inputs):
    minx = box.minx // 16 * 16
    minz = box.minz // 16 * 16

    #Bounding Box MAX tiene 1+ siempre
    boxHeightMap = [[-1 for i in range(0, abs(box.minx - box.maxx))] for j in range(0, abs(box.minz - box.maxz))]
    
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
            Extracts from the chunk Level NBT file the chunk heightmap array
            '''
            if (chunk_root_tag and "Level" in chunk_root_tag.keys() and 
                "HeightMap" in chunk_root_tag["Level"].keys()):

                hmArray = chunk_root_tag["Level"]["HeightMap"].value

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
                            '''
                            The Heightmap detects the first empty block above a solid block,
                            that's why we substract 1 from the heightmap value.

                            Also, if the highest block is water, you can not build in it! So 
                            the heightmap is set as -1
                            '''
                            y = hmArray[i * 16 + j] - 1
                            
                            boxHeightMap[localZ][localX] = hmArray[i * 16 + j] - 1 - box.miny
                            block = level.blockAt(worldX,y,worldZ)

                            if (block == 8 or block == 9):
                                boxHeightMap[localZ][localX] = -1
                                
    return boxHeightMap