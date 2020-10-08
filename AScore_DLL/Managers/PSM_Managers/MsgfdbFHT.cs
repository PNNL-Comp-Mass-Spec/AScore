namespace AScore_DLL.Managers.DatasetManagers
{
    /// <summary>
    /// Track MS-GF+ PSM results from a _fht.txt or _syn.txt file
    /// </summary>
    public class MsgfdbFHT : DatasetManager
    {
        public MsgfdbFHT(string fhtOrSynFilePath) : base(fhtOrSynFilePath) { }

        public override void GetNextRow(out int scanNumber, out int scanCount, out int chargeState, out string peptideSeq,
            ref ParameterFileManager ascoreParam)
        {
            if (mDataTable.Columns.Contains(RESULTS_COL_JOB))
                m_jobNum = (string)mDataTable.Rows[mCurrentRow][RESULTS_COL_JOB];

            scanNumber = int.Parse((string)mDataTable.Rows[mCurrentRow]["Scan"]);
            scanCount = 1;
            chargeState = int.Parse((string)mDataTable.Rows[mCurrentRow]["Charge"]);
            peptideSeq = (string)mDataTable.Rows[mCurrentRow]["Peptide"];

            if (mDataTable.Columns.Contains("FragMethod"))
            {
                var fragtype = ((string)mDataTable.Rows[mCurrentRow]["FragMethod"]).ToLower();

                switch (fragtype)
                {
                    case "hcd":
                        ascoreParam.FragmentType = FragmentType.HCD;
                        break;
                    case "etd":
                        ascoreParam.FragmentType = FragmentType.ETD;
                        break;
                    case "cid":
                        ascoreParam.FragmentType = FragmentType.CID;
                        break;
                    default:
                        ascoreParam.FragmentType = FragmentType.Unspecified;
                        break;
                }
            }
            else
                ascoreParam.FragmentType = FragmentType.Unspecified;
        }

        public override void GetNextRow(out int scanNumber, out int scanCount, out int chargeState, out string peptideSeq, out double msgfScore, ref ParameterFileManager ascoreParam)
        {
            GetNextRow(out scanNumber, out scanCount, out chargeState, out peptideSeq, ref ascoreParam);

            msgfScore = 0;

            if (mDataTable.Columns.Contains("MSGF_SpecProb"))
            {
                if (!double.TryParse((string) mDataTable.Rows[mCurrentRow]["MSGF_SpecProb"], out msgfScore))
                    msgfScore = 0;
            }
            else if (mDataTable.Columns.Contains("MSGFDB_SpecEValue"))
            {
                if (!double.TryParse((string)mDataTable.Rows[mCurrentRow]["MSGFDB_SpecEValue"], out msgfScore))
                    msgfScore = 0;
            }
        }
    }
}
