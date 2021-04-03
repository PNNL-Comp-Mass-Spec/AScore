using System;
using System.Collections.Generic;
using System.IO;
using PRISM;
using PSI_Interface.CV;
using PSI_Interface.MSData;

namespace AScore_DLL.Managers.SpectraManagers
{
    internal class MzMLManager : EventNotifier, ISpectraManager
    {
        // Ignore Spelling: dta

        public const double Proton = 1.00727649;

        public static string GetFilePath(string datasetFilePath, string datasetName)
        {
            var datasetFile = new FileInfo(datasetFilePath);
            return GetFilePath(datasetFile.Directory, datasetName);
        }

        public static string GetFilePath(DirectoryInfo datasetDirectory, string datasetName)
        {
            var mzMLFilePath = datasetName + ".mzML";
            if (datasetDirectory != null)
            {
                mzMLFilePath = Path.Combine(datasetDirectory.FullName, mzMLFilePath);
            }

            // Only grab a gzipped mzML file if an unzipped one doesn't exist.
            if (File.Exists(mzMLFilePath + ".gz") && !File.Exists(mzMLFilePath))
            {
                mzMLFilePath += ".gz";
            }

            return mzMLFilePath;
        }

        string ISpectraManager.GetFilePath(string datasetFilePath, string datasetName)
        {
            return GetFilePath(datasetFilePath, datasetName);
        }

        #region Variables

        private SimpleMzMLReader m_MzMLReader;
        //private List<MS2_Spectrum> m_ms2_spectra = null;
        private readonly Dictionary<string, SimpleMzMLReader> m_readers = new();
        protected string m_datasetName;
        protected bool m_initialized;

        private readonly PHRPReader.PeptideMassCalculator m_PeptideMassCalculator;
        #endregion // Variables

        #region Constructor

        /// <summary>
        /// Initializes a DtaManager for which we don't yet know the path of the CDTA file to read
        /// </summary>
        /// <remarks>You must call UpdateDtaFilePath() prior to using GetDtaFileName() or GetExperimentalSpectra()</remarks>
        public MzMLManager(PHRPReader.PeptideMassCalculator peptideMassCalculator)
        {
            m_PeptideMassCalculator = peptideMassCalculator;
            m_initialized = false;
        }

        /// <summary>
        /// Initializes a new instance of DtaManger.
        /// </summary>
        /// <param name="mzMLPath">Pathname of the master dta file.</param>
        /// <exception cref="DirectoryNotFoundException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        public MzMLManager(string mzMLPath)
        {
            m_initialized = false;
            OpenMzMLFile(mzMLPath);
        }

        #endregion // Constructor

        #region Destructor

        /// <summary>
        /// Closes the open file stream for the dta file and clears the
        /// internal dictionary
        /// </summary>
        ~MzMLManager()
        {
            //ClearCachedData();
            foreach (var reader in m_readers.Values)
            {
                reader.Close();
            }
            m_initialized = false;
        }

        #endregion // Destructor

        #region Public Methods

        public string DatasetName => m_datasetName;

        public bool Initialized => m_initialized;

        public void OpenFile(string filePath)
        {
            OpenMzMLFile(filePath);
        }

        public void OpenMzMLFile(string mzMLPath)
        {
            if (string.Equals(mzMLPath, m_datasetName))
            {
                // Don't reopen the same file.
                return;
            }

            if (m_initialized)
                ClearCachedData();

            try
            {
                m_datasetName = Path.GetFileNameWithoutExtension(mzMLPath);
                if (m_datasetName?.EndsWith(".mzML", StringComparison.OrdinalIgnoreCase) == true)
                {
                    m_datasetName = Path.GetFileNameWithoutExtension(m_datasetName);
                }

                if (string.IsNullOrEmpty(m_datasetName))
                    throw new FileNotFoundException("MzML filename is empty");

                m_datasetName = Utilities.TrimEnd(m_datasetName, "_FIXED");
                m_datasetName = Utilities.TrimEnd(m_datasetName, "_dta");

                Initialize(mzMLPath);
            }
            catch (DirectoryNotFoundException)
            {
                throw new DirectoryNotFoundException("The specified directory for " +
                                                     "the mzML could not be found!");
            }
            catch (FileNotFoundException)
            {
                var fileName = mzMLPath.Substring(
                    mzMLPath.LastIndexOf('\\') + 1);
                throw new FileNotFoundException("The specified mzML file \"" +
                                                fileName + "\" could not be found!");
            }
        }

        public void Abort()
        {
            foreach (var reader in m_readers.Values)
            {
                reader.Close();
            }
        }

        /// <summary>
        /// Retrieves an experimental spectra entry from the Master DTA file.
        /// </summary>
        /// <returns>A newly constructed ExperimentalSpectra if the specified
        /// spectra name exists, null if it does not.</returns>
        public ExperimentalSpectra GetExperimentalSpectra(int scanNumber, int scanCount, int psmChargeState)
        {
            if (!m_initialized)
                throw new Exception("Class has not yet been initialized; call OpenFile() before calling this function");

            var spectrum = m_MzMLReader.GetSpectrumForScan(scanNumber);
            if (spectrum == null)
            {
                return null;
            }

            // Find the desired spectrum
            // Dictionary keys are the header text for each DTA in the _DTA.txt file, for example:
            // MyDataset.0538.0538.3.dta
            // Note that scans could have one or more leading zeros, so we may need to check for that

            var entries = new List<ExperimentalSpectraEntry>();

            // Determine the precursor mass
            // The mass listed in the DTA file is the M+H mass
            // Example line:
            // precursorMass precursorCharge ScanNumber dtaChargeState
            // 1196.03544724 3   scan=99 cs=3

            var monoMzText = "";
            var chargeStateText = "";

            if (spectrum.Precursors.Count > 0 &&
                spectrum.Precursors[0].SelectedIons?.Count > 0)
            {
                foreach (var cvParam in spectrum.Precursors[0].SelectedIons[0].CVParams)
                {
                    switch (cvParam.TermInfo.Cvid)
                    {
                        case CV.CVID.MS_selected_ion_m_z:
                        case CV.CVID.MS_selected_precursor_m_z:
                            monoMzText = cvParam.Value;
                            break;

                        case CV.CVID.MS_charge_state:
                            chargeStateText = cvParam.Value;
                            break;
                    }
                }
            }

            double precursorMass = 0;
            var precursorChargeState = 0;

            if (double.TryParse(monoMzText, out var precursorMonoMz) &&
                int.TryParse(chargeStateText, out var precursorCharge))
            {
                precursorMass = CalculateMPlusHMass(precursorMonoMz, precursorCharge);
                precursorChargeState = (precursorCharge != 0 ? precursorCharge : 1);
            }
            //scanNumber = spectrum.ScanNum;

            // Process the spectra binary data
            for (var i = 0; i < spectrum.Mzs.Length; i++)
            {
                // Add this entry to the entries list
                entries.Add(new ExperimentalSpectraEntry(spectrum.Mzs[i], spectrum.Intensities[i]));
            }

            if (precursorChargeState != psmChargeState)
            {
                //int tempCharge = precursorChargeState;
                // Convert precursor mass from M+H to m/z
                var precursorMZ = m_PeptideMassCalculator.ConvoluteMass(precursorMass, 1, precursorChargeState);

                // Convert precursor m/z to the correct M+H value
                precursorMass = m_PeptideMassCalculator.ConvoluteMass(precursorMZ, psmChargeState);
                precursorChargeState = psmChargeState;
                //ReportWarning("Charge state for spectra \"" + scanNumber + "\" changed from \"" + tempCharge +
                //              "\" to \"" + precursorChargeState + "\"");
            }

            // Finally, create the new ExperimentalSpectra
            var expSpec = new ExperimentalSpectra(scanNumber, psmChargeState,
                precursorMass, precursorChargeState, entries, m_PeptideMassCalculator);

            return expSpec;
        }

        /// <summary>
        /// Computes M+H mass using m/z and charge.
        /// </summary>
        /// <param name="mz"></param>
        /// <param name="charge"></param>
        /// <returns></returns>
        public static double CalculateMPlusHMass(double mz, double charge)
        {
            if (!charge.Equals(0))
            {
                return ((mz - Proton) * charge) + Proton;
                //return (double)((mz * charge) - (charge * Proton) + Proton);
            }
            return mz;
        }

        #endregion // Public Methods

        #region Private Methods

        protected void ClearCachedData()
        {
            foreach (var reader in m_readers.Values)
            {
                reader.ClearDataCache();
            }
        }

        /// <summary>
        /// Initializes the internal dictionary with the offsets of the files located
        /// in the master dta.
        /// </summary>
        protected void Initialize(string mzMLPath)
        {
            if (!m_readers.ContainsKey(mzMLPath))
            {
                m_readers.Add(mzMLPath, new SimpleMzMLReader(mzMLPath, true));
            }

            m_MzMLReader = m_readers[mzMLPath];

            /*
            long bytesRead = 0;
            string line = string.Empty;

            try
            {
                m_masterDta = new StreamReader(masterDtaPath);
                while (!m_masterDta.EndOfStream)
                {
                    // Find the next individual dta file entry
                    while ((!line.Contains("\"")) && (!m_masterDta.EndOfStream))
                    {
                        line = m_masterDta.ReadLine();
                        if (line != null)
                        {
                            bytesRead += line.Length + Environment.NewLine.Length;
                        }
                    }

                    // If we're not at the end of the file get the next entry
                    if (!m_masterDta.EndOfStream)
                    {
                        // First extract the name of this dta entry
                        int entryNameIndex = line.IndexOf('\"') + 1;
                        int entryNameLength = line.LastIndexOf('\"') - entryNameIndex;
                        string entryName = line.Substring(entryNameIndex, entryNameLength);

                        // Add it to the dictionary
                        dtaEntries.Add(entryName, bytesRead);

                        // Read the next line from the file
                        line = m_masterDta.ReadLine();
                        if (line != null)
                        {
                            bytesRead += line.Length + Environment.NewLine.Length;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error initializing the MzMLManager using " + m_MzMLReader + ": " + ex.Message);
            }*/

            m_initialized = true;
        }

        #endregion // Private Methods
    }
}
