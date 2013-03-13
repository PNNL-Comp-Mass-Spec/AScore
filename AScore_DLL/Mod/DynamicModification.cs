//Joshua Aldrich


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AScore_DLL;

namespace AScore_DLL.Mod
{
		
	/// <summary>
	/// Dynamic modification object
	/// Extends the Modification class by adding several fields specific to Dynamic mods
	/// </summary>
	public class DynamicModification : Modification
	{

		public int MaxPerSite { get; set; }
		public int position { get; set; }
		public int Count { get; set; }
		
		/// <summary>
		/// Default constructor
		/// </summary>
		public DynamicModification()
		{
		}

		/// <summary>
		/// Constructor whose source is a dynamic mod entry
		/// </summary>
		public DynamicModification(DynamicModification itemToCopy)
		{		
			this.CopyFrom(itemToCopy);
		}

		protected void CopyFrom(DynamicModification itemToCopy)
		{
			base.CopyFrom(itemToCopy);

			this.MaxPerSite = itemToCopy.MaxPerSite;
			this.position = itemToCopy.position;
			this.Count = itemToCopy.Count;			
		}
	}
}
