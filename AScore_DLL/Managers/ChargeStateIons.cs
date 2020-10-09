////////////////////////////////////////////////////////////////////////////////
// © 2010 Pacific Northwest National Laboratories
//
// File: FragmentIon.cs
// Author: Jeremy Rehkop
// Date Created: 2/15/2010
//
// Last Updated: 2/15/2010 - Jeremy Rehkop
// Last Updated: 6/10/2011 - Joshua Aldrich
////////////////////////////////////////////////////////////////////////////////

using System.Collections.Generic;

namespace AScore_DLL.Managers
{
    // Ignore Spelling: Rehkop

    /// <summary>
    /// Represents the bIons and yIons of a specific charge state of a
    /// TheoreticalSpectra.
    /// </summary>
    public class ChargeStateIons
    {
        #region Class Members

        #region Variables

        private int chargeState = -1;

        // No phosphorylation ions
        private readonly List<double> bIonsOut = new List<double>();
        private readonly List<double> yIonsOut = new List<double>();

        #endregion // Variables

        #region Properties

        /// <summary>
        /// Gets the charge state this fragment ion represents
        /// </summary>
        public int ChargeState => chargeState;

        /// <summary>
        /// Gets the bIons with no phosphorylation
        /// </summary>
        public List<double> BIons => bIonsOut;

        /// <summary>
        /// Gets the yIons with no phosphorylation
        /// </summary>
        public List<double> YIons => yIonsOut;

        #endregion // Properties

        #endregion // Class Members

        #region Constructor

        /// <summary>
        /// Initializes a new instance of FragmentIon
        /// </summary>
        private ChargeStateIons()
        {
            // The constructor is private so that users are required to get
            // fragment ions through the static GenerateFragmentIon method
        }

        #endregion // Constructor

        #region Public Methods

        /// <summary>
        /// Creates a new fragment ion based on the given parameters.
        /// </summary>
        /// <param name="chargeState">Desired charge state for the fragment ion.
        /// Must be greater than 0.</param>
        /// <param name="massType">Mass type.</param>
        /// <param name="dynamicMods">Dynamic mods</param>
        /// <param name="positions">Mod positions</param>
        /// <param name="bIons">The bIons to use corresponding to the mass type.</param>
        /// <param name="yIons">The yIons to use corresponding to the mass type.</param>
        /// <param name="peptideLength">The length of the trimmed peptide without
        /// phosphorylation sites.</param>
        /// <returns>A newly constructed FragmentIon upon success, null if any
        /// errors occurred during construction.</returns>
        public static ChargeStateIons GenerateFragmentIon(int chargeState,
            MassType massType, List<Mod.DynamicModification> dynamicMods, int[] positions,
            List<double> bIons, List<double> yIons, int peptideLength)
        {
            ChargeStateIons fragIon = null;
            MolecularWeights.MassType = massType;
            var sumOfModsB = 0.0;
            var sumOfModsY = 0.0;

            // If the charge state is one, create the
            if (chargeState == 1)
            {
                fragIon = new ChargeStateIons
                {
                    chargeState = chargeState
                };

                // Set the charge state of the fragment ion

                // There is a phosphorylation site
                if (dynamicMods.Count > 0)
                {
                    // bIons
                    for (var i = 0; i < bIons.Count; ++i)
                    {
                        foreach (var mod in dynamicMods)
                        {
                            if (mod.UniqueID == positions[i])
                            {
                                sumOfModsB += mod.MassMonoisotopic;
                            }
                            if (mod.UniqueID == positions[peptideLength - 1 - i])
                            {
                                sumOfModsY += mod.MassMonoisotopic;
                            }
                        }
                        fragIon.bIonsOut.Add(bIons[i] + sumOfModsB);
                        fragIon.yIonsOut.Add(yIons[i] + sumOfModsY);
                    }
                }
            }
            else if (chargeState > 1)
            {
                sumOfModsB = (chargeState - 1) * MolecularWeights.Hydrogen;
                sumOfModsY = sumOfModsB;
                fragIon = new ChargeStateIons
                {
                    chargeState = chargeState
                };

                // Set the charge state of the fragment ion

                // The value to add to each ion
                //  double temp = (chargeState - 1) * MolecularWeights.Hydrogen;

                // There is a phosphorylation site
                if (dynamicMods.Count > 0)
                {
                    for (var i = 0; i < bIons.Count; ++i)
                    {
                        foreach (var mod in dynamicMods)
                        {
                            if (mod.UniqueID == positions[i])
                            {
                                sumOfModsB += mod.MassMonoisotopic;
                            }
                            if (mod.UniqueID == positions[peptideLength - 1 - i])
                            {
                                sumOfModsY += mod.MassMonoisotopic;
                            }
                        }
                        fragIon.bIonsOut.Add((bIons[i] + sumOfModsB /*+temp*/) / chargeState);
                        fragIon.yIonsOut.Add((yIons[i] + sumOfModsY /*+temp*/) / chargeState);
                    }
                }
            }
            return fragIon;
        }

        #endregion // Public Methods
    }
}
