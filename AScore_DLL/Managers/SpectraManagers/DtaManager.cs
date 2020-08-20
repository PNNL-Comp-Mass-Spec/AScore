using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using PRISM;

//TODO: leverage mzXmlFileReader and random access indexing to insert experimental spectra as needed.  may want to use an abstract class that DtaManager can inherit from.  Make all the same calls.
//Need to add some intelligence for grabbing msxml instead of dta when requesting the msgfdb results.

namespace AScore_DLL.Managers.SpectraManagers
{
    /// <summary>
    /// Provides an interface to extract individual dta files from the master dta file
    /// </summary>
    public class DtaManager : EventNotifier, ISpectraManager
    {
        // Ignore Spelling: dta, msxml, msgfdb

        public static string GetFilePath(string datasetFilePath, string datasetName)
        {
            var datasetFile = new FileInfo(datasetFilePath);
            return GetFilePath(datasetFile.Directory, datasetName);
        }

        public static string GetFilePath(DirectoryInfo datasetDirectory, string datasetName)
        {
            var dtaFilePath = datasetName + "_dta.txt";
            if (datasetDirectory != null)
            {
                dtaFilePath = Path.Combine(datasetDirectory.FullName, dtaFilePath);
            }

            return dtaFilePath;
        }

        string ISpectraManager.GetFilePath(string datasetFilePath, string datasetName)
        {
            return GetFilePath(datasetFilePath, datasetName);
        }

        #region Class Members

        #region Variables

        private string m_datasetName;
        private StreamReader m_masterDta;
        private readonly Dictionary<string, long> dtaEntries = new Dictionary<string, long>();

        private readonly PHRPReader.clsPeptideMassCalculator m_PeptideMassCalculator;

        private bool m_initialized;

        #endregion // Variables

        #region Properties

        public string DatasetName => m_datasetName;

        public bool Initialized => m_initialized;

        #endregion // Properties

        #endregion // Class Members

        #region Constructor

        /// <summary>
        /// Initializes a DtaManager for which we don't yet know the path of the CDTA file to read
        /// </summary>
        /// <remarks>You must call UpdateDtaFilePath() prior to using GetDtaFileName() or GetExperimentalSpectra()</remarks>
        public DtaManager(PHRPReader.clsPeptideMassCalculator peptideMassCalculator)
        {
            m_PeptideMassCalculator = peptideMassCalculator;
            m_initialized = false;
        }

        /// <summary>
        /// Initializes a new instance of DtaManger.
        /// </summary>
        /// <param name="masterDtaPath">Pathname of the master dta file.</param>
        /// <exception cref="DirectoryNotFoundException"></exception>
        /// <exception cref="FileNotFoundException"></exception>
        public DtaManager(string masterDtaPath)
        {
            OpenFile(masterDtaPath);
        }

        #endregion // Constructor

        #region Destructor

        /// <summary>
        /// Closes the open file stream for the dta file and clears the
        /// internal dictionary
        /// </summary>
        ~DtaManager()
        {
            ClearCachedData();
            m_initialized =false;
        }

        #endregion // Destructor

        #region Public Methods

        public void OpenFile(string filePath)
        {
            if (string.Equals(filePath, m_datasetName))
            {
                // Don't reopen the same file.
                return;
            }

            if (m_initialized)
                ClearCachedData();

            try
            {
                m_datasetName = Path.GetFileNameWithoutExtension(filePath);
                if (string.IsNullOrEmpty(m_datasetName))
                    throw new FileNotFoundException("Master Dta filename is empty");

                m_datasetName = Utilities.TrimEnd(m_datasetName, "_dta");

                Initialize(filePath);
            }
            catch (DirectoryNotFoundException)
            {
                throw new DirectoryNotFoundException("The specified directory for " +
                                                     "the Master DTA could not be found!");
            }
            catch (FileNotFoundException)
            {
                var fileName = filePath.Substring(filePath.LastIndexOf('\\') + 1);
                throw new FileNotFoundException("The specified Master DTA file \"" +
                                                fileName + "\" could not be found!");
            }
        }

        public void Abort()
        {
            m_masterDta?.Close();
        }

        //Gets a spectra entry from the dta file
        public string GetDtaFileName(int scanNumber, int scanCount, int chargeState)
        {
            return GetDtaFileName(scanNumber, scanCount, chargeState, scanPrefixPad: "");
        }

        public string GetDtaFileName(int scanNumber, int scanCount, int chargeState, string scanPrefixPad)
        {
            if (!m_initialized)
                throw new Exception("Class has not yet been initialized; call OpenCDTAFile() before calling this function");

            if (string.IsNullOrWhiteSpace(scanPrefixPad))
                scanPrefixPad = string.Empty;

            var scanStart = scanPrefixPad + scanNumber;
            var scanEnd = scanPrefixPad + (scanNumber + scanCount - 1);

            return string.Format("{0}.{1}.{2}.{3}.dta",
                m_datasetName, scanStart,
                scanEnd, chargeState);
        }

        /// <summary>
        /// Retrieves an experimental spectra entry from the Master DTA file.
        /// </summary>
        /// <returns>A newly constructed ExperimentalSpectra if the specified
        /// spectra name exists, null if it does not.</returns>
        public ExperimentalSpectra GetExperimentalSpectra(int scanNumber, int scanCount, int psmChargeState)
        {
            if (!m_initialized)
                throw new Exception("Class has not yet been initialized; call OpenCDTAFile() before calling this function");

            var dtaChargeState = psmChargeState;

            // Find the desired spectrum
            // Dictionary keys are the header text for each DTA in the _DTA.txt file, for example:
            // MyDataset.0538.0538.3.dta
            // Note that scans could have one or more leading zeros, so we may need to check for that

            var spectraName = GetDtaFileName(scanNumber, scanCount, dtaChargeState);
            if (!dtaEntries.ContainsKey(spectraName))
            {
                var lstCharges = new List<int>
                {
                    dtaChargeState
                };

                for (var alternateCharge = 1; alternateCharge < 10; alternateCharge++)
                {
                    if (alternateCharge != dtaChargeState)
                        lstCharges.Add(alternateCharge);
                }

                foreach (var chargeState in lstCharges)
                {
                    var matchFound = false;
                    for (var padLength = 0; padLength <= 6; padLength++)
                    {
                        var scanPrefixPad = new string('0', padLength);
                        spectraName = GetDtaFileName(scanNumber, scanCount, chargeState, scanPrefixPad);

                        if (dtaEntries.ContainsKey(spectraName))
                        {
                            matchFound = true;
                            break;
                        }
                    }
                    if (matchFound)
                        break;
                }
            }

            if (!dtaEntries.ContainsKey(spectraName))
                return null;

            var reScan = new Regex(@"scan=(\d+)");
            var reCS = new Regex(@"cs=(\d+)");

            var precursorChargeState = 0;
            var entries = new List<ExperimentalSpectraEntry>();

            // Set the Master DTAs file position to the specified spectra
            m_masterDta.DiscardBufferedData();
            m_masterDta.BaseStream.Position = dtaEntries[spectraName];

            // Read the first line of the entry and extract the precursor
            // entries as well as the scan number and charge state
            var line = m_masterDta.ReadLine();

            if (string.IsNullOrWhiteSpace(line))
            {
                OnWarningEvent("Data not found for DTA " + spectraName);
                return null;
            }

            var splitChars = new[] { ' ' };

            // Determine the precursor mass
            // The mass listed in the DTA file is the M+H mass
            // Example line:
            // 1196.03544724 3   scan=99 cs=3

            var precursorInfo = line.Split(splitChars, 3);
            if (precursorInfo.Length < 1)
            {
                OnWarningEvent("Precursor line is empty for DTA " + spectraName);
                return null;
            }

            double.TryParse(precursorInfo[0], out var precursorMass);

            // Parse out charge state
            if (precursorInfo.Length > 1)
            {
                int.TryParse(precursorInfo[1], out precursorChargeState);

                // Parse out scan number (if it's present)
                if (precursorInfo.Length > 2)
                {
                    var reMatch = reScan.Match(line);
                    if (reMatch.Success)
                    {
                        int.TryParse(reMatch.Groups[1].Value, out scanNumber);
                    }

                    // Additional CS
                    reMatch = reCS.Match(line);
                    if (reMatch.Success)
                    {
                        int.TryParse(reMatch.Groups[1].Value, out dtaChargeState);
                    }
                }
            }

            if (precursorChargeState != dtaChargeState)
            {
                OnWarningEvent("Charge state mismatch: dtaChargeState=" + dtaChargeState + " vs. precursorChargeState=" + precursorChargeState);
                dtaChargeState = precursorChargeState;
            }

            // Process the rest of the entries in this spectra
            line = m_masterDta.ReadLine();
            while (!string.IsNullOrWhiteSpace(line) && !line.Contains("=") && (!m_masterDta.EndOfStream))
            {
                var massAndIntensity = line.Split(splitChars, 3);

                if (massAndIntensity.Length > 1)
                {
                    // Get the first number
                    double.TryParse(massAndIntensity[0], out var ionMz);

                    // Get the second number
                    double.TryParse(massAndIntensity[1], out var ionIntensity);

                    // Add this entry to the entries list
                    entries.Add(new ExperimentalSpectraEntry(ionMz, ionIntensity));
                }

                // Read the next line
                line = m_masterDta.ReadLine();
            }

            if (precursorChargeState != psmChargeState)
            {
                // Convert precursor mass from M+H to m/z
                var precursorMZ = m_PeptideMassCalculator.ConvoluteMass(precursorMass, 1, precursorChargeState);

                // Convert precursor m/z to the correct M+H value
                precursorMass = m_PeptideMassCalculator.ConvoluteMass(precursorMZ, psmChargeState, 1);
                precursorChargeState = psmChargeState;
            }

            // Finally, create the new ExperimentalSpectra
            var expSpec = new ExperimentalSpectra(scanNumber, psmChargeState,
                precursorMass, precursorChargeState, entries, m_PeptideMassCalculator);

            return expSpec;
        }

        #endregion // Public Methods

        #region Private Methods

        private void ClearCachedData()
        {
            dtaEntries?.Clear();

            if (m_masterDta != null)
            {
                m_masterDta.Close();
                m_masterDta.Dispose();
            }
        }

        /// <summary>
        /// Initializes the internal dictionary with the offsets of the files located
        /// in the master dta.
        /// </summary>
        private void Initialize(string masterDtaPath)
        {
            long bytesRead = 0;
            var line = string.Empty;

            try
            {
                m_masterDta = new StreamReader(masterDtaPath);
                while (!m_masterDta.EndOfStream)
                {
                    // Find the next individual dta file entry
                    while (!m_masterDta.EndOfStream && (string.IsNullOrEmpty(line) || !line.Contains("\"")))
                    {
                        line = m_masterDta.ReadLine();
                        if (line != null)
                        {
                            bytesRead += line.Length + Environment.NewLine.Length;
                        }
                    }

                    // If we're not at the end of the file get the next entry
                    if (m_masterDta.EndOfStream || string.IsNullOrEmpty(line))
                        continue;

                    // First extract the name of this dta entry
                    var entryNameIndex = line.IndexOf('\"') + 1;
                    var entryNameLength = line.LastIndexOf('\"') - entryNameIndex;
                    var entryName = line.Substring(entryNameIndex, entryNameLength);

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
            catch (Exception ex)
            {
                throw new Exception("Error initializing the DtaManager using " + m_masterDta + ": " + ex.Message);
            }

            m_initialized = true;
        }

        #endregion // Private Methods
    }
}
