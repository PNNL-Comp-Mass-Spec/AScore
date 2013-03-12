using System;
using System.Collections.Generic;
using System.IO;
using System.Text;


namespace AScore_DLL
{
	/// <summary>
	/// Provides an interface to extract individual dta files from the master dta file
	/// </summary>
	public class DtaManager
	{
		#region Class Members

		#region Variables

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

		/// <summary>
		/// Retrieves an experimental spectra entry from the Master DTA file.
		/// </summary>
		/// <param name="spectraName">Name of the spectra to retrieve</param>
		/// <returns>A newly constructed ExperimentalSpectra if the specified
		/// spectra name exists, null if it does not.</returns>
		public ExperimentalSpectra GetExperimentalSpectra(string spectraName)
		{
			ExperimentalSpectra expSpec = null;
			if (dtaEntries.ContainsKey(spectraName))
			{
				int scanNumber = 0;
				int chargeState = 0;
				double precursorMass = 0.0;
				int precursorChargeState = 0;
				List<ExperimentalSpectraEntry> entries =
					new List<ExperimentalSpectraEntry>();

				// Set the Master DTAs file position to the specified spectra
				masterDta.DiscardBufferedData();
				masterDta.BaseStream.Position = dtaEntries[spectraName];

				// Read the first line of the entry and extract the precursor
				// entries as well as the scan number and charge state
				string line = masterDta.ReadLine();

				// Precursor mass
				int ind1 = 0;
				int ind2 = line.IndexOf(' ');
				double.TryParse(line.Substring(ind1, ind2), out precursorMass);

				// Precursor mass
				ind1 = ind2 + 1;
				ind2 = line.IndexOf(' ', ind1);
				int.TryParse(line.Substring(ind1, ind2 - ind1), out precursorChargeState);

				// Scan number
				ind1 = line.IndexOf('=') + 1;
				ind2 = line.IndexOf(' ', ind1);
				int.TryParse(line.Substring(ind1, ind2 - ind1), out scanNumber);

				// Charge state
				ind1 = line.LastIndexOf('=') + 1;
				int.TryParse(line.Substring(ind1, 1), out chargeState);

				double val1 = 0.0;
				double val2 = 0.0;

				// Process the rest of the entries in this spectra
				line = masterDta.ReadLine();
				while ((line.Length > 0) && (!line.Contains("=")) &&
					(!masterDta.EndOfStream))
				{
					// Get the first number
					ind1 = 0;
					ind2 = line.IndexOf(' ');
					double.TryParse(line.Substring(ind1, ind2), out val1);

					// Get the second number
					ind1 = ind2 + 1;
					double.TryParse(line.Substring(ind1, line.Length - ind1), out val2);

					// Add this entry to the entries list
					entries.Add(new ExperimentalSpectraEntry(val1, val2));

					// Read the next line
					line = masterDta.ReadLine();
				}

				// Finally, create the new ExperimentalSpectra
				expSpec = new ExperimentalSpectra(scanNumber, chargeState,
					precursorMass, precursorChargeState, entries);
			}
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