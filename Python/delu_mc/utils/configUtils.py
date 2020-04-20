import os
import sys
import platform


CONFIG_SETUP = False

def isWindows():
    '''If the OS is Windows'''
    return sys.platform.startswith('win')


def isWindows64():
    '''If the OS is Windows 64 bit'''
    return isWindows() and platform.machine().endswith('64')


def isWindows32():
    '''If the OS is Windows 32 bit'''
    return isWindows() and not isWindows64()


def isLinux():
    '''If the OS is Linux'''
    return 'linux' in sys.platform

def isLinux64():
    '''If the OS is Linux 64 bit'''
    return isLinux() and platform.machine().endswith('64')


def isLinux32():
    '''If the OS is Linux 32 bit'''
    return isLinux() and not isLinux64()


DELU_MC_FOLDER_NAME = "delu_mc"
BIN_FOLDER_NAME = "bin"
EXECUTABLE_NAME = "DeluMc"
OS_NAME = ""

if isWindows():
    EXECUTABLE_NAME += ".exe"
    if isWindows64():
        OS_NAME = "win64"
    else:
        OS_NAME = "win32"
        # TODO: Compile 32 bits
        raise RuntimeError("Unsupported operating system: {}".format(sys.platform))
elif isLinux():
    if isLinux64():
        OS_NAME = "linux-x64"
    else:
        OS_NAME = "linux-x86"
        # TODO: Compile 32 bits
        raise RuntimeError("Unsupported operating system: {}".format(sys.platform))
else:
    raise RuntimeError("Unsupported operating system: {}".format(sys.platform))


def configurePaths(module_path):
    '''Configure Variable Paths for other Delu MC Modules'''
    global CONFIG_SETUP
    if CONFIG_SETUP:
        print ("Config Variables Already Setup")
        return
    CONFIG_SETUP = True
    global MODULE_PATH, MODULE_FOLDER, DELU_MC_FOLDER, BIN_FOLDER, BIN_OS_FOLDER, EXECUTABLE_PATH
    MODULE_PATH = os.path.realpath(module_path)
    MODULE_FOLDER = os.path.dirname(MODULE_PATH)
    DELU_MC_FOLDER = os.path.join(MODULE_FOLDER, DELU_MC_FOLDER_NAME)
    BIN_FOLDER = os.path.join(DELU_MC_FOLDER, BIN_FOLDER_NAME)
    BIN_OS_FOLDER = os.path.join(BIN_FOLDER, OS_NAME)
    EXECUTABLE_PATH = os.path.join(BIN_OS_FOLDER, EXECUTABLE_NAME)
    print("Config Variables Setup Complete")
