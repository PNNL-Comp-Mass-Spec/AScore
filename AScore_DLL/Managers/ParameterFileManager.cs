//Joshua Aldrich

using System;
using System.Collections.Generic;
using System.Xml;
using AScore_DLL.Mod;
using System.Text.RegularExpressions;
using PRISM;

namespace AScore_DLL.Managers
{
    /// <summary>
    /// A class for managing xml input to ascore parameters
    /// </summary>
    public class ParameterFileManager : EventNotifier
    {
        // Ignore Spelling: ascore, Da, pre, pos, nterm, cterm

        #region Member Variables

        private FragmentType fragmentType;

        #endregion

        #region Public Properties

        /// <summary>
        /// Dynamic modifications
        /// </summary>
        public List<DynamicModification> DynamicMods { get; }

        /// <summary>
        /// Static Modifications
        /// </summary>
        public List<Modification> StaticMods { get; }

        /// <summary>
        /// N- and C- Terminus Modifications
        /// </summary>
        public List<Modification> TerminiMods { get; }

        public FragmentType FragmentType
        {
            get => fragmentType;
            set
            {
                fragmentType = value;

                switch (fragmentType)
                {
                    case FragmentType.CID:
                        FragmentMassTolerance = FragmentMassToleranceCID;
                        break;
                    case FragmentType.ETD:
                        FragmentMassTolerance = FragmentMassToleranceETD;
                        break;
                    case FragmentType.HCD:
                        FragmentMassTolerance = FragmentMassToleranceHCD;
                        break;
                }
            }
        }
        public double FragmentMassTolerance { get; private set; }
        public double MSGFPreFilter { get; private set; }

        public bool MultiDissociationParamFile { get; private set; }
        public double FragmentMassToleranceCID { get; private set; } = 0.5;
        public double FragmentMassToleranceETD { get; private set; } = 0.5;
        public double FragmentMassToleranceHCD { get; private set; } = 0.05;

        #endregion

        #region ParameterFileManager Constructors

        /// <summary>
        /// Constructor that accepts an AScore parameter file
        /// </summary>
        /// <param name="inputFile"></param>
        public ParameterFileManager(string inputFile)
        {
            DynamicMods = new List<DynamicModification>();
            StaticMods = new List<Modification>();
            TerminiMods = new List<Modification>();

            ParseXml(inputFile);

            if (MultiDissociationParamFile)
            {
                OnStatusEvent("CID Mass Tolerance: " + FragmentMassToleranceCID + " Da");
                OnStatusEvent("ETD Mass Tolerance: " + FragmentMassToleranceETD + " Da");
                OnStatusEvent("HCD Mass Tolerance: " + FragmentMassToleranceHCD + " Da");
            }
            else
            {
                OnStatusEvent("Fragment Type:  " + FragmentType);
                OnStatusEvent("Mass Tolerance: " + FragmentMassTolerance + " Da");
            }
        }

        /// <summary>
        /// Constructor that accepts modification information
        /// </summary>
        /// <param name="stat"></param>
        /// <param name="term"></param>
        /// <param name="dynam"></param>
        /// <param name="f"></param>
        /// <param name="tol"></param>
        /// <param name="msgfnum"></param>
        public ParameterFileManager(
            List<Modification> stat,
            List<Modification> term,
            List<DynamicModification> dynam,
            FragmentType f,
            double tol,
            double msgfnum)
        {
            DynamicMods = dynam ?? new List<DynamicModification>();
            StaticMods = stat ?? new List<Modification>();
            TerminiMods = term ?? new List<Modification>();

            fragmentType = f;
            FragmentMassTolerance = tol;
            MSGFPreFilter = msgfnum;
            MultiDissociationParamFile = false;
        }
        #endregion

        #region Initializers

        public void InitializeAScoreParameters(List<Modification> stat, List<Modification> term,
            List<DynamicModification> dynam, FragmentType f, double tol, double msgfnum)
        {
            ClearMods();

            DynamicMods.AddRange(dynam);
            StaticMods.AddRange(stat);
            TerminiMods.AddRange(term);

            fragmentType = f;
            FragmentMassTolerance = tol;
            MSGFPreFilter = msgfnum;
            MultiDissociationParamFile = false;
        }

        public void InitializeAScoreParameters(List<Modification> stat, FragmentType f, double tol)
        {
            ClearMods();

            StaticMods.AddRange(stat);

            fragmentType = f;
            FragmentMassTolerance = tol;
            MultiDissociationParamFile = false;
        }

        public void InitializeAScoreParameters(FragmentType f, double tol)
        {
            ClearMods();

            fragmentType = f;
            FragmentMassTolerance = tol;
            MultiDissociationParamFile = false;
        }

        #endregion

        #region Public Methods
        /// <summary>
        /// Make a copy of this class
        /// </summary>
        /// <returns></returns>
        ///
        public ParameterFileManager Copy()
        {
            return new ParameterFileManager(new List<Modification>(StaticMods), new List<Modification>(TerminiMods),
                new List<DynamicModification>(DynamicMods), fragmentType, FragmentMassTolerance, MSGFPreFilter);
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

            var massToleranceNode = parameterFile.SelectSingleNode("/Run/MassTolerance");
            var fragmentTypeNode = parameterFile.SelectSingleNode("/Run/FragmentType");
            var msgfFilterNode = parameterFile.SelectSingleNode("/Run/MSGFPreFilter");
            if (msgfFilterNode == null)
                throw new ArgumentOutOfRangeException("The MSGFPreFilter node was not found in XML file " + inputFile);

            var massTolCID = parameterFile.SelectSingleNode("/Run/CIDMassTolerance");
            var massTolETD = parameterFile.SelectSingleNode("/Run/ETDMassTolerance");
            var massTolHCD = parameterFile.SelectSingleNode("/Run/HCDMassTolerance");

            if ((fragmentTypeNode == null || massToleranceNode == null) && (massTolCID == null || massTolETD == null || massTolHCD == null))
                throw new ArgumentOutOfRangeException("The FragmentType and/or MassTolerance nodes were not found in XML file " + inputFile + ", and alternate parameters were not present");

            var f = FragmentType.CID;
            double massTol;
            var multiDissociationParams = false;
            if (fragmentTypeNode != null && massToleranceNode != null)
            {
                f = GetFragmentType(fragmentTypeNode);
                massTol = double.Parse(massToleranceNode.InnerText);
                MultiDissociationParamFile = false;
            }
            else
            {
                FragmentMassToleranceCID = double.Parse(massTolCID.InnerText);
                FragmentMassToleranceETD = double.Parse(massTolETD.InnerText);
                FragmentMassToleranceHCD = double.Parse(massTolHCD.InnerText);
                MultiDissociationParamFile = true;
                multiDissociationParams = true;
                massTol = FragmentMassToleranceCID;
            }

            var msgfTol = double.Parse(msgfFilterNode.InnerText);

            var uniqueID = 1;

            // Parse the static mods
            var staticModDefs = ParseXmlModInfo(parameterFile, "StaticSeqModifications", ref uniqueID, requireModSites: true);

            // Parse the N and C terminal mods
            var terminalModDefs = ParseXmlModInfo(parameterFile, "TerminiModifications", ref uniqueID, requireModSites: false);

            // Parse the dynamic mods
            var dynamicModDefs = ParseXmlDynamicModInfo(parameterFile, "DynamicModifications", ref uniqueID, requireModSites: true, requireModSymbol: true);

            InitializeAScoreParameters(staticModDefs, terminalModDefs, dynamicModDefs, f, massTol, msgfTol);
            MultiDissociationParamFile = multiDissociationParams;
        }

        private List<Modification> ParseXmlModInfo(XmlNode parameterFile, string sectionName, ref int uniqueID, bool requireModSites)
        {
            var modList = new List<Modification>();

            var modsToStore = ParseXmlDynamicModInfo(parameterFile, sectionName, ref uniqueID, requireModSites: requireModSites, requireModSymbol: false);

            foreach (var item in modsToStore)
            {
                var modEntry = new Modification(item);
                modList.Add(modEntry);
            }

            return modList;
        }

        private List<DynamicModification> ParseXmlDynamicModInfo(XmlNode parameterFile, string sectionName, ref int uniqueID, bool requireModSites, bool requireModSymbol)
        {
            var modList = new List<DynamicModification>();
            var modNumberInSection = 0;

            var xmlModInfo = parameterFile.SelectNodes("/Run/Modifications/" + sectionName);

            foreach (XmlNode mod in xmlModInfo)
            {
                foreach (XmlNode mod2 in mod.ChildNodes)
                {
                    var massMonoIsotopic = 0.0;
                    var massAverage = 0.0;
                    var modSymbol = ' ';
                    var possibleModSites = new List<char>();
                    var nTerminal = false;
                    var cTerminal = false;

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
                            OnErrorEvent("Invalid modification definition in section " + sectionName + ", MassMonoIsotopic is zero for mod #" + modNumberInSection);
                            continue;
                        }

                        if (requireModSymbol && modSymbol == ' ')
                        {
                            OnErrorEvent("Invalid modification definition in section " + sectionName + ", ModSymbol is empty is for mod #" + modNumberInSection);
                            continue;
                        }

                        if (requireModSites && possibleModSites.Count == 0)
                        {
                            OnErrorEvent("Invalid modification definition in section " + sectionName + ", PossibleModSites is missing and/or does not have any <Pos> sub-elements for mod #" + modNumberInSection);
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
        /// Clear the dynamic, static, and termini mod lists
        /// </summary>
        private void ClearMods()
        {
            StaticMods.Clear();
            DynamicMods.Clear();
            TerminiMods.Clear();
        }

        /// <summary>
        /// Method to get fragment type from xml
        /// </summary>
        /// <param name="fragmentTypeNode">XML node with fragment type info</param>
        /// <returns>the type of fragmentation</returns>
        private FragmentType GetFragmentType(XmlNode fragmentTypeNode)
        {
            var f = FragmentType.CID;
            if (Regex.IsMatch(fragmentTypeNode.InnerText, "CID"))
            {
                f = FragmentType.CID;
            }
            else if (Regex.IsMatch(fragmentTypeNode.InnerText, "ETD"))
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
