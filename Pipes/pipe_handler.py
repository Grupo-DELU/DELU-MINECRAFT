import sys
import struct

# its win32, maybe there is win64 too?
is_windows = sys.platform.startswith('win')

if is_windows:
    import win32pipe
    import win32file
    import pywintypes
    class PipeServer(object):

        def __init__(self, name):
            self._name = "\\\\.\\pipe\\{}".format(name)
            self._pipe_handler = None
            try:
                self._pipe_handler = win32pipe.CreateNamedPipe(
                    self._name,
                    win32pipe.PIPE_ACCESS_DUPLEX,
                    win32pipe.PIPE_TYPE_MESSAGE | win32pipe.PIPE_READMODE_MESSAGE | win32pipe.PIPE_WAIT,
                    1, 65536, 65536,
                    0,
                    None)
            except WindowsError as e:
                if e.winerror != win32pipe.ERROR_PIPE_BUSY:
                    self._pipe_handler = None
                    raise

        def getServerName(self):
            return self._name

        def connectClient(self):
            win32pipe.ConnectNamedPipe(self._pipe_handler, None)

        def writeBytes(self, bytesArr):
            win32file.WriteFile(self._pipe_handler, bytesArr)

        def readBytes(self, amountOfBytes):
            return win32file.ReadFile(self._pipe_handler, amountOfBytes)

        '''From https://docs.python.org/3/library/struct.html#byte-order-size-and-alignment'''

        def writeFormat(self, dataFormat, data):
            encoded_data = struct.pack(dataFormat, data)
            self.writeBytes(encoded_data)

        '''From https://docs.python.org/3/library/struct.html#byte-order-size-and-alignment'''

        def readFormat(self, dataFormat):
            encoded_data = self.readBytes(struct.calcsize(dataFormat))
            return struct.unpack(dataFormat, encoded_data)

        def writeInt32(self, integer):
            self.writeFormat('=i', integer)

        def readInt32(self):
            return self.readFormat('=i')

        def writeUint32(self, integer):
            self.writeFormat('=I', integer)

        def readUint32(self):
            return self.readFormat('=I')

        def __del__(self):
            if self._pipe_handler is not None:
                win32file.CloseHandle(self._pipe_handler)


elif 'linux' in sys.platform:
    class PipeServer(object):
        pass
else:
    raise RuntimeError("Unsupported operating system: {}".format(sys.platform))
