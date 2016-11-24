using System;
using System.Collections.Generic;
using System.IO;

namespace AScore_DLL.Managers.SpectraManagers
{
    public class SpectraManagerCache : AScore_DLL.MessageEventBase
    {
        private readonly MzMLManager _mzMLManager;
        private readonly DtaManager _dtaManager;
        private ISpectraManager _lastOpened = null;
        private string _lastDatasetOpened;

        /// <summary>
        /// Constructor
        /// </summary>
        public SpectraManagerCache(PHRPReader.clsPeptideMassCalculator peptideMassCalculator)
        {
            _mzMLManager = new MzMLManager(peptideMassCalculator);
            _dtaManager = new DtaManager(peptideMassCalculator);
            AttachEvents(_dtaManager);
            AttachEvents(_mzMLManager);
        }

        public bool Initialized
        {
            //get { return _mzMLManager.Initialized || _dtaManager.Initialized; }
            get { return _lastOpened != null && _lastOpened.Initialized; }
        }

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
            var foldersToCheck = new List<DirectoryInfo> { fiDatasetFile.Directory };

            if (fiDatasetFile.Directory.Parent != null)
            {
                foldersToCheck.Add(fiDatasetFile.Directory.Parent);
            }

            foreach (var folder in foldersToCheck)
            {
                var mzMLFile = MzMLManager.GetFilePath(folder, datasetName);
                var dtaFile = DtaManager.GetFilePath(folder, datasetName);

                if (File.Exists(mzMLFile))
                {
                    return mzMLFile;
                }
                if (File.Exists(dtaFile))
                {
                    return dtaFile;
                }
            }

            // _dta.txt or .mzML file not found (checked both the folder with the dataset file and the parent folder)

            ReportError(string.Format("Could not find the spectra file for dataset \"{0}\" in {1} or one folder up", datasetName, fiDatasetFile.Directory.FullName));

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
                string errorMessage = "Could not find spectra file for dataset \"" + datasetName + "\" in path \"" + Path.GetDirectoryName(datasetFilePath) + "\"";
                ReportError(errorMessage);
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

        public string DatasetName
        {
            get { return _lastDatasetOpened; }
        }

        /// <summary>
        /// Attaches the Error, Warning, and Message events to the local event handler (which passes them to the parent event handler)
        /// </summary>
        /// <param name="paramManager"></param>
        private void AttachEvents(AScore_DLL.MessageEventBase paramManager)
        {
            paramManager.ErrorEvent += OnErrorMessage;
            paramManager.WarningEvent += OnMessage;
            paramManager.MessageEvent += OnWarningMessage;
        }
    }
}
