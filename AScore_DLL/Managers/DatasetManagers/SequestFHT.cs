﻿namespace AScore_DLL.Managers.DatasetManagers
{
    public class SequestFHT : DatasetManager
    {
        public SequestFHT(string fhtFileName) : base(fhtFileName) { }

        public override void GetNextRow(out int scanNumber, out int scanCount, out int chargeState,
            out string peptideSeq, ref AScore_DLL.Managers.ParameterFileManager ascoreParam)
        {
            if (dt.Columns.Contains(RESULTS_COL_JOB))
                m_jobNum = (string)dt.Rows[t][RESULTS_COL_JOB];

            scanNumber = int.Parse((string)dt.Rows[t]["ScanNum"]);
            scanCount = int.Parse((string)dt.Rows[t]["ScanCount"]);
            chargeState = int.Parse((string)dt.Rows[t]["ChargeState"]);
            peptideSeq = (string)dt.Rows[t]["Peptide"];
        }

        public override void GetNextRow(out int scanNumber, out int scanCount, out int chargeState, out string peptideSeq, out double msgfScore, ref ParameterFileManager ascoreParam)
        {
            this.GetNextRow(out scanNumber, out scanCount, out chargeState, out peptideSeq, ref ascoreParam);

            msgfScore = 0;
            if (this.MSGFSpecProbColumnPresent)
            {
                var msgfSpecProb = (string)dt.Rows[t]["MSGF_SpecProb"];
                if (!double.TryParse(msgfSpecProb, out msgfScore))
                    msgfScore = 1;
            }
        }
    }
}
