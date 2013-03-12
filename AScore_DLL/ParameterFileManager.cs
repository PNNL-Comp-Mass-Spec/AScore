//Joshua Aldrich

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using AScore_DLL.Mod;
using System.Text.RegularExpressions;

namespace AScore_DLL
{
	/// <summary>
	/// A class for managing xml input to ascore parameters
	/// </summary>
	public static class ParameterFileManager
	{
		/// <summary>
		/// Parses the parameter file for ascore
		/// </summary>
		/// <param name="inputFile">name of the xml file</param>
		/// <returns>ascore parameters object</returns>
		public static AScoreParameters ParseXml(string inputFile)
		{
			XmlDocument parameterFile = new XmlDocument();
			parameterFile.Load(new XmlTextReader(inputFile));

			XmlNodeList staticMod = parameterFile.SelectNodes("/Run/Modifications/StaticSeqModifications");
			XmlNodeList terminiMod = parameterFile.SelectNodes("/Run/Modifications/TerminiModifications");
			XmlNodeList dynamicMod = parameterFile.SelectNodes("/Run/Modifications/DynamicModifications");

			XmlNode massTolerance = parameterFile.SelectSingleNode("/Run/MassTolerance");

			XmlNode fragmentType = parameterFile.SelectSingleNode("/Run/FragmentType");



			FragmentType f = GetFragmentType(fragmentType);
			double massTol = double.Parse(massTolerance.InnerText);

			List<Modification> stat = new List<Modification>();
			List<TerminiModification> termMod = new List<TerminiModification>();
			List<DynamicModification> dynam = new List<DynamicModification>();


			foreach (XmlNode mod in staticMod)
			{
				foreach (XmlNode mod2 in mod.ChildNodes)
				{
					double massMonoIsotopic = 0.0;
					char modSymbol = ' ';
					List<char> possibleModSites = new List<char>();
					int uniqueID = 0;
					foreach (XmlNode item in mod2.ChildNodes)
					{
						if (item.Name == "MassMonoIsotopic")
						{
							massMonoIsotopic = double.Parse(item.InnerText);
						}
						else if (item.Name == "PossibleModSites")
						{
							foreach (XmlNode item2 in item.ChildNodes)
							{
								possibleModSites.Add(item2.InnerText[0]);
							}
						}
						else if (item.Name == "UniqueID")
						{
							uniqueID = int.Parse(item.InnerText);
						}
					}
					Modification m = new Modification();
					m.MassMonoisotopic = massMonoIsotopic;
					m.ModSymbol = modSymbol;
					m.PossibleModSites = possibleModSites;
					m.UniqueID = uniqueID;
					stat.Add(m);
				}
			}


			foreach (XmlNode mod in terminiMod)
			{
				foreach (XmlNode mod2 in mod.ChildNodes)
				{
					double massMonoIsotopic = 0.0;
					char modSymbol = ' ';
					List<char> possibleModSites = new List<char>();
					int uniqueID = 0;
					bool nTerm = false;
					bool cTerm = false;
					foreach (XmlNode item in mod2.ChildNodes)
					{
						if (item.Name == "MassMonoIsotopic")
						{
							massMonoIsotopic = double.Parse(item.InnerText);
						}
						else if (item.Name == "UniqueID")
						{
							uniqueID = int.Parse(item.InnerText);
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
					m.ModSymbol = modSymbol;
					m.PossibleModSites = possibleModSites;
					m.UniqueID = uniqueID;
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
					char modSymbol = ' ';
					List<char> possibleModSites = new List<char>();
					int uniqueID = 0;
					foreach (XmlNode item in mod2.ChildNodes)
					{
						if (item.Name == "MassMonoIsotopic")
						{
							massMonoIsotopic = double.Parse(item.InnerText);
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
						else if (item.Name == "UniqueID")
						{
							uniqueID = int.Parse(item.InnerText);
						}
						else if (item.Name == "MaxPerSite")
						{

						}
					}
					DynamicModification m = new DynamicModification();
					m.MassMonoisotopic = massMonoIsotopic;
					m.ModSymbol = modSymbol;
					m.PossibleModSites = possibleModSites;
					m.UniqueID = uniqueID;
					dynam.Add(m);
				}
			
			}



			AScoreParameters aParams = new AScoreParameters(stat, termMod, dynam, f, massTol);
			return aParams;

		}

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

	}
}
