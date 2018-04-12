using System;
using AScore_DLL.Managers;
using AScore_DLL.Managers.DatasetManagers;
using AScore_DLL.Managers.SpectraManagers;
using System.IO;
using PHRPReader;
using PRISM;

namespace AScore_Console
{
    class Program
    {
        static StreamWriter mLogFile;

        struct AScoreOptionsType
        {
            public string SearchType;
            public string FirstHitsFile;
            public string CDtaFile;
            public string JobToDatasetMapFile;
            public string AScoreParamFile;
            public string OutputFolderPath;
            public bool FilterOnMSGFScore;

            public bool SkipExistingResults;
            public bool CreateUpdatedFirstHitsFile;
            public string UpdatedFirstHitsFileName;

            public string FastaFilePath;
            public bool OutputProteinDescriptions;

            public void Initialize()
            {
                SearchType = string.Empty;
                FirstHitsFile = string.Empty;
                CDtaFile = string.Empty;
                JobToDatasetMapFile = string.Empty;
                AScoreParamFile = string.Empty;
                OutputFolderPath = string.Empty;
                FilterOnMSGFScore = true;

                SkipExistingResults = false;
                CreateUpdatedFirstHitsFile = false;
                UpdatedFirstHitsFileName = string.Empty;

                FastaFilePath = string.Empty;
                OutputProteinDescriptions = false;
            }
        }

        private static AScoreOptionsType mAScoreOptions;
        static bool mMultiJobMode;
        static string mLogFilePath = string.Empty;
        const string SupportedSearchModes = "sequest, xtandem, inspect, msgfdb, or msgfplus";

        /// <summary>
        /// Main entry point
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        static int Main(string[] args)
        {
#if (!DEBUG)
            try
            {
#endif
            var clu = new clsParseCommandLine();

            mAScoreOptions.Initialize();
            mLogFilePath = string.Empty;

            var syntaxError = string.Empty;

            if (clu.ParseCommandLine(clsParseCommandLine.ALTERNATE_SWITCH_CHAR))
            {
                ProcessCommandLine(clu, ref syntaxError);
            }

            Console.WriteLine();

            if (args.Length == 0 || clu.NeedToShowHelp || syntaxError.Length > 0)
            {
                if (syntaxError.Length > 0)
                {
                    ShowError("Error, " + syntaxError);
                    Console.WriteLine();
                }

                PrintHelp();

                clsParseCommandLine.PauseAtConsole(750, 250);
                return 0;
            }

            var returnCode = CheckParameters();
            // If we encountered an error in the input - a necessary file does not exist - then exit.
            if (returnCode != 0)
            {
                return returnCode;
            }

            returnCode = RunAScore(mAScoreOptions, mLogFilePath, SupportedSearchModes, mMultiJobMode);

            if (returnCode != 0)
            {
                clsParseCommandLine.PauseAtConsole(2000, 333);
            }
            else
            {
                clsParseCommandLine.PauseAtConsole(1000, 250);
            }
#if (!DEBUG)
            }
            catch (Exception ex)
            {
                Console.WriteLine();
                ShowError("Program failure, possibly incorrect search engine type; " + ex.Message, ex);

                return ex.Message.GetHashCode();
            }
            finally
            {
                mLogFile?.Close();
            }
#endif

            return 0;
        }

        /// <summary>
        /// Process the command line input
        /// </summary>
        /// <param name="clu"></param>
        /// <param name="syntaxError"></param>
        private static void ProcessCommandLine(clsParseCommandLine clu, ref string syntaxError)
        {
            if (!clu.RetrieveValueForParameter("T", out mAScoreOptions.SearchType, false))
                syntaxError = "-T:Search_Engine not defined";

            if (!clu.RetrieveValueForParameter("F", out mAScoreOptions.FirstHitsFile, false))
                syntaxError = "-F:fht_file_path not defined";

            if (clu.RetrieveValueForParameter("JM", out mAScoreOptions.JobToDatasetMapFile, false))
                mMultiJobMode = true;
            else
            {
                if (!clu.RetrieveValueForParameter("D", out mAScoreOptions.CDtaFile, false))
                    syntaxError = "Must use -D:spectra_file_path or -JM:job_to_dataset_mapfile_path";
            }

            if (!clu.RetrieveValueForParameter("P", out mAScoreOptions.AScoreParamFile, false))
                syntaxError = "-P:parameter_file not defined";

            if (!clu.RetrieveValueForParameter("O", out mAScoreOptions.OutputFolderPath, false) || string.IsNullOrWhiteSpace(mAScoreOptions.OutputFolderPath))
                mAScoreOptions.OutputFolderPath = ".";

            // If ascoreOptions.OutputFolderPath points to a file, change it to the parent folder
            var fiOutputFolderAsFile = new FileInfo(mAScoreOptions.OutputFolderPath);
            if (fiOutputFolderAsFile.Extension.Length > 1 && fiOutputFolderAsFile.Directory != null && fiOutputFolderAsFile.Directory.Exists)
            {
                mAScoreOptions.OutputFolderPath = fiOutputFolderAsFile.Directory.FullName;
            }

            // Deprecating starting February 17, 2015; default is true, and the "noFM" switch is all that is needed.
            if (clu.RetrieveValueForParameter("FM", out var outValue, false))
            {
                if (!bool.TryParse(outValue, out mAScoreOptions.FilterOnMSGFScore))
                {
                    ShowWarning("Warning: '-FM:" + outValue + "' not recognized. Assuming '-FM:true'.");
                    //syntaxError = "specify true or false for -FM; not -FM:" + outValue;
                    // Reset it to true, bool.TryParse failed and set it to false.
                    mAScoreOptions.FilterOnMSGFScore = true;
                }
            }

            // If the switch is present, disable filtering on MSGF Score
            if (clu.IsParameterPresent("noFM"))
            {
                mAScoreOptions.FilterOnMSGFScore = false;
            }

            if (clu.RetrieveValueForParameter("L", out outValue, false))
                mLogFilePath = string.Copy(outValue);

            if (clu.RetrieveValueForParameter("U", out outValue, false))
            {
                mAScoreOptions.CreateUpdatedFirstHitsFile = true;
                if (!string.IsNullOrWhiteSpace(outValue))
                    mAScoreOptions.UpdatedFirstHitsFileName = string.Copy(outValue);
            }

            if (clu.RetrieveValueForParameter("Skip", out outValue, false))
            {
                mAScoreOptions.SkipExistingResults = true;
            }

            if (!clu.RetrieveValueForParameter("Fasta", out mAScoreOptions.FastaFilePath, false) ||
                string.IsNullOrWhiteSpace(mAScoreOptions.FastaFilePath))
            {
                mAScoreOptions.FastaFilePath = string.Empty;
            }

            if (clu.RetrieveValueForParameter("PD", out outValue, false) && !string.IsNullOrEmpty(mAScoreOptions.FastaFilePath))
            {
                mAScoreOptions.OutputProteinDescriptions = true;
            }
        }

        /// <summary>
        /// Output the help for the program
        /// </summary>
        private static void PrintHelp()
        {
            Console.WriteLine("Parameters for running AScore include:");
            Console.WriteLine(" -T:search_engine");
            Console.WriteLine("   (allowed values are " + SupportedSearchModes + ")");
            Console.WriteLine(" -F:fht_file_path");
            Console.WriteLine(" -D:spectra_file_path");
            Console.WriteLine("   (_dta.txt or .mzML)");
            Console.WriteLine(" -JM:job_to_dataset_mapfile_path");
            Console.WriteLine("   (use -JM instead of -D if the FHT file has results from");
            Console.WriteLine("    multiple jobs; the map file should have job numbers and");
            Console.WriteLine("    dataset names, using column names Job and Dataset)");
            Console.WriteLine(" -P:parameter_file_path");
            Console.WriteLine(" -O:output_folder_path");
            Console.WriteLine(" -L:log_file_path");

            Console.WriteLine(" -noFM   (disable filtering on data in column MSGF_SpecProb; default is enabled)");
            Console.WriteLine(" -U:updated_fht_file_name");
            Console.WriteLine("   (create a copy of the fht_file with updated peptide");
            Console.WriteLine("    sequences plus new AScore-related columns");
            Console.WriteLine(" -Skip     (will not re-run AScore if an existing");
            Console.WriteLine("            results file already exists)");
            Console.WriteLine(" -Fasta:Fasta_file_path");
            Console.WriteLine("             (add Protein Data from Fasta_file to the output)");
            Console.WriteLine(" -PD       (Include Protein Description in output;)");
            Console.WriteLine("        REQUIRES -Fasta:Fasta_file_path)");
            Console.WriteLine();
            Console.WriteLine("Example command line #1:");
            Console.WriteLine(Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location) +
                              " -T:sequest\n" +
                              " -F:\"C:\\Temp\\DatasetName_fht.txt\"\n" +
                              " -D:\"C:\\Temp\\DatasetName_dta.txt\"\n" +
                              " -O:\"C:\\Temp\"\n" +
                              " -P:C:\\Temp\\DynMetOx_stat_4plex_iodo_hcd.xml\n" +
                              " -L:LogFile.txt");
            Console.WriteLine();
            Console.WriteLine("Example command line #2:");
            Console.WriteLine(Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location) +
                              " -T:msgfplus\n" +
                              " -F:Dataset_W_S2_Fr_04_2May17_msgfplus_syn.txt\n" +
                              " -D:Dataset_W_S2_Fr_04_2May17.mzML\n" +
                              " -P:AScore_CID_0.5Da_ETD_0.5Da_HCD_0.05Da.xml\n" +
                              " -U:W_S2_Fr_04_2May17_msgfplus_syn_plus_ascore.txt\n" +
                              " -L:LogFile.txt");
            Console.WriteLine();
            Console.WriteLine("Example command line #3:");
            Console.WriteLine(Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location) +
                              " -T:msgfplus\n" +
                              " -F:C:\\Temp\\Multi_Job_Results_fht.txt\n" +
                              " -JM:C:\\Temp\\JobToDatasetNameMap.txt\n" +
                              " -O:C:\\Temp\\\n" +
                              " -P:C:\\Temp\\DynPhos_stat_6plex_iodo_hcd.xml\n" +
                              " -L:LogFile.txt\n" +
                              " -noFM");
            Console.WriteLine();
            Console.WriteLine("Example command line #4:");
            Console.WriteLine(Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location) +
                              " -T:msgfplus\n" +
                              " -F:C:\\Temp\\Multi_Job_Results_fht.txt\n" +
                              " -JM:C:\\Temp\\JobToDatasetNameMap.txt\n" +
                              " -O:C:\\Temp\\\n" +
                              " -P:C:\\Temp\\DynPhos_stat_6plex_iodo_hcd.xml\n" +
                              " -L:LogFile.txt\n" +
                              " -Fasta:C:\\Temp\\H_sapiens_Uniprot_SPROT_2013-09-18.fasta\n" +
                              " -PD");
        }

        /// <summary>
        /// Check the command line input for path errors
        /// </summary>
        /// <returns>Error return code for exit status</returns>
        private static int CheckParameters()
        {
            if (!File.Exists(mAScoreOptions.AScoreParamFile))
            {
                ShowError("Input file not found: " + mAScoreOptions.AScoreParamFile);
                clsParseCommandLine.PauseAtConsole(2000, 333);
                return -10;
            }

            if (!string.IsNullOrEmpty(mAScoreOptions.CDtaFile) && !File.Exists(mAScoreOptions.CDtaFile))
            {
                ShowError("Input file not found: " + mAScoreOptions.CDtaFile);
                clsParseCommandLine.PauseAtConsole(2000, 333);
                return -11;
            }

            if (!string.IsNullOrEmpty(mAScoreOptions.JobToDatasetMapFile) && !File.Exists(mAScoreOptions.JobToDatasetMapFile))
            {
                ShowError("Input file not found: " + mAScoreOptions.JobToDatasetMapFile);
                clsParseCommandLine.PauseAtConsole(2000, 333);
                return -11;
            }

            if (!File.Exists(mAScoreOptions.FirstHitsFile))
            {
                ShowError("Input file not found: " + mAScoreOptions.FirstHitsFile);
                clsParseCommandLine.PauseAtConsole(2000, 333);
                return -12;
            }

            if (!string.IsNullOrEmpty(mAScoreOptions.FastaFilePath) && !File.Exists(mAScoreOptions.FastaFilePath))
            {
                ShowError("Fasta file not found: " + mAScoreOptions.FastaFilePath);
                clsParseCommandLine.PauseAtConsole(2000, 333);
                return -13;
            }
            return 0;
        }

        /// <summary>
        /// Configure and run the AScore DLL
        /// </summary>
        /// <param name="ascoreOptions"></param>
        /// <param name="logFilePath"></param>
        /// <param name="supportedSearchModes"></param>
        /// <param name="multiJobMode"></param>
        /// <returns></returns>
        private static int RunAScore(AScoreOptionsType ascoreOptions, string logFilePath, string supportedSearchModes, bool multiJobMode)
        {
            if (!string.IsNullOrWhiteSpace(logFilePath))
            {
                mLogFile = new StreamWriter(new FileStream(logFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
                {
                    AutoFlush = true
                };
            }

            var diOutputFolder = new DirectoryInfo(ascoreOptions.OutputFolderPath);
            if (!diOutputFolder.Exists)
            {
                try
                {
                    ShowMessage("Output folder not found (will auto-create): " + diOutputFolder.FullName);
                    diOutputFolder.Create();
                }
                catch (Exception ex)
                {
                    ShowMessage("Error creating the output folder: " + ex.Message);
                    diOutputFolder = new DirectoryInfo(".");
                    ShowMessage("Changed output to " + diOutputFolder.FullName);
                }
            }

            var fhtFileBaseName = Path.GetFileNameWithoutExtension(ascoreOptions.FirstHitsFile);
            if (fhtFileBaseName.ToLower().EndsWith(".mzid"))
            {
                fhtFileBaseName = Path.GetFileNameWithoutExtension(fhtFileBaseName);
            }
            var ascoreResultsFilePath = Path.Combine(diOutputFolder.FullName, fhtFileBaseName + "_ascore.txt");

            if (ascoreOptions.SkipExistingResults && File.Exists(ascoreResultsFilePath))
            {
                ShowMessage("Existing results file found; will not re-create");
            }
            else
            {
                var paramManager = new ParameterFileManager(ascoreOptions.AScoreParamFile);
                AttachEvents(paramManager);

                DatasetManager datasetManager;
                ascoreOptions.SearchType = ascoreOptions.SearchType.ToLower();

                switch (ascoreOptions.SearchType)
                {
                    case "xtandem":
                        ShowMessage("Caching data in " + Path.GetFileName(ascoreOptions.FirstHitsFile));
                        datasetManager = new XTandemFHT(ascoreOptions.FirstHitsFile);
                        break;
                    case "sequest":
                        ShowMessage("Caching data in " + Path.GetFileName(ascoreOptions.FirstHitsFile));
                        datasetManager = new SequestFHT(ascoreOptions.FirstHitsFile);
                        break;
                    case "inspect":
                        ShowMessage("Caching data in " + Path.GetFileName(ascoreOptions.FirstHitsFile));
                        datasetManager = new InspectFHT(ascoreOptions.FirstHitsFile);
                        break;
                    case "msgfdb":
                    case "msgfplus":
                    case "msgf+":
                        ShowMessage("Caching data in " + Path.GetFileName(ascoreOptions.FirstHitsFile));
                        if (ascoreOptions.FirstHitsFile.ToLower().Contains(".mzid"))
                        {
                            datasetManager = new MsgfMzid(ascoreOptions.FirstHitsFile);
                        }
                        else
                        {
                            datasetManager = new MsgfdbFHT(ascoreOptions.FirstHitsFile);
                        }
                        break;
                    default:
                        ShowError("Incorrect search type: " + ascoreOptions.SearchType + " , supported values are " + supportedSearchModes);
                        return -13;
                }
                var peptideMassCalculator = new clsPeptideMassCalculator();

                var spectraManager = new SpectraManagerCache(peptideMassCalculator);

                AttachEvents(spectraManager);

                ShowMessage("Output folder: " + diOutputFolder.FullName);

                var ascoreEngine = new AScore_DLL.Algorithm();
                AttachEvents(ascoreEngine);

                // Initialize the options
                ascoreEngine.FilterOnMSGFScore = ascoreOptions.FilterOnMSGFScore;

                // Run the algorithm
                if (multiJobMode)
                {
                    ascoreEngine.AlgorithmRun(ascoreOptions.JobToDatasetMapFile, spectraManager, datasetManager, paramManager, ascoreResultsFilePath, ascoreOptions.FastaFilePath, ascoreOptions.OutputProteinDescriptions);
                }
                else
                {
                    spectraManager.OpenFile(ascoreOptions.CDtaFile);

                    ascoreEngine.AlgorithmRun(spectraManager, datasetManager, paramManager, ascoreResultsFilePath, ascoreOptions.FastaFilePath, ascoreOptions.OutputProteinDescriptions);
                }

                ShowMessage("AScore Complete");
            }

            if (ascoreOptions.CreateUpdatedFirstHitsFile && !ascoreOptions.FirstHitsFile.ToLower().Contains(".mzid"))
            {
                var resultsMerger = new AScore_DLL.PHRPResultsMerger();
                AttachEvents(resultsMerger);

                resultsMerger.MergeResults(ascoreOptions.FirstHitsFile, ascoreResultsFilePath, ascoreOptions.UpdatedFirstHitsFileName);

                ShowMessage("Results merged; new file: " + Path.GetFileName(resultsMerger.MergedFilePath));
            }

            return 0;
        }

        /// <summary>
        /// Attaches the Error, Warning, and Message events to the local event handler
        /// </summary>
        /// <param name="oClass"></param>
        private static void AttachEvents(clsEventNotifier oClass)
        {
            oClass.ErrorEvent += AScoreEngineErrorEventHandler;
            oClass.WarningEvent += AScoreEngineWarningEventHandler;
            oClass.StatusEvent += AScoreEngineMessageEventHandler;
        }

        private static void ShowMessage(string message)
        {
            Console.Write("\r"); // clear out any percent complete status before outputting.
            Console.WriteLine(message);

            mLogFile?.WriteLine(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt") + "\t" + message);
        }

        private static void ShowError(string message, Exception ex = null)
        {
            Console.WriteLine();
            var msg = "Error: " + message;

            ConsoleMsgUtils.ShowError(msg, ex);
            mLogFile?.WriteLine(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt") + "\t" + msg);

            if (ex != null)
            {
                mLogFile?.WriteLine(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt") + "\t" + clsStackTraceFormatter.GetExceptionStackTrace(ex));
            }
        }

        private static void ShowWarning(string message)
        {
            Console.WriteLine();
            var msg = "Warning: " + message;

            ConsoleMsgUtils.ShowWarning(msg);
            mLogFile?.WriteLine(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt") + "\t" + msg);
        }

        #region "Event handlers for AutoUIMFCalibration"
        private static void AScoreEngineErrorEventHandler(string message, Exception ex)
        {
            ShowError(message, ex);
        }

        private static void AScoreEngineMessageEventHandler(string message)
        {
            ShowMessage(message);
        }

        private static void AScoreEngineWarningEventHandler(string message)
        {
            ShowWarning(message);
        }
        #endregion
    }
}
