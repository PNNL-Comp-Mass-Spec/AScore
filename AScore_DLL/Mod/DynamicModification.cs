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
	/// </summary>
	public class DynamicModification : Modification
	{


		public int MaxPerSite { get; set; }
		public int position { get; set; }
		public int Count { get; set; }
		public bool OnN { get; set; }
		public bool OnC { get; set; }


	}
}
