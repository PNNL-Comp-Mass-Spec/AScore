namespace AScore_DLL.Managers.DatasetManagers
{
    /// <summary>
    /// Track Inspect PSM results from a _fht.txt file
    /// </summary>
    public class InspectFHT : DatasetManager
    {
        public InspectFHT(string fhtFileName) : base(fhtFileName) { }

        public override void GetNextRow(out int scanNumber, out int scanCount, out int chargeState, out string peptideSeq,
            ref AScore_DLL.Managers.ParameterFileManager ascoreParam)
        {
            if (mDataTable.Columns.Contains(RESULTS_COL_JOB))
                m_jobNum = (string)mDataTable.Rows[mCurrentRow][RESULTS_COL_JOB];

            scanNumber = int.Parse((string)mDataTable.Rows[mCurrentRow]["Scan"]);
            scanCount = 1;
            chargeState = int.Parse((string)mDataTable.Rows[mCurrentRow]["Charge"]);
            peptideSeq = (string)mDataTable.Rows[mCurrentRow]["Peptide"];
        }

        public override void GetNextRow(out int scanNumber, out int scanCount, out int chargeState, out string peptideSeq, out double msgfScore, ref ParameterFileManager ascoreParam)
        {
            this.GetNextRow(out scanNumber, out scanCount, out chargeState, out peptideSeq, ref ascoreParam);

            msgfScore = 0;
            double.TryParse((string)mDataTable.Rows[mCurrentRow]["MSGF_SpecProb"],out msgfScore);
        }
    }
}
