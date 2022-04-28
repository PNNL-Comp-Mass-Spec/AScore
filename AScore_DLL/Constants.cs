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
    // Ignore Spelling: Rehkop, Aspartic, Glutamic

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
        #region Variables

        // This controls the mass numbers that are returned
        #endregion // Variables

        #region Properties

        /// <summary>
        /// Gets or sets the current mass type
        /// </summary>
        public static MassType MassType { get; set; } = MassType.Monoisotopic;

        #endregion // Properties

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
            return aminoAcid switch
            {
                'A' => Alanine,
                'R' => Arginine,
                'N' => Asparagine,
                'D' => AsparticAcid,
                'C' => Cysteine,
                'E' => GlutamicAcid,
                'Q' => Glutamine,
                'G' => Glycine,
                'H' => Histidine,
                'I' => Isoleucine,
                'L' => Leucine,
                'K' => Lysine,
                'M' => Methionine,
                'F' => Phenylalanine,
                'P' => Proline,
                'S' => Serine,
                'T' => Threonine,
                'W' => Tryptophan,
                'Y' => Tyrosine,
                'V' => Valine,
                _ => 0      // Unrecognized character (or a symbol)
            };
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
                return MassType switch
                {
                    MassType.Average => 71.0779,
                    MassType.Monoisotopic => 71.037114,
                    _ => 0
                };
            }
        }

        /// <summary>
        /// (R) Arginine mass
        /// </summary>
        public static double Arginine
        {
            get
            {
                return MassType switch
                {
                    MassType.Average => 156.1857,
                    MassType.Monoisotopic => 156.10111,
                    _ => 0
                };
            }
        }

        /// <summary>
        /// (N) Asparagine mass
        /// </summary>
        public static double Asparagine
        {
            get
            {
                return MassType switch
                {
                    MassType.Average => 114.1026,
                    MassType.Monoisotopic => 114.04293,
                    _ => 0
                };
            }
        }

        /// <summary>
        /// (D) Aspartic Acid mass
        /// </summary>
        public static double AsparticAcid
        {
            get
            {
                return MassType switch
                {
                    MassType.Average => 115.0874,
                    MassType.Monoisotopic => 115.02694,
                    _ => 0
                };
            }
        }

        /// <summary>
        /// (C) Cysteine mass
        /// </summary>
        public static double Cysteine
        {
            get
            {
                return MassType switch
                {
                    MassType.Average => 103.1429,
                    MassType.Monoisotopic => 103.00919,
                    _ => 0
                };
            }
        }

        /// <summary>
        /// (E) Glutamic Acid mass
        /// </summary>
        public static double GlutamicAcid
        {
            get
            {
                return MassType switch
                {
                    MassType.Average => 129.11400,
                    MassType.Monoisotopic => 129.04259,
                    _ => 0
                };
            }
        }

        /// <summary>
        /// (Q) Glutamine mass
        /// </summary>
        public static double Glutamine
        {
            get
            {
                return MassType switch
                {
                    MassType.Average => 128.12920,
                    MassType.Monoisotopic => 128.05858,
                    _ => 0
                };
            }
        }

        /// <summary>
        /// (G) Glycine mass
        /// </summary>
        public static double Glycine
        {
            get
            {
                return MassType switch
                {
                    MassType.Average => 57.05130,
                    MassType.Monoisotopic => 57.021464,
                    _ => 0
                };
            }
        }

        /// <summary>
        /// (H) Histidine mass
        /// </summary>
        public static double Histidine
        {
            get
            {
                return MassType switch
                {
                    MassType.Average => 137.13930,
                    MassType.Monoisotopic => 137.05891,
                    _ => 0
                };
            }
        }

        /// <summary>
        /// (I) Isoleucine mass
        /// </summary>
        public static double Isoleucine
        {
            get
            {
                return MassType switch
                {
                    MassType.Average => 113.15760,
                    MassType.Monoisotopic => 113.08406,
                    _ => 0
                };
            }
        }

        /// <summary>
        /// (L) Leucine mass
        /// </summary>
        public static double Leucine
        {
            get
            {
                return MassType switch
                {
                    MassType.Average => 113.1576,
                    MassType.Monoisotopic => 113.08406,
                    _ => 0
                };
            }
        }

        /// <summary>
        /// (K) Lysine mass
        /// </summary>
        public static double Lysine
        {
            get
            {
                return MassType switch
                {
                    MassType.Average => 128.1723,
                    MassType.Monoisotopic => 128.09496,
                    _ => 0
                };
            }
        }

        /// <summary>
        /// (M) Methionine mass
        /// </summary>
        public static double Methionine
        {
            get
            {
                return MassType switch
                {
                    MassType.Average => 131.1961,
                    MassType.Monoisotopic => 131.04048,
                    _ => 0
                };
            }
        }

        /// <summary>
        /// (F) Phenylalanine mass
        /// </summary>
        public static double Phenylalanine
        {
            get
            {
                return MassType switch
                {
                    MassType.Average => 147.1739,
                    MassType.Monoisotopic => 147.06841,
                    _ => 0
                };
            }
        }

        /// <summary>
        /// (P) Proline mass
        /// </summary>
        public static double Proline
        {
            get
            {
                return MassType switch
                {
                    MassType.Average => 97.1152,
                    MassType.Monoisotopic => 97.052764,
                    _ => 0
                };
            }
        }

        /// <summary>
        /// (S) Serine mass
        /// </summary>
        public static double Serine
        {
            get
            {
                return MassType switch
                {
                    MassType.Average => 87.0773,
                    MassType.Monoisotopic => 87.032029,
                    _ => 0
                };
            }
        }

        /// <summary>
        /// (T) Threonine mass
        /// </summary>
        public static double Threonine
        {
            get
            {
                return MassType switch
                {
                    MassType.Average => 101.1039,
                    MassType.Monoisotopic => 101.04768,
                    _ => 0
                };
            }
        }

        /// <summary>
        /// (W) Tryptophan mass
        /// </summary>
        public static double Tryptophan
        {
            get
            {
                return MassType switch
                {
                    MassType.Average => 186.2099,
                    MassType.Monoisotopic => 186.07931,
                    _ => 0
                };
            }
        }

        /// <summary>
        /// (Y) Tyrosine mass
        /// </summary>
        public static double Tyrosine
        {
            get
            {
                return MassType switch
                {
                    MassType.Average => 163.1733,
                    MassType.Monoisotopic => 163.06333,
                    _ => 0
                };
            }
        }

        /// <summary>
        /// (V) Valine mass
        /// </summary>
        public static double Valine
        {
            get
            {
                return MassType switch
                {
                    MassType.Average => 99.13110,
                    MassType.Monoisotopic => 99.068414,
                    _ => 0
                };
            }
        }

        #endregion // Amino Acid Masses
    }

    public static class MolecularWeights
    {
        #region Variables

        // This controls the mass numbers that are returned
        #endregion // Variables

        #region Properties

        /// <summary>
        /// Gets or sets the current mass type
        /// </summary>
        public static MassType MassType { get; set; } = MassType.Monoisotopic;

        #endregion // Properties

        #region Molecular Weights

        /// <summary>
        /// (H) Hydrogen mass
        /// </summary>
        public static double Hydrogen
        {
            get
            {
                return MassType switch
                {
                    MassType.Average => 1.0072765,
                    MassType.Monoisotopic => 1.0072765,
                    _ => 0
                };
            }
        }

        /// <summary>
        /// (H2O) Water mass
        /// </summary>
        public static double Water
        {
            get
            {
                return MassType switch
                {
                    MassType.Average => 18.0153214,
                    MassType.Monoisotopic => 18.0105633,
                    _ => 0
                };
            }
        }

        /// <summary>
        /// (NH3) Ammonia mass
        /// </summary>
        public static double Ammonia
        {
            get
            {
                return MassType switch
                {
                    MassType.Average => 17.03065,
                    MassType.Monoisotopic => 17.026547,
                    _ => 0
                };
            }
        }

        /// <summary>
        /// (NH2)
        /// </summary>
        public static double NH2
        {
            get
            {
                return MassType switch
                {
                    MassType.Average => 16.02267,
                    MassType.Monoisotopic => 16.0187224,
                    _ => 0
                };
            }
        }

        #endregion // Molecular Weights
    }
}
