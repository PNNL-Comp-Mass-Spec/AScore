using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Data;
using System.Runtime.InteropServices;
using AScore_DLL.Managers;
using AScore_DLL.Managers.DatasetManagers;
using PeptideToProteinMapEngine;
using System.IO;
using PHRPReader;

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

	    struct AScoreRunDataType
	    {
	        public DirectoryInfo diOutputFolder;
	        public string AScoreResultsFilePath;

	        public void Initialize()
	        {
	            AScoreResultsFilePath = string.Empty;
	        }
	    }

	    struct FastaOptionsType
	    {
	        public bool useFasta;
            public string FastaFilePath;
            public bool OutputProteinDescriptions;

            public void Initialize()
            {
                useFasta = false;
                FastaFilePath = string.Empty;
                OutputProteinDescriptions = false;
            }
	    }

	    struct ProteinPeptideMapType
	    {
	        public string peptideSequence;
	        public string proteinName;
	        public int residueStart;
            public int residueEnd;

            public void Initialize()
            {
                peptideSequence = string.Empty;
                proteinName = string.Empty;
            }
	    }

		static int Main(string[] args)
		{
			try
			{
				var clu = new clsParseCommandLine();
                bool read = clu.ParseCommandLine(clsParseCommandLine.ALTERNATE_SWITCH_CHAR);
				bool multiJobMode = false;

				var ascoreOptions = new AScoreOptionsType();
			    var fastaOptions = new FastaOptionsType();
				ascoreOptions.Initialize();
				string logFilePath = string.Empty;

				string syntaxError = string.Empty;
				const string supportedSearchModes = "sequest, xtandem, inspect, msgfdb, or msgfplus";

				if (!clu.NeedToShowHelp)
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
                            Console.WriteLine("Warning: '-FM:" + outValue + "' not recognized. Assuming '-FM:true'.");
						    //syntaxError = "specify true or false for -FM; not -FM:" + outValue;
                            // Reset it to true, bool.TryParse failed and set it to false.
						    ascoreOptions.FilterOnMSGFScore = true;
						}
                    }

                    // If the switch is present, disable filtering on MSGF Score
                    if (clu.IsParameterPresent("noFM"))
                    {
                        ascoreOptions.FilterOnMSGFScore = false;
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

				    if (!clu.RetrieveValueForParameter("Fasta", out fastaOptions.FastaFilePath, false) ||
				        string.IsNullOrWhiteSpace(fastaOptions.FastaFilePath))
				    {
				        fastaOptions.FastaFilePath = string.Empty;
				    }
				    else
				    {
				        fastaOptions.useFasta = true;
				    }

				    if (clu.RetrieveValueForParameter("PD", out outValue, false) && fastaOptions.useFasta)
                    {
                        fastaOptions.OutputProteinDescriptions = true;
                    }
				}


				//	AScore_DLL.AScoreParameters parameters = 
				if ((args.Length == 0) || clu.NeedToShowHelp || syntaxError.Length > 0)
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
                    Console.WriteLine(" -noFM   (disable filtering on data in column MSGF_SpecProb;");
                    Console.WriteLine("          overrides -FM flag)");
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


					clsParseCommandLine.PauseAtConsole(750, 250);
					return 0;
				}


				if (!File.Exists(ascoreOptions.AScoreParamFile))
				{
					Console.WriteLine("Input file not found: " + ascoreOptions.AScoreParamFile);
					clsParseCommandLine.PauseAtConsole(2000, 333);
					return -10;
				}


				if (!string.IsNullOrEmpty(ascoreOptions.CDtaFile) && !File.Exists(ascoreOptions.CDtaFile))
				{
					Console.WriteLine("Input file not found: " + ascoreOptions.CDtaFile);
					clsParseCommandLine.PauseAtConsole(2000, 333);
					return -11;
				}

				if (!string.IsNullOrEmpty(ascoreOptions.JobToDatasetMapFile) && !File.Exists(ascoreOptions.JobToDatasetMapFile))
				{
					Console.WriteLine("Input file not found: " + ascoreOptions.JobToDatasetMapFile);
					clsParseCommandLine.PauseAtConsole(2000, 333);
					return -11;
				}

				if (!File.Exists(ascoreOptions.FirstHitsFile))
				{
					Console.WriteLine("Input file not found: " + ascoreOptions.FirstHitsFile);
					clsParseCommandLine.PauseAtConsole(2000, 333);
					return -12;
                }

                if (!string.IsNullOrEmpty(fastaOptions.FastaFilePath) && !File.Exists(fastaOptions.FastaFilePath))
                {
                    Console.WriteLine("Fasta file not found: " + fastaOptions.FastaFilePath);
                    clsParseCommandLine.PauseAtConsole(2000, 333);
                    return -13;
                }

			    AScoreRunDataType aScoreRunData;
				int returnCode = RunAScore(ascoreOptions, logFilePath, supportedSearchModes, multiJobMode, out aScoreRunData);

			    if (returnCode != 0)
			    {
			        clsParseCommandLine.PauseAtConsole(2000, 333);
			        return returnCode;
			    }
			    else
			    {
			        if (fastaOptions.useFasta)
			        {
			            returnCode = RunProteinMapper(aScoreRunData, ascoreOptions, fastaOptions, logFilePath);
                    }

                    if (returnCode != 0)
                    {
                        clsParseCommandLine.PauseAtConsole(2000, 333);
                    }
                }
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

	    private static int RunProteinMapper(AScoreRunDataType aScoreRunData, AScoreOptionsType aScoreOptions,
	        FastaOptionsType fastaOptions, string logFilePath)
	    {
            Console.WriteLine("Mapping peptides to proteins");
	        string line;
	        string ascoreResultsFilePath = Path.Combine(aScoreRunData.diOutputFolder.FullName,
	            Path.GetFileNameWithoutExtension(aScoreRunData.AScoreResultsFilePath) + "_ProteinMap.txt");
            string tempPeptidesFilePath = Path.Combine(aScoreRunData.diOutputFolder.FullName,
                Path.GetFileNameWithoutExtension(aScoreRunData.AScoreResultsFilePath) + "_Peptides.txt");

            Dictionary<string, int> columnMapAScore = new Dictionary<string, int>();

            Console.WriteLine("Creating peptide list file");
            // Write out a list of peptides for clsPeptideToProteinMapEngine
	        using (StreamReader aScoreReader =
                    new StreamReader(new FileStream(aScoreRunData.AScoreResultsFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
	        {
	            using (StreamWriter peptideWriter =
	                new StreamWriter(new FileStream(tempPeptidesFilePath, FileMode.Create, FileAccess.Write, FileShare.Read)))
                {
                    if ((line = aScoreReader.ReadLine()) != null)
                    {
                        // Assume the first line is column names
                        string[] columns = line.Split('\t');
                        for (int i = 0; i < columns.Length; ++i)
                        {
                            columnMapAScore.Add(columns[i], i);
                        }
                        // Run as long as we can successfully read
                        while ((line = aScoreReader.ReadLine()) != null)
                        {
                            string sequence = line.Split('\t')[columnMapAScore["BestSequence"]];
                            peptideWriter.WriteLine(
                                clsPeptideCleavageStateCalculator.ExtractCleanSequenceFromSequenceWithMods(sequence, true));
                        }
                    }
	            }
	        }

            Console.WriteLine("Running PeptideToProteinMapper");
	        // Configure the peptide to protein mapper
            clsPeptideToProteinMapEngine peptideToProteinMapper = new clsPeptideToProteinMapEngine();

	        peptideToProteinMapper.DeleteTempFiles = true;
	        peptideToProteinMapper.IgnoreILDifferences = false;
	        peptideToProteinMapper.InspectParameterFilePath = string.Empty;

	        if (!string.IsNullOrEmpty(logFilePath))
	        {
	            peptideToProteinMapper.LogMessagesToFile = true;
	            peptideToProteinMapper.LogFolderPath = aScoreRunData.diOutputFolder.FullName;
	        }
	        else
	        {
	            peptideToProteinMapper.LogMessagesToFile = false;
	        }

	        peptideToProteinMapper.MatchPeptidePrefixAndSuffixToProtein = false;
	        peptideToProteinMapper.OutputProteinSequence = false;
	        peptideToProteinMapper.PeptideInputFileFormat = clsPeptideToProteinMapEngine.ePeptideInputFileFormatConstants.PeptideListFile;
	        peptideToProteinMapper.PeptideFileSkipFirstLine = false;
	        peptideToProteinMapper.ProteinDataRemoveSymbolCharacters = true;
	        peptideToProteinMapper.ProteinInputFilePath = fastaOptions.FastaFilePath;
	        peptideToProteinMapper.SaveProteinToPeptideMappingFile = true;
	        peptideToProteinMapper.SearchAllProteinsForPeptideSequence = true;
	        peptideToProteinMapper.SearchAllProteinsSkipCoverageComputationSteps = true;
	        peptideToProteinMapper.ShowMessages = false;

            // Note that clsPeptideToProteinMapEngine utilizes Data.SQLite.dll
	        bool bSuccess = peptideToProteinMapper.ProcessFile(tempPeptidesFilePath, aScoreRunData.diOutputFolder.FullName, string.Empty, true);

	        peptideToProteinMapper.CloseLogFileNow();

	        if (bSuccess)
	        {
                Console.WriteLine("Reading data back");
	            string strMapFilePath = Path.GetFileNameWithoutExtension(tempPeptidesFilePath) + "_ProteinToPeptideMapping.txt";
	            //                            PeptideToProteinMapEngine.clsPeptideToProteinMapEngine.FILENAME_SUFFIX_PEP_TO_PROTEIN_MAPPING;
	            strMapFilePath = Path.Combine(aScoreRunData.diOutputFolder.FullName, strMapFilePath);
	            Dictionary<string, List<ProteinPeptideMapType>> peptideToProteinMap = new Dictionary<string, List<ProteinPeptideMapType>>();
	            Dictionary<string, int> columnMapPTPM = new Dictionary<string, int>();

	            using (StreamReader mapReader =
	                new StreamReader(new FileStream(strMapFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
	            {
	                if ((line = mapReader.ReadLine()) != null)
	                {
	                    // Assume the first line is column names
	                    string[] columns = line.Split('\t');
	                    for (int i = 0; i < columns.Length; ++i)
	                    {
	                        columnMapPTPM.Add(columns[i], i);
	                    }
	                    // Run as long as we can successfully read
	                    while ((line = mapReader.ReadLine()) != null)
	                    {
	                        columns = line.Split('\t');
	                        ProteinPeptideMapType item = new ProteinPeptideMapType();
	                        item.residueStart = Convert.ToInt32(columns[columnMapPTPM["Residue Start"]]);
	                        item.residueEnd = Convert.ToInt32(columns[columnMapPTPM["Residue End"]]);
	                        item.proteinName = columns[columnMapPTPM["Protein Name"]];
	                        item.peptideSequence = columns[columnMapPTPM["Peptide Sequence"]];
	                        // Add the key and a new list if it doesn't yet exist
	                        if (!peptideToProteinMap.ContainsKey(item.peptideSequence))
	                        {
	                            peptideToProteinMap.Add(item.peptideSequence, new List<ProteinPeptideMapType>());
	                        }
	                        peptideToProteinMap[item.peptideSequence].Add(item);
	                    }
	                }
	            }
                Dictionary<string, string> proteinDescriptions = new Dictionary<string, string>();
	            if (fastaOptions.OutputProteinDescriptions)
	            {
	                using (StreamReader fastaReader =
	                        new StreamReader(new FileStream(fastaOptions.FastaFilePath, FileMode.Open, FileAccess.Read,FileShare.ReadWrite)))
	                {
                        while ((line = fastaReader.ReadLine()) != null)
                        {
                            // We only care about the protein name/description line
                            if (line[0] == '>')
                            {
                                int firstSpace = line.IndexOf(' ');
                                // Skip the '>' and split at the first space
                                proteinDescriptions.Add(line.Substring(1, firstSpace - 1), line.Substring(firstSpace + 1));
                            }
                        }
	                }
	            }
                // Read the ascore again...
                using (StreamReader aScoreReader =
                        new StreamReader(new FileStream(aScoreRunData.AScoreResultsFilePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
                {
                    // Can't collapse these to one 'using' because they are not the same type.
                    using (StreamWriter mappedWriter =
                        new StreamWriter(new FileStream(ascoreResultsFilePath, FileMode.Create, FileAccess.Write, FileShare.Read)))
                    {
                        
                        // Reuse 'columnMapAScore', it is the same file.
                        if ((line = aScoreReader.ReadLine()) != null)
                        {
                            // Output the header information, with the new additions
                            mappedWriter.Write(line + "\t");
                            mappedWriter.Write("ProteinName\t");
                            if (fastaOptions.OutputProteinDescriptions)
                            {
                                mappedWriter.Write("Description\t");
                            }
                            mappedWriter.WriteLine("ProteinCount\tResidue\tPosition");
                            // Run as long as we can successfully read
                            while ((line = aScoreReader.ReadLine()) != null)
                            {
                                string sequence = line.Split('\t')[columnMapAScore["BestSequence"]];
                                string cleanSequence =
                                    clsPeptideCleavageStateCalculator.ExtractCleanSequenceFromSequenceWithMods(
                                        sequence, true);
                                string noPrefixSequence = string.Empty;
                                string prefix = string.Empty;
                                string suffix = string.Empty;
                                clsPeptideCleavageStateCalculator.SplitPrefixAndSuffixFromSequence(sequence,
                                    ref noPrefixSequence, ref prefix, ref suffix);
                                List<int> mods = new List<int>();
                                int modPosAdj = 0;
                                for (int i = 0; i < noPrefixSequence.Length; ++i)
                                {
                                    if (noPrefixSequence[i] == '*')
                                    {
                                        mods.Add(i);
                                    }
                                }
                                if (noPrefixSequence[0] == '.')
                                {
                                    ++modPosAdj;
                                }
                                foreach (var match in peptideToProteinMap[cleanSequence])
                                {
                                    for (int i = 0; i < mods.Count; ++i)
                                    {
                                        // Original AScore data
                                        mappedWriter.Write(line + "\t");
                                        // Protein Name
                                        mappedWriter.Write(match.proteinName + "\t");
                                        // Protein Description
                                        if (fastaOptions.OutputProteinDescriptions)
                                        {
                                            mappedWriter.Write(proteinDescriptions[match.proteinName] + "\t");
                                        }
                                        // # of proteins occurred in
                                        mappedWriter.Write(peptideToProteinMap[cleanSequence].Count + "\t");
                                        // Residue of mod
                                        mappedWriter.Write(noPrefixSequence[mods[i] - 1] + "\t");
                                        // Position of residue
                                        // With multiple residues, we need to adjust the position of each subsequent residue by the number of residues we have read
                                        mappedWriter.WriteLine(match.residueStart + mods[i] - i - 1);
                                    }
                                }
                            }
                        }
                    }
                }
	        }
	        return 0;
	    }

		private static int RunAScore(AScoreOptionsType ascoreOptions, string logFilePath, string supportedSearchModes, bool multiJobMode, out AScoreRunDataType aScoreRunData)
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
		    aScoreRunData = new AScoreRunDataType();
            aScoreRunData.diOutputFolder = diOutputFolder;
		    aScoreRunData.AScoreResultsFilePath = ascoreResultsFilePath;

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
