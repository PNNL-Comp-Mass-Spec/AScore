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
        public List<Modification> StaticMods { get {return staticMods;} }
        public List<Modification> TerminiMods { get { return terminiMods; } }
        public List<DynamicModification> DynamicMods { get { return dynamMods; }  }
        public FragmentType FragmentType { get { return fragmentType; }
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
        public double FragmentMassTolerance { get { return fragmentMassTolerance; } }
        public double MSGFPreFilter { get { return msgfPreFilter; } }

        public bool MultiDissociationParamFile { get; private set; }
        public double FragmentMassToleranceCID { get { return fragmentMassToleranceCID; } }
        public double FragmentMassToleranceETD { get { return fragmentMassToleranceETD; } }
        public double FragmentMassToleranceHCD { get { return fragmentMassToleranceHCD; } }
        #endregion



        #region ParameterFileManager Constructors

        public ParameterFileManager(string inputFile)
        {
            ParseXml(inputFile);

            if (MultiDissociationParamFile)
            {
                ReportMessage("CID Mass Tolerance: " + FragmentMassToleranceCID + " Da");
                ReportMessage("ETD Mass Tolerance: " + FragmentMassToleranceETD + " Da");
                ReportMessage("HCD Mass Tolerance: " + FragmentMassToleranceHCD + " Da");
            }
            else
            {
                ReportMessage("Fragment Type:  " + FragmentType);
                ReportMessage("Mass Tolerance: " + FragmentMassTolerance + " Da");
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

            XmlNode massToleranceNode = parameterFile.SelectSingleNode("/Run/MassTolerance");
            XmlNode fragmentTypeNode = parameterFile.SelectSingleNode("/Run/FragmentType");
            XmlNode msgfFilterNode = parameterFile.SelectSingleNode("/Run/MSGFPreFilter");
            if (msgfFilterNode == null)
                throw new ArgumentOutOfRangeException("The MSGFPreFilter node was not found in XML file " + inputFile);

            XmlNode massTolCID = parameterFile.SelectSingleNode("/Run/CIDMassTolerance");
            XmlNode massTolETD = parameterFile.SelectSingleNode("/Run/ETDMassTolerance");
            XmlNode massTolHCD = parameterFile.SelectSingleNode("/Run/HCDMassTolerance");

            if ((fragmentTypeNode == null || massToleranceNode == null) && (massTolCID == null || massTolETD == null || massTolHCD == null))
                throw new ArgumentOutOfRangeException("The FragmentType and/or MassTolerance nodes were not found in XML file " + inputFile + ", and alternate parameters were not present");

            var f = FragmentType.CID;
            double massTol;
            bool multiDissociationParams = false;
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

            double msgfTol = double.Parse(msgfFilterNode.InnerText);

            int uniqueID = 1;

            // Parse the static mods
            List<Modification> staticModDefs = ParseXmlModInfo(parameterFile, "StaticSeqModifications", ref uniqueID, requireModSites: true);

            // Parse the N and C terminal mods
            List<Modification> terminalModDefs = ParseXmlModInfo(parameterFile, "TerminiModifications", ref uniqueID, requireModSites: false);

            // Parse the dynamic mods
            List<DynamicModification> dynamicModDefs = ParseXmlDynamicModInfo(parameterFile, "DynamicModifications", ref uniqueID, requireModSites: true, requireModSymbol: true);

            InitializeAScoreParameters(staticModDefs, terminalModDefs, dynamicModDefs, f, massTol, msgfTol);
            MultiDissociationParamFile = multiDissociationParams;
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

                        if (requireModSymbol && modSymbol == ' ')
                        {
                            ReportError("Invalid modification definition in section " + sectionName + ", ModSymbol is empty is for mod #" + modNumberInSection);
                            continue;
                        }

                        if (requireModSites && possibleModSites.Count == 0)
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
