using System.Collections.Generic;
using System.Linq;

namespace AScore_DLL.Combinatorics
{
    public class ModMixtureCombo
    {
        private readonly List<List<int>> sitePositions;
        private readonly List<List<List<int>>> combinationSets;

        /// <summary>
        /// Generates all combination mixtures
        /// </summary>
        /// <param name="dynMods">Dynamic mods</param>
        /// <param name="sequence">Peptide sequence</param>
        public ModMixtureCombo(List<Mod.DynamicModification> dynMods, string sequence)
        {
            sitePositions = GetSiteLocation(dynMods, sequence);
            combinationSets = GenerateCombosToCheck(sitePositions, dynMods);

            //Generates a list of all modifiable sites for all modifications
            foreach (var sites in sitePositions)
            {
                foreach (var s in sites)
                {
                    if (!AllSite.Contains(s))
                    {
                        AllSite.Add(s);
                    }
                }
            }
            AllSite.Sort();
            CalculateCombos(0, new List<List<int>>());
        }

        #region Public Properties

        /// <summary>
        /// Final Combination sets
        /// </summary>
        public List<List<int>> FinalCombos { get; } = new List<List<int>>();

        /// <summary>
        /// List of all modifiable sites for all modifications
        /// </summary>
        public List<int> AllSite { get; } = new List<int>();

        #endregion

        /// <summary>
        /// Creates a set lists ordered by the dynamic modification list order.
        /// Each list contains the indices within the sequence for possible sites of modification
        /// i.e. R.RFLPSCTK.M would have possiblePositions[0] = {4,6} if phosphorylation were the first
        /// mod in dynamic mods
        /// </summary>
        /// <param name="dynMods">Dynamic modification list from AScore parameters</param>
        /// <param name="sequence">peptide sequence</param>
        /// <returns>list of lists of possible modification sites</returns>
        public List<List<int>> GetSiteLocation(List<Mod.DynamicModification> dynMods, string sequence)
        {
            var possiblePositions = new List<List<int>>();

            foreach (var _ in dynMods)
            {
                possiblePositions.Add(new List<int>());
            }

            for (var i = 0; i < sequence.Length; i++)
            {
                var theCount = 0;
                foreach (var dMod in dynMods)
                {
                    if (dMod.nTerminus && i == 0)
                    {
                        possiblePositions[theCount].Add(i);
                    }
                    else if (dMod.cTerminus && i == sequence.Length - 1)
                    {
                        possiblePositions[theCount].Add(i);
                    }
                    else if (dMod.Match(sequence[i]))
                    {
                        possiblePositions[theCount].Add(i);
                    }
                    theCount++;
                }
            }

            return possiblePositions;
        }

        /// <summary>
        /// Recursively calculates all possible combination mixtures and stores them
        /// in finalCombos
        /// </summary>
        /// <param name="i">depth</param>
        /// <param name="currentList">current list of lists we are adding to</param>
        public void CalculateCombos(int i, List<List<int>> currentList)
        {
            //Reached max depth of recursion
            if (i >= combinationSets.Count)
            {
                var aFinalCombo = new List<int>();
                //Ensure no overlap between modification combination site positions
                foreach (var s in AllSite)
                {
                    var count = 0;
                    //int countAtSameSite = 0;
                    var siteList = new List<int>();

                    //Add all unique ids or zeros associated with this sequence positions
                    foreach (var site in sitePositions)
                    {
                        if (site.Contains(s))
                        {
                            siteList.Add(currentList[count][site.IndexOf(s)]);
                        }
                        count++;
                    }

                    var greaterThanZero = siteList.Count(k => k > 0);

                    //if site has 1 nonzero modification or only zeros continue
                    //if more than 1 nonzero modification, overlap at site, remove

                    if (greaterThanZero == 0)
                    {
                        aFinalCombo.Add(0);
                    }
                    else if (greaterThanZero == 1)
                    {
                        aFinalCombo.Add(siteList.Find(c => c != 0));
                    }
                    else if (greaterThanZero > 1)
                    {
                        currentList.RemoveAt(currentList.Count - 1);
                        return;
                    }
                }
                FinalCombos.Add(aFinalCombo);
                if (currentList.Count > 0)
                    currentList.RemoveAt(currentList.Count - 1);
            }
            else
            {
                for (var k = 0; k < combinationSets[i].Count; k++)
                {
                    currentList.Add(combinationSets[i][k]);
                    CalculateCombos(i + 1, currentList);
                    currentList.Remove(combinationSets[i][k]);
                }
            }
        }

        /// <summary>
        /// Creates a list of modification types each having a list of positional combinations which are themselves int lists
        /// </summary>
        /// <param name="sitePositions">list of site position lists</param>
        /// <param name="myMods">dynamic modifications from AScore parameters</param>
        /// <returns></returns>
        public static List<List<List<int>>> GenerateCombosToCheck(List<List<int>> sitePositions, List<Mod.DynamicModification> myMods)
        {
            // The first loop is to create the template for each of the combination sets
            // For example: given 2 mods, where mod1 has three sites in the sequence and mod2 has 2 mods at 4 sites,
            // create template = {{mod1, 0, 0}, {mod2, mod2, 0, 0}}

            var comboTemplate = new List<List<int>>();
            var siteCount = 0;
            foreach (var m in myMods)
            {
                var templateToAdd = new List<int>();
                for (var i = 0; i < m.Count; i++)
                {
                    templateToAdd.Add(m.UniqueID);
                }
                var remainingZeros = sitePositions[siteCount].Count - templateToAdd.Count;
                for (var i = 0; i < remainingZeros; i++)
                {
                    templateToAdd.Add(0);
                }
                comboTemplate.Add(templateToAdd);
                siteCount++;
            }

            //list of mod types with lists of combinations for each type
            var combinationSets = new List<List<List<int>>>();
            foreach (var combo in comboTemplate)
            {
                var allCombos = new List<List<int>>();

                foreach (IList<int> combination in new Permutations<int>(combo))
                {
                    allCombos.Add(new List<int>(combination));
                }
                combinationSets.Add(allCombos);
            }
            return combinationSets;
        }
    }
}
