"""
Schematics char meaning
    -'w' -> Walls -> Bedrock
    -'f' -> Floor -> Sponge
    -'v' -> Windows -> Beacon
    -'d' -> Door -> not defined yet
    -'c' -> Columns -> Netherack
    -'r' -> Roof -> Quartz BLOCK (NOT ORE) 155
    -'o' -> Air -> Air
"""

import json;

displayName = "Create Schematic"
inputs = (
    ("Identifier", ("wall", "corner", "house")),
    ("ID", 1)
    )

def perform(level, box, options):
    output = {
        "blocks" : [[['o' for i in range(0, box.size[0])] for j in range(0, box.size[2])] 
            for k in range(0, box.size[1])]}
    
    for y in xrange(box.miny, box.maxy):
        for z in xrange(box.minz, box.maxz):
            for x in xrange(box.minx, box.maxx):
                localX = x - box.minx
                localY = y - box.miny
                localZ = z - box.minz
                (output["blocks"])[localY][localZ][localX] = blockToFormat(level.blockAt(x,y,z))
    
    file = open("%s.json"%((options["Identifier"] + "_%s"%(options["ID"]))), 'w')
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
    elif (id == 138):
        return 'v'
    elif (id == 87):
        return 'c'
    elif (id == 155):
        return 'r'
    elif (id == 0):
        return 'o'
    else:
        raise Exception("Block identifier not supported for the schematic")