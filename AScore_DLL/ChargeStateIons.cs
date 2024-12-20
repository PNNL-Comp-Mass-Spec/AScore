﻿////////////////////////////////////////////////////////////////////////////////
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
using System.Collections.ObjectModel;

namespace AScore_DLL
{
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
		private List<double> bIonsOut = new List<double>();
		private List<double> yIonsOut = new List<double>();



		#endregion // Variables

		#region Properties

		/// <summary>
		/// Gets the charge state this fragment ion represents
		/// </summary>
		public int ChargeState
		{
			get { return chargeState; }
		}

		/// <summary>
		/// Gets the bIons with no phosphorylation
		/// </summary>
		public List<double> BIons
		{
			get { return bIonsOut; }
		}

		/// <summary>
		/// Gets the yIons with no phosphorylation
		/// </summary>
		public List<double> YIons
		{
			get { return yIonsOut; }
		}

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
		/// <param name="minPhosphoSite">The smallest from the list of available
		/// phophoSites for this peptide.</param>
		/// <param name="maxPhosphoSite">The biggest from the list of available
		/// phophoSites for this peptide.</param>
		/// <param name="bIons">The bIons to use corresponding to the mass type.</param>
		/// <param name="yIons">The yIons to use corresponding to the mass type.</param>
		/// <param name="peptideLength">The length of the trimmed peptide without
		/// phosphorylation sites.</param>
		/// <returns>A newly constructed FragmentIon upon success, null if any
		/// errors occured during construction.</returns>
		public static ChargeStateIons GenerateFragmentIon(int chargeState,
			MassType massType, List<Mod.DynamicModification> dynamMods, int[] positions,
			List<double> bIons, List<double> yIons, int peptideLength)
		{
			ChargeStateIons fragIon = null;
			MolecularWeights.MassType = massType;
			double sumofModsB = 0.0;
			double sumofModsY = 0.0;


			// If the charge state is one, create the 
			if (chargeState == 1)
			{
				fragIon = new ChargeStateIons();

				// Set the charge state of the frag ion
				fragIon.chargeState = chargeState;

				// There is a phosphorylation site
				if (dynamMods.Count > 0)
				{
					// bIons
					for (int i = 0; i < bIons.Count; ++i)
					{
						foreach (Mod.DynamicModification mod in dynamMods)
						{
							if (mod.UniqueID == positions[i])
							{
								sumofModsB += mod.MassMonoisotopic;
							}
							if (mod.UniqueID == positions[peptideLength - 1 - i])
							{
								sumofModsY += mod.MassMonoisotopic;
							}
						}
							fragIon.bIonsOut.Add(bIons[i] + sumofModsB);
							fragIon.yIonsOut.Add(yIons[i] + sumofModsY);
					}
				}
			}
			else if (chargeState > 1)
			{
				sumofModsB = (chargeState - 1) * MolecularWeights.Hydrogen; ;
				sumofModsY = sumofModsB;
				fragIon = new ChargeStateIons();

				// Set the charge state of the frag ion
				fragIon.chargeState = chargeState;

				// The value to add to each ion
				double temp = (chargeState - 1) * MolecularWeights.Hydrogen;

				// There is a phosphorylation site
				if(dynamMods.Count > 0)
				{
					for (int i = 0; i < bIons.Count; ++i)
					{
						foreach (Mod.DynamicModification mod in dynamMods)
						{
							if (mod.UniqueID == positions[i])
							{
								sumofModsB += mod.MassMonoisotopic;
							}
							if (mod.UniqueID == positions[peptideLength - 1 - i])
							{
								sumofModsY += mod.MassMonoisotopic;
							}
						}
						fragIon.bIonsOut.Add((bIons[i] + sumofModsB)/chargeState);
						fragIon.yIonsOut.Add((yIons[i] + sumofModsY) / chargeState);
					}

				}
			
			}
			return fragIon;
		}

		#endregion // Public Methods
	}
}