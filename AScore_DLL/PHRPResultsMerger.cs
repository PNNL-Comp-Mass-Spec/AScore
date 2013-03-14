using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AScore_DLL.Managers.DatasetManagers;
using PHRPReader;
using PNNLOmics.Utilities;

namespace AScore_DLL
{
	/// <summary>
	/// Merges AScore results with an existing PHRP-compatible tab-delimited text file
	/// </summary>
	public class PHRPResultsMerger : MessageEventBase
	{
		protected string m_MergedFilePath = string.Empty;
		protected PHRPReader.clsPHRPReader mPHRPReader;

		protected struct AScoreResultsType
		{
			public string BestSequence;			// New peptide sequence
			public double PeptideScore;

			// The key in this dictionary is the ModInfo name; the value is the AScore value
			public Dictionary<string, double> AScoreByMod;

			public void Clear()
			{
				BestSequence = string.Empty;
				PeptideScore = 0;
				AScoreByMod = new Dictionary<string, double>();
			}
		}

		#region "Properties"
		public string MergedFilePath
		{
			get
			{
				if (string.IsNullOrEmpty(m_MergedFilePath))
					return string.Empty;
				else
					return m_MergedFilePath;
			}
		}
		#endregion

		public bool MergeResults(string phrpDataFilePath, string ascoreResultsFilePath)
		{
			return MergeResults(phrpDataFilePath, ascoreResultsFilePath, string.Empty);
		}

		public bool MergeResults(string phrpDataFilePath, string ascoreResultsFilePath, string mergedPhrpDataFileName)
		{
			bool success;

			System.IO.FileInfo fiInputFile;
			System.IO.FileInfo fiAScoreResultsFile;
			System.IO.FileInfo fiOutputFilePath;

			try
			{
				fiInputFile = new System.IO.FileInfo(phrpDataFilePath);
				if (!fiInputFile.Exists)
				{
					ReportError("PHRP Data File not found: " + fiInputFile.FullName);
					return false;
				}

				fiAScoreResultsFile = new System.IO.FileInfo(ascoreResultsFilePath);
				if (!fiAScoreResultsFile.Exists)
				{
					ReportError("AScore results file not found: " + fiAScoreResultsFile.FullName);
					return false;
				}

				if (string.IsNullOrEmpty(mergedPhrpDataFileName))
				{
					// Auto-define mergedPhrpDataFileName
					mergedPhrpDataFileName = System.IO.Path.GetFileNameWithoutExtension(fiInputFile.Name) + "_WithAScore.txt";
				}

				m_MergedFilePath = System.IO.Path.Combine(fiAScoreResultsFile.Directory.FullName, mergedPhrpDataFileName);
				fiOutputFilePath = new System.IO.FileInfo(m_MergedFilePath);

				// Cache the AScore results in memory
				Dictionary<string, AScoreResultsType> cachedAscoreResults = new Dictionary<string, AScoreResultsType>();

				success = CacheAScoreResults(ascoreResultsFilePath, cachedAscoreResults);
				if (!success)
					return false;

				success = MakeUpdatedPHRPFile(fiInputFile, fiOutputFilePath, cachedAscoreResults);

			}
			catch (Exception ex)
			{
				ReportError("Error in MergeResults: " + ex.Message);
				return false;
			}

			return true;
		}

		#region "Class functions"

		private bool CacheAScoreResults(string ascoreResultsFilePath, Dictionary<string, AScoreResultsType> cachedAscoreResults)
		{
			string lineIn;
			bool headersParsed = false;
			SortedDictionary<string, int> columnHeaders = new SortedDictionary<string, int>(StringComparer.CurrentCultureIgnoreCase);

			try
			{

				if (!System.IO.File.Exists(ascoreResultsFilePath))
				{
					ReportError("File not found: " + ascoreResultsFilePath);
					return false;
				}

				// Define the default column mapping
				columnHeaders.Add(DatasetManager.RESULTS_COL_JOB, 0);
				columnHeaders.Add(DatasetManager.RESULTS_COL_SCAN, 1);
				columnHeaders.Add(DatasetManager.RESULTS_COL_ORIGINALSEQUENCE, 2);
				columnHeaders.Add(DatasetManager.RESULTS_COL_BESTSEQUENCE, 3);
				columnHeaders.Add(DatasetManager.RESULTS_COL_PEPTIDESCORE, 4);
				columnHeaders.Add(DatasetManager.RESULTS_COL_ASCORE, 5);
				columnHeaders.Add(DatasetManager.RESULTS_COL_NUMSITEIONSPOSS, 6);
				columnHeaders.Add(DatasetManager.RESULTS_COL_NUMSITEIONSMATCHED, 7);
				columnHeaders.Add(DatasetManager.RESULTS_COL_SECONDSEQUENCE, 8);
				columnHeaders.Add(DatasetManager.RESULTS_COL_MODINFO, 9);

				using (System.IO.StreamReader srInFile = new System.IO.StreamReader(new System.IO.FileStream(ascoreResultsFilePath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read)))
				{
					while (srInFile.Peek() > -1)
					{
						lineIn = srInFile.ReadLine();
						if (string.IsNullOrEmpty(lineIn))
							continue;

						string[] splitLine = lineIn.Split('\t');

						if (!headersParsed)
						{
							clsPHRPReader.ParseColumnHeaders(splitLine, ref columnHeaders);
							headersParsed = true;
							continue;
						}

						int scanNumber = clsPHRPReader.LookupColumnValue(ref splitLine, DatasetManager.RESULTS_COL_SCAN, ref columnHeaders, -1);
						string originalPeptide = clsPHRPReader.LookupColumnValue(ref splitLine, DatasetManager.RESULTS_COL_ORIGINALSEQUENCE, ref columnHeaders, string.Empty);
						string bestSequence = clsPHRPReader.LookupColumnValue(ref splitLine, DatasetManager.RESULTS_COL_BESTSEQUENCE, ref columnHeaders, string.Empty);
						double peptideScore = clsPHRPReader.LookupColumnValue(ref splitLine, DatasetManager.RESULTS_COL_PEPTIDESCORE, ref columnHeaders, 0.0);
						double ascoreValue = clsPHRPReader.LookupColumnValue(ref splitLine, DatasetManager.RESULTS_COL_ASCORE, ref columnHeaders, 0.0);
						//int numSiteIonsPossible = clsPHRPReader.LookupColumnValue(ref splitLine, DatasetManager.RESULTS_COL_NUMSITEIONSPOSS, ref columnHeaders, 0);
						//int numSitIonsMatched = clsPHRPReader.LookupColumnValue(ref splitLine, DatasetManager.RESULTS_COL_NUMSITEIONSMATCHED, ref columnHeaders, 0);
						//string secondSequence = clsPHRPReader.LookupColumnValue(ref splitLine, DatasetManager.RESULTS_COL_SECONDSEQUENCE, ref columnHeaders, string.Empty);
						string modInfo = clsPHRPReader.LookupColumnValue(ref splitLine, DatasetManager.RESULTS_COL_MODINFO, ref columnHeaders, string.Empty);

						string scanPeptideKey = ConstructScanPeptideKey(scanNumber, originalPeptide);
						AScoreResultsType ascoreResult;

						if (cachedAscoreResults.TryGetValue(scanPeptideKey, out ascoreResult))
						{
							if (!ascoreResult.AScoreByMod.ContainsKey(modInfo))
							{
								ascoreResult.AScoreByMod.Add(modInfo, ascoreValue);
							}
						}
						else
						{
							ascoreResult = new AScoreResultsType();
							ascoreResult.Clear();

							ascoreResult.BestSequence = string.Copy(bestSequence);
							ascoreResult.PeptideScore = peptideScore;
							ascoreResult.AScoreByMod.Add(modInfo, ascoreValue);

							cachedAscoreResults.Add(scanPeptideKey, ascoreResult);
						}
					}
				}

			}
			catch (Exception ex)
			{
				ReportError("Error in CacheAScoreResults: " + ex.Message);
				return false;
			}

			return true;
		}

		protected string ConstructScanPeptideKey(int scanNumber, string peptideSequence)
		{
			return scanNumber.ToString() + "_" + peptideSequence;
		}

		protected SortedSet<string> DetermineModInfoNames(Dictionary<string, AScoreResultsType> cachedAscoreResults)
		{
			SortedSet<string> modInfoNames = new SortedSet<string>();

			foreach (KeyValuePair<string, AScoreResultsType> ascoreResult in cachedAscoreResults)
			{
				foreach (KeyValuePair<string, double> modInfoEntry in ascoreResult.Value.AScoreByMod)
				{
					// Unmodified peptides will have a ModInfo key of "-"
					// Skip these entries 
					if (modInfoEntry.Key != AScore_DLL.Algorithm.MODINFO_NO_MODIFIED_RESIDUES)
					{
						if (!modInfoNames.Contains(modInfoEntry.Key))
							modInfoNames.Add(modInfoEntry.Key);
					}
				}
				
			}

			return modInfoNames;
		}

		private bool MakeUpdatedPHRPFile(System.IO.FileInfo fiInputFile, System.IO.FileInfo fiOutputFilePath, Dictionary<string, AScoreResultsType> cachedAscoreResults)
		{

			try
			{
				PHRPReader.clsPHRPReader.ePeptideHitResultType ePeptideHitResultType;
				ePeptideHitResultType = PHRPReader.clsPHRPReader.AutoDetermineResultType(fiInputFile.FullName);

				if (ePeptideHitResultType == PHRPReader.clsPHRPReader.ePeptideHitResultType.Unknown)
				{
					ReportError("Error: Could not determine the format of the PHRP data file: " + fiInputFile.FullName);
					return false;
				}

				// Read the header line from the PHRP file
				string outputHeaderLine = ReadHeaderLine(fiInputFile.FullName);

				SortedSet<string> modInfoNames = DetermineModInfoNames(cachedAscoreResults);

				// Open the data file and read the data
				mPHRPReader = new PHRPReader.clsPHRPReader(fiInputFile.FullName, PHRPReader.clsPHRPReader.ePeptideHitResultType.Unknown, false, false, false);
				mPHRPReader.EchoMessagesToConsole = false;
				mPHRPReader.SkipDuplicatePSMs = false;

				if (!mPHRPReader.CanRead)
				{
					ReportError("Aborting since PHRPReader is not ready: " + mPHRPReader.ErrorMessage);
					return false;
				}

				// Attach the events
				mPHRPReader.ErrorEvent += new clsPHRPReader.ErrorEventEventHandler(mPHRPReader_ErrorEvent);
				mPHRPReader.WarningEvent += new clsPHRPReader.WarningEventEventHandler(mPHRPReader_WarningEvent);
				mPHRPReader.MessageEvent += new clsPHRPReader.MessageEventEventHandler(mPHRPReader_MessageEvent);

				// Create the output file
				using (System.IO.StreamWriter swOutFile = new System.IO.StreamWriter(new System.IO.FileStream(fiOutputFilePath.FullName, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.Read)))
				{
					System.Text.StringBuilder outLine = new System.Text.StringBuilder();

					// Write the header line
					outLine.Append(outputHeaderLine);
					outLine.Append("\t" + DatasetManager.RESULTS_COL_PEPTIDESCORE);
					outLine.Append("\t" + "Modified_Residues");

					foreach (string modInfoName in modInfoNames)
					{
						outLine.Append("\t" + modInfoName);
					}
					swOutFile.WriteLine(outLine);

					while (mPHRPReader.MoveNext())
					{
						PHRPReader.clsPSM oPSM = mPHRPReader.CurrentPSM;

						string scanPeptideKey = ConstructScanPeptideKey(oPSM.ScanNumber, oPSM.Peptide);
						AScoreResultsType ascoreResult;

						if (!cachedAscoreResults.TryGetValue(scanPeptideKey, out ascoreResult))
						{
							Console.WriteLine("  Skipping PHRP result without AScore result: " + scanPeptideKey);
							continue;
						}

						// Replace the original peptide with the "best" peptide
						string dataLineUpdated = ReplaceFirst(oPSM.DataLineText, oPSM.Peptide, ascoreResult.BestSequence);

						outLine.Clear();
						outLine.Append(dataLineUpdated);

						outLine.Append("\t" + MathUtilities.ValueToString(ascoreResult.PeptideScore));
						
						// Count the number of modInfo entries that are not "-"
						int modTypeCount = (from item in ascoreResult.AScoreByMod where item.Key != AScore_DLL.Algorithm.MODINFO_NO_MODIFIED_RESIDUES select item.Key).Count();
						outLine.Append("\t" + modTypeCount);

						foreach (string modInfoName in modInfoNames)
						{
							bool modInfoMatch = false;
							foreach (KeyValuePair<string, double> modInfoEntry in ascoreResult.AScoreByMod)
							{
								if (modInfoName == modInfoEntry.Key)
								{
									outLine.Append("\t" + MathUtilities.ValueToString(modInfoEntry.Value));
									modInfoMatch = true;
									break;
								}
							}
							if (!modInfoMatch)
								outLine.Append("\t");
						}

						swOutFile.WriteLine(outLine);

						//UpdateProgress(mPHRPReader.PercentComplete);					
					}
				}
			}
			catch (Exception ex)
			{
				ReportError("Error in CacheAScoreResults: " + ex.Message);
				return false;
			}

			return true;
		}

		protected string ReadHeaderLine(string filePath)
		{
			string headerLine = string.Empty;

			using (System.IO.StreamReader srInFile = new System.IO.StreamReader(new System.IO.FileStream(filePath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read)))
			{
				if (srInFile.Peek() > -1)
				{
					headerLine = srInFile.ReadLine();
				}
			}

			return headerLine;
		}

		protected string ReplaceFirst(string textToSearch, string searchText, string replacementText)
		{
			if (string.IsNullOrEmpty(textToSearch))
				return string.Empty;

			if (string.IsNullOrEmpty(searchText))
				return textToSearch;

			int charIndex = textToSearch.IndexOf(searchText);
			if (charIndex < 0)
			{
				return textToSearch;
			}
			return textToSearch.Remove(charIndex, searchText.Length).Insert(charIndex, replacementText);
		}

		#endregion

		#region "PHRP Reader Event Handlers"
		void mPHRPReader_MessageEvent(string strMessage)
		{
			ReportMessage(strMessage);
		}

		void mPHRPReader_WarningEvent(string strWarningMessage)
		{
			ReportWarning(strWarningMessage);
		}

		void mPHRPReader_ErrorEvent(string strErrorMessage)
		{
			ReportError(strErrorMessage);
		}

		#endregion
	}
}
