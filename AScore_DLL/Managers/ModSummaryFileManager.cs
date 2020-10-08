using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using AScore_DLL.Mod;
using PRISM;

namespace AScore_DLL.Managers
{
    /// <summary>
    /// Override the parameter file modifications with modifications listed in the ModSummary file.
    /// </summary>
    public class ModSummaryFileManager : EventNotifier
    {
        private const string COL_SYMBOL = "Modification_Symbol";
        private const string COL_MASS = "Modification_Mass";
        private const string COL_RESIDUE = "Target_Residues";
        private const string COL_TYPE = "Modification_Type";
        private const string COL_TAG = "Mass_Correction_Tag";
        private const string COL_COUNT = "Occurrence_Count";

        /// <summary>
        /// Look for the mod summary file for the given PSM results file
        /// </summary>
        /// <param name="datasetName"></param>
        /// <param name="psmResultsFilePath"></param>
        /// <param name="ascoreParams"></param>
        /// <returns>True if a mod summary file is found and successfully processed, otherwise false</returns>
        public bool ReadModSummary(
            string datasetName,
            string psmResultsFilePath,
            ParameterFileManager ascoreParams)
        {
            var psmResultsFile = new FileInfo(psmResultsFilePath);
            var workingDirectory = psmResultsFile.Directory;
            if (workingDirectory == null)
                return false;

            var modSummaryFileSpec = datasetName + "*ModSummary.txt";

            var modSummaryFiles = workingDirectory.GetFiles(modSummaryFileSpec).ToList();
            if (modSummaryFiles.Count == 0)
            {
                if (ascoreParams.DynamicMods.Count == 0)
                {
                    OnWarningEvent(string.Format(
                        "ModSummary.txt file not found; PSMs may not be recognized properly;\nLooked for {0} in directory {1}",
                        modSummaryFileSpec, PathUtils.CompactPathString(workingDirectory.FullName, 100)));
                }
                return false;
            }

            var modSummaryFile = modSummaryFiles.First();

            var success = ReadModSummary(modSummaryFile, ascoreParams);

            return success;
        }

        /// <summary>
        /// Read mod information from the specified mod summary file
        /// </summary>
        /// <param name="modSummaryFile"></param>
        /// <param name="ascoreParams"></param>
        /// <returns>True if the mod summary file exists, otherwise false</returns>
        public bool ReadModSummary(FileInfo modSummaryFile, ParameterFileManager ascoreParams)
        {
            if (!modSummaryFile.Exists)
            {
                OnWarningEvent("File not found: " + modSummaryFile.FullName);
                return false;
            }

            ascoreParams.DynamicMods.Clear();
            ascoreParams.StaticMods.Clear();
            ascoreParams.TerminiMods.Clear();

            var mods = Utilities.TextFileToDataTableAssignTypeString(modSummaryFile.FullName);

            for (var i = 0; i < mods.Rows.Count; i++)
            {
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

            OnStatusEvent("Loaded modifications from: " + modSummaryFile.Name);

            foreach (var mod in ascoreParams.StaticMods)
            {
                OnStatusEvent(Utilities.GetModDescription("Static,   ", mod));
            }

            foreach (var mod in ascoreParams.DynamicMods)
            {
                OnStatusEvent(Utilities.GetModDescription("Dynamic,  ", mod));
            }

            foreach (var mod in ascoreParams.TerminiMods)
            {
                OnStatusEvent(Utilities.GetModDescription("Terminus, ", mod));
            }

            return true;
        }

        private Modification ParseMod(DataTable mods, int row)
        {
            return new Modification(ParseDynamicMod(mods, row));
        }

        private DynamicModification ParseDynamicMod(DataTable mods, int row)
        {
            var massMonoIsotopic = double.Parse((string)mods.Rows[row][COL_MASS]);
            var symbol = ((string)mods.Rows[row][COL_SYMBOL])[0];
            var residues = (string)mods.Rows[row][COL_RESIDUE];
            var possibleModSites = new List<char>();
            var nTerminal = false;
            var cTerminal = false;

            if (residues.IndexOfAny(new[] { '<', '[' }) != -1)
            {
                // N-term residue, peptide (<) or protein ([)
                nTerminal = true;
            }
            else if (residues.IndexOfAny(new[] { '>', ']' }) != -1)
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
