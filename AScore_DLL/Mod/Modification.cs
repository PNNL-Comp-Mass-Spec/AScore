using System.Collections.Generic;

namespace AScore_DLL.Mod
{
    /// <summary>
    /// Modification object
    /// </summary>
    public class Modification
    {
        public double MassMonoisotopic { get; set; }
        public double MassAverage { get; set; }
        public char ModSymbol { get; set; }
        public List<char> PossibleModSites { get; set; }
        public int UniqueID { get; set; }
        public MassType ModMassType { get; set; }

        // nTerminus and cTerminus are used by both static and dynamic mods to indicate whether the mod affects the N-terminus or the C-terminus
        public bool nTerminus { get; set; }
        public bool cTerminus { get; set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        public Modification()
        {
        }

        /// <summary>
        /// Constructor whose source is a dynamic mod entry
        /// </summary>
        public Modification(Modification itemToCopy)
        {
            CopyFrom(itemToCopy);
        }

        protected void CopyFrom(Modification itemToCopy)
        {
            MassMonoisotopic = itemToCopy.MassMonoisotopic;
            MassAverage = itemToCopy.MassAverage;
            ModSymbol = itemToCopy.ModSymbol;
            PossibleModSites = itemToCopy.PossibleModSites;
            UniqueID = itemToCopy.UniqueID;
            ModMassType = itemToCopy.ModMassType;
            nTerminus = itemToCopy.nTerminus;
            cTerminus = itemToCopy.cTerminus;
        }

        public double Mass
        {
            get
            {
                switch (ModMassType)
                {
                    case MassType.Average: return MassAverage;
                    case MassType.Monoisotopic: return MassMonoisotopic;
                    default: return -1.0;
                }
            }
        }

        /// <summary>
        /// Matching method, empty lists always matches
        /// </summary>
        /// <param name="c">amino acid to match</param>
        /// <returns>whether site can be modified by this modification</returns>
        public virtual bool Match(char c)
        {
            if (PossibleModSites.Count == 0)
            {
                return true;
            }

            foreach (var p in PossibleModSites)
            {
                if (p == ' ' || c == p)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
