// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Constuctor in an obsolete class", Scope = "member", Target = "~M:AScore_DLL.Managers.SpectraManagers.MzXmlManager.#ctor(System.String,PHRPReader.PeptideMassCalculator)")]
[assembly: SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Keep for reference", Scope = "member", Target = "~M:AScore_DLL.AScoreProcessor.GetCurrentComboTheoreticalIons(System.Double,System.Double,System.Collections.Generic.Dictionary{System.Int32,AScore_DLL.Managers.ChargeStateIons})~System.Collections.Generic.List{System.Double}")]
[assembly: SuppressMessage("General", "RCS1079:Throwing of new NotImplementedException.", Justification = "Unimplemented method in an obsolete class", Scope = "member", Target = "~M:AScore_DLL.Managers.SpectraManagers.MzXmlManager.GetFilePath(System.String,System.String)~System.String")]
[assembly: SuppressMessage("General", "RCS1079:Throwing of new NotImplementedException.", Justification = "Unimplemented method in an obsolete class", Scope = "member", Target = "~M:AScore_DLL.Managers.SpectraManagers.MzXmlManager.OpenFile(System.String)")]
[assembly: SuppressMessage("Usage", "RCS1146:Use conditional access.", Justification = "Leave for readability", Scope = "member", Target = "~M:AScore_DLL.AScoreProcessor.RunAScoreOnSingleFile(AScore_DLL.AScoreOptions,AScore_DLL.Managers.SpectraManagers.SpectraManagerCache,AScore_DLL.Managers.PSM_Managers.PsmResultsManager,AScore_DLL.Managers.ParameterFileManager)")]
[assembly: SuppressMessage("Usage", "RCS1146:Use conditional access.", Justification = "Leave for readability", Scope = "member", Target = "~M:AScore_DLL.Managers.PSM_Managers.MsgfMzid.GetSequenceWithMods(PSI_Interface.IdentData.SimpleMZIdentMLReader.PeptideEvidence)~System.String")]
[assembly: SuppressMessage("Usage", "RCS1146:Use conditional access.", Justification = "Leave for readability", Scope = "member", Target = "~M:AScore_DLL.Managers.PSM_Managers.MsgfMzidFull.GetIsCTerm(PSI_Interface.IdentData.IdentDataObjs.SearchModificationObj)~System.Boolean")]
[assembly: SuppressMessage("Usage", "RCS1146:Use conditional access.", Justification = "Leave for readability", Scope = "member", Target = "~M:AScore_DLL.Managers.PSM_Managers.MsgfMzidFull.GetSequenceWithMods(PSI_Interface.IdentData.IdentDataObjs.PeptideEvidenceObj)~System.String")]
[assembly: SuppressMessage("Usage", "RCS1146:Use conditional access.", Justification = "Leave for readability", Scope = "member", Target = "~P:AScore_DLL.Managers.SpectraManagers.SpectraManagerCache.Initialized")]
[assembly: SuppressMessage("Usage", "RCS1246:Use element access.", Justification = "Prefer to use .First()", Scope = "member", Target = "~M:AScore_DLL.Managers.PSM_Managers.MsgfMzid.GetNextRow(System.Int32@,System.Int32@,System.Int32@,System.String@,System.Double@,AScore_DLL.Managers.ParameterFileManager@)")]
