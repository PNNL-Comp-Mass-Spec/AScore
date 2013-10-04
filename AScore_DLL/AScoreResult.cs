namespace AScore_DLL
{
	public class AScoreResult
	{
		// AScore value (closer to 0 is better; 1000 means horrible, -1 means no modified residues)
		public double AScore { get; set; }

		// Number of b/y ions that could be matched
		public int NumSiteIons { get; set; }

		// Number of b/y ions that were matched
		public int SiteDetermineMatched { get; set; }
		
		// Mod symbol was permuted for this result
		public string ModInfo { get; set; }

		// Mod types and locations
		public int[] PeptideMods;

		public string SecondSequence { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public AScoreResult()
		{
			// Empty mod array
			this.PeptideMods = new int[] { };

			ModInfo = string.Empty;
			SecondSequence = string.Empty;
		}
	}
}
