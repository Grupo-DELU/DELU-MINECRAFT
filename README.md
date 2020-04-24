# DELU-MINECRAFT

## Installation

### MCEdit

MCEdit requires Python2.7, also, it is recommended to use [MiniConda](https://docs.conda.io/en/latest/miniconda.html) as it already includes some packages that MCEdit depends on.

We recommend the [MCEdit](https://github.com/mcgreentn/GDMC/wiki/The-GDMC-Framework) version used by GDMC, but it should also work with [MCEdit-Unified](https://www.mcedit-unified.net/). To install and run it, the following Python packages are required:

* PyOpenGL

* numpy

* pygame 1.9.4

* pyyaml

* pillow

* ftputil

* pypiwin32 or pywin32 (Windows)

Once running, you should do ```python setup.py build_ext --inplace``` inside the MCEdit folder to compile some parts of it for better performance. **Note** on Windows you may be required to download some C++ extensions, the console will guide the process.

Once the process is done, you should be able to generate a new map.

### .NET Core

It is required NET. Core to compile the project. To download it go [here](https://dotnet.microsoft.com/download) to get the .NET Core SDK.

Once installed, you should be able to get all the dependencies using ```dotnet restore``` in the main project folder. To compile a release use ```dotnet build --configuration Release```.

### VS Code

We recommend using [VS Code](https://code.visualstudio.com/download) with its C# and Python extensions.

## MCEdit Filters

To install the filter you need to copy the contents of the project Python folder to MCEdit's external filters folder. On Windows it is normally located in ```...\Documents\MCEdit\Filters```. For Linux it is usually located in ```/home/user_name/.mcedit/MCEdit/Filters/```

## Links of interest
* [MCEdit-Unified Repository](https://github.com/Podshot/MCEdit-Unified)

* [GDMC Website](http://gendesignmc.engineering.nyu.edu)
