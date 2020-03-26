using System.Collections.Generic;

namespace Delu_Mc.MCEdit
{
    /// <summary>
    /// MCEdit Block material
    /// </summary>
    public class Material
    {
        /// <summary>
        /// ID of the material
        /// </summary>
        public int ID { get; private set; }

        /// <summary>
        /// Data of the material
        /// </summary>
        public int Data { get; private set; }

        /// <summary>
        /// Name of the Material
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Invalid Material
        /// </summary>
        public static Material Invalid = new Material(-1, -1, "Invalid");

        /// <summary>
        /// Create new MCEdit Material
        /// </summary>
        /// <param name="id">ID of the Material</param>
        /// <param name="data">Data of the Material, Default is 0</param>
        /// <param name="name">Name of the Material, Default is empty</param>
        public Material(int id, int data = 0, in string name = "")
        {
            this.ID = id;
            this.Data = data;
            this.Name = name;
        }

        /// <summary>
        /// Compare Two Materials by ID only (I.e to check material Families)
        /// </summary>
        /// <param name="other"></param>
        /// <returns>If the Two materials have the same ID</returns>
        bool CompareByID(in Material other)
        {
            return this.ID == other.ID;
        }

        /// <summary>
        /// Compare Two Materials by ID and Data
        /// </summary>
        /// <param name="other"></param>
        /// <returns>If the Two materials have the same ID and Data</returns>
        bool CompareByIDAndData(in Material other)
        {
            return this.ID == other.ID && this.Data == other.Data;
        }

        /// <summary>
        /// String Conversion
        /// </summary>
        /// <returns>String Conversion</returns>
        public override string ToString()
        {
            return $"(Material ID={ID} Data={Data} Name=\"{Name}\")";
        }  
    }

    public class MaterialSet
    {
        /// <summary>
        /// Set of Materials
        /// </summary>
        private Dictionary<int, Dictionary<int, Material>> mSet = null;

        /// <summary>
        /// Create a Material Set
        /// </summary>
        /// <param name="rawSet">Raw Set</param>
        /// <param name="maxID">Maximum ID possible</param>
        public MaterialSet(in Material[] rawSet, int maxID)
        {
            mSet = new Dictionary<int, Dictionary<int, Material>>(maxID + 1);
            for (int i = 0; i < rawSet.Length; i++)
            {
                if (mSet.TryGetValue(rawSet[i].ID, out Dictionary<int, Material> family))
                {
                    family.Add(rawSet[i].Data, rawSet[i]);
                }
                else
                {
                    mSet.Add(
                        rawSet[i].ID,
                        new Dictionary<int, Material>(
                            new KeyValuePair<int, Material>[] { new KeyValuePair<int, Material>(rawSet[i].Data, rawSet[i]) }
                        )
                    );
                }
            }
        }

        /// <summary>
        /// Get A Family of Materials
        /// </summary>
        /// <param name="ID">ID of Family</param>
        /// <returns>Family associated with that ID, if any</returns>
        public Dictionary<int, Material> GetFamily(int ID)
        {
            if (mSet.TryGetValue(ID, out Dictionary<int, Material> family))
            {
                return family;
            }
            return null;
        }

        /// <summary>
        /// Get A Family of Materials
        /// </summary>
        /// <param name="mat">Material to look for its family</param>
        /// <returns>Family associated with that ID, if any</returns>
        public Dictionary<int, Material> GetFamily(in Material mat)
        {
            return GetFamily(mat.ID);
        }

        /// <summary>
        /// Get an specific material
        /// </summary>
        /// <param name="ID">ID if Family</param>
        /// <param name="data">ID of material inside family</param>
        /// <returns>Requested Material, if not found it returns the Invalid Material</returns>
        public Material GetMaterial(int ID, int data)
        {
            if (mSet.TryGetValue(ID, out Dictionary<int, Material> family))
            {
                if (family.TryGetValue(data, out Material mat))
                {
                    return mat;
                }
            }
            return Material.Invalid;
        }
    }
}