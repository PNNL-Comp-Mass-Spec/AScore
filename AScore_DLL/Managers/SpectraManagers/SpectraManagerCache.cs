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
        private ISpectraManager _currentSpectrumManager;
        private string _currentSpectrumFilePath;

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

        public bool Initialized => _currentSpectrumManager != null && _currentSpectrumManager.Initialized;

        /// <summary>
        ///
        /// </summary>
        /// <param name="psmResultsFilePath">_fht.txt or _syn.txt file</param>
        /// <param name="datasetName">_dta.txt or .mzML file</param>
        /// <returns></returns>
        public string GetFilePath(string psmResultsFilePath, string datasetName)
        {
            var psmResultsFile = new FileInfo(psmResultsFilePath);

            if (psmResultsFile.Directory == null)
            {
                throw new DirectoryNotFoundException("Unable to determine the parent directory for " + psmResultsFilePath);
            }

            var directoriesToCheck = new List<DirectoryInfo> { psmResultsFile.Directory };

            if (psmResultsFile.Directory.Parent != null)
            {
                directoriesToCheck.Add(psmResultsFile.Directory.Parent);
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

            OnErrorEvent(string.Format("Could not find the spectra file for dataset \"{0}\" in {1} or one directory up", datasetName, psmResultsFile.Directory.FullName));

            return null;
        }


        /// <summary>
        ///Open the .mzML or _dta.txt file that corresponds to the _fht.txt or _syn.txt file specified by psmResultsFilePath
        /// </summary>
        /// <param name="psmResultsFilePath">_fht.txt or _syn.txt file</param>
        /// <param name="datasetName">dataset name</param>
        /// <returns></returns>
        public ISpectraManager GetSpectraManagerForFile(string psmResultsFilePath, string datasetName)
        {
            var spectrumFilePath = GetFilePath(psmResultsFilePath, datasetName);
            if (string.IsNullOrWhiteSpace(spectrumFilePath))
            {
                var errorMessage = "Could not find spectra file for dataset \"" + datasetName + "\" in path \"" + Path.GetDirectoryName(psmResultsFilePath) + "\"";
                OnErrorEvent(errorMessage);
                throw new Exception(errorMessage);
            }
            OpenFile(spectrumFilePath);
            return _currentSpectrumManager;
        }

        public void OpenFile(string filePath)
        {
            if (filePath.EndsWith(".mzML") || filePath.EndsWith(".mzML.gz"))
            {
                _currentSpectrumManager = _mzMLManager;
            }
            else
            {
                _currentSpectrumManager = _dtaManager;
            }
            _currentSpectrumFilePath = filePath;
            _currentSpectrumManager.OpenFile(filePath);
        }

        public string SpectrumFilePath => _currentSpectrumFilePath;
    }
}
