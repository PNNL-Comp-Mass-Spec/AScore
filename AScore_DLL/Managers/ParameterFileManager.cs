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
        #region Member Variables
        private List<Modification> staticMods;
        private List<Modification> terminiMods;
        private List<DynamicModification> dynamMods;
        private FragmentType fragmentType;
        private double fragmentMassTolerance;
        private double fragmentMassToleranceCID = 0.5;
        private double fragmentMassToleranceETD = 0.5;
        private double fragmentMassToleranceHCD = 0.05;
        private double msgfPreFilter;
        #endregion

        #region Public Properties
        public List<Modification> StaticMods => staticMods;
        public List<Modification> TerminiMods => terminiMods;
        public List<DynamicModification> DynamicMods => dynamMods;

        public FragmentType FragmentType { get => fragmentType;
            set
            {
                fragmentType = value;

                switch (fragmentType)
                {
                    case FragmentType.CID:
                        fragmentMassTolerance = fragmentMassToleranceCID;
                        break;
                    case FragmentType.ETD:
                        fragmentMassTolerance = fragmentMassToleranceETD;
                        break;
                    case FragmentType.HCD:
                        fragmentMassTolerance = fragmentMassToleranceHCD;
                        break;
                }
            }
        }
        public double FragmentMassTolerance => fragmentMassTolerance;
        public double MSGFPreFilter => msgfPreFilter;

        public bool MultiDissociationParamFile { get; private set; }
        public double FragmentMassToleranceCID => fragmentMassToleranceCID;
        public double FragmentMassToleranceETD => fragmentMassToleranceETD;
        public double FragmentMassToleranceHCD => fragmentMassToleranceHCD;

        #endregion

        #region ParameterFileManager Constructors

        public ParameterFileManager(string inputFile)
        {
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

        public ParameterFileManager(List<Modification> stat, List<Modification> term,
            List<DynamicModification> dynam, FragmentType f, double tol, double msgfnum)
        {
            staticMods = stat;
            terminiMods = term;
            dynamMods = dynam;
            fragmentType = f;
            fragmentMassTolerance = tol;
            msgfPreFilter = msgfnum;
            MultiDissociationParamFile = false;
        }
        #endregion

        #region Initializers

        public void InitializeAScoreParameters(List<Modification> stat, List<Modification> term,
            List<DynamicModification> dynam, FragmentType f, double tol, double msgfnum)
        {
            staticMods = stat;
            terminiMods = term;
            dynamMods = dynam;
            fragmentType = f;
            fragmentMassTolerance = tol;
            msgfPreFilter = msgfnum;
            MultiDissociationParamFile = false;
        }

        public void InitializeAScoreParameters(List<Modification> stat, FragmentType f, double tol)
        {
            staticMods = stat;
            terminiMods = new List<Modification>();
            //      dynamMods = new List<Mod.DynamicModification>();
            fragmentType = f;
            fragmentMassTolerance = tol;
            MultiDissociationParamFile = false;
        }

        public void InitializeAScoreParameters(FragmentType f, double tol)
        {
            staticMods = new List<Modification>();
            terminiMods = new List<Modification>();
            //      dynamMods = new List<Mod.DynamicModification>();
            fragmentType = f;
            fragmentMassTolerance = tol;
            MultiDissociationParamFile = false;
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
            return new ParameterFileManager(new List<Modification>(staticMods), new List<Modification>(terminiMods),
                new List<DynamicModification>(dynamMods), fragmentType, fragmentMassTolerance, msgfPreFilter);
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
                fragmentMassToleranceCID = double.Parse(massTolCID.InnerText);
                fragmentMassToleranceETD = double.Parse(massTolETD.InnerText);
                fragmentMassToleranceHCD = double.Parse(massTolHCD.InnerText);
                MultiDissociationParamFile = true;
                multiDissociationParams = true;
                massTol = fragmentMassToleranceCID;
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
