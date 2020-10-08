namespace AScore_DLL.Managers.PSM_Managers
{
    /// <summary>
    /// Track X!Tandem PSM results from a _fht.txt or _syn.txt file
    /// </summary>
    public class XTandemFHT : PsmResultsManager
    {
        public XTandemFHT(string fhtOrSynFilePath) : base(fhtOrSynFilePath) { }

        public override void GetNextRow(
            out int scanNumber, out int scanCount, out int chargeState,
            out string peptideSeq,
            ref ParameterFileManager ascoreParams)
        {
            if (mDataTable.Columns.Contains(RESULTS_COL_JOB))
            {
                m_jobNum = (string)mDataTable.Rows[mCurrentRow][RESULTS_COL_JOB];
                m_JobColumnDefined = true;
            }

            scanNumber = int.Parse((string)mDataTable.Rows[mCurrentRow]["Scan"]);
            scanCount = 1;
            chargeState = int.Parse((string)mDataTable.Rows[mCurrentRow]["Charge"]);
            peptideSeq = (string)mDataTable.Rows[mCurrentRow]["Peptide_Sequence"];
        }

        public override void GetNextRow(
            out int scanNumber, out int scanCount, out int chargeState,
            out string peptideSeq, out double msgfScore, ref ParameterFileManager ascoreParams)
        {
            GetNextRow(out scanNumber, out scanCount, out chargeState, out peptideSeq, ref ascoreParams);
            double.TryParse((string)mDataTable.Rows[mCurrentRow]["MSGF_SpecProb"], out msgfScore);
        }
    }
}
