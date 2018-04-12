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
#endif
            {
                var parser = new CommandLineParser<AScoreOptions>();

                var exeName = Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location);
                parser.UsageExamples.Add("Example command line #1:\n" + exeName +
                                  " -T:sequest\n" +
                                  " -F:\"C:\\Temp\\DatasetName_fht.txt\"\n" +
                                  " -D:\"C:\\Temp\\DatasetName_dta.txt\"\n" +
                                  " -O:\"C:\\Temp\"\n" +
                                  " -P:C:\\Temp\\DynMetOx_stat_4plex_iodo_hcd.xml\n" +
                                  " -L:LogFile.txt");
                parser.UsageExamples.Add("Example command line #2:\n" + exeName +
                                  " -T:msgfplus\n" +
                                  " -F:Dataset_W_S2_Fr_04_2May17_msgfplus_syn.txt\n" +
                                  " -D:Dataset_W_S2_Fr_04_2May17.mzML\n" +
                                  " -P:AScore_CID_0.5Da_ETD_0.5Da_HCD_0.05Da.xml\n" +
                                  " -U:W_S2_Fr_04_2May17_msgfplus_syn_plus_ascore.txt\n" +
                                  " -L:LogFile.txt");
                parser.UsageExamples.Add("Example command line #3:\n" + exeName +
                                  " -T:msgfplus\n" +
                                  " -F:C:\\Temp\\Multi_Job_Results_fht.txt\n" +
                                  " -JM:C:\\Temp\\JobToDatasetNameMap.txt\n" +
                                  " -O:C:\\Temp\\\n" +
                                  " -P:C:\\Temp\\DynPhos_stat_6plex_iodo_hcd.xml\n" +
                                  " -L:LogFile.txt\n" +
                                  " -noFM");
                parser.UsageExamples.Add("Example command line #4:\n" + exeName +
                                  " -T:msgfplus\n" +
                                  " -F:C:\\Temp\\Multi_Job_Results_fht.txt\n" +
                                  " -JM:C:\\Temp\\JobToDatasetNameMap.txt\n" +
                                  " -O:C:\\Temp\\\n" +
                                  " -P:C:\\Temp\\DynPhos_stat_6plex_iodo_hcd.xml\n" +
                                  " -L:LogFile.txt\n" +
                                  " -Fasta:C:\\Temp\\H_sapiens_Uniprot_SPROT_2013-09-18.fasta\n" +
                                  " -PD");

                var results = parser.ParseArgs(args);
                var ascoreOptions = results.ParsedResults;
                if (!results.Success || !ascoreOptions.Validate())
                {
                    System.Threading.Thread.Sleep(1500);
                    return -1;
                }

                mLogFilePath = ascoreOptions.LogFilePath;

                var returnCode = ascoreOptions.CheckFiles(x => ShowError(x));
                // If we encountered an error in the input - a necessary file does not exist - then exit.
                if (returnCode != 0)
                {
                    clsParseCommandLine.PauseAtConsole(2000, 333);
                    return returnCode;
                }

                returnCode = RunAScore(ascoreOptions, mLogFilePath, SupportedSearchModes);

                if (returnCode != 0)
                {
                    clsParseCommandLine.PauseAtConsole(2000, 333);
                }
                else
                {
                    clsParseCommandLine.PauseAtConsole(1000, 250);
                }
            }
#if (!DEBUG)
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
        /// Configure and run the AScore DLL
        /// </summary>
        /// <param name="ascoreOptions"></param>
        /// <param name="logFilePath"></param>
        /// <param name="supportedSearchModes"></param>
        /// <param name="multiJobMode"></param>
        /// <returns></returns>
        private static int RunAScore(AScoreOptions ascoreOptions, string logFilePath, string supportedSearchModes)
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

                switch (ascoreOptions.SearchType)
                {
                    case SearchMode.XTandem:
                        ShowMessage("Caching data in " + Path.GetFileName(ascoreOptions.FirstHitsFile));
                        datasetManager = new XTandemFHT(ascoreOptions.FirstHitsFile);
                        break;
                    case SearchMode.Sequest:
                        ShowMessage("Caching data in " + Path.GetFileName(ascoreOptions.FirstHitsFile));
                        datasetManager = new SequestFHT(ascoreOptions.FirstHitsFile);
                        break;
                    case SearchMode.Inspect:
                        ShowMessage("Caching data in " + Path.GetFileName(ascoreOptions.FirstHitsFile));
                        datasetManager = new InspectFHT(ascoreOptions.FirstHitsFile);
                        break;
                    case SearchMode.Msgfdb:
                    case SearchMode.Msgfplus:
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
                if (ascoreOptions.MultiJobMode)
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
            oClass.ErrorEvent += ShowError;
            oClass.WarningEvent += ShowWarning;
            oClass.StatusEvent += ShowMessage;
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
    }
}
