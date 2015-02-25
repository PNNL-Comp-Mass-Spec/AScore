namespace AScore_DLL.Managers.DatasetManagers
{
    public class XTandemFHT : DatasetManager
    {
        public XTandemFHT(string fhtFileName) : base(fhtFileName) { }

        public override void GetNextRow(out int scanNumber, out int scanCount, out int chargeState, out string peptideSeq,
            ref AScore_DLL.Managers.ParameterFileManager ascoreParam)
        {
            if (dt.Columns.Contains(RESULTS_COL_JOB))
                m_jobNum = (string)dt.Rows[t][RESULTS_COL_JOB];

            scanNumber = int.Parse((string)dt.Rows[t]["Scan"]);
            scanCount = 1;
            chargeState = int.Parse((string)dt.Rows[t]["Charge"]);
            peptideSeq = (string)dt.Rows[t]["Peptide_Sequence"];
        }

        public override void GetNextRow(out int scanNumber, out int scanCount, out int chargeState, out string peptideSeq, out double msgfScore, ref ParameterFileManager ascoreParam)
        {
            this.GetNextRow(out scanNumber, out scanCount, out chargeState, out peptideSeq, ref ascoreParam);

            msgfScore = 0;
            double.TryParse((string)dt.Rows[t]["MSGF_SpecProb"], out msgfScore);
        }
    }
}
