import delu_mc.utils.configUtils as configUtils

# IMPORTANT: This must be done before any other import that requires config
configUtils.configurePaths(__file__)

import delu_mc.utils.pipeHandler as pipeHandler
import delu_mc.utils.pipeProcess as pipeProcessHandler


def perform(level, box, options):
    pipeProcess = pipeProcessHandler.PipeProcess()
    pipeProcess.open()
    pipeServer = pipeProcess.getPipeServer()
    import random

    writer = pipeHandler.MemoryStreamWriter()
    arrSize = 10
    writer.writeInt32(arrSize)
    for _ in xrange(arrSize):
        writer.writeInt32(random.randint(-10, 10))

    pipeServer.writeMemoryBlock(writer.getRawBuffer())
    reader = pipeServer.readMemoryBlock()
    readSize = reader.readInt32()
    print("Size: " + str(readSize))
    for _ in xrange(readSize):
        print(reader.readInt32())

    pipeProcess.close()
