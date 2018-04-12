using System;
using System.IO;
using PRISM;

namespace AScore_Console
{
    public enum SearchMode
    {
        Sequest,
        XTandem,
        Inspect,
        Msgfdb,
        Msgfplus
    }

    public class AScoreOptions
    {
        [Option("T", Required = true, HelpText = "Search engine result type", HelpShowsDefault = false)]
        public SearchMode SearchType { get; set; }

        [Option("F", Required = true, HelpText = "Path to first-hits file (or mzid file, for MSGF+)", HelpShowsDefault = false)]
        public string FirstHitsFile { get; set; }

        [Option("D", HelpText = "Spectra file path (this or -JM is required)", HelpShowsDefault = false)]
        public string CDtaFile { get; set; }

        [Option("JM", HelpText = "Job-to-dataset map file path (this or -D is required). Use this instead of -D if the FHT file has results from multiple jobs; the map file should have job numbers and dataset names, using columns names Job and Dataset.", HelpShowsDefault = false)]
        public string JobToDatasetMapFile { get; set; }

        [Option("P", Required = true, HelpText = "Parameter file path", HelpShowsDefault = false)]
        public string AScoreParamFile { get; set; }

        [Option("O", HelpText = "Output folder path")]
        public string OutputFolderPath { get; set; }

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
        public string UpdatedFirstHitsFileName { get; set; }

        public bool CreateUpdatedFirstHitsFile { get; private set; }

        [Option("Skip", HelpText = "If specified, will not re-run AScore if a results file already exists")]
        public bool SkipExistingResults { get; set; }


        [Option("Fasta", HelpText = "Fasta file path; if set, Protein Data from the Fasta file will be included in the output")]
        public string FastaFilePath { get; set; }

        [Option("PD", HelpText = "If specified, the Protein Description from the Fasta file will also be included in the output. REQUIRES -Fasta")]
        public bool OutputProteinDescriptions { get; set; }

        public bool MultiJobMode { get; private set; }

        public AScoreOptions()
        {
            SearchType = SearchMode.Msgfplus;
            FirstHitsFile = string.Empty;
            CDtaFile = string.Empty;
            JobToDatasetMapFile = string.Empty;
            AScoreParamFile = string.Empty;
            OutputFolderPath = ".";
            DoNotFilterOnMSGFScore = false;

            SkipExistingResults = false;
            CreateUpdatedFirstHitsFile = false;
            UpdatedFirstHitsFileName = string.Empty;

            FastaFilePath = string.Empty;
            OutputProteinDescriptions = false;
            MultiJobMode = false;
        }

        public bool Validate()
        {
            if (string.IsNullOrWhiteSpace(CDtaFile) && string.IsNullOrWhiteSpace(JobToDatasetMapFile))
            {
                Console.WriteLine("ERROR: Must specify -D or -JM!");
                return false;
            }

            if (!string.IsNullOrWhiteSpace(JobToDatasetMapFile))
            {
                MultiJobMode = true;
            }

            if (string.IsNullOrWhiteSpace(OutputFolderPath))
            {
                OutputFolderPath = ".";
            }

            // If OutputFolderPath points to a file, change it to the parent folder
            var outputFolderPathFile = new FileInfo(OutputFolderPath);
            if (outputFolderPathFile.Extension.Length > 1 && outputFolderPathFile.Directory != null && outputFolderPathFile.Directory.Exists)
            {
                OutputFolderPath = outputFolderPathFile.Directory.FullName;
            }

            if (!string.IsNullOrWhiteSpace(UpdatedFirstHitsFileName))
            {
                CreateUpdatedFirstHitsFile = true;
            }

            if (string.IsNullOrWhiteSpace(FastaFilePath))
            {
                FastaFilePath = "";
            }

            if (OutputProteinDescriptions && string.IsNullOrWhiteSpace(FastaFilePath))
            {
                OutputProteinDescriptions = false;
            }

            if (string.IsNullOrWhiteSpace(LogFilePath))
            {
                LogFilePath = null;
            }

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

            if (!string.IsNullOrEmpty(CDtaFile) && !File.Exists(CDtaFile))
            {
                errorReporter("Input file not found: " + CDtaFile);
                return -11;
            }

            if (!string.IsNullOrEmpty(JobToDatasetMapFile) && !File.Exists(JobToDatasetMapFile))
            {
                errorReporter("Input file not found: " + JobToDatasetMapFile);
                return -11;
            }

            if (!File.Exists(FirstHitsFile))
            {
                errorReporter("Input file not found: " + FirstHitsFile);
                return -12;
            }

            if (!string.IsNullOrEmpty(FastaFilePath) && !File.Exists(FastaFilePath))
            {
                errorReporter("Fasta file not found: " + FastaFilePath);
                return -13;
            }

            return 0;
        }
    }
}
