using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AScore_DLL.Managers;
using AScore_DLL.Managers.DatasetManagers;
using AScore_DLL.Mod;
using PRISM;

namespace AScore_DLL
{
    public class AScoreAlgorithm : EventNotifier
    {
        // Ignore Spelling: Da, diff, ascore

        private const double MASS_C13 = 1.00335483;

        private readonly double[] ScoreWeights = { 0.5, 0.75, 1.0, 1.0, 1.0, 1.0, 0.75, 0.5, 0.25, 0.25 };

        #region Public Method

        public void ComputeAScore(DatasetManager datasetManager, ParameterFileManager ascoreParameters, int scanNumber,
            int chargeState, string peptideSeq, string front, string back, string sequenceClean, ExperimentalSpectra expSpec,
            double mzMax, double mzMin, IReadOnlyList<int[]> myPositionsList)
        {
            // Change the charge state to 2+ if it is 1+
            if (chargeState == 1)
            {
                chargeState = 2;
            }

            // Parallel lists of scores
            var peptideScores = new List<List<double>>();
            var weightedScores = new List<List<double>>();

            try
            {
                var theoreticalMonoMassSpectra = new TheoreticalSpectra(sequenceClean, ascoreParameters, chargeState, new List<DynamicModification>(), MassType.Monoisotopic);
                var theoreticalAverageMassSpectra = new TheoreticalSpectra(sequenceClean, ascoreParameters, chargeState, new List<DynamicModification>(), MassType.Average);
                var peptideMassTheoretical = theoreticalMonoMassSpectra.PeptideNeutralMassWithStaticMods + GetModMassTotal(peptideSeq, ascoreParameters.DynamicMods);

                if (Math.Abs(peptideMassTheoretical - expSpec.PrecursorNeutralMass) > 20)
                {
                    OnWarningEvent(string.Format(
                                       "Scan {0}: Observed precursor mass of {1:F1} Da is more than 20 Da away from the computed mass of {2:F1} Da; DeltaMass = {3:F1} Da",
                                       scanNumber,
                                       expSpec.PrecursorNeutralMass,
                                       peptideMassTheoretical,
                                       expSpec.PrecursorNeutralMass - peptideMassTheoretical));
                }
                else
                {
                    // Make sure the masses agree within a reasonable tolerance
                    var validMatch = false;

                    for (double chargeAdjust = 0; chargeAdjust < 0.1; chargeAdjust += 0.005)
                    {
                        for (var massAdjust = -chargeState - 3; massAdjust <= chargeState + 3; massAdjust++)
                        {
                            var delM = peptideMassTheoretical - expSpec.PrecursorNeutralMass + massAdjust * MASS_C13;
                            if (Math.Abs(delM) < 0.15 + chargeState * chargeAdjust)
                            {
                                validMatch = true;
                                break;
                            }
                        }

                        if (validMatch)
                            break;
                    }

                    if (!validMatch)
                    {
                        OnWarningEvent(string.Format(
                                         "Scan {0}: Observed precursor mass of {1:F1} Da is not a reasonable match for computed mass of {2:F1} Da; " +
                                         "DeltaMass = {3:F1} Da; Peptide = {4}",
                                         scanNumber,
                                         expSpec.PrecursorNeutralMass,
                                         peptideMassTheoretical,
                                         expSpec.PrecursorNeutralMass - peptideMassTheoretical,
                                         peptideSeq
                                     ));
                    }
                }

                var modNumber = 0;
                foreach (var myPositions in myPositionsList)
                {
                    //Generate spectra for a modification combination
                    var myIons = GetChargeList(ascoreParameters, mzMax, mzMin, theoreticalMonoMassSpectra, theoreticalAverageMassSpectra, myPositions);
                    peptideScores.Add(new List<double>());
                    weightedScores.Add(new List<double>());

                    for (var peakDepth = 1; peakDepth < 11; ++peakDepth)
                    {
                        var peakDepthSpectra = expSpec.GetPeakDepthSpectra(peakDepth);
                        peakDepthSpectra.Sort();

                        var matchedIons = GetMatchedMZ(ascoreParameters.FragmentMassTolerance, myIons, peakDepthSpectra);

                        //Adjusted peptide score to score based on tolerance window.
                        var score = PeptideScoresManager.GetPeptideScore(
                            peakDepth * ascoreParameters.FragmentMassTolerance * 2 / 100.0, myIons.Count, matchedIons.Count);

                        // Check if there were any negative scores
                        peptideScores[modNumber].Add(score);
                        weightedScores[modNumber].Add(score * ScoreWeights[peakDepth - 1]);
                    }
                    modNumber++;
                }

                var sortedSumScore = new List<ValueIndexPair<double>>();
                for (var seq = 0; seq < peptideScores.Count; ++seq)
                {
                    var score = 0.0;
                    for (var depth = 0; depth < peptideScores[seq].Count; ++depth)
                    {
                        score += weightedScores[seq][depth];
                    }
                    sortedSumScore.Add(new ValueIndexPair<double>(score, seq));
                }

                sortedSumScore.Sort();
                var topPeptideScore = sortedSumScore[0].Value;

                // Need the phosphorylation sites for the top peptide
                var topPeptidePTMSites = myPositionsList[sortedSumScore[0].Index];

                var ascoreResults = CalculateAScoreForSite(ascoreParameters, expSpec, mzMax, mzMin, myPositionsList, topPeptidePTMSites, peptideScores, theoreticalMonoMassSpectra,
                    theoreticalAverageMassSpectra, sortedSumScore);

                foreach (var ascoreResult in ascoreResults)
                {
                    ascoreResult.SecondSequence = front + "." +
                        GenerateFinalSequences(sequenceClean, ascoreParameters, ascoreResult.PeptideMods) + "." + back;
                }

                //Put scores into our table
                var bestSeq = front + "." + GenerateFinalSequences(sequenceClean, ascoreParameters, topPeptidePTMSites) + "." + back;
                foreach (var ascoreResult in ascoreResults)
                {
                    datasetManager.WriteToTable(peptideSeq, bestSeq, scanNumber, topPeptideScore, ascoreResult);
                }
            }
            catch (Exception ex)
            {
                OnErrorEvent("Exception in ComputeAScore: " + ex.Message);
                throw;
            }
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Calculate the AScore for the given site information
        /// </summary>
        /// <param name="ascoreParameters"></param>
        /// <param name="expSpec"></param>
        /// <param name="mzMax"></param>
        /// <param name="mzMin"></param>
        /// <param name="myPositionsList"></param>
        /// <param name="topPeptidePTMSites"></param>
        /// <param name="peptideScores"></param>
        /// <param name="theoreticalMonoMassSpectra"></param>
        /// <param name="theoreticalAverageMassSpectra"></param>
        /// <param name="sortedSumScore"></param>
        /// <returns></returns>
        private List<AScoreResult> CalculateAScoreForSite(ParameterFileManager ascoreParameters, ExperimentalSpectra expSpec,
                                                          double mzMax, double mzMin,
                                                          IReadOnlyList<int[]> myPositionsList,
                                                          int[] topPeptidePTMSites,
                                                          IReadOnlyList<List<double>> peptideScores,
                                                          TheoreticalSpectra theoreticalMonoMassSpectra,
                                                          TheoreticalSpectra theoreticalAverageMassSpectra,
                                                          IReadOnlyList<ValueIndexPair<double>> sortedSumScore)
        {
            // Initialize AScore results storage
            var lstResults = new List<AScoreResult>();

            var siteInfo = GetSiteDict(topPeptidePTMSites);

            // Get the top sequence theoretical spectra
            var topTheoreticalIons = GetChargeList(ascoreParameters, mzMax, mzMin, theoreticalMonoMassSpectra, theoreticalAverageMassSpectra, topPeptidePTMSites);

            for (var indSite = 0; indSite < siteInfo.Count; ++indSite)
            {
                var ascoreResult = new AScoreResult();
                lstResults.Add(ascoreResult);

                ascoreResult.ModInfo = LookupModInfoByID(siteInfo.Values[indSite], ascoreParameters.DynamicMods);

                int secondPeptide;
                for (secondPeptide = 0; secondPeptide < sortedSumScore.Count; ++secondPeptide)
                {
                    var secondDict = GetSiteDict(myPositionsList[sortedSumScore[secondPeptide].Index]);

                    var othersMatch = true;
                    if (!secondDict.ContainsKey(siteInfo.Keys[indSite]))
                    {
                        var sites = siteInfo.Keys.ToList();
                        for (var i = 0; i < sites.Count; i++)
                            if (i != indSite)
                            {
                                othersMatch = othersMatch && secondDict.ContainsKey(sites[i]);
                            }
                        if (othersMatch)
                        {
                            ascoreResult.PeptideMods = myPositionsList[sortedSumScore[secondPeptide].Index];
                            break;
                        }
                    }
                    else
                    {
                        if (secondDict[siteInfo.Keys[indSite]] != siteInfo.Values[indSite])
                        {
                            ascoreResult.PeptideMods = myPositionsList[sortedSumScore[secondPeptide].Index];
                            break;
                        }
                    }
                }

                if (secondPeptide == sortedSumScore.Count)
                {
                    ascoreResult.AScore = 1000;
                    ascoreResult.NumSiteIons = 0;
                    ascoreResult.SiteDetermineMatched = 0;

                    continue;
                }

                var secondTopPeptidePTMSites = myPositionsList[sortedSumScore[secondPeptide].Index];
                // Get the second best scoring spectra

                var secondTopTheoreticalIons = GetChargeList(ascoreParameters,
                                                             mzMax, mzMin,
                                                             theoreticalMonoMassSpectra,
                                                             theoreticalAverageMassSpectra,
                                                             secondTopPeptidePTMSites);

                // Calculate the diff score between the top and second sites
                var diffScore = new List<ValueIndexPair<double>>();
                for (var i = 0; i < peptideScores[0].Count; ++i)
                {
                    diffScore.Add(new ValueIndexPair<double>(
                        peptideScores[sortedSumScore[0].Index][i] -
                        peptideScores[sortedSumScore[secondPeptide].Index][i], i));
                }

                // Sort in descending order
                diffScore.Sort();

                // Find the peak depth for the diff score
                var peakDepthForAScore = 1;
                if (diffScore[0].Value > 0)
                {
                    peakDepthForAScore = diffScore[0].Index + 1;
                }

                var siteIons1 = GetSiteDeterminingIons(topTheoreticalIons, secondTopTheoreticalIons);
                var siteIons2 = GetSiteDeterminingIons(secondTopTheoreticalIons, topTheoreticalIons);

                var peakDepthSpectraFinal = expSpec.GetPeakDepthSpectra(peakDepthForAScore);
                peakDepthSpectraFinal.Sort();

                var bestDeterminingCount = GetMatchedMZ(ascoreParameters.FragmentMassTolerance, siteIons1, peakDepthSpectraFinal).Count;

                var secondBestDeterminingCount = GetMatchedMZ(ascoreParameters.FragmentMassTolerance, siteIons2, peakDepthSpectraFinal).Count;

                var a1 = PeptideScoresManager.GetPeptideScore(peakDepthForAScore * ascoreParameters.FragmentMassTolerance * 2 / 100,
                    siteIons1.Count, bestDeterminingCount);

                var a2 = PeptideScoresManager.GetPeptideScore(peakDepthForAScore * ascoreParameters.FragmentMassTolerance * 2 / 100,
                    siteIons2.Count, secondBestDeterminingCount);

                // Add the results to the list
                ascoreResult.AScore = Math.Abs(a1 - a2);
                ascoreResult.NumSiteIons = siteIons1.Count;                     // numSiteIonsPoss
                ascoreResult.SiteDetermineMatched = bestDeterminingCount;       // numSiteIonsMatched
            }

            return lstResults;
        }

        /// <summary>
        /// Generates a sequence based on final best peptide sequence.
        /// </summary>
        /// <param name="seq">unmodified sequence</param>
        /// <param name="myParam">ascore parameters</param>
        /// <param name="peptideMods">peptide modification position array</param>
        /// <returns></returns>
        private string GenerateFinalSequences(string seq, ParameterFileManager myParam, IReadOnlyList<int> peptideMods)
        {
            var sbFinalSeq = new System.Text.StringBuilder(seq.Length);

            for (var i = 0; i < seq.Length; i++)
            {
                if (i >= peptideMods.Count)
                {
                    // Invalid index for i; assume the residue is not modified
                    sbFinalSeq.Append(seq[i]);
                }
                else if (peptideMods[i] == 0)
                {
                    sbFinalSeq.Append(seq[i]);
                }
                else
                {
                    foreach (var dynamicMod in myParam.DynamicMods)
                    {
                        if (peptideMods[i] == dynamicMod.UniqueID)
                        {
                            sbFinalSeq.Append(seq[i] + dynamicMod.ModSymbol.ToString(CultureInfo.InvariantCulture));
                        }
                    }
                }
            }

            return sbFinalSeq.ToString();
        }

        private double GetModMassTotal(string peptideSeq, IReadOnlyCollection<DynamicModification> dynMods)
        {
            double modMass = 0;

            foreach (var t in peptideSeq)
            {
                if (!char.IsLetter(t))
                {
                    foreach (var m in dynMods)
                    {
                        if (t == m.ModSymbol)
                        {
                            modMass += m.MassMonoisotopic;
                            break;
                        }
                    }
                }
            }

            return modMass;
        }

        /// <summary>
        /// Generates the current modification set of theoretical ions filtered by the mz range
        /// </summary>
        /// <param name="mzMax">max m/z</param>
        /// <param name="mzMin">min m/z</param>
        /// <param name="mySpectra">dictionary of theoretical ions organized by charge</param>
        /// <returns>list of theoretical ions</returns>
        private List<double> GetCurrentComboTheoreticalIons(double mzMax, double mzMin, Dictionary<int, ChargeStateIons> mySpectra)
        {
            var myIons = new List<double>();
            foreach (var csi in mySpectra.Values)
            {
                foreach (var ion in csi.BIons)
                {
                    if (ion < mzMax && ion > mzMin)
                    {
                        myIons.Add(ion);
                    }
                }
                foreach (var ion in csi.YIons)
                {
                    if (ion < mzMax && ion > mzMin)
                    {
                        myIons.Add(ion);
                    }
                }
            }
            return myIons;
        }

        /// <summary>
        /// Matches the theoretical ions to experimental ions for some tolerance
        /// </summary>
        /// <param name="tolerance">width or window for matching</param>
        /// <param name="tempSpec">theoretical ions</param>
        /// <param name="peakDepthSpectra">Experimental ions; assumed to be sorted by m/z</param>
        /// <returns></returns>
        private List<double> GetMatchedMZ(double tolerance, IEnumerable<double> tempSpec, IReadOnlyList<ExperimentalSpectraEntry> peakDepthSpectra)
        {
            return tempSpec.Where(mz => FindBinarySearch(mz, tolerance, peakDepthSpectra)).ToList();

            //var matchedMZ = new List<double>();
            //ForEach (var mz in tempSpec)
            //{
            //    // A smart usage of the .Net built-in binary search; still about 7% slower than the previous internally implemented binary search
            //    var searchMz = new ExperimentalSpectraEntry(mz, 0);
            //    var result = peakDepthSpectra.BinarySearch(searchMz);
            //    if (result >= 0)
            //    {
            //        matchedMZ.Add(mz);
            //    }
            //    else
            //    {
            //        var loc = ~result;
            //        var lowerCheck = loc > 0 && mz - peakDepthSpectra[loc - 1].Mz <= tolerance;
            //        var higherCheck = loc < peakDepthSpectra.Count && peakDepthSpectra[loc].Mz - mz <= tolerance;
            //        if (lowerCheck || higherCheck)
            //        {
            //            matchedMZ.Add(mz);
            //        }
            //    }
            //}
            //
            //return matchedMZ;
        }

        /// <summary>
        /// A fast binary search; oddly, it's faster than the .NET built-in BinarySearch, probably due to dealing with Comparer results.
        /// </summary>
        /// <param name="mz"></param>
        /// <param name="tolerance"></param>
        /// <param name="peaks"></param>
        /// <returns></returns>
        private bool FindBinarySearch(double mz, double tolerance, IReadOnlyList<ExperimentalSpectraEntry> peaks)
        {
            if (peaks.Count == 0)
            {
                return false;
            }

            var minMz = mz - tolerance;
            var maxMz = mz + tolerance;
            var minIndex = 0;
            var maxIndex = peaks.Count - 1;

            // Assuming list is sorted (requirement for binary search), do a fast check for if the m/z is out of range of the list contents.
            if (maxMz < peaks[minIndex].Mz || peaks[maxIndex].Mz < minMz)
            {
                return false;
            }

            // >= to properly handle a final case
            while (maxIndex >= minIndex)
            {
                var mid = (maxIndex + minIndex) >> 1; // bit-shift for really fast divide by 2
                if (minMz <= peaks[mid].Mz)
                {
                    if (peaks[mid].Mz <= maxMz)
                    {
                        // Match found
                        return true;
                    }

                    // mid is too big; set new max to mid - 1 (because we've already tested mid, and don't want to test it again)
                    maxIndex = mid - 1;
                    continue;
                }

                // mid is too small; set new max to mid + 1 (because we've already tested mid, and don't want to test it again)
                minIndex = mid + 1;
            }

            // didn't find it
            return false;
        }

        /// <summary>
        /// Generates the site determining ions by comparing ions of top two spectra and removing overlapping ions
        /// </summary>
        /// <param name="toGetDetermining">list to get unique from</param>
        /// <param name="secondSpec">list which contains overlap to remove from first list</param>
        /// <returns>list of values unique to the toGetDetermining list</returns>
        private List<double> GetSiteDeterminingIons(IEnumerable<double> toGetDetermining, IEnumerable<double> secondSpec)
        {
            var siteDetermined = new List<double>(toGetDetermining);
            foreach (var ion in secondSpec)
            {
                if (siteDetermined.Contains(ion))
                {
                    siteDetermined.Remove(ion);
                }
            }
            return siteDetermined;
        }

        /// <summary>
        /// Creates a dictionary to store a position and mod type
        /// </summary>
        /// <param name="topPeptidePTMSites"></param>
        /// <returns></returns>
        private SortedList<int, int> GetSiteDict(IReadOnlyList<int> topPeptidePTMSites)
        {
            var siteInfo = new SortedList<int, int>();
            for (var n = 0; n < topPeptidePTMSites.Count; n++)
            {
                if (topPeptidePTMSites[n] > 0)
                {
                    siteInfo.Add(n, topPeptidePTMSites[n]);
                }
            }
            return siteInfo;
        }

        /// <summary>
        /// Gets a list of ions for matching
        /// </summary>
        /// <param name="ascoreParameters">parameters for </param>
        /// <param name="mzMax"></param>
        /// <param name="mzMin"></param>
        /// <param name="theoreticalMonoMassSpectra"></param>
        /// <param name="theoreticalAverageMassSpectra"></param>
        /// <param name="myPositions"></param>
        /// <returns></returns>
        private List<double> GetChargeList(ParameterFileManager ascoreParameters, double mzMax, double mzMin,
                                           TheoreticalSpectra theoreticalMonoMassSpectra,
                                           TheoreticalSpectra theoreticalAverageMassSpectra,
                                           int[] myPositions)
        {
            const double FRAGMENT_MASS_TOLERANCE = 0.0501;

            var mySpectraMono = theoreticalMonoMassSpectra.GetTempSpectra(myPositions,
                                  ascoreParameters.DynamicMods, MassType.Monoisotopic);

            var mySpectra = new Dictionary<int, ChargeStateIons>();

            if (ascoreParameters.FragmentMassTolerance <= FRAGMENT_MASS_TOLERANCE)
            {
                var mySpectraAverage = theoreticalAverageMassSpectra.GetTempSpectra(myPositions,
                    ascoreParameters.DynamicMods, MassType.Average);

                //Get ions within m/z range
                mySpectra.Add(1, mySpectraMono[1]);
                foreach (var charge in mySpectraAverage.Keys)
                {
                    if (charge != 1)
                    {
                        mySpectra.Add(charge, mySpectraAverage[charge]);
                    }
                }
            }
            else
            {
                mySpectra = mySpectraMono;
            }

            var myIons = GetCurrentComboTheoreticalIons(mzMax, mzMin, mySpectra);
            return myIons;
        }

        private string LookupModInfoByID(int uniqueID, IEnumerable<DynamicModification> dynamicMods)
        {
            var modInfo = string.Empty;

            foreach (var m in dynamicMods)
            {
                if (m.UniqueID == uniqueID)
                {
                    foreach (var site in m.PossibleModSites)
                    {
                        modInfo += site;
                    }
                    modInfo += m.ModSymbol;
                    break;
                }
            }

            return modInfo;
        }

        #endregion
    }
}
