////////////////////////////////////////////////////////////////////////////////
// © 2010 Pacific Northwest National Laboratories
//
// File: Constants.cs
// Author: Jeremy Rehkop
// Date Created: 2/14/2010
//
// Last Updated: 2/14/2010 - Jeremy Rehkop
// Last Updated: 6/02/2011 - Joshua Aldrich
////////////////////////////////////////////////////////////////////////////////

namespace AScore_DLL
{

	public enum MassType
	{
		/// <summary>
		/// Average ion mass
		/// </summary>
		Average,

		/// <summary>
		/// Monoisotopic ion mass
		/// </summary>
		Monoisotopic
	}

	public enum FragmentType
	{
		/// <summary>
		/// Unspecified fragment type
		/// </summary>
		Unspecified = 0,

		/// <summary>
		/// CID fragment type
		/// </summary>
		CID = 1,

		/// <summary>
		/// ETD fragment type
		/// </summary>
		ETD = 2,

		/// <summary>
		/// HCD fragment type
		/// </summary>
		HCD = 3
	}

	/// <summary>
	/// Contains the average and monoisotopic masses of the amino acids
	/// </summary>
	public static class AminoAcidMass
	{
		#region Class Members

		#region Variables

		// This controls the mass numbers that are returned
		private static MassType massType =
			MassType.Monoisotopic;

		#endregion // Variables

		#region Properties

		/// <summary>
		/// Gets or sets the current mass type
		/// </summary>
		public static MassType MassType
		{
			get => massType;
		    set => massType = value;
		}

		#endregion // Properties

		#endregion // Class Members

		#region Public Methods

		/// <summary>
		/// Gets the mass of an amino acid by its letter representation
		/// </summary>
		/// <param name="aminoAcid">Letter representation of the amino acid
		/// whose mass is to returned.</param>
		/// <returns>The mass of the specified amino acid if it exists, -1 if
		/// it does not.</returns>
		public static double GetMassByLetter(char aminoAcid)
		{
			switch (aminoAcid)
			{
				case 'A': return Alanine;
				case 'R': return Arginine;
				case 'N': return Asparagine;
				case 'D': return AsparticAcid;
				case 'C': return Cysteine;
				case 'E': return GlutamicAcid;
				case 'Q': return Glutamine;
				case 'G': return Glycine;
				case 'H': return Histidine;
				case 'I': return Isoleucine;
				case 'L': return Leucine;
				case 'K': return Lysine;
				case 'M': return Methionine;
				case 'F': return Phenylalanine;
				case 'P': return Proline;
				case 'S': return Serine;
				case 'T': return Threonine;
				case 'W': return Tryptophan;
				case 'Y': return Tyrosine;
				case 'V': return Valine;
				default:
					// Unrecognized character (or a symbol)
					return 0;
			}
		}

		#endregion // Public Methods

		#region Amino Acid Masses

		/// <summary>
		/// (A) Alanine mass
		/// </summary>
		public static double Alanine
		{
			get
			{
				switch (massType)
				{
					case MassType.Average: return 71.0779;
					case MassType.Monoisotopic: return 71.037114;
					default: return 0;
				}
			}
		}

		/// <summary>
		/// (R) Arginine mass
		/// </summary>
		public static double Arginine
		{
			get
			{
				switch (massType)
				{
					case MassType.Average: return 156.1857;
					case MassType.Monoisotopic: return 156.10111;
					default: return 0;
				}
			}
		}

		/// <summary>
		/// (N) Asparagine mass
		/// </summary>
		public static double Asparagine
		{
			get
			{
				switch (massType)
				{
					case MassType.Average: return 114.1026;
					case MassType.Monoisotopic: return 114.04293;
					default: return 0;
				}
			}
		}

		/// <summary>
		/// (D) Aspartic Acid mass
		/// </summary>
		public static double AsparticAcid
		{
			get
			{
				switch (massType)
				{
					case MassType.Average: return 115.0874;
					case MassType.Monoisotopic: return 115.02694;
					default: return 0;
				}
			}
		}

		/// <summary>
		/// (C) Cysteine mass
		/// </summary>
		public static double Cysteine
		{
			get
			{
				switch (massType)
				{
					case MassType.Average: return 103.1429;
					case MassType.Monoisotopic: return 103.00919;
					default: return 0;
				}
			}
		}

		/// <summary>
		/// (E) Glutamic Acid mass
		/// </summary>
		public static double GlutamicAcid
		{
			get
			{
				switch (massType)
				{
					case MassType.Average: return 129.11400;
					case MassType.Monoisotopic: return 129.04259;
					default: return 0;
				}
			}
		}

		/// <summary>
		/// (Q) Glutamine mass
		/// </summary>
		public static double Glutamine
		{
			get
			{
				switch (massType)
				{
					case MassType.Average: return 128.12920;
					case MassType.Monoisotopic: return 128.05858;
					default: return 0;
				}
			}
		}

		/// <summary>
		/// (G) Glycine mass
		/// </summary>
		public static double Glycine
		{
			get
			{
				switch (massType)
				{
					case MassType.Average: return 57.05130;
					case MassType.Monoisotopic: return 57.021464;
					default: return 0;
				}
			}
		}

		/// <summary>
		/// (H) Histidine mass
		/// </summary>
		public static double Histidine
		{
			get
			{
				switch (massType)
				{
					case MassType.Average: return 137.13930;
					case MassType.Monoisotopic: return 137.05891;
					default: return 0;
				}
			}
		}

		/// <summary>
		/// (I) Isoleucine mass
		/// </summary>
		public static double Isoleucine
		{
			get
			{
				switch (massType)
				{
					case MassType.Average: return 113.15760;
					case MassType.Monoisotopic: return 113.08406;
					default: return 0;
				}
			}
		}

		/// <summary>
		/// (L) Leucine mass
		/// </summary>
		public static double Leucine
		{
			get
			{
				switch (massType)
				{
					case MassType.Average: return 113.1576;
					case MassType.Monoisotopic: return 113.08406;
					default: return 0;
				}
			}
		}

		/// <summary>
		/// (K) Lysine mass
		/// </summary>
		public static double Lysine
		{
			get
			{
				switch (massType)
				{
					case MassType.Average: return 128.1723;
					case MassType.Monoisotopic: return 128.09496;
					default: return 0;
				}
			}
		}

		/// <summary>
		/// (M) Methionine mass
		/// </summary>
		public static double Methionine
		{
			get
			{
				switch (massType)
				{
					case MassType.Average: return 131.1961;
					case MassType.Monoisotopic: return 131.04048;
					default: return 0;
				}
			}
		}

		/// <summary>
		/// (F) Phenylalanine mass
		/// </summary>
		public static double Phenylalanine
		{
			get
			{
				switch (massType)
				{
					case MassType.Average: return 147.1739;
					case MassType.Monoisotopic: return 147.06841;
					default: return 0;
				}
			}
		}

		/// <summary>
		/// (P) Proline mass
		/// </summary>
		public static double Proline
		{
			get
			{
				switch (massType)
				{
					case MassType.Average: return 97.1152;
					case MassType.Monoisotopic: return 97.052764;
					default: return 0;
				}
			}
		}

		/// <summary>
		/// (S) Serine mass
		/// </summary>
		public static double Serine
		{
			get
			{
				switch (massType)
				{
					case MassType.Average: return 87.0773;
					case MassType.Monoisotopic: return 87.032029;
					default: return 0;
				}
			}
		}

		/// <summary>
		/// (T) Threonine mass
		/// </summary>
		public static double Threonine
		{
			get
			{
				switch (massType)
				{
					case MassType.Average: return 101.1039;
					case MassType.Monoisotopic: return 101.04768;
					default: return 0;
				}
			}
		}

		/// <summary>
		/// (W) Tryptophan mass
		/// </summary>
		public static double Tryptophan
		{
			get
			{
				switch (massType)
				{
					case MassType.Average: return 186.2099;
					case MassType.Monoisotopic: return 186.07931;
					default: return 0;
				}
			}
		}

		/// <summary>
		/// (Y) Tyrosine mass
		/// </summary>
		public static double Tyrosine
		{
			get
			{
				switch (massType)
				{
					case MassType.Average: return 163.1733;
					case MassType.Monoisotopic: return 163.06333;
					default: return 0;
				}
			}
		}

		/// <summary>
		/// (V) Valine mass
		/// </summary>
		public static double Valine
		{
			get
			{
				switch (massType)
				{
					case MassType.Average: return 99.13110;
					case MassType.Monoisotopic: return 99.068414;
					default: return 0;
				}
			}
		}

		#endregion // Amino Acid Masses


	}

	public static class MolecularWeights
	{
		#region Class Members

		#region Variables

		// This controls the mass numbers that are returned
		private static MassType massType =
			MassType.Monoisotopic;

		#endregion // Variables

		#region Properties

		/// <summary>
		/// Gets or sets the current mass type
		/// </summary>
		public static MassType MassType
		{
			get => massType;
		    set => massType = value;
		}

		#endregion // Properties

		#endregion // Class Members

		#region Molecular Weights

		/// <summary>
		/// (H) Hydrogen mass
		/// </summary>
		public static double Hydrogen
		{
			get
			{
				switch (massType)
				{
					case MassType.Average: return 1.0072765;
					case MassType.Monoisotopic: return 1.0072765;
					default: return 0;
				}
			}
		}

		/// <summary>
		/// (H2O) Water mass
		/// </summary>
		public static double Water
		{
			get
			{
				switch (massType)
				{
					case MassType.Average: return 18.0153214;
					case MassType.Monoisotopic: return 18.0105633;
					default: return 0;
				}
			}
		}


		/// <summary>
		/// (NH3) Ammonia mass
		/// </summary>
		public static double Ammonia
		{
			get
			{
				switch (massType)
				{
					case MassType.Average: return 17.03065;
					case MassType.Monoisotopic: return 17.026547;
					default: return 0;
				}
			}
		}

		/// <summary>
		/// (NH2)
		/// </summary>
		public static double NH2
		{
			get
			{
				switch (massType)
				{
					case MassType.Average: return 16.02267;
					case MassType.Monoisotopic: return 16.0187224;
					default: return 0;
				}
			}
		}

		#endregion // Molecular Weights
	}

}