using System;
using System.IO;
using AScore_DLL;
using PRISM;

namespace AScore_Console
{
    class Program
    {
        static StreamWriter mLogFile;
        static string mLogFilePath = string.Empty;

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

                if (!string.IsNullOrWhiteSpace(mLogFilePath))
                {
                    mLogFile = new StreamWriter(new FileStream(mLogFilePath, FileMode.Create, FileAccess.Write, FileShare.Read))
                    {
                        AutoFlush = true
                    };
                }

                returnCode = RunAScoreProcessor(ascoreOptions);

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

                if (ascoreOptions.CreateUpdatedDbSearchResultsFile && ascoreOptions.SearchResultsType == DbSearchResultsType.Fht)
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
        private static void AttachEvents(EventNotifier oClass)
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
