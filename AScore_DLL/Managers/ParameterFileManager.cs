//Joshua Aldrich

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using AScore_DLL.Mod;
using System.Text.RegularExpressions;

namespace AScore_DLL.Managers
{
	/// <summary>
	/// A class for managing xml input to ascore parameters
	/// </summary>
	public class ParameterFileManager
	{

		#region Member Variables
		private List<Mod.Modification> staticMods;
		private List<Mod.TerminiModification> terminiMods;
		private List<Mod.DynamicModification> dynamMods;
		private FragmentType fragmentType;
		private double fragmentMassTolerance;
        private double msgfPreFilter;
		#endregion



		#region Public Properties
		public List<Mod.Modification> StaticMods { get {return staticMods;} }
		public List<Mod.TerminiModification> TerminiMods { get{return terminiMods;}}
		public List<Mod.DynamicModification> DynamicMods { get { return dynamMods; }  }
		public FragmentType FragmentType { get { return fragmentType; } 
			set 
			{
				fragmentType = value;
				if (fragmentType == FragmentType.HCD)
				{
					fragmentMassTolerance = 0.05;
				}
				else
				{
					fragmentMassTolerance = 0.5;
				}
			} 
		}
		public double FragmentMassTolerance { get { return fragmentMassTolerance; } }
        public double MSGFPreFilter { get { return msgfPreFilter; } }

		#endregion



		#region ParameterFileManager Constructors

		public ParameterFileManager(string inputFile)
		{
			ParseXml(inputFile);
		}


		public ParameterFileManager(List<Mod.Modification> stat, List<Mod.TerminiModification> term, 
			List<Mod.DynamicModification> dynam, FragmentType f, double tol, double msgfnum)
		{
			
			staticMods = stat;
			terminiMods = term;
			dynamMods = dynam;
			fragmentType = f;
			fragmentMassTolerance = tol;
            msgfPreFilter = msgfnum;
		}
		#endregion

		#region Initializers

		public void InitializeAScoreParameters(List<Mod.Modification> stat, List<Mod.TerminiModification> term, 
			List<Mod.DynamicModification> dynam, FragmentType f, double tol, double msgfnum)
		{
			
			staticMods = stat;
			terminiMods = term;
			dynamMods = dynam;
			fragmentType = f;
			fragmentMassTolerance = tol;
            msgfPreFilter = msgfnum;
		}

		public void InitializeAScoreParameters(List<Mod.Modification> stat, FragmentType f, double tol)
		{
			staticMods = stat;
			terminiMods = new List<Mod.TerminiModification>();
			//		dynamMods = new List<Mod.DynamicModification>();
			fragmentType = f;
			fragmentMassTolerance = tol;
		}

		public void InitializeAScoreParameters(FragmentType f, double tol)
		{
			staticMods = new List<Mod.Modification>();
			terminiMods = new List<Mod.TerminiModification>();
			//		dynamMods = new List<Mod.DynamicModification>();
			fragmentType = f;
			fragmentMassTolerance = tol;
		}

		#endregion

		#region Public Methods
		/// <summary>
		/// Make a copy of an ascoreparameters set
		/// </summary>
		/// <returns></returns>
		/// 
		public ParameterFileManager Copy()
		{
			return new ParameterFileManager(new List<Mod.Modification>(staticMods), new List<Mod.TerminiModification>(terminiMods),
				new List<Mod.DynamicModification>(dynamMods), fragmentType, fragmentMassTolerance, msgfPreFilter);

		}

		/// <summary>
		/// Parses the parameter file for ascore
		/// </summary>
		/// <param name="inputFile">name of the xml file</param>
		/// <returns>ascore parameters object</returns>
		public void ParseXml(string inputFile)
		{
			XmlDocument parameterFile = new XmlDocument();
			parameterFile.Load(new XmlTextReader(inputFile));

			XmlNodeList staticMod = parameterFile.SelectNodes("/Run/Modifications/StaticSeqModifications");
			XmlNodeList terminiMod = parameterFile.SelectNodes("/Run/Modifications/TerminiModifications");
			XmlNodeList dynamicMod = parameterFile.SelectNodes("/Run/Modifications/DynamicModifications");

			XmlNode massTolerance = parameterFile.SelectSingleNode("/Run/MassTolerance");

			XmlNode fragmentType = parameterFile.SelectSingleNode("/Run/FragmentType");

            XmlNode msgfFilter = parameterFile.SelectSingleNode("/Run/MSGFPreFilter");

            
			FragmentType f = GetFragmentType(fragmentType);
			double massTol = double.Parse(massTolerance.InnerText);
            double msgfTol = double.Parse(msgfFilter.InnerText);

			List<Modification> stat = new List<Modification>();
			List<TerminiModification> termMod = new List<TerminiModification>();
			List<DynamicModification> dynam = new List<DynamicModification>();

			int uniqueID = 0;

			foreach (XmlNode mod in staticMod)
			{
				foreach (XmlNode mod2 in mod.ChildNodes)
				{
					double massMonoIsotopic = 0.0;
					double massAverage = 0.0;
					char modSymbol = ' ';
					List<char> possibleModSites = new List<char>();
					foreach (XmlNode item in mod2.ChildNodes)
					{
						if (item.Name == "MassMonoIsotopic")
						{
							massMonoIsotopic = double.Parse(item.InnerText);
						}
						else if (item.Name == "MassAverage")
						{
							massAverage = double.Parse(item.InnerText);
						}
						else if (item.Name == "PossibleModSites")
						{
							foreach (XmlNode item2 in item.ChildNodes)
							{
								possibleModSites.Add(item2.InnerText[0]);
							}
						}					
					}
					Modification m = new Modification();
					m.MassMonoisotopic = massMonoIsotopic;
					m.MassAverage = massAverage;
					m.ModSymbol = modSymbol;
					m.PossibleModSites = possibleModSites;
					m.UniqueID = uniqueID++;
					stat.Add(m);
				}
			}

			foreach (XmlNode mod in terminiMod)
			{
				foreach (XmlNode mod2 in mod.ChildNodes)
				{
					double massMonoIsotopic = 0.0;
					double massAverage = 0.0;
					char modSymbol = ' ';
					List<char> possibleModSites = new List<char>();
					bool nTerm = false;
					bool cTerm = false;
					foreach (XmlNode item in mod2.ChildNodes)
					{
						if (item.Name == "MassMonoIsotopic")
						{
							massMonoIsotopic = double.Parse(item.InnerText);
						}
						else if (item.Name == "MassAverage")
						{
							massAverage = double.Parse(item.InnerText);
						}

						else if (item.Name == "Nterm")
						{
							nTerm = bool.Parse(item.InnerText);
						}
						else if (item.Name == "Cterm")
						{
							cTerm = bool.Parse(item.InnerText);
						}
					}

					TerminiModification m = new TerminiModification();
					m.MassMonoisotopic = massMonoIsotopic;
					m.MassAverage = massAverage;
					m.ModSymbol = modSymbol;
					m.PossibleModSites = possibleModSites;
					m.UniqueID = uniqueID++;
					m.nTerminus = nTerm;
					m.cTerminus = cTerm;
					termMod.Add(m);
				}


			}


			foreach (XmlNode mod in dynamicMod)
			{
				foreach (XmlNode mod2 in mod.ChildNodes)
				{
					double massMonoIsotopic = 0.0;
					double massAverage = 0.0;
					char modSymbol = ' ';
					List<char> possibleModSites = new List<char>();
					bool onN = false;
					bool onC = false;
					foreach (XmlNode item in mod2.ChildNodes)
					{
						if (item.Name == "MassMonoIsotopic")
						{
							massMonoIsotopic = double.Parse(item.InnerText);
						}
						else if (item.Name == "MassAverage")
						{
							massAverage = double.Parse(item.InnerText);
						}
						else if (item.Name == "ModificationSymbol")
						{
							modSymbol = item.InnerText[0];
						}
						else if (item.Name == "PossibleModSites")
						{
							foreach (XmlNode item2 in item.ChildNodes)
							{
								possibleModSites.Add(item2.InnerText[0]);
							}
						}
						else if (item.Name == "MaxPerSite")
						{

						}
						else if (item.Name == "OnN")
						{
							onN = bool.Parse(item.InnerText);
						}
						else if (item.Name == "OnC")
						{
							onC = bool.Parse(item.InnerText);
						}
					}
					DynamicModification m = new DynamicModification();
					m.MassMonoisotopic = massMonoIsotopic;
					m.MassAverage = massAverage;
					m.ModSymbol = modSymbol;
					m.PossibleModSites = possibleModSites;
					m.UniqueID = uniqueID++;
					m.OnN = onN;
					m.OnC = onC;
					dynam.Add(m);
				}
			
			}



			InitializeAScoreParameters(stat, termMod, dynam, f, massTol, msgfTol);
		}

		#endregion

		#region Private Methods
		/// <summary>
		/// Method to get fragment type from xml
		/// </summary>
		/// <param name="fragmentType">xmlnode with fragment type info</param>
		/// <returns>the type of fragmentation</returns>
		private static FragmentType GetFragmentType(XmlNode fragmentType)
		{
			FragmentType f = FragmentType.CID;
			if (Regex.IsMatch(fragmentType.InnerText, "CID"))
			{
				f = FragmentType.CID;
			}
			else if (Regex.IsMatch(fragmentType.InnerText,"ETD"))
			{
				f = FragmentType.ETD;
			}
			else if (Regex.IsMatch(fragmentType.InnerText, "HCD"))
			{
				f = FragmentType.HCD;
			}
			return f;
		}
		#endregion

	}
}
