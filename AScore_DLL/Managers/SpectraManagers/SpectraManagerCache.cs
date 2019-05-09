using System;
using System.Collections.Generic;
using System.IO;
using PRISM;

namespace AScore_DLL.Managers.SpectraManagers
{
    public class SpectraManagerCache : EventNotifier
    {
        private readonly MzMLManager _mzMLManager;
        private readonly DtaManager _dtaManager;
        private ISpectraManager _lastOpened;
        private string _lastDatasetOpened;

        /// <summary>
        /// Constructor
        /// </summary>
        public SpectraManagerCache(PHRPReader.clsPeptideMassCalculator peptideMassCalculator)
        {
            _mzMLManager = new MzMLManager(peptideMassCalculator);
            _dtaManager = new DtaManager(peptideMassCalculator);
            RegisterEvents(_dtaManager);
            RegisterEvents(_mzMLManager);
        }

        public bool Initialized => _lastOpened != null && _lastOpened.Initialized;

        /// <summary>
        ///
        /// </summary>
        /// <param name="datasetFilePath">_fht.txt or _syn.txt file</param>
        /// <param name="datasetName">_dta.txt or .mzML file</param>
        /// <returns></returns>
        public string GetFilePath(string datasetFilePath, string datasetName)
        {
            var fiDatasetFile = new FileInfo(datasetFilePath);
            if (fiDatasetFile.Directory == null)
            {
                throw new DirectoryNotFoundException("Unable to determine the parent directory for " + datasetFilePath);
            }

            var directoriesToCheck = new List<DirectoryInfo> { fiDatasetFile.Directory };

            if (fiDatasetFile.Directory.Parent != null)
            {
                directoriesToCheck.Add(fiDatasetFile.Directory.Parent);
            }

            foreach (var directory in directoriesToCheck)
            {
                var mzMLFile = MzMLManager.GetFilePath(directory, datasetName);
                var dtaFile = DtaManager.GetFilePath(directory, datasetName);

                if (File.Exists(mzMLFile))
                {
                    return mzMLFile;
                }
                if (File.Exists(dtaFile))
                {
                    return dtaFile;
                }
            }

            // _dta.txt or .mzML file not found (checked both the directory with the dataset file and the parent directory)

            OnErrorEvent(string.Format("Could not find the spectra file for dataset \"{0}\" in {1} or one directory up", datasetName, fiDatasetFile.Directory.FullName));

            return null;
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="datasetFilePath">_fht.txt or _syn.txt file</param>
        /// <param name="datasetName">dataset name</param>
        /// <returns></returns>
        public ISpectraManager GetSpectraManagerForFile(string datasetFilePath, string datasetName)
        {
            var filePath = GetFilePath(datasetFilePath, datasetName);
            if (string.IsNullOrWhiteSpace(filePath))
            {
                var errorMessage = "Could not find spectra file for dataset \"" + datasetName + "\" in path \"" + Path.GetDirectoryName(datasetFilePath) + "\"";
                OnErrorEvent(errorMessage);
                throw new Exception(errorMessage);
            }
            OpenFile(filePath);
            return _lastOpened;
        }

        public void OpenFile(string filePath)
        {
            if (filePath.EndsWith(".mzML") || filePath.EndsWith(".mzML.gz"))
            {
                _lastOpened = _mzMLManager;
            }
            else
            {
                _lastOpened = _dtaManager;
            }
            _lastDatasetOpened = filePath;
            _lastOpened.OpenFile(filePath);
        }

        public string DatasetName => _lastDatasetOpened;
    }
}
