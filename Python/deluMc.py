import delu_mc.utils.configUtils as configUtils

# IMPORTANT: This must be done before any other import that requires config
configUtils.configurePaths(__file__)

import delu_mc.utils.pipeHandler as pipeHandler
import delu_mc.utils.pipeProcess as pipeProcessHandler


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
