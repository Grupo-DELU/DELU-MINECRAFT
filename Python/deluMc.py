import delu_mc.utils.pipeProcess as pipeProcessHandler
import delu_mc.utils.pipeHandler as pipeHandler
import delu_mc.utils.configUtils as configUtils

# IMPORTANT: This must be done before any other import that requires config
configUtils.configurePaths(__file__)


def perform(level, box, options):
    pipeProcess = pipeProcessHandler.PipeProcess()
    pipeProcess.open()
    pipeServer = pipeProcess.getPipeServer()

    # box.max is open set
    x_size = box.maxx - box.minx
    y_size = box.maxy - box.miny
    z_size = box.maxz - box.minz

    writer = pipeHandler.MemoryStreamWriter()

    # We are going to access using YZX for better cache access
    writer.writeInt32(y_size)
    writer.writeInt32(z_size)
    writer.writeInt32(x_size)

    for y in xrange(box.miny, box.maxy):
        for z in xrange(box.minz, box.maxz):
            for x in xrange(box.minx, box.maxx):
                block_id = level.blockAt(x, y, z)
                block_data = level.blockDataAt(x, y, z)
                writer.writeInt32(block_id)
                writer.writeInt32(block_data)

    biomes, hm, wm = biomeHMCalculator(level, box)
    for z in xrange(0, z_size):
        for x in xrange(0, x_size):
            b, h, w = biomes[z][x], hm[z][x], wm[z][x]
            writer.writeInt32(int(b))
            writer.writeInt32(int(h))
            writer.writeInt32(int(w))

    # Send Work
    pipeServer.writeMemoryBlock(writer.getRawBuffer())

    # Receive Work
    reader = pipeServer.readMemoryBlock()
    # We assume that we are receiving it the same way we sent it
    for y in xrange(box.miny, box.maxy):
        for z in xrange(box.minz, box.maxz):
            for x in xrange(box.minx, box.maxx):
                block_id = reader.readInt32()
                block_data = reader.readInt32()
                level.setBlockAt(x, y, z, block_id)
                level.setBlockDataAt(x, y, z, block_data)

    pipeProcess.close()


'''
Look Filters/BiomeTaker & Filters/HMapTaker for more info
about the operations. Returns a tuple (biomes, heightmap)
'''


def biomeHMCalculator(level, box):
    minx = box.minx // 16 * 16
    minz = box.minz // 16 * 16
    #Bounding Box MAX tiene 1+ siempre

    boxHeightMap = [[-1 for i in range(0, abs(box.minx - box.maxx))] for j in range(0, abs(box.minz - box.maxz))]
    boxBiomes = [[-1 for i in range(0, abs(box.minx - box.maxx))] for j in range(0, abs(box.minz - box.maxz))]
    waterMap = [[0 for i in range(0, abs(box.minx - box.maxx))] for j in range(0, abs(box.minz - box.maxz))]
    
    for z in xrange(minz, box.maxz, 16):
        for x in xrange(minx, box.maxx, 16):
            chunkx = x // 16
            chunkz = z // 16
            chunk = level.getChunk(chunkx, chunkz)
            # Get blocks from the chunk that are in the box
            intersectionBox, slices = chunk.getChunkSlicesForBox(box)

            chunk_root_tag = chunk.root_tag
            # For Java Edition
            if (chunk_root_tag and "Level" in chunk_root_tag.keys() and 
                "Biomes" in chunk_root_tag["Level"].keys() and 
                "HeightMap" in chunk_root_tag["Level"].keys()):

                hmArray = chunk_root_tag["Level"]["HeightMap"].value
                biomeArray = chunk_root_tag["Level"]["Biomes"].value

                #Z Iteration
                for i in range(0, 16):
                    #X Iteration
                    for j in range(0, 16):
                        worldZ = chunkz * 16 + i
                        worldX = chunkx * 16 + j
                        if ((worldX, box.miny, worldZ) in box):
                            localZ = worldZ - box.minz
                            localX = worldX - box.minx
                            '''
                            worldX - box.minx local position from box origin
                            worldZ - box.minz local position from box origin
                            The Heightmap detects the first empty block above a solid block, that's why we substract 1
                            from the heightmap value.
                            '''
                            y = hmArray[i * 16 + j] - 1
                            boxBiomes[localZ][localX] = biomeArray[i * 16 + j]
                            boxHeightMap[localZ][localX] = hmArray[i * 16 + j] - 1 - box.miny
                            block = level.blockAt(worldX,y,worldZ)
                            # Water ID, replace for alphamaterials...
                            if (block == 8 or block == 9):
                                boxHeightMap[localZ][localX] = -1
                                waterMap[localZ][localX] = 1
                                
    return (boxBiomes, boxHeightMap, waterMap)
