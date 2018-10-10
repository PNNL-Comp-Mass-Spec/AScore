using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AScore_DLL.Managers;
using AScore_DLL.Managers.DatasetManagers;
using AScore_DLL.Managers.SpectraManagers;
using AScore_DLL.Mod;
using PHRPReader;
using PRISM;

namespace AScore_DLL
{
    public class AScoreProcessor : EventNotifier
    {
        public const string MOD_INFO_NO_MODIFIED_RESIDUES = "-";

        private const double lowRangeMultiplier = 0.28;
        private const double maxRange = 2000.0;
        private const double minRange = 50.0;

        private bool m_filterOnMSGFScore = true;

        #region "Properties"

        public bool FilterOnMSGFScore
        {
            get => m_filterOnMSGFScore;
            set => m_filterOnMSGFScore = value;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Configure and run the AScore algorithm
        /// </summary>
        /// <param name="ascoreOptions"></param>
        /// <returns></returns>
        public int RunAScore(IAScoreOptions ascoreOptions)
        {
            var paramManager = new ParameterFileManager(ascoreOptions.AScoreParamFile);
            RegisterEvents(paramManager);

            DatasetManager datasetManager;

            switch (ascoreOptions.SearchType)
            {
                case SearchMode.XTandem:
                    OnStatusEvent("Caching data in " + Path.GetFileName(ascoreOptions.DbSearchResultsFile));
                    datasetManager = new XTandemFHT(ascoreOptions.DbSearchResultsFile);
                    break;
                case SearchMode.Sequest:
                    OnStatusEvent("Caching data in " + Path.GetFileName(ascoreOptions.DbSearchResultsFile));
                    datasetManager = new SequestFHT(ascoreOptions.DbSearchResultsFile);
                    break;
                case SearchMode.Inspect:
                    OnStatusEvent("Caching data in " + Path.GetFileName(ascoreOptions.DbSearchResultsFile));
                    datasetManager = new InspectFHT(ascoreOptions.DbSearchResultsFile);
                    break;
                case SearchMode.Msgfdb:
                case SearchMode.Msgfplus:
                    OnStatusEvent("Caching data in " + Path.GetFileName(ascoreOptions.DbSearchResultsFile));
                    if (ascoreOptions.SearchResultsType == DbSearchResultsType.Mzid)
                    {
                        if (ascoreOptions.CreateUpdatedDbSearchResultsFile)
                        {
                            datasetManager = new MsgfMzidFull(ascoreOptions.DbSearchResultsFile);
                        }
                        else
                        {
                            datasetManager = new MsgfMzid(ascoreOptions.DbSearchResultsFile);
                        }
                    }
                    else
                    {
                        datasetManager = new MsgfdbFHT(ascoreOptions.DbSearchResultsFile);
                    }
                    break;
                default:
                    OnErrorEvent("Incorrect search type: " + ascoreOptions.SearchType + " , supported values are " + string.Join(", ", Enum.GetNames(typeof(SearchMode))));
                    return -13;
            }
            var peptideMassCalculator = new clsPeptideMassCalculator();

            var spectraManager = new SpectraManagerCache(peptideMassCalculator);

            RegisterEvents(spectraManager);

            OnStatusEvent("Output folder: " + ascoreOptions.OutputDirectoryInfo.FullName);

            var ascoreEngine = new AScore_DLL.AScoreAlgorithm();
            RegisterEvents(ascoreEngine);

            // Initialize the options
            FilterOnMSGFScore = ascoreOptions.FilterOnMSGFScore;

            // Run the algorithm
            if (ascoreOptions.MultiJobMode)
            {
                RunAScoreWithMappingFile(ascoreOptions.JobToDatasetMapFile, spectraManager, datasetManager, paramManager, ascoreOptions.AScoreResultsFilePath, ascoreOptions.FastaFilePath, ascoreOptions.OutputProteinDescriptions);
            }
            else
            {
                spectraManager.OpenFile(ascoreOptions.MassSpecFile);

                RunAScoreOnSingleFile(spectraManager, datasetManager, paramManager, ascoreOptions.AScoreResultsFilePath, ascoreOptions.FastaFilePath, ascoreOptions.OutputProteinDescriptions);
            }

            OnStatusEvent("AScore Complete");

            if (ascoreOptions.CreateUpdatedDbSearchResultsFile)
            {
                if (ascoreOptions.SearchResultsType == DbSearchResultsType.Fht)
                {
                    CreateUpdatedFirstHitsFile(ascoreOptions);
                }
                else if (datasetManager is MsgfMzidFull mzidFull)
                {
                    mzidFull.WriteToMzidFile(ascoreOptions.UpdatedDbSearchResultsFileName);
                    OnStatusEvent("Results merged; new file: " + Path.GetFileName(ascoreOptions.UpdatedDbSearchResultsFileName));
                }
            }

            return 0;
        }

        /// <summary>
        /// Reads the ascore results and merges them into the FHT file
        /// </summary>
        /// <param name="ascoreOptions"></param>
        public void CreateUpdatedFirstHitsFile(IAScoreOptions ascoreOptions)
        {
            var resultsMerger = new PHRPResultsMerger();
            RegisterEvents(resultsMerger);

            resultsMerger.MergeResults(ascoreOptions.DbSearchResultsFile, ascoreOptions.AScoreResultsFilePath, ascoreOptions.UpdatedDbSearchResultsFileName);

            OnStatusEvent("Results merged; new file: " + Path.GetFileName(resultsMerger.MergedFilePath));
        }

        /// <summary>
        /// Configure and run the AScore algorithm, optionally can add protein mapping information
        /// </summary>
        /// <param name="jobToDatasetMapFile"></param>
        /// <param name="spectraManager"></param>
        /// <param name="datasetManager"></param>
        /// <param name="ascoreParameters"></param>
        /// <param name="outputFilePath">Name of the output file</param>
        /// <param name="fastaFilePath">Path to FASTA file. If this is empty/null, protein mapping will not occur</param>
        /// <param name="outputDescriptions">Whether to include protein description line in output or not.</param>
        public void RunAScoreWithMappingFile(string jobToDatasetMapFile, SpectraManagerCache spectraManager, DatasetManager datasetManager,
                                 ParameterFileManager ascoreParameters, string outputFilePath, string fastaFilePath = "", bool outputDescriptions = false)
        {
            var jobToDatasetNameMap = new Dictionary<string, string>();

            var lstColumnMapping = new Dictionary<string, int>();
            var lstColumnNames = new List<string>
            {
                "Job",
                "Dataset"
            };

            // Read the contents of JobToDatasetMapFile
            using (var srMapFile = new StreamReader(new FileStream(jobToDatasetMapFile, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                var rowNumber = 0;
                while (srMapFile.Peek() > -1)
                {
                    var dataLine = srMapFile.ReadLine();
                    rowNumber++;

                    if (string.IsNullOrWhiteSpace(dataLine))
                        continue;

                    var dataColumns = dataLine.Split('\t').ToList();

                    if (rowNumber == 1)
                    {
                        // Parse the headers

                        foreach (var columnName in lstColumnNames)
                        {
                            var colIndex = dataColumns.IndexOf(columnName);
                            if (colIndex < 0)
                            {
                                var errorMessage = "JobToDatasetMapFile is missing column " + columnName;
                                OnErrorEvent(errorMessage);
                                throw new Exception(errorMessage);
                            }
                            lstColumnMapping.Add(columnName, colIndex);
                        }
                        continue;
                    }

                    if (dataColumns.Count < lstColumnMapping.Count)
                    {
                        OnWarningEvent("Row " + rowNumber + " has fewer than " + lstColumnMapping.Count + " columns; skipping this row");
                        continue;
                    }

                    var job = dataColumns[lstColumnMapping["Job"]];
                    var dataset = dataColumns[lstColumnMapping["Dataset"]];

                    jobToDatasetNameMap.Add(job, dataset);
                }
            }

            RunAScoreOnPreparedData(jobToDatasetNameMap, spectraManager, datasetManager, ascoreParameters, outputFilePath);

            ProteinMapperTestRun(outputFilePath, fastaFilePath, outputDescriptions);
        }

        /// <summary>
        /// Configure and run the AScore algorithm, optionally can add protein mapping information
        /// </summary>
        /// <param name="spectraManager"></param>
        /// <param name="datasetManager"></param>
        /// <param name="ascoreParameters"></param>
        /// <param name="outputFilePath">Name of the output file</param>
        /// <param name="fastaFilePath">Path to FASTA file. If this is empty/null, protein mapping will not occur</param>
        /// <param name="outputDescriptions">Whether to include protein description line in output or not.</param>
        public void RunAScoreOnSingleFile(SpectraManagerCache spectraManager, DatasetManager datasetManager,
                                 ParameterFileManager ascoreParameters, string outputFilePath, string fastaFilePath = "", bool outputDescriptions = false)
        {
            var jobToDatasetNameMap = new Dictionary<string, string>
            {
                {datasetManager.JobNum, spectraManager.DatasetName}
            };

            if (spectraManager == null || !spectraManager.Initialized)
                throw new Exception(
                    "spectraManager must be instantiated and initialized before calling RunAScoreOnSingleFile for a single source file");

            RunAScoreOnPreparedData(jobToDatasetNameMap, spectraManager, datasetManager, ascoreParameters, outputFilePath);

            ProteinMapperTestRun(outputFilePath, fastaFilePath, outputDescriptions);
        }

        private void ProteinMapperTestRun(string outputFilePath, string fastaFilePath, bool outputDescriptions)
        {
            if (!string.IsNullOrWhiteSpace(fastaFilePath))
            {
                var mapProteins = new AScoreProteinMapper(outputFilePath, fastaFilePath, outputDescriptions);
                mapProteins.Run();
            }
        }

        /// <summary>
        /// Runs the all the tools necessary to perform an ascore run
        /// </summary>
        /// <param name="jobToDatasetNameMap">Keys are job numbers (stored as strings); values are Dataset Names or the path to the _dta.txt file</param>
        /// <param name="spectraManager">DtaManager, which the calling class must have already initialized</param>
        /// <param name="datasetManager"></param>
        /// <param name="ascoreParameters"></param>
        /// <param name="outputFilePath"></param>
        private void RunAScoreOnPreparedData(
            IReadOnlyDictionary<string, string> jobToDatasetNameMap,
            SpectraManagerCache spectraManager,
            DatasetManager datasetManager,
            ParameterFileManager ascoreParameters,
            string outputFilePath)
        {
            var totalRows = datasetManager.GetRowLength();
            var dctPeptidesProcessed = new Dictionary<string, int>();

            if (jobToDatasetNameMap == null || jobToDatasetNameMap.Count == 0)
            {
                const string errorMessage = "Error in AlgorithmRun: jobToDatasetNameMap cannot be null or empty";
                OnErrorEvent(errorMessage);
                throw new ArgumentException(errorMessage);
            }

            string spectraManagerCurrentJob = null; // Force open after first read from fht

            var modSummaryManager = new ModSummaryFileManager();
            RegisterEvents(modSummaryManager);

            var peptideMassCalculator = new PHRPReader.clsPeptideMassCalculator();

            ISpectraManager spectraFile = new DtaManager(peptideMassCalculator);

            if (FilterOnMSGFScore)
            {
                OnStatusEvent("Filtering using MSGF_SpecProb <= " + ascoreParameters.MSGFPreFilter.ToString("0.0E+00"));
            }
            Console.WriteLine();

            var statsByType = new int[4];
            var ascoreAlgorithm = new AScoreAlgorithm();
            RegisterEvents(ascoreAlgorithm);

            while (datasetManager.CurrentRowNum < totalRows)
            {
                //  Console.Clear();

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

                switch (ascoreParameters.FragmentType)
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

                if (string.IsNullOrEmpty(spectraManagerCurrentJob) || !string.Equals(spectraManagerCurrentJob, datasetManager.JobNum))
                {
                    // New dataset
                    // Get the correct dta file for the match
                    if (!jobToDatasetNameMap.TryGetValue(datasetManager.JobNum, out var datasetName))
                    {
                        var errorMessage = "Input file refers to job " + datasetManager.JobNum +
                                              " but jobToDatasetNameMap does not contain that job; unable to continue";
                        OnErrorEvent(errorMessage);
                        throw new Exception(errorMessage);
                    }

                    datasetName = GetDatasetName(datasetName);

                    spectraFile = spectraManager.GetSpectraManagerForFile(datasetManager.DatasetFilePath, datasetName);

                    spectraManagerCurrentJob = string.Copy(datasetManager.JobNum);
                    Console.Write("\r");

                    if (datasetManager is MsgfMzid mzid)
                    {
                        mzid.SetModifications(ascoreParameters);
                    }
                    else if (datasetManager is MsgfMzidFull mzidFull)
                    {
                        mzidFull.SetModifications(ascoreParameters);
                    }
                    else
                    {
                        modSummaryManager.ReadModSummary(spectraFile.DatasetName, datasetManager.DatasetFilePath, ascoreParameters);
                    }

                    Console.Write("\rPercent Completion " + Math.Round((double)datasetManager.CurrentRowNum / totalRows * 100) + "%");
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

                var sequenceClean = GetCleanSequence(sequenceWithoutSuffixOrPrefix, ref ascoreParameters);
                var skipPSM = m_filterOnMSGFScore && msgfScore > ascoreParameters.MSGFPreFilter;

                var scanChargePeptide = scanNumber + "_" + chargeState + "_" + sequenceWithoutSuffixOrPrefix;
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
                var expSpec = spectraFile.GetExperimentalSpectra(scanNumber, scanCount, chargeState);

                if (expSpec == null)
                {
                    OnWarningEvent("Scan " + scanNumber + " not found in spectra file for peptide " + peptideSeq);
                    datasetManager.IncrementRow();
                    continue;
                }

                // Assume monoisotopic for both hi res and low res spectra
                MolecularWeights.MassType = MassType.Monoisotopic;

                // Compute precursor m/z value
                var precursorMZ = peptideMassCalculator.ConvoluteMass(expSpec.PrecursorMass, 1, chargeState);

                // Set the m/z range
                var mzMax = maxRange;
                var mzMin = precursorMZ * lowRangeMultiplier;

                if (ascoreParameters.FragmentType != FragmentType.CID)
                {
                    mzMax = maxRange;
                    mzMin = minRange;
                }

                //Generate all combination mixtures
                var modMixture = new Combinatorics.ModMixtureCombo(ascoreParameters.DynamicMods, sequenceClean);

                var myPositionsList = GetMyPositionList(sequenceClean, modMixture);

                //If I have more than 1 modifiable site proceed to calculation
                if (myPositionsList.Count > 1)
                {
                    ascoreAlgorithm.ComputeAScore(datasetManager, ascoreParameters, scanNumber, chargeState,
                                                  peptideSeq, front, back, sequenceClean, expSpec,
                                                  mzMax, mzMin, myPositionsList);
                }
                else if (myPositionsList.Count == 1)
                {
                    // Either one or no modifiable sites
                    var uniqueID = myPositionsList[0].Max();
                    if (uniqueID == 0)
                        datasetManager.WriteToTable(peptideSeq, scanNumber, 0, myPositionsList[0], MOD_INFO_NO_MODIFIED_RESIDUES);
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

            Console.WriteLine();

            OnStatusEvent("Writing " + datasetManager.ResultsCount + " rows to " + Path.GetFileName(outputFilePath));
            datasetManager.WriteToFile(outputFilePath);

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
        /// <param name="ascoreParameters">ascore parameters reference</param>
        /// <returns>protein sequence without mods as well as changing ascoreParameters</returns>
        private string GetCleanSequence(string seq, ref ParameterFileManager ascoreParameters)
        {
            foreach (var dynamicMod in ascoreParameters.DynamicMods)
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
            OnStatusEvent(fragTypeText + " peptides: " + statsByType[(int)fragmentType]);
        }

        private string GetDatasetName(string dataFilepath)
        {
            var suffixes = new List<string> {
                "_dta.txt",
                "_syn.txt",
                "_fht.txt",
                ".mzML",
                ".mzXML"};

            var dataFileName = Path.GetFileName(dataFilepath);
            if (dataFileName == null)
            {
                throw new Exception("Unable to determine the file name of the data file path in GetDatasetName");
            }

            foreach (var suffix in suffixes)
            {
                if (dataFileName.ToLower().EndsWith(suffix.ToLower()))
                {
                    return dataFileName.Substring(0, dataFileName.Length - suffix.Length);
                }
            }

            return Path.GetFileNameWithoutExtension(dataFileName);
        }

        #endregion
    }
}
