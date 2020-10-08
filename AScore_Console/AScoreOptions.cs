using System;
using System.IO;
using AScore_DLL;
using PRISM;

namespace AScore_Console
{
    public class AScoreOptions : IAScoreOptions
    {
        public const string PROGRAM_DATE = "October 8, 2020";

        [Option("T", "ResultType", Required = true, HelpText = "Search engine result type", HelpShowsDefault = false)]
        public SearchMode SearchType { get; set; }

        [Option("F", "PSMResultsFile", Required = true, HelpText = "Path to PHRP first-hits file, PHRP synopsis file, or .mzid file", HelpShowsDefault = false)]
        public string DbSearchResultsFile { get; set; }

        [Option("D", "DatasetFile", "SpectrumFile", HelpText = "Spectrum file path (.mzML, .mzML.gz, or _dta.txt); either -D or -JM must be provided", HelpShowsDefault = false)]
        public string MassSpecFile { get; set; }

        [Option("JM", "JobToDatasetMapFile", HelpText = "Job-to-dataset map file path (this or -D is required). Use this instead of -D if the input FHT or Syn file has results from multiple jobs; the map file should have job numbers and dataset names, using columns named Job and Dataset.", HelpShowsDefault = false)]
        public string JobToDatasetMapFile { get; set; }

        [Option("P", "AScoreParamFile", Required = true, HelpText = "AScore-specific parameter file, specifying HCDMassTolerance and MSGFPreFilter (an example file is at https://github.com/PNNL-Comp-Mass-Spec/AScore/blob/master/AScore_Console/Parameter_Files/AScore_CID_0.5Da_ETD_0.5Da_HCD_0.05Da.xml)", HelpShowsDefault = false)]
        public string AScoreParamFile { get; set; }

        [Option("O", "OutputDirectory", HelpText = "Output directory path")]
        public string OutputDirectoryPath { get; set; }

        [Option("L", "LogFile", HelpText = "Log file path")]
        public string LogFilePath { get; set; }

        [Option("FM", Hidden = true, HelpText = "Old parameter - versions prior to February 17, 2017 required the user to specify this to filter on MSGF Score, and the new default is true")]
        public bool FilterOnMSGFScore
        {
            get => !DoNotFilterOnMSGFScore;
            set => DoNotFilterOnMSGFScore = !value;
        }

        [Option("noFM", "DoNotFilterOnMSGFScore", HelpText = "If specified, filtering on data in column MSGF_SpecProb/MSGF_SpecEValue is disabled")]
        public bool DoNotFilterOnMSGFScore { get; set; }

        [Option("U", HelpText = "Output FHT or Syn  file name; if set, a copy of the input FHT or Syn  file with updated peptide sequences and additional AScore-related columns will be created")]
        public string UpdatedDbSearchResultsFileName { get; set; }

        public bool CreateUpdatedDbSearchResultsFile { get; private set; }

        [Option("Skip", "SkipExistingResults", HelpText = "If specified, will not re-run AScore if a results file already exists")]
        public bool SkipExistingResults { get; set; }

        [Option("Fasta", "FastaFile", HelpText = "Fasta file path; if set, Protein Data from the Fasta file will be included in the output")]
        public string FastaFilePath { get; set; }

        [Option("PD", "OutputProteinDescriptions", HelpText = "If specified, the Protein Description from the Fasta file will also be included in the output. REQUIRES -Fasta")]
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

            if (DbSearchResultsFile.EndsWith(".mzid", StringComparison.OrdinalIgnoreCase) ||
                DbSearchResultsFile.EndsWith(".mzid.gz", StringComparison.OrdinalIgnoreCase))
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
                errorReporter(GetInputFileNotFoundMessage("Input file not found: " + AScoreParamFile));
                return -10;
            }

            if (!string.IsNullOrEmpty(MassSpecFile) && !File.Exists(MassSpecFile))
            {
                errorReporter(GetInputFileNotFoundMessage(MassSpecFile));
                return -11;
            }

            if (!string.IsNullOrEmpty(JobToDatasetMapFile) && !File.Exists(JobToDatasetMapFile))
            {
                errorReporter(GetInputFileNotFoundMessage(JobToDatasetMapFile));
                return -11;
            }

            if (!File.Exists(DbSearchResultsFile))
            {
                errorReporter(GetInputFileNotFoundMessage(DbSearchResultsFile));
                return -12;
            }

            if (!string.IsNullOrEmpty(FastaFilePath) && !File.Exists(FastaFilePath))
            {
                errorReporter(GetInputFileNotFoundMessage(FastaFilePath, "Fasta file"));
                return -13;
            }

            return 0;
        }

        private string GetInputFileNotFoundMessage(string filePath, string fileDescription = "Input file")
        {
            try
            {
                var inputFile = new FileInfo(filePath);
                var fullFilePath =
                    string.Equals(filePath, inputFile.FullName, StringComparison.OrdinalIgnoreCase) ?
                    string.Empty :
                    ";\n" + inputFile.FullName;

                return string.Format("{0} not found: {1}{2}", fileDescription, filePath, fullFilePath);
            }
            catch (Exception)
            {
                return string.Format("Input file not found: ", filePath ?? "?UnknownFile?");
            }
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
            if (fhtFileBaseName.EndsWith(".mzid", StringComparison.OrdinalIgnoreCase))
            {
                fhtFileBaseName = Path.GetFileNameWithoutExtension(fhtFileBaseName);
            }
            AScoreResultsFilePath = Path.Combine(OutputDirectoryInfo.FullName, fhtFileBaseName + "_ascore.txt");

            if (SkipExistingResults && File.Exists(AScoreResultsFilePath))
            {
                messageReporter("Existing results file found; will not re-create " + PathUtils.CompactPathString(AScoreResultsFilePath, 80));
                return false;
            }

            return true;
        }
    }
}
