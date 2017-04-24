//Joshua Aldrich

using System.Collections.Generic;
using System.Linq;

namespace AScore_DLL.Combinatorics
{
    public class ModMixtureCombo
    {
        List<List<int>> finalCombos = new List<List<int>>();
        List<List<int>> sitePositions;
        List<List<List<int>>> combinationSets;
        List<int> allSites = new List<int>(); //List of all modifiable sites for all modifications

        /// <summary>
        /// Generates all combination mixtures
        /// </summary>
        /// <param name="inCombinationSets">All sets of combinations</param>
        /// <param name="inSitePositions">Possible site positions for each combination group</param>
        public ModMixtureCombo(List<Mod.DynamicModification> dynMods, string sequence)
        {

            sitePositions = GetSiteLocation(dynMods, sequence);
            combinationSets = GenerateCombosToCheck(sitePositions, dynMods);

            //Generates a list of all modifiable sites for all modifications
            foreach (List<int> sites in sitePositions)
            {
                foreach (int s in sites)
                {
                    if (!allSites.Contains(s))
                    {
                        allSites.Add(s);
                    }
                }
            }
            allSites.Sort();
            CalculateCombos(0, new List<List<int>>());
        }

        #region Public Properties

        /// <summary>
        /// Final Combination sets
        /// </summary>
        public List<List<int>> FinalCombos
        {
            get { return finalCombos; }
        }

        /// <summary>
        /// List of all modifiable sites for all modifications
        /// </summary>
        public List<int> AllSite
        {
            get { return allSites; }
        }

        #endregion

        /// <summary>
        /// Creates a set lists ordered by the dynamic modification list order.
        /// Each list contains the indices within the sequence for possible sites of modification
        /// ie R.RFLPSCTK.M would have possiblePositions[0] = {4,6} if phosphorylation were the first
        /// mod in dynamic mods
        /// </summary>
        /// <param name="dynMods">Dynamic modification list from ascoreparamters</param>
        /// <param name="sequence">peptide sequence</param>
        /// <returns>list of lists of possible modification sites</returns>
        public List<List<int>> GetSiteLocation(List<Mod.DynamicModification> dynMods, string sequence)
        {
            List<List<int>> possiblePositions = new List<List<int>>();

            foreach (Mod.DynamicModification dMod in dynMods)
            {
                possiblePositions.Add(new List<int>());
            }


            for (int i = 0; i < sequence.Length; i++)
            {
                int theCount = 0;
                foreach (Mod.DynamicModification dMod in dynMods)
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

                List<int> aFinalCombo = new List<int>();
                //Ensure no overlap between modification combination site positions
                foreach (int s in allSites)
                {
                    int count = 0;
                    //int countAtSameSite = 0;
                    List<int> siteList = new List<int>();

                    //Add all uniqueids or zeros associated with this sequence positions
                    foreach (List<int> site in sitePositions)
                    {
                        if (site.Contains(s))
                        {
                            siteList.Add(currentList[count][site.IndexOf(s)]);
                        }
                        count++;
                    }

                    int greaterThanZero = siteList.Count(k => k > 0);

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
                finalCombos.Add(aFinalCombo);
                if (currentList.Count > 0)
                    currentList.RemoveAt(currentList.Count - 1);
            }
            else
            {

                for (int k = 0; k < combinationSets[i].Count; k++)
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
        /// <param name="myMods">dynamic modications from ascore parameters</param>
        /// <returns></returns>
        public static List<List<List<int>>> GenerateCombosToCheck(List<List<int>> sitePositions, List<Mod.DynamicModification> myMods)
        {
            //first loop is to create the template for each of the combination sets
            //example: say 2 mods if mod1 has three sites in the sequence and mod2 has 2 mods at 4 sites
            //we have template = {{mod1, 0, 0}, {mod2, mod2, 0, 0}}

            List<List<int>> comboTemplate = new List<List<int>>();
            int siteCount = 0;
            foreach (Mod.DynamicModification m in myMods)
            {
                List<int> templateToAdd = new List<int>();
                for (int i = 0; i < m.Count; i++)
                {
                    templateToAdd.Add(m.UniqueID);
                }
                int remainingZeros = sitePositions[siteCount].Count - templateToAdd.Count;
                for (int i = 0; i < remainingZeros; i++)
                {
                    templateToAdd.Add(0);
                }
                comboTemplate.Add(templateToAdd);
                siteCount++;
            }

            //list of mod types with lists of combinations for each type
            List<List<List<int>>> combinationSets = new List<List<List<int>>>();
            foreach (List<int> comb in comboTemplate)
            {
                List<List<int>> allCombos = new List<List<int>>();
                Combinatorics.Permutations<int> modcombos = new Combinatorics.Permutations<int>(comb);
                foreach (IList<int> combination in modcombos)
                {
                    allCombos.Add(new List<int>(combination));
                }
                combinationSets.Add(allCombos);
            }
            return combinationSets;

        }


    }
}
