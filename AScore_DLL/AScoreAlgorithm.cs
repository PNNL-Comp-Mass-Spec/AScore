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
    public class AScoreAlgorithm : clsEventNotifier
    {
        private const double MASS_C13 = 1.00335483;

        private readonly double[] ScoreWeights = { 0.5, 0.75, 1.0, 1.0, 1.0, 1.0, 0.75, 0.5, 0.25, 0.25 };

        #region Public Method

        public void ComputeAScore(DatasetManager datasetManager, ParameterFileManager ascoreParameters, int scanNumber,
            int chargeState, string peptideSeq, string front, string back, string sequenceClean, ExperimentalSpectra expSpec,
            double mzmax, double mzmin, IReadOnlyList<int[]> myPositionsList)
        {
            // Initialize AScore results storage
            var lstResults = new List<AScoreResult>();

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
                var theoMono = new TheoreticalSpectra(
                    sequenceClean,
                    ascoreParameters,
                    chargeState,
                    new List<DynamicModification>(),
                    MassType.Monoisotopic);

                var theoAve = new TheoreticalSpectra(
                    sequenceClean,
                    ascoreParameters,
                    chargeState,
                    new List<DynamicModification>(),
                    MassType.Average);

                var peptideMassTheoretical = theoMono.PeptideNeutralMassWithStaticMods + GetModMassTotal(peptideSeq, ascoreParameters.DynamicMods);

                if (Math.Abs(peptideMassTheoretical - expSpec.PrecursorNeutralMass) > 20)
                {
                    OnWarningEvent("Scan " + scanNumber + ": Observed precursor mass of " + expSpec.PrecursorNeutralMass.ToString("0.0") + " Da is more than 20 Da away from the computed mass of " + peptideMassTheoretical.ToString("0.0") + " Da; DeltaMass = " + (expSpec.PrecursorNeutralMass - peptideMassTheoretical).ToString("0.0") + " Da");
                }
                else
                {
                    // Make sure the masses agree within a reasonable tolerance
                    var bValidMatch = false;

                    for (double chargeAdjust = 0; chargeAdjust < 0.1; chargeAdjust += 0.005)
                    {
                        for (var massAdjust = -chargeState - 3; massAdjust <= chargeState + 3; massAdjust++)
                        {
                            var delM = peptideMassTheoretical - expSpec.PrecursorNeutralMass + massAdjust * MASS_C13;
                            if (Math.Abs(delM) < 0.15 + chargeState * chargeAdjust)
                            {
                                bValidMatch = true;
                                break;
                            }
                        }

                        if (bValidMatch)
                            break;
                    }

                    if (!bValidMatch)
                        OnErrorEvent("Scan " + scanNumber + ": Observed precursor mass of " + expSpec.PrecursorNeutralMass.ToString("0.0") + " Da is not a reasonable match for computed mass of " + peptideMassTheoretical.ToString("0.0") + " Da; DeltaMass = " + (expSpec.PrecursorNeutralMass - peptideMassTheoretical).ToString("0.0") + " Da; Peptide = " + peptideSeq);
                }

                var modNumber = 0;
                foreach (var myPositions in myPositionsList)
                {
                    //Generate spectra for a modification combination
                    var myIons = GetChargeList(ascoreParameters, mzmax, mzmin, theoMono, theoAve, myPositions);
                    peptideScores.Add(new List<double>());
                    weightedScores.Add(new List<double>());

                    for (var peakDepth = 1; peakDepth < 11; ++peakDepth)
                    {
                        var peakDepthSpectra = expSpec.GetPeakDepthSpectra(peakDepth);
                        peakDepthSpectra.Sort();

                        var matchedIons = GetMatchedMZ(
                            peakDepth, ascoreParameters.FragmentMassTolerance,
                            myIons, peakDepthSpectra);

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
                var topPeptidePTMsites = myPositionsList[sortedSumScore[0].Index];

                var siteInfo = GetSiteDict(topPeptidePTMsites);

                // Get the top sequence theoretical spectra
                var topTheoIons = GetChargeList(ascoreParameters, mzmax, mzmin, theoMono, theoAve, topPeptidePTMsites);

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

                    var secondTopPeptidePTMsites = myPositionsList[sortedSumScore[secondPeptide].Index];
                    // Get the second best scoring spectra

                    var secondTopTheoIons = GetChargeList(ascoreParameters, mzmax, mzmin, theoMono, theoAve, secondTopPeptidePTMsites);

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

                    var siteIons1 = GetSiteDeterminingIons(topTheoIons, secondTopTheoIons);
                    var siteIons2 = GetSiteDeterminingIons(secondTopTheoIons, topTheoIons);

                    var peakDepthSpectraFinal = expSpec.GetPeakDepthSpectra(peakDepthForAScore);
                    peakDepthSpectraFinal.Sort();

                    var bestDeterminingCount = GetMatchedMZ(peakDepthForAScore,
                        ascoreParameters.FragmentMassTolerance, siteIons1, peakDepthSpectraFinal).Count;

                    var secondBestDeterminingCount = GetMatchedMZ(peakDepthForAScore,
                        ascoreParameters.FragmentMassTolerance, siteIons2, peakDepthSpectraFinal).Count;

                    var a1 = PeptideScoresManager.GetPeptideScore(peakDepthForAScore * ascoreParameters.FragmentMassTolerance * 2 / 100,
                        siteIons1.Count, bestDeterminingCount);

                    var a2 = PeptideScoresManager.GetPeptideScore(peakDepthForAScore * ascoreParameters.FragmentMassTolerance * 2 / 100,
                        siteIons2.Count, secondBestDeterminingCount);

                    // Add the results to the list
                    ascoreResult.AScore = Math.Abs(a1 - a2);
                    ascoreResult.NumSiteIons = siteIons1.Count;                     // numSiteIonsPoss
                    ascoreResult.SiteDetermineMatched = bestDeterminingCount;       // numSiteIonsMatched
                }

                foreach (var ascoreResult in lstResults)
                {
                    ascoreResult.SecondSequence = front + "." +
                        GenerateFinalSequences(sequenceClean, ascoreParameters, ascoreResult.PeptideMods) + "." + back;
                }

                //Put scores into our table
                var bestSeq = front + "." + GenerateFinalSequences(sequenceClean, ascoreParameters, topPeptidePTMsites) + "." + back;
                foreach (var ascoreResult in lstResults)
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
                    foreach (var dmod in myParam.DynamicMods)
                    {
                        if (peptideMods[i] == dmod.UniqueID)
                        {
                            sbFinalSeq.Append(seq[i] + dmod.ModSymbol.ToString(CultureInfo.InvariantCulture));
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
        /// <param name="mzmax">max m/z</param>
        /// <param name="mzmin">min m/z</param>
        /// <param name="mySpectra">dictionary of theoretical ions organized by charge</param>
        /// <returns>list of theoretical ions</returns>
        private List<double> GetCurrentComboTheoreticalIons(double mzmax, double mzmin, Dictionary<int, ChargeStateIons> mySpectra)
        {
            var myIons = new List<double>();
            foreach (var csi in mySpectra.Values)
            {
                foreach (var ion in csi.BIons)
                {
                    if (ion < mzmax && ion > mzmin)
                    {
                        myIons.Add(ion);
                    }
                }
                foreach (var ion in csi.YIons)
                {
                    if (ion < mzmax && ion > mzmin)
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
        /// <param name="peakDepth">number of peaks per 100m/z range</param>
        /// <param name="tolerance">width or window for matching</param>
        /// <param name="tempSpec">theoretical ions</param>
        /// <param name="peakDepthSpectra">Experimental ions; assumed to be sorted by m/z</param>
        /// <param name="binarySearcher"></param>
        /// <returns></returns>
        private List<double> GetMatchedMZ(int peakDepth,
            double tolerance, IEnumerable<double> tempSpec,
            List<ExperimentalSpectraEntry> peakDepthSpectra)
        {
            var matchedMZ = new List<double>();

            // Uncomment to use .NET's binary search (turns out to be 7% slower than using BinarySearchRange)
            // var massComparer = new ExperimentalSpectraEntry.FindValue1InTolerance(tolerance);

            foreach (var mz in tempSpec)
            {
                // Uncomment to use .NET's binary search
                //var searchMz = new ExperimentalSpectraEntry(mz, 0);
                //if (peakDepthSpectra.BinarySearch(searchMz, massComparer) > -1)
                //{
                //    // At least one data point is within tolerance of mz
                //    matchedMZ.Add(mz);
                //}

                // Use BinarySearchRange
                if (BinarySearchRange.FindValueRange(peakDepthSpectra, mz, tolerance, out _, out _))
                {
                    matchedMZ.Add(mz);
                }
            }

            return matchedMZ;
        }

        /// <summary>
        /// Generates the site determining ions by comparing ions of tope two spectra and removing overlapping ions
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
        /// <param name="topPeptidePTMsites"></param>
        /// <returns></returns>
        private SortedList<int, int> GetSiteDict(IReadOnlyList<int> topPeptidePTMsites)
        {
            var siteInfo = new SortedList<int, int>();
            for (var n = 0; n < topPeptidePTMsites.Count; n++)
            {
                if (topPeptidePTMsites[n] > 0)
                {
                    siteInfo.Add(n, topPeptidePTMsites[n]);
                }
            }
            return siteInfo;
        }

        /// <summary>
        /// Gets a list of ions for matching
        /// </summary>
        /// <param name="ascoreParameters">parameters for </param>
        /// <param name="mzmax"></param>
        /// <param name="mzmin"></param>
        /// <param name="theoMono"></param>
        /// <param name="theoAve"></param>
        /// <param name="myPositions"></param>
        /// <returns></returns>
        private List<double> GetChargeList(ParameterFileManager ascoreParameters, double mzmax, double mzmin,
            TheoreticalSpectra theoMono, TheoreticalSpectra theoAve, int[] myPositions)
        {
            var mySpectraMono = theoMono.GetTempSpectra(myPositions,
                                  ascoreParameters.DynamicMods, MassType.Monoisotopic);
            Dictionary<int, ChargeStateIons> mySpectraAverage = null;
            if (ascoreParameters.FragmentMassTolerance <= 0.0501)
            {
                mySpectraAverage = theoAve.GetTempSpectra(myPositions,
                    ascoreParameters.DynamicMods, MassType.Average);
            }
            //Get ions within m/z range
            var mySpectra = new Dictionary<int, ChargeStateIons>();
            if (ascoreParameters.FragmentMassTolerance <= 0.0501)
            {
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

            var myIons = GetCurrentComboTheoreticalIons(mzmax, mzmin, mySpectra);
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
