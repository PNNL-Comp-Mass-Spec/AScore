//Joshua Aldrich

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AScore_DLL.Mod;
using System.Collections;

namespace AScore_DLL.Managers
{
	/// <summary>
	/// An object which holds the b and y ion uncharged masses
	/// </summary>
	public class TheoreticalSpectra:IEnumerable
	{

		private int chargeState;
		private double minRange = 0.0;
		private double maxRange = 2000.0;
		private int count;
		List<double> bIonsMass= new List<double>();
		List<double> yIonsMass = new List<double>();
		string fht;

		public int Count
		{
			get { return count; }
		}

		public TheoreticalSpectra(string fht, ParameterFileManager ascoreParameters, int chargeS, 
			List<Mod.DynamicModification> moveMod, MassType massType)
		{
			chargeState = chargeS;
			this.fht = fht;
			Calculate(chargeState, ascoreParameters, massType);
		}

		

		private void GenerateIonMasses(List<Modification> staticMods,
			List<TerminiModification> terminiMods)
		{
			string peptideNoPmt = fht;


			// Need to iterate through each amino acid in the peptide to
			// generate the ion masses
			for (int i = 0; i < peptideNoPmt.Length; ++i)
			{
				// Current amino acid
				char bAcid = peptideNoPmt[i];
				char yAcid = peptideNoPmt[peptideNoPmt.Length - i - 1];


				// Get the monoisotopic mass for the current amino acid
				
				bIonsMass.Add(AminoAcidMass.GetMassByLetter(bAcid));
				yIonsMass.Add(AminoAcidMass.GetMassByLetter(yAcid));

				foreach (Modification m in staticMods)
				{	
					if(m.Match(bAcid))
					{
						bIonsMass[i] += m.Mass;
					}
					if(m.Match(yAcid))
					{
						yIonsMass[i] += m.Mass;
					}
				}


				// Add the previous mass to the current mass for a rolling total
				if (i > 0)
				{
					bIonsMass[i] += bIonsMass[i - 1];
					yIonsMass[i] += yIonsMass[i - 1];
				}
				// Check for c or n terminus modifications to be
				// applied to the first entry in the mass lists in order to
				// propagate to all the entries
				if (i == 0)
				{

					bIonsMass[i] = EdgeCase(bIonsMass[i], terminiMods, bAcid, true);
					yIonsMass[i] = EdgeCase(yIonsMass[i], terminiMods, yAcid, false);
				}

			}
			bIonsMass.RemoveAt(bIonsMass.Count - 1);
			//yIonsMonoisotopicMass[yIonsMonoisotopicMass.Count - 1] = EdgeCase(
			//    yIonsMonoisotopicMass[yIonsMonoisotopicMass.Count -1], 
			//    terminiMods, fht[0], true);
	//		yIonsMass.RemoveAt(yIonsMass.Count - 1);
		}



		/// <summary>
		/// N and C terminal cases
		/// </summary>
		/// <param name="mass">fragment ion mass</param>
		/// <param name="terminiMods">terminus mod list</param>
		/// <param name="aAcid">current sequence position amino acid letter</param>
		/// <param name="leftSide">true for n-terminus false for c-terminus</param>
		/// <returns>modified mass</returns>
		private static double EdgeCase(double mass, List<TerminiModification> terminiMods, char aAcid, bool leftSide)
		{


			foreach (TerminiModification t in terminiMods)
			{
				if (t.nTerminus && leftSide && t.Match(aAcid))
				{
					mass += t.Mass;
	
				}
				if (t.cTerminus && !leftSide && t.Match(aAcid))
				{
					mass += t.Mass;

				}
			}
			if (leftSide)
			{
				mass += MolecularWeights.Hydrogen;
			}
			else if (!leftSide)
			{
				mass += MolecularWeights.Hydrogen + MolecularWeights.Water;
			}
			return mass;
		}

		/// <summary>
		/// Makes the call to generate the ion masses adjusts for etd fragmentation
		/// </summary>
		/// <param name="charge">charge, do i need this?</param>
		/// <param name="ascoreParameterss">ascore parameters</param>
		private void Calculate(int charge, ParameterFileManager ascoreParameterss, MassType massType)
		{
			chargeState = charge;

			AminoAcidMass.MassType = massType;
			MolecularWeights.MassType = massType;
			foreach (Modification sm in ascoreParameterss.StaticMods)
			{
				sm.ModMassType = massType;
			}
			foreach (TerminiModification tm in ascoreParameterss.TerminiMods)
			{
				tm.ModMassType = massType;
			}

			// First thing to do is generate the base ion masses
			GenerateIonMasses(ascoreParameterss.StaticMods, ascoreParameterss.TerminiMods);
			// If the fragment type is ETD, we need to convert the B and Y ions
			// into C and Z ions
			if (ascoreParameterss.FragmentType == FragmentType.ETD)
			{
				for (int i = 0; i < yIonsMass.Count; ++i)
				{
					if (i < bIonsMass.Count)
					{
						bIonsMass[i] += MolecularWeights.Ammonia;
					}
					yIonsMass[i] -= MolecularWeights.NH2;
				}
			}
		}


		/// <summary>
		/// Generates a theoretical spectra based on a modification list and positions for those modifications
		/// </summary>
		/// <param name="positions">int array has modifications at indice in which they occur in the sequence</param>
		/// <param name="myMods">dynamic modification list</param>
		/// <returns>Dictionary of theoretical ions organized by charge</returns>
		public Dictionary<int,ChargeStateIons> GetTempSpectra(int[] positions, List<DynamicModification> myMods,MassType massType)
		{
			Dictionary<int, ChargeStateIons> tempFragIons = new Dictionary<int, ChargeStateIons>();
			for (int i = 1; i < chargeState; ++i)
			{
				tempFragIons[i] = ChargeStateIons.GenerateFragmentIon(i, massType,
					myMods, positions,
					bIonsMass, yIonsMass, fht.Length);
			}
			return tempFragIons;
		}

		/// <summary>
		/// Provides an enumerable list of ions
		/// </summary>
		/// <returns>enumeration of b and y ion monoisotopic masses</returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			for (int i = 0; i < bIonsMass.Count; i++)
				yield return bIonsMass[i];

			for (int i = 0; i < yIonsMass.Count; i++)
				yield return yIonsMass[i];
		}

		/// <summary>
		/// Sets the spectra range... unused at this point can probably remove
		/// </summary>
		/// <param name="min"></param>
		/// <param name="max"></param>
		public void SetSpectraRange(double min, double max)
		{
			// Set the range variables
			minRange = min;
			maxRange = max;

			// Update the count to reflect the new range
			count = 0;
			foreach (double temp in this)
			{
				++count;
			}
		}
	}
}
