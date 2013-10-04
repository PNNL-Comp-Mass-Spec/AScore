//Joshua Aldrich

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AScore_DLL.Managers;
using AScore_DLL.Managers.DatasetManagers;


namespace AScore_DLL
{
	public class Algorithm : MessageEventBase
	{
		public const string MODINFO_NO_MODIFIED_RESIDUES = "-";
		protected const double MASS_C13 = 1.00335483;

		private readonly double[] ScoreWeights = { 0.5, 0.75, 1.0, 1.0, 1.0, 1.0, 0.75, 0.5, 0.25, 0.25 };
		private const double lowRangeMultiplier = 0.28;
		private const double maxRange = 2000.0;
		private const double minRange = 50.0;

		private bool m_filterOnMSGFScore = true;

		#region "Properties"

		public bool FilterOnMSGFScore
		{
			get
			{
				return m_filterOnMSGFScore;
			}
			set
			{
				m_filterOnMSGFScore = value;
			}
		}

		#endregion

		#region Public Method

		/// <summary>
		/// Runs the all the tools necessary to perform an ascore run
		/// </summary>
		/// <param name="dtaManager"></param>
		/// <param name="datasetManager"></param>
		/// <param name="ascoreParameters"></param>
		/// <param name="outputFilePath"></param>
		public void AlgorithmRun(DtaManager dtaManager, DatasetManager datasetManager,
			ParameterFileManager ascoreParameters, string outputFilePath)
		{

			int totalRows = datasetManager.GetRowLength();
			var dctPeptidesProcessed = new Dictionary<string, int>();

			while (datasetManager.CurrentRowNum < totalRows)
			{
				//	Console.Clear();


				if (datasetManager.CurrentRowNum % 100 == 0)
				{
					Console.Write("\rPercent Completion " + Math.Round((double)datasetManager.CurrentRowNum / totalRows * 100) + "%");
				}

				int scanNumber;
				int scanCount;
				int chargeState;
				string peptideSeq;
				double msgfScore;

				if (m_filterOnMSGFScore)
					datasetManager.GetNextRow(out scanNumber, out scanCount, out chargeState, out peptideSeq, out msgfScore, ref ascoreParameters);
				else
				{
					datasetManager.GetNextRow(out scanNumber, out scanCount, out chargeState, out peptideSeq, ref ascoreParameters);
					msgfScore = 1;
				}

				string[] splittedPep = peptideSeq.Split('.');
				string sequenceWithoutSuffixOrPrefix;
				string front;
				string back;

				if (splittedPep.Length >= 3)
				{
					front = splittedPep[0];
					sequenceWithoutSuffixOrPrefix = splittedPep[1];
					back = splittedPep[2];
				}
				else
				{
					front = "?";
					sequenceWithoutSuffixOrPrefix = string.Copy(peptideSeq);
					back = "?";
				}

				string sequenceClean = GetCleanSequence(sequenceWithoutSuffixOrPrefix, ref ascoreParameters);
				bool skipPSM = m_filterOnMSGFScore && msgfScore > ascoreParameters.MSGFPreFilter;

				string scanChargePeptide = scanNumber + "_" + chargeState + "_" + sequenceWithoutSuffixOrPrefix;
				if (dctPeptidesProcessed.ContainsKey(scanChargePeptide))
					// We have already processed this PSM
					skipPSM = true;
				else
					dctPeptidesProcessed.Add(scanChargePeptide, 0);

				if (skipPSM)
				{
					datasetManager.IncrementRow();
					continue;
				}


				//Get experimental spectra
				ExperimentalSpectra expSpec = dtaManager.GetExperimentalSpectra(
					scanNumber, scanCount, chargeState);

				if (expSpec == null)
				{
					ReportWarning("Scan " + scanNumber + " not found in DTA file for peptide " + peptideSeq);
					datasetManager.IncrementRow();
					continue;
				}

				// Assume monoisotopic if hi-res however in low res we use average for higher charge states.
				MolecularWeights.MassType = MassType.Monoisotopic;

				// Compute precursor m/z value
				double precursorMZ = PHRPReader.clsPeptideMassCalculator.ConvoluteMass(expSpec.PrecursorMass, 1, chargeState);

				// Set the m/z range
				// Remove magic numbers parameterize
				double mzmax = maxRange;
				double mzmin = precursorMZ * lowRangeMultiplier;
				if (ascoreParameters.FragmentType != FragmentType.CID)
				{
					mzmax = maxRange;
					mzmin = minRange;
				}

				//Generate all combination mixtures				
				var modMixture = new Combinatorics.ModMixtureCombo(ascoreParameters.DynamicMods, sequenceClean);

				List<int[]> myPositionsList = GetMyPostionList(sequenceClean, modMixture);

				//If I have more than 1 modifiable site proceed to calculation
				if (myPositionsList.Count > 1)
				{
					ComputeAScore(datasetManager, ascoreParameters, scanNumber, chargeState, peptideSeq, front, back, sequenceClean, expSpec, mzmax, mzmin, myPositionsList);
				}
				else if (myPositionsList.Count == 1)
				{
					// Either one or no modifiable sites
					int uniqueID = myPositionsList[0].Max();
					if (uniqueID == 0)
						datasetManager.WriteToTable(peptideSeq, scanNumber, 0, myPositionsList[0], MODINFO_NO_MODIFIED_RESIDUES);
					else
						datasetManager.WriteToTable(peptideSeq, scanNumber, 0, myPositionsList[0], LookupModInfoByID(uniqueID, ascoreParameters.DynamicMods));
				}
				else
				{
					// No modifiable sites
					datasetManager.WriteToTable(peptideSeq, scanNumber, 0, myPositionsList[0], MODINFO_NO_MODIFIED_RESIDUES);
				}
				datasetManager.IncrementRow();

			}

			ReportMessage("Writing " + datasetManager.ResultsCount + " rows to the output file: " + outputFilePath);

			datasetManager.WriteToFile(outputFilePath);

		}


		#endregion

		#region Private Methods

		private void ComputeAScore(DatasetManager datasetManager, ParameterFileManager ascoreParameters, int scanNumber, int chargeState, string peptideSeq, string front, string back, string sequenceClean, ExperimentalSpectra expSpec, double mzmax, double mzmin, List<int[]> myPositionsList)
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
					new List<Mod.DynamicModification>(), 
					MassType.Monoisotopic);

				var theoAve = new TheoreticalSpectra(
					sequenceClean, 
					ascoreParameters, 
					chargeState, 
					new List<Mod.DynamicModification>(), 
					MassType.Average);

				double peptideMassTheoretical = theoMono.PeptideNeutralMassWithStaticMods + GetModMassTotal(peptideSeq, ascoreParameters.DynamicMods);

				if (Math.Abs(peptideMassTheoretical - expSpec.PrecursorNeutralMass) > 20)
				{
					ReportWarning("Observed precursor mass of " + expSpec.PrecursorNeutralMass.ToString("0.0") + " Da is more than 20 Da away from the computed mass of " + peptideMassTheoretical.ToString("0.0") + " Da; DeltaMass = " + (expSpec.PrecursorNeutralMass - peptideMassTheoretical).ToString("0.0") + " Da");
				}
				else
				{
					// Make sure the masses agree within a reasonable tolerance
					bool bValidMatch = false;

					for (double chargeAdjust = 0; chargeAdjust < 0.1; chargeAdjust += 0.005)
					{
						for (int massAdjust = -chargeState - 3; massAdjust <= chargeState + 3; massAdjust++)
						{
							double delM = peptideMassTheoretical - expSpec.PrecursorNeutralMass + massAdjust * MASS_C13;
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
						ReportError("Observed precursor mass of " + expSpec.PrecursorNeutralMass.ToString("0.0") + " Da is not a reasonable match for computed mass of " + peptideMassTheoretical.ToString("0.0") + " Da; DeltaMass = " + (expSpec.PrecursorNeutralMass - peptideMassTheoretical).ToString("0.0") + " Da; Peptide = " + peptideSeq);

				}

				var sortByMass = new ExperimentalSpectraEntry.SortValue1();
				var binarySearcher = new BinarySearchRange();
				int modNumber = 0;
				foreach (int[] myPositions in myPositionsList)
				{

					//Generate spectra for a modification combination
					List<double> myIons = GetChargeList(ascoreParameters, mzmax, mzmin, theoMono, theoAve, myPositions);
					peptideScores.Add(new List<double>());
					weightedScores.Add(new List<double>());

					for (int peakDepth = 1; peakDepth < 11; ++peakDepth)
					{
						var peakDepthSpectra = expSpec.GetPeakDepthSpectra(peakDepth);
						peakDepthSpectra.Sort(sortByMass);
					
						List<double> matchedIons = GetMatchedMZ(
						    peakDepth, ascoreParameters.FragmentMassTolerance,
						    myIons, peakDepthSpectra, binarySearcher);

						//Adjusted peptide score to score based on tolerance window.
						double score = PeptideScoresManager.GetPeptideScore(
							((double)peakDepth * ascoreParameters.FragmentMassTolerance * 2) / 100.0, myIons.Count, matchedIons.Count);

						// Check if there were any negative scores
						peptideScores[modNumber].Add(score);
						weightedScores[modNumber].Add(score * ScoreWeights[peakDepth - 1]);
					}
					modNumber++;
				}

				var sortedSumScore = new List<ValueIndexPair<double>>();
				for (int seq = 0; seq < peptideScores.Count; ++seq)
				{
					double score = 0.0;
					for (int depth = 0; depth < peptideScores[seq].Count; ++depth)
					{
						score += weightedScores[seq][depth];
					}
					sortedSumScore.Add(new ValueIndexPair<double>(score, seq));
				}

				sortedSumScore.Sort(new ValueIndexPair<double>.SortValueDescend());
				double topPeptideScore = sortedSumScore[0].Value;

				// Need the phosphorylation sites for the top peptide
				int[] topPeptidePTMsites = myPositionsList[sortedSumScore[0].Index];

				SortedList<int, int> siteInfo = GetSiteDict(topPeptidePTMsites);

				// Get the top sequence theoretical spectra
				List<double> topTheoIons = GetChargeList(ascoreParameters, mzmax, mzmin, theoMono, theoAve, topPeptidePTMsites);

				for (int indSite = 0; indSite < siteInfo.Count; ++indSite)
				{
					var ascoreResult = new AScoreResult();
					lstResults.Add(ascoreResult);

					ascoreResult.ModInfo = LookupModInfoByID(siteInfo.Values[indSite], ascoreParameters.DynamicMods);

					int secondPeptide = 0;
					for (secondPeptide = 0; secondPeptide < sortedSumScore.Count; ++secondPeptide)
					{
						SortedList<int, int> secondDict = GetSiteDict(myPositionsList[sortedSumScore[secondPeptide].Index]);

						bool othersMatch = true;
						if (!secondDict.ContainsKey(siteInfo.Keys[indSite]))
						{
							List<int> sites = siteInfo.Keys.ToList();
							for (int i = 0; i < sites.Count; i++)
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


					int[] secondTopPeptidePTMsites = myPositionsList[sortedSumScore[secondPeptide].Index];
					// Get the second best scoring spectra

					List<double> secondTopTheoIons = GetChargeList(ascoreParameters, mzmax, mzmin, theoMono, theoAve, secondTopPeptidePTMsites);


					// Calculate the diff score between the top and second sites
					var diffScore = new List<ValueIndexPair<double>>();
					for (int i = 0; i < peptideScores[0].Count; ++i)
					{
						diffScore.Add(new ValueIndexPair<double>(
							peptideScores[sortedSumScore[0].Index][i] -
							peptideScores[sortedSumScore[secondPeptide].Index][i], i));
					}

					// Sort in descending order
					diffScore.Sort(new ValueIndexPair<double>.SortValueDescend());

					// Find the peak depth for the diff score
					int peakDepthForAScore = 1;
					if (diffScore[0].Value > 0)
					{
						peakDepthForAScore = diffScore[0].Index + 1;
					}

					List<double> siteIons1 = GetSiteDeterminingIons(topTheoIons, secondTopTheoIons);
					List<double> siteIons2 = GetSiteDeterminingIons(secondTopTheoIons, topTheoIons);

					List<ExperimentalSpectraEntry> peakDepthSpectraFinal = expSpec.GetPeakDepthSpectra(peakDepthForAScore);
					peakDepthSpectraFinal.Sort(sortByMass);

					int bestDeterminingCount = GetMatchedMZ(peakDepthForAScore,
						ascoreParameters.FragmentMassTolerance, siteIons1, peakDepthSpectraFinal, binarySearcher).Count;

					int secondBestDeterminingCount = GetMatchedMZ(peakDepthForAScore,
						ascoreParameters.FragmentMassTolerance, siteIons2, peakDepthSpectraFinal, binarySearcher).Count;

					double a1 = PeptideScoresManager.GetPeptideScore(((double)peakDepthForAScore * ascoreParameters.FragmentMassTolerance * 2) / 100,
						siteIons1.Count, bestDeterminingCount);

					double a2 = PeptideScoresManager.GetPeptideScore(((double)peakDepthForAScore * ascoreParameters.FragmentMassTolerance * 2) / 100,
						siteIons2.Count, secondBestDeterminingCount);

					// Add the results to the list
					ascoreResult.AScore = Math.Abs(a1 - a2);
					ascoreResult.NumSiteIons = siteIons1.Count;						// numSiteIonsPoss
					ascoreResult.SiteDetermineMatched = bestDeterminingCount;		// numSiteIonsMatched

				}

				foreach (AScoreResult ascoreResult in lstResults)
				{
					ascoreResult.SecondSequence = front + "." +
						GenerateFinalSequences(sequenceClean, ascoreParameters, ascoreResult.PeptideMods) + "." + back;
				}


				//Put scores into our table
				string bestSeq = front + "." + GenerateFinalSequences(sequenceClean, ascoreParameters, topPeptidePTMsites) + "." + back;
				foreach (AScoreResult ascoreResult in lstResults)
				{
					datasetManager.WriteToTable(peptideSeq, bestSeq, scanNumber, topPeptideScore, ascoreResult);
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("Exception in ComputeAScore: " + ex.Message);
				throw;
			}
		}

		/// <summary>
		/// Gets a clean sequence intitializes dynamic modifications
		/// </summary>
		/// <param name="seq">input protein sequence including mod characters, but without the prefix or suffix residues</param>
		/// <param name="ascoreParameters">ascore parameters reference</param>
		/// <returns>protein sequence without mods as well as changing ascoreParameterss</returns>
		private string GetCleanSequence(string seq, ref ParameterFileManager ascoreParameters)
		{
			foreach (Mod.DynamicModification dmod in ascoreParameters.DynamicMods)
			{
				string newSeq = seq.Replace(dmod.ModSymbol.ToString(), string.Empty);
				dmod.Count = seq.Length - newSeq.Length;
				seq = newSeq;
			}
			return seq;
		}


		/// <summary>
		/// Generates a sequence based on final best peptide sequence.
		/// </summary>
		/// <param name="seq">unmodified sequence</param>
		/// <param name="myParam">ascore parameters</param>
		/// <param name="peptideMods">peptide modification position array</param>
		/// <returns></returns>
		private string GenerateFinalSequences(string seq, ParameterFileManager myParam, int[] peptideMods)
		{
			var sbFinalSeq = new System.Text.StringBuilder(seq.Length);

			for (int i = 0; i < seq.Length; i++)
			{
				if (i >= peptideMods.Length)
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
					foreach (Mod.DynamicModification dmod in myParam.DynamicMods)
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

		private double GetModMassTotal(string peptideSeq, List<Mod.DynamicModification> dynMods)
		{
			double modMass = 0;

			foreach (char t in peptideSeq)
			{
				if (!char.IsLetter(t))
				{
					foreach (Mod.DynamicModification m in dynMods)
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
		private List<double> GetCurrentComboTheoreticalIons(double mzmax, double mzmin,
			Dictionary<int, ChargeStateIons> mySpectra)
		{
			var myIons = new List<double>();
			foreach (ChargeStateIons csi in mySpectra.Values)
			{
				foreach (double ion in csi.BIons)
				{
					if (ion < mzmax && ion > mzmin)
					{
						myIons.Add(ion);
					}
				}
				foreach (double ion in csi.YIons)
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
			List<ExperimentalSpectraEntry> peakDepthSpectra,
			BinarySearchRange binarySearcher)
		{
			var matchedMZ = new List<double>();
			foreach (double mz in tempSpec)
			{
				int matchIndexStart;
				int matchIndexEnd;

				if (binarySearcher.FindValueRange(peakDepthSpectra, mz, tolerance, out matchIndexStart, out matchIndexEnd))
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
		protected List<double> GetSiteDeterminingIons(List<double> toGetDetermining, List<double> secondSpec)
		{
			var siteDetermined = new List<double>(toGetDetermining);
			foreach (double ion in secondSpec)
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
		private SortedList<int, int> GetSiteDict(int[] topPeptidePTMsites)
		{
			var siteInfo = new SortedList<int, int>();
			for (int n = 0; n < topPeptidePTMsites.Length; n++)
			{
				if (topPeptidePTMsites[n] > 0)
				{
					siteInfo.Add(n, topPeptidePTMsites[n]);
				}
			}
			return siteInfo;
		}

		/// <summary>
		/// Generate the position list for the particular sequence
		/// </summary>
		/// <param name="sequence"></param>
		/// <param name="modMixture"></param>
		/// <returns></returns>
		private List<int[]> GetMyPostionList(string sequence, Combinatorics.ModMixtureCombo modMixture)
		{
			var myPositionsList = new List<int[]>();
			foreach (List<int> mycom in modMixture.FinalCombos)
			{
				var myPositions = new int[sequence.Length];
				for (int i = 0; i < mycom.Count; i++)
				{
					myPositions[modMixture.AllSite[i]] = mycom[i];
				}
				myPositionsList.Add(myPositions);
			}
			return myPositionsList;
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
			Dictionary<int, ChargeStateIons> mySpectraMono = theoMono.GetTempSpectra(myPositions,
								  ascoreParameters.DynamicMods, MassType.Monoisotopic);
			Dictionary<int, ChargeStateIons> mySpectraAverage = null;
			if (ascoreParameters.FragmentMassTolerance <= 0.05)
			{
				mySpectraAverage = theoAve.GetTempSpectra(myPositions,
					ascoreParameters.DynamicMods, MassType.Average);
			}
			//Get ions within m/z range
			var mySpectra = new Dictionary<int, ChargeStateIons>();
			if (ascoreParameters.FragmentMassTolerance <= 0.05)
			{
				mySpectra.Add(1, mySpectraMono[1]);
				foreach (int charge in mySpectraAverage.Keys)
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

			List<double> myIons = GetCurrentComboTheoreticalIons(mzmax, mzmin, mySpectra);
			return myIons;
		}


		protected string LookupModInfoByID(int uniqueID, List<Mod.DynamicModification> dynamicMods)
		{
			string modInfo = string.Empty;

			foreach (Mod.DynamicModification m in dynamicMods)
			{
				if (m.UniqueID == uniqueID)
				{
					foreach (char site in m.PossibleModSites)
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
