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
        // Ignore Spelling: ascore, Da, pre, pos, Nterm, Cterm

        #region Member Variables

        private FragmentType mFragmentType;

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
            get => mFragmentType;
            set
            {
                mFragmentType = value;

                switch (mFragmentType)
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

        /// <summary>
        /// MSGF SpecProb filter
        /// </summary>
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
        /// <param name="ascoreParameterFilePath"></param>
        public ParameterFileManager(string ascoreParameterFilePath)
        {
            DynamicMods = new List<DynamicModification>();
            StaticMods = new List<Modification>();
            TerminiMods = new List<Modification>();

            ParseXml(ascoreParameterFilePath);

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
        /// <param name="staticMods"></param>
        /// <param name="terminiMods"></param>
        /// <param name="dynamicMods"></param>
        /// <param name="fragmentType"></param>
        /// <param name="fragmentMassTolerance"></param>
        /// <param name="msgfSpecProbFilter"></param>
        public ParameterFileManager(
            List<Modification> staticMods,
            List<Modification> terminiMods,
            List<DynamicModification> dynamicMods,
            FragmentType fragmentType,
            double fragmentMassTolerance,
            double msgfSpecProbFilter)
        {
            DynamicMods = dynamicMods ?? new List<DynamicModification>();
            StaticMods = staticMods ?? new List<Modification>();
            TerminiMods = terminiMods ?? new List<Modification>();

            mFragmentType = fragmentType;
            FragmentMassTolerance = fragmentMassTolerance;
            MSGFPreFilter = msgfSpecProbFilter;
            MultiDissociationParamFile = false;
        }
        #endregion

        #region Initializers

        public void InitializeAScoreParameters(
            List<Modification> staticModDefs,
            List<Modification> terminalModDefs,
            List<DynamicModification> dynamicModDefs,
            FragmentType fragmentationMode,
            double massTolerance,
            double msgfSpecProbFilterThreshold)
        {
            ClearMods();

            DynamicMods.AddRange(dynamicModDefs);
            StaticMods.AddRange(staticModDefs);
            TerminiMods.AddRange(terminalModDefs);

            mFragmentType = fragmentationMode;
            FragmentMassTolerance = massTolerance;
            MSGFPreFilter = msgfSpecProbFilterThreshold;
            MultiDissociationParamFile = false;
        }

        public void InitializeAScoreParameters(List<Modification> staticModDefs, FragmentType fragmentationMode, double massTolerance)
        {
            ClearMods();

            StaticMods.AddRange(staticModDefs);

            mFragmentType = fragmentationMode;
            FragmentMassTolerance = massTolerance;
            MultiDissociationParamFile = false;
        }

        public void InitializeAScoreParameters(FragmentType fragmentationMode, double massTolerance)
        {
            ClearMods();

            mFragmentType = fragmentationMode;
            FragmentMassTolerance = massTolerance;
            MultiDissociationParamFile = false;
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Make a copy of this class
        /// </summary>
        ///
        // ReSharper disable once UnusedMember.Global
        public ParameterFileManager Clone()
        {
            return new ParameterFileManager(
                new List<Modification>(StaticMods),
                new List<Modification>(TerminiMods),
                new List<DynamicModification>(DynamicMods),
                mFragmentType,
                FragmentMassTolerance,
                MSGFPreFilter);
        }

        /// <summary>
        /// Parses the parameter file for ascore
        /// This parameter file defines mass tolerances by fragmentation type
        /// </summary>
        /// <param name="ascoreParameterFilePath">Path to the AScore Params xml file</param>
        /// <returns>ascore parameters object</returns>
        /// <remarks>
        /// The legacy version of the AScore parameter file has elements "FragmentType" and "MassTolerance" in the "Run" block
        /// The newer version of the AScore parameter file has elements "CIDMassTolerance", "ETDMassTolerance", and "HCDMassTolerance" in the "Run" block
        /// </remarks>
        public void ParseXml(string ascoreParameterFilePath)
        {
            var xmlNodes = new XmlDocument();
            xmlNodes.Load(new XmlTextReader(ascoreParameterFilePath));

            // First look for legacy element names
            var massToleranceNode = xmlNodes.SelectSingleNode("/Run/MassTolerance");
            var fragmentTypeNode = xmlNodes.SelectSingleNode("/Run/FragmentType");

            // Both legacy and current AScore parameter files should have a MSGFPreFilter element
            var msgfFilterNode = xmlNodes.SelectSingleNode("/Run/MSGFPreFilter");
            if (msgfFilterNode == null)
                throw new ArgumentOutOfRangeException("The MSGFPreFilter node was not found in XML file " + ascoreParameterFilePath);

            // Now look for newer element names
            var massTolCID = xmlNodes.SelectSingleNode("/Run/CIDMassTolerance");
            var massTolETD = xmlNodes.SelectSingleNode("/Run/ETDMassTolerance");
            var massTolHCD = xmlNodes.SelectSingleNode("/Run/HCDMassTolerance");

            if ((fragmentTypeNode == null || massToleranceNode == null) && (massTolCID == null || massTolETD == null || massTolHCD == null))
            {
                throw new Exception(string.Format(
                    "The FragmentType and/or MassTolerance nodes were not found in XML file {0}, and alternate parameters were not present",
                    ascoreParameterFilePath));
            }

            var fragmentationMode = FragmentType.CID;
            double massTolerance;
            var multiDissociationParams = false;
            if (fragmentTypeNode != null && massToleranceNode != null)
            {
                fragmentationMode = GetFragmentType(fragmentTypeNode);
                massTolerance = double.Parse(massToleranceNode.InnerText);
                MultiDissociationParamFile = false;
            }
            else
            {
                FragmentMassToleranceCID = double.Parse(massTolCID.InnerText);
                FragmentMassToleranceETD = double.Parse(massTolETD.InnerText);
                FragmentMassToleranceHCD = double.Parse(massTolHCD.InnerText);
                MultiDissociationParamFile = true;
                multiDissociationParams = true;
                massTolerance = FragmentMassToleranceCID;
            }

            var msgfSpecProbFilterThreshold = double.Parse(msgfFilterNode.InnerText);

            var uniqueID = 1;

            // Parse the static mods
            var staticModDefs = ParseXmlModInfo(ascoreParameterFilePath, xmlNodes, "StaticSeqModifications", ref uniqueID, requireModSites: true);

            // Parse the N and C terminal mods
            var terminalModDefs = ParseXmlModInfo(ascoreParameterFilePath, xmlNodes, "TerminiModifications", ref uniqueID, requireModSites: false);

            // Parse the dynamic mods
            var dynamicModDefs = ParseXmlDynamicModInfo(ascoreParameterFilePath, xmlNodes, "DynamicModifications", ref uniqueID, requireModSites: true, requireModSymbol: true);

            InitializeAScoreParameters(staticModDefs, terminalModDefs, dynamicModDefs, fragmentationMode, massTolerance, msgfSpecProbFilterThreshold);
            MultiDissociationParamFile = multiDissociationParams;
        }

        private List<Modification> ParseXmlModInfo(string ascoreParameterFilePath, XmlNode xmlNodes, string sectionName, ref int uniqueID, bool requireModSites)
        {
            var modList = new List<Modification>();

            var modsToStore = ParseXmlDynamicModInfo(ascoreParameterFilePath, xmlNodes, sectionName, ref uniqueID, requireModSites: requireModSites, requireModSymbol: false);

            foreach (var item in modsToStore)
            {
                var modEntry = new Modification(item);
                modList.Add(modEntry);
            }

            return modList;
        }

        private List<DynamicModification> ParseXmlDynamicModInfo(
            string ascoreParameterFilePath,
            XmlNode xmlNodes, string sectionName, ref int uniqueID, bool requireModSites, bool requireModSymbol)
        {
            var modList = new List<DynamicModification>();
            var modNumberInSection = 0;

            var xpath = "/Run/Modifications/" + sectionName;
            var xmlModInfo = xmlNodes.SelectNodes(xpath);

            if (xmlModInfo == null)
            {
                OnErrorEvent(string.Format("Section {0} not found in AScore parameter file {1}", xpath, ascoreParameterFilePath));
                return modList;
            }

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
