
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

Ignore Chunk_format HeightMaps, that for Minecaft versions greater equal than 1.13v
and MCEdit works with 1.12. The HM counts water as a solid block for the HM but not
the lava. Must test if grass or flowers also count as solid blocks!
'''

def perform(level, box, options):
    minx = box.minx // 16 * 16
    minz = box.minz // 16 * 16
    #Bounding Box MAX tiene 1+ siempre
    boxHeightMap = [[-1 for i in range(0, abs(box.minz - box.maxz))] for j in range(0, abs(box.minx - box.maxx))]
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
            if (chunk_root_tag and "Level" in chunk_root_tag.keys() and 
                "HeightMap" in chunk_root_tag["Level"].keys()):

                hmArray = chunk_root_tag["Level"]["HeightMap"].value
                #X Iteration
                for i in range(0, 16):
                #    #Z Iteration
                    for j in range(0, 16):
                        worldZ = chunkz * 16 + j
                        worldX = chunkx * 16 + i
                        if ((worldX, box.miny, worldZ) in box):
                            '''
                            worldX - box.minx local position from box origin
                            worldZ - box.minz local position from box origin
                            '''
                            boxHeightMap[worldX - box.minx][worldZ - box.minz] = hmArray[i * 16 + j]
    print(boxHeightMap)