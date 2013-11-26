using System;
using PNNLOmicsIO.Utilities.ConsoleUtil;
using AScore_DLL.Managers;
using AScore_DLL.Managers.DatasetManagers;
using System.IO;

namespace AScore_Console
{
	class Program
	{
		static StreamWriter mLogFile = null;

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
			}
		}

		static int Main(string[] args)
		{
			try
			{
				var clu = new CommandLineUtil();
				bool read = clu.ParseCommandLine();
				bool multiJobMode = false;

				var ascoreOptions = new AScoreOptionsType();
				ascoreOptions.Initialize();
				string logFilePath = string.Empty;

				string syntaxError = string.Empty;
				const string supportedSearchModes = "sequest, xtandem, inspect, msgfdb, or msgfplus";

				if (!clu.ShowHelp)
				{
					if (!clu.RetrieveValueForParameter("T", out ascoreOptions.SearchType, false))
						syntaxError = "-T:Search_Engine not defined";

					if (!clu.RetrieveValueForParameter("F", out ascoreOptions.FirstHitsFile, false))
						syntaxError = "-F:fht_file_path not defined";

					if (clu.RetrieveValueForParameter("JM", out ascoreOptions.JobToDatasetMapFile, false))
						multiJobMode = true;
					else
					{
						if (!clu.RetrieveValueForParameter("D", out ascoreOptions.CDtaFile, false))
							syntaxError = "Must use -D:dta_file_path or -JM:job_to_dataset_mapfile_path";
					}

					if (!clu.RetrieveValueForParameter("P", out ascoreOptions.AScoreParamFile, false))
						syntaxError = "-P:parameter_file not defined";

					if (!clu.RetrieveValueForParameter("O", out ascoreOptions.OutputFolderPath, false) || string.IsNullOrWhiteSpace(ascoreOptions.OutputFolderPath))
						ascoreOptions.OutputFolderPath = ".";

					// If ascoreOptions.OutputFolderPath points to a file, change it to the parent folder
					var fiOutputFolderAsFile = new FileInfo(ascoreOptions.OutputFolderPath);
					if (fiOutputFolderAsFile.Extension.Length > 1 && fiOutputFolderAsFile.Directory != null && fiOutputFolderAsFile.Directory.Exists)
					{
						ascoreOptions.OutputFolderPath = fiOutputFolderAsFile.Directory.FullName;
					}

					string outValue;
					if (clu.RetrieveValueForParameter("FM", out outValue, false))
					{
						if (!bool.TryParse(outValue, out ascoreOptions.FilterOnMSGFScore))
						{
							syntaxError = "specify true or false for -FM; not -FM:" + outValue;
						}
					}

					if (clu.RetrieveValueForParameter("L", out outValue, false))
						logFilePath = string.Copy(outValue);

					if (clu.RetrieveValueForParameter("U", out outValue, false))
					{
						ascoreOptions.CreateUpdatedFirstHitsFile = true;
						if (!string.IsNullOrWhiteSpace(outValue))
							ascoreOptions.UpdatedFirstHitsFileName = string.Copy(outValue);
					}

					if (clu.RetrieveValueForParameter("Skip", out outValue, false))
					{
						ascoreOptions.SkipExistingResults = true;
					}

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
					Console.WriteLine(" -T:search_engine");
					Console.WriteLine("   (allowed values are " + supportedSearchModes + ")");
					Console.WriteLine(" -F:fht_file_path");
					Console.WriteLine(" -D:dta_file_path");
					Console.WriteLine(" -JM:job_to_dataset_mapfile_path");
					Console.WriteLine("   (use -JM instead of -D if the FHT file has results from");
					Console.WriteLine("    multiple jobs; the map file should have job numbers and");
					Console.WriteLine("    dataset names, using column names Job and Dataset)");
					Console.WriteLine(" -P:parameter_file_path");
					Console.WriteLine(" -O:output_folder_path");
					Console.WriteLine(" -L:log_file_path");
					Console.WriteLine(" -FM:true  (true or false to enable/disable filtering");
					Console.WriteLine("            on data in column MSGF_SpecProb; default is true)");
					Console.WriteLine(" -U:updated_fht_file_name");
					Console.WriteLine("   (create a copy of the fht_file with updated peptide");
					Console.WriteLine("    sequences plus new AScore-related columns");
					Console.WriteLine(" -Skip     (will not re-run AScore if an existing");
					Console.WriteLine("            results file already exists)");
					Console.WriteLine();
					Console.WriteLine("Example command line #1:");
					Console.WriteLine(Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location) +
									  " -T:sequest\n" +
									  " -F:\"C:\\Temp\\DatasetName_fht.txt\"\n" +
									  " -D:\"C:\\Temp\\DatasetName_dta.txt\"\n" +
									  " -O:\"C:\\Temp\"\n" +
									  " -P:C:\\Temp\\DynMetOx_stat_4plex_iodo_hcd.xml\n" +
									  " -L:LogFile.txt\n" +
									  " -FM:true");
					Console.WriteLine();
					Console.WriteLine("Example command line #2:");
					Console.WriteLine(Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location) +
									  " -T:msgfplus\n" +
									  " -F:\"C:\\Temp\\Multi_Job_Results_fht.txt\"\n" +
									  " -JM:\"C:\\Temp\\JobToDatasetNameMap.txt\"\n" +
									  " -O:\"C:\\Temp\\\"\n" +
									  " -P:C:\\Temp\\DynPhos_stat_6plex_iodo_hcd.xml\n" +
									  " -L:LogFile.txt\n" +
									  " -FM:true\n");


					clu.PauseAtConsole(750, 250);
					return 0;
				}


				if (!File.Exists(ascoreOptions.AScoreParamFile))
				{
					Console.WriteLine("Input file not found: " + ascoreOptions.AScoreParamFile);
					clu.PauseAtConsole(2000, 333);
					return -10;
				}


				if (!string.IsNullOrEmpty(ascoreOptions.CDtaFile) && !File.Exists(ascoreOptions.CDtaFile))
				{
					Console.WriteLine("Input file not found: " + ascoreOptions.CDtaFile);
					clu.PauseAtConsole(2000, 333);
					return -11;
				}

				if (!string.IsNullOrEmpty(ascoreOptions.JobToDatasetMapFile) && !File.Exists(ascoreOptions.JobToDatasetMapFile))
				{
					Console.WriteLine("Input file not found: " + ascoreOptions.JobToDatasetMapFile);
					clu.PauseAtConsole(2000, 333);
					return -11;
				}

				if (!File.Exists(ascoreOptions.FirstHitsFile))
				{
					Console.WriteLine("Input file not found: " + ascoreOptions.FirstHitsFile);
					clu.PauseAtConsole(2000, 333);
					return -12;
				}

				int returnCode = RunAScore(ascoreOptions, logFilePath, supportedSearchModes, multiJobMode);

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

		private static int RunAScore(AScoreOptionsType ascoreOptions, string logFilePath, string supportedSearchModes, bool multiJobMode)
		{

			if (!String.IsNullOrWhiteSpace(logFilePath))
			{
				mLogFile = new StreamWriter(new FileStream(logFilePath, FileMode.Create, FileAccess.Write, FileShare.Read));
				mLogFile.AutoFlush = true;
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

			string ascoreResultsFilePath = Path.Combine(diOutputFolder.FullName, Path.GetFileNameWithoutExtension(ascoreOptions.FirstHitsFile) + "_ascore.txt");

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
						ShowMessage("Caching data in " + ascoreOptions.FirstHitsFile);
						datasetManager = new XTandemFHT(ascoreOptions.FirstHitsFile);
						break;
					case "sequest":
						ShowMessage("Caching data in " + ascoreOptions.FirstHitsFile);
						datasetManager = new SequestFHT(ascoreOptions.FirstHitsFile);
						break;
					case "inspect":
						ShowMessage("Caching data in " + ascoreOptions.FirstHitsFile);
						datasetManager = new InspectFHT(ascoreOptions.FirstHitsFile);
						break;
					case "msgfdb":
					case "msgfplus":
					case "msgf+":
						ShowMessage("Caching data in " + ascoreOptions.FirstHitsFile);
						datasetManager = new MsgfdbFHT(ascoreOptions.FirstHitsFile);
						break;
					default:
						ShowMessage("Incorrect search type: " + ascoreOptions.SearchType + " , supported values are " + supportedSearchModes);
						Console.WriteLine();
						return -13;
				}

				var dtaManager = new DtaManager();
				AttachEvents(dtaManager);

				ShowMessage("Computing AScore values and Writing results to " + diOutputFolder.FullName);

				var ascoreEngine = new AScore_DLL.Algorithm();
				AttachEvents(ascoreEngine);

				// Initialize the options
				ascoreEngine.FilterOnMSGFScore = ascoreOptions.FilterOnMSGFScore;


				// Run the algorithm
				if (multiJobMode)
				{
					ascoreEngine.AlgorithmRun(ascoreOptions.JobToDatasetMapFile, dtaManager, datasetManager, paramManager, ascoreResultsFilePath);
				}
				else
				{
					dtaManager.OpenCDTAFile(ascoreOptions.CDtaFile);

					ascoreEngine.AlgorithmRun(dtaManager, datasetManager, paramManager, ascoreResultsFilePath);
				}
				

				ShowMessage("AScore Complete");
			}

			if (ascoreOptions.CreateUpdatedFirstHitsFile)
			{
				var resultsMerger = new AScore_DLL.PHRPResultsMerger();
				AttachEvents(resultsMerger);

				resultsMerger.MergeResults(ascoreOptions.FirstHitsFile, ascoreResultsFilePath, ascoreOptions.UpdatedFirstHitsFileName);

				ShowMessage("Results merged; new file: " + resultsMerger.MergedFilePath);
			}

			return 0;
		}

		/// <summary>
		/// Attaches the Error, Warning, and Message events to the local event handler
		/// </summary>
		/// <param name="paramManager"></param>
		private static void AttachEvents(AScore_DLL.MessageEventBase paramManager)
		{
			paramManager.ErrorEvent += AScoreEngineErrorEventHandler;
			paramManager.WarningEvent += AScoreEngineWarningEventHandler;
			paramManager.MessageEvent += AScoreEngineMessageEventHandler;
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
