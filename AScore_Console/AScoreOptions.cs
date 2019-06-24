using System;
using System.IO;
using AScore_DLL;
using PRISM;

namespace AScore_Console
{

    public class AScoreOptions : IAScoreOptions
    {
        public const string PROGRAM_DATE = "June 23, 2019";

        [Option("T", Required = true, HelpText = "Search engine result type", HelpShowsDefault = false)]
        public SearchMode SearchType { get; set; }

        [Option("F", Required = true, HelpText = "Path to first-hits file (or mzid file, for MSGF+)", HelpShowsDefault = false)]
        public string DbSearchResultsFile { get; set; }

        [Option("D", HelpText = "Spectra file path (this or -JM is required)", HelpShowsDefault = false)]
        public string MassSpecFile { get; set; }

        [Option("JM", HelpText = "Job-to-dataset map file path (this or -D is required). Use this instead of -D if the FHT file has results from multiple jobs; the map file should have job numbers and dataset names, using columns names Job and Dataset.", HelpShowsDefault = false)]
        public string JobToDatasetMapFile { get; set; }

        [Option("P", Required = true, HelpText = "Parameter file path", HelpShowsDefault = false)]
        public string AScoreParamFile { get; set; }

        [Option("O", HelpText = "Output directory path")]
        public string OutputDirectoryPath { get; set; }

        [Option("L", HelpText = "Log file path")]
        public string LogFilePath { get; set; }

        [Option("FM", Hidden = true, HelpText = "Old parameter - versions prior to February 17, 2017 required the user to specify this to filter on MSGF Score, and the new default is true")]
        public bool FilterOnMSGFScore
        {
            get => !DoNotFilterOnMSGFScore;
            set => DoNotFilterOnMSGFScore = !value;
        }

        [Option("noFM", HelpText = "If specified, filtering on data in column MSGF_SpecProb/MSGF_SpecEValue is disabled")]
        public bool DoNotFilterOnMSGFScore { get; set; }

        [Option("U", HelpText = "Output FHT file name; if set, a copy of the FHT file with updated peptide sequences and additional AScore-related columns will be created")]
        public string UpdatedDbSearchResultsFileName { get; set; }

        public bool CreateUpdatedDbSearchResultsFile { get; private set; }

        [Option("Skip", HelpText = "If specified, will not re-run AScore if a results file already exists")]
        public bool SkipExistingResults { get; set; }

        [Option("Fasta", HelpText = "Fasta file path; if set, Protein Data from the Fasta file will be included in the output")]
        public string FastaFilePath { get; set; }

        [Option("PD", HelpText = "If specified, the Protein Description from the Fasta file will also be included in the output. REQUIRES -Fasta")]
        public bool OutputProteinDescriptions { get; set; }

        public bool MultiJobMode { get; private set; }

        public DbSearchResultsType SearchResultsType { get; private set; }

        public string AScoreResultsFilePath { get; private set; }

        public DirectoryInfo OutputDirectoryInfo { get; private set; }

        public AScoreOptions()
        {
            SearchType = SearchMode.Msgfplus;
            SearchResultsType = DbSearchResultsType.Fht;
            DbSearchResultsFile = string.Empty;
            MassSpecFile = string.Empty;
            JobToDatasetMapFile = string.Empty;
            AScoreParamFile = string.Empty;
            OutputDirectoryPath = ".";
            DoNotFilterOnMSGFScore = false;

            SkipExistingResults = false;
            CreateUpdatedDbSearchResultsFile = false;
            UpdatedDbSearchResultsFileName = string.Empty;

            FastaFilePath = string.Empty;
            OutputProteinDescriptions = false;
            MultiJobMode = false;
            AScoreResultsFilePath = string.Empty;
        }

        public bool Validate(out string errorMessage)
        {
            if (string.IsNullOrWhiteSpace(MassSpecFile) && string.IsNullOrWhiteSpace(JobToDatasetMapFile))
            {
                errorMessage = "ERROR: Must specify -D or -JM!";
                return false;
            }

            if (!string.IsNullOrWhiteSpace(JobToDatasetMapFile))
            {
                MultiJobMode = true;
            }

            if (string.IsNullOrWhiteSpace(OutputDirectoryPath))
            {
                OutputDirectoryPath = ".";
            }

            if (DbSearchResultsFile.ToLower().EndsWith(".mzid") || DbSearchResultsFile.ToLower().EndsWith(".mzid.gz"))
            {
                SearchResultsType = DbSearchResultsType.Mzid;
            }

            // If OutputDirectoryPath points to a file, change it to the parent directory
            var outputDirectoryFile = new FileInfo(OutputDirectoryPath);
            if (outputDirectoryFile.Extension.Length > 1 && outputDirectoryFile.Directory != null && outputDirectoryFile.Directory.Exists)
            {
                OutputDirectoryPath = outputDirectoryFile.Directory.FullName;
            }

            if (!string.IsNullOrWhiteSpace(UpdatedDbSearchResultsFileName))
            {
                CreateUpdatedDbSearchResultsFile = true;
            }

            if (string.IsNullOrWhiteSpace(FastaFilePath))
            {
                FastaFilePath = string.Empty;
            }

            if (OutputProteinDescriptions && string.IsNullOrWhiteSpace(FastaFilePath))
            {
                OutputProteinDescriptions = false;
            }

            if (string.IsNullOrWhiteSpace(LogFilePath))
            {
                LogFilePath = null;
            }

            errorMessage = string.Empty;
            return true;
        }

        /// <summary>
        /// Check the command line input for path errors
        /// </summary>
        /// <returns>Error return code for exit status</returns>
        public int CheckFiles(Action<string> errorReporter)
        {
            if (!File.Exists(AScoreParamFile))
            {
                errorReporter("Input file not found: " + AScoreParamFile);
                return -10;
            }

            if (!string.IsNullOrEmpty(MassSpecFile) && !File.Exists(MassSpecFile))
            {
                errorReporter("Input file not found: " + MassSpecFile);
                return -11;
            }

            if (!string.IsNullOrEmpty(JobToDatasetMapFile) && !File.Exists(JobToDatasetMapFile))
            {
                errorReporter("Input file not found: " + JobToDatasetMapFile);
                return -11;
            }

            if (!File.Exists(DbSearchResultsFile))
            {
                errorReporter("Input file not found: " + DbSearchResultsFile);
                return -12;
            }

            if (!string.IsNullOrEmpty(FastaFilePath) && !File.Exists(FastaFilePath))
            {
                errorReporter("Fasta file not found: " + FastaFilePath);
                return -13;
            }

            return 0;
        }

        /// <summary>
        /// Final evaluation and management of settings
        /// </summary>
        /// <param name="messageReporter"></param>
        /// <returns>true if processing should continue, false if it should be skipped</returns>
        public bool ProcessSettings(Action<string> messageReporter)
        {
            OutputDirectoryInfo = new DirectoryInfo(OutputDirectoryPath);
            if (!OutputDirectoryInfo.Exists)
            {
                try
                {
                    messageReporter("Output directory not found (will auto-create): " + OutputDirectoryInfo.FullName);
                    OutputDirectoryInfo.Create();
                }
                catch (Exception ex)
                {
                    messageReporter("Error creating the output directory: " + ex.Message);
                    OutputDirectoryInfo = new DirectoryInfo(".");
                    messageReporter("Changed output to " + OutputDirectoryInfo.FullName);
                    OutputDirectoryPath = OutputDirectoryInfo.FullName;
                }
            }

            var fhtFileBaseName = Path.GetFileNameWithoutExtension(DbSearchResultsFile);
            if (fhtFileBaseName.ToLower().EndsWith(".mzid"))
            {
                fhtFileBaseName = Path.GetFileNameWithoutExtension(fhtFileBaseName);
            }
            AScoreResultsFilePath = Path.Combine(OutputDirectoryInfo.FullName, fhtFileBaseName + "_ascore.txt");

            if (SkipExistingResults && File.Exists(AScoreResultsFilePath))
            {
                messageReporter("Existing results file found; will not re-create");
                return false;
            }

            return true;
        }
    }
}
