//Joshua Aldrich

using System;
using System.Collections.Generic;
using System.Xml;
using AScore_DLL.Mod;
using System.Text.RegularExpressions;

namespace AScore_DLL.Managers
{
	/// <summary>
	/// A class for managing xml input to ascore parameters
	/// </summary>
	public class ParameterFileManager : MessageEventBase
	{

		#region Member Variables
		private List<Mod.Modification> staticMods;
		private List<Mod.Modification> terminiMods;
		private List<Mod.DynamicModification> dynamMods;
		private FragmentType fragmentType;
		private double fragmentMassTolerance;
        private double msgfPreFilter;
		#endregion



		#region Public Properties
		public List<Mod.Modification> StaticMods { get {return staticMods;} }
		public List<Mod.Modification> TerminiMods { get { return terminiMods; } }
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


		public ParameterFileManager(List<Mod.Modification> stat, List<Mod.Modification> term, 
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

		public void InitializeAScoreParameters(List<Mod.Modification> stat, List<Mod.Modification> term, 
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
			terminiMods = new List<Mod.Modification>();
			//		dynamMods = new List<Mod.DynamicModification>();
			fragmentType = f;
			fragmentMassTolerance = tol;
		}

		public void InitializeAScoreParameters(FragmentType f, double tol)
		{
			staticMods = new List<Mod.Modification>();
			terminiMods = new List<Mod.Modification>();
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
			return new ParameterFileManager(new List<Mod.Modification>(staticMods), new List<Mod.Modification>(terminiMods),
				new List<Mod.DynamicModification>(dynamMods), fragmentType, fragmentMassTolerance, msgfPreFilter);

		}

		/// <summary>
		/// Parses the parameter file for ascore
		/// </summary>
		/// <param name="inputFile">name of the xml file</param>
		/// <returns>ascore parameters object</returns>
		public void ParseXml(string inputFile)
		{
			var parameterFile = new XmlDocument();
			parameterFile.Load(new XmlTextReader(inputFile));

			XmlNode massToleranceNode = parameterFile.SelectSingleNode("/Run/MassTolerance");
			if (massToleranceNode == null)
				throw new ArgumentOutOfRangeException("The MassTolerance node was not found in XML file " + inputFile);

			XmlNode fragmentTypeNode = parameterFile.SelectSingleNode("/Run/FragmentType");
			if (fragmentTypeNode == null)
				throw new ArgumentOutOfRangeException("The FragmentType node was not found in XML file " + inputFile);

            XmlNode msgfFilterNode = parameterFile.SelectSingleNode("/Run/MSGFPreFilter");
			if (msgfFilterNode == null)
				throw new ArgumentOutOfRangeException("The MSGFPreFilter node was not found in XML file " + inputFile);
            
			FragmentType f = GetFragmentType(fragmentTypeNode);
			double massTol = double.Parse(massToleranceNode.InnerText);
            double msgfTol = double.Parse(msgfFilterNode.InnerText);

			int uniqueID = 1;

			// Parse the static mods
			List<Modification> staticModDefs = ParseXmlModInfo(parameterFile, "StaticSeqModifications", ref uniqueID, requireModSites: true);

			// Parse the N and C terminal mods
			List<Modification> terminalModDefs = ParseXmlModInfo(parameterFile, "TerminiModifications", ref uniqueID, requireModSites: false);

			// Parse the dynamic mods
			List<DynamicModification> dynamicModDefs = ParseXmlDynamicModInfo(parameterFile, "DynamicModifications", ref uniqueID, requireModSites: true, requireModSymbol: true);

			InitializeAScoreParameters(staticModDefs, terminalModDefs, dynamicModDefs, f, massTol, msgfTol);
		}


		private List<Modification> ParseXmlModInfo(XmlDocument parameterFile, string sectionName, ref int uniqueID, bool requireModSites)
		{
			var modList = new List<Modification>();

			List<DynamicModification> modsToStore = ParseXmlDynamicModInfo(parameterFile, sectionName, ref uniqueID, requireModSites: requireModSites, requireModSymbol: false);

			foreach (DynamicModification item in modsToStore)
			{
				var modEntry = new Modification(item);
				modList.Add(modEntry);
			}

			return modList;
		}

		private List<DynamicModification> ParseXmlDynamicModInfo(XmlDocument parameterFile, string sectionName, ref int uniqueID, bool requireModSites, bool requireModSymbol)
		{
			var modList = new List<DynamicModification>();
			int modNumberInSection = 0;

			XmlNodeList xmlModInfo = parameterFile.SelectNodes("/Run/Modifications/" + sectionName);

			foreach (XmlNode mod in xmlModInfo)
			{
				foreach (XmlNode mod2 in mod.ChildNodes)
				{
					double massMonoIsotopic = 0.0;
					double massAverage = 0.0;
					char modSymbol = ' ';
					var possibleModSites = new List<char>();
					bool nTerminal = false;
					bool cTerminal = false;

					if (mod2.Name.StartsWith("Mod"))
					{
						modNumberInSection++;

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
									if (item2.Name.StartsWith("Pos"))
										possibleModSites.Add(item2.InnerText[0]);
								}
							}
							else if (item.Name == "OnN" || item.Name == "Nterm")
							{
								nTerminal = bool.Parse(item.InnerText);
							}
							else if (item.Name == "OnC" || item.Name == "Cterm")
							{
								cTerminal = bool.Parse(item.InnerText);
							}
						}

						if (Math.Abs(massMonoIsotopic) < 1e-6)
						{
							ReportError("Invalid modification definition in section " + sectionName + ", MassMonoIsotopic is zero for mod #" + modNumberInSection);
							continue;
						}
						else if (requireModSymbol && modSymbol == ' ')
						{
							ReportError("Invalid modification definition in section " + sectionName + ", ModSymbol is empty is for mod #" + modNumberInSection);
							continue;
						}
						else if (requireModSites && possibleModSites.Count == 0)
						{
							ReportError("Invalid modification definition in section " + sectionName + ", PossibleModSites is missing and/or does not have any <Pos> sub-elements for mod #" + modNumberInSection);
							continue;
						}

						var m = new DynamicModification
						{
							MassMonoisotopic = massMonoIsotopic,
							MassAverage = massAverage,
							ModSymbol = modSymbol,
							PossibleModSites = possibleModSites,
							nTerminus = nTerminal,
							cTerminus = cTerminal,
							UniqueID = uniqueID++
						};

						modList.Add(m);
					}
				}
			}

			return modList;
		}

		#endregion

		#region Private Methods
		/// <summary>
		/// Method to get fragment type from xml
		/// </summary>
		/// <param name="fragmentTypeNode">xmlnode with fragment type info</param>
		/// <returns>the type of fragmentation</returns>
		private FragmentType GetFragmentType(XmlNode fragmentTypeNode)
		{
			var f = FragmentType.CID;
			if (Regex.IsMatch(fragmentTypeNode.InnerText, "CID"))
			{
				f = FragmentType.CID;
			}
			else if (Regex.IsMatch(fragmentTypeNode.InnerText,"ETD"))
			{
				f = FragmentType.ETD;
			}
			else if (Regex.IsMatch(fragmentTypeNode.InnerText, "HCD"))
			{
				f = FragmentType.HCD;
			}
			return f;
		}
		#endregion
		
	}
}
