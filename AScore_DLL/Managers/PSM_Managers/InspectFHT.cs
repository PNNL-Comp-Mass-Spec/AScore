namespace AScore_DLL.Managers.PSM_Managers
{
    /// <summary>
    /// Track Inspect PSM results from a _fht.txt file
    /// </summary>
    public class InspectFHT : PsmResultsManager
    {
        public InspectFHT(string fhtOrSynFilePath) : base(fhtOrSynFilePath) { }

        public override void GetNextRow(out int scanNumber, out int scanCount, out int chargeState, out string peptideSeq,
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
            peptideSeq = (string)mDataTable.Rows[mCurrentRow]["Peptide"];
        }

        public override void GetNextRow(out int scanNumber, out int scanCount, out int chargeState, out string peptideSeq, out double msgfScore, ref ParameterFileManager ascoreParams)
        {
            this.GetNextRow(out scanNumber, out scanCount, out chargeState, out peptideSeq, ref ascoreParams);

            msgfScore = 0;
            double.TryParse((string)mDataTable.Rows[mCurrentRow]["MSGF_SpecProb"],out msgfScore);
        }
    }
}
