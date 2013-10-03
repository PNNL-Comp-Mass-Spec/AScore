using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

//TODO: leverage mzxmlfilereader and random access indexing to insert experimental spectra as needed.  may want to use an abstract class that DTAmanager can inherit from.  Make all the same calls.  
//Need to add some intelligence fro grabbing msxml instead of dta when requesting the msgfdb results.
namespace AScore_DLL.Managers
{
	/// <summary>
	/// Provides an interface to extract individual dta files from the master dta file
	/// </summary>
	public class DtaManager : AScore_DLL.MessageEventBase, AScore_DLL.Managers.SpectraManager
	{
		#region Class Members

		#region Variables

		private string datasetName;
		private StreamReader masterDta = null;
		private Dictionary<string, long> dtaEntries = new Dictionary<string, long>();

		#endregion // Variables

		#region Properties

		#endregion // Properties

		#endregion // Class Members

		#region Constructor

		/// <summary>
		/// Initializes a new instance of DtaManger.
		/// </summary>
		/// <param name="masterDtaPath">Pathname of the master dta file.</param>
		/// <exception cref="DirectoryNotFoundException"></exception>
		/// <exception cref="FileNotFoundException"></exception>
		public DtaManager(string masterDtaPath)
		{
			try
			{
				datasetName = System.IO.Path.GetFileNameWithoutExtension(masterDtaPath);
				datasetName = datasetName.Substring(0, datasetName.Length - 4);
				masterDta = new StreamReader(masterDtaPath);
				Initialize();
			}
			catch (DirectoryNotFoundException)
			{
				throw new DirectoryNotFoundException("The specified directory for " +
					"the Master DTA could not be found!");
			}
			catch (FileNotFoundException)
			{
				string fileName = masterDtaPath.Substring(
					masterDtaPath.LastIndexOf('\\') + 1);
				throw new FileNotFoundException("The specified Master DTA file \"" +
					fileName + "\" could not be found!");
			}
		}

		#endregion // Constructor

		#region Destructor

		/// <summary>
		/// Closes the open file stream for the dta file and clears the 
		/// internal dictionary
		/// </summary>
		~DtaManager()
		{
			dtaEntries.Clear();
			dtaEntries = null;
			if (masterDta != null)
			{
				masterDta.Close();
				masterDta.Dispose();
			}
		}

		#endregion // Destructor

		#region Public Methods

		public void Abort()
		{
			if (masterDta != null)
			{
				masterDta.Close();
			}
		}

		//Gets a spectra entry from the dta file
		public string GetDtaFileName(int scanNumber, int scanCount, int chargeState)
		{
			return GetDtaFileName(scanNumber, scanCount, chargeState, scanPrefixPad: "");
		}

		public string GetDtaFileName(int scanNumber, int scanCount, int chargeState, string scanPrefixPad)
		{
			if (string.IsNullOrWhiteSpace(scanPrefixPad))
				scanPrefixPad = string.Empty;

			string scanStart = scanPrefixPad + scanNumber;
			string scanEnd = scanPrefixPad + (scanNumber + scanCount - 1);

			return string.Format("{0}.{1}.{2}.{3}.dta",
				datasetName, scanStart,
				scanEnd, chargeState);
		}




		/// <summary>
		/// Retrieves an experimental spectra entry from the Master DTA file.
		/// </summary>
		/// <returns>A newly constructed ExperimentalSpectra if the specified
		/// spectra name exists, null if it does not.</returns>
		public ExperimentalSpectra GetExperimentalSpectra(int scanNumber, int scanCount, int psmChargeState)
		{
			int dtaChargeState = psmChargeState;
			string spectraName = String.Empty;

			// Find the desired spectrum
			// Dictionary keys are the header text for each DTA in the _DTA.txt file, for example:
			// MyDataset.0538.0538.3.dta
			// Note that scans could have one or more leading zeroes, so we need to account for that

			var lstCharges = new List<int> { dtaChargeState };
			for (int alternateCharge = 1; alternateCharge < 10; alternateCharge++)
			{
				if (alternateCharge != dtaChargeState)
					lstCharges.Add(alternateCharge);
			}

			foreach (int chargeState in lstCharges)
			{
				bool matchFound = false;
				for (int padLength = 0; padLength <= 6; padLength++)
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


			if (!dtaEntries.ContainsKey(spectraName))
				return null;

			double precursorMass = 0.0;
			int precursorChargeState = 0;
			var entries = new List<ExperimentalSpectraEntry>();

			// Set the Master DTAs file position to the specified spectra
			masterDta.DiscardBufferedData();
			masterDta.BaseStream.Position = dtaEntries[spectraName];

			// Read the first line of the entry and extract the precursor
			// entries as well as the scan number and charge state
			string line = masterDta.ReadLine();

			if (string.IsNullOrWhiteSpace(line))
			{
				ReportWarning("Data not found for DTA " + spectraName);
				return null;
			}

			// Determine the precursor mass
			// The mass listed in the DTA file is the M+H mass
			int ind1 = 0;
			int ind2 = line.IndexOf(' ');
			double.TryParse(line.Substring(ind1, ind2), out precursorMass);

			// Parse out charge state
			ind1 = ind2 + 1;
			ind2 = line.IndexOf(' ', ind1);
			if (ind2 > -1)
			{
				int.TryParse(line.Substring(ind1, ind2 - ind1), out precursorChargeState);

				// Parse out scan number (if it's present)
				ind1 = line.IndexOf('=') + 1;
				ind2 = line.IndexOf(' ', ind1);
				int.TryParse(line.Substring(ind1, ind2 - ind1), out scanNumber);

				// Charge state
				ind1 = line.LastIndexOf('=') + 1;
				int.TryParse(line.Substring(ind1, 1), out dtaChargeState);
			}
			else
			{
				int.TryParse(line.Substring(ind1), out precursorChargeState);
			}

			if (precursorChargeState != dtaChargeState)
			{
				ReportWarning("Charge state mismatch: dtaChargeState=" + dtaChargeState + " vs. precursorChargeState=" + precursorChargeState);
				dtaChargeState = precursorChargeState;
			}

			// Process the rest of the entries in this spectra
			line = masterDta.ReadLine();
			while (!string.IsNullOrWhiteSpace(line) && !line.Contains("=") && (!masterDta.EndOfStream))
			{
				// Get the first number
				ind1 = 0;
				ind2 = line.IndexOf(' ');
				double val1 = 0.0;
				double.TryParse(line.Substring(ind1, ind2), out val1);

				// Get the second number
				ind1 = ind2 + 1;
				double val2 = 0.0;
				double.TryParse(line.Substring(ind1, line.Length - ind1), out val2);

				// Add this entry to the entries list
				entries.Add(new ExperimentalSpectraEntry(val1, val2));

				// Read the next line
				line = masterDta.ReadLine();
			}

			if (precursorChargeState != psmChargeState)
			{
				// Convert precursor mass from M+H to m/z
				double precursorMZ = PHRPReader.clsPeptideMassCalculator.ConvoluteMass(precursorMass, 1, precursorChargeState);

				// Convert precursor m/z to the correct M+H value
				precursorMass = PHRPReader.clsPeptideMassCalculator.ConvoluteMass(precursorMZ, psmChargeState, 1);
				precursorChargeState = psmChargeState;
			}

			// Finally, create the new ExperimentalSpectra
			var expSpec = new ExperimentalSpectra(scanNumber, psmChargeState,
				precursorMass, precursorChargeState, entries);

			return expSpec;
		}

		#endregion // Public Methods

		#region Private Methods

		/// <summary>
		/// Initializes the internal dictionary with the offsets of the files located
		/// in the master dta.
		/// </summary>
		private void Initialize()
		{
			long bytesRead = 0;
			string line = string.Empty;

			while (!masterDta.EndOfStream)
			{
				// Find the next individual dta file entry
				while ((!line.Contains("\"")) && (!masterDta.EndOfStream))
				{
					line = masterDta.ReadLine();
					bytesRead += line.Length + Environment.NewLine.Length;
				}

				// If we're not at the end of the file get the next entry
				if (!masterDta.EndOfStream)
				{
					// First extract the name of this dta entry
					int entryNameIndex = line.IndexOf('\"') + 1;
					int entryNameLength = line.LastIndexOf('\"') - entryNameIndex;
					string entryName = line.Substring(entryNameIndex, entryNameLength);

					// Add it to the dictionary
					dtaEntries.Add(entryName, bytesRead);

					// Read the next line from the file
					line = masterDta.ReadLine();
					bytesRead += line.Length + Environment.NewLine.Length;
				}
			}
		}

		#endregion // Private Methods
	}
}