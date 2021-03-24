using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AScore_DLL.Managers;
using AScore_DLL.Managers.PSM_Managers;
using AScore_DLL.Managers.SpectraManagers;
using AScore_DLL.Mod;
using PHRPReader;
using PRISM;

namespace AScore_DLL
{
    public class AScoreProcessor : EventNotifier
    {
        // Ignore Spelling: ascore, fht, dta

        public const string MOD_INFO_NO_MODIFIED_RESIDUES = "-";

        private const double lowRangeMultiplier = 0.28;
        private const double maxRange = 2000.0;
        private const double minRange = 50.0;

        #region "Properties"

        public bool FilterOnMSGFScore { get; set; } = true;

        #endregion

        #region Public Methods

        /// <summary>
        /// Configure and run the AScore algorithm
        /// </summary>
        /// <param name="ascoreOptions"></param>
        /// <returns></returns>
        public int RunAScore(AScoreOptions ascoreOptions)
        {
            var paramManager = new ParameterFileManager(ascoreOptions.AScoreParamFile);
            RegisterEvents(paramManager);

            Console.WriteLine();

            if (paramManager.DynamicMods.Count > 0 || paramManager.StaticMods.Count > 0)
            {
                OnStatusEvent("Loaded modifications from: " + ascoreOptions.AScoreParamFile);

                foreach (var mod in paramManager.StaticMods)
                {
                    OnStatusEvent(Utilities.GetModDescription("Static,   ", mod));
                }

                foreach (var mod in paramManager.DynamicMods)
                {
                    OnStatusEvent(Utilities.GetModDescription("Dynamic,  ", mod));
                }

                foreach (var mod in paramManager.TerminiMods)
                {
                    OnStatusEvent(Utilities.GetModDescription("Terminus, ", mod));
                }

                Console.WriteLine();
            }

            PsmResultsManager psmResultsManager;

            switch (ascoreOptions.SearchType)
            {
                case AScoreOptions.SearchMode.XTandem:
                    OnStatusEvent("Caching data in " + PathUtils.CompactPathString(ascoreOptions.DbSearchResultsFile, 80));
                    psmResultsManager = new XTandemFHT(ascoreOptions.DbSearchResultsFile);
                    break;

                case AScoreOptions.SearchMode.Sequest:
                    OnStatusEvent("Caching data in " + PathUtils.CompactPathString(ascoreOptions.DbSearchResultsFile, 80));
                    psmResultsManager = new SequestFHT(ascoreOptions.DbSearchResultsFile);
                    break;

                case AScoreOptions.SearchMode.Inspect:
                    OnStatusEvent("Caching data in " + PathUtils.CompactPathString(ascoreOptions.DbSearchResultsFile, 80));
                    psmResultsManager = new InspectFHT(ascoreOptions.DbSearchResultsFile);
                    break;

                case AScoreOptions.SearchMode.Msgfdb:
                case AScoreOptions.SearchMode.Msgfplus:
                    OnStatusEvent("Caching data in " + PathUtils.CompactPathString(ascoreOptions.DbSearchResultsFile, 80));
                    if (ascoreOptions.SearchResultsType == AScoreOptions.DbSearchResultsType.Mzid)
                    {
                        if (ascoreOptions.CreateUpdatedDbSearchResultsFile)
                        {
                            psmResultsManager = new MsgfMzidFull(ascoreOptions.DbSearchResultsFile);
                        }
                        else
                        {
                            psmResultsManager = new MsgfMzid(ascoreOptions.DbSearchResultsFile);
                        }
                    }
                    else
                    {
                        psmResultsManager = new MsgfdbFHT(ascoreOptions.DbSearchResultsFile);
                    }
                    break;

                default:
                    OnErrorEvent(string.Format(
                        "Incorrect search type: {0} , supported values are {1}",
                        ascoreOptions.SearchType,
                        string.Join(", ", Enum.GetNames(typeof(AScoreOptions.SearchMode)))
                        ));
                    return -13;
            }
            var peptideMassCalculator = new PeptideMassCalculator();

            var spectraManager = new SpectraManagerCache(peptideMassCalculator);

            RegisterEvents(spectraManager);

            OnStatusEvent("Output directory: " + ascoreOptions.OutputDirectoryInfo.FullName);

            var ascoreEngine = new AScoreAlgorithm();
            RegisterEvents(ascoreEngine);

            // Initialize the options
            FilterOnMSGFScore = ascoreOptions.FilterOnMSGFScore;

            // Run the algorithm
            if (ascoreOptions.MultiJobMode)
            {
                RunAScoreWithMappingFile(ascoreOptions, spectraManager, psmResultsManager, paramManager);
            }
            else
            {
                spectraManager.OpenFile(ascoreOptions.MassSpecFile, ascoreOptions.ModSummaryFile);

                RunAScoreOnSingleFile(ascoreOptions, spectraManager, psmResultsManager, paramManager);
            }

            OnStatusEvent("AScore Complete");

            if (ascoreOptions.CreateUpdatedDbSearchResultsFile)
            {
                if (ascoreOptions.SearchResultsType == AScoreOptions.DbSearchResultsType.Fht)
                {
                    CreateUpdatedFirstHitsFile(ascoreOptions);
                }
                else if (psmResultsManager is MsgfMzidFull mzidFull)
                {
                    mzidFull.WriteToMzidFile(ascoreOptions.UpdatedDbSearchResultsFileName);
                    OnStatusEvent("Results merged; new file: " + PathUtils.CompactPathString(ascoreOptions.UpdatedDbSearchResultsFileName, 80));
                }
            }

            return 0;
        }

        /// <summary>
        /// Reads the ascore results and merges them into the FHT file
        /// </summary>
        /// <param name="ascoreOptions"></param>
        public void CreateUpdatedFirstHitsFile(AScoreOptions ascoreOptions)
        {
            var resultsMerger = new PHRPResultsMerger();
            RegisterEvents(resultsMerger);

            resultsMerger.MergeResults(ascoreOptions);

            OnStatusEvent("Results merged; new file: " + PathUtils.CompactPathString(resultsMerger.MergedFilePath, 80));
        }

        /// <summary>
        /// Configure and run the AScore algorithm, optionally can add protein mapping information
        /// </summary>
        /// <param name="ascoreOptions"></param>
        /// <param name="spectraManager"></param>
        /// <param name="psmResultsManager"></param>
        /// <param name="ascoreParams"></param>
        public void RunAScoreWithMappingFile(
            AScoreOptions ascoreOptions,
            SpectraManagerCache spectraManager,
            PsmResultsManager psmResultsManager,
            ParameterFileManager ascoreParams)
        {
            var requiredColumns = new List<string>
            {
                "Job",
                "Dataset"
            };

            OnStatusEvent("Reading Job to Dataset Map File: " + PathUtils.CompactPathString(ascoreOptions.JobToDatasetMapFile, 80));

            ReadJobToDatasetMapFile(ascoreOptions, requiredColumns, out var jobToDatasetNameMap);

            RunAScoreOnPreparedData(jobToDatasetNameMap, spectraManager, psmResultsManager, ascoreParams, ascoreOptions, false);

            ProteinMapperTestRun(ascoreOptions);
        }

        private void ReadJobToDatasetMapFile(
            AScoreOptions ascoreOptions,
            IReadOnlyCollection<string> requiredColumns,
            out Dictionary<string, DatasetFileInfo> jobToDatasetNameMap)
        {
            jobToDatasetNameMap = new Dictionary<string, DatasetFileInfo>();

            var columnMap = new Dictionary<string, int>();

            // Read the contents of JobToDatasetMapFile
            using var mapFileReader = new StreamReader(new FileStream(ascoreOptions.JobToDatasetMapFile, FileMode.Open, FileAccess.Read, FileShare.Read));

            var rowNumber = 0;
            var requiredColumnCount = 0;

            while (!mapFileReader.EndOfStream)
            {
                var dataLine = mapFileReader.ReadLine();
                rowNumber++;

                if (string.IsNullOrWhiteSpace(dataLine))
                    continue;

                var dataColumns = dataLine.Split('\t').ToList();

                if (requiredColumnCount == 0)
                {
                    // Parse the headers
                    foreach (var columnName in requiredColumns)
                    {
                        var colIndex = dataColumns.IndexOf(columnName);
                        if (colIndex < 0)
                        {
                            var errorMessage = "JobToDatasetMapFile is missing column " + columnName;
                            OnErrorEvent(errorMessage);
                            throw new Exception(errorMessage);
                        }

                        columnMap.Add(columnName, colIndex);
                    }

                    requiredColumnCount = dataColumns.Count;
                    continue;
                }

                if (dataColumns.Count < requiredColumnCount)
                {
                    OnWarningEvent("Row " + rowNumber + " has fewer than " + columnMap.Count + " columns; skipping this row");
                    continue;
                }

                var job = dataColumns[columnMap["Job"]];
                var spectrumFilePath = dataColumns[columnMap["Dataset"]];

                jobToDatasetNameMap.Add(job, new DatasetFileInfo(spectrumFilePath));
            }
        }

        /// <summary>
        /// Configure and run the AScore algorithm, optionally can add protein mapping information
        /// </summary>
        /// <param name="spectraManager"></param>
        /// <param name="psmResultsManager"></param>
        /// <param name="ascoreParams"></param>
        /// <param name="outputFilePath">Name of the output file</param>
        /// <param name="fastaFilePath">Path to FASTA file. If this is empty/null, protein mapping will not occur</param>
        /// <param name="outputDescriptions">Whether to include protein description line in output or not.</param>
        public void RunAScoreOnSingleFile(
            SpectraManagerCache spectraManager,
            PsmResultsManager psmResultsManager,
            ParameterFileManager ascoreParams,
            string outputFilePath,
            string fastaFilePath = "",
            bool outputDescriptions = false
            )
        {
            var ascoreOptions = new AScoreOptions
            {
                FastaFilePath = fastaFilePath,
                OutputProteinDescriptions = outputDescriptions
            };
            ascoreOptions.SetAScoreResultsFilePath(outputFilePath);

            RunAScoreOnSingleFile(ascoreOptions, spectraManager, psmResultsManager, ascoreParams);
        }

        /// <summary>
        /// Configure and run the AScore algorithm, optionally can add protein mapping information
        /// </summary>
        /// <param name="ascoreOptions"></param>
        /// <param name="spectraManager"></param>
        /// <param name="psmResultsManager"></param>
        /// <param name="ascoreParams"></param>
        public void RunAScoreOnSingleFile(
            AScoreOptions ascoreOptions,
            SpectraManagerCache spectraManager,
            PsmResultsManager psmResultsManager,
            ParameterFileManager ascoreParams)
        {
            var jobToDatasetNameMap = new Dictionary<string, DatasetFileInfo>
            {
                {
                    psmResultsManager.JobNum,
                    new DatasetFileInfo(spectraManager.SpectrumFilePath, spectraManager.ModSummaryFilePath)
                }
            };

            if (spectraManager == null || !spectraManager.Initialized)
                throw new Exception(
                    "spectraManager must be instantiated and initialized before calling RunAScoreOnSingleFile for a single source file");

            RunAScoreOnPreparedData(jobToDatasetNameMap, spectraManager, psmResultsManager, ascoreParams, ascoreOptions, true);

            ProteinMapperTestRun(ascoreOptions);
        }

        private void ProteinMapperTestRun(AScoreOptions ascoreOptions)
        {
            if (string.IsNullOrWhiteSpace(ascoreOptions.FastaFilePath))
                return;

            var proteinMapper = new AScoreProteinMapper(ascoreOptions.AScoreResultsFilePath, ascoreOptions.FastaFilePath, ascoreOptions.OutputProteinDescriptions);
            proteinMapper.Run();
        }

        /// <summary>
        /// Runs the all the tools necessary to perform an ascore run
        /// </summary>
        /// <param name="jobToDatasetNameMap">Keys are job numbers (stored as strings); values are Dataset Names or the path to the _dta.txt file</param>
        /// <param name="spectraManager">Manager for reading _dta.txt or .mzML files; must have already been initialized by the calling class</param>
        /// <param name="psmResultsManager"></param>
        /// <param name="ascoreParams"></param>
        /// <param name="ascoreOptions"></param>
        /// <param name="spectraFileOpened">Set to true if processing a single dataset, and spectraManager.OpenFile() has already been called</param>
        private void RunAScoreOnPreparedData(
            IReadOnlyDictionary<string, DatasetFileInfo> jobToDatasetNameMap,
            SpectraManagerCache spectraManager,
            PsmResultsManager psmResultsManager,
            ParameterFileManager ascoreParams,
            AScoreOptions ascoreOptions,
            bool spectraFileOpened)
        {
            var totalRows = psmResultsManager.GetRowLength();
            var dctPeptidesProcessed = new Dictionary<string, int>();

            if (jobToDatasetNameMap == null || jobToDatasetNameMap.Count == 0)
            {
                const string errorMessage = "Error in AlgorithmRun: jobToDatasetNameMap cannot be null or empty";
                OnErrorEvent(errorMessage);
                throw new ArgumentException(errorMessage);
            }

            ISpectraManager spectraFile = null;
            string spectraManagerCurrentJob = null; // Force open after first read from fht

            var modSummaryManager = new ModSummaryFileManager();
            RegisterEvents(modSummaryManager);

            var peptideMassCalculator = new PeptideMassCalculator();

            if (FilterOnMSGFScore)
            {
                OnStatusEvent("Filtering using MSGF_SpecProb <= " + ascoreParams.MSGFPreFilter.ToString("0.0E+00"));
            }
            Console.WriteLine();

            var statsByType = new int[4];
            var ascoreAlgorithm = new AScoreAlgorithm();
            RegisterEvents(ascoreAlgorithm);

            while (psmResultsManager.CurrentRowNum < totalRows)
            {
                //  Console.Clear();

                if (psmResultsManager.CurrentRowNum % 100 == 0)
                {
                    Console.Write("\rPercent Completion " + Math.Round((double)psmResultsManager.CurrentRowNum / totalRows * 100) + "%");
                }

                int scanNumber;
                int scanCount;
                int chargeState;
                string peptideSeq;
                double msgfScore;

                if (FilterOnMSGFScore)
                {
                    psmResultsManager.GetNextRow(out scanNumber, out scanCount, out chargeState, out peptideSeq, out msgfScore, ref ascoreParams);
                }
                else
                {
                    psmResultsManager.GetNextRow(out scanNumber, out scanCount, out chargeState, out peptideSeq, ref ascoreParams);
                    msgfScore = 1;
                }

                switch (ascoreParams.FragmentType)
                {
                    case FragmentType.CID:
                        statsByType[(int)FragmentType.CID]++;
                        break;
                    case FragmentType.ETD:
                        statsByType[(int)FragmentType.ETD]++;
                        break;
                    case FragmentType.HCD:
                        statsByType[(int)FragmentType.HCD]++;
                        break;
                    default:
                        statsByType[(int)FragmentType.Unspecified]++;
                        break;
                }

                if (string.IsNullOrEmpty(spectraManagerCurrentJob) || !string.Equals(spectraManagerCurrentJob, psmResultsManager.JobNum))
                {
                    // New dataset
                    // Get the correct spectrum file for the match
                    if (!jobToDatasetNameMap.TryGetValue(psmResultsManager.JobNum, out var datasetInfo))
                    {
                        var errorMessage = "Input file refers to job " + psmResultsManager.JobNum +
                                           " but jobToDatasetNameMap does not contain that job; unable to continue";
                        OnWarningEvent(errorMessage);

                        if (!psmResultsManager.JobColumnDefined)
                        {
                            OnWarningEvent(
                                "If the input file includes results from multiple jobs, the first column must be job number with Job as the column heading");
                        }

                        throw new Exception(errorMessage);
                    }

                    var datasetName = GetDatasetName(datasetInfo.SpectrumFilePath);
                    OnStatusEvent("Dataset name: " + datasetName);

                    if (!spectraFileOpened)
                    {
                        // This method was called from RunAScoreWithMappingFile
                        // Open the spectrum file for this dataset
                        spectraFile = spectraManager.GetSpectraManagerForFile(
                            psmResultsManager.PSMResultsFilePath,
                            datasetName,
                            datasetInfo.ModSummaryFilePath);
                    }
                    else
                    {
                        spectraFile = spectraManager.GetCurrentSpectrumManager();
                    }

                    spectraManagerCurrentJob = string.Copy(psmResultsManager.JobNum);
                    Console.Write("\r");

                    if (string.IsNullOrWhiteSpace(datasetInfo.ModSummaryFilePath) && !string.IsNullOrWhiteSpace(ascoreOptions.ModSummaryFile))
                    {
                        datasetInfo.ModSummaryFilePath = ascoreOptions.ModSummaryFile;
                    }

                    if (psmResultsManager is MsgfMzid mzid)
                    {
                        mzid.SetModifications(ascoreParams);
                    }
                    else if (psmResultsManager is MsgfMzidFull mzidFull)
                    {
                        mzidFull.SetModifications(ascoreParams);
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(datasetInfo.ModSummaryFilePath))
                        {
                            modSummaryManager.ReadModSummary(spectraFile.DatasetName, psmResultsManager.PSMResultsFilePath, ascoreParams);
                        }
                        else
                        {
                            var modSummaryFile = new FileInfo(datasetInfo.ModSummaryFilePath);
                            modSummaryManager.ReadModSummary(modSummaryFile, ascoreParams);
                        }
                    }

                    Console.WriteLine();

                    Console.Write("\rPercent Completion " + Math.Round((double)psmResultsManager.CurrentRowNum / totalRows * 100) + "%");
                }

                // perform work on the match
                var peptideParts = peptideSeq.Split('.');
                string sequenceWithoutSuffixOrPrefix;
                string front;
                string back;

                if (peptideParts.Length >= 3)
                {
                    front = peptideParts[0];
                    sequenceWithoutSuffixOrPrefix = peptideParts[1];
                    back = peptideParts[2];
                }
                else
                {
                    front = "?";
                    sequenceWithoutSuffixOrPrefix = string.Copy(peptideSeq);
                    back = "?";
                }

                var sequenceClean = GetCleanSequence(sequenceWithoutSuffixOrPrefix, ref ascoreParams);
                var skipPSM = FilterOnMSGFScore && msgfScore > ascoreParams.MSGFPreFilter;

                var scanChargePeptide = scanNumber + "_" + chargeState + "_" + sequenceWithoutSuffixOrPrefix;
                if (dctPeptidesProcessed.ContainsKey(scanChargePeptide))
                {
                    // We have already processed this PSM
                    skipPSM = true;
                }
                else
                {
                    dctPeptidesProcessed.Add(scanChargePeptide, 0);
                }

                if (skipPSM)
                {
                    psmResultsManager.IncrementRow();
                    continue;
                }

                //Get experimental spectra
                if (spectraFile == null)
                {
                    const string errorMessage = "spectraFile is uninitialized in RunAScoreOnPreparedData; this indicates a programming bug";
                    OnErrorEvent(errorMessage);
                    throw new Exception(errorMessage);
                }

                var expSpec = spectraFile.GetExperimentalSpectra(scanNumber, scanCount, chargeState);

                if (expSpec == null)
                {
                    OnWarningEvent("Scan " + scanNumber + " not found in spectra file for peptide " + peptideSeq);
                    psmResultsManager.IncrementRow();
                    continue;
                }

                // Assume monoisotopic for both hi res and low res spectra
                MolecularWeights.MassType = MassType.Monoisotopic;

                // Compute precursor m/z value
                var precursorMZ = peptideMassCalculator.ConvoluteMass(expSpec.PrecursorMass, 1, chargeState);

                // Set the m/z range
                var mzMax = maxRange;
                var mzMin = precursorMZ * lowRangeMultiplier;

                if (ascoreParams.FragmentType != FragmentType.CID)
                {
                    mzMax = maxRange;
                    mzMin = minRange;
                }

                //Generate all combination mixtures
                var modMixture = new Combinatorics.ModMixtureCombo(ascoreParams.DynamicMods, sequenceClean);

                var myPositionsList = GetMyPositionList(sequenceClean, modMixture);

                //If I have more than 1 modifiable site proceed to calculation
                if (myPositionsList.Count > 1)
                {
                    ascoreAlgorithm.ComputeAScore(psmResultsManager, ascoreParams, scanNumber, chargeState,
                                                  peptideSeq, front, back, sequenceClean, expSpec,
                                                  mzMax, mzMin, myPositionsList);
                }
                else if (myPositionsList.Count == 1)
                {
                    // Either one or no modifiable sites
                    var uniqueID = myPositionsList[0].Max();
                    if (uniqueID == 0)
                        psmResultsManager.WriteToTable(peptideSeq, scanNumber, 0, myPositionsList[0], MOD_INFO_NO_MODIFIED_RESIDUES);
                    else
                        psmResultsManager.WriteToTable(peptideSeq, scanNumber, 0, myPositionsList[0], LookupModInfoByID(uniqueID, ascoreParams.DynamicMods));
                }
                else
                {
                    // No modifiable sites
                    psmResultsManager.WriteToTable(peptideSeq, scanNumber, 0, new int[0], MOD_INFO_NO_MODIFIED_RESIDUES);
                }
                psmResultsManager.IncrementRow();
            }

            Console.WriteLine();

            OnStatusEvent(string.Format("Writing {0:N0} rows to {1}", psmResultsManager.ResultsCount, PathUtils.CompactPathString(ascoreOptions.AScoreResultsFilePath, 80)));
            psmResultsManager.WriteToFile(ascoreOptions.AScoreResultsFilePath);

            Console.WriteLine();

            if (statsByType.Sum() == 0)
            {
                OnWarningEvent("Input file appeared empty");
            }
            else
            {
                OnStatusEvent("Stats by fragmentation ion type:");
                ReportStatsForFragType("  CID", statsByType, FragmentType.CID);
                ReportStatsForFragType("  ETD", statsByType, FragmentType.ETD);
                ReportStatsForFragType("  HCD", statsByType, FragmentType.HCD);
            }

            Console.WriteLine();
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Gets a clean sequence initializes dynamic modifications
        /// </summary>
        /// <param name="seq">input protein sequence including mod characters, but without the prefix or suffix residues</param>
        /// <param name="ascoreParams">ascore parameters reference</param>
        /// <returns>protein sequence without mods as well as changing ascoreParams</returns>
        private string GetCleanSequence(string seq, ref ParameterFileManager ascoreParams)
        {
            foreach (var dynamicMod in ascoreParams.DynamicMods)
            {
                var newSeq = seq.Replace(dynamicMod.ModSymbol.ToString(), string.Empty);
                dynamicMod.Count = seq.Length - newSeq.Length;
                seq = newSeq;
            }
            return seq;
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
        /// Generate the position list for the particular sequence
        /// </summary>
        /// <param name="sequence"></param>
        /// <param name="modMixture"></param>
        /// <returns></returns>
        private List<int[]> GetMyPositionList(string sequence, Combinatorics.ModMixtureCombo modMixture)
        {
            var myPositionsList = new List<int[]>();
            foreach (var combo in modMixture.FinalCombos)
            {
                var myPositions = new int[sequence.Length];
                for (var i = 0; i < combo.Count; i++)
                {
                    myPositions[modMixture.AllSite[i]] = combo[i];
                }
                myPositionsList.Add(myPositions);
            }
            return myPositionsList;
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

        private void ReportStatsForFragType(string fragTypeText, IReadOnlyList<int> statsByType, FragmentType fragmentType)
        {
            OnStatusEvent(string.Format("{0} peptides: {1:N0}", fragTypeText, statsByType[(int)fragmentType]));
        }

        /// <summary>
        /// Get the dataset name from the data file path or dataset name
        /// </summary>
        /// <param name="filePathOrDatasetName"></param>
        /// <returns></returns>
        private string GetDatasetName(string filePathOrDatasetName)
        {
            var suffixes = new List<string> {
                "_dta.txt",
                "_syn.txt",
                "_fht.txt",
                ".mzML",
                ".mzML.gz",
                ".mzXML",
                ".mzid",
                ".mzid.gz"
            };

            var dataFileName = Path.GetFileName(filePathOrDatasetName);
            if (dataFileName == null)
            {
                throw new Exception("Unable to determine the file name of the data file path in GetDatasetName");
            }

            foreach (var suffix in suffixes)
            {
                if (dataFileName.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                {
                    var datasetName = dataFileName.Substring(0, dataFileName.Length - suffix.Length);
                    return Utilities.TrimEnd(datasetName, "_FIXED");
                }
            }

            // No match to any of the standard suffixes
            var baseFileName = Path.GetFileNameWithoutExtension(dataFileName);
            return Utilities.TrimEnd(baseFileName, "_FIXED");
        }

        #endregion
    }
}
