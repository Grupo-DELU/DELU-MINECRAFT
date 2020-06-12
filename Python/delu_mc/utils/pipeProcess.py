import subprocess
import uuid
import time

# Warning, it must have been setup earlier
import configUtils as configUtils

import pipeHandler as pipeHandler


class PipeProcess(object):
    '''Class For Handling Pipe Server and Process Creation'''

    def __init__(self):
        self._process = None
        self._pipeServer = None
        self._servername = ""
    
    def open(self):
        '''Open a new Pipe Process'''
        self._servername = str(uuid.uuid4()).replace('-', '_') + "_delu_mc"
        self._pipeServer = pipeHandler.PipeServer(self._servername)

        self._pipeServer.startServer()

        print("Opening Process: '{}' '{}'".format(configUtils.EXECUTABLE_PATH, self._servername))
        self._process = subprocess.Popen([configUtils.EXECUTABLE_PATH, self._servername])

        self._pipeServer.connectClient()
        print("Pipe Connected")

    def close(self):
        '''Close the Pipe Process'''
        if self._pipeServer is not None:
            try:
                self._pipeServer.closePipe()
                self._pipeServer = None
            except Exception as e:
                print("Failed to Close Pipe with: " + str(e))

        if self._process is not None:
            if self._process.poll() is None:
                print("Waiting Process 5 seconds")
                time.sleep(5)
            if self._process.poll() is None:
                print("Terminating Process")
                self._process.terminate()
                time.sleep(0.1)
                if self._process.poll() is None:
                    print("Killing Process")
                    self._process.kill()
                print("Process returned: " + str(self._process.wait()))
                self._process = None
            else:
                print("Process returned: " + str(self._process.wait()))
                self._process = None
            
    def __del__(self):
        self.close()

    def getPipeServer(self):
        '''Get Current Pipe Server'''
        return self._pipeServer

    def getPipeServerName(self):
        '''Get Current Pipe Server Name'''
        return self._servername

        
