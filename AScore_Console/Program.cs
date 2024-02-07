
// Uncomment to disable the try/catch handler in Main
// #define DISABLE_ROOT_EXCEPTION_HANDLER

using System;
using System.IO;
using System.Reflection;
using System.Threading;
using AScore_DLL;
using PRISM;
using PRISM.FileProcessor;
using PRISM.Logging;

namespace AScore_Console
{
    internal static class Program
    {
        // Ignore Spelling: dyn, iodo, phos, yyyy-MM-dd hh:mm:ss tt

        private static StreamWriter mLogFile;
        private static string mLogFilePath = string.Empty;

        /// <summary>
        /// Main entry point
        /// </summary>
        /// <param name="args"></param>
        /// <returns>0 if successful; error code if a problem</returns>
        private static int Main(string[] args)
        {
#if (!DISABLE_ROOT_EXCEPTION_HANDLER)
            try
#endif
            {
                var parser = new CommandLineParser<AScoreOptions>();

                var exeName = Path.GetFileName(Assembly.GetExecutingAssembly().Location);
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

                var parseResults = parser.ParseArgs(args);
                var ascoreOptions = parseResults.ParsedResults;

                Console.WriteLine("AScore version " + GetAppVersion());
                Console.WriteLine();

                if (!parseResults.Success)
                {
                    Thread.Sleep(1500);
                    return -1;
                }

                if (!ascoreOptions.Validate(out var errorMessage))
                {
                    parser.PrintHelp();

                    Console.WriteLine();
                    ConsoleMsgUtils.ShowWarning("Validation error:");
                    ConsoleMsgUtils.ShowWarning(errorMessage);

                    Thread.Sleep(1500);
                    return -1;
                }

                mLogFilePath = ascoreOptions.LogFilePath;

                var returnCode = ascoreOptions.CheckFiles(x => ShowError(x));

                // If we encountered an error in the input - a necessary file does not exist - then exit.
                if (returnCode != 0)
                {
                    ConsoleMsgUtils.PauseAtConsole(2000, 333);
                    return returnCode;
                }

                if (!string.IsNullOrWhiteSpace(mLogFilePath))
                {
                    var logFile = new FileInfo(mLogFilePath);
                    if (logFile.Directory == null)
                    {
                        ConsoleMsgUtils.ShowWarning("Unable to determine the parent directory of " + mLogFilePath);
                        return -1;
                    }

                    if (!logFile.Directory.Exists)
                    {
                        Console.WriteLine("Creating " + logFile.Directory.FullName);
                        logFile.Directory.Create();
                    }

                    mLogFile = new StreamWriter(new FileStream(mLogFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
                    {
                        AutoFlush = true
                    };
                }

                returnCode = RunAScoreProcessor(ascoreOptions);

                if (returnCode != 0)
                {
                    ConsoleMsgUtils.PauseAtConsole(2000, 333);
                }
                else
                {
                    Thread.Sleep(1000);
                }
            }
#if (!DISABLE_ROOT_EXCEPTION_HANDLER)
            catch (Exception ex)
            {
                Console.WriteLine();
                ShowError("Program failure", ex);

                return ex.Message.GetHashCode();
            }
            finally
            {
                mLogFile?.Close();
            }
#endif

            return 0;
        }

        private static int RunAScoreProcessor(AScoreOptions ascoreOptions)
        {
            var returnCode = 0;

            var processor = new AScoreProcessor();
            AttachEvents(processor);

            if (ascoreOptions.ProcessSettings(ShowMessage))
            {
                returnCode = processor.RunAScore(ascoreOptions);
            }
            else
            {
                ShowMessage("Existing results file found; will not re-create");

                if (ascoreOptions.CreateUpdatedDbSearchResultsFile && ascoreOptions.SearchResultsType == AScoreOptions.DbSearchResultsType.Fht)
                {
                    processor.CreateUpdatedFirstHitsFile(ascoreOptions);
                }
            }

            return returnCode;
        }

        /// <summary>
        /// Attaches the Error, Warning, and Message events to the local event handler
        /// </summary>
        /// <param name="oClass"></param>
        private static void AttachEvents(IEventNotifier oClass)
        {
            oClass.ErrorEvent += ShowError;
            oClass.WarningEvent += ShowWarning;
            oClass.StatusEvent += ShowMessage;
        }

        private static string GetAppVersion()
        {
            return ProcessFilesOrDirectoriesBase.GetAppVersion(AScoreOptions.PROGRAM_DATE);
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
                mLogFile?.WriteLine(DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt") + "\t" + StackTraceFormatter.GetExceptionStackTrace(ex));
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
