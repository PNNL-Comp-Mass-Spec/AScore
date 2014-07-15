using System;
using System.Collections.Generic;
using System.IO;
using System.Data.SQLite; // The DLL is required for execution of the PeptideToProteinMapper
using System.Linq;
using PeptideToProteinMapEngine;
using PHRPReader;
using ProteinCoverageSummarizer;

namespace AScore_Console
{
    class AScoreProteinMapper
    {
        private const int MaxUnfoundPeptidesOutput = 30;

        private struct ProteinPeptideMapType
        {
            public string peptideSequence;
            public string proteinName;
            public int residueStart;
            public int residueEnd;

            public void Initialize()
            {
                peptideSequence = String.Empty;
                proteinName = String.Empty;
            }
        }

        private string mAScoreResultsFilePath;
        private string mOutputFolderPath;
        private bool mOutputProteinDescriptions;
        private string mFastaFilePath;
        private string mLogFilePath;
        private string mPeptideListFilePath;
        private string mProteinToPeptideMapFilePath;
        private string mMappingResultsFilePath;
        Dictionary<string, List<ProteinPeptideMapType>> mPeptideToProteinMap;
        private Dictionary<string, string> mProteinDescriptions;
        private Dictionary<string, int> mPeptidesNotFound;
        private Dictionary<string, int> mPeptidesReallyNotFound;
        private int mTotalPeptidesNotFound;
        private int mTotalPeptides;
        private int mDistinctPeptides;
        private int mDistinctReverseHits;
        private int mTotalReverseHits;
        private int mTotalPeptidesReallyNotFound;


        /// <summary>
        /// Configure the AScore Protein Mapper
        /// </summary>
        /// <param name="aScoreRunData">Result data from running AScore algorithm</param>
        /// <param name="fastaOptions">Information for running the protein mapper</param>
        /// <param name="logFilePath"></param>
        public AScoreProteinMapper(Program.AScoreRunDataType aScoreRunData, Program.FastaOptionsType fastaOptions,
            string logFilePath)
        {
            mAScoreResultsFilePath = aScoreRunData.AScoreResultsFilePath;
            mOutputFolderPath = aScoreRunData.diOutputFolder.FullName;
            mOutputProteinDescriptions = fastaOptions.OutputProteinDescriptions;
            mFastaFilePath = fastaOptions.FastaFilePath;
            mLogFilePath = logFilePath;
            mMappingResultsFilePath = Path.Combine(mOutputFolderPath, Path.GetFileNameWithoutExtension(mAScoreResultsFilePath) + "_ProteinMap.txt");
            mPeptideListFilePath = Path.Combine(mOutputFolderPath, Path.GetFileNameWithoutExtension(mAScoreResultsFilePath) + "_Peptides.txt");
            mProteinToPeptideMapFilePath = Path.Combine(mOutputFolderPath, Path.GetFileNameWithoutExtension(mPeptideListFilePath) + clsProteinCoverageSummarizer.FILENAME_SUFFIX_PROTEIN_TO_PEPTIDE_MAPPING);

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
            bool bSuccess = MapProteins();

            if (bSuccess)
            {
                // Read the output of the peptide to protein mapper back in
                ReadBackMap();

                if (mOutputProteinDescriptions)
                {
                    // Read the protein descriptions
                    ReadFastaProteinDescription();
                }
                // Read the ascore again, and output a combined results file
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

                        if (mPeptidesReallyNotFound.Count < MaxUnfoundPeptidesOutput)
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
                Console.WriteLine("Error: Failed to map the peptides to proteins. See Log file \"" + mLogFilePath + "\" for details.");
            }
        }

        /// <summary>
        /// Re-run the mapping with not found peptides reversed to see if they are reverse hits
        /// </summary>
        private void CheckReversedPeptides()
        {
            CreateReversedPeptideList();
            bool bSuccess = MapProteins();
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
            char[] charArray = s.ToCharArray();
            Array.Reverse(charArray);
            return new string(charArray);
        }

        /// <summary>
        /// Output the list of not found peptides to a file, and 
        /// </summary>
        private void CreateReversedPeptideList()
        {
            // Write out a list of peptides for clsPeptideToProteinMapEngine
            using (StreamWriter peptideWriter =
                new StreamWriter(new FileStream(mPeptideListFilePath, FileMode.Create, FileAccess.Write, FileShare.Read)))
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
            string line;
            Dictionary<string, int> columnMap = new Dictionary<string, int>();
            Dictionary<string, int> peptides = new Dictionary<string, int>();
            // Write out a list of peptides for clsPeptideToProteinMapEngine
            using (StreamReader aScoreReader =
                    new StreamReader(new FileStream(mAScoreResultsFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                using (StreamWriter peptideWriter =
                    new StreamWriter(new FileStream(mPeptideListFilePath, FileMode.Create, FileAccess.Write, FileShare.Read)))
                {
                    if ((line = aScoreReader.ReadLine()) != null)
                    {
                        // Assume the first line is column names
                        string[] columns = line.Split('\t');
                        for (int i = 0; i < columns.Length; ++i)
                        {
                            columnMap.Add(columns[i], i);
                        }
                        // Run as long as we can successfully read
                        while ((line = aScoreReader.ReadLine()) != null)
                        {
                            string sequence = line.Split('\t')[columnMap["BestSequence"]];
                            string cleanSequence = clsPeptideCleavageStateCalculator.ExtractCleanSequenceFromSequenceWithMods(sequence, true);
                            if (!peptides.ContainsKey(cleanSequence))
                            {
                                peptides.Add(cleanSequence, 0);
                            }
                            // We are only looking for total distinct peptides, we don't need to keep a count.
                            //peptides[cleanSequence]++;
                            peptideWriter.WriteLine(cleanSequence);
                        }
                    }
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
            clsPeptideToProteinMapEngine peptideToProteinMapper = new clsPeptideToProteinMapEngine();

            peptideToProteinMapper.DeleteTempFiles = true;
            peptideToProteinMapper.IgnoreILDifferences = false;
            peptideToProteinMapper.InspectParameterFilePath = String.Empty;

            if (!String.IsNullOrEmpty(mLogFilePath))
            {
                peptideToProteinMapper.LogMessagesToFile = true;
                peptideToProteinMapper.LogFolderPath = mOutputFolderPath;
            }
            else
            {
                peptideToProteinMapper.LogMessagesToFile = false;
            }

            peptideToProteinMapper.MatchPeptidePrefixAndSuffixToProtein = false;
            peptideToProteinMapper.OutputProteinSequence = false;
            peptideToProteinMapper.PeptideInputFileFormat = clsPeptideToProteinMapEngine.ePeptideInputFileFormatConstants.PeptideListFile;
            peptideToProteinMapper.PeptideFileSkipFirstLine = false;
            peptideToProteinMapper.ProteinDataRemoveSymbolCharacters = true;
            peptideToProteinMapper.ProteinInputFilePath = mFastaFilePath;
            peptideToProteinMapper.SaveProteinToPeptideMappingFile = true;
            peptideToProteinMapper.SearchAllProteinsForPeptideSequence = true;
            peptideToProteinMapper.SearchAllProteinsSkipCoverageComputationSteps = true;
            peptideToProteinMapper.ShowMessages = false;

            // Note that clsPeptideToProteinMapEngine utilizes Data.SQLite.dll
            bool bSuccess = peptideToProteinMapper.ProcessFile(mPeptideListFilePath, mOutputFolderPath, String.Empty, true);
            
            peptideToProteinMapper.CloseLogFileNow();

            return bSuccess;
        }

        /// <summary>
        /// Read the resulting data from the peptide to protein mapper into a dictionary.
        /// </summary>
        private void ReadBackMap()
	    {
	        string line;
	        Dictionary<string, int> columnMapPTPM = new Dictionary<string, int>();
            using (StreamReader mapReader =
                new StreamReader(new FileStream(mProteinToPeptideMapFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                if ((line = mapReader.ReadLine()) != null)
                {
                    // Assume the first line is column names
                    string[] columns = line.Split('\t');
                    for (int i = 0; i < columns.Length; ++i)
                    {
                        columnMapPTPM.Add(columns[i], i);
                    }
                    // Run as long as we can successfully read
                    while ((line = mapReader.ReadLine()) != null)
                    {
                        columns = line.Split('\t');
                        ProteinPeptideMapType item = new ProteinPeptideMapType();
                        item.residueStart = Convert.ToInt32(columns[columnMapPTPM["Residue Start"]]);
                        item.residueEnd = Convert.ToInt32(columns[columnMapPTPM["Residue End"]]);
                        item.proteinName = columns[columnMapPTPM["Protein Name"]];
                        item.peptideSequence = columns[columnMapPTPM["Peptide Sequence"]];
                        // Add the key and a new list if it doesn't yet exist
                        if (!mPeptideToProteinMap.ContainsKey(item.peptideSequence))
                        {
                            mPeptideToProteinMap.Add(item.peptideSequence, new List<ProteinPeptideMapType>());
                        }
                        mPeptideToProteinMap[item.peptideSequence].Add(item);
                    }
                }
            }
	    }

        /// <summary>
        /// Read the protein name and description lines out of the Fasta file, and store them in a dictionary.
        /// </summary>
        private void ReadFastaProteinDescription()
        {
            string line;
            using (StreamReader fastaReader =
                    new StreamReader(new FileStream(mFastaFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)))
            {
                while ((line = fastaReader.ReadLine()) != null)
                {
                    // We only care about the protein name/description line
                    if (line[0] == '>')
                    {
                        int firstSpace = line.IndexOf(' ');
                        // Skip the '>' and split at the first space
                        mProteinDescriptions.Add(line.Substring(1, firstSpace - 1), line.Substring(firstSpace + 1));
                    }
                }
            }
        }

        /// <summary>
        /// Combine the data from AScore and the PeptideToProteinMapper into one results file
        /// </summary>
        private void CombineAScoreAndProteinData()
        {
            string line;
            // Read the ascore again...
            using (StreamReader aScoreReader =
                    new StreamReader(new FileStream(mAScoreResultsFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                // Can't collapse these to one 'using' because they are not the same type.
                using (StreamWriter mappedWriter =
                    new StreamWriter(new FileStream(mMappingResultsFilePath, FileMode.Create, FileAccess.Write, FileShare.Read)))
                {
                    Dictionary<string, int> columnMapAScore = new Dictionary<string, int>();
                    // Reuse 'columnMapAScore', it is the same file.
                    if ((line = aScoreReader.ReadLine()) != null)
                    {
                        // Assume the first line is column names
                        string[] columns = line.Split('\t');
                        for (int i = 0; i < columns.Length; ++i)
                        {
                            columnMapAScore.Add(columns[i], i);
                        }
                        // Output the header information, with the new additions
                        mappedWriter.Write(line + "\t");
                        mappedWriter.Write("ProteinName\t");
                        // Protein Description - if it contains key-value pairs, use it.
                        if (mOutputProteinDescriptions)
                        {
                            mappedWriter.Write("Description\t");
                        }
                        mappedWriter.WriteLine("ProteinCount\tResidue\tPosition");
                        // Run as long as we can successfully read
                        while ((line = aScoreReader.ReadLine()) != null)
                        {
                            string sequence = line.Split('\t')[columnMapAScore["BestSequence"]];
                            string cleanSequence =
                                clsPeptideCleavageStateCalculator.ExtractCleanSequenceFromSequenceWithMods(
                                    sequence, true);
                            ++mTotalPeptides;
                            if (mPeptideToProteinMap.ContainsKey(cleanSequence))
                            {
                                string noPrefixSequence = String.Empty;
                                string prefix = String.Empty;
                                string suffix = String.Empty;
                                clsPeptideCleavageStateCalculator.SplitPrefixAndSuffixFromSequence(sequence,
                                    ref noPrefixSequence, ref prefix, ref suffix);
                                List<int> mods = new List<int>();
                                int modPosAdj = 0;
                                for (int i = 0; i < noPrefixSequence.Length; ++i)
                                {
                                    if (noPrefixSequence[i] == '*')
                                    {
                                        mods.Add(i);
                                    }
                                }
                                if (noPrefixSequence[0] == '.')
                                {
                                    ++modPosAdj;
                                }
                                foreach (var match in mPeptideToProteinMap[cleanSequence])
                                {
                                    for (int i = 0; i < mods.Count; ++i)
                                    {
                                        // Original AScore data
                                        mappedWriter.Write(line + "\t");
                                        // Protein Name
                                        mappedWriter.Write(match.proteinName + "\t");
                                        // Protein Description - if it contains key-value pairs, use it.
                                        if (mOutputProteinDescriptions)
                                        {
                                            mappedWriter.Write(mProteinDescriptions[match.proteinName] + "\t");
                                        }
                                        // # of proteins occurred in
                                        mappedWriter.Write(mPeptideToProteinMap[cleanSequence].Count + "\t");
                                        // Residue of mod
                                        mappedWriter.Write(noPrefixSequence[mods[i] - 1] + "\t");
                                        // Position of residue
                                        // With multiple residues, we need to adjust the position of each subsequent residue by the number of residues we have read
                                        mappedWriter.WriteLine(match.residueStart + mods[i] - i - 1);
                                    }
                                }
                            }
                            else
                            {
                                mappedWriter.WriteLine(line);
                                if (!mPeptidesNotFound.ContainsKey(cleanSequence))
                                {
                                    mPeptidesNotFound.Add(cleanSequence, 0);
                                }
                                mPeptidesNotFound[cleanSequence]++;
                                ++mTotalPeptidesNotFound;
                            }
                        }
                    }
                }
            }
        }
    }
}
