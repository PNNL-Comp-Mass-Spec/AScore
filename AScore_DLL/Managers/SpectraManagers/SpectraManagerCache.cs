using System;
using System.Collections.Generic;
using System.IO;
using PRISM;

namespace AScore_DLL.Managers.SpectraManagers
{
    public class SpectraManagerCache : EventNotifier
    {
        // Ignore Spelling: psm

        private readonly MzMLManager _mzMLManager;
        private readonly DtaManager _dtaManager;
        private ISpectraManager _currentSpectrumManager;

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
        /// Determine the input spectrum file path, given psmResultsFilePath
        /// </summary>
        /// <param name="psmResultsFilePath">_fht.txt or _syn.txt file</param>
        /// <param name="datasetName">Dataset name</param>
        /// <returns></returns>
        public string GetSpectrumFilePath(string psmResultsFilePath, string datasetName)
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

        public ISpectraManager GetCurrentSpectrumManager()
        {
            return _currentSpectrumManager;
        }

        /// <summary>
        /// Open the .mzML or _dta.txt file that corresponds to the _fht.txt or _syn.txt file specified by psmResultsFilePath
        /// </summary>
        /// <param name="psmResultsFilePath">_fht.txt or _syn.txt file</param>
        /// <param name="datasetName">Dataset name</param>
        /// <param name="modSummaryFilePath">_ModSummary.txt file path, or an empty string to have AScore auto-find it</param>
        /// <returns></returns>
        public ISpectraManager GetSpectraManagerForFile(string psmResultsFilePath, string datasetName, string modSummaryFilePath)
        {
            var spectrumFilePath = GetSpectrumFilePath(psmResultsFilePath, datasetName);

            if (string.IsNullOrWhiteSpace(spectrumFilePath))
            {
                var errorMessage = "Could not find spectra file for dataset \"" + datasetName + "\" in path \"" + Path.GetDirectoryName(psmResultsFilePath) + "\"";
                OnErrorEvent(errorMessage);
                throw new Exception(errorMessage);
            }

            OpenFile(spectrumFilePath, modSummaryFilePath);

            return _currentSpectrumManager;
        }

        public void OpenFile(string spectrumFilePath, string modSummaryFilePath = "")
        {
            ModSummaryFilePath = modSummaryFilePath;

            if (spectrumFilePath.EndsWith(".mzML", StringComparison.OrdinalIgnoreCase) ||
                spectrumFilePath.EndsWith(".mzML.gz", StringComparison.OrdinalIgnoreCase))
            {
                _currentSpectrumManager = _mzMLManager;
            }
            else
            {
                _currentSpectrumManager = _dtaManager;
            }
            SpectrumFilePath = spectrumFilePath;

            OnStatusEvent("Opening " + PathUtils.CompactPathString(SpectrumFilePath, 80));
            _currentSpectrumManager.OpenFile(spectrumFilePath);
        }

        public string ModSummaryFilePath { get; private set; }

        public string SpectrumFilePath { get; private set; }

        public override string ToString()
        {
            return SpectrumFilePath ?? string.Empty;
        }
    }
}
