using System;
using System.Collections.Generic;
using System.IO;
using PeptideToProteinMapEngine;
using PHRPReader;
using PRISM;
using ProteinCoverageSummarizer;

namespace AScore_DLL
{
    class AScoreProteinMapper
    {
        // Ignore Spelling: AScore

        private const string OutputFilenameAddition = "_ProteinMap";
        private const string PeptideFilenameAddition = "_Peptides";
        private const int MaxMissingPeptidesToShow = 30;

        private struct ProteinPeptideMapType
        {
            public string peptideSequence;
            public string proteinName;
            public int residueStart;
        }

        private readonly string mAScoreResultsFilePath;
        private readonly string mOutputDirectoryPath;
        private readonly bool mOutputProteinDescriptions;
        private readonly string mFastaFilePath;
        private readonly string mPeptideListFilePath;
        private readonly string mProteinToPeptideMapFilePath;
        private readonly string mMappingResultsFilePath;

        /// <summary>
        /// Keys are peptide clean sequence, values are the list of proteins that contain the peptide
        /// </summary>
        readonly Dictionary<string, List<ProteinPeptideMapType>> mPeptideToProteinMap;

        /// <summary>
        /// Keys are protein names, values are descriptions (from the FASTA file)
        /// </summary>
        private readonly Dictionary<string, string> mProteinDescriptions;

        /// <summary>
        /// Dictionary that keeps track of peptides not found in mPeptideToProteinMap
        /// Keys are peptide sequence, values are the number of times the peptide is in the input file
        /// </summary>
        private readonly Dictionary<string, int> mPeptidesNotFound;

        private readonly Dictionary<string, int> mPeptidesReallyNotFound;

        private DateTime mLastProgressTime = DateTime.UtcNow;

        private int mTotalPeptidesNotFound;
        private int mTotalPeptides;
        private int mDistinctPeptides;
        private int mDistinctReverseHits;
        private int mTotalReverseHits;
        private int mTotalPeptidesReallyNotFound;

        /// <summary>
        /// Configure the AScore Protein Mapper
        /// </summary>
        /// <param name="aScoreResultsFilePath">Results file from running AScore algorithm</param>
        /// <param name="fastaFilePath">Path to the desired FASTA file</param>
        /// <param name="outputDescriptions">Whether to include protein description line in output</param>
        public AScoreProteinMapper(string aScoreResultsFilePath, string fastaFilePath, bool outputDescriptions)
        {
            mAScoreResultsFilePath = aScoreResultsFilePath;
            mOutputDirectoryPath = Path.GetDirectoryName(aScoreResultsFilePath);
            mOutputProteinDescriptions = outputDescriptions;
            mFastaFilePath = fastaFilePath;
            mMappingResultsFilePath = Path.Combine(mOutputDirectoryPath ?? string.Empty, Path.GetFileNameWithoutExtension(mAScoreResultsFilePath) + OutputFilenameAddition + Path.GetExtension(mAScoreResultsFilePath));
            mPeptideListFilePath = Path.Combine(mOutputDirectoryPath ?? string.Empty, Path.GetFileNameWithoutExtension(mAScoreResultsFilePath) + PeptideFilenameAddition + Path.GetExtension(mAScoreResultsFilePath));
            mProteinToPeptideMapFilePath = Path.Combine(mOutputDirectoryPath ?? string.Empty, Path.GetFileNameWithoutExtension(mPeptideListFilePath) + clsProteinCoverageSummarizer.FILENAME_SUFFIX_PROTEIN_TO_PEPTIDE_MAPPING);

            mPeptideToProteinMap = new Dictionary<string, List<ProteinPeptideMapType>>();
            mProteinDescriptions = new Dictionary<string, string>();
            mPeptidesNotFound = new Dictionary<string, int>();
            mPeptidesReallyNotFound = new Dictionary<string, int>();

            mTotalPeptidesNotFound = 0;
            mTotalPeptides = 0;
            mDistinctPeptides = 0;
            mDistinctReverseHits = 0;
            mTotalReverseHits = 0;
            mTotalPeptidesReallyNotFound = 0;
        }

        /// <summary>
        /// Destructor: remove the temporary files.
        /// </summary>
        ~AScoreProteinMapper()
        {
            if (File.Exists(mPeptideListFilePath))
            {
                File.Delete(mPeptideListFilePath);
            }
            if (File.Exists(mProteinToPeptideMapFilePath))
            {
                File.Delete(mProteinToPeptideMapFilePath);
            }
        }

        /// <summary>
        /// Run - configure data, call protein mapper, and aggregate the data
        /// </summary>
        public void Run()
        {
            // Create the list of cleaned peptide sequences
            CreatePeptideList();

            // Configure and call the peptide to protein mapper
            var bSuccess = MapProteins();

            if (bSuccess)
            {
                // Read the output of the peptide to protein mapper back in
                ReadBackMap();

                if (mOutputProteinDescriptions)
                {
                    // Read the protein descriptions
                    ReadFastaProteinDescription();
                }

                // Read the AScore again, and output a combined results file
                CombineAScoreAndProteinData();

                if (mTotalPeptidesNotFound > 0)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    CheckReversedPeptides();

                    Console.WriteLine("Peptide match failures:");
                    Console.WriteLine("\tDistinct Reverse Hits: \t\t" + mDistinctReverseHits + " \t(" + (mDistinctReverseHits / (double)mDistinctPeptides).ToString("P") + ")");
                    Console.WriteLine("\tTotal Reverse Hits: \t\t" + mTotalReverseHits + " \t(" + (mTotalReverseHits / (double)mTotalPeptides).ToString("P") + ")");
                    Console.WriteLine("\tDistinct Peptides Not Found: \t" + mPeptidesReallyNotFound.Count + " \t(" + (mPeptidesReallyNotFound.Count / (double)mDistinctPeptides).ToString("P") + ")");
                    Console.WriteLine("\tTotal Peptides Not Found: \t" + mTotalPeptidesReallyNotFound + " \t(" + (mTotalPeptidesReallyNotFound / (double)mTotalPeptides).ToString("P") + ")");

                    if (mPeptidesReallyNotFound.Count > 0)
                    {
                        Console.WriteLine("\nWarning: Some peptide sequences were not found, and are not reverse hits.");
                        Console.WriteLine("\tYou may be using the WRONG FASTA file.");

                        if (mPeptidesReallyNotFound.Count < MaxMissingPeptidesToShow)
                        {
                            Console.WriteLine("\tCount\tPeptide");
                            foreach (var peptide in mPeptidesReallyNotFound)
                            {
                                Console.WriteLine("\t" + peptide.Value + "\t" + peptide.Key);
                            }
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("Error: Failed to map the peptides to proteins. See Log file in \"" + mOutputDirectoryPath + "\" for details.");
            }
        }

        /// <summary>
        /// Re-run the mapping with not found peptides reversed to see if they are reverse hits
        /// </summary>
        private void CheckReversedPeptides()
        {
            CreateReversedPeptideList();
            var bSuccess = MapProteins();
            if (bSuccess)
            {
                ReadBackMap();
                foreach (var peptide in mPeptidesNotFound)
                {
                    if (mPeptideToProteinMap.ContainsKey(Reverse(peptide.Key)))
                    {
                        ++mDistinctReverseHits;
                        mTotalReverseHits += peptide.Value;
                    }
                    else
                    {
                        mPeptidesReallyNotFound.Add(peptide.Key, peptide.Value);
                        mTotalPeptidesReallyNotFound += peptide.Value;
                    }
                }
            }
        }

        /// <summary>
        /// Simple string reversing function to check for reverse hits
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        public static string Reverse(string s)
        {
            var charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }

        /// <summary>
        /// Output the list of not found peptides to a file
        /// </summary>
        private void CreateReversedPeptideList()
        {
            // Write out a list of peptides for clsPeptideToProteinMapEngine
            using (var peptideWriter = new StreamWriter(new FileStream(mPeptideListFilePath, FileMode.Create, FileAccess.Write, FileShare.Read)))
            {
                foreach (var peptide in mPeptidesNotFound)
                {
                    peptideWriter.WriteLine(Reverse(peptide.Key));
                }
            }
        }

        /// <summary>
        /// Pull sequences out of AScore results, clean them, and output them to a peptide sequence list file
        /// </summary>
        private void CreatePeptideList()
        {
            var columnMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var peptides = new Dictionary<string, int>();

            // Write out a list of peptides for clsPeptideToProteinMapEngine
            using (var aScoreReader = new StreamReader(new FileStream(mAScoreResultsFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            using (var peptideWriter = new StreamWriter(new FileStream(mPeptideListFilePath, FileMode.Create, FileAccess.Write, FileShare.Read)))
            {
                while (!aScoreReader.EndOfStream)
                {
                    var dataLine = aScoreReader.ReadLine();
                    if (string.IsNullOrWhiteSpace(dataLine))
                        continue;

                    var columns = dataLine.Split('\t');

                    if (columnMap.Count == 0)
                    {
                        // Assume the first line is column names
                        for (var i = 0; i < columns.Length; ++i)
                        {
                            columnMap.Add(columns[i], i);
                        }

                        var requiredColumns = new List<string>
                        {
                            "BestSequence"
                        };

                        if (!VerifyRequiredColumns(requiredColumns, columnMap, "CreatePeptideList", mAScoreResultsFilePath))
                            return;

                        continue;
                    }

                    var sequence = columns[columnMap["BestSequence"]];
                    var cleanSequence = clsPeptideCleavageStateCalculator.ExtractCleanSequenceFromSequenceWithMods(sequence, true);
                    if (!peptides.ContainsKey(cleanSequence))
                    {
                        peptides.Add(cleanSequence, 0);
                    }

                    peptideWriter.WriteLine(cleanSequence);
                }
            }
            mDistinctPeptides = peptides.Count;
        }

        /// <summary>
        /// Configure and call the protein mapper
        /// </summary>
        /// <returns></returns>
        private bool MapProteins()
        {
            // Configure the peptide to protein mapper
            var options = new ProteinCoverageSummarizerOptions
            {
                IgnoreILDifferences = false,
                MatchPeptidePrefixAndSuffixToProtein = false,
                OutputProteinSequence = false,
                PeptideFileSkipFirstLine = false,
                RemoveSymbolCharacters = true,
                ProteinInputFilePath = mFastaFilePath,
                SaveProteinToPeptideMappingFile = true,
                SearchAllProteinsForPeptideSequence = true,
                SearchAllProteinsSkipCoverageComputationSteps = true
            };

            var peptideToProteinMapper = new clsPeptideToProteinMapEngine(options)
            {
                DeleteTempFiles = true,
                InspectParameterFilePath = string.Empty,
                PeptideInputFileFormat = clsPeptideToProteinMapEngine.PeptideInputFileFormatConstants.PeptideListFile,
            };

            if (!string.IsNullOrEmpty(mOutputDirectoryPath))
            {
                peptideToProteinMapper.LogMessagesToFile = true;
                peptideToProteinMapper.LogDirectoryPath = mOutputDirectoryPath;
                peptideToProteinMapper.LogFilePath = "PeptideToProteinMapper_Log.txt";
            }
            else
            {
                peptideToProteinMapper.LogMessagesToFile = false;
            }

            peptideToProteinMapper.ProgressUpdate += PeptideToProteinMapper_ProgressUpdate;

            // Note that clsPeptideToProteinMapEngine utilizes Data.SQLite.dll
            var success = peptideToProteinMapper.ProcessFile(mPeptideListFilePath, mOutputDirectoryPath, string.Empty, true);

            peptideToProteinMapper.CloseLogFileNow();

            return success;
        }

        /// <summary>
        /// Read the resulting data from the peptide to protein mapper into a dictionary.
        /// </summary>
        private void ReadBackMap()
        {
            var columnMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            using (var mapReader = new StreamReader(new FileStream(mProteinToPeptideMapFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                while (!mapReader.EndOfStream)
                {
                    var dataLine = mapReader.ReadLine();
                    if (string.IsNullOrWhiteSpace(dataLine))
                        continue;

                    var columns = dataLine.Split('\t');

                    if (columnMap.Count == 0)
                    {
                        // Assume the first line is column names
                        for (var i = 0; i < columns.Length; ++i)
                        {
                            columnMap.Add(columns[i], i);
                        }

                        var requiredColumns = new List<string>
                        {
                            "Residue Start",
                            "Residue End",
                            "Protein Name",
                            "Peptide Sequence"
                        };

                        if (!VerifyRequiredColumns(requiredColumns, columnMap, "ReadBackMap", mProteinToPeptideMapFilePath))
                            return;

                        continue;
                    }

                    var item = new ProteinPeptideMapType
                    {
                        residueStart = Convert.ToInt32(columns[columnMap["Residue Start"]]),
                        // residueEnd = Convert.ToInt32(columns[columnMap["Residue End"]]),
                        proteinName = columns[columnMap["Protein Name"]],
                        peptideSequence = columns[columnMap["Peptide Sequence"]]
                    };

                    // Add the key and a new list if it doesn't yet exist
                    if (mPeptideToProteinMap.TryGetValue(item.peptideSequence, out var proteinsForPeptide))
                    {
                        proteinsForPeptide.Add(item);
                    }
                    else
                    {
                        var newProteinList = new List<ProteinPeptideMapType> { item };
                        mPeptideToProteinMap.Add(item.peptideSequence, newProteinList);
                    }
                }
            }
        }

        /// <summary>
        /// Read the protein name and description lines out of the Fasta file, and store them in a dictionary.
        /// </summary>
        private void ReadFastaProteinDescription()
        {
            using (var fastaReader = new StreamReader(new FileStream(mFastaFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                while (!fastaReader.EndOfStream)
                {
                    var line = fastaReader.ReadLine();
                    if (string.IsNullOrWhiteSpace(line) || line[0] != '>')
                        continue;

                    // This is a protein name/description line (starts with >)
                    // Extract the protein description
                    var spaceIndex = line.IndexOf(' ');

                    if (spaceIndex > 0)
                    {
                        var proteinName = line.Substring(1, spaceIndex - 1);
                        var proteinDescription = line.Substring(spaceIndex + 1);
                        mProteinDescriptions.Add(proteinName, proteinDescription);
                    }
                    else
                    {
                        mProteinDescriptions.Add(line, string.Empty);
                    }
                }
            }
        }

        /// <summary>
        /// Combine the data from AScore and the PeptideToProteinMapper into one results file
        /// </summary>
        private void CombineAScoreAndProteinData()
        {
            // Read the AScore again...
            using (var aScoreReader = new StreamReader(new FileStream(mAScoreResultsFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
            using (var mappedWriter = new StreamWriter(new FileStream(mMappingResultsFilePath, FileMode.Create, FileAccess.Write, FileShare.Read)))
            {
                var columnMapAScore = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                if (aScoreReader.EndOfStream)
                {
                    return;
                }

                // Run as long as we can successfully read
                while (!aScoreReader.EndOfStream)
                {
                    var dataLine = aScoreReader.ReadLine();
                    if (string.IsNullOrWhiteSpace(dataLine))
                        continue;

                    var columns = dataLine.Split('\t');

                    if (columnMapAScore.Count == 0)
                    {
                        for (var i = 0; i < columns.Length; ++i)
                        {
                            columnMapAScore.Add(columns[i], i);
                        }

                        var requiredColumns = new List<string>
                        {
                            "BestSequence"
                        };

                        if (!VerifyRequiredColumns(requiredColumns, columnMapAScore, "CombineAScoreAndProteinData", mAScoreResultsFilePath))
                        {
                            return;
                        }

                        var outputFileHeaders = new List<string>();
                        outputFileHeaders.AddRange(columns);

                        // Append additional columns to outputFileHeaders
                        outputFileHeaders.Add("ProteinName");

                        // Protein Description - if it contains key-value pairs, use it.
                        if (mOutputProteinDescriptions)
                        {
                            outputFileHeaders.Add("Description");
                        }

                        outputFileHeaders.Add("ProteinCount");
                        outputFileHeaders.Add("Residue");
                        outputFileHeaders.Add("Position");

                        mappedWriter.WriteLine(string.Join("\t", outputFileHeaders));

                        continue;
                    }

                    var sequence = columns[columnMapAScore["BestSequence"]];

                    var cleanSequence = clsPeptideCleavageStateCalculator.ExtractCleanSequenceFromSequenceWithMods(sequence, true);

                    ++mTotalPeptides;

                    if (!mPeptideToProteinMap.ContainsKey(cleanSequence))
                    {
                        // Match not found
                        WriteCombinedLine(mappedWriter, dataLine);

                        if (!mPeptidesNotFound.ContainsKey(cleanSequence))
                        {
                            mPeptidesNotFound.Add(cleanSequence, 0);
                        }
                        mPeptidesNotFound[cleanSequence]++;
                        ++mTotalPeptidesNotFound;

                        continue;
                    }

                    clsPeptideCleavageStateCalculator.SplitPrefixAndSuffixFromSequence(sequence, out var noPrefixSequence, out _, out _);

                    var mods = new List<int>();

                    for (var i = 0; i < noPrefixSequence.Length; ++i)
                    {
                        if (noPrefixSequence[i] == '*')
                        {
                            mods.Add(i);
                        }
                    }

                    foreach (var match in mPeptideToProteinMap[cleanSequence])
                    {
                        // Protein Name
                        var proteinName = match.proteinName;

                        var proteinDescription = string.Empty;

                        // Protein Description - if it contains key-value pairs, use it.
                        if (mOutputProteinDescriptions)
                        {
                            proteinDescription = mProteinDescriptions[match.proteinName];
                        }

                        // # of proteins occurred in
                        var proteinCount = mPeptideToProteinMap[cleanSequence].Count;

                        var matchFound = false;

                        for (var i = 0; i < mods.Count; ++i)
                        {
                            matchFound = true;

                            var modifiedResidue = ' ';

                            // Residue of mod
                            if (mods[i] > 0)
                                modifiedResidue = noPrefixSequence[mods[i] - 1];

                            // Position of residue
                            // With multiple residues, we need to adjust the position of each subsequent residue by the number of residues we have read
                            var residuePosition = match.residueStart + mods[i] - i - 1;

                            WriteCombinedLine(mappedWriter, dataLine, proteinName, proteinDescription, proteinCount, modifiedResidue, residuePosition);
                        }

                        if (!matchFound)
                        {
                            const char modifiedResidue = ' ';
                            const int residuePosition = 0;

                            WriteCombinedLine(mappedWriter, dataLine, proteinName, proteinDescription, proteinCount, modifiedResidue, residuePosition);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Verifies that the specified column names are present in columnMap
        /// </summary>
        /// <param name="requiredColumns"></param>
        /// <param name="columnMap"></param>
        /// <param name="callingMethod"></param>
        /// <param name="sourceFile"></param>
        /// <returns></returns>
        private bool VerifyRequiredColumns(
            IEnumerable<string> requiredColumns,
            IReadOnlyDictionary<string, int> columnMap,
            string callingMethod,
            string sourceFile)
        {
            foreach (var columnName in requiredColumns)
            {
                if (columnMap.ContainsKey(columnName))
                    continue;

                ConsoleMsgUtils.ShowError("Error in {0}: Required column '{1}' not found in {2}", callingMethod, columnName, sourceFile);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Write out the original line plus the additional data
        /// </summary>
        /// <param name="mappedWriter"></param>
        /// <param name="ascoreLine"></param>
        private void WriteCombinedLine(TextWriter mappedWriter, string ascoreLine)
        {
            WriteCombinedLine(mappedWriter, ascoreLine, string.Empty, string.Empty, 0, ' ', 0);
        }

        /// <summary>
        /// Write out the original line plus the additional data
        /// </summary>
        /// <param name="mappedWriter"></param>
        /// <param name="ascoreLine"></param>
        /// <param name="proteinName"></param>
        /// <param name="proteinDescription"></param>
        /// <param name="proteinCount"></param>
        /// <param name="modifiedResidue"></param>
        /// <param name="residuePosition"></param>
        private void WriteCombinedLine(
            TextWriter mappedWriter,
            string ascoreLine,
            string proteinName,
            string proteinDescription,
            int proteinCount,
            char modifiedResidue,
            int residuePosition)
        {
            var dataToWrite = new List<string> {
                ascoreLine,     // Original AScore data
                proteinName
            };

            // Protein Description - if it contains key-value pairs, use it.
            if (mOutputProteinDescriptions)
            {
                dataToWrite.Add(proteinDescription);
            }

            // # of proteins occurred in
            dataToWrite.Add(proteinCount.ToString());

            // Residue of mod
            dataToWrite.Add(modifiedResidue.ToString());

            // Position of residue
            dataToWrite.Add(residuePosition.ToString());

            mappedWriter.WriteLine(string.Join("\t", dataToWrite));
        }

        private void PeptideToProteinMapper_ProgressUpdate(string progressMessage, float percentComplete)
        {
            if (DateTime.UtcNow.Subtract(mLastProgressTime).TotalSeconds < 1)
                return;

            mLastProgressTime = DateTime.UtcNow;

            ConsoleMsgUtils.ShowDebug("Peptide to protein mapper is {0:F1}% complete: {1}", percentComplete, progressMessage);
        }
    }
}
