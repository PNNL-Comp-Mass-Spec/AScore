using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PNNLOmicsIO.Utilities.ConsoleUtil;
using AScore_DLL.Managers;
using AScore_DLL.Managers.DatasetManagers;
using System.IO;

namespace AScore_Console
{
	class Program
	{
		static System.IO.StreamWriter mLogFile = null;

		static int Main(string[] args)
		{
			try
			{
				CommandLineUtil clu = new CommandLineUtil();
				bool read = clu.ParseCommandLine();
				string searchType = string.Empty;
				string fhtFile = string.Empty;
				string dtaFile = string.Empty;
				string paramFile = string.Empty;
				string outPath = string.Empty;
				string logFilePath = string.Empty;
				string outValue;

				bool filterOnMSGFScore = true;
				string syntaxError = string.Empty;
				string supportedSearchModes = "sequest, xtandem, inspect, msgfdb, or msgfplus";

				if (!clu.ShowHelp)
				{
					if (!clu.RetrieveValueForParameter("T", out searchType, false))
						syntaxError = "-T:Search_Engine not defined";

					if (!clu.RetrieveValueForParameter("F", out fhtFile, false))
						syntaxError = "-F:fhtfile_path not defined";

					if (!clu.RetrieveValueForParameter("D", out dtaFile, false))
						syntaxError = "-D:dta_file_path not defined";

					if (!clu.RetrieveValueForParameter("P", out paramFile, false))
						syntaxError = "-P:parameter_file not defined";

					if (!clu.RetrieveValueForParameter("O", out outPath, false) || string.IsNullOrWhiteSpace(outPath))
						outPath = ".";

					if (clu.RetrieveValueForParameter("FM", out outValue, false))
					{
						if (!bool.TryParse(outValue, out filterOnMSGFScore))
						{
							syntaxError = "specify true or false for -FM; not -FM:" + outValue;
						}
					}

					if (clu.RetrieveValueForParameter("L", out outValue, false))
						logFilePath = string.Copy(outValue);
				}




				//	AScore_DLL.AScoreParameters parameters = 
				if ((args.Length == 0) || clu.ShowHelp || syntaxError.Length > 0)
				{
					Console.WriteLine();
					if (syntaxError.Length > 0)
					{
						Console.WriteLine("Error, " + syntaxError);
						Console.WriteLine();
					}

					Console.WriteLine("Parameters for running AScore include:");
					Console.WriteLine(" -T:search_engine (allowed values are " + supportedSearchModes + ")");
					Console.WriteLine(" -F:fht_file_path");
					Console.WriteLine(" -D:dta_file_path");
					Console.WriteLine(" -P:parameter_file_path");
					Console.WriteLine(" -O:output_file_path");
					Console.WriteLine(" -L:log_file_path");
					Console.WriteLine(" -FM:true  (true or false to enable/disable filtering on data in column MSGF_SpecProb; default is true)");
					Console.WriteLine();
					Console.WriteLine("Example command line:");
					Console.WriteLine(System.IO.Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location) +
									  " -T:sequest\n" +
									  " -F:\"C:\\Temp\\GmaxP_itraq_NiNTA_15_29Apr10_Hawk_03-10-09p_fht.txt\"\n" +
									  " -D:\"C:\\Temp\\GmaxP_itraq_NiNTA_15_29Apr10_Hawk_03-10-09p_dta.txt\"\n" +
									  " -O:\"C:\\Temp\\GmaxP_itraq_NiNTA_15_29Apr10_Hawk_03-10-09.txt\"\n" +
									  " -P:C:\\Temp\\parameterFileForGmax.xml\n" +
									  " -L:LogFile.txt\n" +
									  " -FM:true\n");

					clu.PauseAtConsole(750, 250);
					return 0;
				}


				if (!File.Exists(paramFile))
				{
					Console.WriteLine("Input file not found: " + paramFile);
					clu.PauseAtConsole(2000, 333);
					return -10;
				}

				if (!File.Exists(dtaFile))
				{
					Console.WriteLine("Input file not found: " + dtaFile);
					clu.PauseAtConsole(2000, 333);
					return -11;
				}

				if (!File.Exists(fhtFile))
				{
					Console.WriteLine("Input file not found: " + fhtFile);
					clu.PauseAtConsole(2000, 333);
					return -12;
				}

				int returnCode = RunAScore(searchType, fhtFile, dtaFile, paramFile, outPath, logFilePath, filterOnMSGFScore, supportedSearchModes);

				if (returnCode != 0)
					clu.PauseAtConsole(2000, 333);

			}
			catch (Exception ex)
			{
			    Console.WriteLine();
			    ShowMessage("Program failure, possibly incorrect search engine type; " + ex.Message);
			    ShowMessage("Stack Track: " + ex.StackTrace);

			    return ((int)ex.Message.GetHashCode());
			}
			finally
			{
			    if (mLogFile != null)
			        mLogFile.Close();
			}

			return 0;

		}

		private static int RunAScore(string searchType, string fhtFile, string dtaFile, string paramFile, string outPath, string logFilePath, bool filterOnMSGFScore, string supportedSearchModes)
		{

			if (!String.IsNullOrWhiteSpace(logFilePath))
			{
				mLogFile = new System.IO.StreamWriter(new System.IO.FileStream(logFilePath, FileMode.Create, FileAccess.Write, FileShare.Read));
				mLogFile.AutoFlush = true;
			}

			ParameterFileManager paramManager = new ParameterFileManager(paramFile);
			DtaManager dtaManager = new DtaManager(dtaFile);

			paramManager.ErrorEvent += new AScore_DLL.MessageEventBase.MessageEventHandler(AScoreEngineErrorEventHandler);
			paramManager.WarningEvent += new AScore_DLL.MessageEventBase.MessageEventHandler(AScoreEngineWarningEventHandler);
			paramManager.MessageEvent += new AScore_DLL.MessageEventBase.MessageEventHandler(AScoreEngineMessageEventHandler);

			dtaManager.ErrorEvent += new AScore_DLL.MessageEventBase.MessageEventHandler(AScoreEngineErrorEventHandler);
			dtaManager.WarningEvent += new AScore_DLL.MessageEventBase.MessageEventHandler(AScoreEngineWarningEventHandler);
			dtaManager.MessageEvent += new AScore_DLL.MessageEventBase.MessageEventHandler(AScoreEngineMessageEventHandler);

			DatasetManager datasetManager;
			searchType = searchType.ToLower();

			switch (searchType)
			{
				case "xtandem":
					datasetManager = new XTandemFHT(fhtFile);
					break;
				case "sequest":
					datasetManager = new SequestFHT(fhtFile);
					break;
				case "inspect":
					datasetManager = new InspectFHT(fhtFile);
					break;
				case "msgfdb":
				case "msgfplus":
				case "msgf+":
					datasetManager = new MsgfdbFHT(fhtFile);
					break;
				default:
					ShowMessage("Incorrect search type: " + searchType + " , supported values are " + supportedSearchModes);
					Console.WriteLine();
					return -13;
			}

			ShowMessage("Parsing input file: " + fhtFile);

			System.IO.DirectoryInfo diOutputFolder = new System.IO.DirectoryInfo(outPath);
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
					diOutputFolder = new System.IO.DirectoryInfo(".");
					ShowMessage("Changed output to " + diOutputFolder.FullName);
				}
			}
			else
			{
				ShowMessage("Writing results to " + diOutputFolder.FullName);
			}

			string outputFilePath = System.IO.Path.Combine(diOutputFolder.FullName, System.IO.Path.GetFileNameWithoutExtension(fhtFile) + "_ascore.txt");

			AScore_DLL.Algorithm ascoreEngine = new AScore_DLL.Algorithm();

			ascoreEngine.ErrorEvent += new AScore_DLL.MessageEventBase.MessageEventHandler(AScoreEngineErrorEventHandler);
			ascoreEngine.WarningEvent += new AScore_DLL.MessageEventBase.MessageEventHandler(AScoreEngineWarningEventHandler);
			ascoreEngine.MessageEvent += new AScore_DLL.MessageEventBase.MessageEventHandler(AScoreEngineMessageEventHandler);

			ascoreEngine.AlgorithmRun(dtaManager, datasetManager, paramManager, outputFilePath, filterOnMSGFScore);


			ShowMessage("Success");

			return 0;
		}

		private static void ShowMessage(string message)
		{
			Console.WriteLine(message);

			if (mLogFile != null)
				mLogFile.WriteLine(System.DateTime.Now.ToString("yyyy-MM-dd hh:mm:ss tt") + "\t" + message);
		}

		#region "Event handlers for AutoUIMFCalibration"
		private static void AScoreEngineErrorEventHandler(object sender, AScore_DLL.MessageEventArgs e)
		{
			Console.WriteLine();
			ShowMessage("Error: " + e.Message);
		}

		private static void AScoreEngineMessageEventHandler(object sender, AScore_DLL.MessageEventArgs e)
		{
			ShowMessage(e.Message);
		}

		private static void AScoreEngineWarningEventHandler(object sender, AScore_DLL.MessageEventArgs e)
		{
			Console.WriteLine();
			ShowMessage("Warning: " + e.Message);
		}
		#endregion



	}
}
