using System;
using System.IO;
using AScore_DLL.Managers;
using AScore_DLL.Managers.DatasetManagers;
using AScore_DLL.Managers.SpectraManagers;
using PHRPReader;
using PRISM;

namespace AScore_DLL
{
    public class AScoreProcessor : clsEventNotifier
    {
        /// <summary>
        /// Configure and run the AScore DLL
        /// </summary>
        /// <param name="ascoreOptions"></param>
        /// <returns></returns>
        public int RunAScore(IAScoreOptions ascoreOptions)
        {
            var paramManager = new ParameterFileManager(ascoreOptions.AScoreParamFile);
            RegisterEvents(paramManager);

            DatasetManager datasetManager;

            switch (ascoreOptions.SearchType)
            {
                case SearchMode.XTandem:
                    OnStatusEvent("Caching data in " + Path.GetFileName(ascoreOptions.DbSearchResultsFile));
                    datasetManager = new XTandemFHT(ascoreOptions.DbSearchResultsFile);
                    break;
                case SearchMode.Sequest:
                    OnStatusEvent("Caching data in " + Path.GetFileName(ascoreOptions.DbSearchResultsFile));
                    datasetManager = new SequestFHT(ascoreOptions.DbSearchResultsFile);
                    break;
                case SearchMode.Inspect:
                    OnStatusEvent("Caching data in " + Path.GetFileName(ascoreOptions.DbSearchResultsFile));
                    datasetManager = new InspectFHT(ascoreOptions.DbSearchResultsFile);
                    break;
                case SearchMode.Msgfdb:
                case SearchMode.Msgfplus:
                    OnStatusEvent("Caching data in " + Path.GetFileName(ascoreOptions.DbSearchResultsFile));
                    if (ascoreOptions.DbSearchResultsFile.ToLower().Contains(".mzid"))
                    {
                        datasetManager = new MsgfMzid(ascoreOptions.DbSearchResultsFile);
                    }
                    else
                    {
                        datasetManager = new MsgfdbFHT(ascoreOptions.DbSearchResultsFile);
                    }
                    break;
                default:
                    OnErrorEvent("Incorrect search type: " + ascoreOptions.SearchType + " , supported values are " + string.Join(", ", Enum.GetNames(typeof(SearchMode))));
                    return -13;
            }
            var peptideMassCalculator = new clsPeptideMassCalculator();

            var spectraManager = new SpectraManagerCache(peptideMassCalculator);

            RegisterEvents(spectraManager);

            OnStatusEvent("Output folder: " + ascoreOptions.OutputDirectoryInfo.FullName);

            var ascoreEngine = new AScore_DLL.Algorithm();
            RegisterEvents(ascoreEngine);

            // Initialize the options
            ascoreEngine.FilterOnMSGFScore = ascoreOptions.FilterOnMSGFScore;

            // Run the algorithm
            if (ascoreOptions.MultiJobMode)
            {
                ascoreEngine.AlgorithmRun(ascoreOptions.JobToDatasetMapFile, spectraManager, datasetManager, paramManager, ascoreOptions.AScoreResultsFilePath, ascoreOptions.FastaFilePath, ascoreOptions.OutputProteinDescriptions);
            }
            else
            {
                spectraManager.OpenFile(ascoreOptions.MassSpecFile);

                ascoreEngine.AlgorithmRun(spectraManager, datasetManager, paramManager, ascoreOptions.AScoreResultsFilePath, ascoreOptions.FastaFilePath, ascoreOptions.OutputProteinDescriptions);
            }

            OnStatusEvent("AScore Complete");

            if (ascoreOptions.CreateUpdatedDbSearchResultsFile && ascoreOptions.SearchResultsType == DbSearchResultsType.Fht)
            {
                CreateUpdatedFirstHitsFile(ascoreOptions);
            }

            return 0;
        }

        /// <summary>
        /// Reads the ascore results and merges them into the FHT file
        /// </summary>
        /// <param name="ascoreOptions"></param>
        public void CreateUpdatedFirstHitsFile(IAScoreOptions ascoreOptions)
        {
            var resultsMerger = new PHRPResultsMerger();
            RegisterEvents(resultsMerger);

            resultsMerger.MergeResults(ascoreOptions.DbSearchResultsFile, ascoreOptions.AScoreResultsFilePath, ascoreOptions.UpdatedDbSearchResultsFileName);

            OnStatusEvent("Results merged; new file: " + Path.GetFileName(resultsMerger.MergedFilePath));
        }
    }
}
