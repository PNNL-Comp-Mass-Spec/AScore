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

		// nTerminus and cTerminus are used by both static and dynamic mods to indicate whether the mod affects the N-terminus or the C-terminus
		public bool nTerminus { get; set; }
		public bool cTerminus { get; set; }

		/// <summary>
		/// Default constructor
		/// </summary>
		public Modification()
		{
		}

		/// <summary>
		/// Constructor whose source is a dynamic mod entry
		/// </summary>
		public Modification(Modification itemToCopy)
		{
			this.CopyFrom(itemToCopy);
		}

		protected void CopyFrom(Modification itemToCopy)
		{
			this.MassMonoisotopic = itemToCopy.MassMonoisotopic;
			this.MassAverage = itemToCopy.MassAverage;
			this.ModSymbol = itemToCopy.ModSymbol;
			this.PossibleModSites = itemToCopy.PossibleModSites;
			this.UniqueID = itemToCopy.UniqueID;
			this.ModMassType = itemToCopy.ModMassType;
			this.nTerminus = itemToCopy.nTerminus;
			this.cTerminus = itemToCopy.cTerminus;
		}

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
				if ( p == ' ' || c == p)
				{
					return true;
				}
			}
			return false;
		}

		
	}
}
