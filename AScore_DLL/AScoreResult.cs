namespace AScore_DLL
{
    public class AScoreResult
    {
        // Ignore Spelling: phosphosite

        /// <summary>
        ///  AScore value (larger is better)
        /// </summary>
        /// <remarks>
        /// 0 means unable to localize (too ambiguous due to too many S, T, Y residues)
        /// 19 or higher indicates 99% certainty of the phosphosite localization
        /// 1000 means the peptide only has one phosphosite
        /// -1 means no modified residues
        /// </remarks>
        public double AScore { get; set; }

        /// <summary>
        /// Number of b/y ions that could be matched
        /// </summary>
        public int NumSiteIons { get; set; }

        /// <summary>
        /// Number of b/y ions that were matched
        /// </summary>
        public int SiteDetermineMatched { get; set; }

        /// <summary>
        /// Mod symbol was permuted for this result
        /// </summary>
        public string ModInfo { get; set; }

        /// <summary>
        /// Mod types and locations
        /// </summary>
        public int[] PeptideMods;

        public string SecondSequence { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        public AScoreResult()
        {
            // Empty mod array
            PeptideMods = new int[] { };

            ModInfo = string.Empty;
            SecondSequence = string.Empty;
        }
    }
}
