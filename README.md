# DELU-MINECRAFT

## Installation

### MCEdit

We require Python 2.7. We recommend [MiniConda](https://docs.conda.io/en/latest/miniconda.html), as it includes some packages already.

We require [MCEdit](https://github.com/mcgreentn/GDMC/wiki/The-GDMC-Framework) used by GMCD. To install it and run it it requires:

* PyOpenGL

* numpy

* pygame

* pyyaml

* pillow

* ftputil

* pypiwin32 (Windows)

Once running, you should do ```python setup.py build_ext --inplace``` inside the MCEdit folder to compile some parts of it for better performance. **Note** on Windows you may be required to download some C++ extensions, the console will guide the process.

Once all of it is done, you should be able to generate a new map

### NetCore

We require NetCore to compile our project. To download it go [here](https://dotnet.microsoft.com/download) to get the .NET Core SDK.

Once installed you should be able to get all the dependencies using ```dotnet restore``` in our main folder. To compile release you need ```dotnet build --configuration Release```.

### VS Code

We recommend to use [VS Code](https://code.visualstudio.com/download) with its C# and Python extensions.

## MCEdit Filters

To install the filter you need to copy the contents of the Python folder to MCEdit's external filters folder. On Windows it is normally located in ```...\Documents\MCEdit\Filters```.
