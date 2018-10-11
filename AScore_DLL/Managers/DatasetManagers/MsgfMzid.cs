using System.Collections.Generic;
using System.Linq;
using AScore_DLL.Mod;
using PSI_Interface.IdentData;

namespace AScore_DLL.Managers.DatasetManagers
{
    public class MsgfMzid : DatasetManager
    {
        /// <summary>
        /// Default modification symbols
        /// </summary>
        public const string DEFAULT_MODIFICATION_SYMBOLS = "*#@$&!%~†‡¤º^`×÷+=ø¢";         // A few other possibilities: €£¥§

        public MsgfMzid(string mzidFileName) : base(mzidFileName, false)
        {
            // load mzid file;
            // obviously won't have a 'Job' number available
            var reader = new SimpleMZIdentMLReader();
            var mzidData = reader.Read(mzidFileName);
            data = mzidData.Identifications;

            AssignSymbolsToMods(mzidData.SearchModifications);

            // maxSteps normally set using DataTable information in base constructor
            maxSteps = data.Count;
        }

        private readonly List<SimpleMZIdentMLReader.SpectrumIdItem> data;

        private class SearchModificationAndSymbol
        {
            public SimpleMZIdentMLReader.SearchModification Mod { get; }
            public char Symbol { get; }

            public SearchModificationAndSymbol(SimpleMZIdentMLReader.SearchModification mod, char symbol)
            {
                Mod = mod;
                Symbol = symbol;
            }
        }

        private readonly List<SearchModificationAndSymbol> searchMods = new List<SearchModificationAndSymbol>();
        private readonly Dictionary<string, List<SearchModificationAndSymbol>> modLookup = new Dictionary<string, List<SearchModificationAndSymbol>>();

        private string FormatModName(SimpleMZIdentMLReader.Modification mod)
        {
            return $"{mod.Name}_{mod.Mass:F3}"; // using name and mass to compensate for "unknown modifications"
        }

        /// <summary>
        /// Populate the AScore modification parameters with the search modifications from the mzid file
        /// </summary>
        /// <param name="ascoreParams"></param>
        public void SetModifications(ParameterFileManager ascoreParams)
        {
            ascoreParams.DynamicMods.Clear();
            ascoreParams.StaticMods.Clear();
            ascoreParams.TerminiMods.Clear();

            if (searchMods.Count == 0)
            {
                return;
            }

            var idCounter = 1;
            foreach (var mod in searchMods)
            {
                var newMod = ConvertToAscoreMod(mod, idCounter++);
                if (mod.Mod.IsFixed)
                {
                    var newStaticMod = new Modification(newMod);
                    if (mod.Mod.IsNTerm || mod.Mod.IsCTerm)
                    {
                        // add a static terminal mod
                        ascoreParams.TerminiMods.Add(newStaticMod);
                    }
                    else
                    {
                        // add a static mod
                        ascoreParams.StaticMods.Add(newStaticMod);
                    }
                }
                else
                {
                    // add a dynamic mod
                    ascoreParams.DynamicMods.Add(newMod);
                }
            }
        }

        private DynamicModification ConvertToAscoreMod(SearchModificationAndSymbol mod, int id)
        {
            var residues = new List<char>();
            if (!string.IsNullOrWhiteSpace(mod.Mod.Residues))
            {
                residues.AddRange(mod.Mod.Residues.Where(x => x != '.')); // remove '.', which is used for N/C-term modifications that can affect any residue
            }

            return new DynamicModification
            {
                MassMonoisotopic = mod.Mod.Mass,
                MassAverage = 0.0,
                ModSymbol = mod.Symbol,
                PossibleModSites = residues,
                nTerminus = mod.Mod.IsNTerm,
                cTerminus = mod.Mod.IsCTerm,
                UniqueID = id
            };
        }

        private void AssignSymbolsToMods(IEnumerable<SimpleMZIdentMLReader.SearchModification> mods)
        {
            var currentSymbolIndex = 0;
            var nameMap = new Dictionary<string, SearchModificationAndSymbol>();
            foreach (var mod in mods)
            {
                var nameFmtted = FormatModName(mod);
                char symbol;
                if (nameMap.TryGetValue(nameFmtted, out var combined))
                {
                    symbol = combined.Symbol;
                    // if only the residues don't match, add the residue(s) to the existing mod entry, and go on to the next mod
                    if (combined.Mod.AreModificationsSimilar(mod))
                    {
                        combined.Mod.Residues += mod.Residues;
                        continue;
                    }

                    combined = new SearchModificationAndSymbol(mod, symbol);
                }
                else
                {
                    if (mod.IsFixed)
                    {
                        // Static mods don't get a symbol; use '-' as a placeholder
                        symbol = '-';
                    }
                    else if (currentSymbolIndex < DEFAULT_MODIFICATION_SYMBOLS.Length)
                    {
                        symbol = DEFAULT_MODIFICATION_SYMBOLS[currentSymbolIndex];
                        currentSymbolIndex++;
                    }
                    else
                    {
                        symbol = '~'; // TODO: if this is ever hit, then it should be more robust; this could be combatted by adding more symbols to DEFAULT_MODIFICATION_SYMBOLS...
                    }

                    combined = new SearchModificationAndSymbol(mod, symbol);
                    nameMap.Add(nameFmtted, combined);
                }

                if (!modLookup.TryGetValue(nameFmtted, out var similar))
                {
                    similar = new List<SearchModificationAndSymbol>();
                    modLookup.Add(nameFmtted, similar);
                }

                similar.Add(combined);
                searchMods.Add(combined);
            }
        }

        /// <summary>
        /// Insert modification symbols into the sequence, and add the pre/post residues.
        /// </summary>
        /// <param name="pepEv"></param>
        /// <returns></returns>
        private string GetSequenceWithMods(SimpleMZIdentMLReader.PeptideEvidence pepEv)
        {
            var peptide = pepEv.PeptideRef;
            var sequence = peptide.Sequence;
            var sequenceOrig = peptide.Sequence;
            var sequenceOrigLength = sequence.Length;
            foreach (var mod in peptide.Mods.OrderByDescending(x => x.Key)) // insert dynamic mods from last to first
            {
                var isCTerm = mod.Key > sequence.Length;
                var isNTerm = mod.Key == 0;
                var residue = ' ';
                if (isNTerm)
                {
                    residue = sequenceOrig[0];
                }
                else if (isCTerm)
                {
                    residue = sequenceOrig[sequenceOrig.Length - 1];
                }
                else
                {
                    residue = sequenceOrig[mod.Key - 1];
                }
                if (!modLookup.TryGetValue(FormatModName(mod.Value), out var matchingMods))
                {
                    // TODO: report an error!
                    continue;
                }

                var symbol = "";
                SearchModificationAndSymbol modAndSymbol = null;
                foreach (var matchingMod in matchingMods)
                {
                    if (matchingMod.Mod.Residues.Contains(residue))
                    {
                        modAndSymbol = matchingMod;
                        symbol = matchingMod.Symbol.ToString();
                        break;
                    }
                    if (isNTerm && matchingMod.Mod.IsNTerm && matchingMod.Mod.Residues.Contains("."))
                    {
                        modAndSymbol = matchingMod;
                        symbol = matchingMod.Symbol.ToString();
                        break;
                    }
                    if (isCTerm && matchingMod.Mod.IsCTerm && matchingMod.Mod.Residues.Contains("."))
                    {
                        modAndSymbol = matchingMod;
                        symbol = matchingMod.Symbol.ToString();
                        break;
                    }
                }

                // Do not add in static mod symbols
                if (modAndSymbol == null || modAndSymbol.Mod.IsFixed)
                {
                    continue;
                }

                var loc = mod.Key;
                if (loc > sequenceOrigLength)
                {
                    // C-terminal modification - the location is sequence length + 1, but it really just goes at the end.
                    loc = sequenceOrigLength;
                }
                var leftSide = sequence.Substring(0, loc);
                var rightSide = sequence.Substring(loc);
                sequence = leftSide + symbol + rightSide;
            }

            sequence = $"{pepEv.Pre}.{sequence}.{pepEv.Post}";

            return sequence;
        }

        public override int GetRowLength()
        {
            return data.Count;
        }

        public override void GetNextRow(out int scanNumber, out int scanCount, out int chargeState, out string peptideSeq, ref ParameterFileManager ascoreParam)
        {
            GetNextRow(out scanNumber, out scanCount, out chargeState, out peptideSeq, out _, ref ascoreParam);
        }

        public override void GetNextRow(out int scanNumber, out int scanCount, out int chargeState, out string peptideSeq, out double msgfScore, ref ParameterFileManager ascoreParam)
        {
            var id = data[mCurrentRow];
            scanNumber = id.ScanNum;
            scanCount = 1;
            chargeState = id.Charge;
            peptideSeq = GetSequenceWithMods(id.PepEvidence.First());
            msgfScore = id.SpecEv;

            if (id.AllParamsDict.TryGetValue("AssumedDissociationMethod", out var fragType))
            {
                switch (fragType.ToLower())
                {
                    case "hcd":
                        ascoreParam.FragmentType = FragmentType.HCD;
                        break;
                    case "etd":
                        ascoreParam.FragmentType = FragmentType.ETD;
                        break;
                    case "cid":
                        ascoreParam.FragmentType = FragmentType.CID;
                        break;
                    default:
                        ascoreParam.FragmentType = FragmentType.Unspecified;
                        break;
                }
            }
            else
            {
                ascoreParam.FragmentType = FragmentType.Unspecified;
            }
        }
    }
}
