"""
How to use: Put on MC Edit top folder, run with python 2.7, copy Blocks.cs to the Materials folder in the C# project
"""
from pymclevel import *

def printMaterialSet(mats, name):

	msg = "\n\n\t\t/// <summary>"
	msg += "\n\t\t/// %s Set of Materials" % name
	msg += "\n\t\t/// </summary>"
	msg += "\n\t\tpublic static class %s\n\t\t{" % name.replace(" ", "")
	
	sortedMats =  sorted(mats.allBlocks)
	maxId = 0
	materialsIntance = ""
	for b in sortedMats:
		materialsIntance += "\n\t\t\t\t\tnew Material({0}, {1}, \"{2}\"),".format(
			b.ID, b.blockData,
			b.name
		)
		maxId = max(maxId, b.ID)

	msg += "\n\t\t\t/// <summary>"
	msg += "\n\t\t\t/// Set of Materials"
	msg += "\n\t\t\t/// </summary>"
	msg += """\n\t\t\tpublic static readonly MaterialSet Set = new MaterialSet(\n\t\t\t\tnew Material[] {%s\n\t\t\t\t}, \n\t\t\t\t%d\n\t\t\t);""" % (materialsIntance, maxId)

	prevID = -1

	for b in sortedMats:
		msg += "\n\t\t\t/// <summary>"
		msg += "\n\t\t\t/// %s Block" % b.name.replace("&", "&amp;")
		msg += "\n\t\t\t/// </summary>"
		msg += "\n\t\t\tpublic static readonly Material {0}_{1}_{2} = Set.GetMaterial({1}, {2});".format(
			b.name
			.replace(" ", "").replace("(", "_").replace(")", "")
			.replace(",", "_").replace("/", "_").replace("&", "_")
			.replace("-", "_").replace("'", "_").replace("!", "_"),
			b.ID, b.blockData,
		)
		if prevID != b.ID:
			msg += "\n"
			prevID = b.ID
	msg += "\n\t\t}"
	return msg
	

if __name__ == '__main__':
	print "Exporting to Blocks.cs"
	with open("Blocks.cs", "w+") as f:
		f.write("namespace DeluMc.MCEdit\n{")
		f.write("\n\tnamespace Block\n\t{")
		f.write(printMaterialSet(indevMaterials, "Indev Materials"))
		f.write(printMaterialSet(pocketMaterials, "Pocket Materials"))
		f.write(printMaterialSet(alphaMaterials, "Alpha Materials"))
		f.write(printMaterialSet(classicMaterials, "Classic Materials"))
		f.write("\n\t}")
		f.write("\n}")
		