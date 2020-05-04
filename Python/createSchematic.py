"""
Schematics char meaning
    -'w' -> Walls -> Bedrock -> 7
    -'f' -> Floor -> Sponge -> 19
    -'v' -> Windows -> Glass -> 20
    -'d' -> Door -> not defined yet
    -'c' -> Columns -> Netherack -> 87
    -'r' -> Roof -> Quartz BLOCK (NOT ORE) -> 155
    -'e' -> Road -> Bricks -> 45 (NOT SLAB)
    -'o' -> Air -> Anything that isn't above
"""

import json;

displayName = "Create Schematic"
inputs = ()

def perform(level, box, options):
    # Size YZX
    output = {
        "blocks" : [[['o' for i in range(0, box.size[0])] for j in range(0, box.size[2])] 
            for k in range(0, box.size[1])],
        "size" : [box.size[1], box.size[2], box.size[0]]    
        }
    
    for y in xrange(box.miny, box.maxy):
        for z in xrange(box.minz, box.maxz):
            for x in xrange(box.minx, box.maxx):
                localX = x - box.minx
                localY = y - box.miny
                localZ = z - box.minz
                (output["blocks"])[localY][localZ][localX] = blockToFormat(level.blockAt(x,y,z))
    
    # TODO: Folder for schematics in MCEdit-unified
    name = raw_input("Insert name: ")
    file = open("%s.json"%(name), 'w')
    json.dump(output, file, indent = 4)
    file.close()
    
"""
Converts block ID to char identifier associated.
Conversion table above.
"""
def blockToFormat(id):
    if (id == 7):
        return 'w'
    elif (id == 19):
        return 'f'
    elif (id == 20):
        return 'v'
    elif (id == 87):
        return 'c'
    elif (id == 155):
        return 'r'
    elif (id == 45):
        return 'e'
    else:   # air or anything else
        return 'o'