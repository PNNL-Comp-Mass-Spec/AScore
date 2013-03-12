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
                    Console.WriteLine(" -F:fhtfile_path");
                    Console.WriteLine(" -D:dta_file_path");
                    Console.WriteLine(" -P:parameter_file");
                    Console.WriteLine(" -O:output_filepath");
					Console.WriteLine(" -FM:true  (true or false to enable/disable filtering on data in column MSGF_SpecProb; default is true)");
					Console.WriteLine();
                    Console.WriteLine("Example command line:");
					Console.WriteLine(System.IO.Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location) + 
						              " -T:sequest\n" +
						              " -F:\"C:\\Temp\\GmaxP_itraq_NiNTA_15_29Apr10_Hawk_03-10-09p_fht.txt\"\n" +
									  " -D:\"C:\\Temp\\GmaxP_itraq_NiNTA_15_29Apr10_Hawk_03-10-09p_dta.txt\"\n" +
									  " -O:\"C:\\Temp\\GmaxP_itraq_NiNTA_15_29Apr10_Hawk_03-10-09.txt\"\n" + 
                                      " -P:C:\\Temp\\parameterFileForGmax.xml\n" +
									  " -FM:true");

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

                ParameterFileManager paramManager = new ParameterFileManager(paramFile);
				DtaManager dtaManager = new DtaManager(dtaFile);
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
						Console.WriteLine("Incorrect search type: " + searchType + " , supported values are " + supportedSearchModes);
						clu.PauseAtConsole(2000, 333);
                        return -13;
                }

				System.IO.DirectoryInfo diOutputFolder = new System.IO.DirectoryInfo(outPath);
				if (!diOutputFolder.Exists)
				{
					try
					{
						Console.WriteLine("Folder not found, " + diOutputFolder.FullName);
						Console.WriteLine("Creating now");
						diOutputFolder.Create();
					}
					catch (Exception ex)
					{
						Console.WriteLine("Error creating the output folder: " + ex.Message);
						diOutputFolder = new System.IO.DirectoryInfo(".");
						Console.WriteLine("Changed output to " + diOutputFolder.FullName);
					}
				}
				else
				{
					Console.WriteLine("Writing results to " + diOutputFolder.FullName);
				}

				string outputFilePath = System.IO.Path.Combine(diOutputFolder.FullName, System.IO.Path.GetFileNameWithoutExtension(fhtFile) + "_ascore.txt");
				AScore_DLL.Algorithm.AlgorithmRun(dtaManager, datasetManager, paramManager, outputFilePath, filterOnMSGFScore);

				Console.WriteLine("Success");
			}
			catch (Exception ex)
			{
			    Console.WriteLine();
			    Console.WriteLine("Program failure, possibly incorrect search engine type; " + ex.Message);
			    return ((int)ex.Message.GetHashCode());
			}

			return 0;
		
		}


	}
}
