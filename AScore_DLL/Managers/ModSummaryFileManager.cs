using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using AScore_DLL.Mod;

namespace AScore_DLL.Managers
{
    /// <summary>
    /// Override the parameter file modifications with modifications listed in the ModSummary file.
    /// </summary>
    public class ModSummaryFileManager : MessageEventBase
    {

        private const string COL_SYMBOL = "Modification_Symbol";
        private const string COL_MASS = "Modification_Mass";
        private const string COL_RESIDUE = "Target_Residues";
        private const string COL_TYPE = "Modification_Type";
        private const string COL_TAG = "Mass_Correction_Tag";
        private const string COL_COUNT = "Occurrence_Count";

        /// <summary>
        /// Look for the mod summary file for the given data file
        /// </summary>
        /// <param name="datasetName"></param>
        /// <param name="datasetFilePath"></param>
        /// <param name="ascoreParams"></param>
        /// <returns>True if a mod summary file is found and successfully processed, otherwise false</returns>
        public bool ReadModSummary(
            string datasetName,
            string datasetFilePath,
            ParameterFileManager ascoreParams)
        {
            var fiDataFile = new FileInfo(datasetFilePath);
            var diWorkingDirectory = fiDataFile.Directory;
            if (diWorkingDirectory == null)
                return false;

            var modSummaryFiles = diWorkingDirectory.GetFiles(datasetName + "*ModSummary.txt").ToList();
            if (modSummaryFiles.Count == 0)
                return false;

            var modSummaryFile = modSummaryFiles.First();

            ReadModSummary(modSummaryFile, ascoreParams);

            ReportMessage("Loaded modifications from: " + modSummaryFile.Name);

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

            return true;
        }

        private void WriteMod(string type, Modification mod)
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
            ReportMessage("    " + type + mod.MassMonoisotopic + " on " + residues);
        }

        public void ReadModSummary(FileInfo modSummaryFile, ParameterFileManager ascoreParams)
        {
            if (modSummaryFile == null || !modSummaryFile.Exists)
            {
                // No file, so do nothing.
                return;
            }

            ascoreParams.DynamicMods.Clear();
            ascoreParams.StaticMods.Clear();
            ascoreParams.TerminiMods.Clear();

            DataTable mods = Utilities.TextFileToDataTableAssignTypeString(modSummaryFile.FullName, false);
            for (int i = 0; i < mods.Rows.Count; i++)
            {
                var residues = (string)mods.Rows[i][COL_RESIDUE];

                switch ((string)mods.Rows[i][COL_TYPE])
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

        private Modification ParseMod(DataTable mods, int row)
        {
            return new Modification(ParseDynamicMod(mods, row));
        }

        private DynamicModification ParseDynamicMod(DataTable mods, int row)
        {
            double massMonoIsotopic = double.Parse((string)mods.Rows[row][COL_MASS]);
            char symbol = ((string)mods.Rows[row][COL_SYMBOL])[0];
            var residues = (string)mods.Rows[row][COL_RESIDUE];
            var possibleModSites = new List<char>();
            bool nTerminal = false;
            bool cTerminal = false;

            if (residues.IndexOfAny(new char[] { '<', '[' }) != -1)
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
