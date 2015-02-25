//Joshua Aldrich

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AScore_DLL.Managers
{
    /// <summary>
    /// Stores the parameters for a given AScore run
    /// </summary>
    public class AScoreParameters
    {
        private List<Mod.Modification> staticMods;
        private List<Mod.TerminiModification> terminiMods;
        private List<Mod.DynamicModification> dynamMods;
        private FragmentType fragmentType;
        private double fragmentMassTolerance;




        #region Public Properties
        public List<Mod.Modification> StaticMods { get {return staticMods;} }
        public List<Mod.TerminiModification> TerminiMods { get{return terminiMods;}}
        public List<Mod.DynamicModification> DynamicMods { get { return dynamMods; }  }
        public FragmentType FragmentType { get { return fragmentType; } }
        public double FragmentMassTolerance { get { return fragmentMassTolerance; } }
        #endregion

        /// <summary>
        /// Make a copy of an ascoreparameters set
        /// </summary>
        /// <returns></returns>
        public AScoreParameters Copy()
        {
            return new AScoreParameters(new List<Mod.Modification>(staticMods), new List<Mod.TerminiModification>(terminiMods),
                new List<Mod.DynamicModification>(dynamMods), fragmentType, fragmentMassTolerance);

        }

        #region AScoreParameters Constructors

        public AScoreParameters(List<Mod.Modification> stat, List<Mod.TerminiModification> term,
            List<Mod.DynamicModification> dynam, FragmentType f, double tol)
        {

            staticMods = stat;
            terminiMods = term;
            dynamMods = dynam;
            fragmentType = f;
            fragmentMassTolerance = tol;
        }

        public AScoreParameters(List<Mod.Modification> stat, FragmentType f, double tol)
        {
            staticMods = stat;
            terminiMods = new List<Mod.TerminiModification>();
            //      dynamMods = new List<Mod.DynamicModification>();
            fragmentType = f;
            fragmentMassTolerance = tol;
        }

        public AScoreParameters(FragmentType f, double tol)
        {
            staticMods = new List<Mod.Modification>();
            terminiMods = new List<Mod.TerminiModification>();
            //      dynamMods = new List<Mod.DynamicModification>();
            fragmentType = f;
            fragmentMassTolerance = tol;
        }
        #endregion

    }
}
