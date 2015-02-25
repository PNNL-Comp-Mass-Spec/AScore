using System;
using System.IO;

namespace AScore_DLL.Managers.SpectraManagers
{
    public class SpectraManagerCache : AScore_DLL.MessageEventBase
    {
        private MzMLManager _mzMLManager;
        private DtaManager _dtaManager;
        private ISpectraManager _lastOpened = null;
        private string _lastDatasetOpened;

        public SpectraManagerCache()
        {
            _mzMLManager = new MzMLManager();
            _dtaManager = new DtaManager();
            AttachEvents(_dtaManager);
            AttachEvents(_mzMLManager);
        }

        public bool Initialized
        {
            //get { return _mzMLManager.Initialized || _dtaManager.Initialized; }
            get { return _lastOpened != null && _lastOpened.Initialized; }
        }

        public string GetFilePath(string datasetFilePath, string datasetName)
        {
            var mzMLFile = MzMLManager.GetFilePath(datasetFilePath, datasetName);
            var dtaFile = DtaManager.GetFilePath(datasetFilePath, datasetName);
            if (File.Exists(mzMLFile))
            {
                return mzMLFile;
            }
            if (File.Exists(dtaFile))
            {
                return dtaFile;
            }
            ReportError("Could not file spectra file for dataset \"" + datasetName + "\"");
            return null;
        }

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
