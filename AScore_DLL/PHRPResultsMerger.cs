using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AScore_DLL.Managers.PSM_Managers;
using PHRPReader;
using PRISM;

namespace AScore_DLL
{
    /// <summary>
    /// Merges AScore results with an existing PHRP-compatible tab-delimited text file
    /// </summary>
    public class PHRPResultsMerger : EventNotifier
    {
        // Ignore Spelling: phrp

        protected string m_MergedFilePath = string.Empty;
        protected clsPHRPReader mPHRPReader;

        protected struct AScoreResultsType
        {
            public string BestSequence;  // New peptide sequence
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

                return m_MergedFilePath;
            }
        }
        #endregion

        public bool MergeResults(AScoreOptions ascoreOptions)
        {
            return MergeResults(
                ascoreOptions.DbSearchResultsFile,
                ascoreOptions.AScoreResultsFilePath,
                ascoreOptions.UpdatedDbSearchResultsFileName);
        }

        public bool MergeResults(string phrpDataFilePath, string ascoreResultsFilePath, string mergedPhrpDataFileName)
        {
            try
            {
                var phrpDataFile = new FileInfo(phrpDataFilePath);
                if (!phrpDataFile.Exists)
                {
                    OnErrorEvent("PHRP Data File not found: " + phrpDataFile.FullName);
                    return false;
                }

                var ascoreResultsFile = new FileInfo(ascoreResultsFilePath);
                if (!ascoreResultsFile.Exists)
                {
                    OnErrorEvent("AScore results file not found: " + ascoreResultsFile.FullName);
                    return false;
                }

                // Initialize the PHRPReader
                var success = InitializeReader(phrpDataFile);
                if (!success)
                    return false;

                if (string.IsNullOrEmpty(mergedPhrpDataFileName))
                {
                    // Auto-define mergedPhrpDataFileName
                    mergedPhrpDataFileName = Path.GetFileNameWithoutExtension(phrpDataFile.Name) + "_WithAScore" + phrpDataFile.Extension;
                }

                if (ascoreResultsFile.DirectoryName == null)
                    m_MergedFilePath = mergedPhrpDataFileName;
                else
                    m_MergedFilePath = Path.Combine(ascoreResultsFile.DirectoryName, Path.GetFileName(mergedPhrpDataFileName));

                var fiOutputFilePath = new FileInfo(m_MergedFilePath);

                if (FilePathsMatch(phrpDataFile, fiOutputFilePath))
                {
                    OnErrorEvent("Input PHRP file has the same name as the specified updated PHRP file; unable to create merged file: " + fiOutputFilePath.FullName);
                    return false;
                }

                if (FilePathsMatch(ascoreResultsFile, fiOutputFilePath))
                {
                    OnErrorEvent("AScore results file has the same name as the specified updated PHRP file; unable to create merged file: " + fiOutputFilePath.FullName);
                    return false;
                }

                // Cache the AScore results in memory
                var cachedAscoreResults = new Dictionary<string, AScoreResultsType>();

                success = CacheAScoreResults(ascoreResultsFilePath, cachedAscoreResults);
                if (!success)
                    return false;

                MakeUpdatedPHRPFile(phrpDataFile, fiOutputFilePath, mPHRPReader, cachedAscoreResults);
            }
            catch (Exception ex)
            {
                OnErrorEvent("Error in MergeResults: " + ex.Message);
                return false;
            }

            return true;
        }

        #region "Class functions"

        private bool CacheAScoreResults(string ascoreResultsFilePath, IDictionary<string, AScoreResultsType> cachedAscoreResults)
        {
            var headersParsed = false;
            var columnHeaders = new SortedDictionary<string, int>(StringComparer.CurrentCultureIgnoreCase);

            try
            {
                if (!File.Exists(ascoreResultsFilePath))
                {
                    OnErrorEvent("File not found: " + ascoreResultsFilePath);
                    return false;
                }

                // Define the default column mapping
                columnHeaders.Add(PsmResultsManager.RESULTS_COL_JOB, 0);
                columnHeaders.Add(PsmResultsManager.RESULTS_COL_SCAN, 1);
                columnHeaders.Add(PsmResultsManager.RESULTS_COL_ORIGINAL_SEQUENCE, 2);
                columnHeaders.Add(PsmResultsManager.RESULTS_COL_BEST_SEQUENCE, 3);
                columnHeaders.Add(PsmResultsManager.RESULTS_COL_PEPTIDE_SCORE, 4);
                columnHeaders.Add(PsmResultsManager.RESULTS_COL_ASCORE, 5);
                columnHeaders.Add(PsmResultsManager.RESULTS_COL_NUM_SITE_IONS_POSS, 6);
                columnHeaders.Add(PsmResultsManager.RESULTS_COL_NUM_SITE_IONS_MATCHED, 7);
                columnHeaders.Add(PsmResultsManager.RESULTS_COL_SECOND_SEQUENCE, 8);
                columnHeaders.Add(PsmResultsManager.RESULTS_COL_MOD_INFO, 9);

                using (var resultsFileReader = new StreamReader(new FileStream(ascoreResultsFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
                {
                    while (!resultsFileReader.EndOfStream)
                    {
                        var lineIn = resultsFileReader.ReadLine();
                        if (string.IsNullOrEmpty(lineIn))
                            continue;

                        var splitLine = lineIn.Split('\t');

                        if (!headersParsed)
                        {
                            clsPHRPReader.ParseColumnHeaders(splitLine, columnHeaders);
                            headersParsed = true;
                            continue;
                        }

                        var scanNumber = clsPHRPReader.LookupColumnValue(splitLine, PsmResultsManager.RESULTS_COL_SCAN, columnHeaders, -1);
                        var originalPeptide = clsPHRPReader.LookupColumnValue(splitLine, PsmResultsManager.RESULTS_COL_ORIGINAL_SEQUENCE, columnHeaders, string.Empty);
                        var bestSequence = clsPHRPReader.LookupColumnValue(splitLine, PsmResultsManager.RESULTS_COL_BEST_SEQUENCE, columnHeaders, string.Empty);
                        var peptideScore = clsPHRPReader.LookupColumnValue(splitLine, PsmResultsManager.RESULTS_COL_PEPTIDE_SCORE, columnHeaders, 0.0);
                        var ascoreValue = clsPHRPReader.LookupColumnValue(splitLine, PsmResultsManager.RESULTS_COL_ASCORE, columnHeaders, 0.0);

                        // ReSharper disable CommentTypo
                        //int numSiteIonsPossible = clsPHRPReader.LookupColumnValue(splitLine, PsmResultsManager.RESULTS_COL_NUM_SITE_IONS_POSS, columnHeaders, 0);
                        //int numSitIonsMatched = clsPHRPReader.LookupColumnValue(splitLine, PsmResultsManager.RESULTS_COL_NUM_SITE_IONS_MATCHED, columnHeaders, 0);
                        //string secondSequence = clsPHRPReader.LookupColumnValue(splitLine, PsmResultsManager.RESULTS_COL_SECOND_SEQUENCE, columnHeaders, string.Empty);
                        // ReSharper restore CommentTypo

                        var modInfo = clsPHRPReader.LookupColumnValue(splitLine, PsmResultsManager.RESULTS_COL_MOD_INFO, columnHeaders, string.Empty);

                        var scanPeptideKey = ConstructScanPeptideKey(scanNumber, originalPeptide);

                        if (cachedAscoreResults.TryGetValue(scanPeptideKey, out var ascoreResult))
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
                OnErrorEvent("Error in CacheAScoreResults: " + ex.Message);
                return false;
            }

            return true;
        }

        protected string ConstructScanPeptideKey(int scanNumber, string peptideSequence)
        {
            return scanNumber + "_" + peptideSequence;
        }

        protected SortedSet<string> DetermineModInfoNames(Dictionary<string, AScoreResultsType> cachedAscoreResults)
        {
            var modInfoNames = new SortedSet<string>();

            foreach (var ascoreResult in cachedAscoreResults)
            {
                foreach (var modInfoEntry in ascoreResult.Value.AScoreByMod)
                {
                    // Unmodified peptides will have a ModInfo key of "-"
                    // Skip these entries
                    if (modInfoEntry.Key != AScoreProcessor.MOD_INFO_NO_MODIFIED_RESIDUES)
                    {
                        if (!modInfoNames.Contains(modInfoEntry.Key))
                            modInfoNames.Add(modInfoEntry.Key);
                    }
                }
            }

            return modInfoNames;
        }

        protected bool InitializeReader(FileInfo fiInputFile)
        {
            try
            {
                var ePeptideHitResultType = clsPHRPReader.AutoDetermineResultType(fiInputFile.FullName);

                if (ePeptideHitResultType == clsPHRPReader.ePeptideHitResultType.Unknown)
                {
                    OnErrorEvent("Error: Could not determine the format of the PHRP data file: " + fiInputFile.FullName);
                    return false;
                }

                // Open the data file and read the data
                mPHRPReader = new clsPHRPReader(fiInputFile.FullName, clsPHRPReader.ePeptideHitResultType.Unknown, false, false, false)
                {
                    EchoMessagesToConsole = false,
                    SkipDuplicatePSMs = false
                };

                if (!mPHRPReader.CanRead)
                {
                    OnErrorEvent("Aborting since PHRPReader is not ready: " + mPHRPReader.ErrorMessage);
                    return false;
                }

                // Attach the events
                RegisterEvents(mPHRPReader);
            }
            catch (Exception ex)
            {
                OnErrorEvent("Error in InitializeReader: " + ex.Message);
                return false;
            }

            return true;
        }

        private void MakeUpdatedPHRPFile(
            FileSystemInfo inputFile,
            FileSystemInfo outputFile,
            clsPHRPReader phrpReader,
            Dictionary<string, AScoreResultsType> cachedAscoreResults)
        {
            try
            {
                // Read the header line from the PHRP file
                var outputHeaderLine = ReadHeaderLine(inputFile.FullName);

                var modInfoNames = DetermineModInfoNames(cachedAscoreResults);

                // Create the output file
                using (var phrpWriter = new StreamWriter(new FileStream(outputFile.FullName, FileMode.Create, FileAccess.Write, FileShare.Read)))
                {
                    var outLine = new System.Text.StringBuilder();

                    // Write the header line
                    outLine.Append(outputHeaderLine);
                    outLine.Append("\t" + PsmResultsManager.RESULTS_COL_PEPTIDE_SCORE);
                    outLine.Append("\t" + "Modified_Residues");

                    foreach (var modInfoName in modInfoNames)
                    {
                        outLine.Append("\t" + modInfoName);
                    }
                    phrpWriter.WriteLine(outLine);

                    var skipCount = 0;

                    while (phrpReader.MoveNext())
                    {
                        var currentPSM = phrpReader.CurrentPSM;

                        var scanPeptideKey = ConstructScanPeptideKey(currentPSM.ScanNumber, currentPSM.Peptide);

                        if (!cachedAscoreResults.TryGetValue(scanPeptideKey, out var ascoreResult))
                        {
                            skipCount++;
                            if (skipCount < 10)
                                Console.WriteLine("  Skipping PHRP result without AScore result: " + scanPeptideKey);

                            continue;
                        }

                        // Replace the original peptide with the "best" peptide
                        var dataLineUpdated = ReplaceFirst(currentPSM.DataLineText, currentPSM.Peptide, ascoreResult.BestSequence);

                        outLine.Clear();
                        outLine.Append(dataLineUpdated);

                        outLine.Append("\t" + StringUtilities.ValueToString(ascoreResult.PeptideScore));

                        // Count the number of modInfo entries that are not "-"
                        var modTypeCount = (from item in ascoreResult.AScoreByMod where item.Key != AScoreProcessor.MOD_INFO_NO_MODIFIED_RESIDUES select item.Key).Count();
                        outLine.Append("\t" + modTypeCount);

                        foreach (var modInfoName in modInfoNames)
                        {
                            var modInfoMatch = false;
                            foreach (var modInfoEntry in ascoreResult.AScoreByMod)
                            {
                                if (modInfoName == modInfoEntry.Key)
                                {
                                    outLine.Append("\t" + StringUtilities.ValueToString(modInfoEntry.Value));
                                    modInfoMatch = true;
                                    break;
                                }
                            }
                            if (!modInfoMatch)
                                outLine.Append("\t");
                        }

                        phrpWriter.WriteLine(outLine);
                    }

                    if (skipCount > 0)
                        OnStatusEvent("  Skipped " + skipCount + " PHRP results without an AScore result");
                }
            }
            catch (Exception ex)
            {
                OnErrorEvent("Error in CacheAScoreResults: " + ex.Message);
            }
        }

        protected bool FilePathsMatch(FileInfo fiFile1, FileInfo fiFile2)
        {
            var filePath1 = Path.GetFullPath(fiFile1.FullName);
            var filePath2 = Path.GetFullPath(fiFile2.FullName);

            if (string.Equals(filePath1, filePath2, StringComparison.CurrentCultureIgnoreCase))
                return true;

            return false;
        }

        protected string ReadHeaderLine(string filePath)
        {
            var headerLine = string.Empty;

            using (var reader = new StreamReader(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                if (!reader.EndOfStream)
                {
                    headerLine = reader.ReadLine();
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

            var charIndex = textToSearch.IndexOf(searchText, StringComparison.Ordinal);
            if (charIndex < 0)
            {
                return textToSearch;
            }
            return textToSearch.Remove(charIndex, searchText.Length).Insert(charIndex, replacementText);
        }

        #endregion
    }
}
