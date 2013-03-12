//Joshua Aldrich

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AScore_DLL.Mod
{
	/// <summary>
	/// Modification object
	/// </summary>
	public class Modification
	{


		public double MassMonoisotopic { get; set; }
		public double MassAverage { get; set; }
		public char ModSymbol { get; set; }
		public List<char> PossibleModSites { get; set; }
		public int UniqueID { get; set; }
		public MassType ModMassType { get; set; }



		public double Mass
		{
			get
			{
				switch (ModMassType)
				{
					case MassType.Average: return MassAverage;
					case MassType.Monoisotopic: return MassMonoisotopic;
					default: return -1.0;
				}
			}
		}

		/// <summary>
		/// Matching method, empty lists always matches
		/// </summary>
		/// <param name="c">amino acid to match</param>
		/// <returns>whether site can be modified by this modification</returns>
		public virtual bool Match(char c)
		{
			if (PossibleModSites.Count == 0)
			{
				return true;
			}
			foreach (char p in PossibleModSites)
			{
				if (p==' ' || c == p)
				{
					return true;
				}
			}
			return false;
		}

		
	}
}
