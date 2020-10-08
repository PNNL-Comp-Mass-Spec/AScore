namespace AScore_DLL.Managers.DatasetManagers
{
    /// <summary>
    /// Track SEQUEST PSM results from a _fht.txt or _syn.txt file
    /// </summary>
    public class SequestFHT : DatasetManager
    {
        public SequestFHT(string fhtOrSynFilePath) : base(fhtOrSynFilePath) { }

        public override void GetNextRow(out int scanNumber, out int scanCount, out int chargeState,
            out string peptideSeq, ref AScore_DLL.Managers.ParameterFileManager ascoreParam)
        {
            if (mDataTable.Columns.Contains(RESULTS_COL_JOB))
            {
                m_jobNum = (string)mDataTable.Rows[mCurrentRow][RESULTS_COL_JOB];
                m_JobColumnDefined = true;
            }

            scanNumber = int.Parse((string)mDataTable.Rows[mCurrentRow]["ScanNum"]);
            scanCount = int.Parse((string)mDataTable.Rows[mCurrentRow]["ScanCount"]);
            chargeState = int.Parse((string)mDataTable.Rows[mCurrentRow]["ChargeState"]);
            peptideSeq = (string)mDataTable.Rows[mCurrentRow]["Peptide"];
        }

        public override void GetNextRow(out int scanNumber, out int scanCount, out int chargeState, out string peptideSeq, out double msgfScore, ref ParameterFileManager ascoreParam)
        {
            this.GetNextRow(out scanNumber, out scanCount, out chargeState, out peptideSeq, ref ascoreParam);

            msgfScore = 0;
            if (this.MSGFSpecProbColumnPresent)
            {
                var msgfSpecProb = (string)mDataTable.Rows[mCurrentRow]["MSGF_SpecProb"];
                if (!double.TryParse(msgfSpecProb, out msgfScore))
                    msgfScore = 1;
            }
        }
    }
}
