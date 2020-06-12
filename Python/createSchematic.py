"""
Schematics char meaning
    -'w' -> block1 -> Bedrock -> 7
    -'f' -> block2 -> Sponge -> 19
    -'v' -> block3 -> Glass -> 20
    -'c' -> block4 -> Netherack -> 87
    -'r' -> block5 -> Quartz BLOCK (NOT ORE) -> 155
    -'e' -> Road -> Bricks -> 45 (NOT SLAB)
    -'d' -> Door -> CraftingTable -> 58
    -'o' -> Air -> Air -> 0
    -'n' -> Don't replace -> Anything that isn't above

Converts the box blocks into the schematic char formats and
serializes the data of the structure into a .json file to
load it in the C# main application.

IMPORTANT NOTE: Important: Road block must be at the bottom of the schematic box or else, it could be
marked as home (applies when doing schematics).
"""

import json;
import os;

formatDict = {
    7:'w',
    19:'f',
    20:'v',
    87:'c',
    155:'r',
    45:'e',
    58:'d',
    0:'o',}

buildDict = {
    "House": 0,
    "Farm": 1,
    "Plaza": 2}

displayName = "Create Schematic"
inputs = (
    ("Build type", ("House", "Farm", "Plaza")),
    )

def perform(level, box, options):
    # Size YZX
    # Creates .json dictionary structure
    output = {
        "blocks" : [[['o' for i in range(0, box.size[0])] for j in range(0, box.size[2])] 
            for k in range(0, box.size[1])],
        "size" : [box.size[1], box.size[2], box.size[0]],
        "buildType" : buildDict[options["Build type"]],
        "roadStartZ" : 0,
        "roadStartX" : 0,
        }
    
    for y in xrange(box.miny, box.maxy):
        for z in xrange(box.minz, box.maxz):
            for x in xrange(box.minx, box.maxx):
                localX = x - box.minx
                localY = y - box.miny
                localZ = z - box.minz
                if (level.blockAt(x,y,z) == 45):
                    output["roadStartZ"] = localZ
                    output["roadStartX"] = localX
                (output["blocks"])[localY][localZ][localX] = blockToFormat(level.blockAt(x,y,z))
    
    if not(os.path.exists("schematics")):
        os.mkdir("schematics")

    name = raw_input("Insert name: ")
    file = open(os.path.join("schematics", "%s.json"%(name)), 'w')
    json.dump(output, file, indent = 4)
    file.close()
    
"""
Converts block ID to char identifier associated.
Conversion table above.
"""
def blockToFormat(id):
    if (id in formatDict):
        return formatDict[id]
    return 'n'