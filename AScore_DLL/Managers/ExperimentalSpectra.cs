using System;
using System.Collections.Generic;
using System.Linq;

namespace AScore_DLL.Managers
{
    /// <summary>
    /// Represents the top ten spectra for each section in the DTA file from the Master DTA.
    /// </summary>
    public class ExperimentalSpectra
    {
        #region Variables

        public double m_maxMZ = -1;
        public double m_minMZ = -1;
        private readonly List<List<ExperimentalSpectraEntry>> TopTenSpectra = new();

        private readonly PHRPReader.PeptideMassCalculator mPeptideMassCalculator;

        #endregion // Variables

        #region Properties

        /// <summary>
        /// Gets the scan number of this ExperimentalSpectra
        /// </summary>
        public int ScanNumber { get; }

        /// <summary>
        /// Gets the charge state of this ExperimentalSpectra
        /// </summary>
        public int ChargeState { get; }

        /// <summary>
        /// Gets the precursor mass (M+H value)
        /// </summary>
        public double PrecursorMass => PrecursorMass1;

        public double PrecursorNeutralMass => mPeptideMassCalculator.ConvoluteMass(PrecursorMass1, 1, 0);

        /// <summary>
        /// Gets the precursor charge state
        /// </summary>
        public int PrecursorChargeState { get; }

        public double PrecursorMass1 { get; set; }

        #endregion // Properties

        #region Constructor

        /// <summary>
        /// Initializes a new instance of ExperimentalSpectra.
        /// </summary>
        /// <param name="scanNum">Scan number of this experimental spectra.</param>
        /// <param name="chargeState">Charge state of the peptide in this experimental spectrum</param>
        /// <param name="precursorMass">Precursor mass (first number in DTA file); this is an M+H value</param>
        /// <param name="precursorChargeState">Precursor charge state (second number in DTA file).</param>
        /// <param name="spectra">List of the experimental spectra from the Master DTA file.</param>
        /// <param name="peptideMassCalculator">Mass calculator class</param>
        public ExperimentalSpectra(int scanNum, int chargeState, double precursorMass,
            int precursorChargeState, List<ExperimentalSpectraEntry> spectra,
            PHRPReader.PeptideMassCalculator peptideMassCalculator)
        {
            ScanNumber = scanNum;
            ChargeState = chargeState;
            PrecursorMass1 = precursorMass;
            PrecursorChargeState = precursorChargeState;
            GenerateSpectraForPeptideScore(spectra);

            mPeptideMassCalculator = peptideMassCalculator;
        }

        #endregion // Constructor

        #region Destructor

        /// <summary>
        /// Clears out the experimental spectra lists
        /// </summary>
        ~ExperimentalSpectra()
        {
            foreach (var spectrum in TopTenSpectra)
            {
                spectrum.Clear();
            }
            TopTenSpectra.Clear();
        }

        #endregion // Destructor

        #region Public Methods

        /// <summary>
        /// Gets the peak depth spectra from the top tens list.
        /// </summary>
        /// <param name="peakDepth">Peak depth to use to generate the list.</param>
        public List<ExperimentalSpectraEntry> GetPeakDepthSpectra(int peakDepth)
        {
            var expSpecPeakDepth = new List<ExperimentalSpectraEntry>();
            foreach (var list in TopTenSpectra)
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
            const double tol = 0.6;
            var index = 0;

            var minMZ = spectra[0].Mz;
            var maxMZ = spectra[spectra.Count - 1].Mz;
            m_minMZ = minMZ;
            m_maxMZ = maxMZ;
            var numSections = Convert.ToInt32(Math.Ceiling((maxMZ - minMZ) / 100.0));
            var descendSort = new ExperimentalSpectraEntry.SortIntensityDescend();

            // Start getting the top ten hits from each section
            for (var i = 0; i < numSections; ++i)
            {
                // Set the lower and upper bounds for this section
                var lowerBound = minMZ + (i * 100.0);
                var upperBound = lowerBound + 100.0;
                var currentSection = new List<ExperimentalSpectraEntry>();
                // Now iterate through each mz value in this section looking for
                // entries that are within 1.0 of the current mz value and picking
                // the one with the highest Value2 property
                for (var mz = lowerBound; mz < upperBound; ++mz)
                {
                    // Get all of the entries for this mz value
                    var count = 0;
                    while ((index < spectra.Count) && (spectra[index].Mz >= mz) &&
                        (spectra[index].Mz < (mz + 1.0)))
                    {
                        ++index;
                        ++count;
                    }

                    // If there's only one, just add it
                    if (count == 1)
                    {
                        currentSection.Add(spectra[index - 1]);
                    }
                    // If there were more then one, sort them in descending
                    // order and pick the biggest one
                    else if (count > 1)
                    {
                        spectra.Sort(index - count, count, descendSort);
                        currentSection.Add(spectra[index - count]);
                    }
                }

                // Sort the data by descending intensity
                currentSection.Sort(descendSort);

                if (currentSection.Count >= 10)
                {
                    // If there are more than 10 hits for this section, then
                    // only keep the top 10 most abundant ions

                    var currentSectionFiltered = new List<ExperimentalSpectraEntry>();

                    foreach (var s in currentSection)
                    {
                        // Make sure the current data point is not too close in mass to the filtered data points
                        var closeToExistingPoint = currentSectionFiltered.Any(p => Math.Abs(s.Mz - p.Mz) < tol);
                        if (!closeToExistingPoint)
                        {
                            currentSectionFiltered.Add(s);

                            if (currentSectionFiltered.Count >= 10)
                                break;
                        }
                    }
                    currentSection = currentSectionFiltered;
                }

                // Add the current section to the top ten list
                TopTenSpectra.Add(currentSection);
            }
        }

        #endregion // Private Method
    }
}
