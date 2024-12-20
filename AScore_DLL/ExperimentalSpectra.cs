﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace AScore_DLL
{
	/// <summary>
	/// Represents the top ten spectra for each section in the dta file from
	/// the Master DTA.
	/// </summary>
	public class ExperimentalSpectra
	{
		#region Class Members

		#region Variables

		public double m_maxMZ = -1;
		public double m_minMZ = -1;
		private int scanNumber;
		private int chargeState;
		private double precursorMass;
		private int precursorChargeState;
		private List<List<ExperimentalSpectraEntry>> topTenSpectra =
			new List<List<ExperimentalSpectraEntry>>();

		#endregion // Variables

		#region Properties

		/// <summary>
		/// Gets the scan number of this ExperimentalSpectra
		/// </summary>
		public int ScanNumber
		{
			get { return scanNumber; }
		}

		/// <summary>
		/// Gets the charge state of this ExperimentalSpectra
		/// </summary>
		public int ChargeState
		{
			get { return chargeState; }
		}

		/// <summary>
		/// Gets the precursor mass
		/// </summary>
		public double PrecursorMass
		{
			get { return precursorMass; }
		}

		/// <summary>
		/// Gets the precursor charge state
		/// </summary>
		public int PrecursorChargeState
		{
			get { return precursorChargeState; }
		}

		#endregion // Properties

		#endregion // Class Members

		#region Constructor

		/// <summary>
		/// Initializes a new instance of ExperimentalSpectra.
		/// </summary>
		/// <param name="scanNum">Scan number of this experimental spectra.</param>
		/// <param name="chargeState">Charge state of the peptide in this
		/// experimental spectra.</param>
		/// <param name="name">Name of the dta file this spectra was extracted from.</param>
		/// <param name="precursorMass">Precursor mass (first number in dta file).</param>
		/// <param name="precursorChargeState">Precursor charge state (second 
		/// number in dta file).</param>
		/// <param name="spectra">List of the experimental spectra from the
		/// Master DTA file.</param>
		public ExperimentalSpectra(int scanNum, int chargeState, double precursorMass,
			int precursorChargeState, List<ExperimentalSpectraEntry> spectra)
		{
			this.scanNumber = scanNum;
			this.chargeState = chargeState;
			this.precursorMass = precursorMass;
			this.precursorChargeState = precursorChargeState;
			GenerateSpectraForPeptideScore(spectra);
		}

		#endregion // Constructor

		#region Destructor

		/// <summary>
		/// Clears out the experimental spectra lists
		/// </summary>
		~ExperimentalSpectra()
		{
			for (int i = 0; i < topTenSpectra.Count; ++i)
			{
				topTenSpectra[i].Clear();
			}
			topTenSpectra.Clear();
		}

		#endregion // Destructor

		#region Public Methods

		/// <summary>
		/// Gets the peak depth spectra from the top tens list.
		/// </summary>
		/// <param name="peakDepth">Peak depth to use to generate the list.</param>
		public List<ExperimentalSpectraEntry> GetPeakDepthSpectra(int peakDepth)
		{
			List<ExperimentalSpectraEntry> expSpecPeakDepth =
				new List<ExperimentalSpectraEntry>();
			foreach (List<ExperimentalSpectraEntry> list in topTenSpectra)
			{
				if (list.Count >= peakDepth)
				{
					expSpecPeakDepth.AddRange(list.GetRange(0, peakDepth));
				}
				else
				{
					expSpecPeakDepth.AddRange(list.GetRange(0, list.Count));
				}
			}
			return expSpecPeakDepth;
		}

		#endregion // Public Methods

		#region Private Methods

		/// <summary>
		/// Splits the experimental spectra from the Master DTA into sections
		/// of range 100.0 starting from the smallest value in the spectra.
		/// </summary>
		/// <param name="spectra"></param>
		private void GenerateSpectraForPeptideScore(
			List<ExperimentalSpectraEntry> spectra)
		{
			double tol = 0.6;
			int index = 0;

			double minMZ = spectra[0].Value1;
			double maxMZ = spectra[spectra.Count - 1].Value1;
			m_minMZ = minMZ;
			m_maxMZ = maxMZ;
			int numSections = Convert.ToInt32(Math.Ceiling((maxMZ - minMZ) / 100.0));
			ExperimentalSpectraEntry.SortValue2Descend descendSort =
				new ExperimentalSpectraEntry.SortValue2Descend();

			// Start getting the top ten hits from each section
			for (int i = 0; i < numSections; ++i)
			{
				// Set the lower and upper bounds for this section
				double lowerBound = minMZ + (i * 100.0);
				double upperBound = lowerBound + 100.0;
				List<ExperimentalSpectraEntry> currentSection =
					new List<ExperimentalSpectraEntry>();
				// Now iterate through each mz value in this section looking for
				// entries that are within 1.0 of the current mz value and picking
				// the one with the highest Value2 property
				for (double mz = lowerBound; mz < upperBound; mz += 1.0)
				{
					// Get all of the entries for this mz value
					int count = 0;
					while ((index < spectra.Count) && (spectra[index].Value1 >= mz) &&
						(spectra[index].Value1 < (mz + 1.0)))
					{
						++index;
						++count;
					}

					// If theres only one, just add it
					if (count == 1)
					{
						currentSection.Add(
							new ExperimentalSpectraEntry(spectra[index - 1]));
					}
					// If there were more then one, sort them in descending
					// order and pick the biggest one
					else if (count > 1)
					{
						spectra.Sort(index - count, count, descendSort);
						currentSection.Add(
							new ExperimentalSpectraEntry(spectra[index - count]));
					}
				}

				// If there are more than 10 hits for this section, sort them
				// and chop off everything past 10.

				//Adding a method here to prevent peaks from landing right next to each other
				if (currentSection.Count >= 10)
				{
					List<ExperimentalSpectraEntry> notCurrentSection =
						new List<ExperimentalSpectraEntry>();
					currentSection.Sort(descendSort);
					foreach (ExperimentalSpectraEntry s in currentSection)
					{
						bool alreadyThere = false;
						foreach (ExperimentalSpectraEntry p in notCurrentSection)
						{
							if (Math.Abs(s.Value1 - p.Value1) < tol)
							{
								alreadyThere = true;
							}
						}
						if (!alreadyThere)
						{
							notCurrentSection.Add(s);
						}
						if (notCurrentSection.Count >= 10)
						{
							break;
						}
					}
					currentSection = notCurrentSection;
				}

				// Add the current section to the top ten list
				topTenSpectra.Add(currentSection);
			}
		}

		#endregion // Private Method
	}
}