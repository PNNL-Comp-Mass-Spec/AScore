using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using AScore_DLL.Mod;
using PSI_Interface;
using PSI_Interface.CV;
using PSI_Interface.IdentData;
using PSI_Interface.IdentData.IdentDataObjs;

namespace AScore_DLL.Managers.DatasetManagers
{
    /// <summary>
    /// Track PSM results from a .mzid file
    /// </summary>
    public class MsgfMzidFull : DatasetManager
    {
        // Ignore Spelling: hcd, etd, cid, pre, Ident, namespace, unimod, ascore

        /// <summary>
        /// Default modification symbols
        /// </summary>
        public const string DEFAULT_MODIFICATION_SYMBOLS = "*#@$&!%~†‡¤º^`×÷+=ø¢";         // A few other possibilities: €£¥§

        private readonly IdentDataObj identData;

        public MsgfMzidFull(string mzidFileName) : base(mzidFileName, false)
        {
            // load mzid file;
            // obviously won't have a 'Job' number available
            identData = IdentDataReaderWriter.Read(mzidFileName);
            identData.Version = "1.2"; // Instruct the output to use MzIdentML 1.2 schema and namespace
            var ascoreSoftware = new AnalysisSoftwareObj()
                {
                    Id = "AScore_software",
                    Name = "AScore",
                    Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(),
                    SoftwareName = new ParamObj() { Item = new CVParamObj(CV.CVID.MS_Ascore_software) },
                };
            identData.AnalysisSoftwareList.Add(ascoreSoftware);

            identData.AnalysisProtocolCollection.SpectrumIdentificationProtocols.First().AdditionalSearchParams.Items.Add(new CVParamObj(CV.CVID.MS_modification_localization_scoring));

            foreach (var peptide in identData.SequenceCollection.Peptides)
            {
                var modIndex = 1;
                foreach (var mod in peptide.Modifications)
                {
                    mod.CVParams.Add(new CVParamObj(CV.CVID.MS_modification_index, modIndex.ToString()));
                    modIndex++;
                }
            }

            foreach (var list in identData.DataCollection.AnalysisData.SpectrumIdentificationList)
            {
                foreach (var result in list.SpectrumIdentificationResults)
                {
                    foreach (var ident in result.SpectrumIdentificationItems)
                    {
                        data.Add(new SpectrumIdentificationItemWrapper(result, ident));
                    }
                }
            }

            AssignSymbolsToMods(identData.AnalysisProtocolCollection.SpectrumIdentificationProtocols.SelectMany(x => x.ModificationParams)); // TODO

            // maxSteps normally set using DataTable information in base constructor
            maxSteps = data.Count;
        }

        private readonly List<SpectrumIdentificationItemWrapper> data = new List<SpectrumIdentificationItemWrapper>();

        private class SpectrumIdentificationItemWrapper
        {
            public SpectrumIdentificationItemObj SpectrumIdentification { get; }
            public SpectrumIdentificationResultObj SpectrumResult { get; }

            public SpectrumIdentificationItemWrapper(SpectrumIdentificationResultObj specResult, SpectrumIdentificationItemObj specIdent)
            {
                SpectrumResult = specResult;
                SpectrumIdentification = specIdent;
            }
        }

        private class SearchModificationAndSymbol
        {
            public SearchModificationObj Mod { get; }
            public char Symbol { get; }
            public bool IsNTerm { get; }
            public bool IsCTerm { get; }
            public string Residues { get; private set; }

            public void AddResidues(string residues)
            {
                Residues += residues;
            }

            public SearchModificationAndSymbol(SearchModificationObj mod, char symbol)
            {
                Mod = mod;
                Symbol = symbol;
                IsNTerm = GetIsNTerm(mod);
                IsCTerm = GetIsCTerm(mod);
                Residues = mod.Residues;
            }
        }

        private readonly List<SearchModificationAndSymbol> searchMods = new List<SearchModificationAndSymbol>();
        private readonly Dictionary<string, List<SearchModificationAndSymbol>> modLookup = new Dictionary<string, List<SearchModificationAndSymbol>>();

        private string FormatModName(string modName, double modMass)
        {
            return $"{modName}_{modMass:F3}"; // using name and mass to compensate for "unknown modifications"
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
                if (mod.Mod.FixedMod)
                {
                    var newStaticMod = new Modification(newMod);
                    if (mod.IsNTerm || mod.IsCTerm)
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
            if (!string.IsNullOrWhiteSpace(mod.Residues))
            {
                residues.AddRange(mod.Residues.Where(x => x != '.')); // remove '.', which is used for N/C-term modifications that can affect any residue
            }

            return new DynamicModification
            {
                MassMonoisotopic = mod.Mod.MassDelta,
                MassAverage = 0.0,
                ModSymbol = mod.Symbol,
                PossibleModSites = residues,
                nTerminus = mod.IsNTerm,
                cTerminus = mod.IsCTerm,
                UniqueID = id
            };
        }

        private bool AreModificationsSimilar(SearchModificationObj sm1, SearchModificationObj sm2)
        {
            return sm1.FixedMod == sm2.FixedMod && sm1.MassDelta.Equals(sm2.MassDelta) && GetModName(sm1.CVParams).Equals(GetModName(sm2.CVParams)) &&
                   GetIsNTerm(sm1) == GetIsNTerm(sm2) && GetIsCTerm(sm1) == GetIsCTerm(sm2);
        }

        private static string GetModName(IEnumerable<CVParamObj> cvps)
        {
            foreach (var cvp in cvps)
            {
                if (cvp.Accession.IndexOf("unimod", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                    cvp.Cvid == CV.CVID.MS_unknown_modification)
                {
                    return cvp.Name;
                }
            }

            return "unknown modification";
        }

        private static bool GetIsNTerm(SearchModificationObj sm)
        {
            if (sm.SpecificityRules != null && sm.SpecificityRules.Count > 0)
            {
                var specRules = sm.SpecificityRules.First(); // Only 0 or 1 allowed
                var peptide = specRules.CVParams.GetCvParam(CV.CVID.MS_modification_specificity_peptide_N_term, "");
                var protein = specRules.CVParams.GetCvParam(CV.CVID.MS_modification_specificity_protein_N_term, "");
                return peptide.Cvid == CV.CVID.MS_modification_specificity_peptide_N_term ||
                       protein.Cvid == CV.CVID.MS_modification_specificity_protein_N_term;
            }

            return false;
        }

        private static bool GetIsCTerm(SearchModificationObj sm)
        {
            if (sm.SpecificityRules != null && sm.SpecificityRules.Count > 0)
            {
                var specRules = sm.SpecificityRules.First(); // Only 0 or 1 allowed
                var peptide = specRules.CVParams.GetCvParam(CV.CVID.MS_modification_specificity_peptide_C_term, "");
                var protein = specRules.CVParams.GetCvParam(CV.CVID.MS_modification_specificity_protein_C_term, "");
                return peptide.Cvid == CV.CVID.MS_modification_specificity_peptide_C_term ||
                       protein.Cvid == CV.CVID.MS_modification_specificity_protein_C_term;
            }

            return false;
        }

        private void AssignSymbolsToMods(IEnumerable<SearchModificationObj> mods)
        {
            var currentSymbolIndex = 0;
            var nameMap = new Dictionary<string, SearchModificationAndSymbol>();
            foreach (var mod in mods)
            {
                var formattedModName = FormatModName(GetModName(mod.CVParams), mod.MassDelta);
                char symbol;
                if (nameMap.TryGetValue(formattedModName, out var combined))
                {
                    symbol = combined.Symbol;
                    // if only the residues don't match, add the residue(s) to the existing mod entry, and go on to the next mod
                    if (AreModificationsSimilar(combined.Mod, mod))
                    {
                        combined.AddResidues(mod.Residues);
                        continue;
                    }

                    combined = new SearchModificationAndSymbol(mod, symbol);
                }
                else
                {
                    if (mod.FixedMod)
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
                        // TODO: if this is ever hit, then it should be more robust;
                        // This could be addressed by adding more symbols to DEFAULT_MODIFICATION_SYMBOLS
                        symbol = '~';
                    }

                    combined = new SearchModificationAndSymbol(mod, symbol);
                    nameMap.Add(formattedModName, combined);
                }

                if (!modLookup.TryGetValue(formattedModName, out var similar))
                {
                    similar = new List<SearchModificationAndSymbol>();
                    modLookup.Add(formattedModName, similar);
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
        private string GetSequenceWithMods(PeptideEvidenceObj pepEv)
        {
            var peptide = pepEv.Peptide;
            var sequence = peptide.PeptideSequence;
            var sequenceOrig = peptide.PeptideSequence;
            var sequenceOrigLength = sequence.Length;
            foreach (var mod in peptide.Modifications.OrderByDescending(x => x.Location)) // insert dynamic mods from last to first
            {
                var isCTerm = mod.Location > sequence.Length;
                var isNTerm = mod.Location == 0;
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
                    residue = sequenceOrig[mod.Location - 1];
                }
                if (!modLookup.TryGetValue(FormatModName(GetModName(mod.CVParams), mod.MonoisotopicMassDelta), out var matchingMods))
                {
                    // TODO: report an error!
                    continue;
                }

                var symbol = "";
                SearchModificationAndSymbol modAndSymbol = null;
                foreach (var matchingMod in matchingMods)
                {
                    if (matchingMod.Residues.Contains(residue))
                    {
                        modAndSymbol = matchingMod;
                        symbol = matchingMod.Symbol.ToString();
                        break;
                    }
                    if (isNTerm && matchingMod.IsNTerm && matchingMod.Residues.Contains("."))
                    {
                        modAndSymbol = matchingMod;
                        symbol = matchingMod.Symbol.ToString();
                        break;
                    }
                    if (isCTerm && matchingMod.IsCTerm && matchingMod.Residues.Contains("."))
                    {
                        modAndSymbol = matchingMod;
                        symbol = matchingMod.Symbol.ToString();
                        break;
                    }
                }

                // Do not add in static mod symbols
                if (modAndSymbol == null || modAndSymbol.Mod.FixedMod)
                {
                    continue;
                }

                var loc = mod.Location;
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
            ascoreResultsCount = 0;

            scanNumber = 0;
            if (NativeIdConversion.TryGetScanNumberInt(id.SpectrumResult.SpectrumID, out var scanNum))
            {
                scanNumber = scanNum;
            }

            scanCount = 1;
            chargeState = id.SpectrumIdentification.ChargeState;
            peptideSeq = GetSequenceWithMods(id.SpectrumIdentification.PeptideEvidences.First().PeptideEvidence);
            msgfScore = id.SpectrumIdentification.GetSpecEValue();

            var scanNumMatch = id.SpectrumResult.CVParams.GetCvParam(CV.CVID.MS_scan_number_s__OBSOLETE, "");
            if (!string.IsNullOrWhiteSpace(scanNumMatch.Value) && int.TryParse(scanNumMatch.Value, out scanNum))
            {
                // if this is available, this is more accurate than the parsed NativeID number (should be identical for most mzML input searches, much different for *_dta.txt input searches)
                scanNumber = scanNum;
            }

            var fragMatches = id.SpectrumIdentification.UserParams.Where(x => x.Name.Equals("AssumedDissociationMethod")).ToList();

            if (fragMatches.Count > 0)
            {
                switch (fragMatches[0].Value.ToLower())
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

        private int ascoreResultsCount = 0;

        /// <summary>
        /// Writes ascore information to table
        /// </summary>
        /// <param name="peptideSeq"></param>
        /// <param name="bestSeq"></param>
        /// <param name="scanNum"></param>
        /// <param name="topPeptideScore"></param>
        /// <param name="ascoreResult"></param>
        public override void WriteToTable(string peptideSeq, string bestSeq, int scanNum, double topPeptideScore, AScoreResult ascoreResult)
        {
            base.WriteToTable(peptideSeq, bestSeq, scanNum, topPeptideScore, ascoreResult);

            //var newRow = mAScoresTable.NewRow();
            //
            //newRow[RESULTS_COL_JOB] = m_jobNum;
            //newRow[RESULTS_COL_SCAN] = scanNum;
            //newRow[RESULTS_COL_ORIGINAL_SEQUENCE] = peptideSeq;
            //newRow[RESULTS_COL_BEST_SEQUENCE] = bestSeq;
            //newRow[RESULTS_COL_PEPTIDE_SCORE] = PRISM.StringUtilities.ValueToString(topPeptideScore);
            //
            //newRow[RESULTS_COL_ASCORE] = PRISM.StringUtilities.ValueToString(ascoreResult.AScore);
            //newRow[RESULTS_COL_NUM_SITE_IONS_POSS] = ascoreResult.NumSiteIons;
            //newRow[RESULTS_COL_NUM_SITE_IONS_MATCHED] = ascoreResult.SiteDetermineMatched;
            //newRow[RESULTS_COL_SECOND_SEQUENCE] = ascoreResult.SecondSequence;
            //newRow[RESULTS_COL_MOD_INFO] = ascoreResult.ModInfo;
            //
            //mAScoresTable.Rows.Add(newRow);

            ascoreResultsCount++;

            var id = data[mCurrentRow];
            if (ascoreResultsCount == 1)
            {
                id.SpectrumIdentification.UserParams.Add(new UserParamObj()
                {
                    Name = "AScore BestSequence",
                    Value = bestSeq,
                });

                id.SpectrumIdentification.UserParams.Add(new UserParamObj()
                {
                    Name = "AScore PeptideScore",
                    Value = topPeptideScore.ToString(CultureInfo.InvariantCulture),
                });
            }

            var modSymbol = ascoreResult.ModInfo.Last();
            var modIndex = GetModIndexForModSpec(id, modSymbol, ascoreResultsCount, out var modLocation);

            var ascoreFormatted = $"{modIndex}:{ascoreResult.AScore.ToString(CultureInfo.InvariantCulture)}:{modLocation}:true"; // TODO: need to modify the threshold (true/false); also, output the scores for the best/second sequence positions

            id.SpectrumIdentification.CVParams.Add(new CVParamObj(CV.CVID.MS_Ascore, ascoreFormatted));

            id.SpectrumIdentification.UserParams.Add(new UserParamObj()
            {
                Name = $"AScore PTM{ascoreResultsCount}",
                Value = ascoreResult.AScore.ToString(CultureInfo.InvariantCulture),
            });

            id.SpectrumIdentification.UserParams.Add(new UserParamObj()
            {
                Name = $"AScore PTM{ascoreResultsCount} SecondSequence",
                Value = ascoreResult.SecondSequence,
            });

            id.SpectrumIdentification.UserParams.Add(new UserParamObj()
            {
                Name = $"AScore PTM{ascoreResultsCount} numSiteIonsPoss",
                Value = ascoreResult.NumSiteIons.ToString(),
            });

            id.SpectrumIdentification.UserParams.Add(new UserParamObj()
            {
                Name = $"AScore PTM{ascoreResultsCount} numSiteIonsMatched",
                Value = ascoreResult.SiteDetermineMatched.ToString(),
            });
        }
        /*
         * TermData.Add(CVID.MS_Ascore_software, new TermInfo(CVID.MS_Ascore_software, @"MS", @"MS:1001984", @"Ascore software", @"Ascore software.", false));
         * TermData.Add(CVID.MS_Ascore, new TermInfo(CVID.MS_Ascore, @"MS", @"MS:1001985", @"Ascore", @"A-score for PTM site location at the PSM-level.", false));
         * TermData.Add(CVID.MS_peptide_Ascore, new TermInfo(CVID.MS_peptide_Ascore, @"MS", @"MS:1002551", @"peptide:Ascore", @"A-score for PTM site location at the peptide-level.", false));
         * TermData.Add(CVID.MS_Ascore_threshold, new TermInfo(CVID.MS_Ascore_threshold, @"MS", @"MS:1002556", @"Ascore threshold", @"Threshold for Ascore PTM site location score.", false));
         */

        /// <summary>
        /// Writes ascore information to table
        /// Call this function if a peptide has zero or just one modifiable site
        /// </summary>
        /// <param name="peptideSeq"></param>
        /// <param name="scanNum"></param>
        /// <param name="pScore"></param>
        /// <param name="positionList"></param>
        /// <param name="modInfo"></param>
        public override void WriteToTable(string peptideSeq, int scanNum, double pScore, int[] positionList, string modInfo)
        {
            base.WriteToTable(peptideSeq, scanNum, pScore, positionList, modInfo);

            var id = data[mCurrentRow];

            if (positionList.Count(x => x > 0) != 0)
            {
                id.SpectrumIdentification.UserParams.Add(new UserParamObj()
                {
                    Name = "AScore PeptideScore",
                    Value = pScore.ToString(CultureInfo.InvariantCulture),
                });

                const double ascoreScore = 1000.0;
                var modSymbol = modInfo.Last();
                var modIndex = GetModIndexForModSpec(id, modSymbol, 0, out var modLocation);

                var ascoreFormatted = $"{modIndex}:{ascoreScore.ToString(CultureInfo.InvariantCulture)}:{modLocation}:true";

                id.SpectrumIdentification.CVParams.Add(new CVParamObj(CV.CVID.MS_Ascore, ascoreFormatted));

                id.SpectrumIdentification.UserParams.Add(new UserParamObj()
                {
                    Name = "AScore PTM1",
                    Value = ascoreScore.ToString(CultureInfo.InvariantCulture),
                });
            }

            ascoreResultsCount++;
        }

        private int GetModIndexForModSpec(SpectrumIdentificationItemWrapper id, char modSymbol, int modCount, out int modLocation)
        {
            var mod = searchMods.First(x => x.Symbol == modSymbol);
            var modIndex = 0;
            var count = 0;
            modLocation = 0;
            foreach (var pepMod in id.SpectrumIdentification.Peptide.Modifications)
            {
                if (FormatModName(GetModName(mod.Mod.CVParams), mod.Mod.MassDelta)
                    .Equals(FormatModName(GetModName(pepMod.CVParams), pepMod.MonoisotopicMassDelta)))
                {
                    count++;
                    if (count < modCount)
                    {
                        continue;
                    }

                    modLocation = pepMod.Location;
                    var modIndexParam = pepMod.CVParams.GetCvParam(CV.CVID.MS_modification_index, "0");
                    if (modIndexParam.Cvid == CV.CVID.MS_modification_index)
                    {
                        modIndex = int.Parse(modIndexParam.Value);
                        break;
                    }
                }
            }

            return modIndex;
        }

        /// <summary>
        /// Writes the current dataset to file
        /// </summary>
        /// <param name="outputFilePath"></param>
        public void WriteToMzidFile(string outputFilePath)
        {
            IdentDataReaderWriter.Write(identData, outputFilePath);
        }
    }
}
