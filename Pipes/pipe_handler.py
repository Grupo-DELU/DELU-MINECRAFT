import sys
import struct
import io


class MemoryStreamWriter(object):
    def __init__(self):
        self.buffer = io.BytesIO()

    def __del__(self):
        if not self.buffer.closed:
            self.buffer.close()

    def getRawBuffer(self):
        return self.buffer.getvalue()

    def writeBytes(self, bytesArr):
        self.buffer.write(bytesArr)

    def writeFormat(self, dataFormat, data):
        '''From https://docs.python.org/3/library/struct.html#byte-order-size-and-alignment'''
        encoded_data = struct.pack(dataFormat, data)
        self.writeBytes(encoded_data)

    def writeBool(self, boolean):
        self.writeFormat('=?', boolean)

    def writeByte(self, byteToWrite):
        self.writeFormat('=b', byteToWrite)

    def writeUbyte(self, byteToWrite):
        self.writeFormat('=B', byteToWrite)

    def writeInt16(self, int16ToWrite):
        self.writeFormat('=h', int16ToWrite)

    def writeUint16(self, uint16ToWrite):
        self.writeFormat('=H', uint16ToWrite)

    def writeInt32(self, integer):
        self.writeFormat('=i', integer)

    def writeUint32(self, integer):
        self.writeFormat('=I', integer)

    def writeFloat(self, numberFloat):
        self.writeFormat('=f', numberFloat)

    def writeDouble(self, numberDouble):
        self.writeFormat('=d', numberDouble)

class MemoryStreamReader(object):

    def __init__(self, byterArr):
        self.buffer = io.BytesIO(byterArr)

    def __del__(self):
        if not self.buffer.closed:
            self.buffer.close()

    def readBytes(self, amountOfBytes):
        return self.buffer.read(amountOfBytes)

    def readFormat(self, dataFormat):
        '''From https://docs.python.org/3/library/struct.html#byte-order-size-and-alignment'''
        encoded_data = self.readBytes(struct.calcsize(dataFormat))
        return struct.unpack(dataFormat, encoded_data)[0]

    def readBool(self):
        return self.readFormat('=?')

    def readByte(self):
        return self.readFormat('=b')

    def readUbyte(self):
        return self.readFormat('=B')

    def readInt16(self):
        return self.readFormat('=h')

    def readUint16(self):
        return self.readFormat('=H')

    def readInt32(self):
        return self.readFormat('=i')

    def readUint32(self):
        return self.readFormat('=I')

    def readFloat(self):
        return self.readFormat('=f')

    def readDouble(self):
        return self.readFormat('=d')

class PipeServerBase(object):

    def __init__(self, name):
        self._raw_name = name
        self._name = self.formatName(name)
        self._pipe_handler = None

    def formatName(self, name):
        raise NotImplementedError("PipeServerBase is an Abstract Class")

    def startServer(self):
        raise NotImplementedError("PipeServerBase is an Abstract Class")

    def getRawServerName(self):
        return self._raw_name

    def getServerName(self):
        return self._name

    def connectClient(self):
        raise NotImplementedError("PipeServerBase is an Abstract Class")

    def closePipe(self):
        raise NotImplementedError("PipeServerBase is an Abstract Class")

    def writeBytes(self, bytesArr):
        raise NotImplementedError("PipeServerBase is an Abstract Class")

    def writeMemoryBlock(self, bytesArr):
        self.writeBytes(struct.pack('=i', len(bytesArr)))
        self.writeBytes(bytesArr)

    def readBytes(self, amountOfBytes):
        raise NotImplementedError("PipeServerBase is an Abstract Class")

    def readMemoryBlock(self):
        byteArr = self.readBytes(struct.calcsize('=i'))[1]
        memorySize = struct.unpack('=i', byteArr)[0]
        return MemoryStreamReader(self.readBytes(memorySize)[1])

    def __del__(self):
        raise NotImplementedError("PipeServerBase is an Abstract Class")


# its win32, maybe there is win64 too?
is_windows = sys.platform.startswith('win')

if is_windows:
    import win32pipe
    import win32file
    import pywintypes

    class PipeServer(PipeServerBase):

        def __init__(self, name):
            super(PipeServer, self).__init__(name)

        def formatName(self, name):
            return "\\\\.\\pipe\\{}".format(name)

        def startServer(self):
            default_buffer_size = 1100000
            try:
                self._pipe_handler = win32pipe.CreateNamedPipe(
                    self._name,
                    win32pipe.PIPE_ACCESS_DUPLEX,
                    win32pipe.PIPE_TYPE_MESSAGE | win32pipe.PIPE_READMODE_MESSAGE | win32pipe.PIPE_WAIT,
                    1, default_buffer_size, default_buffer_size,
                    0,
                    None)
            except WindowsError as e:
                if e.winerror != win32pipe.ERROR_PIPE_BUSY:
                    self._pipe_handler = None
                    raise

        def connectClient(self):
            win32pipe.ConnectNamedPipe(self._pipe_handler, None)

        def writeBytes(self, bytesArr):
            win32file.WriteFile(self._pipe_handler, bytesArr)

        def readBytes(self, amountOfBytes):
            return win32file.ReadFile(self._pipe_handler, amountOfBytes)

        def closePipe(self):
            if self._pipe_handler is not None:
                win32file.CloseHandle(self._pipe_handler)
                self._pipe_handler = None

        def __del__(self):
            self.closePipe()


elif 'linux' in sys.platform:
    class PipeServer(object):
        pass
else:
    raise RuntimeError("Unsupported operating system: {}".format(sys.platform))


'''
writer = MemoryStreamWriter()
writer.writeInt32(42)
writer.writeFloat(1.3)
writer.writeBool(False)
writer.writeInt32(42)
reader = MemoryStreamReader(writer.getRawBuffer())
print(reader.readInt32())
print(reader.readFloat())
print(reader.readBool())
print(reader.readInt32())
'''

serverName = "testPipe"
pipeServer = PipeServer(serverName)
pipeServer.startServer()
# Here put creation of other program
print("Raw Pipe Name: " + pipeServer.getRawServerName())
pipeServer.connectClient()
print("Pipe Connected")

import random

writer = MemoryStreamWriter()
arrSize = 10
writer.writeInt32(arrSize)
for _ in xrange(arrSize):
    writer.writeInt32(random.randint(-10,10))

pipeServer.writeMemoryBlock(writer.getRawBuffer())
reader = pipeServer.readMemoryBlock()
readSize = reader.readInt32()
print("Size: " + str(readSize))
for _ in xrange(readSize):
    print(reader.readInt32())

pipeServer.closePipe()