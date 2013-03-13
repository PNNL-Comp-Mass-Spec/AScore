//Joshua Aldrich

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AScore_DLL.Mod
{
	/// <summary>
	/// Termini Modifications
	/// </summary>
	public class TerminiModification : Modification
	{
		public bool nTerminus { get; set; }
		public bool cTerminus { get; set; }

			
		/// <summary>
		/// Default constructor
		/// </summary>
		public TerminiModification()
		{			
		}
		
		/// <summary>
		/// Constructor whose source is a dynamic mod entry
		/// </summary>
		public TerminiModification(TerminiModification itemToCopy)
		{		
			this.CopyFrom(itemToCopy);
		}

		/// <summary>
		/// Constructor whose source is a terminal mod entry
		/// </summary>	
		protected void CopyFrom(TerminiModification itemToCopy)
		{
			base.CopyFrom(itemToCopy);

			this.nTerminus = itemToCopy.nTerminus;
			this.cTerminus = itemToCopy.cTerminus;
			
		}
	}
}
