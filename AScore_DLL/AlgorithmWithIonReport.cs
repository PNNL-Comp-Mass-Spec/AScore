//Joshua Aldrich

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using AScore_DLL.Managers;


namespace AScore_DLL
{
	public static class AlgorithmWithIonReport
	{
		private static readonly double[] ScoreWeights = { 0.5, 0.75, 1.0, 1.0, 1.0, 1.0, 0.75, 0.5, 0.25, 0.25 };


		#region Public Method
		/// <summary>
		/// Runs the all the tools necessary to perform an ascore run
		/// </summary>
		/// <param name="dtaFileName">dta file path</param>
		/// <param name="parameterFile">parameter file path</param>
		/// <param name="datasetFileName">dataset file path</param>
		/// <param name="outputFilePath">output file path</param>
		public static void AlgorithmRunChargeAve(string dtaFileName, string parameterFile, string datasetFileName, string outputFilePath)
		{

			//		IonWriter myIonWriter = new IonWriter(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(datasetFileName), "IonLog.txt"));

			System.Data.DataTable dt = Utilities.TextFileToDataTableAssignTypeString(datasetFileName, false);
			DtaManager dtaManager = new DtaManager(dtaFileName);
			ParameterFileManager ascoreParameters = new ParameterFileManager(parameterFile);

			//string datasetName = "Syne_Glyco-2_degly-S_10Jul11_Andromeda_11-06-19";
			//int scanNumber = 7315;
			//int scanCount = 1;
			//int chargeState = 3;
			//string sequence = "IVNDELESLGYGENLLNLSTINR";


			//Switch back to zero
			string datasetName = System.IO.Path.GetFileName(dtaFileName).Substring(0, System.IO.Path.GetFileName(dtaFileName).Length - 8);

			//adds columns to the datatable corresponding to ascore info
			dt.Columns.Add("BestSequence", typeof(string));
			dt.Columns.Add("PeptideScore", typeof(double));



			for (int i = 1; i < 4; i++)
			{
				dt.Columns.Add("AScore" + i, typeof(double));
				dt.Columns.Add("numSiteIonsMatched" + i, typeof(int));
				dt.Columns.Add("numSiteIonsPoss" + i, typeof(int));
				dt.Columns.Add("SecondSequence" + i, typeof(string));


				dt.Columns["AScore" + i].DefaultValue = -1;
				dt.Columns["numSiteIonsMatched" + i].DefaultValue = 0;
				dt.Columns["numSiteIonsPoss" + i].DefaultValue = 0;
				dt.Columns["SecondSequence" + i].DefaultValue = "---";
			}

			for (int k = 0; k < dt.Rows.Count; k++)
			{
				for (int p = 1; p < 4; p++)
				{
					dt.Rows[k]["AScore" + p] = -1;
					dt.Rows[k]["numSiteIonsMatched" + p] = 0;
					dt.Rows[k]["numSiteIonsPoss" + p] = 0;
					dt.Rows[k]["SecondSequence" + p] = "---";

				}
			}

			int totalRows = dt.Rows.Count;
			int rowsCounts = 0;


			//Where all the action happens
			for (int t = 0; t < totalRows; t++)
			{
				Console.WriteLine(rowsCounts++ + " / " + totalRows);


				//Use array of column names specific to other id forms
				int scanNumber = int.Parse((string)dt.Rows[t]["ScanNum"]);
				int scanCount = int.Parse((string)dt.Rows[t]["ScanCount"]);
				int chargeState = int.Parse((string)dt.Rows[t]["ChargeState"]);
				string peptideSeq = (string)dt.Rows[t]["Peptide"];

				string[] splittedPep = peptideSeq.Split('.');
				string front = splittedPep[0];
				string back = splittedPep[2];

				//TODO:what this does and change name of ascoreParameters
				string sequence = GetCleanSequence(peptideSeq, ref ascoreParameters);

				//Generate combinations for this sequence/mod seto
				//Dictionary of list
				//List<List<int>> mySites = GetSiteLocation(ascoreParameters.DynamicMods, sequence);
				//List<List<List<int>>> myCombos = GenerateCombosToCheck(mySites, ascoreParameters.DynamicMods);

				//Get experimental spectra
				ExperimentalSpectra expSpec = dtaManager.GetExperimentalSpectra(GetDtaFileName(datasetName, scanNumber,
					scanCount, chargeState));

				if (expSpec == null)
				{
					continue;
				}

				//I assume monoisotopic here, nobody uses average anymore.
				MolecularWeights.MassType = MassType.Monoisotopic;

				//Get precursor
				double precursorMZ = (expSpec.PrecursorMass +
					((expSpec.PrecursorChargeState - 1) *
					MolecularWeights.Hydrogen)) / chargeState;
				//Set the m/z range
				//Remove magic numbers parameterize
				double mzmax = 2000.0;
				double mzmin = precursorMZ * 0.28;
				if (ascoreParameters.FragmentType == FragmentType.CID)
				{
					mzmax = 2000.0;
					mzmin = precursorMZ * 0.28;
				}
				else
				{
					mzmax = 2000.0;
					mzmin = 50.0;
				}

				//initialize ascore variable storage
				List<double> vecAScore = new List<double>();
				List<int> vecNumSiteIons = new List<int>();
				List<int[]> AScorePeptide = new List<int[]>();
				List<int> siteDetermineMatched = new List<int>();

				//Generate all combination mixtures
				Combinatorics.ModMixtureCombo modMixture = new Combinatorics.ModMixtureCombo(ascoreParameters.DynamicMods, sequence);

				List<int[]> myPositionsList = new List<int[]>();
				foreach (List<int> mycom in modMixture.FinalCombos)
				{
					int[] myPositions = new int[sequence.Length];
					for (int i = 0; i < mycom.Count; i++)
					{
						myPositions[modMixture.AllSite[i]] = mycom[i];
					}
					myPositionsList.Add(myPositions);
				}

				//If I have more than 1 modifiable site proceed to calculation
				if (myPositionsList.Count > 1 && chargeState > 1)
				{
					List<List<double>> peptideScores = new List<List<double>>();
					List<List<double>> weightedScores = new List<List<double>>();


					TheoreticalSpectra theoMono = new TheoreticalSpectra(sequence, ascoreParameters, chargeState,
						new List<Mod.DynamicModification>(), MassType.Monoisotopic);
					TheoreticalSpectra theoAve = new TheoreticalSpectra(sequence, ascoreParameters, chargeState,
						new List<Mod.DynamicModification>(), MassType.Average);

					int modNumber = 0;
					foreach (int[] myPositions in myPositionsList)
					{

						//Generate spectra for a modification combination
						List<double> myIons = GetChargeList(ascoreParameters, mzmax, mzmin, theoMono, theoAve, myPositions);
						peptideScores.Add(new List<double>());
						weightedScores.Add(new List<double>());

						for (int peakDepth = 1; peakDepth < 11; ++peakDepth)
						{
							List<ExperimentalSpectraEntry> peakDepthSpectra = expSpec.GetPeakDepthSpectra(peakDepth);
							List<double> matchedIons = GetMatchedMZ(
								peakDepth, ascoreParameters.FragmentMassTolerance,
								myIons, peakDepthSpectra);

							//if (scanNumber == 19446)
							//{

							//    myIonWriter.MatchList(matchedIons);
							//}

							//Adjusted peptide score to score based on tolerance window.
							double score = PeptideScoresManager.GetPeptideScore(
								((double)peakDepth * ascoreParameters.FragmentMassTolerance * 2) / 100.0, myIons.Count, matchedIons.Count);

							// Check if there were any negative scores
							peptideScores[modNumber].Add(score);
							weightedScores[modNumber].Add(
								score * ScoreWeights[peakDepth - 1]);
						}
						modNumber++;
					}
					List<ValueIndexPair<double>> sortedSumScore = new List<ValueIndexPair<double>>();
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
					int[] topPeptidePTMsites =
						myPositionsList[sortedSumScore[0].Index];









					SortedList<int, int> siteInfo = GetSiteDict(topPeptidePTMsites);


					Dictionary<int, ChargeStateIons> bestDatatoWrite = new Dictionary<int, ChargeStateIons>();
					// Get the top sequence theoretical spectra
					List<double> topTheoIons = GetChargeList(ascoreParameters, mzmax, mzmin, theoMono, theoAve, topPeptidePTMsites, out bestDatatoWrite);

					List<char> modChars = new List<char>();
					foreach (Mod.DynamicModification m in ascoreParameters.DynamicMods)
					{
						modChars.Add(m.ModSymbol);
					}
					
					DataToExcelPractice.InfoForIons bestIonReport = new DataToExcelPractice.InfoForIons();
					
					bestIonReport.Sequence = GenerateFinalSequences(sequence, ascoreParameters, topPeptidePTMsites);
					bestIonReport.InitializeSequenceList(modChars.ToArray());
					bestIonReport.ChargeState = chargeState;
					bestIonReport.Depth = -1;
					foreach (int k in bestDatatoWrite.Keys)
					{
						bestIonReport.BionData.Add(k, new List<double>(bestDatatoWrite[k].BIons));
						bestIonReport.YionData.Add(k, new List<double>(bestDatatoWrite[k].YIons));
					}
					List<double> topIonIntensity = new List<double>();
					bestIonReport.MatchesForThisDepth = GetMatchedMZStoreIntensity(10, ascoreParameters.FragmentMassTolerance, topTheoIons,
						expSpec.GetPeakDepthSpectra(10), out topIonIntensity);
					bestIonReport.MatchedIonIntensity = new List<double>(topIonIntensity);
					bestIonReport.AScore = -1.0;
					bestIonReport.ScanNumber = scanNumber;
					




					List<DataToExcelPractice.InfoForIons> otherSites = new List<DataToExcelPractice.InfoForIons>();



					for (int indSite = 0; indSite < siteInfo.Count; ++indSite)
					{

						int secondPeptide = 0;
						for (secondPeptide = 0; secondPeptide < sortedSumScore.Count; ++secondPeptide)
						{
							SortedList<int, int> secondDict = GetSiteDict(myPositionsList[
								sortedSumScore[secondPeptide].Index]);
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
									AScorePeptide.Add(myPositionsList[sortedSumScore[secondPeptide].Index]);
									break;
								}
							}
							else
							{
								if (secondDict[siteInfo.Keys[indSite]] != siteInfo.Values[indSite])
								{
									AScorePeptide.Add(myPositionsList[sortedSumScore[secondPeptide].Index]);
									break;
								}
							}
						}
						if (secondPeptide == sortedSumScore.Count)
						{
							continue;
						}


						int[] secondTopPeptidePTMsites = myPositionsList[sortedSumScore[secondPeptide].Index];
						// Get the second best scoring spectra


						Dictionary<int, ChargeStateIons> secondIonWriter = new Dictionary<int, ChargeStateIons>();
						List<double> secondTopTheoIons = GetChargeList(ascoreParameters, mzmax, mzmin, theoMono, theoAve, secondTopPeptidePTMsites,
							out secondIonWriter);


						// Calculate the diff score between the top and second sites
						List<ValueIndexPair<double>> diffScore = new List<ValueIndexPair<double>>();
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


						DataToExcelPractice.InfoForIons secondBestIonReport = new DataToExcelPractice.InfoForIons();
						secondBestIonReport.Sequence = GenerateFinalSequences(sequence, ascoreParameters, secondTopPeptidePTMsites);
						secondBestIonReport.ChargeState = chargeState;
						foreach (int k in secondIonWriter.Keys)
						{
							secondBestIonReport.BionData.Add(k, new List<double>(secondIonWriter[k].BIons));
							secondBestIonReport.YionData.Add(k, new List<double>(secondIonWriter[k].YIons));
						}


						List<double> sTopIonIntensity = new List<double>();
						secondBestIonReport.MatchesForThisDepth = GetMatchedMZStoreIntensity(10, ascoreParameters.FragmentMassTolerance, secondTopTheoIons,
							expSpec.GetPeakDepthSpectra(10), out sTopIonIntensity);
						secondBestIonReport.MatchedIonIntensity = new List<double>(sTopIonIntensity);
						secondBestIonReport.Depth = peakDepthForAScore;
						secondBestIonReport.InitializeSequenceList(modChars.ToArray());
						secondBestIonReport.ScanNumber = scanNumber;

						List<double> siteIons1 = GetSiteDeterminingIons(topTheoIons, secondTopTheoIons);
						List<double> siteIons2 = GetSiteDeterminingIons(secondTopTheoIons, topTheoIons);
						


						List<ExperimentalSpectraEntry> peakDepthSpectraFinal = expSpec.GetPeakDepthSpectra(peakDepthForAScore);

						List<double> matched1 = GetMatchedMZ(peakDepthForAScore,
							ascoreParameters.FragmentMassTolerance, siteIons1, peakDepthSpectraFinal);
						List<double> matched2 = GetMatchedMZ(peakDepthForAScore,
							ascoreParameters.FragmentMassTolerance, siteIons2, peakDepthSpectraFinal);

						int bestDterminingCount = matched1.Count;

						double a1 = PeptideScoresManager.GetPeptideScore(((double)peakDepthForAScore * ascoreParameters.FragmentMassTolerance * 2) / 100,
							siteIons1.Count, matched1.Count);
						double a2 = PeptideScoresManager.GetPeptideScore(((double)peakDepthForAScore * ascoreParameters.FragmentMassTolerance * 2) / 100,
							siteIons2.Count, matched2.Count);

						bestIonReport.AddToSiteDeterminingIons(matched1);
						secondBestIonReport.SiteDeterminingIons = new List<double>(matched2);
						secondBestIonReport.AScore = Math.Abs(a1 - a2);

						otherSites.Add(secondBestIonReport);

						// Add the results to the list
						vecAScore.Add(Math.Abs(a1 - a2));
						vecNumSiteIons.Add(siteIons1.Count);
						siteDetermineMatched.Add(bestDterminingCount);
					}

					DataToExcelPractice.PeptideIonGroup myIonGroup = new DataToExcelPractice.PeptideIonGroup(bestIonReport, otherSites);

					DataToExcelPractice.WriteIonsToExcel myExcel = new DataToExcelPractice.WriteIonsToExcel();
					myExcel.PrintIonsToExcel(myIonGroup, System.IO.Path.GetDirectoryName(outputFilePath));

					//Put scores into our table
					dt.Rows[t]["BestSequence"] = front + "." + GenerateFinalSequences(sequence, ascoreParameters, topPeptidePTMsites) + "." + back;
					dt.Rows[t]["PeptideScore"] = "" + topPeptideScore;
					for (int i = 0; i < vecAScore.Count && i < 3; i++)
					{
						dt.Rows[t]["AScore" + (i + 1)] = "" + vecAScore[i];
						dt.Rows[t]["numSiteIonsPoss" + (i + 1)] = vecNumSiteIons[i];
						dt.Rows[t]["numSiteIonsMatched" + (i + 1)] = "" + siteDetermineMatched[i];
						dt.Rows[t]["SecondSequence" + (i + 1)] = front + "." + GenerateFinalSequences(sequence, ascoreParameters, AScorePeptide[i]) + "." + back;
					}

				}
				else if (chargeState > 1)
				{
					List<double> weightedScores = new List<double>();
					TheoreticalSpectra theo = new TheoreticalSpectra(sequence, ascoreParameters, chargeState, new List<Mod.DynamicModification>(), MassType.Monoisotopic);
					foreach (int[] myPositions in myPositionsList)
					{


						Dictionary<int, ChargeStateIons> mySpectra = theo.GetTempSpectra(myPositions, ascoreParameters.DynamicMods, MassType.Monoisotopic);

						List<double> myIons = GetCurrentComboTheoreticalIons(mzmax, mzmin, mySpectra);

						for (int peakDepth = 1; peakDepth < 11; ++peakDepth)
						{
							List<ExperimentalSpectraEntry> peakDepthSpectra = expSpec.GetPeakDepthSpectra(peakDepth);

							List<double> matchedIons = GetMatchedMZ(
								peakDepth, ascoreParameters.FragmentMassTolerance,
								myIons, peakDepthSpectra);

							//Adjusted peptide score to score based on tolerance window.
							double score = PeptideScoresManager.GetPeptideScore(
								((double)peakDepth * ascoreParameters.FragmentMassTolerance * 2) / 100.0, myIons.Count, matchedIons.Count);

							// Check if there were any negative scores
							weightedScores.Add(
								score * ScoreWeights[peakDepth - 1]);
						}
					}
					double pScore = 0.0;
					for (int depth = 0; depth < weightedScores.Count; ++depth)
					{
						pScore += weightedScores[depth];
					}


					//Nothing to calculate
					dt.Rows[t]["BestSequence"] = peptideSeq;
					dt.Rows[t]["PeptideScore"] = "" + pScore;
					if (myPositionsList[0].Count(i => i > 0) > 0)
					{
						dt.Rows[t]["AScore1"] = "1000";
					}
					else
					{
						dt.Rows[t]["AScore1"] = "-1";
					}
					dt.Rows[t]["numSiteIonsMatched1"] = 0;
					dt.Rows[t]["numSiteIonsPoss1"] = 0;
					dt.Rows[t]["SecondSequence1"] = "--";
				}
				else
				{
					dt.Rows[t]["BestSequence"] = peptideSeq;
					dt.Rows[t]["PeptideScore"] = "0.0";
					dt.Rows[t]["AScore1"] = "-1";
					dt.Rows[t]["numSiteIonsMatched1"] = 0;
					dt.Rows[t]["numSiteIonsPoss1"] = 0;
					dt.Rows[t]["SecondSequence1"] = "--";

				}

			}


			Utilities.WriteDataTableToText(dt, outputFilePath);

		}



		private static SortedList<int, int> GetSiteDict(int[] topPeptidePTMsites)
		{
			SortedList<int, int> siteInfo = new SortedList<int, int>();
			for (int n = 0; n < topPeptidePTMsites.Length; n++)
			{
				if (topPeptidePTMsites[n] > 0)
				{
					siteInfo.Add(n, topPeptidePTMsites[n]);
				}
			}
			return siteInfo;
		}
		private static List<double> GetChargeList(ParameterFileManager ascoreParameters, double mzmax, double mzmin, TheoreticalSpectra theoMono, TheoreticalSpectra theoAve, int[] myPositions)
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
			Dictionary<int, ChargeStateIons> mySpectra = new Dictionary<int, ChargeStateIons>();
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


		private static List<double> GetChargeList(ParameterFileManager ascoreParameters, double mzmax, double mzmin, TheoreticalSpectra theoMono, 
			TheoreticalSpectra theoAve, int[] myPositions, out Dictionary<int, ChargeStateIons> toWrite)
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
			Dictionary<int, ChargeStateIons> mySpectra = new Dictionary<int, ChargeStateIons>();
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
			toWrite = mySpectra;
			List<double> myIons = GetCurrentComboTheoreticalIons(mzmax, mzmin, mySpectra);
			return myIons;
		}


		/// <summary>
		/// Runs the all the tools necessary to perform an ascore run
		/// </summary>
		/// <param name="dtaFileName">dta file path</param>
		/// <param name="parameterFile">parameter file path</param>
		/// <param name="datasetFileName">dataset file path</param>
		/// <param name="outputFilePath">output file path</param>
		public static void AlgorithmRun(string dtaFileName, string parameterFile, string datasetFileName, string outputFilePath)
		{

			//		IonWriter myIonWriter = new IonWriter(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(datasetFileName), "IonLog.txt"));

			System.Data.DataTable dt = Utilities.TextFileToDataTableAssignTypeString(datasetFileName, false);
			DtaManager dtaManager = new DtaManager(dtaFileName);
			ParameterFileManager ascoreParameters = new ParameterFileManager(parameterFile);

			//string datasetName = "Syne_Glyco-2_degly-S_10Jul11_Andromeda_11-06-19";
			//int scanNumber = 7315;
			//int scanCount = 1;
			//int chargeState = 3;
			//string sequence = "IVNDELESLGYGENLLNLSTINR";

			string datasetName = System.IO.Path.GetFileName(dtaFileName).Substring(0, System.IO.Path.GetFileName(dtaFileName).Length - 8);

			//adds columns to the datatable corresponding to ascore info
			dt.Columns.Add("BestSequence", typeof(string));
			dt.Columns.Add("PeptideScore", typeof(double));
			dt.Columns.Add("AScore", typeof(double));
			dt.Columns.Add("numSiteIons", typeof(int));
			dt.Columns.Add("SecondSequence", typeof(string));
			int totalRows = dt.Rows.Count;
			int rowsCounts = 0;


			//Where all the action happens
			for (int t = 0; t < totalRows; t++)
			{
				Console.WriteLine(rowsCounts++ + " / " + totalRows);


				//Use array of column names specific to other id forms
				int scanNumber = int.Parse((string)dt.Rows[t]["ScanNum"]);
				int scanCount = int.Parse((string)dt.Rows[t]["ScanCount"]);
				int chargeState = int.Parse((string)dt.Rows[t]["ChargeState"]);
				string peptideSeq = (string)dt.Rows[t]["Peptide"];

				string[] splittedPep = peptideSeq.Split('.');
				string front = splittedPep[0];
				string back = splittedPep[2];

				//TODO:what this does and change name of ascoreParameters
				string sequence = GetCleanSequence(peptideSeq, ref ascoreParameters);

				//Generate combinations for this sequence/mod seto
				//Dictionary of list Now part of an object
				//List<List<int>> mySites = GetSiteLocation(ascoreParameters.DynamicMods, sequence);
				//List<List<List<int>>> myCombos = GenerateCombosToCheck(mySites, ascoreParameters.DynamicMods);

				//Get experimental spectra
				ExperimentalSpectra expSpec = dtaManager.GetExperimentalSpectra(GetDtaFileName(datasetName, scanNumber,
					scanCount, chargeState));

				//I assume monoisotopic here, nobody uses average anymore.
				MolecularWeights.MassType = MassType.Monoisotopic;

				//Get precursor
				double precursorMZ = (expSpec.PrecursorMass +
					((expSpec.PrecursorChargeState - 1) *
					MolecularWeights.Hydrogen)) / chargeState;
				//Set the m/z range
				//Remove magic numbers parameterize
				double mzmax = 2000.0;
				double mzmin = precursorMZ * 0.28;
				if (ascoreParameters.FragmentType == FragmentType.CID)
				{
					mzmax = 2000.0;
					mzmin = precursorMZ * 0.28;
				}
				else
				{
					mzmax = 2000.0;
					mzmin = 50.0;
				}

				//initialize ascore variable storage
				List<double> vecAScore = new List<double>();
				List<int> vecNumSiteIons = new List<int>();
				List<List<int>> AScorePeptide = new List<List<int>>();

				//Generate all combination mixtures
				Combinatorics.ModMixtureCombo modMixture = new Combinatorics.ModMixtureCombo(ascoreParameters.DynamicMods, sequence);

				List<int[]> myPositionsList = new List<int[]>();
				foreach (List<int> mycom in modMixture.FinalCombos)
				{
					int[] myPositions = new int[sequence.Length];
					for (int i = 0; i < mycom.Count; i++)
					{
						myPositions[modMixture.AllSite[i]] = mycom[i];
					}
					myPositionsList.Add(myPositions);
				}

				//If I have more than 1 modifiable site proceed to calculation
				if (myPositionsList.Count > 1)
				{
					List<List<double>> peptideScores = new List<List<double>>();
					List<List<double>> weightedScores = new List<List<double>>();

					TheoreticalSpectra theo = new TheoreticalSpectra(sequence, ascoreParameters, chargeState,
						new List<Mod.DynamicModification>(), MassType.Monoisotopic);

					int modNumber = 0;
					foreach (int[] myPositions in myPositionsList)
					{

						//Generate spectra for a modification combination
						Dictionary<int, ChargeStateIons> mySpectra = theo.GetTempSpectra(myPositions,
							ascoreParameters.DynamicMods, MassType.Monoisotopic);
						//Get ions within m/z range
						List<double> myIons = GetCurrentComboTheoreticalIons(mzmax, mzmin, mySpectra);
						peptideScores.Add(new List<double>());
						weightedScores.Add(new List<double>());

						for (int peakDepth = 1; peakDepth < 11; ++peakDepth)
						{
							List<ExperimentalSpectraEntry> peakDepthSpectra = expSpec.GetPeakDepthSpectra(peakDepth);
							List<double> matchedIons = GetMatchedMZ(
								peakDepth, ascoreParameters.FragmentMassTolerance,
								myIons, peakDepthSpectra);

							//if (scanNumber == 19446)
							//{

							//    myIonWriter.MatchList(matchedIons);
							//}

							//Adjusted peptide score to score based on tolerance window.
							double score = PeptideScoresManager.GetPeptideScore(
								((double)peakDepth * ascoreParameters.FragmentMassTolerance * 2) / 100.0, myIons.Count, matchedIons.Count);

							// Check if there were any negative scores
							peptideScores[modNumber].Add(score);
							weightedScores[modNumber].Add(
								score * ScoreWeights[peakDepth - 1]);
						}
						modNumber++;
					}
					List<ValueIndexPair<double>> sortedSumScore = new List<ValueIndexPair<double>>();
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
					int[] topPeptidePTMsites =
						myPositionsList[sortedSumScore[0].Index];

					// Get the top sequence theoretical spectra
					Dictionary<int, ChargeStateIons> topSpectra = theo.GetTempSpectra(topPeptidePTMsites,
						ascoreParameters.DynamicMods, MassType.Monoisotopic);

					List<double> topTheoIons = GetCurrentComboTheoreticalIons(mzmax, mzmin, topSpectra);

					int secondPeptide = 1;


					int[] secondTopPeptidePTMsites = myPositionsList[sortedSumScore[secondPeptide].Index];
					// Get the second best scoring spectra
					Dictionary<int, ChargeStateIons> secondTopSpectra = theo.GetTempSpectra(
						secondTopPeptidePTMsites, ascoreParameters.DynamicMods, MassType.Monoisotopic);

					List<double> secondTopTheoIons = GetCurrentComboTheoreticalIons(mzmax, mzmin, secondTopSpectra);


					// Calculate the diff score between the top and second sites
					List<ValueIndexPair<double>> diffScore = new List<ValueIndexPair<double>>();
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

					int bestDterminingCount = GetMatchedMZ(peakDepthForAScore,
						ascoreParameters.FragmentMassTolerance, siteIons1, peakDepthSpectraFinal).Count;

					double a1 = PeptideScoresManager.GetPeptideScore(
						((double)peakDepthForAScore * ascoreParameters.FragmentMassTolerance * 2) / 100,
						siteIons1.Count, bestDterminingCount);
					double a2 = PeptideScoresManager.GetPeptideScore(
						((double)peakDepthForAScore * ascoreParameters.FragmentMassTolerance * 2) / 100,
						siteIons2.Count, GetMatchedMZ(peakDepthForAScore,
						ascoreParameters.FragmentMassTolerance, siteIons2, peakDepthSpectraFinal).Count);

					// Add the results to the list
					vecAScore.Add(Math.Abs(a1 - a2));
					vecNumSiteIons.Add(siteIons1.Count);


					//Put scores into our table
					dt.Rows[t]["BestSequence"] = front + "." + GenerateFinalSequences(sequence,
						ascoreParameters, topPeptidePTMsites) + "." + back;
					dt.Rows[t]["PeptideScore"] = "" + topPeptideScore;
					dt.Rows[t]["AScore"] = "" + vecAScore[0];
					dt.Rows[t]["numSiteIons"] = "" + bestDterminingCount;
					dt.Rows[t]["SecondSequence"] = front + "." + GenerateFinalSequences(sequence,
						ascoreParameters, secondTopPeptidePTMsites) + "." + back;

				}
				else
				{
					List<double> weightedScores = new List<double>();
					TheoreticalSpectra theo = new TheoreticalSpectra(sequence, ascoreParameters, chargeState,
						new List<Mod.DynamicModification>(), MassType.Monoisotopic);
					foreach (int[] myPositions in myPositionsList)
					{


						Dictionary<int, ChargeStateIons> mySpectra = theo.GetTempSpectra(myPositions,
							ascoreParameters.DynamicMods, MassType.Monoisotopic);

						List<double> myIons = GetCurrentComboTheoreticalIons(mzmax, mzmin, mySpectra);

						for (int peakDepth = 1; peakDepth < 11; ++peakDepth)
						{
							List<ExperimentalSpectraEntry> peakDepthSpectra = expSpec.GetPeakDepthSpectra(peakDepth);

							List<double> matchedIons = GetMatchedMZ(
								peakDepth, ascoreParameters.FragmentMassTolerance,
								myIons, peakDepthSpectra);

							//Adjusted peptide score to score based on tolerance window.
							double score = PeptideScoresManager.GetPeptideScore(
								((double)peakDepth * ascoreParameters.FragmentMassTolerance * 2) / 100.0, myIons.Count, matchedIons.Count);

							// Check if there were any negative scores
							weightedScores.Add(
								score * ScoreWeights[peakDepth - 1]);
						}
					}
					double pScore = 0.0;
					for (int depth = 0; depth < weightedScores.Count; ++depth)
					{
						pScore += weightedScores[depth];
					}


					//Nothing to calculate
					dt.Rows[t]["BestSequence"] = peptideSeq;
					dt.Rows[t]["PeptideScore"] = "" + pScore;
					if (myPositionsList[0].Count(i => i > 0) > 0)
					{
						dt.Rows[t]["AScore"] = "1000";
					}
					else
					{
						dt.Rows[t]["AScore"] = "-1";
					}
					dt.Rows[t]["numSiteIons"] = "0";
					dt.Rows[t]["SecondSequence"] = "---";
				}


			}

			Utilities.WriteDataTableToText(dt, outputFilePath);

		}

		#endregion

		#region Private Methods
		/// <summary>
		/// Gets a clean sequence intitializes dynamic modifications
		/// </summary>
		/// <param name="seq">input protein sequence including mod characters</param>
		/// <param name="ascoreParameterss">ascore parameters reference</param>
		/// <returns>protein sequence without mods as well as changing ascoreParameterss</returns>
		private static string GetCleanSequence(string seq, ref ParameterFileManager ascoreParameterss)
		{
			seq = seq.Split('.')[1];
			foreach (Mod.DynamicModification dmod in ascoreParameterss.DynamicMods)
			{
				string s = "";
				//xml hates sequest
				if (dmod.ModSymbol == '*')
				{
					s = @"\*";
				}
				else if (dmod.ModSymbol == '^')
				{
					s = @"\^";
				}
				else
				{
					s = dmod.ModSymbol.ToString();
				}
				dmod.Count = Regex.Matches(seq, s).Count;
				seq = seq.Replace(dmod.ModSymbol.ToString(), string.Empty);
			}
			return seq;
		}


		/// <summary>
		/// Generates a sequence based on final best peptide sequence.
		/// </summary>
		/// <param name="seq">unmodified sequence</param>
		/// <param name="myParam">ascore parameters</param>
		/// <param name="peptides">peptide modification position array</param>
		/// <returns></returns>
		private static string GenerateFinalSequences(string seq, ParameterFileManager myParam, int[] peptides)
		{
			string finalSeq = "";
			char[] c = seq.ToCharArray();
			for (int i = 0; i < c.Length; i++)
			{
				if (peptides[i] == 0)
				{
					finalSeq += "" + c[i];
				}
				else
				{
					foreach (Mod.DynamicModification dmod in myParam.DynamicMods)
					{
						if (peptides[i] == dmod.UniqueID)
						{
							finalSeq += "" + c[i] + "" + dmod.ModSymbol;
						}
					}
				}
			}
			return finalSeq;

		}

		/// <summary>
		/// Generates the current modification set of theoretical ions filtered by the mz range
		/// </summary>
		/// <param name="mzmax">max m/z</param>
		/// <param name="mzmin">min m/z</param>
		/// <param name="mySpectra">dictionary of theoretical ions organized by charge</param>
		/// <returns>list of theoretical ions</returns>
		private static List<double> GetCurrentComboTheoreticalIons(double mzmax, double mzmin,
			Dictionary<int, ChargeStateIons> mySpectra)
		{
			List<double> myIons = new List<double>();
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
		/// <param name="peakDepthSpectra">experimental ions</param>
		/// <returns></returns>
		private static List<double> GetMatchedMZ(int peakDepth,
			double tolerance, List<double> tempSpec,
			List<ExperimentalSpectraEntry> peakDepthSpectra)
		{
			List<double> matchedMZ = new List<double>();
			foreach (double mz in tempSpec)
			{
				foreach (ExperimentalSpectraEntry entry in peakDepthSpectra)
				{
					if (Math.Abs(entry.Value1 - mz) <= tolerance)
					{
						matchedMZ.Add(mz);
						break;
					}
				}
			}
			return matchedMZ;
		}

		/// </summary>
		/// <param name="peakDepth">number of peaks per 100m/z range</param>
		/// <param name="tolerance">width or window for matching</param>
		/// <param name="tempSpec">theoretical ions</param>
		/// <param name="peakDepthSpectra">experimental ions</param>
		/// <returns></returns>
		private static List<double> GetMatchedMZStoreIntensity(int peakDepth,
			double tolerance, List<double> tempSpec,
			List<ExperimentalSpectraEntry> peakDepthSpectra, out List<double> intensity)
		{
			intensity = new List<double>();
			List<double> matchedMZ = new List<double>();
			foreach (double mz in tempSpec)
			{
				foreach (ExperimentalSpectraEntry entry in peakDepthSpectra)
				{
					if (Math.Abs(entry.Value1 - mz) <= tolerance)
					{
						matchedMZ.Add(mz);
						intensity.Add(entry.Value2);
						break;
					}
				}
			}
			return matchedMZ;
		}


		//Gets a spectra entry from the dta file
		public static string GetDtaFileName(string datasetName, int scanNumber, int scanCount, int chargeState)
		{
			return string.Format("{0}.{1}.{2}.{3}.dta",
				datasetName, scanNumber,
				scanNumber + scanCount - 1, chargeState);
		}



		/// <summary>
		/// Generates the site determining ions by comparing ions of tope two spectra and removing overlapping ions
		/// </summary>
		/// <param name="toGetDetermining">list to get unique from</param>
		/// <param name="secondSpec">list which contains overlap to remove from first list</param>
		/// <returns>list of values unique to the toGetDetermining list</returns>
		public static List<double> GetSiteDeterminingIons(List<double> toGetDetermining, List<double> secondSpec)
		{
			List<double> siteDetermined = new List<double>(toGetDetermining);
			foreach (double ion in secondSpec)
			{
				if (siteDetermined.Contains(ion))
				{
					siteDetermined.Remove(ion);
				}
			}
			return siteDetermined;
		}
		#endregion
	}
}
