using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using AScore_DLL.Managers.DatasetManagers;
using AScore_DLL.Mod;

namespace AScore_DLL.Managers
{
	/// <summary>
	/// Override the parameter file modifications with modifications listed in the ModSummary file.
	/// </summary>
	public static class ModSummaryFileManager
	{
		private const string COL_SYMBOL = "Modification_Symbol";
		private const string COL_MASS = "Modification_Mass";
		private const string COL_RESIDUE = "Target_Residues";
		private const string COL_TYPE = "Modification_Type";
		private const string COL_TAG = "Mass_Correction_Tag";
		private const string COL_COUNT = "Occurrence_Count";

		public static void ReadModSummary(DtaManager dtaManager, DatasetManager datasetManager,
			ParameterFileManager ascoreParams)
		{
			var dName = dtaManager.DatasetName;
			var dParent = Path.GetDirectoryName(datasetManager.DatasetFilePath);//datasetManager.DatasetFilePath;
			//var combo = Path.Combine(dParent, dName);
			var files = Directory.GetFiles(dParent, dName + "*ModSummary.txt");
			if (files.Length > 0)
			{
				ReadModSummary(files[0], ascoreParams);
				System.Console.WriteLine("Modifications for Dataset: " + dName);
				foreach (var mod in ascoreParams.StaticMods)
				{
					WriteMod("Static,   ", mod);
				}
				foreach (var mod in ascoreParams.DynamicMods)
				{
					WriteMod("Dynamic,  ", mod);
				}
				foreach (var mod in ascoreParams.TerminiMods)
				{
					WriteMod("Terminus, ", mod);
				}
			}
		}

		private static void WriteMod(string type, Modification mod)
		{
			string residues = string.Empty;
			foreach (var res in mod.PossibleModSites)
			{
				residues += res;
			}
			if (mod.cTerminus)
			{
				residues += ">";
			}
			if (mod.nTerminus)
			{
				residues += "<";
			}
			System.Console.WriteLine("\t" + type + mod.MassMonoisotopic + " on " + residues);
		}

		public static void ReadModSummary(string modSummaryFilePath, ParameterFileManager ascoreParams)
		{
			if (string.IsNullOrWhiteSpace(modSummaryFilePath) || !File.Exists(modSummaryFilePath))
			{
				// No file, so do nothing.
				return;
			}

			ascoreParams.DynamicMods.Clear();
			ascoreParams.StaticMods.Clear();
			ascoreParams.TerminiMods.Clear();

			DataTable mods = Utilities.TextFileToDataTableAssignTypeString(modSummaryFilePath, false);
			for (int i = 0; i < mods.Rows.Count; i++)
			{
				string residues = (string) mods.Rows[i][COL_RESIDUE];

				switch ((string) mods.Rows[i][COL_TYPE])
				{
					case "T":
						ascoreParams.TerminiMods.Add(ParseMod(mods, i));
						break;
					case "S":
						ascoreParams.StaticMods.Add(ParseMod(mods, i));
						break;
					case "D":
						ascoreParams.DynamicMods.Add(ParseDynamicMod(mods, i));
						break;
				}
			}
		}

		private static Modification ParseMod(DataTable mods, int row)
		{
			return new Modification(ParseDynamicMod(mods, row));
		}

		private static DynamicModification ParseDynamicMod(DataTable mods, int row)
		{
			double massMonoIsotopic = double.Parse((string)mods.Rows[row][COL_MASS]);
			char symbol = ((string)mods.Rows[row][COL_SYMBOL])[0];
			string residues = (string)mods.Rows[row][COL_RESIDUE];
			var possibleModSites = new List<char>();
			bool nTerminal = false;
			bool cTerminal = false;

			if (residues.IndexOfAny(new char[]{'<', '['}) != -1)
			{
				// N-term residue, peptide (<) or protein ([)
				nTerminal = true;
			}
			else if (residues.IndexOfAny(new char[] { '>', ']' }) != -1)
			{
				// C-term residue, peptide (>) or protein (])
				cTerminal = true;
			}

			foreach (var res in residues)
			{
				if ("<>[]".IndexOf(res) == -1) // Ignore the terminal residues
				{
					possibleModSites.Add(res);
				}
			}

			return new DynamicModification
			{
				MassMonoisotopic = massMonoIsotopic,
				MassAverage = 0.0,
				ModSymbol = symbol,
				PossibleModSites = possibleModSites,
				nTerminus = nTerminal,
				cTerminus = cTerminal,
				UniqueID = row + 1
			};
		}
	}
}
